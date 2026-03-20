using Docker.DotNet;
using OperatingSystemLake.Abstractions;
using OperatingSystemLake.Constants;
using OSOrchestrator.Implementations;

namespace Engines.FileStorageEngines.ContainerBuild;

public class DockerClientFactory : IDockerClientFactory
{
    private readonly IEnumerable<OSLakeConnector> _connectors;

    public DockerClientFactory(IEnumerable<OSLakeConnector> connectors)
        => _connectors = connectors;

    public DockerClient CreateForLake(OSLakeTechTypes techType, OSLakeTypes osType)
    {
        var connector = _connectors.First(c => c.GetType().Name.Contains(techType.ToString()));
        var lake = connector.GetOSLakeByType(osType);
        var orchestrator = (DockerOSOrchestrator)new DockerClientFromOSLake(lake.OSLakeIp).GetOSOrchestrator();
        return orchestrator.GetOSOrchestratorClient<DockerClient>();
    }
}
