using Microsoft.Extensions.Hosting;
using MyDiscordBot.Services;

namespace MyDiscordBot;

public sealed class Startup : IHostedService
{
    private readonly IDiscordBotService bot;

    public Startup(IDiscordBotService bot)
    {
        this.bot = bot;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await bot.StartAsync();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await bot.StopAsync();
    }
}
