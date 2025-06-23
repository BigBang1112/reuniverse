using Microsoft.Extensions.Hosting;
using MyGenericHost.Configuration;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDomainServices();
builder.Services.AddInfrastructureServices();
builder.Services.AddTelemetryServices(builder.Configuration, builder.Environment);

await builder.Build().RunAsync();