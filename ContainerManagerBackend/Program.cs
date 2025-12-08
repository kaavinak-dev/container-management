using ContainerManagerBackend.Helpers;
using ContainerManagerBackend.Services;
using OperatingSystemHelpers.Implementations.Windows;
using OperatingSystemLake.Abstractions;
using OperatingSystemLake.Implementations.Windows;
using OSOrchestrator.Abstractions;
using Engines.FileStorageEngines;
using Hangfire;
using Hangfire.Redis.StackExchange;
using Engines.FileStorageEngines.Implementations;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
//var singletonPreConfigureServicesBuilder = new SingletonServicesPreConfigurationBuilder();
//singletonPreConfigureServicesBuilder.ConfigureOSLakeOrchestrator(OperatingSystemLake.Constants.OSLakeTechTypes.VirtualBox).ConfigureOSOrchestrator(OSOrchestrator.Constants.OSOrchestratorTypes.Docker);
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
//builder.Services.AddSingleton<OSLakeOrchestrator>(singletonPreConfigureServicesBuilder.OSLakeOrchestrator);
//builder.Services.AddSingleton<OSOrchestrator.Abstractions.OSOrchestrator>(singletonPreConfigureServicesBuilder.OSOrchestrator);
builder.Services.AddScoped<RequestBodyParser>();
//builder.Services.AddSingleton<FileStorageManager>();
builder.Services.AddSingleton<FileStorageManager>(serviceProvider =>
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
    return new FileStorageManager(fileServers);

});
builder.Services.AddHostedService<FileStorageEngineBackgroundService>();
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseRedisStorage("192.168.99.101:6379", new RedisStorageOptions
    {
        Prefix = "hangfire:",
        ExpiryCheckInterval = TimeSpan.FromHours(1)
    }));
//builder.Services.AddScoped<ExecutableProcessingJobEnque>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

app.Run();
