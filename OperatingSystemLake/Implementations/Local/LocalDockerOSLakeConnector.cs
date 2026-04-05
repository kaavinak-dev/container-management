using OperatingSystemLake.Abstractions;
using OperatingSystemLake.Constants;
using OperatingSystemLake.Implementations.Linux;

namespace OperatingSystemLake.Implementations.Local;

/// <summary>
/// OS Lake connector for Docker Desktop (local development).
/// No CLI discovery needed — Docker Desktop is always reachable on the local machine.
/// Returns a single lake at 127.0.0.1; the Docker client URI (named pipe / Unix socket)
/// is resolved separately in DockerClientFactory.
/// </summary>
public class LocalDockerOSLakeConnector : OSLakeConnector
{
    private static readonly BaseOSLake LocalLake = new LinuxOSLake("local", "127.0.0.1");

    public override List<BaseOSLake> GetAvailableOSLakes() => new() { LocalLake };

    public override BaseOSLake GetOSLakeByType(OSLakeTypes osType)
    {
        if (osType != OSLakeTypes.Linux)
            throw new NotSupportedException(
                $"LocalDockerOSLakeConnector only supports Linux OS lakes. Requested: {osType}");

        return LocalLake;
    }

    public override string GetOSLakeIp(string lakeName) => "127.0.0.1";
}
