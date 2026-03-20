namespace Engines.DataBaseStorageEngines.Entities;

public class JsDependencyRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid JsMetadataId { get; set; }
    public JsMetadataRecord JsMetadata { get; set; } = null!;
    public required string PackageName { get; set; }
    public string? Version { get; set; }
}
