# Implementation Plan: Distributed Container PTY Proxy (Multi-EC2, No Port Forwarding)

## Context

The current `attachPtyProxy` function in `bff/src/proxy/ptyProxy.js` acts as a transparent WebSocket bridge:

```
Browser → BFF WebSocket (/proxy/:sessionId) → Container Sidecar (ws://host:port/pty)
```

The BFF looks up a session by ID (from an in-memory Map), retrieves `{ host, port }`, and opens a direct WebSocket to `ws://host:port/pty`. This works only when the BFF can directly reach the container's internal Docker bridge IP (`172.17.x.x`).

**The problem:** When Docker containers are spread across multiple EC2 instances (Linux EC2 for Linux containers, Windows EC2 for Windows containers) and the BFF is on a separate server:
- Docker bridge IPs (`172.17.x.x`) are **not routable** across EC2 boundaries
- The constraint prohibits Docker port publishing (`-p 8080:8080`) on containers
- The BFF has no direct network path to the container sidecar

---

## Critical Files

- `container-management-studio/bff/src/proxy/ptyProxy.js` — WebSocket bridge (core change target)
- `container-management-studio/bff/src/routes/sessions.js` — In-memory session store (`sessionId → { host, port }`)
- `container-management/ContainerManagerBackend/` — .NET backend that creates containers and registers sessions with BFF

---

## Current Architecture (Single-Server)

```
Browser
  │ WebSocket: /proxy/:sessionId
  ▼
BFF (Node.js, port 3000)
  │ WebSocket: ws://172.17.0.5:8080/pty  ← Direct, only works on same host
  ▼
Container Sidecar (Go: os-process-manager-service)
  - Port 8080: WebSocket /pty
  - Port 5001: gRPC
  └── Manages user process (node index.js)
```

Session registration flow (called by .NET backend after container starts):
```
POST /sessions { host: "172.17.0.5", port: 8080, label: "..." }
→ BFF stores { sessionId → { host, port } }
→ Returns { sessionId, wsUrl: "wss://public/proxy/:sessionId" }
```

---

## Implementation Plans

### Plan A: EC2 Relay Agent with Reverse WebSocket Tunnel ⭐ RECOMMENDED

**Concept:** Deploy a lightweight "relay agent" process on each EC2 instance. The agent initiates an **outbound** WebSocket connection to the BFF (reverse tunnel), so no inbound ports are needed on EC2. When the BFF needs to reach a container, it sends commands through this pre-established tunnel. The agent, being co-located on the same EC2 as the containers, can reach the container's sidecar via its Docker bridge IP.

**Connection flow:**
```
EC2 Agent startup:
  EC2 Agent → (outbound WS) → BFF: "register agentId=ec2-linux-1, os=linux"
  BFF: stores agentId → WebSocket connection

Terminal session:
  .NET Backend → POST /sessions { agentId: "ec2-linux-1", containerId: "abc123", port: 8080 }
  Browser → BFF WS (/proxy/:sessionId)
    → BFF sends via reverse tunnel: { action: "proxy", sessionId, containerId, port: 8080 }
    → Agent connects: ws://172.17.x.x:8080/pty  ← same EC2, reachable!
    → Agent relays binary frames through reverse tunnel ↔ BFF ↔ Browser
```

**Changes needed:**
- New service: **EC2 Relay Agent** (small Node.js or Go service per EC2 instance)
  - Connects outbound to BFF's agent registration endpoint
  - Handles proxy commands (connect to container sidecar, relay I/O)
- **BFF changes:**
  - New route: `WS /agents` for agent registration (agents connect here)
  - `ptyProxy.js`: instead of direct WS to container, route through agent tunnel for remote sessions
  - `sessions.js`: store `{ agentId, containerId, port }` alongside or instead of `{ host, port }`
- **.NET backend**: register sessions with `agentId` (agent ID = EC2 instance identifier)

**Pros:**
- No inbound ports on EC2 (agent connects out)
- Container sidecar unchanged — agent still uses `ws://172.17.x.x:8080/pty`
- Works identically on Windows (PowerShell/cmd) and Linux EC2
- Low latency — direct relay, no message broker

