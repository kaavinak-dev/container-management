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
        Guid executableProjectId,
        TDomain metadata,
        IMetadataMapper<TDomain, TRecord> mapper)
        where TDomain : ProjectMetaData
        where TRecord : class, IProjectForeignKey
    {
        var record = mapper.ToRecord(executableProjectId, metadata);
        db.Set<TRecord>().Add(record);
        await db.SaveChangesAsync();
    }

    public async Task SaveRiskAssessmentAsync(Guid executableProjectId, RiskAssessmentRecord assessment)
    {
        assessment.ExecutableProjectId = executableProjectId;
        db.RiskAssessments.Add(assessment);
        await db.SaveChangesAsync();
    }

    public async Task<ProjectRecord?> GetProjectAsync(Guid projectId)
        => await db.Projects.Include(p => p.ExecutableProjects).FirstOrDefaultAsync(p => p.Id == projectId);

    public async Task<IList<ProjectRecord>> GetAllProjectsAsync()
        => await db.Projects.OrderByDescending(p => p.UploadDate).ToListAsync();

    public async Task DeleteProjectAsync(Guid projectId)
    {
        var project = await db.Projects.FindAsync(projectId);
        if (project != null) { db.Projects.Remove(project); await db.SaveChangesAsync(); }
    }

    public async Task<Guid> SaveExecutableProjectAsync(ExecutableProject ep)
    {
        db.ExecutableProjects.Add(ep);
        await db.SaveChangesAsync();
        return ep.Id;
    }

    public async Task UpdateExecutableProjectStatusAsync(Guid executableProjectId, string status, string? virusScanResult)
    {
        var ep = await db.ExecutableProjects.FindAsync(executableProjectId);
        if (ep != null) { ep.Status = status; ep.VirusScanResult = virusScanResult; await db.SaveChangesAsync(); }
    }

    public async Task<TDomain?> GetMetadataAsync<TDomain, TRecord>(
        Guid executableProjectId,
        IMetadataMapper<TDomain, TRecord> mapper)
        where TDomain : ProjectMetaData
        where TRecord : class, IProjectForeignKey
    {
        var record = await db.Set<TRecord>().FirstOrDefaultAsync(r => r.ProjectId == executableProjectId);
        return record is null ? null : mapper.ToDomain(record);
    }
}
