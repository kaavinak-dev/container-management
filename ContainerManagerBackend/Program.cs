using ContainerManagerBackend.Helpers;
using ContainerManagerBackend.Services;
using Engines.FileStorageEngines;
using Engines.FileStorageEngines.ContainerBuild;
using Engines.FileStorageEngines.Resources;
using OperatingSystemHelpers.Abstractions;
using OperatingSystemHelpers.Implementations.Windows;
using OperatingSystemHelpers.Implementations.Linux;
using OperatingSystemLake.Abstractions;
using OperatingSystemLake.Implementations.Linux;
using OperatingSystemLake.Implementations.Local;
using OperatingSystemLake.Implementations.Windows;
using System.Runtime.InteropServices;
using OSOrchestrator.Abstractions;
using Engines.DeploymentTracking;
using Engines.FileStorageEngines;
using Engines.DataBaseStorageEngines;
using Engines.DataBaseStorageEngines.Abstractions;
using Engines.DataBaseStorageEngines.Implementations;
using Hangfire;
using Hangfire.Redis.StackExchange;
using Engines.FileStorageEngines.Implementations;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
var builder = WebApplication.CreateBuilder(args);

// Selects the correct process communicator for the current OS at startup
static ProcessCommunicator CreatePlatformCommunicator() =>
    RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
        ? new WindowsProcessCommunicator()
        : (ProcessCommunicator)new LinuxProcessCommunicator();

// Add services to the container.
//var singletonPreConfigureServicesBuilder = new SingletonServicesPreConfigurationBuilder();
//singletonPreConfigureServicesBuilder.ConfigureOSLakeOrchestrator(OperatingSystemLake.Constants.OSLakeTechTypes.VirtualBox).ConfigureOSOrchestrator(OSOrchestrator.Constants.OSOrchestratorTypes.Docker);
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
//builder.Services.AddSingleton<OSLakeOrchestrator>(singletonPreConfigureServicesBuilder.OSLakeOrchestrator);
//builder.Services.AddSingleton<OSOrchestrator.Abstractions.OSOrchestrator>(singletonPreConfigureServicesBuilder.OSOrchestrator);
builder.Services.AddSingleton<OSLakeConnector>(new VirtualBoxOSLakeConnector(CreatePlatformCommunicator()));
builder.Services.AddSingleton<OSLakeConnector>(new DockerMachineOSLakeConnector(CreatePlatformCommunicator()));
builder.Services.AddSingleton<OSLakeConnector>(new LocalDockerOSLakeConnector());
builder.Services.AddScoped<RequestBodyParser>();
//builder.Services.AddSingleton<FileStorageManager>();
builder.Services.AddSingleton<ProjectStorageManager>(serviceProvider =>
{
    //var config = serviceProvider.GetRequiredService<IConfiguration>();
    var fileServers = builder.Configuration
        .GetSection("MinioServers")
        .Get<List<Dictionary<string, string>>>()!;
    return new ProjectStorageManager(fileServers, isDevEnv: builder.Environment.IsDevelopment());

});
builder.Services.AddHostedService<ProjectStorageEngineBackgroundService>();
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseRedisStorage(builder.Configuration.GetConnectionString("Redis")!, new RedisStorageOptions
    {
        Prefix = "hangfire:",
        ExpiryCheckInterval = TimeSpan.FromHours(1)
    }));
builder.Services.AddDbContext<ProjectDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")!));
builder.Services.AddScoped<IMetadataStorageEngine, PostgresMetadataStorageEngine>();

// Editor session management
builder.Services.AddSingleton<IDockerClientFactory, DockerClientFactory>();
builder.Services.AddScoped<ProjectFabricService>();
builder.Services.AddScoped<ResourceProvisionerService>();
builder.Services.AddScoped<SdkInjectionService>();
builder.Services.AddScoped<EditorContainerService>();
builder.Services.AddHostedService<EditorVolumeCleanupService>();
builder.Services.AddHostedService<FabricCleanupService>();
//builder.Services.AddScoped<ExecutableProcessingJobEnque>();

var redisConnection = ConnectionMultiplexer.Connect(
    builder.Configuration.GetConnectionString("Redis")!);
builder.Services.AddSingleton<IConnectionMultiplexer>(redisConnection);
builder.Services.AddSingleton<IDeploymentProgressTracker, RedisDeploymentProgressTracker>();

var app = builder.Build();

// Approach A: auto-migrate DB on startup — creates all tables fresh on first boot, no-op after
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();
    await db.Database.MigrateAsync();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

app.Run();
