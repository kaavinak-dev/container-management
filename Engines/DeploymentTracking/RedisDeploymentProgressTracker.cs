using System.Text.Json;
using StackExchange.Redis;

namespace Engines.DeploymentTracking;

public class RedisDeploymentProgressTracker : IDeploymentProgressTracker
{
    private readonly IConnectionMultiplexer _redis;
    private static readonly TimeSpan Ttl = TimeSpan.FromHours(24);

    public RedisDeploymentProgressTracker(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    private static string Key(Guid id) => $"deployment:{id}:steps";

    public async Task InitializeAsync(Guid executableProjectId)
    {
        var steps = DeploymentStepKey.Ordered.Select((s, i) => new DeploymentStep
        {
            Key         = s.Key,
            Label       = s.Label,
            Status      = i == 0 ? DeploymentStepStatus.Completed : DeploymentStepStatus.Pending,
            StartedAt   = i == 0 ? DateTime.UtcNow : null,
            CompletedAt = i == 0 ? DateTime.UtcNow : null,
        }).ToList();

        await Save(executableProjectId, steps);
    }

    public Task SetStepRunningAsync(Guid executableProjectId, string stepKey) =>
        Mutate(executableProjectId, stepKey, s =>
        {
            s.Status    = DeploymentStepStatus.Running;
            s.StartedAt = DateTime.UtcNow;
        });

    public Task SetStepCompletedAsync(Guid executableProjectId, string stepKey) =>
        Mutate(executableProjectId, stepKey, s =>
        {
            s.Status      = DeploymentStepStatus.Completed;
            s.CompletedAt = DateTime.UtcNow;
        });

    public Task SetStepFailedAsync(Guid executableProjectId, string stepKey, string errorMessage) =>
        Mutate(executableProjectId, stepKey, s =>
        {
            s.Status       = DeploymentStepStatus.Failed;
            s.CompletedAt  = DateTime.UtcNow;
            s.ErrorMessage = errorMessage;
        });

    public Task SetStepSkippedAsync(Guid executableProjectId, string stepKey) =>
        Mutate(executableProjectId, stepKey, s =>
        {
            s.Status      = DeploymentStepStatus.Skipped;
            s.CompletedAt = DateTime.UtcNow;
        });

    public async Task<IReadOnlyList<DeploymentStep>?> GetStepsAsync(Guid executableProjectId)
    {
        var db  = _redis.GetDatabase();
        var raw = await db.StringGetAsync(Key(executableProjectId));
        if (raw.IsNullOrEmpty) return null;
        return JsonSerializer.Deserialize<List<DeploymentStep>>(raw!);
    }

    // ---------- private helpers ----------

    private async Task Mutate(Guid id, string stepKey, Action<DeploymentStep> update)
    {
        var db   = _redis.GetDatabase();
        var raw  = await db.StringGetAsync(Key(id));
        if (raw.IsNullOrEmpty) return; // key expired or never initialised — no-op

        var steps = JsonSerializer.Deserialize<List<DeploymentStep>>(raw!)!;
        var step  = steps.FirstOrDefault(s => s.Key == stepKey);
        if (step != null) update(step);

        await Save(id, steps);
    }

    private async Task Save(Guid id, IEnumerable<DeploymentStep> steps)
    {
        var db   = _redis.GetDatabase();
        var json = JsonSerializer.Serialize(steps);
        await db.StringSetAsync(Key(id), json, Ttl);
    }
}
