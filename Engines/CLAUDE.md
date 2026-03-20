# Engines — File & Database Storage

The `Engines` project (`Engines.csproj`) is the core business logic library. It has no ASP.NET dependency — it is consumed by both `ContainerManagerBackend` and `AsyncJobWorkers`.

---

## FileStorageEngines

### Abstractions (base classes, not interfaces)

| Class | Role |
|---|---|
| `ProjectStorageEngine` | Abstract MinIO wrapper: upload, download, delete, health check |
| `ProjectUploadStrategy` | Abstract upload pipeline (extract → validate → repack → store) |
| `ProjectContainer` | Abstract handle for a stored project (name, bucket, server URL) |
| `FileVirusScanner<TClient,TResult>` | Abstract virus scanner |
| `ProcessingJobEnque` | Abstract Hangfire job (DoWork, EnqueJob, ProcessProject, VirusScanAndExtractMetaData) |

### Implementations

| Class | Description |
|---|---|
| `MinioProjectStorageEngine` | Uploads via presigned PUT URL; downloads via GetObjectAsync; polls `/minio/v2/metrics/cluster` for health |
| `JSProjectUploadStrategy` | ZIP → extract → find package.json → filter files → `.tar.gz` → upload to MinIO |
| `JavaScriptProjectContainer` | Holds projectName (= projectId GUID), bucketName, serverUrl |
| `ClamAVVirusScanner` / `ClamAVClient` | Wraps `nClam.ClamClient`; returns CLEAN/VIRUS/QUARANTINE |
| `JavaScriptFileMetaDataExtractor` | PE binary parser (via PeNet) — currently unused in main flow |
| `JSProjectProcessingJobEnque` | Main Hangfire job: download → virus scan → npm audit → risk assessment → container build |

### Manager.cs — `ProjectStorageManager` + `ProjectStorageEngineBackgroundService`

`ProjectStorageManager` holds a static list of `MinioProjectStorageEngine` instances and a currently-elected engine (`StorageEngine`).

`ProjectStorageEngineBackgroundService` (IHostedService) runs on startup to generate MinIO clients and elect the best engine. It re-runs every 250,000 seconds (polling interval is effectively once at startup — the timer value appears to be a placeholder).

**Election algorithm (weighted score):**
- Free bytes usable × 4
- Health status × 5
- S3 requests pending × 3 (lower is better)
- S3 requests rejected × 2 (lower is better)

---

## ContainerBuild

### `IProjectContainerRecipe` / `NodeProjectContainerRecipe`

```csharp
public class NodeProjectContainerRecipe : IProjectContainerRecipe {
    BaseImage    = "node:20-slim"
    BuildStep    = "RUN npm install --production"
    StartCommand = "node index.js"
    ProjectType  = ProjectTypes.JS
}
```

`ProjectContainerRecipeFactory.GetRecipe(ProjectTypes.JS)` returns the recipe for a given project type.

### `DockerfileTemplateRenderer`

Takes an `IProjectContainerRecipe` and renders the Dockerfile string. The template is:
```
FROM {BaseImage}
WORKDIR /project
COPY ./user-project .
{BuildStep}
WORKDIR /sidecar
COPY ./sidecar .
COPY entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh
EXPOSE 5000
EXPOSE 5001
ENTRYPOINT ["/entrypoint.sh", "{StartCommand}"]
```

### `ContainerContextAssembler`

Produces a single TAR stream for `docker build`. The TAR contains:
- `Dockerfile` — rendered from template
- `entrypoint.sh` — supervisor script (starts user process, captures PID, starts sidecar)
- `user-project/` — user's files repacked from the MinIO `.tar.gz` artifact
- `sidecar/` — all files from `sidecarPublishDir` (pre-built `os-process-manager-service`)

The `entrypoint.sh` supervisor script:
```sh
#!/bin/sh
$@ &
USER_PID=$!
echo $USER_PID > /tmp/user-process.pid
/sidecar/os-process-manager-service
kill $USER_PID
```

### `ContainerBuildService`

- Takes `IDockerClientFactory` + `sidecarPublishDir`
- Calls `DockerfileTemplateRenderer`, `ContainerContextAssembler`
- POSTs the TAR to Docker daemon via `DockerClient.Images.BuildImageFromDockerfileAsync()`
- Creates and starts the container: name = `project-{projectId}`, image = `project-{projectId}:latest`
- Throws if container fails to start

### `DockerClientFactory` / `IDockerClientFactory`

Resolves a `DockerClient` for a given `(OSLakeTechTypes, OSLakeTypes)` pair by going through the OSLake connector chain.

---

## DataBaseStorageEngines

Uses EF Core with PostgreSQL (`Npgsql`).

### Entities

| Entity | Key Fields |
|---|---|
| `ProjectRecord` | Id (Guid PK), ProjectName, ProjectType, StorageUrl, BucketName, VirusScanResult |
| `JsMetadataRecord` | ProjectId (FK), VulnerabilityCount, CriticalVulns, HighVulns, DependencyCount, PackageSize, FileCount |
| `JsDependencyRecord` | ProjectId (FK), Name, Version, IsDevDependency |
| `JsVulnerabilityRecord` | ProjectId (FK), Name, Severity, CVSS, FixAvailable |
| `RiskAssessmentRecord` | ProjectId (FK), RiskLevel, RiskScore, Action, Issues |

### `PostgresMetadataStorageEngine`

Implements `IMetadataStorageEngine`. Key methods:
- `SaveProjectAsync(ProjectRecord)` → returns `Guid` project ID
- `SaveMetadataAsync<TDomain, TRecord>(projectId, metadata, mapper)` — generic, uses `IMetadataMapper`
- `SaveRiskAssessmentAsync(projectId, assessment)`
- `GetProjectAsync(projectId)` — includes RiskAssessments

### `JsMetadataMapper`

Maps between `JSProjectMetadata` (domain) and `JsMetadataRecord` (DB entity).

---

## Risk Assessment Logic (in `JSProjectProcessingJobEnque`)

Scores 0–100, action determined by score:

| Score | Action |
|---|---|
| ≥ 80 | REJECT → treated as VIRUS, quarantined |
| ≥ 50 | QUARANTINE |
| ≥ 30 | WARN_USER |
| < 30 | APPROVE |

Scoring factors:
- Critical vulns × 10 (from npm audit)
- High vulns × 5
- Total vuln count (capped at 20)
- Dep count > 500 → +15, > 200 → +10, > 100 → +5
- Package size > 100MB → +10
- Unpacked size > 500MB → +10
- Compression ratio > 100× → +15
- File count > 10,000 → +5
- Node version < 14 → +5
