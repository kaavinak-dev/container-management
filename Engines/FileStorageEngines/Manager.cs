using Engines.FileStorageEngines.Abstractions;
using Engines.FileStorageEngines.Implementations;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Configuration;
using System.Collections;
using Newtonsoft.Json;



namespace Engines.FileStorageEngines
{

    public interface IProjectStorageManager
    {

        public List<ProjectStorageEngine> GetStorageEngines();


        public ProjectStorageEngine GetFileStorageEngine();


        public void SetFileStorageEngine(ProjectStorageEngine electedStorageEngine);


        public ProjectStorageEngine QueryStorageEngine(string url, int port);


    }


    public class ProjectStorageManager : IProjectStorageManager
    {
        private static List<ProjectStorageEngine> storageEngines;
        private static ProjectStorageEngine StorageEngine;
        private static bool isDevEnv = true;
        private static bool server_clients_generated = false;

        public ProjectStorageManager(List<Dictionary<string, string>> config)
        {

            if (storageEngines == null || storageEngines.Count <= 0)
            {
                storageEngines = FetchCurrentRunningStorageEngines(config);
            }
        }

        public void GenerateServerClients()
        {
            if (server_clients_generated == false)
            {
                storageEngines.ForEach(engine => engine.CreateStorageEngineClient());
                server_clients_generated = true;
            }

        }


        public List<ProjectStorageEngine> FetchCurrentRunningStorageEnginesDevEnv(List<Dictionary<string, string>> config)
        {
            var fetchedEngines = new List<ProjectStorageEngine>();

            // Iterate through each server dictionary in the list
            foreach (var server in config)
            {
                string serverUrl = server["Url"];
                int serverPort = int.Parse(server["Port"]);

                fetchedEngines.Add(new MinioProjectStorageEngine(serverUrl, serverPort));
            }

            return fetchedEngines;
        }


        public List<ProjectStorageEngine> FetchCurrentRunningStorageEngines(List<Dictionary<string, string>> config = null)
        {

            if (isDevEnv)
            {
                return FetchCurrentRunningStorageEnginesDevEnv(config);
            }
            return null;
            // string infraUri = "tcp://{Ip}:2375";
            // var uri = new Uri(infraUri);
            // var dockerClient = new DockerClientConfiguration(uri).CreateClient();
            // //AI:  use the parameters inside ListContainersAsync to fetch for minio images, after that extract the port and url to create FileStorageEngine objects

            // // Create list to store the FileStorageEngine objects
            // //

            // var filters = new Dictionary<string, IDictionary<string, bool>>
            // {
            //                 // Filter by status = running
            //     { "status", new Dictionary<string, bool> { { "running", true } } },

            //     // Filter by image name
            //     { "ancestor", new Dictionary<string, bool> { { "minio/minio", true } } },

            //     // Filter by container name
            //     // NOTE: container names in Docker API always start with '/'
            //     //{ "name", new Dictionary<string, bool> { { containerNameFilter, true } } }
            // };

            // var containers = await dockerClient.Containers.ListContainersAsync(
            //     new ContainersListParameters()
            //     {
            //         Filters = filters
            //     }
            // );

            // var engines = new List<FileStorageEngine>() { };

            // // Process each container
            // foreach (var container in containers)
            // {
            //     // Inspect container to get detailed port information
            //     var containerDetails = await dockerClient.Containers.InspectContainerAsync(container.ID);

            //     // Extract port mappings from NetworkSettings
            //     var ports = containerDetails.NetworkSettings.Ports;

            //     // MinIO typically uses port 9000 or 9001 internally
            //     // Look for the mapped port
            //     foreach (var portMapping in ports)
            //     {
            //         // portMapping.Key is like "9000/tcp"
            //         // portMapping.Value is the list of host bindings
            //         if (portMapping.Key.StartsWith("9000") || portMapping.Key.StartsWith("9001"))
            //         {
            //             if (portMapping.Value != null && portMapping.Value.Count > 0)
            //             {
            //                 // Get the first host binding
            //                 var hostBinding = portMapping.Value[0];
            //                 string hostIp = hostBinding.HostIP;
            //                 int hostPort = int.Parse(hostBinding.HostPort);

            //                 // Use localhost if HostIP is 0.0.0.0 or empty
            //                 if (string.IsNullOrEmpty(hostIp) || hostIp == "0.0.0.0")
            //                 {
            //                     hostIp = "localhost";
            //                 }

            //                 // Create MinioFileStorageEngine instance
            //                 engines.Add(new MinioFileStorageEngine(hostIp, hostPort));
            //                 break; // Move to next container after finding the first valid port
            //             }
            //         }
            //     }
            // }

            // return engines;

        }

        public List<ProjectStorageEngine> GetStorageEngines()
        {

            return storageEngines;
        }

        public ProjectStorageEngine GetFileStorageEngine()
        {
            return StorageEngine;
        }

        public void SetFileStorageEngine(ProjectStorageEngine electedStorageEngine)
        {
            StorageEngine = electedStorageEngine;
        }

