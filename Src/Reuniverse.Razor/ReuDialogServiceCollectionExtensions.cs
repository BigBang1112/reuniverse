using Microsoft.Extensions.DependencyInjection;

namespace Reuniverse.Razor;

/// <summary>
/// Registers <see cref="ReuDialogService"/>.
/// </summary>
public static class ReuDialogServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="ReuDialogService"/> as scoped, so that each user (circuit) has their own.
    /// Render a <see cref="ReuDialogHost"/> in the layout to show its questions.
    /// </summary>
    public static IServiceCollection AddReuDialogs(this IServiceCollection services)
    {
        services.AddScoped<ReuDialogService>();
        return services;
    }
}
