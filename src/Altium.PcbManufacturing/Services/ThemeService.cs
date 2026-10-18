using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Altium.PcbManufacturing.Settings;

namespace Altium.PcbManufacturing.Services;

/// <summary>
/// Swaps the theme palette dictionary in the application resources; every brush is referenced with DynamicResource, so
/// open windows update immediately.
/// </summary>

public sealed class ThemeService : IThemeService
{
    private const string themeFolder = "Resources/Themes/";
    private const int dwmUseImmersiveDarkMode = 20;

    private readonly ResourceDictionary resources;

    public ThemeService(ResourceDictionary applicationResources)
    {
        ArgumentNullException.ThrowIfNull(applicationResources);

        resources = applicationResources;
        EventManager.RegisterClassHandler(typeof(Window), FrameworkElement.LoadedEvent, new RoutedEventHandler(OnWindowLoaded));
    }

    public AppTheme Theme { get; private set; } = AppTheme.Light;

    public void Apply(AppTheme theme)
    {
        ResourceDictionary palette = new() { Source = new Uri($"pack://application:,,,/Altium.PcbManufacturing;component/{themeFolder}{theme}.xaml") };
        int index = FindPaletteIndex();
        if (index >= 0)
        {
            resources.MergedDictionaries[index] = palette;
        }
        else
        {
            resources.MergedDictionaries.Insert(0, palette);
        }

        Theme = theme;
        foreach (Window window in Application.Current?.Windows ?? new WindowCollection())
        {
            ApplyTitleBarTheme(window);
        }
    }

    private int FindPaletteIndex()
    {
        for (int i = 0; i < resources.MergedDictionaries.Count; i++)
        {
            if (resources.MergedDictionaries[i].Source?.OriginalString.Contains(themeFolder, StringComparison.OrdinalIgnoreCase) == true)
            {
                return i;
            }
        }

        return -1;
    }

    private void OnWindowLoaded(object sender, RoutedEventArgs e) => ApplyTitleBarTheme((Window)sender);

    private void ApplyTitleBarTheme(Window window)
    {
        IntPtr handle = new WindowInteropHelper(window).Handle;
        if (handle == IntPtr.Zero)
        {
            return;
        }

        int useDark = Theme == AppTheme.Dark ? 1 : 0;
        _ = DwmSetWindowAttribute(handle, dwmUseImmersiveDarkMode, ref useDark, sizeof(int));
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
}
