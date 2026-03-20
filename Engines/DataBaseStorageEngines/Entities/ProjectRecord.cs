namespace Engines.DataBaseStorageEngines.Entities;

public class ProjectRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string ProjectName { get; set; }
    public required string ProjectType { get; set; }
    public DateTime UploadDate { get; set; } = DateTime.UtcNow;
    public required string StorageUrl { get; set; }
    public required string BucketName { get; set; }
    public string? VirusScanResult { get; set; }
    public ICollection<RiskAssessmentRecord> RiskAssessments { get; set; } = [];
}
