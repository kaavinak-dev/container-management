namespace Engines.DataBaseStorageEngines.Entities;

public class AgentRecord
{
    public int Id { get; set; }
    public required string AgentId { get; set; }     // unique — "local-dev" or EC2 instance ID
    public required string DockerHost { get; set; }  // e.g. "http://172.31.1.10:2375"
    public string? Hostname { get; set; }
    public DateTime LastSeen { get; set; } = DateTime.UtcNow;
}
