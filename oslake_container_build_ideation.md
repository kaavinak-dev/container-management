# Resolving the OSLake ↔ ContainerBuildService Disconnect

## The Problem

[ContainerBuildService](file:///c:/Users/kaavin/programming/container-management/Engines/FileStorageEngines/ContainerBuild/ContainerBuildService.cs#7-60) creates its own [DockerClient](file:///c:/Users/kaavin/programming/container-management/OSOrchestrator/Implementations/DockerClientFromOSLake.cs#12-29) from a hardcoded `dockerDaemonUri` string:

```csharp
// ContainerBuildService.cs line 14
_dockerClient = new DockerClientConfiguration(new Uri(dockerDaemonUri)).CreateClient();
```

Meanwhile, the codebase already has a full abstraction layer for discovering Docker daemons dynamically:

```
OSLakeConnector (abstract)
  ├── VirtualBoxOSLakeConnector  → runs VBoxManage.exe to get VM IP
  ├── DockerMachineOSLakeConnector → runs docker-machine ip to get VM IP
  └── AwsOSLakeConnector         → (stub for EC2 instances)
         │
         ▼
    BaseOSLake { OSLakeName, OSLakeIp }
         │
         ▼
    DockerClientFromOSLake → builds DockerOSOrchestrator(tcp://{ip}:2375)
```

The two paths are completely disconnected. [ContainerBuildService](file:///c:/Users/kaavin/programming/container-management/Engines/FileStorageEngines/ContainerBuild/ContainerBuildService.cs#7-60) doesn't know about OS Lakes. `DeploymentManager.Program.cs` uses the [OSLakeConnector](file:///c:/Users/kaavin/programming/container-management/OperatingSystemLake/Abstractions/OSLakeConnector.cs#15-32) chain but never calls [ContainerBuildService](file:///c:/Users/kaavin/programming/container-management/Engines/FileStorageEngines/ContainerBuild/ContainerBuildService.cs#7-60).

---

## The Fix: Make ContainerBuildService Accept a DockerClient, Not a URI

The root cause is that [ContainerBuildService](file:///c:/Users/kaavin/programming/container-management/Engines/FileStorageEngines/ContainerBuild/ContainerBuildService.cs#7-60) **owns** its Docker client creation. It shouldn't — the caller should provide it, after resolving the target lake.

### Current (broken)
```csharp
public ContainerBuildService(string dockerDaemonUri, string sidecarPublishDir)
{
    _dockerClient = new DockerClientConfiguration(new Uri(dockerDaemonUri)).CreateClient();
}
```

### Proposed
```csharp
public ContainerBuildService(DockerClient dockerClient, string sidecarPublishDir)
{
    _dockerClient = dockerClient;
    _sidecarPublishDir = sidecarPublishDir;
}
```

Now the calling code is responsible for deciding *which* Docker daemon to use.

---

## The Calling Pattern: OSLake → DockerClient → ContainerBuildService

The call chain becomes:

```
1. OSLakeConnectorFactory.Create(techType, processCommunicator)
       → returns an OSLakeConnector (VirtualBox / DockerMachine / AWS)

2. connector.GetOSLakeByType(OSLakeTypes.Linux)
       → returns a BaseOSLake { OSLakeIp = "192.168.99.100" }

3. new DockerClientFromOSLake(lake.OSLakeIp).GetOSOrchestrator()
       → returns DockerOSOrchestrator with a live DockerClient connected to tcp://192.168.99.100:2375

4. new ContainerBuildService(orchestrator._client, sidecarPublishDir)
       → builds & starts the user container on THAT specific lake
```

In code, the wiring looks like:

```csharp
// In ProcessProject or a new ContainerBuildJob — after a project is approved

// 1. Pick the lake
var connector = OSLakeConnectorFactory.Create(
    OSLakeTechTypes.DockerMachine, processCommunicator);
var lake = connector.GetOSLakeByType(OSLakeTypes.Linux);

// 2. Get a DockerClient connected to that lake's Docker daemon
var orchestratorStrategy = new DockerClientFromOSLake(lake.OSLakeIp);
var orchestrator = (DockerOSOrchestrator)orchestratorStrategy.GetOSOrchestrator();

// 3. Pass the live client to ContainerBuildService
var buildService = new ContainerBuildService(orchestrator._client, sidecarPublishDir);
await buildService.BuildAndStartProjectContainer(projectStream, recipe, projectId);
```

---

## What Needs to Change (File by File)

| File | Change | Effort |
|---|---|---|
| [ContainerBuildService.cs](file:///c:/Users/kaavin/programming/container-management/Engines/FileStorageEngines/ContainerBuild/ContainerBuildService.cs) | Constructor takes [DockerClient](file:///c:/Users/kaavin/programming/container-management/OSOrchestrator/Implementations/DockerClientFromOSLake.cs#12-29) instead of `string dockerDaemonUri` | 2 lines |
| [DockerOSOrchestrator.cs](file:///c:/Users/kaavin/programming/container-management/OSOrchestrator/Implementations/DockerOSOrchestrator.cs) | Make `_client` accessible (it's already `public` — no change needed) | 0 lines |
| [ProcessProject in ProjectProcessingJobEnque.cs](file:///c:/Users/kaavin/programming/container-management/Engines/FileStorageEngines/Implementations/ProjectProcessingJobEnque.cs) | After `CLEAN` result, add the 3-step wiring above to trigger build on the resolved lake | ~15 lines |
| Engines [.csproj](file:///c:/Users/kaavin/programming/container-management/Domain/Domain.csproj) | Add project reference to `OperatingSystemLake` and [OSOrchestrator](file:///c:/Users/kaavin/programming/container-management/OSOrchestrator/Abstractions/OSOrchestrator.cs#12-16) if not already present | 2 lines |

**Total: ~20 lines of actual new code.**

---

## Why This Design is Extensible

Adding a new OS Lake provider (e.g. AWS EC2) requires **zero changes** to [ContainerBuildService](file:///c:/Users/kaavin/programming/container-management/Engines/FileStorageEngines/ContainerBuild/ContainerBuildService.cs#7-60). The only new code is:

1. Implement `AwsOSLakeConnector.GetOSLakeIp()` (this stub already exists)
2. Add `OSLakeTechTypes.Aws` to the config/request

[ContainerBuildService](file:///c:/Users/kaavin/programming/container-management/Engines/FileStorageEngines/ContainerBuild/ContainerBuildService.cs#7-60) doesn't care *where* the Docker daemon is — it just receives a connected [DockerClient](file:///c:/Users/kaavin/programming/container-management/OSOrchestrator/Implementations/DockerClientFromOSLake.cs#12-29). The lake resolution is completely decoupled.

---

## Optional Enhancement: Lake Selection per Request

Right now [GetOSLakeByType()](file:///c:/Users/kaavin/programming/container-management/OperatingSystemLake/Implementations/Linux/DockerMachineOSLakeConnector.cs#58-73) returns the *first* available lake. For multi-tenant production use, the caller could pass a `lakeName` or let `FileStorageEngineBackgroundService.SelectBestEngine` (the health-based selector you already have) pick the least-loaded lake. The contract stays the same — the output is always a [BaseOSLake](file:///c:/Users/kaavin/programming/container-management/OperatingSystemLake/Abstractions/BaseOSLake.cs#10-21) with an IP.
