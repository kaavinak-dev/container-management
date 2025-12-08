using System.Net.Http;
using Minio;
using Microsoft.Extensions.Hosting;
using Amazon.Runtime;
using Amazon.Runtime.Internal.Auth;
using Engines.FileStorageEngines.Abstractions;
using nClam;

namespace Engines.FileStorageEngines.Implementations
{

    //AI ensure ClamAVClient is added into DI container in AsyncJobWorkers project
    //AI ClamAv should be accessible to ExecutableProciessingJobEnque constructor which is in DI container of AsyncJobWorkers
    //AI url and port will be provided in asynjobworkers program.cs
    public class ClamAVClient
    {
        private readonly string virusScannerServerUrl;
        private readonly int virusScannerServerPort;

        // Static nClam client for ClamAV, this will be a singleton in DI container hence only one client instance for entire app
        public readonly ClamClient _clamClient;

        public ClamAVClient(string url, int port)
        {
            this.virusScannerServerUrl = url;
            this.virusScannerServerPort = port;

            // Create the nClam client
            _clamClient = new ClamClient(url, port);
        }

        public async Task<ClamScanResult> ScanStreamAsync(Stream fileStream)
        {
            return await _clamClient.SendAndScanFileAsync(fileStream);
        }


    }


    public class ClamAVVirusScanner : FileVirusScanner<ClamAVClient, VirusScanResults>
    {
        private readonly ClamAVClient _clamAVClient;

        public ClamAVVirusScanner(ClamAVClient clamAVClient)
        {
            _clamAVClient = clamAVClient;
        }

        public override ClamAVClient GetVirusScanClient()
        {

            return _clamAVClient;

        }

        public override async Task<VirusScanResults> ScanFileDataAsync(Stream fileData)
        {
            try
            {
                Console.WriteLine("Starting ClamAV virus scan...");

                // Scan the file stream
                var scanResult = await _clamAVClient.ScanStreamAsync(fileData);

                // Check the result
                if (scanResult.Result == ClamScanResults.Clean)
                {
                    //Console.WriteLine("✓ File is clean - no virus detected");
                    return VirusScanResults.CLEAN;
                }
                else if (scanResult.Result == ClamScanResults.VirusDetected)
                {
                    var virusName = scanResult.InfectedFiles?.FirstOrDefault()?.VirusName ?? "Unknown";
                    //Console.WriteLine($"✗ VIRUS DETECTED: {virusName}");
                    return VirusScanResults.VIRUS;
                }
                else if (scanResult.Result == ClamScanResults.Error)
                {
                    return VirusScanResults.QUARANTINE;
                }
                else
                {
                    return VirusScanResults.QUARANTINE;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during virus scan: {ex.Message}");
                throw;
            }
        }
    }

}