**Cons:**
- Must deploy and manage the agent on every EC2 instance
- BFF must handle agent disconnection/reconnection gracefully
- Multiplexing multiple concurrent sessions through one agent tunnel adds complexity

**Complexity:** Medium

---

### Plan B: Docker Remote API over TLS (No Sidecar WebSocket Needed)

**Concept:** Enable Docker's remote API (TLS) on each EC2. The BFF connects to the Docker daemon directly via `https://ec2-ip:2376` and uses Docker's exec API to open a PTY session inside the container — bypassing the sidecar WebSocket entirely.

**Connection flow:**
```
.NET Backend → POST /sessions { dockerHost: "https://ec2-linux-1:2376", containerId: "abc123" }
Browser → BFF WS (/proxy/:sessionId)
  → BFF: POST https://ec2-linux-1:2376/containers/abc123/exec { Cmd: ["sh"], Tty: true, ... }
  → BFF: POST https://ec2-linux-1:2376/exec/{execId}/start (HTTP upgrade → raw TCP stream)
  → BFF demultiplexes Docker stream → relays to Browser WS
```

**Changes needed:**
- Docker daemon TLS config on each EC2 (port 2376, mTLS with certs)
- Security group rule: BFF → EC2:2376
- `ptyProxy.js`: replace WS-to-sidecar with Docker API exec calls
- `sessions.js`: store `{ dockerHost, containerId }` instead of `{ host, port }`
- Protocol adapter: Docker exec stream (multiplexed TCP) → WebSocket binary frames

**Pros:**
- No custom agent to deploy
- No sidecar network exposure needed
- Full Docker API access (could replace other sidecar functionality too)
- Works on Linux and Windows Docker (same API)

**Cons:**
- Requires opening port 2376 on EC2 security groups (inbound from BFF)
- mTLS certificate management overhead
- Docker exec stream protocol is different from WebSocket — needs adapter code
- Exposes Docker daemon (even with TLS, a security-sensitive surface)

**Complexity:** Medium-High

---

### Plan C: Agent + Docker Exec (Sidecar Bypassed for PTY)

**Concept:** Same reverse-tunnel approach as Plan A, but the agent doesn't connect to the container's sidecar. Instead, the agent executes `docker exec -it <container> sh` directly (using the Docker socket) and streams the PTY I/O back through the tunnel. The sidecar is only used for other functions (process management, monitoring).

**Connection flow:**
```
Agent (same EC2 as containers)
  - Has access to Docker socket (/var/run/docker.sock or Windows named pipe)
  - Establishes reverse tunnel to BFF

Browser → BFF → (reverse tunnel) → Agent
  → Agent: docker.exec(containerId, ["sh", "-i"], tty: true)
  → Raw PTY stream ↔ Agent ↔ Tunnel ↔ BFF ↔ Browser WS
```

**Changes needed:**
- New EC2 Relay Agent with Docker SDK integration (Go preferred for cross-platform Docker socket access)
- BFF: same reverse tunnel changes as Plan A
- ptyProxy.js: route through agent tunnel
- `sessions.js`: store `{ agentId, containerId }` (no port needed)

**Pros:**
- No inbound EC2 ports
- No sidecar WebSocket port at all — removes networking concern completely
- Works cross-platform (Docker SDK works on Windows and Linux)

**Cons:**
- Shell differences: Linux uses `sh`/`bash`, Windows uses `cmd.exe`/`PowerShell` — agent must select the right shell
- Must handle PTY sizing (resize messages) via Docker exec resize API
- Agent is more complex than Plan A's simple WebSocket relay

**Complexity:** Medium-High

---

### Plan D: Redis Pub/Sub Message Broker

**Concept:** Use Redis (already in the stack for Hangfire) as a message relay. The BFF publishes keystrokes to a per-session Redis channel; the container sidecar subscribes and writes to its PTY. Output flows back the same way. No direct network path between BFF and container.

**Connection flow:**
```
Browser keystroke → BFF → PUBLISH "pty:{sessionId}:in" <data> → Redis
  Container sidecar subscribes → SUBSCRIBE "pty:{sessionId}:in"
  → writes to PTY
  PTY output → sidecar → PUBLISH "pty:{sessionId}:out" <data> → Redis
  BFF subscribes → SUBSCRIBE "pty:{sessionId}:out"
  → sends to Browser WS
```

