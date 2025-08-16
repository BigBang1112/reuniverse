using MyWebApi.Endpoints.GitHub;

namespace MyWebApi.Endpoints;

public static class GitHubEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        GitHubUserEndpoint.Map(group.MapGroup("/user"));
    }
}
