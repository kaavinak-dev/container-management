using OperatingSystemLake.Abstractions;
using OperatingSystemLake.Constants;
using OperatingSystemLake.Implementations.Linux;
using OperatingSystemLake.Implementations.Windows;
using OSOrchestrator.Abstractions;
using OSOrchestrator.Constants;
using OSOrchestrator.Implementations;

namespace ContainerManagerBackend.Helpers
{
    public class RequestBodyParser
    {
        IEnumerable<OSOrchestrator.Abstractions.OSOrchestrator> OSOrchestrators;
        IEnumerable<OSLakeConnector> OSLakeConnectors;

        public RequestBodyParser(IEnumerable<OSOrchestrator.Abstractions.OSOrchestrator> OsOrchestrator, IEnumerable<OSLakeConnector> osLakeConnectors)
        {
            this.OSOrchestrators = OsOrchestrator;
            this.OSLakeConnectors = osLakeConnectors;
        }

        public OSOrchestrator.Abstractions.OSOrchestrator GetOSOrchestratorFromRequest(HttpRequest request)
        {
            var osOrchestratorType = request.Query["OSOrchestrator"];
            if (osOrchestratorType.ToString().ToLower() == OSOrchestratorTypes.Docker.ToString().ToLower())
            {
                var validOrchestrators = this.OSOrchestrators.Where((orchestrators) =>
                {
                    if (orchestrators.GetType() == typeof(DockerOSOrchestrator))
                    {
                        return true;
                    }
                    return false;
                });
                if (validOrchestrators.Any())
                {
                    return validOrchestrators.First();
                }
            }
            return null;
        }

        public OSLakeConnector GetOSLakeConnectorFromRequest(HttpRequest request)
        {
            var techType = request.Query["OSLakeTechType"];

            if (techType.ToString().ToLower() == OSLakeTechTypes.VirtualBox.ToString().ToLower())
            {
                var match = this.OSLakeConnectors.Where(c => c.GetType() == typeof(VirtualBoxOSLakeConnector));
                if (match.Any()) return match.First();
            }

            if (techType.ToString().ToLower() == OSLakeTechTypes.DockerMachine.ToString().ToLower())
            {
                var match = this.OSLakeConnectors.Where(c => c.GetType() == typeof(DockerMachineOSLakeConnector));
                if (match.Any()) return match.First();
            }

            return null;
        }
    }
}
