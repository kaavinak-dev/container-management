using ContainerManagerBackend.Helpers;
using ContainerManagerBackend.Services;
using OperatingSystemHelpers.Abstractions;
using OperatingSystemHelpers.Implementations.Windows;
using OperatingSystemHelpers.Implementations.Linux;
using OperatingSystemLake.Abstractions;
using OperatingSystemLake.Implementations.Linux;
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
builder.Services.AddScoped<RequestBodyParser>();
//builder.Services.AddSingleton<FileStorageManager>();
builder.Services.AddSingleton<ProjectStorageManager>(serviceProvider =>
{
    //var config = serviceProvider.GetRequiredService<IConfiguration>();
    List<Dictionary<string, string>> fileServers = new List<Dictionary<string, string>>()
    {
        new Dictionary<string, string>(){
            {"Url", "192.168.99.101"},
            {"Port", "9002"},
            {"AccessKey", "minioadmin"},
            {"SecretKey","minioadmin"}
        },
        new Dictionary<string,string>(){
            {"Url", "192.168.99.101"},
            {"Port", "9003"},
            {"AccessKey","minioadmin"},
            {"SecretKey","minioadmin"}
        }
    };
    return new ProjectStorageManager(fileServers, isDevEnv: builder.Environment.IsDevelopment());

});
builder.Services.AddHostedService<ProjectStorageEngineBackgroundService>();
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseRedisStorage("192.168.99.101:6379", new RedisStorageOptions
    {
        Prefix = "hangfire:",
        ExpiryCheckInterval = TimeSpan.FromHours(1)
    }));
builder.Services.AddDbContext<ProjectDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")
        ?? "Host=192.168.99.101;Port=5432;Database=container_management;Username=admin;Password=admin123"));
builder.Services.AddScoped<IMetadataStorageEngine, PostgresMetadataStorageEngine>();
//builder.Services.AddScoped<ExecutableProcessingJobEnque>();

var redisConnection = ConnectionMultiplexer.Connect(
    builder.Configuration.GetConnectionString("Redis") ?? "192.168.99.101:6379");
builder.Services.AddSingleton<IConnectionMultiplexer>(redisConnection);
builder.Services.AddSingleton<IDeploymentProgressTracker, RedisDeploymentProgressTracker>();

var app = builder.Build();

// Approach A: auto-migrate DB on startup — creates all tables fresh on first boot, no-op after
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<IMetadataStorageEngine>();
    await db.MigrateAsync();
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
