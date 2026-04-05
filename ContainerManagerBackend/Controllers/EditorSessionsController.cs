using Engines.FileStorageEngines;
using Microsoft.AspNetCore.Mvc;

namespace ContainerManagerBackend.Controllers;

[Route("api/editor-sessions")]
[ApiController]
public class EditorSessionsController : ControllerBase
{
    private readonly EditorContainerService _editorService;

    public EditorSessionsController(EditorContainerService editorService)
        => _editorService = editorService;

    // POST /api/editor-sessions
    // Body: { "projectId": "abc123" }
    // Starts the editor container (or returns existing session) and polls until LSP is ready.
    [HttpPost]
    public async Task<IActionResult> StartSession([FromBody] StartSessionRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.ProjectId))
            return BadRequest(new { error = "projectId is required" });

        var (containerIp, alreadyRunning) = await _editorService.StartEditorContainerAsync(req.ProjectId);

        if (!alreadyRunning)
            await _editorService.PollUntilReadyAsync(req.ProjectId, containerIp);

        var status = await _editorService.GetEditorContainerStatusAsync(req.ProjectId);
        return Ok(new
        {
            containerIp = status!.ContainerIp,
            fileApiPort = status.FileApiPort,
            ptyPort = status.PtyPort,
            status = status.Status.ToLowerInvariant(),
        });
    }

    // DELETE /api/editor-sessions/{projectId}
    [HttpDelete("{projectId}")]
    public async Task<IActionResult> StopSession(string projectId)
    {
        await _editorService.StopEditorContainerAsync(projectId);
        return NoContent();
    }

    // GET /api/editor-sessions/{projectId}
    [HttpGet("{projectId}")]
    public async Task<IActionResult> GetSession(string projectId)
    {
        var status = await _editorService.GetEditorContainerStatusAsync(projectId);
        if (status is null)
            return NotFound(new { error = "No editor session found for this project" });

        return Ok(new
        {
            containerIp = status.ContainerIp,
            fileApiPort = status.FileApiPort,
            ptyPort = status.PtyPort,
            status = status.Status.ToLowerInvariant(),
        });
    }

    // PUT /api/editor-sessions/{projectId}/activity
    // Called by the BFF as a heartbeat to keep the session alive.
    [HttpPut("{projectId}/activity")]
    public async Task<IActionResult> Heartbeat(string projectId)
    {
        await _editorService.UpdateLastActiveAsync(projectId);
        return Ok();
    }
}

public record StartSessionRequest(string ProjectId);
