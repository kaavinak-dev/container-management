using Docker.DotNet;
using Docker.DotNet.Models;
using Engines.DataBaseStorageEngines;
using Engines.DataBaseStorageEngines.Entities;
using Engines.FileStorageEngines.ContainerBuild;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Minio;
using Minio.DataModel.Args;
using OperatingSystemLake.Abstractions;
using OperatingSystemLake.Constants;
using System.Security.Cryptography;

namespace Engines.FileStorageEngines;

public class EditorContainerService
{
    private readonly IDockerClientFactory _dockerFactory;
    private readonly ProjectDbContext _db;
    private readonly IConfiguration _config;

    private string MinioEndpoint => _config["EditorContainer:MinioEndpoint"]!;
    private int MinioPort => _config.GetValue<int>("EditorContainer:MinioPort");
    private string MinioAccessKey => _config["EditorContainer:MinioAccessKey"]!;
    private string MinioSecretKey => _config["EditorContainer:MinioSecretKey"]!;
    private string MinioBucket => _config["EditorContainer:MinioBucket"]!;

    public EditorContainerService(
        IDockerClientFactory dockerFactory,
        ProjectDbContext db,
        IConfiguration config)
    {
        _dockerFactory = dockerFactory;
        _db = db;
        _config = config;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private DockerClient GetDockerClient()
    {
        var techTypeName = _config["EditorContainer:DockerTechType"] ?? "LocalDocker";
        var techType = Enum.Parse<OSLakeTechTypes>(techTypeName, ignoreCase: true);
        return _dockerFactory.CreateForLake(techType, OSLakeTypes.Linux);
    }

    private async Task<DockerClient> GetDockerClientForAgentAsync(string agentId)
    {
        var agent = await _db.AgentRecords.FirstOrDefaultAsync(a => a.AgentId == agentId);
        if (agent is null)
            throw new InvalidOperationException($"No relay agent registered with id '{agentId}'. Ensure the relay agent is running.");

        return new DockerClientConfiguration(new Uri(agent.DockerHost)).CreateClient();
    }

    //bug: agent id has to be passed in function are param below this has to be fixed
    // 
    private async Task<string> ResolveAgentIdAsync()
    {
        // Pick the most recently seen agent. In local dev there will only be one ("local-dev").
        // In production, caller should pass the desired agentId explicitly.
        var agent = await _db.AgentRecords
            .OrderByDescending(a => a.LastSeen)
            .FirstOrDefaultAsync();

        if (agent is null)
            throw new InvalidOperationException("No relay agents are registered. Ensure the relay agent container is running.");

        return agent.AgentId;
    }

    private IMinioClient BuildMinioClient() =>
        new MinioClient()
            .WithEndpoint(MinioEndpoint, MinioPort)
            .WithCredentials(MinioAccessKey, MinioSecretKey)
            .WithSSL(false)
            .Build();

    /// <summary>
    /// Resolves the MinIO endpoint hostname/IP to pass as an environment variable
    /// into the editor container so the sidecar can reach MinIO from inside Docker.
    ///
    /// CURRENT BEHAVIOUR (dev / Docker Desktop):
    ///   Returns "host.docker.internal" — Docker Desktop's built-in DNS name that
    ///   resolves to the host machine from inside any container. This works because
    ///   the MinIO container (on the "infra" docker-compose network) is port-forwarded
    ///   to the host at the same port number, so host.docker.internal:{port} reaches it.
    ///
    /// WHY THIS IS A TEMPORARY ASSUMPTION:
    ///   This assumes the infra containers (MinIO) and the editor containers run on the
    ///   same Docker host, so the port-forward on that host bridges the two. If in future
    ///   these are on separate hosts — e.g. MinIO on an infra EC2 instance and editor
    ///   containers on a separate compute fleet — this method must be changed to return
    ///   the actual network-accessible address of the MinIO service as seen from the
    ///   editor container's host (e.g. a private VPC hostname, an internal load balancer
    ///   DNS name, or a service mesh endpoint). At that point MinioEndpoint from config
    ///   will likely already be that address and this method can simply return it directly.
    ///
    /// DO NOT replace this with MinioEndpoint directly without addressing the above —
    /// 127.0.0.1 / localhost from config refers to the .NET backend's own loopback,
    /// which is unreachable from inside a container.
    /// </summary>
    private string ResolveMinioEndpointForContainer()
        => "host.docker.internal";

    /// <summary>
    /// Ensures a project-specific Docker bridge network exists for the editor container.
    /// Each project gets its own isolated network (editor-net-{projectId}).
    /// The relay agent (network_mode: host) can reach any bridge network, so it will
    /// reach this container regardless of which project network it is on.
    /// </summary>
    private async Task<string> EnsureProjectNetworkAsync(DockerClient client, string projectId)
    {
        var networkName = $"editor-net-{projectId}";
        var networks = await client.Networks.ListNetworksAsync(new NetworksListParameters
        {
            Filters = new Dictionary<string, IDictionary<string, bool>>
            {
                ["name"] = new Dictionary<string, bool> { [networkName] = true }
            }
        });

        if (!networks.Any(n => n.Name == networkName))
        {
            await client.Networks.CreateNetworkAsync(new NetworksCreateParameters
            {
                Name = networkName,
                Driver = "bridge",
            });
            Console.WriteLine($"[editor] Created Docker network {networkName}");
        }

        return networkName;
    }

    /// <summary>
    /// Returns first 8 chars of SHA256 hash of package-lock.json (preferred) or package.json (fallback).
    /// If neither exists, hashes the projectId itself as a stable last-resort fallback.
    /// </summary>
    private async Task<string> GetNpmCacheHashAsync(string projectId)
    {
        var client = BuildMinioClient();

        foreach (var fileName in new[] { "package-lock.json", "package.json" })
        {
            try
            {
                using var ms = new MemoryStream();
                var args = new GetObjectArgs()
                    .WithBucket(MinioBucket)
                    .WithObject($"{projectId}/{fileName}")
                    .WithCallbackStream(stream => stream.CopyTo(ms));

                await client.GetObjectAsync(args);
                var hash = SHA256.HashData(ms.ToArray());
                return Convert.ToHexString(hash)[..8].ToLowerInvariant();
            }
            catch (Exception e) { }
        }

        // Both files missing — use projectId itself as stable fallback
        var fallbackHash = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(projectId));
        return Convert.ToHexString(fallbackHash)[..8].ToLowerInvariant();
    }

