using OperatingSystemLake.Abstractions;
using OperatingSystemLake.Constants;
using OperatingSystemLake.Implementations.Windows;
using OSOrchestrator.Abstractions;
using OSOrchestrator.Constants;
using OSOrchestrator.Implementations;
using System.Runtime.CompilerServices;

namespace ContainerManagerBackend.Helpers
{
    public  class RequestBodyParser
    {
        IEnumerable<OSOrchestrator.Abstractions.OSOrchestrator> OSOrchestrators;
        IEnumerable<OSLakeOrchestrator> OSLakeOrchestrators;

        public RequestBodyParser(IEnumerable<OSOrchestrator.Abstractions.OSOrchestrator> OsOrchestrator,IEnumerable<OSLakeOrchestrator> OsLakeOrchestrator) { 
            this.OSOrchestrators = OsOrchestrator;
            this.OSLakeOrchestrators = OsLakeOrchestrator;
        }

        public  OSOrchestrator.Abstractions.OSOrchestrator GetOSOrchestratorFromRequest(HttpRequest request)
        {
            var osOrchestratorType = request.Query["OSOrchestrator"];
            if (osOrchestratorType.ToString().ToLower() == OSOrchestratorTypes.Docker.ToString().ToLower()) {

                var validOrchestrators= this.OSOrchestrators.Where((orchestrators) =>
                                        {
                                            if (orchestrators.GetType() == typeof(DockerOSOrchestrator))
                                            {
                                                return true;
                                            }
                                            return false;
                                        });
                if (validOrchestrators.Any()) {
                    return validOrchestrators.First();
                }

            }
            return null;
        }

        public OSLakeOrchestrator GetOSLakeOrchestratorFromRequest(HttpRequest request) {
            var osLakeOrchestratorType = request.Query["OSLakeOrchestrator"];
            if (osLakeOrchestratorType.ToString().ToLower() == OSLakeTechTypes.VirtualBox.ToString().ToLower()) {
                var validLakeOrchestrators = this.OSLakeOrchestrators.Where((orchestrators) =>
                {
                    if (orchestrators.GetType() == typeof(VirtualBoxOSLakeOrchestrator))
                    {
                        return true;
                    }
                    return false;
                });
                if (validLakeOrchestrators.Any())
                {
                    return validLakeOrchestrators.First();
                }
                
            
            }
            return null;
        
        
        } 
    }
}
