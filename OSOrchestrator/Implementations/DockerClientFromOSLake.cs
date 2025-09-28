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
    public class DockerClientFromOSLake : OSOrchestratorFromOSLakeStrategy<DockerClient>
    {
        private static string osLakeIp;
        public DockerClientFromOSLake(string _osLakeIp)
        {
            osLakeIp= _osLakeIp;
        }
        public Abstractions.OSOrchestrator<DockerClient> GetOSOrchestrator()
        {
            string uri = $"tcp://{osLakeIp}:2375";
            var uriObj= new Uri(uri);
            DockerClient client;
            client = new DockerClientConfiguration(uriObj).CreateClient();
            var containers=client.Containers.ListContainersAsync(new Docker.DotNet.Models.ContainersListParameters() { Limit = 10 }).GetAwaiter().GetResult();
            foreach (var container in containers)
            {
                Console.WriteLine(container.ID + " - " + container.Image);
            }
            return new DockerOSOrchestrator(client);
        }
    }
}
