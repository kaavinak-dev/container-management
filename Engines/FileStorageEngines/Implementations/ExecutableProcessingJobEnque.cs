using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Engines.FileStorageEngines.Abstractions;
using Hangfire;

namespace Engines.FileStorageEngines.Implementations
{
    public class ExecutableProcessingJobEnque : FileProcessingJobEnque
    {

        FileStorageEngine storageEngine;
        IBackgroundJobClient backgroundJobClient;
        FileVirusScanner<ClamAVClient, VirusScanResults> virusScannerClient;
        public ExecutableProcessingJobEnque(IBackgroundJobClient backgroundJobClient, ClamAVVirusScanner localClient)
        {

            this.backgroundJobClient = backgroundJobClient;
            this.virusScannerClient = localClient;
        }




        public override bool isValidFile(Stream fileStreamData)
        {
            return false;
        }

        public override List<string> getFileExtensionsSupported()
        {
            return new List<string>() { "exe" };

        }

        public override async Task DoWork(FileContainer executablFileContainer)
        {
            var serverUrl = executablFileContainer.getFileStoredServerUrl();

            try
            {
                // Split server url which is format http://url:port to get port and url info
                var uri = new Uri(serverUrl);
                string url = uri.Host;
                int port = uri.Port;

                // Use the url and port to create miniofilestorageengine
                var minioEngine = new MinioFileStorageEngine(url, port);
                storageEngine = minioEngine;
                // Create the client using existing method
                storageEngine.CreateStorageEngineClient();

                Console.WriteLine($"Processing executable from {url}:{port}");
                Console.WriteLine($"File: {executablFileContainer.getFileName()}");
                Console.WriteLine($"Bucket: {executablFileContainer.getBucketName()}");

                // Download the file using the new method in MinioFileStorageEngine
                using (var fileStream = await minioEngine.DownloadFile(
                    executablFileContainer.getBucketName(),
                    executablFileContainer.getFileName()))
                {
                    Console.WriteLine($"Downloaded file size: {fileStream.Length} bytes");

                    // Process the executable file
                    ProcessExecutableFile(fileStream, executablFileContainer.getFileName());
                }

                Console.WriteLine($"Completed processing: {executablFileContainer.getFileName()}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing file: {ex.Message}");
                throw; // Hangfire will retry
            }
        }


        private async Task QuarantineExecutableFile(Stream fileStream)
        {

            var fileContainer = await storageEngine.UploadRawBinary(fileStream, "hazard");


        }

        private async Task ProcessExecutableFile(Stream fileStream, string fileName)
        {
            // Your executable processing logic here
            Console.WriteLine($"Processing executable: {fileName}");

            // Example operations:

            // - Virus scan
            var virusScanResult = await this.virusScannerClient.ScanFileDataAsync(fileStream);

            if (virusScanResult != VirusScanResults.CLEAN)
            {
                await QuarantineExecutableFile(fileStream);
            }


            // - Extract metadata
            // - Validate digital signature
            // - Store in database
            // - Move to different bucket
            // etc.
        }

        public override void EnqueJob(FileContainer executablFileContainer)
        {
            this.backgroundJobClient.Enqueue(() => this.DoWork(executablFileContainer));
        }
    }

}
