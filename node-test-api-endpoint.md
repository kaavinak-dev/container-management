# Implementation Plan: E2E Test Endpoint for Node.js Project Upload

## Goal
Create a testing mechanism to verify the complete end-to-end (E2E) flow: from a simulated Node.js project upload, through the Hangfire processing job, virus scanning (simulated/bypassed if needed), metadata extraction, and finally into the new Container Build pipeline with the sidecar.

## Proposed Changes

### 1. `TestProjects` Directory
We will create a dummy Node.js project on disk that represents a user upload.
- **[NEW]** `TestProjects/SimpleNodeApp/package.json` — A basic package file with a couple of safe dependencies (e.g., `express`) to ensure `npm audit`/`npm list` work correctly during the metadata extraction phase.
- **[NEW]** `TestProjects/SimpleNodeApp/index.js` — A minimal Express server that listens on port `3000` and responds to a health check, allowing us to verify the user container is actually running.

### 2. Update [OSManagerApiController.cs](file:///c:/Users/kaavin/programming/container-management/ContainerManagerBackend/Controllers/OSManagerApiController.cs)
- **[MODIFY]** [ContainerManagerBackend/Controllers/OSManagerApiController.cs](file:///c:/Users/kaavin/programming/container-management/ContainerManagerBackend/Controllers/OSManagerApiController.cs)
  - Add a new `[HttpGet("/TestNodeUpload")]` endpoint.
  - This endpoint will *programmatically* create a `.tar.gz` (or `.zip` as currently expected by [UploadJS](file:///c:/Users/kaavin/programming/container-management/ContainerManagerBackend/Controllers/OSManagerApiController.cs#45-68)) of the `TestProjects/SimpleNodeApp` directory in memory.
  - It will then pass this stream into the existing `JSProjectUploadStrategy.UploadProject` method, effectively simulating the `IFormFile` upload from a real user.
  - Finally, it will enqueue the `ProjectProcessingJobEnqueHelper.EnqueJob`, triggering the background pipeline.

### 3. Pipeline Adjustments (If Necessary)
- Ensure the `JSProjectUploadStrategy` and [ProjectProcessingJobEnque](file:///c:/Users/kaavin/programming/container-management/Engines/FileStorageEngines/Implementations/ProjectProcessingJobEnque.cs#58-69) can handle the locally generated archive stream just as they would an HTTP upload stream.
- As part of the recent architecture ideation, ensure the Hangfire job eventually routes this test project into the [ContainerBuildService](file:///c:/Users/kaavin/programming/container-management/Engines/FileStorageEngines/ContainerBuild/ContainerBuildService.cs#14-19).

## Verification Plan

### Automated/Manual Verification
1. Start the backend server (`dotnet run --project ContainerManagerBackend`).
2. Trigger the test endpoint via `curl http://localhost:5000/TestNodeUpload` or Swagger UI.
3. Observe the Hangfire logs to ensure the `JSProjectProcessingJobEnque.DoWork` executes successfully.
4. Verify that the project passes the simulated/local ClamAV scan and metadata extraction (`npm audit`).
5. **Final Output:** Verify that a Docker container named `project-{id}` is successfully built and started on the target OS Lake, and that both the Node app (port 3000) and the C# Sidecar (port 5000/5001) are running inside it.
