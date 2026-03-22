using Engines.DataBaseStorageEngines.Entities;
using Microsoft.EntityFrameworkCore;

namespace Engines.DataBaseStorageEngines;

public class ProjectDbContext(DbContextOptions<ProjectDbContext> options) : DbContext(options)
{
    public DbSet<ProjectRecord> Projects => Set<ProjectRecord>();
    public DbSet<ExecutableProject> ExecutableProjects => Set<ExecutableProject>();
    public DbSet<RiskAssessmentRecord> RiskAssessments => Set<RiskAssessmentRecord>();
    public DbSet<JsMetadataRecord> JsMetadata => Set<JsMetadataRecord>();
    public DbSet<JsDependencyRecord> JsDependencies => Set<JsDependencyRecord>();
    public DbSet<JsVulnerabilityRecord> JsVulnerabilities => Set<JsVulnerabilityRecord>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<ProjectRecord>().ToTable("projects")
            .HasKey(p => p.Id);

        b.Entity<ExecutableProject>().ToTable("executable_projects")
            .HasKey(ep => ep.Id);
        b.Entity<ExecutableProject>()
            .HasOne(ep => ep.SourceProject)
            .WithMany(p => p.ExecutableProjects)
            .HasForeignKey(ep => ep.SourceProjectId);

        b.Entity<RiskAssessmentRecord>().ToTable("risk_assessments")
            .HasKey(r => r.Id);
        b.Entity<RiskAssessmentRecord>()
            .HasOne(r => r.ExecutableProject)
            .WithMany(ep => ep.RiskAssessments)
            .HasForeignKey(r => r.ExecutableProjectId);

        b.Entity<JsMetadataRecord>().ToTable("js_project_metadata")
            .HasKey(m => m.Id);
        // ProjectId satisfies IProjectForeignKey; EF FK target is ExecutableProject, not ProjectRecord
        b.Entity<JsMetadataRecord>()
            .HasOne<ExecutableProject>()
            .WithMany()
            .HasForeignKey(m => m.ProjectId);

        b.Entity<JsDependencyRecord>().ToTable("js_dependencies")
            .HasKey(d => d.Id);
        b.Entity<JsDependencyRecord>()
            .HasOne(d => d.JsMetadata)
            .WithMany(m => m.Dependencies)
            .HasForeignKey(d => d.JsMetadataId);

        b.Entity<JsVulnerabilityRecord>().ToTable("js_vulnerabilities")
            .HasKey(v => v.Id);
        b.Entity<JsVulnerabilityRecord>()
            .HasOne(v => v.JsMetadata)
            .WithMany(m => m.Vulnerabilities)
            .HasForeignKey(v => v.JsMetadataId);
    }
}
