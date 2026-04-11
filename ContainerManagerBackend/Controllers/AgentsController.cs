using Engines.DataBaseStorageEngines;
using Engines.DataBaseStorageEngines.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContainerManagerBackend.Controllers;

[ApiController]
[Route("api/agents")]
public class AgentsController : ControllerBase
{
    private readonly ProjectDbContext _db;

    public AgentsController(ProjectDbContext db) => _db = db;

    // POST /api/agents/register
    // Called by each relay agent on startup to record its agentId and Docker daemon address.
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] AgentRegistrationRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.AgentId) || string.IsNullOrWhiteSpace(req.DockerHost))
            return BadRequest(new { error = "agentId and dockerHost are required" });
        var existing = await _db.AgentRecords
            .FirstOrDefaultAsync(a => a.AgentId == req.AgentId);

        if (existing is null)
        {
            _db.AgentRecords.Add(new AgentRecord
            {
                AgentId = req.AgentId,
                DockerHost = req.DockerHost,
                Hostname = req.Hostname,
                LastSeen = DateTime.UtcNow,
            });
        }
        else
        {
            existing.DockerHost = req.DockerHost;
            existing.Hostname = req.Hostname;
            existing.LastSeen = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return Ok();
    }
}

public record AgentRegistrationRequest(string AgentId, string DockerHost, string? Hostname);
