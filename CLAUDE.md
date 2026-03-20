# container-management — Project Overview

## What This Project Does

Accepts user-uploaded JavaScript projects, scans them for viruses/vulnerabilities, and automatically builds + runs them as Docker containers. Each container also runs an internal sidecar process (`os-process-manager-service`) that exposes process diagnostics over gRPC.

The companion project `compute-instance-ui` provides a browser-based XFCE desktop viewer for these containers (VNC-over-WebSocket).

---

## Solution Structure

```
container-management-solution.sln
├── ContainerManagerBackend/        # ASP.NET Core Web API (entry point for uploads)
├── AsyncJobWorkers/                # Hangfire worker host (background job processor)
├── Engines/
│   ├── FileStorageEngines/         # MinIO upload, virus scan, metadata, container build
│   └── DataBaseStorageEngines/     # PostgreSQL via EF Core (project metadata)
├── Domain/                         # Abstract base entities (OSProcess, etc.)
├── OSOrchestrator/                 # Docker container lifecycle (create/start)
├── OperatingSystemLake/            # VM/EC2 discovery layer (DockerMachine, VirtualBox, AWS)
├── OperatingSystemHelpers/         # ProcessCommunicator — wraps CLI subprocesses
├── os-process-manager-infrastructure/  # gRPC sidecar server (runs inside containers)
├── os-process-manager-service/     # Sidecar HTTP service (health check)
├── DeploymentManager/              # Builds native OS binaries for the sidecar
└── TestProjects/SimpleNodeApp/     # Sample Node.js project for manual testing
```

---

## Infrastructure (docker-compose.yml)

All services run on `192.168.99.101` (the Docker Machine VM IP):

| Service   | Port  | Purpose                        |
|-----------|-------|--------------------------------|
| Redis     | 6379  | Hangfire job queue             |
| ClamAV    | 3310  | Virus scanner                  |
| MinIO-1   | 9002  | Object storage (primary)       |
| MinIO-2   | 9003  | Object storage (replica/elect) |
| PostgreSQL | 5432 | Project/metadata database      |
| pgAdmin   | 5050  | DB admin UI                    |

---

## End-to-End Data Flow

```
1. POST /UploadJS  (ContainerManagerBackend)
        ↓
2. JSProjectUploadStrategy
   - Extract ZIP → temp dir
   - Validate package.json exists
   - Filter files (exclude node_modules, .git, dist, etc.)
   - Repack as .tar.gz → upload to MinIO bucket "Projects"
        ↓
3. Hangfire job enqueued → JSProjectProcessingJobEnque.DoWork()
        ↓
4. Download artifact from MinIO
        ↓
5. VirusScanAndExtractMetaData()
   - ClamAV stream scan
   - Extract tar to temp dir
   - Run: npm audit, npm list, npm config, npm pack (via WindowsProcessCommunicator)
   - Parse JSON output → JSProjectMetadata
   - RiskAssessment (score 0-100: APPROVE / WARN / QUARANTINE / REJECT)
        ↓
   VIRUS/REJECT → QuarantineExecutableFile() → move to "hazard" bucket, delete from "Projects"
   CLEAN/APPROVE ↓
        ↓
6. ContainerBuildService.BuildAndStartProjectContainer()
   - ProjectContainerRecipeFactory.GetRecipe(JS) → NodeProjectContainerRecipe
   - DockerfileTemplateRenderer.Render(recipe) → Dockerfile string
   - ContainerContextAssembler.BuildContextTar():
       • Dockerfile
       • entrypoint.sh (supervisor script)
       • user-project/ (repacked from MinIO artifact)
       • sidecar/ (pre-built os-process-manager-service binary)
   - POST tar → Docker daemon → build image "project-{projectId}:latest"
   - Create + start container
        ↓
7. Container runs:
   entrypoint.sh → starts "node index.js" → writes PID to /tmp/user-process.pid
                 → starts os-process-manager-service (sidecar)
   Sidecar reads PID → monitors /proc/{pid} → exposes gRPC :5001
```

---

## Key Design Patterns

### Recipe Pattern (`IProjectContainerRecipe`)
Each supported language has a recipe class with 3 fields: `BaseImage`, `BuildStep`, `StartCommand`. Adding a new language = adding one new class. The Dockerfile template, entrypoint, and sidecar are unchanged.

Currently implemented: `NodeProjectContainerRecipe` (node:20-slim).

### OSLake → Docker Client Indirection
`ContainerBuildService` receives a `IDockerClientFactory` rather than a hardcoded URI. The factory resolves the correct Docker daemon by going through: `OSLakeConnector` → `BaseOSLake` (has IP) → `DockerClientFromOSLake`.

Connectors available: `DockerMachineOSLakeConnector` (dev), `VirtualBoxOSLakeConnector` (dev), `AwsOSLakeConnector` (stub, production).

### Storage Engine Election
`ProjectStorageEngineBackgroundService` polls MinIO metrics (Prometheus format) every 250,000s and elects the healthiest MinIO instance as the active `StorageEngine` using a weighted score (free bytes × 4, health × 5, pending requests × 3, rejections × 2).

---

## API Endpoints (ContainerManagerBackend)

| Method | Route             | Description                                       |
|--------|-------------------|---------------------------------------------------|
| GET    | /CreateOS         | Manually create a Docker container (legacy)       |
| POST   | /UploadJS         | Upload a `.zip` JS project, triggers pipeline     |
| GET    | /TestNodeUpload   | Dev helper: upload `TestProjects/SimpleNodeApp`   |

---

## Database Schema (PostgreSQL / EF Core)

Tables (see `Engines/DataBaseStorageEngines/Entities/`):
- `ProjectRecord` — project name, type, bucket, storage URL, virus scan result
- `JsMetadataRecord` — vulnerability counts, dependency count, package size, file count
- `JsDependencyRecord` — individual dependency records
- `JsVulnerabilityRecord` — individual vulnerability records
- `RiskAssessmentRecord` — risk score, risk level, action taken

Migration: `Engines/Migrations/20260314190834_InitialCreate.cs`

---

## Ideation Documents

- `container_sidecar_ideation.md` — sidecar/supervisor architecture design, recipe pattern, gRPC wire-up
- `oslake_container_build_ideation.md` — how to wire OSLake → DockerClient → ContainerBuildService
- `node-test-api-endpoint.md` — notes on the `/TestNodeUpload` endpoint

---

## Known Issues / In-Progress

- `AsyncJobWorkers/Program.cs` has a hardcoded dev path for the sidecar publish dir (`c:\Users\kaavin\...`) — needs to be config-driven
- `NodeMetaDataExtraction` uses `WindowsProcessCommunicator` even inside the Docker container; needs `LinuxProcessCommunicator` in the worker
- `ProjectStorageManager.FetchCurrentRunningStorageEngines()` production path is commented out; dev path hardcodes MinIO IPs
- MinIO credentials are hardcoded (`minioadmin`/`minioadmin`) — should come from config/secrets
