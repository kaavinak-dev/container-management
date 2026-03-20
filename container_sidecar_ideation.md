# Container + Sidecar Architecture — Ideation

## The Core Mental Model

The goal is: *for every approved user project, produce a Docker image that runs the user's app process **and** the `os-process-manager-service` sidecar side by side, where the sidecar can monitor the user process and expose that data over gRPC/HTTP.*

The pattern to use is the **init-process / supervisor model** inside a single container. Rather than two containers (which would require a pod/sidecar k8s pattern), both processes run inside one image under a lightweight process supervisor. This keeps the architecture simple (no k8s needed) and lets the sidecar reach the user process via `/proc/{pid}` directly.

```
┌─────────────────────────────────────────────────────────┐
│  Docker Container                                        │
│                                                          │
│  [supervisor / entrypoint.sh]                            │
│       ├── node index.js          → USER PROCESS (PID N)  │
│       └── os-process-manager     → SIDECAR (PID M)       │
│               │                                          │
│               └── reads /proc/N/status                   │
│               └── exposes gRPC :5001 / HTTP :5000        │
└─────────────────────────────────────────────────────────┘
```

---

## The Extensibility Problem

The *only* thing that changes between a Node project and a Go/Rust/.NET project is:

1. The **base Docker image** (node:20, golang:1.22, rust:1.80, mcr.microsoft.com/dotnet/runtime:8.0)
2. The **build step** inside the image (npm install, go build, cargo build, dotnet publish)
3. The **run command** (node index.js, ./app, ./app, dotnet App.dll)
4. Any **pre-flight checks** specific to that runtime (e.g. `npm audit` for Node)

Everything else — sidecar injection, supervisor setup, PID handshake, gRPC exposure — is **identical across all project types**. This screams for a template/strategy pattern.

---

## Proposed Abstraction: `ProjectContainerRecipe`

Introduce a new abstraction in the codebase, `ProjectContainerRecipe`, that encodes the project-type-specific parts of a Dockerfile:

```csharp
// Domain layer — project-type agnostic interface
public interface IProjectContainerRecipe
{
    string BaseImage { get; }
    string BuildStep { get; }   // e.g. "RUN npm install"
    string StartCommand { get; } // e.g. "node index.js"
    ProjectTypes ProjectType { get; }
}

// Concrete implementations, one per project type
public class NodeProjectContainerRecipe : IProjectContainerRecipe
{
    public string BaseImage => "node:20-slim";
    public string BuildStep => "RUN npm install --production";
    public string StartCommand => "node index.js";
    public ProjectTypes ProjectType => ProjectTypes.JS;
}

public class DotNetProjectContainerRecipe : IProjectContainerRecipe
{
    public string BaseImage => "mcr.microsoft.com/dotnet/aspnet:8.0";
    public string BuildStep => ""; // pre-built artifact, nothing to build
    public string StartCommand => "dotnet App.dll";
    public ProjectTypes ProjectType => ProjectTypes.DotNet;
}

// Go, Rust etc. follow the exact same pattern
```

Adding a new language = adding one new class. Nothing else in the pipeline changes.

---

## Dockerfile Template Strategy

Instead of a static hardcoded Dockerfile, maintain a **parameterised template** that accepts a `IProjectContainerRecipe` and renders the final Dockerfile at job time:

```
# Template (stored in DeploymentComponents or embedded resource)

FROM {recipe.BaseImage}

# === User project ===
WORKDIR /project
COPY ./user-project .
{recipe.BuildStep}

# === Sidecar ===
WORKDIR /sidecar
COPY ./sidecar .

# === Supervisor / entrypoint ===
COPY entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh

EXPOSE 5000
EXPOSE 5001

ENTRYPOINT ["/entrypoint.sh", "{recipe.StartCommand}"]
```

The `entrypoint.sh` is the key runtime glue:

```bash
#!/bin/sh
# Start the user process, capture its PID
$1 &
USER_PID=$!

# Write PID to a known location so the sidecar can read it
echo $USER_PID > /tmp/user-process.pid

# Start the sidecar
/sidecar/os-process-manager-service

# If sidecar exits, kill the user process too
kill $USER_PID
```

This `entrypoint.sh` is **the same for every project type** — only the `$1` argument (the start command) differs.

---

## PID Handshake: How the Sidecar Finds the Node Process

`entrypoint.sh` writes the PID to `/tmp/user-process.pid`. The sidecar reads this on startup:

```csharp
// In os-process-manager-service startup (or in ApplyService.Apply())
var pidFile = "/tmp/user-process.pid";
var pid = int.Parse(File.ReadAllText(pidFile).Trim());
// Now pass this pid to OSProcessDiagnosticManager.GetProcessDiagnosticsByProcessId(pid)
```

