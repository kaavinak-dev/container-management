namespace Engines.DeploymentTracking;

public interface IDeploymentProgressTracker
{
    /// <summary>Writes the initial step list to Redis with packaging=completed, rest=pending.</summary>
    Task InitializeAsync(Guid executableProjectId);

    Task SetStepRunningAsync(Guid executableProjectId, string stepKey);
    Task SetStepCompletedAsync(Guid executableProjectId, string stepKey);
    Task SetStepFailedAsync(Guid executableProjectId, string stepKey, string errorMessage);
    Task SetStepSkippedAsync(Guid executableProjectId, string stepKey);

    /// <summary>Returns the current step list, or null if the key has expired / never existed.</summary>
    Task<IReadOnlyList<DeploymentStep>?> GetStepsAsync(Guid executableProjectId);
}