**Changes needed:**
- Container sidecar (Go): replace WebSocket server with Redis Pub/Sub client (go-redis)
- `ptyProxy.js`: replace direct WS connection with Redis publish/subscribe
- `sessions.js`: store only `{ sessionId }` (no host/port needed)
- Redis must be reachable from both BFF and container EC2 instances (VPC routing or Redis cluster with public endpoint)

**Pros:**
- No direct networking between BFF and containers at all
- Naturally supports multiple BFF instances (horizontal scaling)
- Windows and Linux containers both just need a Redis client

**Cons:**
- **Latency**: every keystroke and output byte goes through Redis — adds ~1-5ms per round trip
- Redis must be reachable from container EC2s (requires VPC routing to Redis, or Redis exposed externally)
- Sidecar code must be rewritten (Go WebSocket server → Redis Pub/Sub)
- Binary PTY data in Redis pub/sub requires careful encoding (base64 or binary-safe channels)
- Redis becomes a SPOF for all active terminal sessions

**Complexity:** Medium (but higher operational risk for real-time terminal use)

---

### Plan E: AWS Systems Manager (SSM) Session Manager

**Concept:** Use AWS SSM Session Manager to create shell sessions. SSM Agent (pre-installed on Amazon Linux, available for Windows) creates outbound connections to AWS SSM endpoints. BFF uses the AWS SDK to start sessions — no inbound ports needed.

**Connection flow:**
```
.NET Backend → POST /sessions { ec2InstanceId: "i-0abc123", containerId: "abc123" }
Browser → BFF WS (/proxy/:sessionId)
  → BFF: aws ssm start-session --target i-0abc123 --document-name AWS-StartInteractiveCommand
  → SSM tunnel: BFF ←→ AWS SSM endpoint ←→ SSM Agent (EC2) ←→ docker exec <container>
  → BFF relays SSM I/O to Browser WS
```

**Changes needed:**
- SSM Agent configured on all EC2 instances (already present on Amazon Linux)
- IAM role for BFF to call SSM APIs
- `sessions.js`: store `{ ec2InstanceId, containerId }`
- `ptyProxy.js`: replace WS connection with SSM session via AWS SDK
- SSM document to exec into a specific container by ID

**Pros:**
- No custom agent code — SSM Agent is AWS-managed
- No inbound EC2 ports
- Built-in IAM access control and CloudTrail audit logging
- Handles both Windows and Linux EC2 (SSM supports both)

**Cons:**
- AWS vendor lock-in
- SSM session latency (traffic routes through AWS regional endpoints)
- SSM plugin (`session-manager-plugin`) must be installed on BFF server
- SSM document for container exec is non-trivial to configure
- Each SSM session uses AWS API calls (cost at scale)

**Complexity:** Low-Medium (mostly configuration, not custom code)

---

## Recommendation: Plan A (EC2 Relay Agent with Reverse WebSocket Tunnel)

**Why Plan A:**

1. **Preserves the existing sidecar** — the container's `os-process-manager-service` WebSocket remains unchanged. The agent just acts as a local relay to it.
2. **No inbound ports** — agents connect outbound to BFF, identical security model to BFF's existing outbound calls.
3. **Cross-platform** — an agent can run on both Windows and Linux EC2 with the same codebase (Node.js or Go).
4. **Low latency** — direct socket relay with no broker intermediary.
5. **Minimal BFF changes** — `ptyProxy.js` only needs an agent routing layer; the protocol and message format stay the same.
6. **Session registration** only changes by adding `agentId` alongside existing fields.

---

## High-Level Implementation Steps (Plan A)

1. **EC2 Relay Agent service:**
   - Small process (Node.js ws + Docker SDK, or Go)
   - On startup: connects to `ws://bff:3000/agents` with `{ agentId, os, hostname }`
   - Handles incoming proxy commands from BFF: `{ action: "open", sessionId, containerHost, containerPort }`
   - Opens WS to container sidecar (`ws://172.x.x.x:8080/pty`) locally
   - Multiplexes multiple sessions over the single reverse tunnel using a session envelope: `{ sessionId, data }`

