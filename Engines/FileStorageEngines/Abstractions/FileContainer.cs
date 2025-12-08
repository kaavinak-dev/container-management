using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
//using Engines.FileStorageEngines.Abstractions;

namespace Engines.FileStorageEngines.Abstractions
{

    public abstract class FileContainer
    {

        protected string fileName;
        protected string bucketName;
        protected string serverUrl;

        public FileContainer(string _fileName, string _bucketName, string _serverUrl)
        {
            this.fileName = _fileName;
            this.serverUrl = _serverUrl;
            this.bucketName = _bucketName;

        }

        public abstract string getFileName();

        public abstract string getFileStoredServerUrl();

        public abstract string getBucketName();
    }



};
