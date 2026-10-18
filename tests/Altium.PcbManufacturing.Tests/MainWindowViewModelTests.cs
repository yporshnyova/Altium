using System.IO;
using Altium.PcbManufacturing.Localization;
using Altium.PcbManufacturing.Models;
using Altium.PcbManufacturing.Services;
using Altium.PcbManufacturing.Settings;
using Altium.PcbManufacturing.ViewModels;

namespace Altium.PcbManufacturing.Tests;

[Collection(UiCultureCollection.Name)]
public sealed class MainWindowViewModelTests : UiCultureTest, IDisposable
{
    private readonly PcbCatalog catalog = TestCatalogs.Create();
    private readonly ManufacturingPreferences preferences;
    private readonly Localizer localizer = new();
    private readonly FakeThemeService theme = new();
    private readonly string settingsPath = Path.Combine(Path.GetTempPath(), $"ui-settings-{Guid.NewGuid():N}.json");
    private readonly UiSettingsStore store;
    private readonly PreferencesViewModel preferencesViewModel;
    private readonly MainWindowViewModel viewModel;
    private readonly List<string?> changed = [];

    public MainWindowViewModelTests()
    {
        preferences = new ManufacturingPreferences(catalog);
        store = new UiSettingsStore(settingsPath);
        preferencesViewModel = new PreferencesViewModel(
            catalog, preferences, new PcbPricingService(catalog), new WorkflowNavigationState(), new FakeConfirmationService(), localizer);
        viewModel = new MainWindowViewModel(preferencesViewModel, new WorkflowNavigationState(), localizer, theme, store);
        viewModel.PropertyChanged += (_, e) => changed.Add(e.PropertyName);
    }

    void IDisposable.Dispose()
    {
        File.Delete(settingsPath);
        Dispose();
    }

    [Fact]
    public void Initially_ShowsEnglishAndDarkTheme()
    {
        Assert.Equal(AppLanguage.English, Assert.IsType<LanguageOption>(viewModel.SelectedLanguage).Language);
        Assert.True(viewModel.IsDarkTheme);
        Assert.Equal(Localizer.SupportedLanguages, viewModel.Languages);
    }

    [Fact]
    public void ToggleTheme_FromDark_AppliesLightNotifiesAndSaves()
    {
        viewModel.ToggleThemeCommand.Execute(null);

        Assert.Equal([AppTheme.Light], theme.Applied);
        Assert.False(viewModel.IsDarkTheme);
        Assert.Contains(nameof(MainWindowViewModel.IsDarkTheme), changed);
        Assert.Equal(new UiSettings(AppLanguage.English, AppTheme.Light), store.Load());
    }

    [Fact]
    public void ToggleTheme_Twice_ReturnsToDark()
    {
        viewModel.ToggleThemeCommand.Execute(null);
        viewModel.ToggleThemeCommand.Execute(null);

        Assert.Equal([AppTheme.Light, AppTheme.Dark], theme.Applied);
        Assert.True(viewModel.IsDarkTheme);
        Assert.Equal(UiSettings.Default, store.Load());
    }

    [Fact]
    public void SelectingRussian_SwitchesLanguageNotifiesAndSaves()
    {
        viewModel.SelectedLanguage = Localizer.SupportedLanguages[1];

        Assert.Equal(AppLanguage.Russian, localizer.Language);
        Assert.Equal(AppLanguage.Russian, Assert.IsType<LanguageOption>(viewModel.SelectedLanguage).Language);
        Assert.Contains(nameof(MainWindowViewModel.SelectedLanguage), changed);
        Assert.Equal(new UiSettings(AppLanguage.Russian, AppTheme.Dark), store.Load());
    }

    [Fact]
    public void SelectingCurrentLanguageOrNull_ChangesNothingAndSavesNothing()
    {
        viewModel.SelectedLanguage = Localizer.SupportedLanguages[0];
        viewModel.SelectedLanguage = null!;

        Assert.Equal(AppLanguage.English, localizer.Language);
        Assert.False(File.Exists(settingsPath));
        Assert.Empty(changed);
    }

    [Fact]
    public void LanguageAndThemeChanges_LeaveManufacturingStateUntouched()
    {
        preferencesViewModel.ProjectName = "Sensor board";
        preferencesViewModel.Postcode = "55-220";
        preferencesViewModel.SelectedMaterial = catalog.Materials[1];
        preferencesViewModel.SelectedSolderMaskColor = catalog.SolderMaskColors[1];
        preferencesViewModel.SelectedBoardThickness = catalog.BoardThicknesses[1];
        preferencesViewModel.Notes = "Rush";

        viewModel.SelectedLanguage = Localizer.SupportedLanguages[1];
        viewModel.ToggleThemeCommand.Execute(null);
        viewModel.SelectedLanguage = Localizer.SupportedLanguages[0];

        Assert.Equal("Sensor board", preferences.ProjectName);
        Assert.Equal("55-220", preferences.Postcode);
        Assert.Same(catalog.Materials[1], preferences.SelectedMaterial);
        Assert.Same(catalog.SolderMaskColors[1], preferences.SelectedSolderMaskColor);
        Assert.Same(catalog.BoardThicknesses[1], preferences.SelectedBoardThickness);
        Assert.Equal("Rush", preferences.Notes);
        Assert.Equal("$372.50", preferencesViewModel.EstimatedPriceText);
        Assert.False(preferencesViewModel.HasErrors);
    }
}
