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
    public class DockerOSOrchestrator:OSOrchestrator<DockerClient>
    {
        public DockerOSOrchestrator(DockerClient client)
        {
            this.OSOrchestratorClient= client;
        }
       
        public override Task BuildOSArtifact()
        {
            throw new NotImplementedException();
        }

        public override DockerClient GetOSOrchestratorClient()
        {
            return this.OSOrchestratorClient;
        }
    }
}
