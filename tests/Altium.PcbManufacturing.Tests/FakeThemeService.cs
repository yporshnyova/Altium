using Altium.PcbManufacturing.Services;
using Altium.PcbManufacturing.Settings;

namespace Altium.PcbManufacturing.Tests;

/// <summary>
/// Records applied themes without touching WPF resources.
/// </summary>
internal sealed class FakeThemeService(AppTheme initial = AppTheme.Dark) : IThemeService
{
    public AppTheme Theme { get; private set; } = initial;

    public List<AppTheme> Applied { get; } = [];

    public void Apply(AppTheme theme)
    {
        Applied.Add(theme);
        Theme = theme;
    }
}
