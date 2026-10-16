using System.Windows;
using Altium.PcbManufacturing.Models;
using Altium.PcbManufacturing.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Altium.PcbManufacturing;

public partial class App : Application
{
    private ServiceProvider? serviceProvider;

    // async void is limited to this Startup event override; every failure is handled here.
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        PcbCatalog catalog;
        try
        {
            catalog = await new PcbCatalogLoader(PcbCatalogLoader.DefaultCatalogPath).LoadAsync();
        }
        catch (CatalogLoadException ex)
        {
            MessageBox.Show(ex.Message, "PCB Manufacturing", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
            return;
        }

        serviceProvider = new ServiceCollection()
            .AddApplicationServices(catalog)
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
