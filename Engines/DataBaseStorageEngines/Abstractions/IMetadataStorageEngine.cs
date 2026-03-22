using Engines.DataBaseStorageEngines.Entities;
using Engines.FileStorageEngines.Abstractions;

namespace Engines.DataBaseStorageEngines.Abstractions;

public interface IMetadataStorageEngine
{
    Task<Guid> SaveProjectAsync(ProjectRecord project);
    Task SaveMetadataAsync<TDomain, TRecord>(Guid executableProjectId, TDomain metadata, IMetadataMapper<TDomain, TRecord> mapper)
        where TDomain : ProjectMetaData
        where TRecord : class, IProjectForeignKey;
    Task SaveRiskAssessmentAsync(Guid executableProjectId, RiskAssessmentRecord assessment);
    Task<ProjectRecord?> GetProjectAsync(Guid projectId);
    Task<IList<ProjectRecord>> GetAllProjectsAsync();
    Task DeleteProjectAsync(Guid projectId);
    Task<Guid> SaveExecutableProjectAsync(ExecutableProject executableProject);
    Task UpdateExecutableProjectStatusAsync(Guid executableProjectId, string status, string? virusScanResult);
    Task UpdateContainerInfoAsync(Guid executableProjectId, string containerId, string networkId, string networkName);
    Task<TDomain?> GetMetadataAsync<TDomain, TRecord>(Guid executableProjectId, IMetadataMapper<TDomain, TRecord> mapper)
        where TDomain : ProjectMetaData
        where TRecord : class, IProjectForeignKey;
}