    // ── StartEditorContainerAsync ─────────────────────────────────────────────

    public async Task<(string containerIp, bool alreadyRunning)> StartEditorContainerAsync(string projectId)
    {
        // Return existing session if already running
        var existing = await _db.EditorSessions
            .FirstOrDefaultAsync(e => e.ProjectId == projectId);

        if (existing is not null && existing.Status is "Ready" or "Starting")
            return (existing.ContainerIp, alreadyRunning: true);

        var hashSuffix = await GetNpmCacheHashAsync(projectId);

        var containerName = $"editor-{projectId}";
        var npmCacheVolume = $"npm-cache-{projectId}-{hashSuffix}"; // workspace-* volume removed; rclone FUSE serves /workspace

        var agentId = await ResolveAgentIdAsync();
        var client = await GetDockerClientForAgentAsync(agentId);

        var networkName = await EnsureProjectNetworkAsync(client, projectId);

        // Docker auto-creates named volumes when specified in Binds
        var createParams = new CreateContainerParameters
        {
            Image = "editor-base:latest",
            Name = containerName,
            Labels = new Dictionary<string, string>
            {
                { "com.manager.projectid", projectId }
            },
            Env = new List<string>
            {
                $"MINIO_ENDPOINT={ResolveMinioEndpointForContainer()}",
                $"MINIO_PORT={MinioPort}",
                $"MINIO_ACCESS_KEY={MinioAccessKey}",
                $"MINIO_SECRET_KEY={MinioSecretKey}",
                $"MINIO_BUCKET={MinioBucket}",
                $"PROJECT_ID={projectId}",
            },
            ExposedPorts = new Dictionary<string, EmptyStruct>
            {
                { "9999/tcp", default },
                { "9229/tcp", default },
            },
            HostConfig = new HostConfig
            {
                Binds = new List<string>
                {
                    $"{npmCacheVolume}:/workspace/node_modules", // workspace-* volume removed; rclone FUSE serves /workspace
                },
                Devices = new List<DeviceMapping>
                {
                    new DeviceMapping
                    {
                        PathOnHost        = "/dev/fuse",
                        PathInContainer   = "/dev/fuse",
                        CgroupPermissions = "rwm",
                    }
                },
                CapAdd      = new List<string> { "SYS_ADMIN" },
                SecurityOpt = new List<string> { "apparmor:unconfined" }, // required on Ubuntu/WSL2 for FUSE mount syscalls
                // No port bindings — PTY is routed through the relay agent tunnel,
                // and the file API has been replaced by the FUSE/MinIO virtual filesystem.
            },
            // Attach to the project-specific network so containers are isolated per project.
            // The relay agent (network_mode: host) can reach any bridge network, so it will
            // reach this container regardless of which project network it is on.
            NetworkingConfig = new NetworkingConfig
            {
                EndpointsConfig = new Dictionary<string, EndpointSettings>
                {
                    [networkName] = new EndpointSettings()
                }
            },
        };

        var createResponse = await client.Containers.CreateContainerAsync(createParams);
        var containerId = createResponse.ID;

        var started = await client.Containers.StartContainerAsync(containerId, new ContainerStartParameters());
        if (!started)
            throw new InvalidOperationException($"Container {containerName} failed to start.");

        var inspect = await client.Containers.InspectContainerAsync(containerId);
        var containerIp = inspect.NetworkSettings.Networks[networkName].IPAddress;

        if (existing is null)
        {
            _db.EditorSessions.Add(new EditorSessionRecord
            {
                ProjectId = projectId,
                ContainerName = containerName,
                WorkspaceVolume = "fuse-managed", // no Docker volume; rclone FUSE serves /workspace
                NpmCacheVolume = npmCacheVolume,
                ContainerIp = containerIp,
                AgentId = agentId,
                ContainerId = containerId,
                Status = "Starting",
                LastActive = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
            });
        }
        else
        {
            existing.ContainerIp = containerIp;
            existing.AgentId = agentId;
            existing.ContainerId = containerId;
            existing.NpmCacheVolume = npmCacheVolume;
            existing.Status = "Starting";
            existing.LastActive = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return (containerIp, alreadyRunning: false);
    }

    // ── WaitForContainerRunningAsync ──────────────────────────────────────────

    public async Task WaitForContainerRunningAsync(string projectId, string containerId)
    {
        var record = await _db.EditorSessions.FirstOrDefaultAsync(e => e.ProjectId == projectId);
        if (record?.AgentId is null)
            throw new InvalidOperationException($"No agent found for project {projectId}");

        var client = await GetDockerClientForAgentAsync(record.AgentId);
        var deadline = DateTime.UtcNow.AddMinutes(3);

        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var inspect = await client.Containers.InspectContainerAsync(containerId);
                if (inspect.State.Running)
                {
                    // Give the FUSE mount and sidecar a moment to initialise
                    await Task.Delay(TimeSpan.FromSeconds(2));

                    if (record is not null)
                    {
                        record.Status = "Ready";
                        record.LastActive = DateTime.UtcNow;
                        await _db.SaveChangesAsync();
                    }
                    return;
                }
            }
            catch (Exception)
            {
                // Container not yet inspectable — keep polling
            }

            await Task.Delay(TimeSpan.FromSeconds(2));
        }

