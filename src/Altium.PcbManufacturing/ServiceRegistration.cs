using Altium.PcbManufacturing.Localization;
using Altium.PcbManufacturing.Models;
using Altium.PcbManufacturing.Services;
using Altium.PcbManufacturing.Settings;
using Altium.PcbManufacturing.ViewModels;
using Altium.PcbManufacturing.ViewModels.Factories;
using Altium.PcbManufacturing.Views;
using Microsoft.Extensions.DependencyInjection;

namespace Altium.PcbManufacturing;

/// <summary>
/// Composition root: the single place where application types are registered with the container.
/// </summary>
public static class ServiceRegistration
{
    /// <param name="catalog">The catalog loaded and validated at startup, before the container is built.</param>
    /// <param name="settingsStore">The UI settings store, already used at startup to restore language and theme.</param>
    /// <param name="themeService">The theme service, already applied at startup.</param>
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        PcbCatalog catalog,
        UiSettingsStore settingsStore,
        IThemeService themeService)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(settingsStore);
        ArgumentNullException.ThrowIfNull(themeService);

        services.AddSingleton(catalog);
        services.AddSingleton<PcbPricingService>();
        services.AddSingleton<IConfirmationService, DialogConfirmationService>();

        // UI-wide services. The localizer instance is the one XAML markup extensions bind to.
        services.AddSingleton(Localizer.Instance);
        services.AddSingleton(settingsStore);
        services.AddSingleton(themeService);

        // One main window per application, so its view models share the window's (and the draft's) lifetime. The
        // wizard creates each step once through its factory and keeps it; the steps themselves are not registered.
        services.AddSingleton<MainWindow>();
        services.AddSingleton<MainWindowViewModel>();

        // The draft is not registered: each wizard creates and owns its own.
        services.AddSingleton<OrderWizardViewModel>();

        services.AddSingleton<PreferencesViewModelFactory>();
        services.AddSingleton<QuoteViewModelFactory>();
        services.AddSingleton<OrderViewModelFactory>();

        return services;
    }
}
