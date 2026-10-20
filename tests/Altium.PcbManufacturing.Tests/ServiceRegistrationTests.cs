using System.IO;
using Altium.PcbManufacturing.Models;
using Altium.PcbManufacturing.Settings;
using Altium.PcbManufacturing.ViewModels;
using Altium.PcbManufacturing.ViewModels.Factories;
using Microsoft.Extensions.DependencyInjection;

namespace Altium.PcbManufacturing.Tests;

/// <summary>
/// The composition root: the container validates, one wizard owns one instance of each step, and every step shares the
/// wizard's session draft.
/// </summary>
public sealed class ServiceRegistrationTests
{
    [Fact]
    public void AddApplicationServices_BuildsOneWizardWhoseStepsShareItsSessionDraft()
    {
        PcbCatalog catalog = TestCatalogs.Create();
        // The store points to a file that this test never writes.
        UiSettingsStore store = new(Path.Combine(Path.GetTempPath(), $"ui-settings-{Guid.NewGuid():N}.json"));
        using ServiceProvider provider = new ServiceCollection()
            .AddApplicationServices(catalog, store, new FakeThemeService())
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        MainWindowViewModel main = provider.GetRequiredService<MainWindowViewModel>();
        OrderWizardViewModel wizard = provider.GetRequiredService<OrderWizardViewModel>();
        ManufacturingOrderDraft draft = wizard.Preferences.Draft;
        Assert.Null(provider.GetService<ManufacturingOrderDraft>());

        Assert.Same(wizard, main.Wizard);
        Assert.Same(draft, wizard.Preferences.Draft);
        Assert.Same(draft, wizard.Quote.Draft);
        Assert.Same(draft, wizard.Order.Draft);
        Assert.Same(draft, provider.GetRequiredService<PreferencesViewModelFactory>().Create(draft).Draft);
        // Steps are created only by the wizard, through the factories, so the container never hands out another one.
        Assert.Null(provider.GetService<PreferencesViewModel>());
        Assert.Null(provider.GetService<QuoteViewModel>());
        Assert.Null(provider.GetService<OrderViewModel>());

        wizard.Preferences.QuantityInput = "4";

        Assert.Equal("4 pcs.", wizard.Quote.ParameterGroups.SelectMany(g => g.Parameters).Single(p => p.Label == "Quantity").Value);
    }
}
