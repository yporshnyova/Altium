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
/// Runtime EN/RU switching of the screens, theme toggling, and persistence of both choices.
/// </summary>
[Collection(UiCultureCollection.Name)]
public sealed class LanguageAndThemeTests : IDisposable
{
    private readonly TestScreens screens = new();
    private readonly FakeThemeService theme = new();
    private readonly string settingsPath = Path.Combine(Path.GetTempPath(), $"ui-settings-{Guid.NewGuid():N}.json");
    private readonly UiSettingsStore store;
    private readonly MainWindowViewModel mainWindow;

    public LanguageAndThemeTests()
    {
        store = new UiSettingsStore(settingsPath);
        mainWindow = new MainWindowViewModel(screens.Preferences, screens.Quote, screens.Order, screens.Navigation, screens.Localizer, theme, store);
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
        screens.Localizer.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        mainWindow.SelectedLanguage = Localizer.SupportedLanguages[1];

        Assert.Equal("Параметры", screens.Localizer[nameof(Strings.Tab_Preferences)]);
        Assert.Equal("Введите почтовый индекс.", Strings.Validation_PostcodeRequired);
        // {loc:Text} bindings go through the indexer and refresh on "Item[]".
        Assert.Contains("Item[]", changed);
        Assert.Equal(new UiSettings(AppLanguage.Russian, AppTheme.Dark), store.Load());

        mainWindow.SelectedLanguage = Localizer.SupportedLanguages[0];

        Assert.Equal("Preferences", screens.Localizer[nameof(Strings.Tab_Preferences)]);
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
    public void Russian_LocalizesTheScreensButKeepsPricesInUsd()
    {
        screens.Preferences.SelectedMaterial = screens.Catalog.Materials[1];
        screens.Preferences.QuantityInput = "2";

        screens.Localizer.SetLanguage(AppLanguage.Russian);

        Assert.Equal("2 шт.", screens.Preferences.QuantityText);
        Assert.Equal("+$223.50 за плату", screens.Preferences.MaterialSurchargeText);
        Assert.Equal("12 дней", screens.Quote.EstimatedProductionTimeText);
        Assert.Equal("$745.00", screens.Quote.EstimatedTotalPriceText);
        Assert.Equal(["Размеры", "Слои", "Покрытие", "Параметры платы"], screens.Quote.ParameterGroups.Select(group => group.Title));
        // Numbers in texts follow the UI language.
        Assert.Equal("1,6 мм", screens.Quote.ParameterGroups.SelectMany(group => group.Parameters).Single(row => row.Label == "Толщина платы").Value);
        Assert.Equal("2 шт.", screens.Order.QuantityText);
        Assert.Equal("Зелёный", screens.Order.SolderMaskText);
        Assert.Equal("$745.00", screens.Order.EstimatedTotalPriceText);
    }

    [Fact]
    public void LanguageChange_RelocalizesShownErrorsOnlyAndKeepsEveryValue()
    {
        screens.Preferences.Postcode = "55-2";
        screens.Preferences.QuantityInput = "abc";

        screens.Localizer.SetLanguage(AppLanguage.Russian);

        Assert.Equal(["Используйте формат NN-NNN."], Errors(nameof(PreferencesViewModel.Postcode)));
        Assert.Equal(["Введите целое число плат, не меньше 1."], Errors(nameof(PreferencesViewModel.QuantityInput)));
        // The untouched project name is not validated by a language change.
        Assert.Empty(Errors(nameof(PreferencesViewModel.ProjectName)));
        Assert.Equal("55-2", screens.State.Postcode);
        Assert.Equal("abc", screens.Preferences.QuantityInput);
    }

    [Fact]
    public void LanguageAndThemeChanges_LeaveTheManufacturingStateUntouched()
    {
        screens.Preferences.Postcode = "55-220";
        screens.Preferences.SelectedMaterial = screens.Catalog.Materials[1];

        mainWindow.SelectedLanguage = Localizer.SupportedLanguages[1];
        mainWindow.ToggleThemeCommand.Execute(null);
        mainWindow.SelectedLanguage = Localizer.SupportedLanguages[0];

        Assert.Equal("55-220", screens.State.Postcode);
        Assert.Same(screens.Catalog.Materials[1], screens.State.SelectedMaterial);
        Assert.Equal("$372.50", screens.Preferences.EstimatedUnitPriceText);
        Assert.False(screens.Preferences.HasErrors);
    }

    [Fact]
    public async Task NewOrder_KeepsTheLanguageAndTheme()
    {
        mainWindow.SelectedLanguage = Localizer.SupportedLanguages[1];
        mainWindow.ToggleThemeCommand.Execute(null);
        screens.CompleteQuote();
        await screens.Order.PlaceOrderCommand.ExecuteAsync(null);

        screens.Order.NewOrderCommand.Execute(null);

        Assert.Equal(AppLanguage.Russian, screens.Localizer.Language);
        Assert.Equal(AppTheme.Light, theme.Theme);
        Assert.Equal([AppTheme.Light], theme.Applied);
        Assert.Equal(new UiSettings(AppLanguage.Russian, AppTheme.Light), store.Load());
        Assert.Equal("10 шт.", screens.Order.QuantityText);
    }

    private IEnumerable<string?> Errors(string propertyName) =>
        screens.Preferences.GetErrors(propertyName).Select(result => result.ErrorMessage);
}
