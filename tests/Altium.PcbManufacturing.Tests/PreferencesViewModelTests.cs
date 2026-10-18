using System.ComponentModel.DataAnnotations;
using Altium.PcbManufacturing.Localization;
using Altium.PcbManufacturing.Models;
using Altium.PcbManufacturing.Services;
using Altium.PcbManufacturing.ViewModels;

namespace Altium.PcbManufacturing.Tests;

public sealed class PreferencesViewModelTests
{
    private readonly PcbCatalog catalog = TestCatalogs.Create();
    private readonly ManufacturingPreferences preferences;
    private readonly PreferencesViewModel viewModel;
    private readonly List<string?> changedProperties = [];
    private readonly List<string?> errorsChangedProperties = [];

    public PreferencesViewModelTests()
    {
        preferences = new ManufacturingPreferences(catalog);
        viewModel = CreateViewModel();
        viewModel.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName);
        viewModel.ErrorsChanged += (_, e) => errorsChangedProperties.Add(e.PropertyName);
    }

    // Initial state

    [Fact]
    public void Constructor_ExposesCatalogOptions()
    {
        Assert.Same(catalog.Materials, viewModel.Materials);
        Assert.Same(catalog.SolderMaskColors, viewModel.SolderMaskColors);
        Assert.Same(catalog.BoardThicknesses, viewModel.BoardThicknesses);
    }

    [Fact]
    public void Constructor_ReadsCurrentValuesFromSharedState()
    {
        preferences.ProjectName = "Sensor board";
        preferences.SelectedMaterial = catalog.Materials[1];
        preferences.SelectedSolderMaskColor = catalog.SolderMaskColors[1];
        preferences.SelectedBoardThickness = catalog.BoardThicknesses[1];
        preferences.Postcode = "55-220";

        PreferencesViewModel created = CreateViewModel();

        Assert.Equal("Sensor board", created.ProjectName);
        Assert.Same(catalog.Materials[1], created.SelectedMaterial);
        Assert.Same(catalog.SolderMaskColors[1], created.SelectedSolderMaskColor);
        Assert.Same(catalog.BoardThicknesses[1], created.SelectedBoardThickness);
        Assert.Equal("55-220", created.Postcode);
    }

    [Fact]
    public void Constructor_ProjectNameDefaultsToPcb001() => Assert.Equal("PCB-001", viewModel.ProjectName);

    [Fact]
    public void Constructor_ShowsPricingAndProductionTimeOfDefaultMaterial()
    {
        Assert.Equal(new PriceEstimate(149.00m, 0.00m, 149.00m), viewModel.PriceEstimate);
        Assert.Equal("$149.00", viewModel.BasePriceText);
        Assert.Equal("Included", viewModel.MaterialSurchargeText);
        Assert.Equal("$149.00", viewModel.EstimatedPriceText);
        Assert.Equal(5, viewModel.EstimatedProductionDays);
    }

    [Fact]
    public void Constructor_HasNoValidationErrors()
    {
        Assert.False(viewModel.HasErrors);
        Assert.False(viewModel.HasProjectNameErrors);
        Assert.False(viewModel.HasPostcodeErrors);
    }

    // Selections

    [Fact]
    public void SelectedMaterial_Changed_UpdatesSharedState()
    {
        viewModel.SelectedMaterial = catalog.Materials[1];

        Assert.Same(catalog.Materials[1], preferences.SelectedMaterial);
        Assert.Same(catalog.Materials[1], viewModel.SelectedMaterial);
    }

    [Fact]
    public void SelectedMaterial_Changed_RecalculatesPricingAndProductionTime()
    {
        viewModel.SelectedMaterial = catalog.Materials[1];

        // Rogers: 149.00 × 2.5 = 372.50
        Assert.Equal(new PriceEstimate(149.00m, 223.50m, 372.50m), viewModel.PriceEstimate);
        Assert.Equal("+$223.50", viewModel.MaterialSurchargeText);
        Assert.Equal("$372.50", viewModel.EstimatedPriceText);
        Assert.Equal("$149.00", viewModel.BasePriceText);
        Assert.Equal(12, viewModel.EstimatedProductionDays);
    }

    [Fact]
    public void SelectedMaterial_ChangedBack_ShowsSurchargeAsIncludedAgain()
    {
        viewModel.SelectedMaterial = catalog.Materials[1];
        viewModel.SelectedMaterial = catalog.Materials[0];

        Assert.Equal("Included", viewModel.MaterialSurchargeText);
        Assert.Equal("$149.00", viewModel.EstimatedPriceText);
    }

    [Fact]
    public void SelectedMaterial_Changed_NotifiesSelectionAndAllDerivedValues()
    {
        viewModel.SelectedMaterial = catalog.Materials[1];

        Assert.Equal(
            [
                nameof(PreferencesViewModel.SelectedMaterial),
                nameof(PreferencesViewModel.PriceEstimate),
                nameof(PreferencesViewModel.MaterialSurchargeText),
                nameof(PreferencesViewModel.EstimatedPriceText),
                nameof(PreferencesViewModel.EstimatedProductionDays),
                nameof(PreferencesViewModel.EstimatedProductionTimeText),
            ],
            changedProperties);
    }

    [Fact]
    public void SelectedSolderMaskColor_Changed_UpdatesSharedStateAndNotifies()
    {
        viewModel.SelectedSolderMaskColor = catalog.SolderMaskColors[1];

        Assert.Same(catalog.SolderMaskColors[1], preferences.SelectedSolderMaskColor);
        Assert.Same(catalog.SolderMaskColors[1], viewModel.SelectedSolderMaskColor);
        Assert.Equal([nameof(PreferencesViewModel.SelectedSolderMaskColor)], changedProperties);
    }

    [Fact]
    public void SelectedBoardThickness_Changed_UpdatesSharedStateAndNotifies()
    {
        viewModel.SelectedBoardThickness = catalog.BoardThicknesses[1];

        Assert.Same(catalog.BoardThicknesses[1], preferences.SelectedBoardThickness);
        Assert.Same(catalog.BoardThicknesses[1], viewModel.SelectedBoardThickness);
        Assert.Equal([nameof(PreferencesViewModel.SelectedBoardThickness)], changedProperties);
    }

    [Fact]
    public void SharedStateChangedElsewhere_ViewModelReflectsItAndNotifies()
    {
        preferences.ProjectName = "Sensor board";
        preferences.SelectedMaterial = catalog.Materials[1];
        preferences.SelectedSolderMaskColor = catalog.SolderMaskColors[1];
        preferences.SelectedBoardThickness = catalog.BoardThicknesses[1];
        preferences.Postcode = "55-220";

        Assert.Equal("Sensor board", viewModel.ProjectName);
        Assert.Same(catalog.Materials[1], viewModel.SelectedMaterial);
        Assert.Equal("$372.50", viewModel.EstimatedPriceText);
        Assert.Same(catalog.SolderMaskColors[1], viewModel.SelectedSolderMaskColor);
        Assert.Same(catalog.BoardThicknesses[1], viewModel.SelectedBoardThickness);
        Assert.Equal("55-220", viewModel.Postcode);
        Assert.Equal(
            [
                nameof(PreferencesViewModel.ProjectName),
                nameof(PreferencesViewModel.IsProjectNameBlank),
                nameof(PreferencesViewModel.SelectedMaterial),
                nameof(PreferencesViewModel.PriceEstimate),
                nameof(PreferencesViewModel.MaterialSurchargeText),
                nameof(PreferencesViewModel.EstimatedPriceText),
                nameof(PreferencesViewModel.EstimatedProductionDays),
                nameof(PreferencesViewModel.EstimatedProductionTimeText),
                nameof(PreferencesViewModel.SelectedSolderMaskColor),
                nameof(PreferencesViewModel.SelectedBoardThickness),
                nameof(PreferencesViewModel.Postcode),
                nameof(PreferencesViewModel.IsPostcodeBlank),
            ],
            changedProperties);
    }

    [Fact]
    public void AssigningCurrentValues_DoesNotNotify()
    {
        viewModel.Notes = string.Empty;
        viewModel.ProjectName = ManufacturingPreferences.DefaultProjectName;
        viewModel.SelectedMaterial = catalog.Materials[0];
        viewModel.SelectedSolderMaskColor = catalog.SolderMaskColors[0];
        viewModel.SelectedBoardThickness = catalog.BoardThicknesses[0];
        viewModel.Postcode = string.Empty;

        Assert.Empty(changedProperties);
        Assert.Empty(errorsChangedProperties);
    }

    [Fact]
    public void AssigningNullMaterial_ThrowsAndKeepsCurrentMaterial()
    {
        Assert.Throws<ArgumentNullException>(() => viewModel.SelectedMaterial = null!);

        Assert.Same(catalog.Materials[0], preferences.SelectedMaterial);
        Assert.Empty(changedProperties);
    }

    [Fact]
    public void AssigningNullSolderMaskOrThickness_KeepsCurrentSelections()
    {
        viewModel.SelectedSolderMaskColor = null;
        viewModel.SelectedBoardThickness = null;

        Assert.Same(catalog.SolderMaskColors[0], preferences.SelectedSolderMaskColor);
        Assert.Same(catalog.BoardThicknesses[0], preferences.SelectedBoardThickness);
        Assert.Empty(changedProperties);
    }

    // Project name

    [Theory]
    [InlineData("Sensor board")]
    [InlineData("PCB_002-rev B")]
    [InlineData("X")]
    [InlineData("Płytka główna #3 (v2.1)")]
    public void ProjectName_Valid_HasNoErrorsAndUpdatesSharedState(string projectName)
    {
        viewModel.ProjectName = projectName;

        Assert.False(viewModel.HasProjectNameErrors);
        Assert.Equal(projectName, preferences.ProjectName);
    }

    [Fact]
    public void ProjectName_Changed_NotifiesValueAndBlankState()
    {
        viewModel.ProjectName = "Sensor board";

        Assert.Equal(
            [nameof(PreferencesViewModel.ProjectName), nameof(PreferencesViewModel.IsProjectNameBlank)],
            changedProperties);
    }

    [Theory]
    [InlineData("", true)]
    [InlineData("   ", true)]
    [InlineData("PCB-002", false)]
    public void IsProjectNameBlank_ReflectsWhitespaceOnlyText(string projectName, bool expected)
    {
        viewModel.ProjectName = projectName;

        Assert.Equal(expected, viewModel.IsProjectNameBlank);
    }

    [Theory]
    [InlineData("", true)]
    [InlineData("   ", true)]
    [InlineData("55-2", false)]
    public void IsPostcodeBlank_ReflectsWhitespaceOnlyText(string postcode, bool expected)
    {
        viewModel.Postcode = "x";
        viewModel.Postcode = postcode;

        Assert.Equal(expected, viewModel.IsPostcodeBlank);
    }

    [Fact]
    public void ProjectName_Exactly50Characters_IsValid()
    {
        viewModel.ProjectName = new string('a', 50);

        Assert.False(viewModel.HasProjectNameErrors);
    }

    [Fact]
    public void ProjectName_MoreThan50Characters_ReportsLengthError()
    {
        // The TextBox blocks typing past 50 characters; a longer value can still be assigned in code.
        viewModel.ProjectName = new string('a', 51);

        Assert.True(viewModel.HasProjectNameErrors);
        Assert.Equal(["Use at most 50 characters."], Errors(nameof(PreferencesViewModel.ProjectName)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void ProjectName_EmptyOrWhitespace_ReportsRequiredError(string projectName)
    {
        viewModel.ProjectName = projectName;

        Assert.True(viewModel.HasProjectNameErrors);
        Assert.Equal(["Enter a project name."], Errors(nameof(PreferencesViewModel.ProjectName)));
    }

    [Fact]
    public void ProjectName_Null_IsTreatedAsEmpty()
    {
        viewModel.ProjectName = null!;

        Assert.Equal(string.Empty, preferences.ProjectName);
        Assert.Equal(["Enter a project name."], Errors(nameof(PreferencesViewModel.ProjectName)));
    }

    [Fact]
    public void ProjectName_CorrectedToValid_ClearsErrorsAndNotifies()
    {
        viewModel.ProjectName = "";
        viewModel.ProjectName = "PCB-002";

        Assert.False(viewModel.HasErrors);
        Assert.False(viewModel.HasProjectNameErrors);
        Assert.Empty(viewModel.GetErrors(nameof(PreferencesViewModel.ProjectName)));
        Assert.Equal([nameof(PreferencesViewModel.ProjectName), nameof(PreferencesViewModel.ProjectName)], errorsChangedProperties);
        Assert.Equal(2, changedProperties.Count(name => name == nameof(PreferencesViewModel.HasProjectNameErrors)));
    }

    [Fact]
    public void ProjectName_Invalid_DoesNotFlagPostcode()
    {
        viewModel.ProjectName = "";

        Assert.True(viewModel.HasErrors);
        Assert.False(viewModel.HasPostcodeErrors);
        Assert.DoesNotContain(nameof(PreferencesViewModel.HasPostcodeErrors), changedProperties);
    }

    // Notes

    [Fact]
    public void Notes_DefaultEmpty_IsValid()
    {
        Assert.Equal(string.Empty, viewModel.Notes);
        Assert.Equal("0 / 300", viewModel.NotesCounterText);
        Assert.False(viewModel.HasErrors);
    }

    [Fact]
    public void Notes_MultilineText_IsAcceptedAndStoredInSharedState()
    {
        const string notes = "Panelize 2x2, please.\r\nCall before shipping: +48 123-456-789!";

        viewModel.Notes = notes;

        Assert.Equal(notes, preferences.Notes);
        Assert.Empty(viewModel.GetErrors(nameof(PreferencesViewModel.Notes)));
    }

    [Fact]
    public void Notes_Exactly300Characters_IsValid()
    {
        viewModel.Notes = new string('n', 300);

        Assert.False(viewModel.HasErrors);
        Assert.Equal("300 / 300", viewModel.NotesCounterText);
    }

    [Fact]
    public void Notes_MoreThan300Characters_ReportsLengthError()
    {
        viewModel.Notes = new string('n', 301);

        Assert.True(viewModel.HasErrors);
        Assert.Equal(["Use at most 300 characters."], Errors(nameof(PreferencesViewModel.Notes)));
    }

    [Fact]
    public void Notes_ClearedAfterEditing_IsValid()
    {
        viewModel.Notes = new string('n', 301);

        viewModel.Notes = string.Empty;

        Assert.False(viewModel.HasErrors);
        Assert.Equal("0 / 300", viewModel.NotesCounterText);
    }

    [Fact]
    public void Notes_Changed_NotifiesValueAndCounter()
    {
        viewModel.Notes = "Rush order";

        Assert.Equal([nameof(PreferencesViewModel.Notes), nameof(PreferencesViewModel.NotesCounterText)], changedProperties);
    }

    [Theory]
    [InlineData("a", "1 / 300")]
    [InlineData("Line 1\r\nLine 2", "14 / 300")]
    [InlineData("  ", "2 / 300")]
    public void NotesCounterText_ReflectsActualStringLength(string notes, string expected)
    {
        viewModel.Notes = notes;

        Assert.Equal(expected, viewModel.NotesCounterText);
    }

    [Fact]
    public void Notes_SetInSharedStateElsewhere_UpdatesCounterAndIsValidated()
    {
        preferences.Notes = new string('n', 301);

        Assert.Equal("301 / 300", viewModel.NotesCounterText);
        Assert.Equal(["Use at most 300 characters."], Errors(nameof(PreferencesViewModel.Notes)));
    }

    [Fact]
    public void Notes_Null_IsTreatedAsEmpty()
    {
        viewModel.Notes = "x";

        viewModel.Notes = null!;

        Assert.Equal(string.Empty, preferences.Notes);
    }

    // Postcode

    [Theory]
    [InlineData("55-220")]
    [InlineData("00-001")]
    [InlineData("99-999")]
    public void Postcode_Valid_HasNoErrorsAndUpdatesSharedState(string postcode)
    {
        viewModel.Postcode = postcode;

        Assert.False(viewModel.HasErrors);
        Assert.False(viewModel.HasPostcodeErrors);
        Assert.Equal(postcode, preferences.Postcode);
    }

    [Theory]
    [InlineData("55220")]
    [InlineData("5-5220")]
    [InlineData("55-22")]
    [InlineData("55-2200")]
    [InlineData("ab-cde")]
    [InlineData("55 220")]
    [InlineData(" 55-220")]
    [InlineData("55-220 ")]
    [InlineData("55-220\n")]
    [InlineData("٥٥-٢٢٠")]
    public void Postcode_Malformed_ReportsFormatError(string postcode)
    {
        viewModel.Postcode = postcode;

        Assert.True(viewModel.HasPostcodeErrors);
        Assert.Equal(["Use the NN-NNN format."], Errors(nameof(PreferencesViewModel.Postcode)));
    }

    [Fact]
    public void Postcode_Malformed_IsStillStoredInSharedState()
    {
        viewModel.Postcode = "55-2";

        Assert.Equal("55-2", preferences.Postcode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Postcode_ClearedAfterEditing_ReportsRequiredError(string postcode)
    {
        viewModel.Postcode = "55-220";

        viewModel.Postcode = postcode;

        Assert.True(viewModel.HasPostcodeErrors);
        Assert.Equal(["Enter a postcode."], Errors(nameof(PreferencesViewModel.Postcode)));
    }

    [Fact]
    public void Postcode_Null_IsTreatedAsEmpty()
    {
        viewModel.Postcode = "55-220";

        viewModel.Postcode = null!;

        Assert.Equal(string.Empty, preferences.Postcode);
        Assert.Equal(["Enter a postcode."], Errors(nameof(PreferencesViewModel.Postcode)));
    }

    [Fact]
    public void Postcode_CorrectedFromInvalidToValid_ClearsErrors()
    {
        viewModel.Postcode = "55-2";
        Assert.True(viewModel.HasPostcodeErrors);

        viewModel.Postcode = "55-220";

        Assert.False(viewModel.HasErrors);
        Assert.False(viewModel.HasPostcodeErrors);
    }

    [Fact]
    public void Postcode_BecomingInvalidAndValid_RaisesErrorsChangedAndErrorStateNotifications()
    {
        viewModel.Postcode = "55-2";
        viewModel.Postcode = "55-220";

        Assert.Equal([nameof(PreferencesViewModel.Postcode), nameof(PreferencesViewModel.Postcode)], errorsChangedProperties);
        Assert.Equal(2, changedProperties.Count(name => name == nameof(PreferencesViewModel.HasErrors)));
        Assert.Equal(2, changedProperties.Count(name => name == nameof(PreferencesViewModel.HasPostcodeErrors)));
    }

    [Fact]
    public void Postcode_SetInSharedStateElsewhere_IsValidated()
    {
        preferences.Postcode = "invalid";

        Assert.True(viewModel.HasPostcodeErrors);
        Assert.Equal(["Use the NN-NNN format."], Errors(nameof(PreferencesViewModel.Postcode)));
    }

    [Fact]
    public void Postcode_Invalid_DoesNotFlagProjectName()
    {
        viewModel.Postcode = "55-2";

        Assert.False(viewModel.HasProjectNameErrors);
    }

    private PreferencesViewModel CreateViewModel() =>
        new(catalog, preferences, new PcbPricingService(catalog), new WorkflowNavigationState(), new FakeConfirmationService(), new Localizer());

    private IEnumerable<string?> Errors(string propertyName) =>
        viewModel.GetErrors(propertyName).Select(result => result.ErrorMessage);
}
