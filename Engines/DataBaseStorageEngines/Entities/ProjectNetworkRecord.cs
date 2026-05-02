namespace Engines.DataBaseStorageEngines.Entities;

public class ProjectNetworkRecord
{
    public int Id { get; set; }
    public required string ProjectId { get; set; }
    public required string NetworkDockerId { get; set; }
    public required string NetworkName { get; set; }
    public required string AgentId { get; set; }
    public string Status { get; set; } = "Active";                 // Active | TearingDown | Removed
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastActivity { get; set; } = DateTime.UtcNow;

    public ICollection<ProjectResourceRecord> Resources { get; set; } = new List<ProjectResourceRecord>();
    public ICollection<EditorSessionRecord> EditorSessions { get; set; } = new List<EditorSessionRecord>();
}
