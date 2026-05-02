using Docker.DotNet;
using Docker.DotNet.Models;
using Engines.DataBaseStorageEngines;
using Engines.DataBaseStorageEngines.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Engines.FileStorageEngines.Resources;

public class ResourceProvisioningJob
{
    private readonly ProjectDbContext _db;
    private readonly IConnectionMultiplexer _redis;
    private readonly SdkInjectionService _sdkInjection;
    private readonly ILogger<ResourceProvisioningJob> _logger;

    private static readonly TimeSpan LockTtl         = TimeSpan.FromMinutes(12);
    private static readonly TimeSpan WaitTimeout      = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan SpinPollInterval = TimeSpan.FromSeconds(3);

    public ResourceProvisioningJob(
        ProjectDbContext db,
        IConnectionMultiplexer redis,
        SdkInjectionService sdkInjection,
        ILogger<ResourceProvisioningJob> logger)
    {
        _db           = db;
        _redis        = redis;
        _sdkInjection = sdkInjection;
        _logger       = logger;
    }

    // ── Entry point — called by Hangfire ───────────────────────────────────

    public async Task ExecuteAsync(int resourceRecordId)
    {
        var record = await _db.ProjectResources
            .Include(r => r.NetworkRecord)
            .FirstOrDefaultAsync(r => r.Id == resourceRecordId)
            ?? throw new InvalidOperationException($"ResourceRecord {resourceRecordId} not found.");

        var client     = await GetDockerClientAsync(record.AgentId);
        var definition = ResourceCatalog.GetDefinition(Enum.Parse<ResourceType>(record.ResourceType));

        try
        {
            await EnsureImageAsync(client, record.AgentId, definition);
            await ProvisionContainerAsync(client, record, definition);

            record.Status          = "Ready";
            record.LastHealthCheck = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            _logger.LogInformation("[resource] Provisioned {Type} ({Container}) for project {Project}",
                record.ResourceType, record.ContainerName, record.ProjectId);

            // Best-effort MinIO writes — work regardless of whether the editor is running.
            // InjectHelperAsync writes the SDK helper file; WriteResourceEnvFileAsync
            // rewrites .env.resources with the full set of currently active resources.
            try
            {
                await _sdkInjection.InjectHelperAsync(
                    record.ProjectId,
                    Enum.Parse<ResourceType>(record.ResourceType));

                await _sdkInjection.WriteResourceEnvFileAsync(record.ProjectId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[resource] MinIO SDK/env injection failed for project {ProjectId}.",
                    record.ProjectId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[resource] Provisioning failed for record {Id} ({Type})",
                resourceRecordId, record.ResourceType);

            record.Status          = "Failed";
            record.LastHealthCheck = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            throw; // let Hangfire see the failure (retry / dead-letter)
        }
    }

    // ── Image pull with Redis lock-based coordination ──────────────────────

    private async Task EnsureImageAsync(DockerClient client, string agentId, ResourceDefinition definition)
    {
        var parts     = definition.Image.Split(':');
        var imageName = parts[0];
        var imageTag  = parts.Length > 1 ? parts[1] : "latest";

        // Fast path: image already on the agent
        if (await ImageExistsAsync(client, imageName, imageTag))
        {
            _logger.LogInformation("[resource] Image {Image} already present on agent {AgentId}.",
                definition.Image, agentId);
            return;
        }

        var redisDb = _redis.GetDatabase();
        var lockKey = $"image_pull_lock:{agentId}:{imageName}:{imageTag}";

        // Try to claim the pull (SET NX)
        bool lockAcquired = await redisDb.StringSetAsync(lockKey, "1", LockTtl, When.NotExists);

        if (lockAcquired)
        {
            // ── Primary puller ──────────────────────────────────────────────
            // Pull the image then delete the lock. Deleting the lock is what
            // unblocks all spin-waiters.
            _logger.LogInformation("[resource] Lock acquired — pulling {Image} on agent {AgentId}.",
                definition.Image, agentId);
            try
            {
                await client.Images.CreateImageAsync(
                    new ImagesCreateParameters { FromImage = imageName, Tag = imageTag },
                    null,
                    new Progress<JSONMessage>(m =>
                    {
                        if (!string.IsNullOrEmpty(m.Status))
                            _logger.LogDebug("[resource] pull {Image}: {Status}", definition.Image, m.Status);
                    }));

                _logger.LogInformation("[resource] Pull complete for {Image}.", definition.Image);
            }
            finally
            {
                // Always release — even on failure — so waiters are not stuck
                // until the TTL expires. They will verify the image and fail
                // fast if the pull didn't actually succeed.
                await redisDb.KeyDeleteAsync(lockKey);
            }
        }
        else
        {
            // ── Spin-waiter ─────────────────────────────────────────────────
            // Another worker holds the lock and is pulling right now.
            // Poll until the lock key disappears (primary deleted it on completion).
            _logger.LogInformation(
                "[resource] Image {Image} pull in progress on agent {AgentId} — spin-waiting for lock to clear.",
                definition.Image, agentId);

            var deadline = DateTime.UtcNow.Add(WaitTimeout);

            while (DateTime.UtcNow < deadline)
            {
                await Task.Delay(SpinPollInterval);

                bool lockStillHeld = await redisDb.KeyExistsAsync(lockKey);
                if (!lockStillHeld)
                {
                    _logger.LogInformation("[resource] Lock cleared for {Image}. Verifying image presence.",
                        definition.Image);
                    break;
                }
            }

            // Final guarantee check.
            // Covers: (a) primary succeeded, (b) primary failed and released lock, (c) timeout.
            if (!await ImageExistsAsync(client, imageName, imageTag))
                throw new InvalidOperationException(
                    $"Image '{definition.Image}' is not present on agent '{agentId}' after waiting for in-progress pull to complete.");
        }
    }

    // ── Container creation + start ─────────────────────────────────────────

    private async Task ProvisionContainerAsync(
        DockerClient client, ProjectResourceRecord record, ResourceDefinition definition)
    {
        var envList = definition.ContainerEnvVars
            .Select(kv => $"{kv.Key}={kv.Value}")
            .ToList();

        var createResponse = await client.Containers.CreateContainerAsync(new CreateContainerParameters
        {
            Image  = definition.Image,
            Name   = record.ContainerName,
            Env    = envList,
            Labels = new Dictionary<string, string>
            {
                ["com.manager.projectid"]    = record.ProjectId,
                ["com.manager.resourcetype"] = record.ResourceType,
            },
            NetworkingConfig = new NetworkingConfig
            {
                EndpointsConfig = new Dictionary<string, EndpointSettings>
                {
                    [record.NetworkRecord.NetworkName] = new EndpointSettings
                    {
                        Aliases = [definition.DefaultAlias]
                    }
                }
            },
        });

        // Persist the container ID as soon as we have it
        record.ContainerDockerId = createResponse.ID;
        await _db.SaveChangesAsync();

        var started = await client.Containers.StartContainerAsync(
            createResponse.ID, new ContainerStartParameters());
        if (!started)
            throw new InvalidOperationException(
                $"Resource container {record.ContainerName} failed to start.");

        await WaitForContainerRunningAsync(client, createResponse.ID, record.ContainerName);
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private async Task<DockerClient> GetDockerClientAsync(string agentId)
    {
        var agent = await _db.AgentRecords.FirstOrDefaultAsync(a => a.AgentId == agentId)
            ?? throw new InvalidOperationException($"No relay agent registered with id '{agentId}'.");
        return new DockerClientConfiguration(new Uri(agent.DockerHost)).CreateClient();
    }

    private static async Task<bool> ImageExistsAsync(DockerClient client, string imageName, string imageTag)
    {
        var images = await client.Images.ListImagesAsync(new ImagesListParameters
        {
            Filters = new Dictionary<string, IDictionary<string, bool>>
            {
                ["reference"] = new Dictionary<string, bool> { [$"{imageName}:{imageTag}"] = true }
            }
        });
        return images.Count > 0;
    }

    private static async Task WaitForContainerRunningAsync(
        DockerClient client, string containerId, string containerName)
    {
        var deadline = DateTime.UtcNow.AddMinutes(2);
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var inspect = await client.Containers.InspectContainerAsync(containerId);
                if (inspect.State.Running) return;
            }
            catch { }
            await Task.Delay(TimeSpan.FromSeconds(1));
        }
        throw new TimeoutException(
            $"Resource container {containerName} did not become ready within 2 minutes.");
    }
}
