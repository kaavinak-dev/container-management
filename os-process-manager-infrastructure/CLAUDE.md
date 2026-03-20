# os-process-manager-infrastructure — Sidecar gRPC Service

This library runs **inside every user container** as the sidecar process. It provides process diagnostics (CPU, memory, etc.) over gRPC.

## How It Gets Into the Container

`ContainerContextAssembler` copies the entire contents of `DeploymentManager/DeploymentComponents/os-process-manager-binaries/linux/` into the `sidecar/` directory of the Docker build context. The Dockerfile copies this to `/sidecar/` and `entrypoint.sh` starts `/sidecar/os-process-manager-service` after the user app.

## gRPC Contract (`Grpc/Protos/process_service.proto`)

```protobuf
service ProcessService {
    rpc GetProcessDiagnostics(GetDiagnosticsRequest) returns (GetDiagnosticsResponse);
    rpc StreamProcessDiagnostics(GetDiagnosticsRequest) returns (stream DiagnosticsEvent);
}
```

- `GetProcessDiagnostics` — returns a snapshot of all diagnostic key-value pairs
- `StreamProcessDiagnostics` — streams diagnostic events every 1 second until cancelled

Each `ProcessDiagnosticEntry` has: `key`, `value`, `category` ("process" or "system").

## `ProcessServiceImpl`

Reads the user process PID from `/tmp/user-process.pid` (written by `entrypoint.sh`).

Delegates to `OSProcessDiagnosticManager.GetProcessDiagnosticsByProcessId(pid)` — this uses `LinuxProcessDiagnosticsManager` which reads `/proc/{pid}/status` and system-level `/proc` files.

The PID file approach is the handshake contract between `entrypoint.sh` and the sidecar — no direct IPC needed.

## `GrpcDTOHelpers`

Utility methods for converting domain diagnostics objects to gRPC protobuf types.

## Infrastructure (Linux vs Windows)

| Class | Platform | Implementation |
|---|---|---|
| `LinuxProcessManager` | Linux | Uses `/proc/{pid}/` directly |
| `LinuxProcessDiagnosticsManager` | Linux | Reads `/proc/{pid}/status`, `/proc/meminfo`, etc. |
| `WindowsProcessManager` | Windows | Uses Win32 P/Invoke (`NativeStructs`) |
| `WindowsProcessDiagnosticsManager` | Windows | Uses Windows API calls |

`LinuxInfrastructureServiceInjector` / `WindowsInfrastructureServiceInjector` register the correct implementations into DI based on the host OS.

## Ports

- `:5000` — HTTP (health check, exposed by `os-process-manager-service`)
- `:5001` — gRPC (`ProcessService`)

Both ports are `EXPOSE`d in the Dockerfile template.
