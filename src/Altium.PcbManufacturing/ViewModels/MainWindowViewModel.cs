using System.ComponentModel;
using Altium.PcbManufacturing.Localization;
using Altium.PcbManufacturing.Services;
using Altium.PcbManufacturing.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Altium.PcbManufacturing.ViewModels;

/// <summary>
/// Root data context of the main window: one view model per tab, the workflow navigation, and the header's language
/// and theme settings.
/// </summary>
/// <remarks>
/// Language and theme changes take effect immediately and are saved right away; they never touch the manufacturing
/// preferences. 
/// </remarks>
public sealed partial class MainWindowViewModel : ObservableObject
{
    private readonly Localizer localizer;
    private readonly IThemeService themeService;
    private readonly UiSettingsStore settingsStore;

    public MainWindowViewModel(
        PreferencesViewModel preferences,
        WorkflowNavigationState navigationState,
        Localizer localizer,
        IThemeService themeService,
        UiSettingsStore settingsStore)
    {
        ArgumentNullException.ThrowIfNull(preferences);
        ArgumentNullException.ThrowIfNull(navigationState);
        ArgumentNullException.ThrowIfNull(localizer);
        ArgumentNullException.ThrowIfNull(themeService);
        ArgumentNullException.ThrowIfNull(settingsStore);

        Preferences = preferences;
        NavigationState = navigationState;
        this.localizer = localizer;
        this.themeService = themeService;
        this.settingsStore = settingsStore;
        this.localizer.PropertyChanged += OnLocalizerPropertyChanged;
    }

    public PreferencesViewModel Preferences { get; }

    public WorkflowNavigationState NavigationState { get; }

    public IReadOnlyList<LanguageOption> Languages => Localizer.SupportedLanguages;

    public LanguageOption? SelectedLanguage
    {
        get => Languages.First(option => option.Language == localizer.Language);
        set
        {
            if (value is null || value.Language == localizer.Language)
            {
                return;
            }

            localizer.SetLanguage(value.Language);
            SaveSettings();
        }
    }

    /// <summary>
    /// Whether the dark theme is active; the header toggle offers the other theme.
    /// </summary>
    public bool IsDarkTheme => themeService.Theme == AppTheme.Dark;

    [RelayCommand]
    private void ToggleTheme()
    {
        themeService.Apply(IsDarkTheme ? AppTheme.Light : AppTheme.Dark);
        OnPropertyChanged(nameof(IsDarkTheme));
        SaveSettings();
    }

    private void SaveSettings() => settingsStore.Save(new UiSettings(localizer.Language, themeService.Theme));

    private void OnLocalizerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Localizer.Language))
        {
            OnPropertyChanged(nameof(SelectedLanguage));
        }
    }
}
