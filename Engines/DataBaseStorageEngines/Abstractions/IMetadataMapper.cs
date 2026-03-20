using Engines.FileStorageEngines.Abstractions;

namespace Engines.DataBaseStorageEngines.Abstractions;

/// <summary>
/// Implement one per language/project-type to plug into the storage engine.
/// TDomain = in-memory metadata model (e.g. JSProjectMetadata)
/// TRecord  = EF Core entity   (e.g. JsMetadataRecord)
///
/// Adding a new language (Go, Rust, .NET) means:
///   1. A new *MetadataRecord entity
///   2. A new IMetadataMapper implementation
///   3. DI registration
/// — zero changes to IMetadataStorageEngine or the processing pipeline.
/// </summary>
public interface IMetadataMapper<TDomain, TRecord>
    where TDomain : ProjectMetaData
    where TRecord : class, IProjectForeignKey
{
    string ProjectType { get; }
    TRecord ToRecord(Guid projectId, TDomain domain);
    TDomain ToDomain(TRecord record);
}
