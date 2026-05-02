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
    public DbSet<EditorSessionRecord> EditorSessions => Set<EditorSessionRecord>();
    public DbSet<AgentRecord> AgentRecords => Set<AgentRecord>();
    public DbSet<ProjectNetworkRecord> ProjectNetworks => Set<ProjectNetworkRecord>();
    public DbSet<ProjectResourceRecord> ProjectResources => Set<ProjectResourceRecord>();

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

        b.Entity<EditorSessionRecord>().ToTable("editor_sessions")
            .HasKey(e => e.Id);
        b.Entity<EditorSessionRecord>()
            .HasIndex(e => e.ProjectId)
            .IsUnique();
        b.Entity<EditorSessionRecord>()
            .Property(e => e.Id)
            .ValueGeneratedOnAdd();
        b.Entity<EditorSessionRecord>()
            .HasOne(e => e.NetworkRecord)
            .WithMany(n => n.EditorSessions)
            .HasForeignKey(e => e.NetworkRecordId)
            .OnDelete(DeleteBehavior.SetNull);

        b.Entity<AgentRecord>().ToTable("agent_records")
            .HasKey(a => a.Id);
        b.Entity<AgentRecord>()
            .HasIndex(a => a.AgentId)
            .IsUnique();
        b.Entity<AgentRecord>()
            .Property(a => a.Id)
            .ValueGeneratedOnAdd();

        // ── Project Fabric (PRS) ──────────────────────────────────
        b.Entity<ProjectNetworkRecord>().ToTable("project_networks")
            .HasKey(n => n.Id);
        b.Entity<ProjectNetworkRecord>()
            .HasIndex(n => n.ProjectId)
            .IsUnique();
        b.Entity<ProjectNetworkRecord>()
            .Property(n => n.Id)
            .ValueGeneratedOnAdd();

        b.Entity<ProjectResourceRecord>().ToTable("project_resources")
            .HasKey(r => r.Id);
        b.Entity<ProjectResourceRecord>()
            .HasIndex(r => r.ProjectId);
        b.Entity<ProjectResourceRecord>()
            .Property(r => r.Id)
            .ValueGeneratedOnAdd();
        b.Entity<ProjectResourceRecord>()
            .HasOne(r => r.NetworkRecord)
            .WithMany(n => n.Resources)
            .HasForeignKey(r => r.NetworkRecordId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
