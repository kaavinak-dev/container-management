namespace Engines.DataBaseStorageEngines.Entities;

public class ProjectRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string ProjectName { get; set; }
    public required string ProjectType { get; set; }
    public DateTime UploadDate { get; set; } = DateTime.UtcNow;
    public required string StorageUrl { get; set; }     // MinIO endpoint URL
    public required string BucketName { get; set; }     // "editor-projects"
    public string Status { get; set; } = "draft";       // draft | deploying
    public ICollection<ExecutableProject> ExecutableProjects { get; set; } = [];
}
