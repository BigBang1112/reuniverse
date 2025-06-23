using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MyGenericHost.Services;

public sealed class Worker : BackgroundService
{
    private readonly IGitHubService githubService;
    private readonly ILogger<Worker> logger;

    private readonly PeriodicTimer timer = new(TimeSpan.FromSeconds(10));

    public Worker(IGitHubService githubService, ILogger<Worker> logger)
    {
        this.githubService = githubService;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        do
        {
            try
            {
                var user = await githubService.GetUserAsync("dotnet", stoppingToken);

                if (user is not null)
                {
                    logger.LogInformation("User {Login} has {Followers} followers", user.Login, user.Followers);
                }
                else
                {
                    logger.LogWarning("User not found");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while fetching user data");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
