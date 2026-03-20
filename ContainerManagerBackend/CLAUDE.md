# ContainerManagerBackend — ASP.NET Core Web API

The public-facing API. Receives project uploads, triggers background jobs, and (legacy) creates Docker containers on demand.

## Startup / DI (Program.cs)

Key singletons registered:
- `OSLakeConnector` × 2: `VirtualBoxOSLakeConnector` and `DockerMachineOSLakeConnector` (both using `WindowsProcessCommunicator`)
- `ProjectStorageManager` — hardcoded to two MinIO instances at `192.168.99.101:9002` and `:9003`
- `ProjectStorageEngineBackgroundService` — hosted service that elects the best MinIO on startup
- Hangfire → Redis at `192.168.99.101:6379`
- EF Core `ProjectDbContext` → PostgreSQL at `192.168.99.101:5432`, DB `container_management`

Swagger is available in Development mode.

## Endpoints (OSManagerApiController)

### `GET /CreateOS`
Uses `RequestBodyParser` to extract an `OSOrchestrator` and `OSLakeConnector` from request headers/query, then calls `orchestrator.CreateOS()`. Returns JSON success/error message. This is the older manual-trigger path.

### `POST /UploadJS`
- Accepts multipart form: `List<IFormFile> files` — expects exactly one `.zip`
- Delegates to `JSProjectUploadStrategy.UploadProject(zipStream, "Projects")`
- Enqueues `JSProjectProcessingJobEnque` via `ProjectProcessingJobEnqueHelper.EnqueJob()`
- Returns `{ "status": "ok", "message": "file uploaded" }`

### `GET /TestNodeUpload`
Development convenience endpoint. Reads `TestProjects/SimpleNodeApp/` from disk (relative to the binary), packs it as `.tar.gz` in memory, runs through the same upload + enqueue pipeline as `/UploadJS`. Useful for testing the full pipeline without a real zip upload.

## Helpers

| Class | Role |
|---|---|
| `RequestBodyParser` | Scoped service; extracts OSOrchestrator and OSLakeConnector from HTTP request context |
| `ResponseFormatter` | Serializes `{ status, message }` response objects |
| `FileHandler` | Utility for file I/O operations |

## Important Notes

- The `/CreateOS` endpoint uses `RequestBodyParser` which still references the older OSOrchestrator/OSLake wiring — it is not part of the new upload pipeline.
- All addresses (MinIO, Redis, Postgres) are hardcoded in `Program.cs` — they should be moved to `appsettings.json` / environment variables.
- No authentication/authorization is configured (no middleware).