        throw new TimeoutException(
            $"Editor container for project {projectId} did not become ready within 3 minutes.");
    }

    // ── StopEditorContainerAsync ──────────────────────────────────────────────

    public async Task StopEditorContainerAsync(string projectId)
    {
        var record = await _db.EditorSessions.FirstOrDefaultAsync(e => e.ProjectId == projectId);

        try
        {
            var agentId = record?.AgentId ?? await ResolveAgentIdAsync();
            var client = await GetDockerClientForAgentAsync(agentId);

            await client.Containers.StopContainerAsync(
                $"editor-{projectId}",
                new ContainerStopParameters { WaitBeforeKillSeconds = 5 });
            try { await client.Networks.DeleteNetworkAsync($"editor-net-{projectId}"); } catch { }
        }
        catch (DockerContainerNotFoundException)
        {
            // Container already gone — still update DB below
        }

        // Remove the project-specific network now that the container is stopped
        

        if (record is not null)
        {
            record.Status = "Stopped";
            await _db.SaveChangesAsync();
        }
    }

    // ── GetEditorContainerStatusAsync ─────────────────────────────────────────

    public async Task<EditorSessionStatus?> GetEditorContainerStatusAsync(string projectId)
    {
        var record = await _db.EditorSessions.FirstOrDefaultAsync(e => e.ProjectId == projectId);
        if (record is null) return null;

        var liveStatus = record.Status;
        try
        {
            var agentId = record.AgentId ?? await ResolveAgentIdAsync();
            var client = await GetDockerClientForAgentAsync(agentId);

            var inspect = await client.Containers.InspectContainerAsync($"editor-{projectId}");
            liveStatus = inspect.State.Running ? record.Status : "Stopped";
        }
        catch (DockerContainerNotFoundException)
        {
            liveStatus = "Stopped";
        }

        return new EditorSessionStatus(
            ContainerIp: record.ContainerIp,
            PtyPort: 9999,
            Status: liveStatus,
            AgentId: record.AgentId,
            ContainerId: record.ContainerId);
    }

    // ── UpdateLastActiveAsync ─────────────────────────────────────────────────

    public async Task UpdateLastActiveAsync(string projectId)
    {
        var record = await _db.EditorSessions.FirstOrDefaultAsync(e => e.ProjectId == projectId);
        if (record is null) return;
        record.LastActive = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    // ── Volume cleanup helpers ────────────────────────────────────────────────

    public async Task DeleteVolumesAsync(string workspaceVolume, string npmCacheVolume)
    {
        var agentId = await ResolveAgentIdAsync();
        var client = await GetDockerClientForAgentAsync(agentId);
        // workspaceVolume is "fuse-managed" sentinel — no Docker volume to delete
        try { await client.Volumes.RemoveAsync(npmCacheVolume); } catch { }
    }

    public async Task DeleteOrphanedNpmCacheVolumesAsync(string projectId, string currentNpmCacheVolume)
    {
        var agentId = await ResolveAgentIdAsync();
        var client = await GetDockerClientForAgentAsync(agentId);
        var allVolumes = await client.Volumes.ListAsync(new VolumesListParameters());
        var prefix = $"npm-cache-{projectId}-";
        var orphans = allVolumes.Volumes
            .Where(v => v.Name.StartsWith(prefix) && v.Name != currentNpmCacheVolume)
            .ToList();

        foreach (var vol in orphans)
        {
            try { await client.Volumes.RemoveAsync(vol.Name); } catch { }
        }
    }
}

public record EditorSessionStatus(
    string ContainerIp,
    int PtyPort,
    string Status,
    string? AgentId = null,
    string? ContainerId = null
);
