using System.Globalization;
using System.IO;
using Altium.PcbManufacturing.Localization;
using Altium.PcbManufacturing.Settings;
using Altium.PcbManufacturing.ViewModels;

namespace Altium.PcbManufacturing.Tests;

/// <summary>
/// Tests that switch the UI language. The resource culture (<see cref="Strings.Culture"/>) is process-wide, like the UI
/// language of a WPF application, so these tests run alone, after all parallel tests.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class UiCultureCollection
{
    public const string Name = "UI culture";
}

/// <summary>
/// Runtime EN/RU switching of the steps and the footer, theme toggling, and persistence of both choices.
/// </summary>
[Collection(UiCultureCollection.Name)]
public sealed class LanguageAndThemeTests : IDisposable
{
    private readonly TestWizard wizard = new();
    private readonly FakeThemeService theme = new();
    private readonly string settingsPath = Path.Combine(Path.GetTempPath(), $"ui-settings-{Guid.NewGuid():N}.json");
    private readonly UiSettingsStore store;
    private readonly MainWindowViewModel mainWindow;

    public LanguageAndThemeTests()
    {
        store = new UiSettingsStore(settingsPath);
        mainWindow = new MainWindowViewModel(wizard.Wizard, wizard.Localizer, theme, store);
    }

    public void Dispose()
    {
        Strings.Culture = CultureInfo.GetCultureInfo("en");
        File.Delete(settingsPath);
    }

    [Fact]
    public void SelectingRussian_SwitchesTheResourcesNotifiesBindingsAndSaves()
    {
        List<string?> changed = [];
        wizard.Localizer.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        mainWindow.SelectedLanguage = Localizer.SupportedLanguages[1];

        Assert.Equal("Параметры", wizard.Localizer[nameof(Strings.Tab_Preferences)]);
        Assert.Equal("Введите почтовый индекс.", Strings.Validation_PostcodeRequired);
        // {loc:Text} bindings go through the indexer and refresh on "Item[]".
        Assert.Contains("Item[]", changed);
        Assert.Equal(new UiSettings(AppLanguage.Russian, AppTheme.Dark), store.Load());

        mainWindow.SelectedLanguage = Localizer.SupportedLanguages[0];

        Assert.Equal("Preferences", wizard.Localizer[nameof(Strings.Tab_Preferences)]);
    }

    [Fact]
    public void ToggleTheme_AppliesTheOtherThemeAndSaves()
    {
        mainWindow.ToggleThemeCommand.Execute(null);

        Assert.Equal([AppTheme.Light], theme.Applied);
        Assert.False(mainWindow.IsDarkTheme);
        Assert.Equal(new UiSettings(AppLanguage.English, AppTheme.Light), store.Load());
    }

    [Fact]
    public void FooterLabels_FollowTheLanguageAndTheStep()
    {
        List<string?> changed = [];
        wizard.Wizard.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        wizard.Localizer.SetLanguage(AppLanguage.Russian);

        Assert.Equal("Сохранить и продолжить", wizard.Wizard.PrimaryActionText);
        Assert.Contains(nameof(OrderWizardViewModel.PrimaryActionText), changed);
        Assert.Contains(nameof(OrderWizardViewModel.BackActionText), changed);

        wizard.CompletePreferences();

        Assert.Equal("Перейти к заказу", wizard.Wizard.PrimaryActionText);
        Assert.Equal("Назад к параметрам", wizard.Wizard.BackActionText);

        wizard.Localizer.SetLanguage(AppLanguage.English);

        Assert.Equal("Continue to Order", wizard.Wizard.PrimaryActionText);
        Assert.Equal("Back to Preferences", wizard.Wizard.BackActionText);
    }

    [Fact]
    public void LanguageChange_RelocalizesShownErrorsOnlyAndKeepsEveryValue()
    {
        wizard.Preferences.PostcodeInput = "55-2";
        wizard.Preferences.QuantityInput = "abc";

        wizard.Localizer.SetLanguage(AppLanguage.Russian);

        Assert.Equal(["Используйте формат NN-NNN."], Errors(nameof(PreferencesViewModel.PostcodeInput)));
        Assert.Equal(["Введите целое число плат, не меньше 1."], Errors(nameof(PreferencesViewModel.QuantityInput)));
        // The untouched project name is not validated by a language change.
        Assert.Empty(Errors(nameof(PreferencesViewModel.ProjectNameInput)));
        Assert.Equal("55-2", wizard.Draft.Postcode);
        Assert.Equal("abc", wizard.Preferences.QuantityInput);
    }

    [Fact]
    public async Task NewOrder_KeepsTheLanguageAndTheme()
    {
        mainWindow.SelectedLanguage = Localizer.SupportedLanguages[1];
        mainWindow.ToggleThemeCommand.Execute(null);
        await wizard.CompleteOrderAsync();

        await wizard.ClickPrimaryAsync(); // New Order

        Assert.Equal(AppLanguage.Russian, wizard.Localizer.Language);
        Assert.Equal(AppTheme.Light, theme.Theme);
        Assert.Equal([AppTheme.Light], theme.Applied);
        Assert.Equal(new UiSettings(AppLanguage.Russian, AppTheme.Light), store.Load());
        Assert.Equal("Сохранить и продолжить", wizard.Wizard.PrimaryActionText);
    }

    private IEnumerable<string?> Errors(string propertyName) =>
        wizard.Preferences.GetErrors(propertyName).Select(result => result.ErrorMessage);
}
