using Docker.DotNet;
using OSOrchestrator.Abstractions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSOrchestrator.Implementations
{
    public class DockerClientFromOSLake : OSOrchestratorFromOSLakeStrategy
    {
        private readonly string osLakeIp;
        public DockerClientFromOSLake(string _osLakeIp)
        {
            osLakeIp= _osLakeIp;
        }
        public Abstractions.OSOrchestrator GetOSOrchestrator()
        {
            string uri = $"tcp://{osLakeIp}:2375";
            var uriObj= new Uri(uri);
           
            var orchestrator=new DockerOSOrchestrator(uri);
            orchestrator.CreateAndSetOSOrchestratorClient();
            return orchestrator;
        }
    }
}
