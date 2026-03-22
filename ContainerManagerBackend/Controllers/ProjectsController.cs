using Engines.DataBaseStorageEngines.Abstractions;
using Engines.DataBaseStorageEngines.Entities;
using Engines.FileStorageEngines;
using Engines.FileStorageEngines.Implementations;
using Hangfire;
using Microsoft.AspNetCore.Mvc;

namespace ContainerManagerBackend.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ProjectsController : ControllerBase
{
    readonly IMetadataStorageEngine _db;
    readonly ProjectStorageManager _storage;
    readonly IBackgroundJobClient _jobClient;

    public ProjectsController(IMetadataStorageEngine db, ProjectStorageManager storage, IBackgroundJobClient jobClient)
        => (_db, _storage, _jobClient) = (db, storage, jobClient);

    // POST /api/projects
    // Body: { projectName, projectType }
    // Creates ProjectRecord in DB — BFF uploads template files to MinIO separately using the returned projectId
    [HttpPost]
    public async Task<IActionResult> CreateProject([FromBody] CreateProjectRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.ProjectName))
            return BadRequest(new { error = "projectName is required" });

        var engine = _storage.GetFileStorageEngine();
        var record = new ProjectRecord
        {
            ProjectName = req.ProjectName,
            ProjectType = req.ProjectType ?? "js",
            StorageUrl = $"http://{engine._engineUrl}:{engine._enginePort}",
            BucketName = "editor-projects",
            Status = "draft",
        };
        var id = await _db.SaveProjectAsync(record);
        return Created($"/api/projects/{id}", new {
            projectId = id,
            projectName = record.ProjectName,
            projectType = record.ProjectType,
            status = record.Status,
            storageUrl = record.StorageUrl,
        });
    }

    // GET /api/projects
    [HttpGet]
    public async Task<IActionResult> ListProjects()
    {
        var projects = await _db.GetAllProjectsAsync();
        return Ok(projects.Select(p => new {
            projectId = p.Id, projectName = p.ProjectName,
            projectType = p.ProjectType, status = p.Status, createdAt = p.UploadDate,
            storageUrl = p.StorageUrl,
        }));
    }

    // GET /api/projects/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetProject(Guid id)
    {
        var p = await _db.GetProjectAsync(id);
        if (p is null) return NotFound(new { error = "Project not found" });
        return Ok(new {
            projectId = p.Id, projectName = p.ProjectName,
            projectType = p.ProjectType, status = p.Status, createdAt = p.UploadDate,
            storageUrl = p.StorageUrl,
        });
    }

    // DELETE /api/projects/{id}
    // Deletes DB record only — BFF is responsible for cleaning up MinIO editor-projects files
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteProject(Guid id)
    {
        var p = await _db.GetProjectAsync(id);
        if (p is null) return NotFound(new { error = "Project not found" });
        await _db.DeleteProjectAsync(id);
        return NoContent();
    }

    // POST /api/projects/{id}/deploy
    // Accepts a .zip of source files from BFF.
    // Stores artifact in executable-projects bucket, creates ExecutableProject record,
    // enqueues Hangfire scan+build job.
    [HttpPost("{id:guid}/deploy")]
    public async Task<IActionResult> Deploy(Guid id, List<IFormFile> files)
    {
        var project = await _db.GetProjectAsync(id);
        if (project is null) return NotFound(new { error = "Project not found" });

        var zipFile = files.FirstOrDefault();
        if (zipFile is null || !zipFile.FileName.EndsWith(".zip"))
            return BadRequest(new { error = "A .zip file is required" });

        var engine = _storage.GetFileStorageEngine();
        var storageUrl = $"http://{engine._engineUrl}:{engine._enginePort}";

        using var zipStream = zipFile.OpenReadStream();
        var uploadStrategy = new JSProjectUploadStrategy(engine);
        var fileContainer = await uploadStrategy.UploadProject(zipStream, "executable-projects");

        var ep = new ExecutableProject
        {
            SourceProjectId = id,
            ArtifactBucket = fileContainer.getBucketName(),
            ArtifactName = fileContainer.getProjectArtifactName(),
            StorageUrl = storageUrl,
            Status = "pending",
        };
        var epId = await _db.SaveExecutableProjectAsync(ep);

        fileContainer.ExecutableProjectId = epId;
        ProjectProcessingJobEnqueHelper.EnqueJob(_jobClient, fileContainer);

        return Accepted(new { executableProjectId = epId });
    }
}

public record CreateProjectRequest(string ProjectName, string ProjectType = "js");
