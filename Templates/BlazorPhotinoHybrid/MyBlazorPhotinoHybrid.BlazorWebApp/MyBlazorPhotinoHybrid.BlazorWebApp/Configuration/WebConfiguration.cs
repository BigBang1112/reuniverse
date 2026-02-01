using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using System.Net;

namespace MyBlazorPhotinoHybrid.BlazorWebApp.Configuration;

internal static class WebConfiguration
{
    public static void AddWebServices(this IServiceCollection services, IConfiguration config, IHostEnvironment environment)
    {
        services.AddRazorComponents(options =>
        {
            options.DetailedErrors = environment.IsDevelopment();
        })
            .AddInteractiveServerComponents()
            .AddInteractiveWebAssemblyComponents();

        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();
        });

        services.AddSingleton(TimeProvider.System);

        // Figures out HTTPS behind proxies
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders =
                ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

            foreach (var knownProxy in config.GetSection("KnownProxies").Get<string[]>() ?? [])
            {
                if (IPAddress.TryParse(knownProxy, out var ipAddress))
                {
                    options.KnownProxies.Add(ipAddress);
                    continue;
                }

                foreach (var hostIpAddress in Dns.GetHostAddresses(knownProxy))
                {
                    options.KnownProxies.Add(hostIpAddress);
                }
            }
        });
    }
}