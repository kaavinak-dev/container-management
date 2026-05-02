using Docker.DotNet;
using Docker.DotNet.Models;
using Engines.DataBaseStorageEngines;
using Engines.DataBaseStorageEngines.Entities;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace Engines.FileStorageEngines.Resources;

public record ResourceStatusDto(
    int Id,
    string ResourceType,
    string Alias,
    string Status,
    string ImageTag,
    string ContainerName,
    DateTime CreatedAt,
    string? LiveState);

public class ResourceProvisionerService
{
    private readonly ProjectFabricService _fabricService;
    private readonly ProjectDbContext _db;
    private readonly IConfiguration _config;
    private readonly IBackgroundJobClient _jobClient;

    public ResourceProvisionerService(
        ProjectFabricService fabricService,
        ProjectDbContext db,
        IConfiguration config,
        IBackgroundJobClient jobClient)
    {
        _fabricService = fabricService;
        _db            = db;
        _config        = config;
        _jobClient     = jobClient;
    }

    // ── Docker Client Resolution ────────────────────────────────────────────

    private async Task<DockerClient> GetDockerClientForAgentAsync(string agentId)
    {
        var agent = await _db.AgentRecords.FirstOrDefaultAsync(a => a.AgentId == agentId);
        if (agent is null)
            throw new InvalidOperationException($"No relay agent registered with id '{agentId}'.");
        return new DockerClientConfiguration(new Uri(agent.DockerHost)).CreateClient();
    }

    // ── ProvisionResourceAsync — REQUEST PHASE only ────────────────────────
    // Creates the DB record immediately (Status = Provisioning) and hands all
    // Docker work off to ResourceProvisioningJob running in AsyncJobWorkers.

    public async Task<ProjectResourceRecord> ProvisionResourceAsync(string projectId, ResourceType resourceType)
    {
        // Validate: no duplicate resource of the same type for this project
        var duplicateExists = await _db.ProjectResources.AnyAsync(r =>
            r.ProjectId == projectId
            && r.ResourceType == resourceType.ToString()
            && r.Status != "Stopped");

        if (duplicateExists)
            throw new InvalidOperationException(
                $"A {resourceType} resource already exists for project {projectId}. Remove it before adding another.");

        var definition    = ResourceCatalog.GetDefinition(resourceType);
        var agentId       = await ResolveAgentIdForProjectAsync(projectId);
        var networkRecord = await _fabricService.EnsureNetworkAsync(projectId, agentId);

        var record = new ProjectResourceRecord
        {
            ProjectId       = projectId,
            NetworkRecordId = networkRecord.Id,
            ResourceType    = resourceType.ToString(),
            ContainerName   = $"{resourceType.ToString().ToLowerInvariant()}-{projectId}",
            Alias           = definition.DefaultAlias,
            ImageTag        = definition.Image,
            AgentId         = agentId,
            Status          = "Provisioning",
            EnvironmentJson = JsonSerializer.Serialize(definition.ContainerEnvVars),
            CreatedAt       = DateTime.UtcNow,
            LastHealthCheck = DateTime.UtcNow,
        };

        _db.ProjectResources.Add(record);
        await _db.SaveChangesAsync();   // record gets its Id here

        // Hand off all Docker operations to the background worker
        _jobClient.Enqueue<ResourceProvisioningJob>(job => job.ExecuteAsync(record.Id));

        Console.WriteLine($"[resource] Enqueued provisioning job for {resourceType} (record {record.Id}) on agent {agentId}");
        return record;
    }

    // ── DeprovisionResourceAsync ────────────────────────────────────────────

    public async Task DeprovisionResourceAsync(string projectId, int resourceId)
    {
        var record = await _db.ProjectResources
            .FirstOrDefaultAsync(r => r.Id == resourceId && r.ProjectId == projectId);

        if (record is null)
            throw new InvalidOperationException($"Resource {resourceId} not found for project {projectId}.");

        var client = await GetDockerClientForAgentAsync(record.AgentId);

        try
        {
            await client.Containers.StopContainerAsync(
                record.ContainerDockerId,
                new ContainerStopParameters { WaitBeforeKillSeconds = 5 });
            await client.Containers.RemoveContainerAsync(
                record.ContainerDockerId,
                new ContainerRemoveParameters { Force = true });
        }
        catch (DockerContainerNotFoundException) { }

        record.Status = "Stopped";
        await _db.SaveChangesAsync();

        Console.WriteLine($"[resource] Deprovisioned {record.ResourceType} ({record.ContainerName}) for project {projectId}");
    }

    // ── ListResourcesAsync ──────────────────────────────────────────────────

    public async Task<List<ProjectResourceRecord>> ListResourcesAsync(string projectId)
    {
        return await _db.ProjectResources
            .Where(r => r.ProjectId == projectId && r.Status != "Stopped")
            .OrderBy(r => r.CreatedAt)
            .ToListAsync();
    }

    // ── GetResourceStatusAsync ──────────────────────────────────────────────

    public async Task<ResourceStatusDto?> GetResourceStatusAsync(string projectId, int resourceId)
    {
        var record = await _db.ProjectResources
            .FirstOrDefaultAsync(r => r.Id == resourceId && r.ProjectId == projectId);

        if (record is null) return null;

        string? liveState = null;
        try
        {
            var client = await GetDockerClientForAgentAsync(record.AgentId);
            var inspect = await client.Containers.InspectContainerAsync(record.ContainerDockerId);
            liveState = inspect.State.Status;
        }
        catch { }

        return new ResourceStatusDto(
            record.Id,
            record.ResourceType,
            record.Alias,
            record.Status,
            record.ImageTag,
            record.ContainerName,
            record.CreatedAt,
            liveState);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<string> ResolveAgentIdForProjectAsync(string projectId)
    {
        // If a fabric already exists, use that agent
        var existingNetwork = await _db.ProjectNetworks
            .FirstOrDefaultAsync(n => n.ProjectId == projectId && n.Status == "Active");
        if (existingNetwork is not null) return existingNetwork.AgentId;

        // If an editor session exists, use that agent
        var session = await _db.EditorSessions
            .FirstOrDefaultAsync(e => e.ProjectId == projectId && e.AgentId != null);
        if (session is not null) return session.AgentId!;

        // Fall back to most recently seen agent
        var agent = await _db.AgentRecords
            .OrderByDescending(a => a.LastSeen)
            .FirstOrDefaultAsync();
        if (agent is null)
            throw new InvalidOperationException("No relay agents are registered.");

        return agent.AgentId;
    }

}
