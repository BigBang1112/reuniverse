﻿using Microsoft.Extensions.Hosting;
using MyDiscordBot.Configuration;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDiscordServices();

builder.Services.AddDomainServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddTelemetryServices(builder.Configuration, builder.Environment);

await builder.Build().RunAsync();