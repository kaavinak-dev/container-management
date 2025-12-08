using System.Net.Http;
using Minio;
using Microsoft.Extensions.Hosting;
using Amazon.Runtime;
using Amazon.Runtime.Internal.Auth;

namespace Engines.FileStorageEngines.Abstractions
{


    public abstract class FileStorageEngine
    {
        public string _engineUrl;
        public int _enginePort;


        public FileStorageEngine(string engineUrl, int enginePort)
        {
            _engineUrl = engineUrl;
            _enginePort = enginePort;

        }

        public abstract void CreateStorageEngineClient();

        public string GetServerUrl()
        {
            return $"http://{_engineUrl}:{_enginePort.ToString()}";
        }



        public abstract Task<FileContainer> UploadRawBinary(Stream fileData, string bucketName);
        public abstract void UploadExecutable(string bucketName, Stream fileData);

        public abstract Task<Dictionary<string, object>> StorageEngineStatusChecker();



    }
};


