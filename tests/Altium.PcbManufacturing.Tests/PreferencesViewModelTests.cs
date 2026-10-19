using Altium.PcbManufacturing.Models;
using Altium.PcbManufacturing.Services;
using Altium.PcbManufacturing.ViewModels;

namespace Altium.PcbManufacturing.Tests;

/// <summary>
/// The Preferences screen: editing the shared state, the price summary, field validation (project name, postcode,
/// quantity, notes) with recovery from invalid text, and the Reset / Save &amp; Continue commands. Where Save leads in the
/// workflow is covered by <see cref="WorkflowTests"/>.
/// </summary>
public sealed class PreferencesViewModelTests
{
    private const string quantityWholeNumberMessage = "Enter a whole number of boards, 1 or more.";

    private readonly TestScreens screens = new();

    private PreferencesViewModel ViewModel => screens.Preferences;

    private ManufacturingPreferences State => screens.State;

    private PcbCatalog Catalog => screens.Catalog;

    // Shared state and summary

    [Fact]
    public void Initially_ShowsTheDefaultsAndPriceWithoutErrors()
    {
        Assert.Equal("PCB-001", ViewModel.ProjectName);
        Assert.Equal("10", ViewModel.QuantityInput);
        Assert.Same(Catalog.Materials[0], ViewModel.SelectedMaterial);
        Assert.Equal("Included", ViewModel.MaterialSurchargePerUnitText);
        Assert.Equal("$149.00", ViewModel.EstimatedUnitPriceText);
        Assert.Equal("$1,490.00", ViewModel.EstimatedTotalPriceText);
        Assert.Equal("5 days", ViewModel.EstimatedProductionTimeText);
        // The empty postcode is not reported until it is edited or Save is clicked.
        Assert.False(ViewModel.HasErrors);
    }

    [Fact]
    public void MaterialAndQuantity_UpdateTheSharedStateAndTheSummary()
    {
        List<string?> changed = [];
        ViewModel.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        ViewModel.SelectedMaterial = Catalog.Materials[1];
        ViewModel.QuantityInput = "3";

        // Test catalog Rogers: 149.00 × 2.5 = 372.50 per board, 12 days.
        Assert.Same(Catalog.Materials[1], State.SelectedMaterial);
        Assert.Equal(3, State.Quantity);
        Assert.Equal("+$223.50", ViewModel.MaterialSurchargePerUnitText);
        Assert.Equal("$372.50", ViewModel.EstimatedUnitPriceText);
        Assert.Equal("$1,117.50", ViewModel.EstimatedTotalPriceText);
        Assert.Equal("3 pcs.", ViewModel.QuantityText);
        Assert.Equal("12 days", ViewModel.EstimatedProductionTimeText);
        Assert.Contains(nameof(PreferencesViewModel.EstimatedUnitPriceText), changed);
        Assert.Contains(nameof(PreferencesViewModel.EstimatedTotalPriceText), changed);
    }

    // The indicator beside the material selector, for every material of the shipped catalog ($149.00 base price):
    // the selected material's unit price minus the base price, per board.
    [Theory]
    [InlineData(0, "Included", "$149.00")]
    [InlineData(1, "+$37.25 / board", "$186.25")]
    [InlineData(2, "+$208.60 / board", "$357.60")]
    public async Task MaterialSurcharge_ShowsTheExtraCostPerBoardOfTheSelectedMaterial(int material, string surcharge, string unitPrice)
    {
        TestScreens shipped = new(await new PcbCatalogLoader(PcbCatalogLoader.DefaultCatalogPath).LoadAsync(TestContext.Current.CancellationToken));
        List<string?> changed = [];
        shipped.Preferences.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        shipped.Preferences.SelectedMaterial = shipped.Catalog.Materials[material];

        Assert.Equal(surcharge, shipped.Preferences.MaterialSurchargeText);
        Assert.Equal(unitPrice, shipped.Preferences.EstimatedUnitPriceText);
        Assert.Equal(unitPrice, shipped.Quote.EstimatedUnitPriceText);
        Assert.Equal(unitPrice, shipped.Order.EstimatedUnitPriceText);
        Assert.Equal(material != 0, changed.Contains(nameof(PreferencesViewModel.MaterialSurchargeText)));
    }

