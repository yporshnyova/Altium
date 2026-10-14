using Microsoft.Extensions.DependencyInjection;

namespace Altium.PcbManufacturing;

/// <summary>
/// Composition root: the single place where application types are registered with the container.
/// </summary>
public static class ServiceRegistration
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddSingleton<MainWindow>();

        return services;
    }
}
