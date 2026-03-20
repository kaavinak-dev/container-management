using Docker.DotNet;
using OperatingSystemLake.Abstractions;
using OperatingSystemLake.Constants;

namespace Engines.FileStorageEngines.ContainerBuild;

public interface IDockerClientFactory
{
    DockerClient CreateForLake(OSLakeTechTypes techType, OSLakeTypes osType);
}
