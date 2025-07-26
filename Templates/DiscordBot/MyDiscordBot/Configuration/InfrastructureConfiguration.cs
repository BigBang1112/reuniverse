using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MyDiscordBot.Options;
using MyDiscordBot.Services;

namespace MyDiscordBot.Configuration;

public static class InfrastructureConfiguration
{
    public static void AddInfrastructureServices(this IServiceCollection services, IConfiguration config)
    {
        services.AddHttpClient<IGitHubService, GitHubService>(client =>
        {
            client.DefaultRequestHeaders.UserAgent.ParseAdd("MyDiscordBot/1.0 (+https://example.com; support@example.com)");
        }).AddStandardResilienceHandler();

        services.AddSingleton(TimeProvider.System);

        services.AddOptions<DiscordOptions>()
            .Bind(config.GetSection(DiscordOptions.Discord))
            .ValidateDataAnnotations();
    }
}
