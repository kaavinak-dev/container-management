using Engines.DataBaseStorageEngines.Abstractions;

namespace Engines.DataBaseStorageEngines.Entities;

public class JsMetadataRecord : IProjectForeignKey
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public string? ProjectVersion { get; set; }
    public int DependencyCount { get; set; }
    public int VulnerabilityCount { get; set; }
    public int CriticalVulnerabilities { get; set; }
    public int HighVulnerabilities { get; set; }
    public string? NodeVersion { get; set; }
    public string? NpmVersion { get; set; }
    public long PackageSize { get; set; }
    public long UnpackedSize { get; set; }
    public int FileCount { get; set; }
    public ICollection<JsDependencyRecord> Dependencies { get; set; } = [];
    public ICollection<JsVulnerabilityRecord> Vulnerabilities { get; set; } = [];
}
