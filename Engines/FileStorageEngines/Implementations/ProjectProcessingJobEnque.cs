using System;
using System.Collections.Generic;
using System.Formats.Tar;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Engines.DataBaseStorageEngines.Abstractions;
using Engines.DataBaseStorageEngines.Entities;
using Engines.DataBaseStorageEngines.Implementations.Mappers;
using Engines.FileStorageEngines.Abstractions;
using Engines.FileStorageEngines.ContainerBuild;
using Engines.FileStorageEngines.Recipes;
using Hangfire;
using OperatingSystemHelpers;
using OperatingSystemHelpers.Abstractions;

namespace Engines.FileStorageEngines.Implementations
{

    public static class ProjectProcessingJobEnqueHelper
    {

        public static void EnqueJob(IBackgroundJobClient jobClient, ProjectContainer file)
        {
            var projectType = file.getProjectType();
            switch (projectType)
            {
                case ProjectTypes.JS:
                    new JSProjectProcessingJobEnque(jobClient).EnqueJob(file);
                    break;
                default:
                    throw new Exception("Unrecogonized project type");
            }
        }
    }

    public class RiskAssessment
    {
        public string RiskLevel { get; set; }
        public int RiskScore { get; set; }
        public List<string> Issues { get; set; }
        public string Action { get; set; }
    }

    public class JSProjectProcessingJobEnque : ProcessingJobEnque
    {

        ProjectStorageEngine storageEngine;
        IBackgroundJobClient backgroundJobClient;
        FileVirusScanner<ClamAVClient, VirusScanResults> virusScannerClient;
        IMetadataStorageEngine? metadataStorageEngine;
        ContainerBuildService? containerBuildService;
        ProcessCommunicator? processCommunicator;

        public JSProjectProcessingJobEnque(
            IBackgroundJobClient backgroundJobClient,
            ClamAVVirusScanner localClient = null,
            IMetadataStorageEngine metadataStorageEngine = null,
            ContainerBuildService containerBuildService = null,
            ProcessCommunicator processCommunicator = null)
        {
            this.backgroundJobClient = backgroundJobClient;
            this.virusScannerClient = localClient;
            this.metadataStorageEngine = metadataStorageEngine;
            this.containerBuildService = containerBuildService;
            this.processCommunicator = processCommunicator;
        }


        public override bool isValidProject(Stream fileStreamData)
        {
            return false;
        }

        public override List<string> getProjectFileExtensionsSupported()
        {
            return new List<string>() { "js" };

        }

        public override async Task DoWork(ProjectContainer programFileContainer)
        {
            var serverUrl = programFileContainer.getProjectStoredServerUrl();

            try
            {
                // Split server url which is format http://url:port to get port and url info
                var uri = new Uri(serverUrl);
                string url = uri.Host;
                int port = uri.Port;

                // Use the url and port to create miniofilestorageengine
                var minioEngine = new MinioProjectStorageEngine(url, port);
                storageEngine = minioEngine;
                // Create the client using existing method
                storageEngine.CreateStorageEngineClient();

                Console.WriteLine($"Processing program file from {url}:{port}");
                Console.WriteLine($"File: {programFileContainer.getProjectName()}");
                Console.WriteLine($"Bucket: {programFileContainer.getBucketName()}");

                // Download the file using the new method in MinioFileStorageEngine
                using (var fileStream = await minioEngine.DownloadProject(
                    programFileContainer.getBucketName(),
                    programFileContainer.getProjectArtifactName()))
                {
                    // Ensure seekable — downstream code resets Position multiple times;
                    // guard against future SDK changes that may return a non-seekable stream.
                    Stream seekable = fileStream.CanSeek
                        ? fileStream
                        : await ToMemoryStreamAsync(fileStream);
                    Console.WriteLine($"Downloaded file size: {seekable.Length} bytes");

                    await ProcessProject(seekable, programFileContainer);
                }

                Console.WriteLine($"Completed processing: {programFileContainer.getProjectName()}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing file: {ex.Message}");
                throw; // Hangfire will retry
            }
        }


        private async Task<ProjectContainer> QuarantineExecutableFile(Stream fileStream, ProjectContainer originalContainer)
        {
            // Reset stream — VirusScanAndExtractMetaData consumed it during extraction
            fileStream.Position = 0;

            // Upload to hazard, keeping the same project name for traceability
            var hazardObjectName = await storageEngine.UploadProject(
                fileStream, "hazard", originalContainer.getProjectName());

            // Remove from original bucket — file no longer belongs in "Projects"
            await storageEngine.DeleteProject(
                originalContainer.getBucketName(),
                originalContainer.getProjectArtifactName());

            // Return a new container reflecting the file's final location
            return new JavaScriptProjectContainer(
                hazardObjectName,
                "hazard",
                originalContainer.getProjectStoredServerUrl());
        }

