namespace Engines.DataBaseStorageEngines.Entities;

public class RiskAssessmentRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ExecutableProjectId { get; set; }
    public ExecutableProject ExecutableProject { get; set; } = null!;
    public required string RiskLevel { get; set; }
    public int RiskScore { get; set; }
    public required string Action { get; set; }
    public string IssuesJson { get; set; } = "[]";
    public DateTime AssessedAt { get; set; } = DateTime.UtcNow;
}
