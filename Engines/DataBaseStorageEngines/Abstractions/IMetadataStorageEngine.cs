using Engines.DataBaseStorageEngines.Entities;
using Engines.FileStorageEngines.Abstractions;

namespace Engines.DataBaseStorageEngines.Abstractions;

public interface IMetadataStorageEngine
{
    Task<Guid> SaveProjectAsync(ProjectRecord project);
    Task SaveMetadataAsync<TDomain, TRecord>(Guid projectId, TDomain metadata, IMetadataMapper<TDomain, TRecord> mapper)
        where TDomain : ProjectMetaData
        where TRecord : class, IProjectForeignKey;
    Task SaveRiskAssessmentAsync(Guid projectId, RiskAssessmentRecord assessment);
    Task<ProjectRecord?> GetProjectAsync(Guid projectId);
    Task<TDomain?> GetMetadataAsync<TDomain, TRecord>(Guid projectId, IMetadataMapper<TDomain, TRecord> mapper)
        where TDomain : ProjectMetaData
        where TRecord : class, IProjectForeignKey;
}
