using System.Net.Http;
using Minio;
using Microsoft.Extensions.Hosting;
using Amazon.Runtime;
using Amazon.Runtime.Internal.Auth;

namespace Engines.FileStorageEngines.Abstractions
{


    public abstract class ProjectStorageEngine
    {
        public string _engineUrl;
        public int _enginePort;


        public ProjectStorageEngine(string engineUrl, int enginePort)
        {
            _engineUrl = engineUrl;
            _enginePort = enginePort;

        }

        public abstract void CreateStorageEngineClient();

        public string GetServerUrl()
        {
            return $"http://{_engineUrl}:{_enginePort.ToString()}";
        }



        public abstract Task<string> UploadProject(Stream projectData, string bucketName, string projectName = null);
        public abstract Task DeleteProject(string bucketName, string objectName);
        public abstract void UploadExecutable(string bucketName, Stream projectData);

        public abstract Task<Dictionary<string, object>> StorageEngineStatusChecker();



    }
};


