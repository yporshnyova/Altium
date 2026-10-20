using System.Runtime.ExceptionServices;
using Altium.PcbManufacturing.Localization;
using Altium.PcbManufacturing.Models;
using Altium.PcbManufacturing.Services;
using Altium.PcbManufacturing.Settings;
using Altium.PcbManufacturing.ViewModels;
using Altium.PcbManufacturing.ViewModels.Factories;

namespace Altium.PcbManufacturing.Tests;

/// <summary>
/// The wizard, its step factories and the shared draft, wired as the composition root wires them, but with a fake
/// confirmation dialog and a private <see cref="Localizer"/>.
/// </summary>
internal sealed class TestWizard
{
    public TestWizard(PcbCatalog? catalog = null)
    {
        Catalog = catalog ?? TestCatalogs.Create();
        Pricing = new PcbPricingService(Catalog);
        PreferencesFactory = new PreferencesViewModelFactory(Catalog, Confirmation, Localizer);
        QuoteFactory = new QuoteViewModelFactory(Catalog, Localizer);
        OrderFactory = new OrderViewModelFactory(Catalog, Confirmation, Localizer);
        Wizard = new OrderWizardViewModel(Catalog, Pricing, PreferencesFactory, QuoteFactory, OrderFactory, Localizer);
    }

    public PcbCatalog Catalog { get; }

    public PcbPricingService Pricing { get; }

    public ManufacturingOrderDraft Draft => Wizard.Preferences.Draft;

    public FakeConfirmationService Confirmation { get; } = new();

    public Localizer Localizer { get; } = new();

    public PreferencesViewModelFactory PreferencesFactory { get; }

    public QuoteViewModelFactory QuoteFactory { get; }

    public OrderViewModelFactory OrderFactory { get; }

    public OrderWizardViewModel Wizard { get; }

    public PreferencesViewModel Preferences => Wizard.Preferences;

    public QuoteViewModel Quote => Wizard.Quote;

    public OrderViewModel Order => Wizard.Order;

    /// <summary>
    /// Enters the only required value the defaults lack and saves, which opens Quote.
    /// </summary>
    public void CompletePreferences()
    {
        Preferences.PostcodeInput = "55-220";
        Assert.True(Wizard.SubmitPreferences());
        Assert.Equal(WorkflowStep.Quote, Wizard.CurrentStep);
    }

    /// <summary>
    /// Completes Preferences and Quote, which opens Order.
    /// </summary>
    public void CompleteQuote()
    {
        CompletePreferences();
        Assert.True(Wizard.ContinueToOrder());
        Assert.Equal(WorkflowStep.Order, Wizard.CurrentStep);
    }

    /// <summary>
    /// Completes every step, confirming the order.
    /// </summary>
    public async Task CompleteOrderAsync()
    {
        CompleteQuote();
        Assert.True(await Wizard.PlaceOrderAsync());
        Assert.True(Wizard.IsCompleted);
    }

    /// <summary>
    /// Runs the footer's primary action as a bound button does: only when it can execute.
    /// </summary>
    public Task ClickPrimaryAsync()
    {
        Assert.True(Wizard.PrimaryActionCommand.CanExecute(null), "The primary action is disabled.");
        return Wizard.PrimaryActionCommand.ExecuteAsync(null);
    }
}

/// <summary>
/// Records confirmation requests and answers them with <see cref="Answer"/>, or, for an asynchronous request while
/// <see cref="PendingAnswer"/> is set, when the test completes it.
/// </summary>
internal sealed class FakeConfirmationService : IConfirmationService
{
    public bool Answer { get; set; } = true;

    public TaskCompletionSource<bool>? PendingAnswer { get; set; }

    public List<(string Title, string Message)> Requests { get; } = [];

    public bool Confirm(string title, string message)
    {
        Requests.Add((title, message));
        return Answer;
    }

    public Task<bool> ConfirmAsync(string title, string message)
    {
        Requests.Add((title, message));
        return PendingAnswer?.Task ?? Task.FromResult(Answer);
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
