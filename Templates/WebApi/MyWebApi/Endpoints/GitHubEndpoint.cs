using MyWebApi.Endpoints.GitHub;

namespace MyWebApi.Endpoints;

public static class GitHubEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/", async (context) =>
        {
            await context.Response.WriteAsJsonAsync(new
            {
                message = "Welcome to MyWebApi!"
            });
        });

        GitHubUserEndpoint.Map(group.MapGroup("/user"));
    }
}
