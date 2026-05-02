using Docker.DotNet;
using Docker.DotNet.Models;
using Engines.DataBaseStorageEngines;
using Engines.DataBaseStorageEngines.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Engines.FileStorageEngines;

public record FabricStatus(
    string NetworkName,
    string Status,
    int ResourceCount,
    DateTime LastActivity);

public class ProjectFabricService
{
    private readonly ProjectDbContext _db;
    private readonly IConfiguration _config;

    public ProjectFabricService(ProjectDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    // ── Docker Client Resolution ────────────────────────────────────────────

    private async Task<DockerClient> GetDockerClientForAgentAsync(string agentId)
    {
        var agent = await _db.AgentRecords.FirstOrDefaultAsync(a => a.AgentId == agentId);
        if (agent is null)
            throw new InvalidOperationException($"No relay agent registered with id '{agentId}'.");
        return new DockerClientConfiguration(new Uri(agent.DockerHost)).CreateClient();
    }

    // ── EnsureNetworkAsync ──────────────────────────────────────────────────

    /// <summary>
    /// Ensures a fabric network exists for the project on the given agent.
    /// Returns the existing record if the network is still alive, or creates a new one.
    /// </summary>
    public async Task<ProjectNetworkRecord> EnsureNetworkAsync(string projectId, string agentId)
    {
        var existing = await _db.ProjectNetworks
            .FirstOrDefaultAsync(n => n.ProjectId == projectId && n.Status == "Active");

        if (existing is not null)
        {
            // Verify the Docker network still exists on the agent
            var client = await GetDockerClientForAgentAsync(existing.AgentId);
            try
            {
                await client.Networks.InspectNetworkAsync(existing.NetworkDockerId);
                existing.LastActivity = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                return existing;
            }
            catch (DockerNetworkNotFoundException)
            {
                // Network was removed externally — mark stale and recreate
                existing.Status = "Removed";
                await _db.SaveChangesAsync();
            }
        }

        // Create the network
        var networkName = $"net-{projectId}";
        var dockerClient = await GetDockerClientForAgentAsync(agentId);

        var createResponse = await dockerClient.Networks.CreateNetworkAsync(new NetworksCreateParameters
        {
            Name = networkName,
            Driver = "bridge",
            Labels = new Dictionary<string, string>
            {
                ["com.manager.projectid"] = projectId,
            },
        });

        var record = new ProjectNetworkRecord
        {
            ProjectId = projectId,
            NetworkDockerId = createResponse.ID,
            NetworkName = networkName,
            AgentId = agentId,
            Status = "Active",
            CreatedAt = DateTime.UtcNow,
            LastActivity = DateTime.UtcNow,
        };

        _db.ProjectNetworks.Add(record);
        await _db.SaveChangesAsync();

        Console.WriteLine($"[fabric] Created Docker network {networkName} on agent {agentId}");
        return record;
    }

    // ── ConnectContainerToNetworkAsync ───────────────────────────────────────

    public async Task ConnectContainerToNetworkAsync(
        string networkName, string containerId, string alias, string agentId)
    {
        var client = await GetDockerClientForAgentAsync(agentId);
        await client.Networks.ConnectNetworkAsync(networkName, new NetworkConnectParameters
        {
            Container = containerId,
            EndpointConfig = new EndpointSettings
            {
                Aliases = new List<string> { alias },
            },
        });
    }

    // ── DisconnectContainerFromNetworkAsync ──────────────────────────────────

    public async Task DisconnectContainerFromNetworkAsync(
        string networkName, string containerId, string agentId)
    {
        var client = await GetDockerClientForAgentAsync(agentId);
        try
        {
            await client.Networks.DisconnectNetworkAsync(networkName, new NetworkDisconnectParameters
            {
                Container = containerId,
                Force = true,
            });
        }
        catch (Exception)
        {
            // Container may already be disconnected or removed
        }
    }

    // ── TeardownFabricAsync ─────────────────────────────────────────────────

    /// <summary>
    /// Stops all resource containers, disconnects everything, removes the network,
    /// and marks all DB records as stopped/removed.
    /// </summary>
    public async Task TeardownFabricAsync(string projectId)
    {
        var networkRecord = await _db.ProjectNetworks
            .FirstOrDefaultAsync(n => n.ProjectId == projectId && n.Status == "Active");

        if (networkRecord is null) return;

        networkRecord.Status = "TearingDown";
        await _db.SaveChangesAsync();

        var client = await GetDockerClientForAgentAsync(networkRecord.AgentId);

        // Stop and remove all resource containers
        var resources = await _db.ProjectResources
            .Where(r => r.ProjectId == projectId && r.Status != "Stopped")
            .ToListAsync();

        foreach (var resource in resources)
        {
            try
            {
                await client.Containers.StopContainerAsync(
                    resource.ContainerDockerId,
                    new ContainerStopParameters { WaitBeforeKillSeconds = 5 });
                await client.Containers.RemoveContainerAsync(
                    resource.ContainerDockerId,
                    new ContainerRemoveParameters { Force = true });
            }
            catch (DockerContainerNotFoundException) { }
            catch (Exception) { }

            resource.Status = "Stopped";
        }

        // Remove the Docker network
        try
        {
            await client.Networks.DeleteNetworkAsync(networkRecord.NetworkName);
        }
        catch (Exception) { }

        networkRecord.Status = "Removed";
        await _db.SaveChangesAsync();

        Console.WriteLine($"[fabric] Torn down fabric for project {projectId}");
    }

    // ── UpdateFabricActivityAsync ───────────────────────────────────────────

    public async Task UpdateFabricActivityAsync(string projectId)
    {
        var record = await _db.ProjectNetworks
            .FirstOrDefaultAsync(n => n.ProjectId == projectId && n.Status == "Active");

        if (record is null) return;

        record.LastActivity = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    // ── GetFabricStatusAsync ────────────────────────────────────────────────

    public async Task<FabricStatus?> GetFabricStatusAsync(string projectId)
    {
        var record = await _db.ProjectNetworks
            .FirstOrDefaultAsync(n => n.ProjectId == projectId && n.Status == "Active");

        if (record is null) return null;

        var resourceCount = await _db.ProjectResources
            .CountAsync(r => r.ProjectId == projectId && r.Status != "Stopped");

        return new FabricStatus(record.NetworkName, record.Status, resourceCount, record.LastActivity);
    }
}
