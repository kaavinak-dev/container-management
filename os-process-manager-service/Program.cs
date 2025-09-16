using Domain.Entities.Abstractions;
using Domain.Entities.Implementations.Windows;
using Domain.Ports.OSPorts;
using OSProcessManagerInfastructure.Grpc;
using OSProcessManagerInfastructure.OSInfrastructure.WindowsInfrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddGrpc();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddWindowsDomainServices();
builder.Services.AddWindowsInfrastructureServices();
builder.Services.AddGrpcServices();

builder.WebHost.ConfigureKestrel((options) =>
{
    options.ListenAnyIP(5000, listenOptions =>
    {
        listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1;
    });
    options.ListenAnyIP(5001, listenOptions =>
    {
        listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http2;
    });
});

builder.WebHost.ConfigureServices((services) =>
{
    services.AddGrpc();
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();

//app.UseHttpsRedirection();

app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
    endpoints.MapGrpcService<ApplyService>();
});

app.Run();
