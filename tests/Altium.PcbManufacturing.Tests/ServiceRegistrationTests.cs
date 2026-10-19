using System.IO;
using Altium.PcbManufacturing.Models;
using Altium.PcbManufacturing.Settings;
using Altium.PcbManufacturing.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Altium.PcbManufacturing.Tests;

/// <summary>
/// The composition root: the container validates and the screens share one state and one workflow.
/// </summary>
public sealed class ServiceRegistrationTests
{
    [Fact]
    public void AddApplicationServices_BuildsAValidProviderWhoseScreensShareOneState()
    {
        PcbCatalog catalog = TestCatalogs.Create();
        // The store points to a file that this test never writes.
        UiSettingsStore store = new(Path.Combine(Path.GetTempPath(), $"ui-settings-{Guid.NewGuid():N}.json"));
        using ServiceProvider provider = new ServiceCollection()
            .AddApplicationServices(catalog, store, new FakeThemeService())
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        MainWindowViewModel main = provider.GetRequiredService<MainWindowViewModel>();

        main.Preferences.SelectedMaterial = catalog.Materials[1];

        Assert.Same(provider.GetRequiredService<WorkflowNavigationState>(), main.NavigationState);
        Assert.Same(catalog.Materials[1], provider.GetRequiredService<ManufacturingPreferences>().SelectedMaterial);
        Assert.Same(catalog.Materials[1], main.Quote.SelectedMaterial);
    }
}
