using Altium.PcbManufacturing.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Altium.PcbManufacturing;

/// <summary>
/// Composition root: the single place where application types are registered with the container.
/// </summary>
public static class ServiceRegistration
{
    /// <param name="catalog">The catalog loaded and validated at startup, before the container is built.</param>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, PcbCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        services.AddSingleton(catalog);
        services.AddSingleton<ManufacturingPreferences>();
        services.AddSingleton<MainWindow>();

        return services;
    }
}
