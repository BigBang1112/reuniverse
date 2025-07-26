﻿using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using MyDiscordBot.Services;

namespace MyDiscordBot.Configuration;

public static class DiscordConfiguration
{
    public static void AddDiscordServices(this IServiceCollection services)
    {
        services.AddSingleton(new DiscordSocketConfig()
        {
            LogLevel = LogSeverity.Verbose
        });

        services.AddSingleton<DiscordSocketClient>();
        services.AddSingleton<InteractionService>(provider => new(provider.GetRequiredService<DiscordSocketClient>(), new()
        {
            LogLevel = LogSeverity.Verbose
        }));

        services.AddSingleton<IDiscordBotService, DiscordBotService>();
    }
}
