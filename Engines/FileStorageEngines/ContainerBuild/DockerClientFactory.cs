using Docker.DotNet;
using OperatingSystemLake.Abstractions;
using OperatingSystemLake.Constants;
using OperatingSystemLake.Implementations.AWS;
using OperatingSystemLake.Implementations.Linux;
using OperatingSystemLake.Implementations.Windows;
using OSOrchestrator.Implementations;

namespace Engines.FileStorageEngines.ContainerBuild;

public class DockerClientFactory : IDockerClientFactory
{
    private readonly IEnumerable<OSLakeConnector> _connectors;

    public DockerClientFactory(IEnumerable<OSLakeConnector> connectors)
        => _connectors = connectors;

    public DockerClient CreateForLake(OSLakeTechTypes techType, OSLakeTypes osType)
    {
        var connector = techType switch
        {
            OSLakeTechTypes.DockerMachine => _connectors.FirstOrDefault(c => c is DockerMachineOSLakeConnector),
            OSLakeTechTypes.VirtualBox    => _connectors.FirstOrDefault(c => c is VirtualBoxOSLakeConnector),
            OSLakeTechTypes.Aws           => _connectors.FirstOrDefault(c => c is AwsOSLakeConnector),
            _ => throw new NotSupportedException($"No connector type mapped for tech type: {techType}")
        } ?? throw new InvalidOperationException(
            $"No registered OSLakeConnector found for tech type '{techType}'. " +
            "Ensure it is registered in DI (AddSingleton<OSLakeConnector, ...>).");
        var lake = connector.GetOSLakeByType(osType);
        var orchestrator = (DockerOSOrchestrator)new DockerClientFromOSLake(lake.OSLakeIp).GetOSOrchestrator();
        return orchestrator.GetOSOrchestratorClient<DockerClient>();
    }
}