2. **BFF agent registry** (new module in BFF):
   - WebSocket endpoint `/agents` where agents connect and register
   - Maintains Map: `agentId → agentWebSocket`
   - Handles agent reconnection (re-register on disconnect)

3. **BFF `ptyProxy.js` changes:**
   - Check if session has `agentId` (remote) or `host` (local, backward compatible)
   - If remote: route through agent tunnel instead of direct WS
   - Framing: add `sessionId` envelope for multiplexing

4. **BFF `sessions.js` changes:**
   - Session record: `{ agentId?, host?, port, label }` (agentId for remote, host for local)

5. **.NET backend changes:**
   - When creating a container on a remote EC2: include `agentId` in POST /sessions body
   - Agent ID maps to EC2 instance (registered when agent connects)

---

## EC2 Relay Agent — Detailed Responsibilities

The relay agent is a long-running service process installed on each EC2 instance. It is the single point of contact between the BFF and every container running on that EC2. Its responsibilities fall into four categories:

### 1. Registration & Heartbeat

- On startup the agent reads a local config file that contains its `agentId` (e.g. `ec2-linux-us-east-1a`), `os` type (`linux` or `windows`), and the BFF WebSocket URL
- It opens a persistent outbound WebSocket to the BFF `/agents` endpoint and sends a registration message: `{ agentId, os, hostname, ec2InstanceId }`
- It sends a heartbeat ping to the BFF every N seconds so the BFF knows the agent is still alive
- If the connection drops the agent retries with exponential backoff until it reconnects, then re-registers

### 2. Session Lifecycle Management

- The BFF sends commands over the reverse tunnel to open or close a PTY proxy session
- **Open command:** `{ action: "open", sessionId, containerHost, containerPort }` — agent connects to `ws://containerHost:containerPort/pty` on the local Docker network, then acknowledges to BFF
- **Close command:** `{ action: "close", sessionId }` — agent tears down that specific container WebSocket and cleans up its local state
- The agent maintains a local Map of `sessionId → containerWebSocket` so it can manage multiple concurrent sessions independently
- If the container sidecar closes its WebSocket (container died, app exited), the agent notifies the BFF which notifies the browser

### 3. PTY Data Relay

- All frames between BFF and a container sidecar are wrapped in an envelope: `{ sessionId, data, isBinary }`
- The agent strips the envelope on inbound frames from BFF, forwards raw data to the correct container sidecar WebSocket
- The agent wraps outbound frames from the container sidecar, adds the `sessionId`, and sends through the reverse tunnel to BFF
- Binary frames (raw PTY output/keystrokes) and text frames (JSON resize messages) are both handled — the agent must preserve the `isBinary` flag in the envelope

### 4. Docker Network Topology Awareness (Subnet/Project Isolation)

> This is covered in detail in the next section.

- The agent knows which Docker networks exist on its EC2 (it queries the Docker daemon)
- It maps each network to a `projectId` and `userId`
- It enforces that a session can only connect to a container that belongs to the project and user associated with that session (validated against metadata provided by the BFF)
- It refuses proxy requests where the target container is not in the expected network

---

## Multi-Container Project Subnets & User Isolation

### The Problem

Today each project is a single container. In the future a project may be composed of multiple containers — for example:

```
Project "my-app" (userId: user-42, projectId: proj-99)
  ├── app container      (Node.js application)
  ├── nginx container    (reverse proxy / SSL termination)
  └── database container (Postgres or MySQL)
```

All three containers belong to the same project and user. They need to communicate with each other, but must be completely isolated from containers belonging to other users or other projects.

Additionally the user should only be able to open a PTY terminal into their **own** containers — not into containers owned by other users, even if those containers happen to be on the same EC2 instance.

### Docker Network as the Project Subnet

Each project gets its own **dedicated Docker network** (bridge or overlay) when it is provisioned. This network acts as the project's private subnet.

```
Docker network: "project-proj-99"  (172.20.0.0/24)
  ├── app-container      172.20.0.2
  ├── nginx-container    172.20.0.3
  └── db-container       172.20.0.4

Docker network: "project-proj-55"  (172.21.0.0/24)  ← different user, completely isolated
  ├── app-container      172.21.0.2
  └── db-container       172.21.0.3
```

