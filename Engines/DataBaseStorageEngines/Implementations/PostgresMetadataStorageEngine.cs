using System.Text.Json;
using Engines.DataBaseStorageEngines.Abstractions;
using Engines.DataBaseStorageEngines.Entities;
using Engines.FileStorageEngines.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Engines.DataBaseStorageEngines.Implementations;

public class PostgresMetadataStorageEngine(ProjectDbContext db) : IMetadataStorageEngine, IDatabaseEngine
{
    public async Task MigrateAsync() => await db.Database.MigrateAsync();

    public async Task<bool> IsHealthyAsync()
    {
        try { return await db.Database.CanConnectAsync(); }
        catch { return false; }
    }

    public async Task<Guid> SaveProjectAsync(ProjectRecord project)
    {
        db.Projects.Add(project);
        await db.SaveChangesAsync();
        return project.Id;
    }

    public async Task SaveMetadataAsync<TDomain, TRecord>(
        Guid projectId,
        TDomain metadata,
        IMetadataMapper<TDomain, TRecord> mapper)
        where TDomain : ProjectMetaData
        where TRecord : class, IProjectForeignKey
    {
        var record = mapper.ToRecord(projectId, metadata);
        db.Set<TRecord>().Add(record);
        await db.SaveChangesAsync();
    }

    public async Task SaveRiskAssessmentAsync(Guid projectId, RiskAssessmentRecord assessment)
    {
        assessment.ProjectId = projectId;
        db.RiskAssessments.Add(assessment);
        await db.SaveChangesAsync();
    }

    public async Task<ProjectRecord?> GetProjectAsync(Guid projectId)
        => await db.Projects.Include(p => p.RiskAssessments).FirstOrDefaultAsync(p => p.Id == projectId);

    public async Task<TDomain?> GetMetadataAsync<TDomain, TRecord>(
        Guid projectId,
        IMetadataMapper<TDomain, TRecord> mapper)
        where TDomain : ProjectMetaData
        where TRecord : class, IProjectForeignKey
    {
        var record = await db.Set<TRecord>().FirstOrDefaultAsync(r => r.ProjectId == projectId);
        return record is null ? null : mapper.ToDomain(record);
    }
}
