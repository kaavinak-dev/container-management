namespace Engines.DeploymentTracking;

public static class DeploymentStepKey
{
    public const string Packaging      = "packaging";
    public const string VirusScan      = "virus-scan";
    public const string NpmAudit       = "npm-audit";
    public const string ContainerBuild = "container-build";
    public const string ContainerStart = "container-start";

    public static readonly IReadOnlyList<(string Key, string Label)> Ordered =
    [
        (Packaging,      "Packaging files"),
        (VirusScan,      "Running virus scan"),
        (NpmAudit,       "Running npm audit"),
        (ContainerBuild, "Building Docker container"),
        (ContainerStart, "Starting container"),
    ];
}

public static class DeploymentStepStatus
{
    public const string Pending   = "pending";
    public const string Running   = "running";
    public const string Completed = "completed";
    public const string Failed    = "failed";
    public const string Skipped   = "skipped";
}

public class DeploymentStep
{
    public string Key           { get; set; } = "";
    public string Label         { get; set; } = "";
    public string Status        { get; set; } = DeploymentStepStatus.Pending;
    public DateTime? StartedAt   { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage  { get; set; }
}
