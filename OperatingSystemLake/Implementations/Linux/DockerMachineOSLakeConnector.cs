using OperatingSystemHelpers.Abstractions;
using OperatingSystemLake.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OperatingSystemLake.Implementations.Linux
{
    /// <summary>
    /// OS Lake connector backed by Docker Machine (docker-machine CLI).
    /// Used for local Linux container development — each docker-machine VM
    /// hosts a Docker daemon where Linux user containers are created.
    ///
    /// For production, swap this connector with AwsOSLakeConnector by changing
    /// OSLakeTechTypes.DockerMachine → OSLakeTechTypes.Aws in config.
    /// </summary>
    public class DockerMachineOSLakeConnector : OSLakeConnector
    {
        private readonly ProcessCommunicator _processCommunicator;

        public DockerMachineOSLakeConnector(ProcessCommunicator processCommunicator)
        {
            _processCommunicator = processCommunicator;
            _processCommunicator.StartProcess();
        }

        public override List<BaseOSLake> GetAvailableOSLakes()
        {
            var machineNames = new List<string>();

            _processCommunicator.StartTransaction();
            _processCommunicator.ExecuteCommand("docker-machine ls -q",
                (err, outputLogs) =>
                {
                    if (outputLogs.Data != null && outputLogs.Data.Trim().Length > 0)
                    {
                        machineNames.Add(outputLogs.Data.Trim());
                    }
                },
                (err, errorLogs) =>
                {
                    if (errorLogs.Data != null) Console.WriteLine(errorLogs.Data);
                });
            _processCommunicator.EndTransaction();

            var lakes = new List<BaseOSLake>();
            foreach (var name in machineNames)
            {
                var ip = GetOSLakeIp(name);
                if (ip != null) lakes.Add(new LinuxOSLake(name, ip));
            }

            return lakes;
        }

        public override BaseOSLake GetOSLakeByType(OSLakeTypes osType)
        {
            if (osType != OSLakeTypes.Linux)
            {
                throw new NotSupportedException($"DockerMachine connector only supports Linux OS lakes. Requested: {osType}");
            }

            var lakes = GetAvailableOSLakes();
            var lake = lakes.FirstOrDefault();
            if (lake == null)
            {
                throw new InvalidOperationException("No running Docker Machine instances found. Run 'docker-machine start' first.");
            }
            return lake;
        }

        public override string GetOSLakeIp(string machineName)
        {
            string? ipAddress = null;

            _processCommunicator.StartTransaction();
            _processCommunicator.ExecuteCommand($"docker-machine ip {machineName}",
                (err, outputLogs) =>
                {
                    if (outputLogs.Data != null && ipAddress == null)
                    {
                        var trimmed = outputLogs.Data.Trim();
                        if (trimmed.Length > 0 && !trimmed.Contains("Error"))
                        {
                            ipAddress = trimmed;
                        }
                    }
                },
                (err, errorLogs) =>
                {
                    if (errorLogs.Data != null) Console.WriteLine(errorLogs.Data);
                });
            _processCommunicator.EndTransaction();

            return ipAddress;
        }
    }
}
