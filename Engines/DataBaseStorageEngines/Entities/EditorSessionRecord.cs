namespace Engines.DataBaseStorageEngines.Entities;

public class EditorSessionRecord
{
    public int Id { get; set; }                               // PK, auto-increment
    public required string ProjectId { get; set; }            // unique, indexed
    public required string ContainerName { get; set; }        // e.g. "editor-abc123"
    public required string WorkspaceVolume { get; set; }      // e.g. "workspace-abc123"
    public required string NpmCacheVolume { get; set; }       // e.g. "npm-cache-abc123-a1b2c3d4"
    public string ContainerIp { get; set; } = string.Empty;
    public string? AgentId { get; set; }                      // [NEW] ID of the EC2 Relay Agent
    public string? ContainerId { get; set; }                  // [NEW] Docker container ID on the host
    public string Status { get; set; } = "Starting";          // Starting | Ready | Stopped | Cleaned
    public DateTime LastActive { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
