using System.Runtime.ExceptionServices;
using Altium.PcbManufacturing.Localization;
using Altium.PcbManufacturing.Models;
using Altium.PcbManufacturing.Services;
using Altium.PcbManufacturing.Settings;
using Altium.PcbManufacturing.ViewModels;

namespace Altium.PcbManufacturing.Tests;

/// <summary>
/// The Preferences and Quote screens wired to one shared state and one workflow, as the composition root wires them,
/// but with a fake confirmation dialog and a private <see cref="Localizer"/>.
/// </summary>
internal sealed class TestScreens
{
    public TestScreens(PcbCatalog? catalog = null)
    {
        Catalog = catalog ?? TestCatalogs.Create();
        State = new ManufacturingPreferences(Catalog);
        PcbPricingService pricing = new(Catalog);
        Preferences = new PreferencesViewModel(Catalog, State, pricing, Navigation, Confirmation, Localizer);
        Quote = new QuoteViewModel(Catalog, State, pricing, Navigation, Localizer);
    }

    public PcbCatalog Catalog { get; }

    public ManufacturingPreferences State { get; }

    public WorkflowNavigationState Navigation { get; } = new();

    public FakeConfirmationService Confirmation { get; } = new();

    public Localizer Localizer { get; } = new();

    public PreferencesViewModel Preferences { get; }

    public QuoteViewModel Quote { get; }

    /// <summary>
    /// Enters the only required value the defaults lack and saves, which opens Quote.
    /// </summary>
    public void CompletePreferences()
    {
        Preferences.Postcode = "55-220";
        Preferences.SaveAndContinueCommand.Execute(null);
        Assert.Equal(WorkflowStep.Quote, Navigation.CurrentStep);
    }
}

/// <summary>
/// Records confirmation requests and answers them with <see cref="Answer"/>.
/// </summary>
internal sealed class FakeConfirmationService : IConfirmationService
{
    public bool Answer { get; set; } = true;

    public List<(string Title, string Message)> Requests { get; } = [];

    public bool Confirm(string title, string message)
    {
        Requests.Add((title, message));
        return Answer;
    }
}

/// <summary>
/// Records applied themes without touching WPF resources.
/// </summary>
internal sealed class FakeThemeService : IThemeService
{
    public AppTheme Theme { get; private set; } = AppTheme.Dark;

    public List<AppTheme> Applied { get; } = [];

    public void Apply(AppTheme theme)
    {
        Applied.Add(theme);
        Theme = theme;
    }
}

/// <summary>
/// Runs a test that creates WPF elements on its own STA thread, as WPF requires.
/// </summary>
internal static class Sta
{
    public static void Run(Action test)
    {
        ExceptionDispatchInfo? failure = null;
        Thread thread = new(() =>
        {
            try
            {
                test();
            }
            catch (Exception ex)
            {
                failure = ExceptionDispatchInfo.Capture(ex);
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        failure?.Throw();
    }
}
