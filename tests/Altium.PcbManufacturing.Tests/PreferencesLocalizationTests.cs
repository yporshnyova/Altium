using Altium.PcbManufacturing.Localization;
using Altium.PcbManufacturing.Models;
using Altium.PcbManufacturing.Services;
using Altium.PcbManufacturing.ViewModels;

namespace Altium.PcbManufacturing.Tests;

/// <summary>
/// Localized texts and validation messages of <see cref="PreferencesViewModel"/> across language switches.
/// </summary>
[Collection(UiCultureCollection.Name)]
public sealed class PreferencesLocalizationTests : UiCultureTest
{
    private readonly PcbCatalog catalog = TestCatalogs.Create();
    private readonly ManufacturingPreferences preferences;
    private readonly Localizer localizer = new();
    private readonly FakeConfirmationService confirmation = new();
    private readonly PreferencesViewModel viewModel;

    public PreferencesLocalizationTests()
    {
        preferences = new ManufacturingPreferences(catalog);
        viewModel = new PreferencesViewModel(
            catalog, preferences, new PcbPricingService(catalog), new WorkflowNavigationState(), confirmation, localizer);
    }

    [Fact]
    public void Russian_LocalizesIncludedSurchargeAndProductionTime()
    {
        localizer.SetLanguage(AppLanguage.Russian);

        Assert.Equal("Включено", viewModel.MaterialSurchargeText);
        Assert.Equal("5 дней", viewModel.EstimatedProductionTimeText);
    }

    [Fact]
    public void LanguageChange_NotifiesLocalizedTexts()
    {
        List<string?> changed = [];
        viewModel.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        localizer.SetLanguage(AppLanguage.Russian);

        Assert.Contains(nameof(PreferencesViewModel.MaterialSurchargeText), changed);
        Assert.Contains(nameof(PreferencesViewModel.EstimatedProductionTimeText), changed);
    }

    [Fact]
    public void Prices_AreTheSameInEveryLanguage()
    {
        viewModel.SelectedMaterial = catalog.Materials[1];

        localizer.SetLanguage(AppLanguage.Russian);

        Assert.Equal("+$223.50", viewModel.MaterialSurchargeText);
        Assert.Equal("$372.50", viewModel.EstimatedPriceText);
        Assert.Equal("$149.00", viewModel.BasePriceText);
    }

    [Fact]
    public void Russian_ValidationMessagesAreRussian()
    {
        localizer.SetLanguage(AppLanguage.Russian);

        viewModel.ProjectName = new string('a', 51);
        viewModel.Postcode = "55-2";

        Assert.Equal(["Не более 50 символов."], Errors(nameof(PreferencesViewModel.ProjectName)));
        Assert.Equal(["Используйте формат NN-NNN."], Errors(nameof(PreferencesViewModel.Postcode)));
    }

    [Fact]
    public void LanguageChange_RelocalizesShownErrors()
    {
        viewModel.Postcode = "55-2";
        viewModel.Notes = new string('n', 301);
        List<string?> errorsChanged = [];
        viewModel.ErrorsChanged += (_, e) => errorsChanged.Add(e.PropertyName);

        localizer.SetLanguage(AppLanguage.Russian);

        Assert.Equal(["Используйте формат NN-NNN."], Errors(nameof(PreferencesViewModel.Postcode)));
        Assert.Equal(["Не более 300 символов."], Errors(nameof(PreferencesViewModel.Notes)));
        Assert.Contains(nameof(PreferencesViewModel.Postcode), errorsChanged);
        Assert.Contains(nameof(PreferencesViewModel.Notes), errorsChanged);
    }

    [Fact]
    public void LanguageChange_DoesNotValidateUntouchedFields()
    {
        localizer.SetLanguage(AppLanguage.Russian);

        Assert.False(viewModel.HasErrors);
    }

    [Fact]
    public void LanguageChange_KeepsEnteredValues()
    {
        viewModel.ProjectName = "Płytka #3";
        viewModel.Postcode = "55-2";
        viewModel.Notes = "Line 1\r\nLine 2";

        localizer.SetLanguage(AppLanguage.Russian);
        localizer.SetLanguage(AppLanguage.English);

        Assert.Equal("Płytka #3", preferences.ProjectName);
        Assert.Equal("55-2", preferences.Postcode);
        Assert.Equal("Line 1\r\nLine 2", preferences.Notes);
        Assert.Equal(["Use the NN-NNN format."], Errors(nameof(PreferencesViewModel.Postcode)));
    }

    [Fact]
    public void Russian_ResetConfirmationIsRussian()
    {
        localizer.SetLanguage(AppLanguage.Russian);
        viewModel.Notes = "x";

        viewModel.ResetToDefaultsCommand.Execute(null);

        (string title, string message) = Assert.Single(confirmation.Requests);
        Assert.Equal("Сброс параметров", title);
        Assert.Equal("Отменить изменения и восстановить параметры по умолчанию?", message);
    }

    private IEnumerable<string?> Errors(string propertyName) =>
        viewModel.GetErrors(propertyName).Select(result => result.ErrorMessage);
}
