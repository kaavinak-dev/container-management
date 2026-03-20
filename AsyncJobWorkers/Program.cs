// See https://aka.ms/new-console-template for more information
using Hangfire;
using Hangfire.Redis.StackExchange;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Engines.FileStorageEngines.ContainerBuild;
using Engines.FileStorageEngines.Implementations;
using OperatingSystemHelpers.Implementations.Windows;
using OperatingSystemLake.Abstractions;
using OperatingSystemLake.Constants;
using OperatingSystemLake.Factory;
using OperatingSystemLake.Implementations.Linux;
using OperatingSystemLake.Implementations.Windows;
using OSOrchestrator.Implementations;

var builder = Host.CreateApplicationBuilder(args);
// Register job classes
//builder.Services.AddScoped<ExecutableProcessingJobEnque>();
// Configure Hangfire
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseRedisStorage("192.168.99.101:6379", new RedisStorageOptions
    {
        Prefix = "hangfire:",
        ExpiryCheckInterval = TimeSpan.FromHours(1)
    }));

// Add multiple Hangfire servers (listeners) with different queue configurations

builder.Services.AddSingleton<ClamAVClient>(serviceProvider =>
{
    return new ClamAVClient("192.168.99.101", 3310);
});

// Listener 1: Critical queue only (high priority)
builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = 10;  // Number of concurrent jobs
    options.ServerName = "AsyncJobWorker";
});

// Register OS Lake connectors as singletons — they are stateless CLI wrappers, one per tech type
builder.Services.AddSingleton<OSLakeConnector>(new VirtualBoxOSLakeConnector(new WindowsProcessCommunicator()));
builder.Services.AddSingleton<OSLakeConnector>(new DockerMachineOSLakeConnector(new WindowsProcessCommunicator()));

// Factory resolves the correct DockerClient per job based on tech type — mirrors RequestBodyParser pattern
builder.Services.AddSingleton<IDockerClientFactory, DockerClientFactory>();
builder.Services.AddSingleton<ContainerBuildService>(sp => new ContainerBuildService(
    dockerClientFactory: sp.GetRequiredService<IDockerClientFactory>(),
    sidecarPublishDir: @"c:\Users\kaavin\programming\container-management\DeploymentManager\DeploymentComponents\os-process-manager-binaries\linux"));

builder.Services.AddScoped<JSProjectProcessingJobEnque>();


var host = builder.Build();


host.Run();