- Containers in `project-proj-99` can reach each other freely by container name (Docker DNS)
- Containers in `project-proj-99` **cannot** reach containers in `project-proj-55` — different subnets, no cross-network routing
- No container has any published ports to the EC2 host

### What the Agent Knows

When the .NET backend provisions a project on an EC2, it tells the agent (via the BFF or a direct provisioning call):

```
{
  projectId: "proj-99",
  userId: "user-42",
  networkName: "project-proj-99",
  containers: [
    { containerId: "abc1", role: "app" },
    { containerId: "abc2", role: "nginx" },
    { containerId: "abc3", role: "db" }
  ]
}
```

The agent stores this as a **project registry**: `projectId → { userId, networkName, containerIds[] }`

### How the Agent Enforces Isolation

When the BFF sends a proxy open command to the agent, it includes not just the target container info but also the ownership context:

```
{
  action: "open",
  sessionId: "sess-xyz",
  containerId: "abc1",
  containerPort: 8080,
  projectId: "proj-99",   ← BFF includes these from the validated session
  userId: "user-42"
}
```

Before connecting to the container sidecar, the agent performs these checks in order:

1. **Container exists on this EC2** — look up `containerId` in the local Docker daemon; reject if not found
2. **Container belongs to the stated project** — check the agent's project registry; the container must be listed under `projectId`
3. **Project belongs to the stated user** — check the agent's project registry; `userId` must match
4. **Container is on the expected Docker network** — query Docker to confirm the container is actually attached to `networkName`; reject if it is on a different network (could indicate a misconfiguration or spoofed request)

If any check fails the agent sends `{ action: "denied", sessionId, reason: "..." }` back to the BFF and does not open the container connection. The BFF closes the browser WebSocket with an appropriate error code.

### Network Topology Diagram (Multi-Container Project)

```
EC2 Instance (Linux)
│
├── Docker network: "project-proj-99"  (172.20.0.0/24)  [user-42]
│     ├── app-container      172.20.0.2  ← sidecar on :8080
│     ├── nginx-container    172.20.0.3  ← no sidecar (config-only)
│     └── db-container       172.20.0.4  ← no sidecar (data-only)
│
├── Docker network: "project-proj-55"  (172.21.0.0/24)  [user-77]
│     ├── app-container      172.21.0.2  ← sidecar on :8080
│     └── db-container       172.21.0.3
│
└── Relay Agent (host process, not in any container)
      ├── Has read access to Docker socket
      ├── Can reach 172.20.x.x and 172.21.x.x (host → bridge)
      ├── Maintains project registry (projectId → { userId, networkName, containerIds })
      └── Reverse WS tunnel → BFF
```

### What Happens When the BFF is Compromised or Sends a Bad Request

The agent is the **last line of defence** for cross-user container access. Even if the BFF sends an incorrect `userId` or `projectId` (due to a bug or compromise), the agent independently verifies by comparing against its local project registry and the live Docker network state. The agent never trusts the BFF blindly for access decisions — it always cross-checks against what it knows locally.

### Future: Containers on Different EC2s Within the Same Project

If a project spans multiple EC2 instances (e.g. the app container on a Linux EC2, a Windows service container on a Windows EC2), each EC2 will have a relay agent with a partial view of the project's containers. The BFF knows the full topology (which container is on which agent) because the .NET backend tracks this at provisioning time.

The project registry on each agent only needs to know about the containers on **its own** EC2. Cross-EC2 inter-container communication for the same project is a separate concern (VPC private networking, or a future overlay network), but the isolation model remains the same — each agent only permits access to containers it locally owns for the given project/user.

---

## Verification

- Start BFF and relay agent on separate machines; verify agent appears in BFF's registry
- Create a container on the agent's EC2; register session with `agentId`
- Browser connects to BFF WebSocket `/proxy/:sessionId`; verify terminal input/output flows correctly
- Test with both Linux (`sh`) and Windows (`cmd.exe`) containers
- Disconnect agent mid-session; verify BFF sends appropriate close code to browser
- Reconnect agent; verify new sessions work
- Attempt to open a PTY session with a mismatched `userId`; verify the agent rejects it and BFF closes the browser socket
- Create two projects on the same EC2; verify containers from project A cannot be accessed via a session registered under project B
