using Engines.DeploymentTracking;
using Microsoft.AspNetCore.Mvc;

namespace ContainerManagerBackend.Controllers;

[Route("api/deployments")]
[ApiController]
public class DeploymentStatusController : ControllerBase
{
    private readonly IDeploymentProgressTracker _tracker;

    public DeploymentStatusController(IDeploymentProgressTracker tracker)
        => _tracker = tracker;

    // GET /api/deployments/{executableProjectId}/steps
    [HttpGet("{executableProjectId:guid}/steps")]
    public async Task<IActionResult> GetSteps(Guid executableProjectId)
    {
        var steps = await _tracker.GetStepsAsync(executableProjectId);
        if (steps is null)
            return NotFound(new { error = "No deployment status found for this id" });

        return Ok(steps);
    }
}