        //the flow is during doWork when the project files are fetched from the storage engine we have to run the below function
        // in order to ensure that we check for virus scanning and also exract met data, ignore storing of meta data for now
        // use the fucntion in proper place and ensure its used in do work of JSProjectProcessingJobEnque

        public override async Task<(VirusScanResults, ProjectMetaData)> VirusScanAndExtractMetaData(Stream tarStream, ProjectContainer projectContainer)
        {
            //the virsu scan client is an clamAV local virus scanner whicjh is running and hosted locally, how can this be used to conduct an virus/ sanity scan of a js/node js project, the stream data is a tar archve of *.js files and package.json/ other json files , this is an ideation session hence dont make edits and dont make reports

            //perform a simple clamAv check using the stream data for initial virus checks
            //take the stream data which will be  a TAR file , create a local temp dir and extract this tar file
            // start  a child process using process.start where the base directory is the tar file which was extracted
            // the child process will be an npm process and run npm audit and get the final output response from npm audit


            var virusResult = await this.virusScannerClient.ScanFileDataAsync(tarStream);
            if (virusResult != VirusScanResults.CLEAN)
            {
                return (VirusScanResults.VIRUS, new JSProjectMetadata
                {
                    ProjectName = projectContainer.getProjectName()
                });
            }
            tarStream.Position = 0;
            var tempDir = Path.Combine(Path.GetTempPath(), projectContainer.getProjectName());
            Directory.CreateDirectory(tempDir);
            try
            {
                await ExtractTarToTemp(tarStream, tempDir);
                var (virusScanResult, projectMetaData, risk) = await NodeMetaDataExtraction(tempDir);
                if (virusScanResult != VirusScanResults.CLEAN)
                {
                    return (VirusScanResults.QUARANTINE, projectMetaData);
                }
                return (VirusScanResults.CLEAN, projectMetaData);


            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, recursive: true);
                }
            }


        }

        public async Task<VirusScanResults> ExtractTarToTemp(Stream tarStream, string dirPath)
        {
            using var zipStream = new GZipStream(tarStream, CompressionMode.Decompress);
            using var tarReader = new TarReader(zipStream);
            TarEntry entry;
            while ((entry = await tarReader.GetNextEntryAsync()) != null)
            {
                var fullPath = Path.Combine(dirPath, entry.Name);
                if (!fullPath.StartsWith(dirPath))
                {
                    return VirusScanResults.VIRUS;
                }

                if (entry.EntryType == TarEntryType.Directory)
                {
                    Directory.CreateDirectory(fullPath);
                }
                else if (entry.EntryType == TarEntryType.RegularFile)
                {
                    var directory = Path.GetDirectoryName(fullPath);
                    Directory.CreateDirectory(directory);
                    await entry.ExtractToFileAsync(fullPath, overwrite: true);

                }

            }
            return VirusScanResults.CLEAN;
        }

        public async Task<(VirusScanResults, ProjectMetaData, RiskAssessment)> NodeMetaDataExtraction(string nodeProjectPath)
        {
            var comm = this.processCommunicator
                ?? throw new InvalidOperationException("ProcessCommunicator not injected — cannot run npm commands");
            comm.StartProcess(nodeProjectPath);
            comm.StartTransaction();
            var auditData = "";
            var listData = "";
            var configData = "";
            var packData = "";
            // 1. npm audit --json
            comm.ExecuteCommand("npm audit --json",
                (err, outputLogs) =>
                {
                    if (outputLogs.Data != null)
                    {
                        auditData += outputLogs.Data;
                        Console.WriteLine("npm audit output received");
                    }
                },
                (err, errorLogs) =>
                {
                    if (errorLogs.Data != null)
                    {
                        Console.WriteLine($"npm audit error: {errorLogs.Data}");
                    }
                });
            // 2. npm list --json --depth=0
            comm.ExecuteCommand("npm list --json --depth=0",
                (err, outputLogs) =>
                {
                    if (outputLogs.Data != null)
                    {
                        listData += outputLogs.Data;
                        Console.WriteLine("npm list output received");
                    }
                },
                (err, errorLogs) =>
                {
                    if (errorLogs.Data != null)
                    {
                        Console.WriteLine($"npm list error: {errorLogs.Data}");
                    }
                });
            // 3. npm config list --json
            comm.ExecuteCommand("npm config list --json",
                (err, outputLogs) =>
                {
                    if (outputLogs.Data != null)
                    {
                        configData += outputLogs.Data;
                        Console.WriteLine("npm config output received");
                    }
                },
                (err, errorLogs) =>
                {
                    if (errorLogs.Data != null)
                    {
                        Console.WriteLine($"npm config error: {errorLogs.Data}");
                    }
                });
            // 4. npm pack --dry-run --json
            comm.ExecuteCommand("npm pack --dry-run --json",
                (err, outputLogs) =>
                {
                    if (outputLogs.Data != null)
                    {
                        packData += outputLogs.Data;
                        Console.WriteLine("npm pack output received");
                    }
                },
                (err, errorLogs) =>
                {
                    if (errorLogs.Data != null)
                    {
                        Console.WriteLine($"npm pack error: {errorLogs.Data}");
                    }
                });
            // Wait for all commands to complete
            comm.EndTransaction();
            comm.EndProcess();
            if (comm is IDisposable disposableComm)
                disposableComm.Dispose();
            // Parse all collected data
            var metadata = new JSProjectMetadata();
            // Parse audit data
            if (!string.IsNullOrEmpty(auditData))
            {
                var auditResult = JsonSerializer.Deserialize<NpmAuditOutput>(auditData);
                metadata.VulnerabilityCount = auditResult?.Metadata?.Vulnerabilities?.Total ?? 0;
                metadata.CriticalVulnerabilities = auditResult?.Metadata?.Vulnerabilities?.Critical ?? 0;
                metadata.HighVulnerabilities = auditResult?.Metadata?.Vulnerabilities?.High ?? 0;
            }
            // Parse list data
            if (!string.IsNullOrEmpty(listData))
            {
                var listResult = JsonSerializer.Deserialize<NpmListOutput>(listData);
                metadata.ProjectName = listResult?.Name;
                metadata.ProjectVersion = listResult?.Version;
                metadata.Dependencies = listResult?.Dependencies?.Keys.ToList();
                metadata.DependencyCount = listResult?.Dependencies?.Count ?? 0;
            }
            // Parse config data
            if (!string.IsNullOrEmpty(configData))
            {
                var configResult = JsonSerializer.Deserialize<NpmConfigOutput>(configData);
                metadata.NodeVersion = configResult?.NodeVersion;
                metadata.NpmVersion = configResult?.NpmVersion;
            }
            // Parse pack data
            if (!string.IsNullOrEmpty(packData))
            {
                var packResult = JsonSerializer.Deserialize<NpmPackOutput>(packData);
                metadata.PackageSize = packResult?.Size ?? 0;
                metadata.UnpackedSize = packResult?.UnpackedSize ?? 0;
                metadata.FileCount = packResult?.EntryCount ?? 0;
            }

            var packageJsonPath = Path.Combine(nodeProjectPath, "package.json");
            var jsonContent = await File.ReadAllTextAsync(packageJsonPath);

            var packageData = JsonSerializer.Deserialize<PackageJson>(jsonContent);

            metadata.Organization = new PackageJson
            {
                Author = packageData.Author,
                Maintainers = packageData.Maintainers,
                Contributors = packageData.Contributors,
                Repository = packageData.Repository,
                Homepage = packageData.Homepage,
                Bugs = packageData.Bugs
            };

            var risk = AssessProject(metadata);
            var virusResult = VirusScanResults.CLEAN;
            if (risk.Action == "REJECT")
            {
                virusResult = VirusScanResults.VIRUS;
            }
            else if (risk.Action == "QUARANTINE")
            {
                virusResult = VirusScanResults.QUARANTINE;
            }
            return (virusResult, metadata, risk);
        }

        public RiskAssessment AssessProject(JSProjectMetadata metadata)
        {
            var assessment = new RiskAssessment();
            var issues = new List<string>();

            // 1. Critical vulnerabilities
            if (metadata.CriticalVulnerabilities > 0)
            {
                issues.Add($"CRITICAL: {metadata.CriticalVulnerabilities} critical vulnerabilities");
                assessment.RiskLevel = "CRITICAL";
            }

            // 2. High vulnerabilities
            if (metadata.HighVulnerabilities >= 3)
            {
                issues.Add($"HIGH: {metadata.HighVulnerabilities} high-severity vulnerabilities");
                if (assessment.RiskLevel != "CRITICAL")
                    assessment.RiskLevel = "HIGH";
            }

            // 3. Excessive dependencies
            if (metadata.DependencyCount > 500)
            {
                issues.Add($"HIGH: Excessive dependencies ({metadata.DependencyCount})");
                if (assessment.RiskLevel != "CRITICAL")
                    assessment.RiskLevel = "HIGH";
            }

            // 4. Large package size
            if (metadata.PackageSize > 100_000_000)
            {
                issues.Add($"MEDIUM: Large package size ({metadata.PackageSize / 1_000_000}MB)");
                if (assessment.RiskLevel == null)
                    assessment.RiskLevel = "MEDIUM";
            }

            // 5. Outdated Node version
            if (metadata.NodeVersion != null)
            {
                var nodeVersion = Version.Parse(metadata.NodeVersion.TrimStart('v'));
                if (nodeVersion < new Version(14, 0, 0))
                {
                    issues.Add($"MEDIUM: Outdated Node.js ({metadata.NodeVersion})");
                    if (assessment.RiskLevel == null)
                        assessment.RiskLevel = "MEDIUM";
                }
            }

            // Calculate overall risk score
            assessment.RiskScore = CalculateRiskScore(metadata);
            assessment.Issues = issues;
            assessment.Action = DetermineAction(assessment.RiskScore);

            return assessment;
        }

        public string DetermineAction(int riskScore)
        {
            if (riskScore >= 80)
                return "REJECT";           // Critical risk
            else if (riskScore >= 50)
                return "QUARANTINE";       // High risk
            else if (riskScore >= 30)
                return "WARN_USER";        // Medium risk
            else
                return "APPROVE";          // Low risk
        }

        public int CalculateRiskScore(JSProjectMetadata metadata)
        {
            int score = 0;

            // Vulnerability scoring (0-40 points)
            score += metadata.CriticalVulnerabilities * 10;  // 10 points each
            score += metadata.HighVulnerabilities * 5;       // 5 points each
            score += Math.Min(metadata.VulnerabilityCount, 20); // Max 20 points

            // Dependency scoring (0-20 points)
            if (metadata.DependencyCount > 500) score += 15;
            else if (metadata.DependencyCount > 200) score += 10;
            else if (metadata.DependencyCount > 100) score += 5;

            if (metadata.DependencyCount == 0 && metadata.FileCount > 20)
                score += 10; // No deps but many files

            // Size scoring (0-20 points)
            if (metadata.PackageSize > 100_000_000) score += 10;
            if (metadata.UnpackedSize > 500_000_000) score += 10;

            var compressionRatio = (double)metadata.UnpackedSize / metadata.PackageSize;
            if (compressionRatio > 100) score += 15;

            // File count scoring (0-10 points)
            if (metadata.FileCount > 10_000) score += 5;
            if (metadata.FileCount < 5 && metadata.PackageSize > 10_000_000) score += 10;

            // Version scoring (0-10 points)
            if (metadata.NodeVersion != null)
            {
                var nodeVersion = Version.Parse(metadata.NodeVersion.TrimStart('v'));
                if (nodeVersion < new Version(14, 0, 0)) score += 5;
            }

            return Math.Min(score, 100); // Cap at 100
        }


        public override async Task ProcessProject(Stream fileStream, ProjectContainer projectContainer)
        {
            var (virusScanResult, metadata) = await VirusScanAndExtractMetaData(fileStream, projectContainer);
            var executableProjectId = ((JavaScriptProjectContainer)projectContainer).ExecutableProjectId;

            if (virusScanResult != VirusScanResults.CLEAN)
            {
                // Move file: original bucket → hazard; delete from original
                await QuarantineExecutableFile(fileStream, projectContainer);

                if (metadataStorageEngine != null)
                {
                    await metadataStorageEngine.UpdateExecutableProjectStatusAsync(
                        executableProjectId, "quarantined", virusScanResult.ToString());
                    await metadataStorageEngine.SaveMetadataAsync(
                        executableProjectId, (JSProjectMetadata)metadata, new JsMetadataMapper());
                }
                return;
            }

            // CLEAN path: update status, store metadata, then build container
            if (metadataStorageEngine != null)
            {
                await metadataStorageEngine.UpdateExecutableProjectStatusAsync(
                    executableProjectId, "approved", virusScanResult.ToString());
                await metadataStorageEngine.SaveMetadataAsync(
                    executableProjectId, (JSProjectMetadata)metadata, new JsMetadataMapper());
            }

            if (containerBuildService != null)
            {
                var recipe = ProjectContainerRecipeFactory.GetRecipe(projectContainer.getProjectType());
                await containerBuildService.BuildAndStartProjectContainer(fileStream, recipe, projectContainer.getProjectName());
            }
        }

        public override void EnqueJob(ProjectContainer executablFileContainer)
        {
            this.backgroundJobClient.Enqueue(() => this.DoWork(executablFileContainer));
        }

        private static async Task<MemoryStream> ToMemoryStreamAsync(Stream source)
        {
            var ms = new MemoryStream();
            await source.CopyToAsync(ms);
            ms.Position = 0;
            return ms;
        }
    }

}
