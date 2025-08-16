using MyWebApi.Endpoints;

namespace MyWebApi.Configuration;

public static class EndpointConfiguration
{
    public static void MapEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/", async (context) =>
        {
            await context.Response.WriteAsJsonAsync(new
            {
                message = "Welcome to MyWebApi!"
            });
        });

        GitHubEndpoint.Map(app.MapGroup("/github"));
    }
}