    [Fact]
    public void SharedStateChangedElsewhere_IsShownAndValidated()
    {
        State.ProjectName = "Sensor board";
        State.Postcode = "invalid";
        State.Quantity = 42;

        Assert.Equal("Sensor board", ViewModel.ProjectName);
        Assert.Equal("42", ViewModel.QuantityInput);
        Assert.Equal(["Use the NN-NNN format."], Errors(nameof(PreferencesViewModel.Postcode)));
    }

    [Fact]
    public void NullFromABinding_IsHandledSafely()
    {
        // A ComboBox can push null while its items are replaced; text boxes are normalised to empty text.
        ViewModel.SelectedSolderMaskColor = null;
        ViewModel.Postcode = null!;

        Assert.Same(Catalog.SolderMaskColors[0], State.SelectedSolderMaskColor);
        Assert.Equal(string.Empty, State.Postcode);
    }

    // Validation

    [Theory]
    [InlineData("Płytka główna #3 (v2.1)", null)]
    [InlineData("", "Enter a project name.")]
    [InlineData("   ", "Enter a project name.")]
    public void ProjectName_IsRequired(string projectName, string? error)
    {
        ViewModel.ProjectName = projectName;

        Assert.Equal(error is null ? [] : [error], Errors(nameof(PreferencesViewModel.ProjectName)));
        Assert.Equal(projectName, State.ProjectName);
    }

    [Fact]
    public void ProjectName_IsLimitedTo50Characters()
    {
        // The TextBox blocks typing past the limit; a longer value can still be assigned in code.
        ViewModel.ProjectName = new string('a', 50);
        Assert.False(ViewModel.HasProjectNameErrors);

        ViewModel.ProjectName = new string('a', 51);
        Assert.Equal(["Use at most 50 characters."], Errors(nameof(PreferencesViewModel.ProjectName)));
    }

    [Theory]
    [InlineData("55-220", null)]
    [InlineData("55220", "Use the NN-NNN format.")]
    [InlineData("55-220 ", "Use the NN-NNN format.")]
    [InlineData("", "Enter a postcode.")]
    public void Postcode_IsValidatedButAlwaysStored(string postcode, string? error)
    {
        ViewModel.Postcode = "5";

        ViewModel.Postcode = postcode;

        Assert.Equal(error is null ? [] : [error], Errors(nameof(PreferencesViewModel.Postcode)));
        Assert.Equal(postcode, State.Postcode);
    }

    [Theory]
    [InlineData("1", 1)]
    [InlineData("007", 7)]
    [InlineData("2147483647", int.MaxValue)]
    public void Quantity_ValidWholeNumber_IsStored(string input, int expected)
    {
        ViewModel.QuantityInput = input;

        Assert.False(ViewModel.HasQuantityErrors);
        Assert.Equal(expected, State.Quantity);
    }

    // Typing is filtered to digits in the view, but bound or programmatic text is still validated here.
    [Theory]
    [InlineData("0", quantityWholeNumberMessage)]
    [InlineData("-5", quantityWholeNumberMessage)]
    [InlineData("2.5", quantityWholeNumberMessage)]
    [InlineData("abc", quantityWholeNumberMessage)]
    [InlineData("", "Enter the number of boards.")]
    [InlineData("2147483648", "The quantity is too large.")]
    public void Quantity_InvalidText_IsReportedAndKeptWhileTheLastValidQuantityIsUsed(string input, string error)
    {
        ViewModel.QuantityInput = "4";

        ViewModel.QuantityInput = input;

        Assert.Equal([error], Errors(nameof(PreferencesViewModel.QuantityInput)));
        Assert.Equal(input, ViewModel.QuantityInput);
        Assert.Equal(4, State.Quantity);
        Assert.Equal("$596.00", ViewModel.EstimatedTotalPriceText);
    }

