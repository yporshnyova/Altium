using Microsoft.Extensions.DependencyInjection;

namespace Altium.PcbManufacturing.Tests;

public sealed class ServiceServiceRegistrationTests
{
    [Fact]
    public void AddPcbManufacturing_BuildsValidServiceProvider()
    {
        IServiceCollection services = new ServiceCollection().AddApplicationServices();

        using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });

        Assert.NotNull(provider);
    }

    [Fact]
    public void AddPcbManufacturing_RegistersMainWindowAsSingleton()
    {
        IServiceCollection services = new ServiceCollection().AddApplicationServices();

        ServiceDescriptor descriptor = Assert.Single(services, d => d.ServiceType == typeof(MainWindow));
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }
}