        public ProjectStorageEngine QueryStorageEngine(string url, int port)
        {
            ProjectStorageEngine filteredEngine = storageEngines.Where(engine =>
            {

                if (engine._enginePort == port && engine._engineUrl == url)
                {
                    return true;
                }
                return false;
            }).ToList().FirstOrDefault();

            return filteredEngine;

        }

    }


    public class ProjectStorageEngineBackgroundService : BackgroundService
    {

        ProjectStorageManager engineManagerInstance;
        public ProjectStorageEngineBackgroundService(ProjectStorageManager engineManager)
        {

            engineManagerInstance = engineManager;
            engineManagerInstance.GenerateServerClients();

        }

        protected ProjectStorageEngine ElectBestEngine(List<Dictionary<string, object>> engineHealthObjects)
        {
            var byFreeBytesUsable = engineHealthObjects
            .Where(e => e.ContainsKey("FreeBytesUsable") && e.ContainsKey("server"))
            .OrderByDescending(e => Convert.ToInt64(e["FreeBytesUsable"]))
            .Select(e => e["server"].ToString())
            .ToList();

            var byS3RequestsPending = engineHealthObjects
            .Where(e => e.ContainsKey("S3RequestsPending") && e.ContainsKey("server"))
            .OrderBy(e => Convert.ToInt32(e["S3RequestsPending"]))
            .Select(e => e["server"].ToString())
            .ToList();

            var byHealthStatus = engineHealthObjects
            .Where(e => e.ContainsKey("HealthStatus") && e.ContainsKey("server"))
            .OrderByDescending(e => e["HealthStatus"].ToString())
            .Select(e => e["server"].ToString())
            .ToList();

            var byS3RejectedTotal = engineHealthObjects
            .Where(e => e.ContainsKey("S3RejectedTotal") && e.ContainsKey("server"))
            .OrderBy(e => Convert.ToInt32(e["S3RejectedTotal"]))
            .Select(e => e["server"].ToString())
            .ToList();

            var engineScores = new Dictionary<string, int>();

            foreach (var engine in engineHealthObjects)
            {
                if (!engine.ContainsKey("server")) continue;

                string serverKey = engine["server"].ToString();
                int score = 0;

                // Higher score for better position in each list
                int freeBytesIndex = byFreeBytesUsable.IndexOf(serverKey);
                if (freeBytesIndex >= 0)
                    score += (byFreeBytesUsable.Count - freeBytesIndex) * 4;

                int requestsPendingIndex = byS3RequestsPending.IndexOf(serverKey);
                if (requestsPendingIndex >= 0)
                    score += (byS3RequestsPending.Count - requestsPendingIndex) * 3;

                int healthStatusIndex = byHealthStatus.IndexOf(serverKey);
                if (healthStatusIndex >= 0)
                    score += (byHealthStatus.Count - healthStatusIndex) * 5;

                int rejectedTotalIndex = byS3RejectedTotal.IndexOf(serverKey);
                if (rejectedTotalIndex >= 0)
                    score += (byS3RejectedTotal.Count - rejectedTotalIndex) * 2;

                engineScores[serverKey] = score;
            }

            // Get the server key with the highest score
            string bestServerKey = engineScores.OrderByDescending(kvp => kvp.Value).FirstOrDefault().Key;

            // Return the engine object that matches the best server key
            List<string> serverInfo = bestServerKey.Split(":").ToList();
            string serverUrl = serverInfo[0];
            int serverPort = int.Parse(serverInfo[1]);
            return engineManagerInstance.QueryStorageEngine(serverUrl, serverPort);


        }


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(250000));


            try
            {
                if (!stoppingToken.IsCancellationRequested)
                {
                    List<Dictionary<string, object>> engineStatusObjects = new();

                    foreach (var engine in engineManagerInstance.GetStorageEngines())
                    {
                        try
                        {
                            engineStatusObjects.Add(await EngineInfoCheck(engine, stoppingToken));
                        }
                        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                        {
                            return;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error checking engine: {ex.Message}");
                        }
                    }
                    var electedStorageEngine = ElectBestEngine(engineStatusObjects);
                    engineManagerInstance.SetFileStorageEngine(electedStorageEngine);


                }
                while (await timer.WaitForNextTickAsync(stoppingToken))
                {

                    if (stoppingToken.IsCancellationRequested) { break; }
                    List<Dictionary<string, object>> engineStatusObjects = new();

                    foreach (var engine in engineManagerInstance.GetStorageEngines())
                    {
                        try
                        {
                            engineStatusObjects.Add(await EngineInfoCheck(engine, stoppingToken));
                        }
                        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                        {
                            return;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error checking engine: {ex.Message}");
                        }
                    }
                    var electedStorageEngine = ElectBestEngine(engineStatusObjects);
                    engineManagerInstance.SetFileStorageEngine(electedStorageEngine);

                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {

            }
            catch (Exception ex)
            {

                throw ex;

            }
        }

        private async Task<Dictionary<string, object>> EngineInfoCheck(ProjectStorageEngine engine, CancellationToken cancelToken)
        {
            if (!cancelToken.IsCancellationRequested)
            {
                return await engine.StorageEngineStatusChecker();
            }
            return null;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            return base.StopAsync(cancellationToken);
        }
    }
}
