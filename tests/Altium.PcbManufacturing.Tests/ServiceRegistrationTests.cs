using System.IO;
using Altium.PcbManufacturing.Localization;
using Altium.PcbManufacturing.Models;
using Altium.PcbManufacturing.Services;
using Altium.PcbManufacturing.Settings;
using Altium.PcbManufacturing.ViewModels;
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
        IServiceCollection services = new ServiceCollection().AddApplicationServices(TestCatalogs.Create(), CreateStore(), new FakeThemeService());

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

    [Fact]
    public void AddApplicationServices_PreferencesViewModelEditsSharedPreferences()
    {
        PcbCatalog catalog = TestCatalogs.Create();
        using ServiceProvider provider = BuildProvider(catalog);
        PreferencesViewModel viewModel = provider.GetRequiredService<PreferencesViewModel>();

        viewModel.SelectedMaterial = catalog.Materials[1];

        Assert.Same(catalog.Materials[1], provider.GetRequiredService<ManufacturingPreferences>().SelectedMaterial);
    }

    [Fact]
    public void AddApplicationServices_PricingServiceUsesCatalogBasePrice()
    {
        PcbCatalog catalog = TestCatalogs.Create();
        using ServiceProvider provider = BuildProvider(catalog);

        Assert.Equal(catalog.BasePriceUsd, provider.GetRequiredService<PcbPricingService>().BasePrice);
    }

    [Fact]
    public void AddApplicationServices_SharesOneWorkflowNavigationStartingAtPreferences()
    {
        using ServiceProvider provider = BuildProvider(TestCatalogs.Create());

        WorkflowNavigationState navigation = provider.GetRequiredService<WorkflowNavigationState>();
        Assert.Equal(WorkflowStep.Preferences, navigation.CurrentStep);
        Assert.Same(navigation, provider.GetRequiredService<MainWindowViewModel>().NavigationState);
    }

    [Fact]
    public void AddApplicationServices_RegistersDialogConfirmationService() =>
        Assert.IsType<DialogConfirmationService>(
            BuildProvider(TestCatalogs.Create()).GetRequiredService<IConfirmationService>());

    [Fact]
    public void AddApplicationServices_MainWindowViewModelExposesSharedPreferencesViewModel()
    {
        using ServiceProvider provider = BuildProvider(TestCatalogs.Create());

        Assert.Same(
            provider.GetRequiredService<PreferencesViewModel>(),
            provider.GetRequiredService<MainWindowViewModel>().Preferences);
    }

    [Fact]
    public void AddApplicationServices_RegistersSharedLocalizerAndGivenUiServices()
    {
        UiSettingsStore store = CreateStore();
        FakeThemeService theme = new();
        using ServiceProvider provider = new ServiceCollection()
            .AddApplicationServices(TestCatalogs.Create(), store, theme)
            .BuildServiceProvider();

        Assert.Same(Localizer.Instance, provider.GetRequiredService<Localizer>());
        Assert.Same(store, provider.GetRequiredService<UiSettingsStore>());
        Assert.Same(theme, provider.GetRequiredService<IThemeService>());
    }

    // The store points to a file that is never written by these tests.
    private static UiSettingsStore CreateStore() =>
        new(Path.Combine(Path.GetTempPath(), $"ui-settings-{Guid.NewGuid():N}.json"));

    private static ServiceProvider BuildProvider(PcbCatalog catalog) =>
        new ServiceCollection()
            .AddApplicationServices(catalog, CreateStore(), new FakeThemeService())
            .BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true,
            });
}
