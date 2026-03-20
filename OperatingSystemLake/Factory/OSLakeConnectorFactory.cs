using OperatingSystemHelpers.Abstractions;
using OperatingSystemLake.Abstractions;
using OperatingSystemLake.Constants;
using OperatingSystemLake.Implementations.AWS;
using OperatingSystemLake.Implementations.Linux;
using OperatingSystemLake.Implementations.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OperatingSystemLake.Factory
{
    public static class OSLakeConnectorFactory
    {
        /// <summary>
        /// Creates the appropriate OS Lake connector based on the tech type.
        /// Add new cases here when adding support for new hosting providers.
        /// </summary>
        public static OSLakeConnector Create(OSLakeTechTypes techType, ProcessCommunicator processCommunicator = null)
        {
            return techType switch
            {
                OSLakeTechTypes.VirtualBox    => new VirtualBoxOSLakeConnector(processCommunicator
                    ?? throw new ArgumentNullException(nameof(processCommunicator), "VirtualBox connector requires a ProcessCommunicator")),
                OSLakeTechTypes.DockerMachine => new DockerMachineOSLakeConnector(processCommunicator
                    ?? throw new ArgumentNullException(nameof(processCommunicator), "DockerMachine connector requires a ProcessCommunicator")),
                OSLakeTechTypes.Aws           => new AwsOSLakeConnector(),
                _ => throw new NotSupportedException($"OS Lake tech type '{techType}' is not supported.")
            };
        }
    }
}
