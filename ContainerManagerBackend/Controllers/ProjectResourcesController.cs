using Engines.FileStorageEngines;
using Engines.FileStorageEngines.Resources;
using Microsoft.AspNetCore.Mvc;

namespace ContainerManagerBackend.Controllers;

[Route("api/projects/{projectId}/resources")]
[ApiController]
public class ProjectResourcesController : ControllerBase
{
    private readonly ResourceProvisionerService _provisioner;

    public ProjectResourcesController(ResourceProvisionerService provisioner)
    {
        _provisioner = provisioner;
    }

    // POST /api/projects/{projectId}/resources
    // Body: { "resourceType": "MySQL" }
    [HttpPost]
    public async Task<IActionResult> AddResource(string projectId, [FromBody] AddResourceRequest req)
    {
        if (!Enum.TryParse<ResourceType>(req.ResourceType, ignoreCase: true, out var resourceType))
            return BadRequest(new { error = $"Unsupported resource type '{req.ResourceType}'. Supported: {string.Join(", ", Enum.GetNames<ResourceType>())}" });

        try
        {
            var record = await _provisioner.ProvisionResourceAsync(projectId, resourceType);

            // Returns 202: the record is persisted and the provisioning job is enqueued.
            // Clients should poll GET ./{resourceId} until status transitions to "ready" or "failed".
            return StatusCode(202, new
            {
                id            = record.Id,
                resourceType  = record.ResourceType,
                alias         = record.Alias,
                status        = record.Status.ToLowerInvariant(),   // "provisioning"
                imageTag      = record.ImageTag,
                containerName = record.ContainerName,
                createdAt     = record.CreatedAt,
            });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    // GET /api/projects/{projectId}/resources
    [HttpGet]
    public async Task<IActionResult> ListResources(string projectId)
    {
        var resources = await _provisioner.ListResourcesAsync(projectId);
        return Ok(resources.Select(r => new
        {
            id = r.Id,
            resourceType = r.ResourceType,
            alias = r.Alias,
            status = r.Status.ToLowerInvariant(),
            imageTag = r.ImageTag,
            containerName = r.ContainerName,
            createdAt = r.CreatedAt,
        }));
    }

    // GET /api/projects/{projectId}/resources/{resourceId}
    [HttpGet("{resourceId:int}")]
    public async Task<IActionResult> GetResource(string projectId, int resourceId)
    {
        var status = await _provisioner.GetResourceStatusAsync(projectId, resourceId);
        if (status is null)
            return NotFound(new { error = "Resource not found" });

        return Ok(new
        {
            id = status.Id,
            resourceType = status.ResourceType,
            alias = status.Alias,
            status = status.Status.ToLowerInvariant(),
            imageTag = status.ImageTag,
            containerName = status.ContainerName,
            createdAt = status.CreatedAt,
            liveState = status.LiveState,
        });
    }

    // DELETE /api/projects/{projectId}/resources/{resourceId}
    [HttpDelete("{resourceId:int}")]
    public async Task<IActionResult> RemoveResource(string projectId, int resourceId)
    {
        try
        {
            await _provisioner.DeprovisionResourceAsync(projectId, resourceId);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }
}

// Separate route for the catalog — not scoped to a project
[Route("api/resource-catalog")]
[ApiController]
public class ResourceCatalogController : ControllerBase
{
    // GET /api/resource-catalog
    [HttpGet]
    public IActionResult GetCatalog()
    {
        return Ok(ResourceCatalog.SupportedResources.Select(r => new
        {
            type = r.Type.ToString(),
            displayName = r.DisplayName,
            defaultAlias = r.DefaultAlias,
            image = r.Image,
            defaultPort = r.DefaultPort,
        }));
    }
}

public record AddResourceRequest(string ResourceType);
