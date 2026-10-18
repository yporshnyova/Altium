using Altium.PcbManufacturing.Settings;

namespace Altium.PcbManufacturing.Services;

/// <summary>
/// Switches the colour theme; keeps view models free of WPF resource dictionaries.
/// </summary>
public interface IThemeService
{
    AppTheme Theme { get; }

    void Apply(AppTheme theme);
}
