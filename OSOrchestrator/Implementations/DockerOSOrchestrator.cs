using Docker.DotNet;
using Docker.DotNet.Models;
using Newtonsoft.Json;
using OSOrchestrator.Abstractions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSOrchestrator.Implementations
{
    public class DockerOSOrchestrator:OSOrchestrator.Abstractions.OSOrchestrator
    {
        public DockerClient _client;
        public string dockerDaemonUri;
        public DockerOSOrchestrator():base()
        {
            
        }

        public DockerOSOrchestrator(string _dockerDaemonUri)
        {
            this.dockerDaemonUri = _dockerDaemonUri;
        }

        public override void CreateOS()
        {
            var createContainerConfig = new CreateContainerParameters() { 
            Image="osprocessmanager",
            Name="new_auto_image"
            };
            var containerCreationResponse= _client.Containers.CreateContainerAsync(createContainerConfig).GetAwaiter().GetResult();
            var containerId = containerCreationResponse.ID;
            var started = _client.Containers.StartContainerAsync(containerId, new ContainerStartParameters()).GetAwaiter().GetResult();
            if (!started)
            {
                throw new Exception("container failed to start");
            }

        }

        public override T GetOSOrchestratorClient<T>()
        {
            if (typeof(T) == typeof(DockerClient))
            {
                return (T)(object)_client;
            }
            throw new Exception("invalid docker client");
        }

        public override void CreateAndSetOSOrchestratorClient()
        {
            var uri = new Uri(dockerDaemonUri);
            this._client = new DockerClientConfiguration(uri).CreateClient();           
        }
    }
}
