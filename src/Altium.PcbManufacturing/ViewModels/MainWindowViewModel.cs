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
public sealed partial class MainWindowViewModel : ObservableObject
{
    private readonly Localizer localizer;
    private readonly IThemeService themeService;
    private readonly UiSettingsStore settingsStore;

    public MainWindowViewModel(
        PreferencesViewModel preferences,
        QuoteViewModel quote,
        WorkflowNavigationState navigationState,
        Localizer localizer,
        IThemeService themeService,
        UiSettingsStore settingsStore)
    {
        ArgumentNullException.ThrowIfNull(preferences);
        ArgumentNullException.ThrowIfNull(quote);
        ArgumentNullException.ThrowIfNull(navigationState);
        ArgumentNullException.ThrowIfNull(localizer);
        ArgumentNullException.ThrowIfNull(themeService);
        ArgumentNullException.ThrowIfNull(settingsStore);

        Preferences = preferences;
        Quote = quote;
        NavigationState = navigationState;
        this.localizer = localizer;
        this.themeService = themeService;
        this.settingsStore = settingsStore;
        this.localizer.PropertyChanged += OnLocalizerPropertyChanged;
    }

    public PreferencesViewModel Preferences { get; }

    public QuoteViewModel Quote { get; }

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
