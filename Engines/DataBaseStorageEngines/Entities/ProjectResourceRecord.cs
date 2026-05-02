namespace Engines.DataBaseStorageEngines.Entities;

public class ProjectResourceRecord
{
    public int Id { get; set; }
    public required string ProjectId { get; set; }
    public int NetworkRecordId { get; set; }
    public required string ResourceType { get; set; }              // MySQL | Redis | MongoDB | PostgreSQL
    public string ContainerDockerId { get; set; } = "";      // filled in by worker after container is created
    public required string ContainerName { get; set; }
    public required string Alias { get; set; }                     // db | cache | mongo | pg
    public required string ImageTag { get; set; }                  // mysql:8.0 | redis:7-alpine | etc.
    public required string AgentId { get; set; }
    public string Status { get; set; } = "Provisioning";          // Provisioning | Ready | Stopped | Failed
    public string? EnvironmentJson { get; set; }                   // JSON blob of env vars passed to container
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastHealthCheck { get; set; } = DateTime.UtcNow;

    public ProjectNetworkRecord NetworkRecord { get; set; } = null!;
}
