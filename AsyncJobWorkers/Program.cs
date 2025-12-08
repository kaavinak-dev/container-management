// See https://aka.ms/new-console-template for more information
using Hangfire;
using Hangfire.Redis.StackExchange;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Engines.FileStorageEngines.Implementations;

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
builder.Services.AddScoped<ExecutableProcessingJobEnque>();


var host = builder.Build();


host.Run();
