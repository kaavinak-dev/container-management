using System.Net.Http;
using Minio;
using Microsoft.Extensions.Hosting;
using Amazon.Runtime;
using Amazon.Runtime.Internal.Auth;

namespace Engines.FileStorageEngines.Abstractions
{

    public enum VirusScanResults
    {
        CLEAN,
        QUARANTINE,
        VIRUS


    }

    public abstract class FileVirusScanner<T, ScanResult> where T : class
    {
        public abstract Task<ScanResult> ScanFileDataAsync(Stream fileData);

        public abstract T GetVirusScanClient();

    }



}
