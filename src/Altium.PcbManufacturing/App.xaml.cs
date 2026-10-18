using System.Windows;
using Altium.PcbManufacturing.Localization;
using Altium.PcbManufacturing.Models;
using Altium.PcbManufacturing.Services;
using Altium.PcbManufacturing.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace Altium.PcbManufacturing;

public partial class App : Application
{
    private ServiceProvider? serviceProvider;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Restore language and theme first, so everything shown from here on (even a catalog error) uses them.
        UiSettingsStore settingsStore = new(UiSettingsStore.DefaultFilePath);
        UiSettings uiSettings = settingsStore.Load();
        Localizer.Instance.SetLanguage(uiSettings.Language);
        ThemeService themeService = new(Resources);
        themeService.Apply(uiSettings.Theme);

        PcbCatalog catalog;
        try
        {
            catalog = await new PcbCatalogLoader(PcbCatalogLoader.DefaultCatalogPath).LoadAsync();
        }
        catch (CatalogLoadException ex)
        {
            MessageBox.Show(
                ex.Message, Localizer.Instance[nameof(Strings.App_Title)], MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
            return;
        }

        serviceProvider = new ServiceCollection()
            .AddApplicationServices(catalog, settingsStore, themeService)
            .BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true,
            });

        MainWindow mainWindow = serviceProvider.GetRequiredService<MainWindow>();
        MainWindow = mainWindow;
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        serviceProvider?.Dispose();
        base.OnExit(e);
    }
}
