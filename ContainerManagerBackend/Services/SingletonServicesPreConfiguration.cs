using OperatingSystemHelpers.Implementations.Windows;
using OperatingSystemLake.Abstractions;
using OperatingSystemLake.Constants;
using OperatingSystemLake.Implementations.Windows;
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
        public OSLakeOrchestrator OSLakeOrchestrator { get; set; }

        public SingletonServicesPreConfigurationBuilder() { }

       
        public SingletonServicesPreConfigurationBuilder ConfigureOSLakeOrchestrator(OperatingSystemLake.Constants.OSLakeTechTypes oSOrchestratorTypes)
        {
            if (OSLakeTechTypes.VirtualBox.ToString().ToLower() == oSOrchestratorTypes.ToString().ToLower())
            {
                this.osLakeOrchestrationTechType = oSOrchestratorTypes;
                this.OSLakeOrchestrator= new VirtualBoxOSLakeOrchestrator(new WindowsProcessCommunicator());
            }
            return this;
        }

        public SingletonServicesPreConfigurationBuilder ConfigureOSOrchestrator(OSOrchestratorTypes orchestrationTypes)
        {
            if(OSOrchestratorTypes.Docker.ToString().ToLower() == orchestrationTypes.ToString().ToLower() && 
                OSLakeTechTypes.VirtualBox.ToString().ToLower() == this.osLakeOrchestrationTechType.ToString().ToLower()
                )
            {
                this.osOrchestrationType= orchestrationTypes;
                var ip = this.OSLakeOrchestrator.GetRunningOSLakeForOrchestratorType(OSLakeTypes.Windows).OSLakeIp;
                this.OSOrchestrator = new DockerClientFromOSLake(ip).GetOSOrchestrator();
            }
            return this;
        }

      
    }
}
