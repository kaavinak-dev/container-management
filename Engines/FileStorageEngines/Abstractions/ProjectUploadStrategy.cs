using System.Net.Http;
using Minio;
using Microsoft.Extensions.Hosting;
using Amazon.Runtime;
using Amazon.Runtime.Internal.Auth;
using Engines.FileStorageEngines.Implementations;


namespace Engines.FileStorageEngines.Abstractions
{

    public abstract class ProjectUploadStrategy
    {
        protected ProjectStorageEngine storageEngine;

        public ProjectUploadStrategy(ProjectStorageEngine storageEngine)
        {
            this.storageEngine = storageEngine;
        }

        public abstract Task<ProjectContainer> UploadProject(Stream ProjectData, string bucketName);



    }

    public static class ProjectUploadStrategyFactory
    {
        public static ProjectUploadStrategy GetUploadStrategy(ProjectStorageEngine storageEngine, ProjectTypes projectType)
        {
            switch (projectType)
            {
                case ProjectTypes.JS:
                    return new JSProjectUploadStrategy(storageEngine);
                default:
                    return null;
            }
        }
    }

}