This is simple, reliable, and requires zero coordination between the sidecar and the user process. The `/proc` filesystem approach already used in [LinuxProcessDiagnosticsManager](file:///c:/Users/kaavin/programming/container-management/os-process-manager-infrastructure/OSInfrastructure/LinuxInfrastructure/LinuxProcessDiagnosticsManager.cs#33-77) works perfectly here.

---

## gRPC Wire-Up: Exposing Diagnostics

The existing [process_service.proto](file:///c:/Users/kaavin/programming/container-management/os-process-manager-infrastructure/Grpc/Protos/process_service.proto) needs a service block and an implementation:

```protobuf
// Gap 2 from the previous analysis — add this to process_service.proto
service ProcessService {
    rpc GetProcessDiagnostics (GetDiagnosticsRequest) returns (GetDiagnosticsResponse);
    rpc StreamProcessDiagnostics (GetDiagnosticsRequest) returns (stream DiagnosticsEvent);
}

message GetDiagnosticsRequest { string processId = 1; }
message GetDiagnosticsResponse {
    repeated ProcessDiagnosticEntry entries = 1;
}
message DiagnosticsEvent {
    ProcessDiagnosticEntry entry = 1;
    string timestamp = 2;
}
message ProcessDiagnosticEntry {
    string key = 1;
    string value = 2;
    string category = 3; // "system" | "process"
}
```

The gRPC service impl reads the PID from `/tmp/user-process.pid` and calls `OSProcessDiagnosticManager.GetProcessDiagnosticsByProcessId(pid)` — the existing Linux/Windows implementations handle the actual data collection. Then register in [Program.cs](file:///c:/Users/kaavin/programming/container-management/DeploymentManager/Program.cs):

```csharp
endpoints.MapGrpcService<ProcessServiceImpl>(); // alongside existing ApplyService
```

---

## The Missing Post-Processing Pipeline Trigger

After [VirusScanAndExtractMetaData](file:///c:/Users/kaavin/programming/container-management/Engines/FileStorageEngines/Implementations/ProjectProcessingJobEnque.cs#130-170) approves a project, [ProcessProject](file:///c:/Users/kaavin/programming/container-management/Engines/FileStorageEngines/Implementations/ProjectProcessingJobEnque.cs#450-482) needs to:

1. Determine the recipe: `var recipe = ProjectContainerRecipeFactory.GetRecipe(projectContainer.getProjectType())`
2. Render the Dockerfile from the template using the recipe
3. Call [DockerImageBuilder](file:///c:/Users/kaavin/programming/container-management/DeploymentManager/Implementations/DockerImageBuilder.cs#12-97) (adapted to accept per-project context) to assemble the TAR context:
   - user's project files (from MinIO artifact)
   - pre-built sidecar binary
   - rendered Dockerfile
   - `entrypoint.sh`
4. Build the image via `DockerClient.Images.BuildImageFromDockerfileAsync()`
5. Create and start the container via `DockerOSOrchestrator.CreateOS(imageName)`

The `DockerOSOrchestrator.CreateOS()` currently hardcodes `Image="osprocessmanager"` — it needs to accept a project-specific image name.

---

## Full Data Flow (End to End)

```
User uploads .tar.gz
        │
        ▼
[ContainerManagerBackend]
  Enqueues Hangfire job → JSProjectProcessingJobEnque
        │
        ▼
[DoWork]
  Downloads artifact from MinIO
        │
        ▼
[ProcessProject]
  VirusScanAndExtractMetaData()
  → ClamAV scan of stream
  → Extract to temp dir
  → npm audit / npm list / npm pack (metadata)
  → RiskAssessment
        │
   VIRUS/REJECT ──────────────────────────► QuarantineExecutableFile()
        │
   CLEAN/APPROVE
        │
        ▼
[ContainerBuildJob]  ← NEW Hangfire job
  recipe = ProjectContainerRecipeFactory.GetRecipe(JS)
  dockerfile = DockerfileTemplateRenderer.Render(recipe)
  tar = ContainerContextAssembler.Build(projectArtifact, sidecarBinary, dockerfile, entrypoint.sh)
        │
        ▼
[DockerImageBuilder]
  Sends tar to Docker daemon → builds image "project-{projectId}:latest"
        │
        ▼
[DockerOSOrchestrator]
  CreateOS("project-{projectId}:latest")
  → Container starts
  → entrypoint.sh runs
  → node process starts, PID written to /tmp/user-process.pid
  → sidecar starts, reads PID
  → gRPC :5001 live
        │
        ▼
[External caller / ContainerManagerBackend]
  Calls gRPC ProcessService.GetProcessDiagnostics(processId)
  → LinuxProcessDiagnosticsManager reads /proc/{pid}/status
  → Returns diagnostics
```

---

## What New Code Is Needed (Summary)

| Component | What to add |
|---|---|
| **Domain** | `IProjectContainerRecipe` interface + concrete impls per language |
| **Domain** | `ProjectContainerRecipeFactory` — maps `ProjectTypes` → recipe |
| **DeploymentManager** | `DockerfileTemplateRenderer` — renders template string from recipe |
| **DeploymentManager** | `ContainerContextAssembler` — builds the TAR build context (project + sidecar + Dockerfile + entrypoint.sh) |
| **DeploymentManager** | Update [DockerImageBuilder](file:///c:/Users/kaavin/programming/container-management/DeploymentManager/Implementations/DockerImageBuilder.cs#12-97) to accept recipe + project-specific context |
| **DeploymentManager** | Update `DockerOSOrchestrator.CreateOS()` to accept `imageName` param |
| **os-process-manager-service** | `entrypoint.sh` (shared, runtime-agnostic) |
| **os-process-manager-infrastructure** | Add `service ProcessService` to proto + `ProcessServiceImpl` |
| **os-process-manager-service** | Register `ProcessServiceImpl` in [Program.cs](file:///c:/Users/kaavin/programming/container-management/DeploymentManager/Program.cs) |
| **Engines / ProjectProcessingJobEnque** | Add `ContainerBuildJob` step after `CLEAN` result in [ProcessProject](file:///c:/Users/kaavin/programming/container-management/Engines/FileStorageEngines/Implementations/ProjectProcessingJobEnque.cs#450-482) |

---

## Extensibility Scorecard

| Future project type | What you'd need to add |
|---|---|
| Go | `GoProjectContainerRecipe` (5 lines) |
| Rust | `RustProjectContainerRecipe` + a build step since cargo needs compile |
| .NET | `DotNetProjectContainerRecipe` |
| Python | `PythonProjectContainerRecipe` |

The supervisor entrypoint, sidecar, gRPC contract, Dockerfile template structure, and pipeline trigger are **zero-change** for any new language. Only the recipe class changes.