    [Fact]
    public void Quantity_CorrectedAfterInvalidText_IsValidAndStoredAgain()
    {
        ViewModel.QuantityInput = "abc";

        ViewModel.QuantityInput = "3";

        Assert.False(ViewModel.HasErrors);
        Assert.Equal(3, State.Quantity);
    }

    [Fact]
    public void Notes_AreOptionalAndLimitedTo300Characters()
    {
        ViewModel.Notes = new string('n', 300);
        Assert.False(ViewModel.HasErrors);
        Assert.Equal("300 / 300", ViewModel.NotesCounterText);

        ViewModel.Notes = new string('n', 301);
        Assert.Equal(["Use at most 300 characters."], Errors(nameof(PreferencesViewModel.Notes)));
    }

    // Reset to Defaults

    [Fact]
    public void Reset_Confirmed_RestoresDefaultsEvenOverInvalidTextAndClearsErrors()
    {
        ChangeEverything();
        ViewModel.Postcode = "55-2";
        ViewModel.QuantityInput = "abc";

        ViewModel.ResetToDefaultsCommand.Execute(null);

        Assert.Single(screens.Confirmation.Requests);
        Assert.True(State.MatchesDefaults());
        Assert.Equal("10", ViewModel.QuantityInput);
        Assert.Equal("$149.00", ViewModel.EstimatedUnitPriceText);
        Assert.False(ViewModel.HasErrors);
    }

    [Fact]
    public void Reset_Cancelled_KeepsEveryValue()
    {
        ChangeEverything();
        screens.Confirmation.Answer = false;

        ViewModel.ResetToDefaultsCommand.Execute(null);

        Assert.Single(screens.Confirmation.Requests);
        Assert.Equal("Sensor board", State.ProjectName);
        Assert.Same(Catalog.Materials[1], State.SelectedMaterial);
        Assert.Equal(25, State.Quantity);
    }

    [Fact]
    public void Reset_AtDefaults_DoesNotAsk()
    {
        ViewModel.ResetToDefaultsCommand.Execute(null);

        Assert.Empty(screens.Confirmation.Requests);
    }

    // Save & Continue validation

    [Fact]
    public void Save_UntouchedEmptyPostcode_ReportsIt()
    {
        ViewModel.SaveAndContinueCommand.Execute(null);

        Assert.Equal(["Enter a postcode."], Errors(nameof(PreferencesViewModel.Postcode)));
    }

    [Theory]
    [InlineData("", "55-220", "10")]
    [InlineData("PCB-001", "55-2", "10")]
    [InlineData("PCB-001", "55-220", "0")]
    public void Save_Invalid_StaysOnPreferencesAndAsksTheViewToFocusTheError(string projectName, string postcode, string quantity)
    {
        ViewModel.ProjectName = projectName;
        ViewModel.Postcode = postcode;
        ViewModel.QuantityInput = quantity;

        ViewModel.SaveAndContinueCommand.Execute(null);

        Assert.Equal(WorkflowStep.Preferences, screens.Navigation.CurrentStep);
        Assert.False(screens.Navigation.IsQuoteUnlocked);
        Assert.Equal(1, ViewModel.InvalidSubmitCount);
    }

    private void ChangeEverything()
    {
        ViewModel.ProjectName = "Sensor board";
        ViewModel.Postcode = "55-220";
        ViewModel.QuantityInput = "25";
        ViewModel.SelectedMaterial = Catalog.Materials[1];
        ViewModel.SelectedSolderMaskColor = Catalog.SolderMaskColors[1];
        ViewModel.SelectedBoardThickness = Catalog.BoardThicknesses[1];
        ViewModel.Notes = "Rush";
    }

    private IEnumerable<string?> Errors(string propertyName) =>
        ViewModel.GetErrors(propertyName).Select(result => result.ErrorMessage);
}
