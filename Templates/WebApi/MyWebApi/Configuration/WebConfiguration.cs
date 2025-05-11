using MyWebApi.Services;

namespace MyWebApi.Configuration;

public static class WebConfiguration
{
    public static void AddWebServices(this IServiceCollection services, IConfiguration config)
    {
        services.AddHttpClient<IGitHubService, GitHubService>(client =>
        {
            client.DefaultRequestHeaders.UserAgent.ParseAdd("MyWebApi/1.0 (+https://example.com; support@example.com)");
        }).AddStandardResilienceHandler();

        services.AddAuthentication();
        services.AddAuthorization();

        services.AddOpenApi();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = 429;
        });

        services.AddHealthChecks();

        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
        });

        services.AddSingleton(TimeProvider.System);
    }
}
