﻿using Microsoft.Extensions.DependencyInjection;

namespace MyDiscordBot.Configuration;

public static class DomainConfiguration
{
    public static void AddDomainServices(this IServiceCollection services)
    {
        services.AddHostedService<Startup>();

        // .. your domain services here ...
    }
}
