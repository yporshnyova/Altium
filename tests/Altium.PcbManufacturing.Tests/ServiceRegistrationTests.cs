using Altium.PcbManufacturing.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Altium.PcbManufacturing.Tests;

public sealed class ServiceRegistrationTests
{
    [Fact]
    public void AddApplicationServices_BuildsValidServiceProvider()
    {
        using ServiceProvider provider = BuildProvider(TestCatalogs.Create());

        Assert.NotNull(provider);
    }

    [Fact]
    public void AddApplicationServices_RegistersMainWindowAsSingleton()
    {
        IServiceCollection services = new ServiceCollection().AddApplicationServices(TestCatalogs.Create());

        ServiceDescriptor descriptor = Assert.Single(services, d => d.ServiceType == typeof(MainWindow));
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    [Fact]
    public void AddApplicationServices_RegistersLoadedCatalogInstance()
    {
        PcbCatalog catalog = TestCatalogs.Create();
        using ServiceProvider provider = BuildProvider(catalog);

        Assert.Same(catalog, provider.GetRequiredService<PcbCatalog>());
    }

    [Fact]
    public void AddApplicationServices_SharesOneManufacturingPreferencesInstance()
    {
        using ServiceProvider provider = BuildProvider(TestCatalogs.Create());

        Assert.Same(
            provider.GetRequiredService<ManufacturingPreferences>(),
            provider.GetRequiredService<ManufacturingPreferences>());
    }

    private static ServiceProvider BuildProvider(PcbCatalog catalog) =>
        new ServiceCollection()
            .AddApplicationServices(catalog)
            .BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true,
            });
}
