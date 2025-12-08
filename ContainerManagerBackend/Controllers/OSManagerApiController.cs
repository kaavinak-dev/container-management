using ContainerManagerBackend.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Engines;
using Engines.FileStorageEngines;
using Engines.FileStorageEngines.Implementations;
using Hangfire;

namespace ContainerManagerBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OSManagerApiController : ControllerBase
    {
        RequestBodyParser _requestParser;
        FileStorageManager exeStorageManager;
        IBackgroundJobClient jobEnqueHelper;
        public OSManagerApiController(RequestBodyParser requestBodyParser, FileStorageManager manager, IBackgroundJobClient jobEnqueHelper)
        {

            _requestParser = requestBodyParser;
            exeStorageManager = manager;
            this.jobEnqueHelper = jobEnqueHelper;
        }


        [HttpGet("/CreateOS")]
        public string CreateOS()
        {
            try
            {
                var orchestrator = _requestParser.GetOSOrchestratorFromRequest(Request);
                var osLakeOrchestrator = _requestParser.GetOSLakeOrchestratorFromRequest(Request);
                orchestrator.CreateOS();
                return new ResponseFormatter().Ok("os created successfully");
            }
            catch (Exception ex)
            {
                return new ResponseFormatter().Error(ex.Message);

            }

        }

        [HttpPost("/UploadExecutable")]
        public async Task<string> UploadExecutable(List<IFormFile> files)
        {
            var file = files.FirstOrDefault();
            //var engine = exeStorageManager.GetEngine();
            //var stream = FileHandler.GetFileStream(file);
            //engine.UploadExecutable(stream);
            var engine = exeStorageManager.GetFileStorageEngine();
            var stream = FileHandler.GetFileStream(file);
            var fileContainer = await engine.UploadRawBinary(stream);
            ExecutableProcessingJobEnque jobEnque = new ExecutableProcessingJobEnque(jobEnqueHelper);
            jobEnque.EnqueJob(fileContainer);

            return new ResponseFormatter().Ok("file uploaded");
        }


    }
}
