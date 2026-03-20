using Engines.DataBaseStorageEngines.Abstractions;
using Engines.DataBaseStorageEngines.Entities;
using Engines.FileStorageEngines.Implementations;

namespace Engines.DataBaseStorageEngines.Implementations.Mappers;

public class JsMetadataMapper : IMetadataMapper<JSProjectMetadata, JsMetadataRecord>
{
    public string ProjectType => "js";

    public JsMetadataRecord ToRecord(Guid projectId, JSProjectMetadata d) => new()
    {
        ProjectId = projectId,
        ProjectVersion = d.ProjectVersion,
        DependencyCount = d.DependencyCount,
        VulnerabilityCount = d.VulnerabilityCount,
        CriticalVulnerabilities = d.CriticalVulnerabilities,
        HighVulnerabilities = d.HighVulnerabilities,
        NodeVersion = d.NodeVersion,
        NpmVersion = d.NpmVersion,
        PackageSize = d.PackageSize,
        UnpackedSize = d.UnpackedSize,
        FileCount = d.FileCount,
        Dependencies = (d.Dependencies ?? [])
            .Select(name => new JsDependencyRecord { PackageName = name })
            .ToList(),
    };

    public JSProjectMetadata ToDomain(JsMetadataRecord r) => new()
    {
        ProjectVersion = r.ProjectVersion,
        DependencyCount = r.DependencyCount,
        VulnerabilityCount = r.VulnerabilityCount,
        CriticalVulnerabilities = r.CriticalVulnerabilities,
        HighVulnerabilities = r.HighVulnerabilities,
        NodeVersion = r.NodeVersion,
        NpmVersion = r.NpmVersion,
        PackageSize = r.PackageSize,
        UnpackedSize = r.UnpackedSize,
        FileCount = r.FileCount,
        Dependencies = r.Dependencies.Select(d => d.PackageName).ToList(),
    };
}
