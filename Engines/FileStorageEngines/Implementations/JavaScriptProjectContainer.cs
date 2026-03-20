using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Engines.FileStorageEngines.Abstractions;
//using Engines.FileStorageEngines.Abstractions;

namespace Engines.FileStorageEngines.Implementations
{
    public class JavaScriptProjectContainer : ProjectContainer
    {

        public JavaScriptProjectContainer(string projectName, string bucketName, string serverUrl) : base(projectName, bucketName, serverUrl)
        {


        }

        public override string getBucketName()
        {
            return this.bucketName;

        }

        public override string getProjectName()
        {
            return this.projectName;
        }

        public override string getProjectStoredServerUrl()
        {
            return this.serverUrl;

        }

        public override ProjectTypes getProjectType()
        {
            return ProjectTypes.JS;
        }

        public override string getProjectArtifactName()
        {
            return this.projectName + ".tar.gz";
        }
    }

    public class ReactJSProjectContainer : JavaScriptProjectContainer
    {
        public ReactJSProjectContainer(string projectName, string bucketName, string serverUrl) : base(projectName, bucketName, serverUrl)
        {

        }
    }

    public class PackageJson
    {
        [JsonPropertyName("author")]
        public Author Author { get; set; }

        [JsonPropertyName("maintainers")]
        public List<Person> Maintainers { get; set; }

        [JsonPropertyName("contributors")]
        public List<Person> Contributors { get; set; }

        [JsonPropertyName("repository")]
        public Repository Repository { get; set; }

        [JsonPropertyName("homepage")]
        public string Homepage { get; set; }

        [JsonPropertyName("bugs")]
        public Bugs Bugs { get; set; }
    }
    public class Author
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }
    }
    public class Person
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; }
    }
    public class Repository
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }
    }
    public class Bugs
    {
        [JsonPropertyName("url")]
        public string Url { get; set; }
    }


    public class JSProjectMetadata : ProjectMetaData
    {
        public string ProjectName { get; set; }
        public string ProjectVersion { get; set; }
        public int DependencyCount { get; set; }
        public List<string> Dependencies { get; set; }
        public int VulnerabilityCount { get; set; }
        public int CriticalVulnerabilities { get; set; }
        public int HighVulnerabilities { get; set; }
        public string NodeVersion { get; set; }
        public string NpmVersion { get; set; }
        public long PackageSize { get; set; }
        public long UnpackedSize { get; set; }
        public int FileCount { get; set; }
        public PackageJson Organization { get; set; }
    }
    public class NpmAuditOutput
    {
        public NpmAuditMetadata Metadata { get; set; }
    }
    public class NpmAuditMetadata
    {
        public NpmVulnerabilitySummary Vulnerabilities { get; set; }
    }
    public class NpmVulnerabilitySummary
    {
        public int Total { get; set; }
        public int Critical { get; set; }
        public int High { get; set; }
        public int Moderate { get; set; }
        public int Low { get; set; }
    }
    public class NpmListOutput
    {
        public string Name { get; set; }
        public string Version { get; set; }
        public Dictionary<string, NpmDependency> Dependencies { get; set; }
    }
    public class NpmDependency
    {
        public string Version { get; set; }
        public string Resolved { get; set; }
    }
    public class NpmConfigOutput
    {
        [JsonPropertyName("node-version")]
        public string NodeVersion { get; set; }

        [JsonPropertyName("npm-version")]
        public string NpmVersion { get; set; }
    }
    public class NpmPackOutput
    {
        public string Name { get; set; }
        public string Version { get; set; }
        public long Size { get; set; }
        public long UnpackedSize { get; set; }
        public int EntryCount { get; set; }
    }
}
