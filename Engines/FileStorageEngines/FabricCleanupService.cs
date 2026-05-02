using Engines.DataBaseStorageEngines;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Engines.FileStorageEngines;

/// <summary>
/// Background service that tears down idle project fabrics.
/// Runs every 5 minutes. Any fabric whose LastActivity is older than the idle threshold
/// (default 10 minutes) gets fully torn down: resource containers stopped/removed,
/// editor containers stopped, Docker network deleted, DB records updated.
/// </summary>
public class FabricCleanupService : IHostedService, IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<FabricCleanupService> _logger;
    private Timer? _timer;

    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan IdleThreshold = TimeSpan.FromMinutes(10);

    public FabricCleanupService(
        IServiceScopeFactory scopeFactory,
        ILogger<FabricCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "FabricCleanupService starting — check interval: {Interval}, idle threshold: {Threshold}",
            CheckInterval, IdleThreshold);

        // First run after one interval (not immediately), then repeat
        _timer = new Timer(RunCleanup, null, CheckInterval, CheckInterval);
        return Task.CompletedTask;
    }

    private void RunCleanup(object? state) => _ = RunCleanupAsync();

    private async Task RunCleanupAsync()
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();
            var fabricService = scope.ServiceProvider.GetRequiredService<ProjectFabricService>();

            var cutoff = DateTime.UtcNow - IdleThreshold;

            var staleNetworks = await db.ProjectNetworks
                .Where(n => n.Status == "Active" && n.LastActivity < cutoff)
                .ToListAsync();

            if (staleNetworks.Count == 0) return;

            _logger.LogInformation(
                "FabricCleanupService: found {Count} idle fabric(s) to tear down.", staleNetworks.Count);

            foreach (var network in staleNetworks)
            {
                try
                {
                    await fabricService.TeardownFabricAsync(network.ProjectId);
                    _logger.LogInformation(
                        "FabricCleanupService: torn down idle fabric for project {ProjectId} " +
                        "(last activity: {LastActivity})",
                        network.ProjectId, network.LastActivity);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "FabricCleanupService: failed to tear down fabric for project {ProjectId}",
                        network.ProjectId);
                }
            }

            _logger.LogInformation(
                "FabricCleanupService: cleanup pass complete — processed {Count} fabric(s).",
                staleNetworks.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FabricCleanupService: unhandled error in cleanup pass.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("FabricCleanupService stopping.");
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public void Dispose() => _timer?.Dispose();
}
