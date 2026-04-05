using Engines.DataBaseStorageEngines;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Engines.FileStorageEngines;

public class EditorVolumeCleanupService : IHostedService, IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EditorVolumeCleanupService> _logger;
    private Timer? _timer;

    public EditorVolumeCleanupService(
        IServiceScopeFactory scopeFactory,
        ILogger<EditorVolumeCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("EditorVolumeCleanupService starting.");
        // Run immediately on startup, then every 30 minutes
        _timer = new Timer(RunCleanup, null, TimeSpan.Zero, TimeSpan.FromMinutes(30));
        return Task.CompletedTask;
    }

    private void RunCleanup(object? state) => _ = RunCleanupAsync();

    private async Task RunCleanupAsync()
    {
        _logger.LogInformation("EditorVolumeCleanupService: starting cleanup pass.");
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();
            var editorService = scope.ServiceProvider.GetRequiredService<EditorContainerService>();

            // Stop and delete volumes for sessions idle for more than 2 hours
            var staleThreshold = DateTime.UtcNow.AddHours(-2);
            var staleRecords = await db.EditorSessions
                .Where(e => e.LastActive < staleThreshold
                         && e.Status != "Stopped"
                         && e.Status != "Cleaned")
                .ToListAsync();

            foreach (var record in staleRecords)
            {
                _logger.LogInformation(
                    "Cleaning stale editor session for project {ProjectId}", record.ProjectId);
                try
                {
                    await editorService.StopEditorContainerAsync(record.ProjectId);
                    await editorService.DeleteVolumesAsync(record.WorkspaceVolume, record.NpmCacheVolume);
                    record.Status = "Cleaned";
                    await db.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex, "Failed to clean session for project {ProjectId}", record.ProjectId);
                }
            }

            // Delete orphaned npm cache volumes for all known projects
            var allRecords = await db.EditorSessions.ToListAsync();
            foreach (var record in allRecords)
            {
                try
                {
                    await editorService.DeleteOrphanedNpmCacheVolumesAsync(
                        record.ProjectId, record.NpmCacheVolume);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Failed to clean orphaned npm cache volumes for project {ProjectId}",
                        record.ProjectId);
                }
            }

            _logger.LogInformation("EditorVolumeCleanupService: cleanup pass complete.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "EditorVolumeCleanupService: unhandled error in cleanup pass.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public void Dispose() => _timer?.Dispose();
}
