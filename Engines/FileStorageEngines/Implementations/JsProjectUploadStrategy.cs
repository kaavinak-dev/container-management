using System.Formats.Tar;
using System.IO.Compression;
using System.Text.Json;
using Engines.FileStorageEngines.Abstractions;
using Newtonsoft.Json;
using PeNet;
using PeNet.Header.Pe;

namespace Engines.FileStorageEngines.Implementations
{

    public class JSProjectUploadStrategy : ProjectUploadStrategy
    {

        public JSProjectUploadStrategy(ProjectStorageEngine engine) : base(engine)
        {

        }




        public override async Task<ProjectContainer> UploadProject(Stream zipStream, string bucketName)
        {
            // Generate unique project ID
            var projectId = Guid.NewGuid().ToString("N");
            var tempDir = Path.Combine(Path.GetTempPath(), $"js-project-{projectId}");
            var tarFilePath = Path.Combine(Path.GetTempPath(), $"{projectId}.tar.gz");
            try
            {
                // 1. Extract ZIP to temporary directory
                Directory.CreateDirectory(tempDir);
                await ExtractZipToDirectory(zipStream, tempDir);
                // 2. Validate project structure (check for package.json)
                var packageJsonPath = FindPackageJson(tempDir);
                if (packageJsonPath == null)
                {
                    throw new InvalidOperationException("Invalid JavaScript project: package.json not found");
                }
                // Get project root (directory containing package.json)
                var projectRoot = Path.GetDirectoryName(packageJsonPath);
                var filesToInclude = GetFilteredFiles(projectRoot);
                if (filesToInclude.Count == 0)
                {
                    throw new InvalidOperationException("No valid JavaScript files found in project");
                }
                // 5. Create TAR.GZ archive from filtered files
                await CreateTarGzArchive(projectRoot, filesToInclude, tarFilePath);
                // 6. Upload TAR.GZ to MinIO
                using (var tarStream = File.OpenRead(tarFilePath))
                {
                    var objectKey = $"{projectId}.tar.gz";
                    await storageEngine.UploadProject(tarStream, bucketName, objectKey);
                }
                // 7. Upload metadata as separate JSON file

                // 8. Return project container with storage information
                return new JavaScriptProjectContainer(
                    projectName: $"{projectId}",
                    bucketName: bucketName,
                    serverUrl: storageEngine.GetServerUrl()
                );
            }
            finally
            {
                // Cleanup: Delete temporary directory and tar file
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, recursive: true);
                }
                if (File.Exists(tarFilePath))
                {
                    File.Delete(tarFilePath);
                }
            }
        }
        // Helper method: Extract ZIP to directory
        private async Task ExtractZipToDirectory(Stream zipStream, string targetDir)
        {
            using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Read, leaveOpen: true))
            {
                foreach (var entry in archive.Entries)
                {
                    // Skip directory entries
                    if (string.IsNullOrEmpty(entry.Name))
                        continue;
                    // Construct full path
                    var fullPath = Path.Combine(targetDir, entry.FullName);
                    // Security: Prevent path traversal attacks
                    if (!fullPath.StartsWith(targetDir, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException($"Invalid ZIP entry: {entry.FullName}");
                    }
                    // Create directory if needed
                    var directory = Path.GetDirectoryName(fullPath);
                    Directory.CreateDirectory(directory);
                    // Extract file
                    entry.ExtractToFile(fullPath, overwrite: true);
                }
            }
        }
        // Helper method: Find package.json in extracted directory
        private string FindPackageJson(string rootDir)
        {
            // Search for package.json (could be in root or subdirectory)
            var packageJsonFiles = Directory.GetFiles(rootDir, "package.json", SearchOption.AllDirectories);

            // Return the first one found (prefer root level)
            return packageJsonFiles
                .OrderBy(f => f.Split(Path.DirectorySeparatorChar).Length)
                .FirstOrDefault();
        }


        // Helper method: Get filtered files (exclude node_modules, .git, etc.)
        private List<string> GetFilteredFiles(string projectRoot)
        {
            var excludePatterns = new[]
            {
                "node_modules",
                ".git",
                ".env",
                "dist",
                "build",
                "coverage",
                ".cache",
                ".next",
                ".nuxt",
                "out",
                ".DS_Store"
            };
            var allFiles = Directory.GetFiles(projectRoot, "*", SearchOption.AllDirectories);
            return allFiles
                .Where(file => !excludePatterns.Any(pattern =>
                    file.Contains(Path.DirectorySeparatorChar + pattern + Path.DirectorySeparatorChar) ||
                    file.Contains(Path.DirectorySeparatorChar + pattern)))
                .ToList();
        }
        // Helper method: Create TAR.GZ archive
        private async Task CreateTarGzArchive(string sourceDir, List<string> filesToInclude, string outputPath)
        {
            using (var tarStream = File.Create(outputPath))
            using (var gzipStream = new GZipStream(tarStream, CompressionMode.Compress))
            using (var tarWriter = new TarWriter(gzipStream))
            {
                foreach (var filePath in filesToInclude)
                {
                    // Get relative path for tar entry
                    var relativePath = Path.GetRelativePath(sourceDir, filePath);

                    // Create tar entry
                    await tarWriter.WriteEntryAsync(filePath, relativePath);
                }
            }
        }
        // Helper method: Create metadata JSON
        private string CreateMetadataJson(string projectId, Dictionary<string, object> packageData, List<string> files)
        {
            var metadata = new
            {
                projectId = projectId,
                uploadDate = DateTime.UtcNow,
                projectName = packageData.ContainsKey("name") ? packageData["name"].ToString() : "unknown",
                version = packageData.ContainsKey("version") ? packageData["version"].ToString() : "0.0.0",
                totalFiles = files.Count,
                totalSize = files.Sum(f => new FileInfo(f).Length),
                fileTypes = files
                    .GroupBy(f => Path.GetExtension(f).ToLowerInvariant())
                    .ToDictionary(g => g.Key, g => g.Count()),
                dependencies = packageData.ContainsKey("dependencies") ? packageData["dependencies"] : null,
                devDependencies = packageData.ContainsKey("devDependencies") ? packageData["devDependencies"] : null
            };
            return System.Text.Json.JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true });
        }




    }

}
