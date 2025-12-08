using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Engines.FileStorageEngines.Abstractions;
//using Engines.FileStorageEngines.Abstractions;

namespace Engines.FileStorageEngines.Implementations
{
    public class ExecutableFileContainer : FileContainer
    {

        public ExecutableFileContainer(string fileName, string bucketName, string serverUrl) : base(fileName, bucketName, serverUrl)
        {


        }

        public override string getBucketName()
        {
            return this.bucketName;

        }

        public override string getFileName()
        {
            return this.fileName;
        }

        public override string getFileStoredServerUrl()
        {
            return this.serverUrl;

        }
    }
}
