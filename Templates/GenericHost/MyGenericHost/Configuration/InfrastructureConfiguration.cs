using Microsoft.Extensions.DependencyInjection;
using MyGenericHost.Services;

namespace MyGenericHost.Configuration;

public static class InfrastructureConfiguration
{
    public static void AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddHttpClient<IGitHubService, GitHubService>(client =>
        {
            client.DefaultRequestHeaders.UserAgent.ParseAdd("MyGenericHost/1.0 (+https://example.com; support@example.com)");
        }).AddStandardResilienceHandler();

        services.AddSingleton(TimeProvider.System);
    }
}
