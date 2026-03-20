using OperatingSystemHelpers.Implementations.Windows;
using OperatingSystemLake.Abstractions;
using OperatingSystemLake.Constants;
using OperatingSystemLake.Factory;
using OSOrchestrator.Abstractions;
using OSOrchestrator.Constants;
using OSOrchestrator.Implementations;

namespace ContainerManagerBackend.Services
{
    public class SingletonServicesPreConfigurationBuilder
    {
        private BaseOSLake osLake;
        private OSLakeTechTypes osLakeOrchestrationTechType;
        private OSOrchestratorTypes osOrchestrationType;
        public OSOrchestrator.Abstractions.OSOrchestrator OSOrchestrator { get; set; }
        public OSLakeConnector OSLakeConnector { get; set; }

        public SingletonServicesPreConfigurationBuilder() { }

        /// <summary>
        /// Configures the OS Lake connector using the factory.
        /// Pass OSLakeTechTypes.VirtualBox for Windows VMs (local),
        /// OSLakeTechTypes.DockerMachine for Linux VMs (local dev),
        /// or OSLakeTechTypes.Aws for cloud-hosted instances.
        /// </summary>
        public SingletonServicesPreConfigurationBuilder ConfigureOSLakeConnector(OSLakeTechTypes techType)
        {
            this.osLakeOrchestrationTechType = techType;
            this.OSLakeConnector = OSLakeConnectorFactory.Create(techType, new WindowsProcessCommunicator());
            return this;
        }

        /// <summary>
        /// Configures the OS orchestrator by discovering the appropriate OS lake
        /// via the connector and connecting to its Docker daemon.
        /// </summary>
        public SingletonServicesPreConfigurationBuilder ConfigureOSOrchestrator(OSOrchestratorTypes orchestrationType, OSLakeTypes osType)
        {
            if (OSOrchestratorTypes.Docker.ToString().ToLower() == orchestrationType.ToString().ToLower())
            {
                this.osOrchestrationType = orchestrationType;
                var lake = this.OSLakeConnector.GetOSLakeByType(osType);
                this.OSOrchestrator = new DockerClientFromOSLake(lake.OSLakeIp).GetOSOrchestrator();
            }
            return this;
        }
    }
}
