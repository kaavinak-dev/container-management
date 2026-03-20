using ContainerManagerBackend.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Engines;
using Engines.FileStorageEngines;
using Engines.FileStorageEngines.Implementations;
using Hangfire;
using System.Formats.Tar;
using System.IO.Compression;
namespace ContainerManagerBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OSManagerApiController : ControllerBase
    {
        RequestBodyParser _requestParser;
        ProjectStorageManager projectStorageManager;
        IBackgroundJobClient jobEnqueHelper;
        public OSManagerApiController(RequestBodyParser requestBodyParser, ProjectStorageManager manager, IBackgroundJobClient jobEnqueHelper)
        {

            _requestParser = requestBodyParser;
            projectStorageManager = manager;
            this.jobEnqueHelper = jobEnqueHelper;
        }


        [HttpGet("/CreateOS")]
        public string CreateOS()
        {
            try
            {
                var orchestrator = _requestParser.GetOSOrchestratorFromRequest(Request);
                var osLakeOrchestrator = _requestParser.GetOSLakeConnectorFromRequest(Request);
                orchestrator.CreateOS();
                return new ResponseFormatter().Ok("os created successfully");
            }
            catch (Exception ex)
            {
                return new ResponseFormatter().Error(ex.Message);

            }

        }

        [HttpPost("/UploadJS")]
        public async Task<string> UploadJS(List<IFormFile> files)
        {
            var zipFile = files.FirstOrDefault();
            if (zipFile == null)
            {
                throw new Exception("no zip file provided");
            }
            if (!zipFile.FileName.Contains(".zip"))
            {
                throw new Exception("invalid file format, not a zip file");
            }

            // get the uploaded zip file and pass it into UploadProject function
            var engine = projectStorageManager.GetFileStorageEngine();
            using var zipStream = zipFile.OpenReadStream();
            var uploadStrategy = new JSProjectUploadStrategy(engine);
            var fileContainer = await uploadStrategy.UploadProject(zipStream, "Projects");
            //ProgramFileProcessingJobEnqueHelper.EnqueJob(jobEnqueHelper, fileContainer);
            ProjectProcessingJobEnqueHelper.EnqueJob(jobEnqueHelper, fileContainer);

            return new ResponseFormatter().Ok("file uploaded");
        }

        [HttpGet("/TestNodeUpload")]
        public async Task<string> TestNodeUpload()
        {
            try
            {
                // 1. Locate the test project on disk
                var testProjectDir = Path.Combine(
                    AppContext.BaseDirectory, "..", "..", "..", "..",
                    "TestProjects", "SimpleNodeApp");

                testProjectDir = Path.GetFullPath(testProjectDir);

                if (!Directory.Exists(testProjectDir))
                {
                    return new ResponseFormatter().Error(
                        $"Test project not found at: {testProjectDir}");
                }

                // 2. Create a .tar.gz stream in memory (simulates user upload)
                var tarGzStream = CreateTarGzFromDirectory(testProjectDir);

                // 3. Upload through the same strategy the real endpoint uses
                var engine = projectStorageManager.GetFileStorageEngine();
                var uploadStrategy = new JSProjectUploadStrategy(engine);
                var fileContainer = await uploadStrategy.UploadProject(tarGzStream, "Projects");

                // 4. Enqueue the Hangfire background job (virus scan → metadata → container build)
                ProjectProcessingJobEnqueHelper.EnqueJob(jobEnqueHelper, fileContainer);

                return new ResponseFormatter().Ok(
                    $"Test project uploaded and job enqueued. Project: {fileContainer.getProjectName()}");
            }
            catch (Exception ex)
            {
                return new ResponseFormatter().Error($"TestNodeUpload failed: {ex.Message}");
            }
        }

        private static MemoryStream CreateTarGzFromDirectory(string sourceDir)
        {
            var output = new MemoryStream();

            using (var gzipStream = new GZipStream(output, CompressionLevel.Fastest, true))
            {
                using var tarWriter = new System.Formats.Tar.TarWriter(gzipStream, System.Formats.Tar.TarEntryFormat.Pax, true);
                
                // Need to ensure we create directory entries in the tar
                var directoryPaths = Directory.EnumerateDirectories(sourceDir, "*", SearchOption.AllDirectories);
                foreach (var dirPath in directoryPaths)
                {
                    var relativePath = Path.GetRelativePath(sourceDir, dirPath).Replace('\\', '/');
                    tarWriter.WriteEntry(new System.Formats.Tar.PaxTarEntry(System.Formats.Tar.TarEntryType.Directory, relativePath + "/"));
                }
                
                var filePaths = Directory.EnumerateFiles(sourceDir, "*", SearchOption.AllDirectories);
                foreach (var filePath in filePaths)
                {
                    var relativePath = Path.GetRelativePath(sourceDir, filePath).Replace('\\', '/');
                    tarWriter.WriteEntry(filePath, relativePath);
                }
            }

            output.Position = 0;
            return output;
        }
    }
}

