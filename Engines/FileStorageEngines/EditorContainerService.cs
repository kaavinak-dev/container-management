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
using System.Text.Json;

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
    /// Resolves the host to use when the .NET backend makes HTTP calls to a running
    /// editor container (e.g. polling /health, future direct file API calls).
    ///
    /// CURRENT BEHAVIOUR (dev / Docker Desktop on Windows):
    ///   Returns "localhost" — the container's ports (5002, 5003) are forwarded to the
    ///   host machine's localhost, so the .NET backend reaches them via localhost:{port}.
    ///   Docker Desktop on Windows runs containers inside a WSL2 VM, meaning the
    ///   container's internal bridge IP (e.g. 172.17.0.x) is NOT directly reachable
    ///   from a process running on the Windows host. Port-forwarding to localhost is the
    ///   only reliable path from host → container on Docker Desktop.
    ///
    /// WHY THIS IS A TEMPORARY ASSUMPTION:
    ///   This assumes the .NET backend and the editor containers share the same Docker
    ///   host with port-forwarding in place. In a production topology where editor
    ///   containers run on a separate compute fleet, this approach does not scale —
    ///   port conflicts arise with multiple containers and localhost is meaningless
    ///   across machines.
    ///
    ///   The intended future architecture is a dedicated container-proxy / sidecar
    ///   service that maintains a registry of running editor containers and their
    ///   reachable endpoints, acting as the intermediary between the .NET backend and
    ///   the containers. At that point this method should return the proxy's address
    ///   for the given container, and the containerIp parameter (internal bridge IP)
    ///   becomes the proxy's routing key rather than a directly-dialed address.
    ///
    /// DO NOT replace "localhost" with containerIp directly on Docker Desktop —
    /// the internal bridge IP is unreachable from the Windows host process.
    /// </summary>
    private string ResolveContainerHost(string containerIp)
        => "localhost";

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

        var client = GetDockerClient();

        // Docker auto-creates named volumes when specified in Binds
        var createParams = new CreateContainerParameters
        {
            Image = "editor-base:latest",
            Name = containerName,
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
                { "5003/tcp", default },
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
                PortBindings = new Dictionary<string, IList<PortBinding>>
                {
                    { "5003/tcp", new List<PortBinding> { new() { HostIP = "127.0.0.1", HostPort = "5003" } } },
                    { "9999/tcp", new List<PortBinding> { new() { HostIP = "127.0.0.1", HostPort = "9999" } } },
                    { "9229/tcp", new List<PortBinding> { new() { HostIP = "127.0.0.1", HostPort = "9229" } } },
                },
            },
        };

        var createResponse = await client.Containers.CreateContainerAsync(createParams);
        var containerId = createResponse.ID;

        var started = await client.Containers.StartContainerAsync(containerId, new ContainerStartParameters());
        if (!started)
            throw new InvalidOperationException($"Container {containerName} failed to start.");

        var inspect = await client.Containers.InspectContainerAsync(containerId);
        var containerIp = inspect.NetworkSettings.Networks["bridge"].IPAddress;

        if (existing is null)
        {
            _db.EditorSessions.Add(new EditorSessionRecord
            {
                ProjectId = projectId,
                ContainerName = containerName,
                WorkspaceVolume = "fuse-managed", // no Docker volume; rclone FUSE serves /workspace
                NpmCacheVolume = npmCacheVolume,
                ContainerIp = containerIp,
                Status = "Starting",
                LastActive = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
            });
        }
        else
        {
            existing.ContainerIp = containerIp;
            existing.NpmCacheVolume = npmCacheVolume;
            existing.Status = "Starting";
            existing.LastActive = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return (containerIp, alreadyRunning: false);
    }

    // ── PollUntilReadyAsync ───────────────────────────────────────────────────

    public async Task PollUntilReadyAsync(string projectId, string containerIp)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        var deadline = DateTime.UtcNow.AddMinutes(3);

        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var response = await http.GetStringAsync($"http://{ResolveContainerHost(containerIp)}:5003/health");
                using var doc = JsonDocument.Parse(response);
                if (doc.RootElement.TryGetProperty("lspRunning", out var lspProp) && lspProp.GetBoolean())
                {
                    var record = await _db.EditorSessions.FirstOrDefaultAsync(e => e.ProjectId == projectId);
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
                // Container not ready yet — keep polling
            }

            await Task.Delay(TimeSpan.FromSeconds(2));
        }

        throw new TimeoutException(
            $"Editor container for project {projectId} did not become ready within 3 minutes.");
    }

    // ── StopEditorContainerAsync ──────────────────────────────────────────────

    public async Task StopEditorContainerAsync(string projectId)
    {
        var client = GetDockerClient();
        try
        {
            await client.Containers.StopContainerAsync(
                $"editor-{projectId}",
                new ContainerStopParameters { WaitBeforeKillSeconds = 5 });
        }
        catch (DockerContainerNotFoundException)
        {
            // Container already gone — still update DB below
        }

        var record = await _db.EditorSessions.FirstOrDefaultAsync(e => e.ProjectId == projectId);
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
            var client = GetDockerClient();
            var inspect = await client.Containers.InspectContainerAsync($"editor-{projectId}");
            liveStatus = inspect.State.Running ? record.Status : "Stopped";
        }
        catch (DockerContainerNotFoundException)
        {
            liveStatus = "Stopped";
        }

        return new EditorSessionStatus(
            ContainerIp: record.ContainerIp,
            FileApiPort: 5003,
            PtyPort: 9999,
            Status: liveStatus);
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
        var client = GetDockerClient();
        // workspaceVolume is "fuse-managed" sentinel — no Docker volume to delete
        try { await client.Volumes.RemoveAsync(npmCacheVolume); } catch { }
    }

    public async Task DeleteOrphanedNpmCacheVolumesAsync(string projectId, string currentNpmCacheVolume)
    {
        var client = GetDockerClient();
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

public record EditorSessionStatus(string ContainerIp, int FileApiPort, int PtyPort, string Status);
