using MyWebApi.Endpoints;

namespace MyWebApi.Configuration;

public static class EndpointConfiguration
{
    public static void MapEndpoints(this IEndpointRouteBuilder app)
    {
        GitHubEndpoint.Map(app.MapGroup("/github"));
    }
}
