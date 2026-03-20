# OperatingSystemLake — VM/Docker Host Discovery

The "OS Lake" abstraction represents the host machine(s) running Docker daemons where user containers are deployed. The abstraction lets the codebase switch between local dev VMs and production EC2 instances without changing container build logic.

## Concept

```
OSLakeConnector (abstract)
    ├── DockerMachineOSLakeConnector  → runs "docker-machine ls / ip" CLI commands
    ├── VirtualBoxOSLakeConnector     → runs "VBoxManage" CLI commands
    └── AwsOSLakeConnector            → stub (EC2 production, not yet implemented)
            ↓
    BaseOSLake { OSLakeName, OSLakeIp }
            ↓
    DockerClientFromOSLake → new DockerClientConfiguration("tcp://{ip}:2375")
```

## Key Classes

### `OSLakeConnector` (abstract)
- `GetAvailableOSLakes()` — list all known VMs/instances
- `GetOSLakeByType(OSLakeTypes)` — get first lake matching Linux/Windows
- `GetOSLakeIp(machineName)` — resolve the IP of a named lake

### `DockerMachineOSLakeConnector`
Uses `WindowsProcessCommunicator` (or Linux equivalent) to run:
- `docker-machine ls -q` — list machine names
- `docker-machine ip {name}` — get IP

**Dev environment default.** Returns `LinuxOSLake` instances.

### `VirtualBoxOSLakeConnector`
Uses VBoxManage CLI. Alternative dev connector.

### `AwsOSLakeConnector`
Stub — implement `GetAvailableOSLakes()` using AWS SDK to list EC2 instances and their IPs.

## `OSLakeConnectorFactory`
Creates the correct connector for a given `OSLakeTechTypes` enum value:
- `OSLakeTechTypes.DockerMachine` → `DockerMachineOSLakeConnector`
- `OSLakeTechTypes.VirtualBox` → `VirtualBoxOSLakeConnector`
- `OSLakeTechTypes.Aws` → `AwsOSLakeConnector`

## `DockerClientFromOSLake` (`OSOrchestrator` project)
Bridge from a `BaseOSLake` to a live `DockerClient`:
```csharp
var lake = connector.GetOSLakeByType(OSLakeTypes.Linux);
var orchestrator = new DockerClientFromOSLake(lake.OSLakeIp).GetOSOrchestrator();
// orchestrator._client is a live DockerClient connected to tcp://{lake.ip}:2375
```

## Usage in AsyncJobWorkers
`DockerClientFactory` (in `ContainerBuild/`) wraps this chain and is injected into `ContainerBuildService`. The factory resolves the docker host on demand per job.
