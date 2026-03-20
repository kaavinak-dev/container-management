# AsyncJobWorkers — Hangfire Background Job Host

This is a `Host.CreateApplicationBuilder` (not ASP.NET) console process. It listens to the Redis-backed Hangfire queue and executes background jobs independently of the API.

## What It Does

- Connects to Redis (Hangfire storage) and starts a Hangfire server with 10 worker threads
- Dequeues and processes `JSProjectProcessingJobEnque` jobs (virus scan → npm audit → container build)
- Does **not** expose HTTP endpoints

## DI Registrations (Program.cs)

| Service | Details |
|---|---|
| Hangfire | Redis at `192.168.99.101:6379`, prefix `hangfire:` |
| `ClamAVClient` | Singleton — connects to ClamAV at `192.168.99.101:3310` |
| Hangfire server | 10 workers, named `AsyncJobWorker` |
| `OSLakeConnector` × 2 | `VirtualBoxOSLakeConnector` + `DockerMachineOSLakeConnector` |
| `IDockerClientFactory` → `DockerClientFactory` | Singleton — resolves DockerClient via OSLake |
| `ContainerBuildService` | Singleton — hardcoded sidecar path (see known issue below) |
| `JSProjectProcessingJobEnque` | Scoped — the actual job class Hangfire invokes |

## Job: `JSProjectProcessingJobEnque`

Hangfire calls `DoWork(ProjectContainer)`:

1. Reconstruct `MinioProjectStorageEngine` from `projectContainer.getProjectStoredServerUrl()`
2. Download artifact from MinIO
3. Call `ProcessProject(stream, container)`:
   - `VirusScanAndExtractMetaData()` — ClamAV scan, then extract tar, run npm commands
   - If not CLEAN → `QuarantineExecutableFile()` → move to "hazard" bucket
   - If CLEAN → save metadata to Postgres (if `metadataStorageEngine != null`) → `ContainerBuildService.BuildAndStartProjectContainer()`

## Known Issues

- `sidecarPublishDir` is hardcoded to `c:\Users\kaavin\programming\...` — must be changed to a config value or relative path before deployment
- `NodeMetaDataExtraction()` uses `WindowsProcessCommunicator` — will fail on Linux workers; needs `LinuxProcessCommunicator`
- `metadataStorageEngine` is `null` in the current DI registration (no `IMetadataStorageEngine` registered) → metadata is never saved to Postgres from the worker
- `containerBuildService` IS registered and WILL attempt Docker builds if `virusScanResult == CLEAN`
