using Docker.DotNet;
using OperatingSystemLake.Abstractions;
using OperatingSystemLake.Constants;
using OperatingSystemLake.Implementations.AWS;
using OperatingSystemLake.Implementations.Linux;
using OperatingSystemLake.Implementations.Local;
using OperatingSystemLake.Implementations.Windows;
using OSOrchestrator.Implementations;
using System.Runtime.InteropServices;

namespace Engines.FileStorageEngines.ContainerBuild;

public class DockerClientFactory : IDockerClientFactory
{
    private readonly IEnumerable<OSLakeConnector> _connectors;

    public DockerClientFactory(IEnumerable<OSLakeConnector> connectors)
        => _connectors = connectors;

    public DockerClient CreateForLake(OSLakeTechTypes techType, OSLakeTypes osType)
    {
        // LocalDocker bypasses the OSLake connector chain entirely — Docker Desktop
        // is always local and reachable via named pipe (Windows) or Unix socket (Linux/Mac).
        if (techType == OSLakeTechTypes.LocalDocker)
        {
            var uri = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? new Uri("npipe://./pipe/docker_engine")
                : new Uri("unix:///var/run/docker.sock");
            return new DockerClientConfiguration(uri).CreateClient();
        }

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
