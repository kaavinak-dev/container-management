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


    public enum ProjectTypes
    {
        JS
    }


    public abstract class ProjectContainer
    {

        protected string projectName;
        protected string bucketName;
        protected string serverUrl;


        public ProjectContainer(string _projectName, string _bucketName, string _serverUrl)
        {
            this.projectName = _projectName;
            this.serverUrl = _serverUrl;
            this.bucketName = _bucketName;

        }

        public abstract string getProjectName();

        public abstract string getProjectStoredServerUrl();

        public abstract string getBucketName();

        public abstract ProjectTypes getProjectType();

        public abstract string getProjectArtifactName();
    }



};
