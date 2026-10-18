using Altium.PcbManufacturing.Localization;

namespace Altium.PcbManufacturing.Settings;

/// <summary>
/// The colour themes.
/// </summary>
public enum AppTheme
{
    Light,
    Dark,
}

/// <summary>
/// Per-user interface preferences that survive a restart. Manufacturing preferences are deliberately not included.
/// </summary>
public sealed record UiSettings(AppLanguage Language, AppTheme Theme)
{
    public static UiSettings Default { get; } = new(AppLanguage.English, AppTheme.Dark);
}
