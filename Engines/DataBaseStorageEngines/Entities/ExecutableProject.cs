namespace Engines.DataBaseStorageEngines.Entities;

public class ExecutableProject
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SourceProjectId { get; set; }
    public required string ArtifactBucket { get; set; }
    public required string ArtifactName { get; set; }
    public required string StorageUrl { get; set; }
    public string Status { get; set; } = "pending"; // pending | approved | quarantined | rejected
    public string? VirusScanResult { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? ContainerId { get; set; }
    public string? DockerNetworkId { get; set; }
    public string? DockerNetworkName { get; set; }
    public ProjectRecord SourceProject { get; set; } = null!;
    public ICollection<RiskAssessmentRecord> RiskAssessments { get; set; } = [];
}
