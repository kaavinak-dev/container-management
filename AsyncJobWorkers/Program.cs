// See https://aka.ms/new-console-template for more information
using Hangfire;
using Hangfire.Redis.StackExchange;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Engines.DeploymentTracking;
using Engines.FileStorageEngines.ContainerBuild;
using Engines.FileStorageEngines.Implementations;
using Engines.DataBaseStorageEngines;
using Engines.DataBaseStorageEngines.Abstractions;
using Engines.DataBaseStorageEngines.Implementations;
using OperatingSystemHelpers.Abstractions;
using OperatingSystemHelpers.Implementations.Windows;
using OperatingSystemHelpers.Implementations.Linux;
using OperatingSystemLake.Abstractions;
using OperatingSystemLake.Constants;
using OperatingSystemLake.Factory;
using OperatingSystemLake.Implementations.Linux;
using OperatingSystemLake.Implementations.Windows;
using OSOrchestrator.Implementations;
using System.Runtime.InteropServices;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

var builder = Host.CreateApplicationBuilder(args);

// Selects the correct process communicator for the current OS at startup
static ProcessCommunicator CreatePlatformCommunicator() =>
    RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
        ? new WindowsProcessCommunicator()
        : (ProcessCommunicator)new LinuxProcessCommunicator();
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
builder.Services.AddSingleton<OSLakeConnector>(new VirtualBoxOSLakeConnector(CreatePlatformCommunicator()));
builder.Services.AddSingleton<OSLakeConnector>(new DockerMachineOSLakeConnector(CreatePlatformCommunicator()));

// Register ProcessCommunicator so it can be injected into job classes (e.g. JSProjectProcessingJobEnque)
builder.Services.AddTransient<ProcessCommunicator>(_ => CreatePlatformCommunicator());

// Factory resolves the correct DockerClient per job based on tech type — mirrors RequestBodyParser pattern
builder.Services.AddSingleton<IDockerClientFactory, DockerClientFactory>();
builder.Services.AddSingleton<ContainerBuildService>(sp => new ContainerBuildService(
    dockerClientFactory: sp.GetRequiredService<IDockerClientFactory>(),
    sidecarPublishDir: builder.Configuration["SidecarPublishDir"]
        ?? throw new InvalidOperationException("SidecarPublishDir is not configured")));

builder.Services.AddDbContext<ProjectDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")
        ?? "Host=192.168.99.101;Port=5432;Database=container_management;Username=admin;Password=admin123"));
builder.Services.AddScoped<IMetadataStorageEngine, PostgresMetadataStorageEngine>();

var redisConnection = ConnectionMultiplexer.Connect(
    builder.Configuration.GetConnectionString("Redis") ?? "192.168.99.101:6379");
builder.Services.AddSingleton<IConnectionMultiplexer>(redisConnection);
builder.Services.AddSingleton<IDeploymentProgressTracker, RedisDeploymentProgressTracker>();

builder.Services.AddScoped<JSProjectProcessingJobEnque>();


var host = builder.Build();


host.Run();
