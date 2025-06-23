using Microsoft.Extensions.DependencyInjection;
using MyGenericHost.Services;

namespace MyGenericHost.Configuration;

public static class DomainConfiguration
{
    public static void AddDomainServices(this IServiceCollection services)
    {
        services.AddHostedService<Worker>();

        // .. your domain services here ...
    }
}
