using Altium.PcbManufacturing.Localization;
using Altium.PcbManufacturing.Models;
using Altium.PcbManufacturing.Services;
using Altium.PcbManufacturing.ViewModels;

namespace Altium.PcbManufacturing.Tests;

/// <summary>
/// Reset to Defaults and Save &amp; Continue on <see cref="PreferencesViewModel"/>.
/// </summary>
public sealed class PreferencesCommandsTests
{
    private readonly PcbCatalog catalog = TestCatalogs.Create();
    private readonly ManufacturingPreferences preferences;
    private readonly WorkflowNavigationState navigation = new();
    private readonly FakeConfirmationService confirmation = new();
    private readonly PreferencesViewModel viewModel;

    public PreferencesCommandsTests()
    {
        preferences = new ManufacturingPreferences(catalog);
        viewModel = new PreferencesViewModel(catalog, preferences, new PcbPricingService(catalog), navigation, confirmation, new Localizer());
    }

    // Reset to Defaults

    [Fact]
    public void Reset_AfterChanges_AsksForConfirmation()
    {
        viewModel.Notes = "Rush";

        viewModel.ResetToDefaultsCommand.Execute(null);

        (string title, string message) = Assert.Single(confirmation.Requests);
        Assert.Equal("Reset to Defaults", title);
        Assert.Equal("Discard your changes and restore the default preferences?", message);
    }

    [Fact]
    public void Reset_Confirmed_RestoresEveryDefault()
    {
        ChangeEverything();

        viewModel.ResetToDefaultsCommand.Execute(null);

        Assert.Equal("PCB-001", preferences.ProjectName);
        Assert.Equal(string.Empty, preferences.Postcode);
        Assert.Same(catalog.Materials[0], preferences.SelectedMaterial);
        Assert.Same(catalog.SolderMaskColors[0], preferences.SelectedSolderMaskColor);
        Assert.Same(catalog.BoardThicknesses[0], preferences.SelectedBoardThickness);
        Assert.Equal(string.Empty, preferences.Notes);
        Assert.True(preferences.MatchesDefaults());
    }

    [Fact]
    public void Reset_Confirmed_RecalculatesPricingAndProductionTimeAndNotifies()
    {
        ChangeEverything();
        List<string?> changed = [];
        viewModel.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        viewModel.ResetToDefaultsCommand.Execute(null);

        Assert.Equal("$149.00", viewModel.EstimatedPriceText);
        Assert.Equal("Included", viewModel.MaterialSurchargeText);
        Assert.Equal(5, viewModel.EstimatedProductionDays);
        Assert.Equal("0 / 300", viewModel.NotesCounterText);
        Assert.Contains(nameof(PreferencesViewModel.EstimatedPriceText), changed);
        Assert.Contains(nameof(PreferencesViewModel.EstimatedProductionDays), changed);
        Assert.Contains(nameof(PreferencesViewModel.ProjectName), changed);
        Assert.Contains(nameof(PreferencesViewModel.NotesCounterText), changed);
    }

    [Fact]
    public void Reset_Confirmed_ClearsValidationErrors()
    {
        viewModel.ProjectName = "";
        viewModel.Postcode = "55-2";
        Assert.True(viewModel.HasErrors);

        viewModel.ResetToDefaultsCommand.Execute(null);

        Assert.False(viewModel.HasErrors);
        Assert.False(viewModel.HasProjectNameErrors);
        Assert.False(viewModel.HasPostcodeErrors);
    }

    [Fact]
    public void Reset_Cancelled_LeavesEveryValueUnchanged()
    {
        ChangeEverything();
        confirmation.Answer = false;

        viewModel.ResetToDefaultsCommand.Execute(null);

        Assert.Single(confirmation.Requests);
        Assert.Equal("Sensor board", preferences.ProjectName);
        Assert.Equal("55-220", preferences.Postcode);
        Assert.Same(catalog.Materials[1], preferences.SelectedMaterial);
        Assert.Same(catalog.SolderMaskColors[1], preferences.SelectedSolderMaskColor);
        Assert.Same(catalog.BoardThicknesses[1], preferences.SelectedBoardThickness);
        Assert.Equal("Rush", preferences.Notes);
        Assert.Equal("$372.50", viewModel.EstimatedPriceText);
    }

    [Fact]
    public void Reset_Cancelled_KeepsValidationErrors()
    {
        viewModel.Postcode = "55-2";
        confirmation.Answer = false;

        viewModel.ResetToDefaultsCommand.Execute(null);

        Assert.True(viewModel.HasPostcodeErrors);
    }

    [Fact]
    public void Reset_AtDefaults_DoesNotAskForConfirmation()
    {
        viewModel.ResetToDefaultsCommand.Execute(null);

        Assert.Empty(confirmation.Requests);
        Assert.True(preferences.MatchesDefaults());
    }

    [Fact]
    public void Reset_AtDefaultsWithStaleError_ClearsItWithoutAsking()
    {
        // Typing and then clearing the postcode returns it to its default but leaves the "required" error.
        viewModel.Postcode = "5";
        viewModel.Postcode = "";
        Assert.True(viewModel.HasPostcodeErrors);

        viewModel.ResetToDefaultsCommand.Execute(null);

        Assert.Empty(confirmation.Requests);
        Assert.False(viewModel.HasErrors);
    }

    // Save & Continue

    [Fact]
    public void Save_ValidPreferences_NavigatesToQuote()
    {
        viewModel.Postcode = "55-220";

        viewModel.SaveAndContinueCommand.Execute(null);

        Assert.Equal(WorkflowStep.Quote, navigation.CurrentStep);
        Assert.Equal(0, viewModel.InvalidSubmitCount);
    }

    [Fact]
    public void Save_ValidPreferences_KeepsEnteredValuesInSharedState()
    {
        ChangeEverything();

        viewModel.SaveAndContinueCommand.Execute(null);

        Assert.Equal(WorkflowStep.Quote, navigation.CurrentStep);
        Assert.Equal("Sensor board", preferences.ProjectName);
        Assert.Equal("55-220", preferences.Postcode);
        Assert.Same(catalog.Materials[1], preferences.SelectedMaterial);
        Assert.Same(catalog.SolderMaskColors[1], preferences.SelectedSolderMaskColor);
        Assert.Same(catalog.BoardThicknesses[1], preferences.SelectedBoardThickness);
        Assert.Equal("Rush", preferences.Notes);
    }

    [Fact]
    public void Save_UntouchedEmptyPostcode_ReportsItAndStays()
    {
        // On first launch the empty postcode shows no error; Save validates it.
        Assert.False(viewModel.HasErrors);

        viewModel.SaveAndContinueCommand.Execute(null);

        Assert.Equal(WorkflowStep.Preferences, navigation.CurrentStep);
        Assert.True(viewModel.HasPostcodeErrors);
        Assert.Equal(
            ["Enter a postcode."],
            viewModel.GetErrors(nameof(PreferencesViewModel.Postcode)).Select(r => r.ErrorMessage));
    }

    [Theory]
    [InlineData("55-2")]
    [InlineData("invalid")]
    public void Save_InvalidPostcode_StaysOnPreferences(string postcode)
    {
        viewModel.Postcode = postcode;

        viewModel.SaveAndContinueCommand.Execute(null);

        Assert.Equal(WorkflowStep.Preferences, navigation.CurrentStep);
        Assert.True(viewModel.HasPostcodeErrors);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Save_BlankProjectName_StaysOnPreferences(string projectName)
    {
        viewModel.Postcode = "55-220";
        viewModel.ProjectName = projectName;

        viewModel.SaveAndContinueCommand.Execute(null);

        Assert.Equal(WorkflowStep.Preferences, navigation.CurrentStep);
        Assert.True(viewModel.HasProjectNameErrors);
        Assert.False(viewModel.HasPostcodeErrors);
    }

    [Fact]
    public void Save_ProjectNameAssignedInCodeTooLong_StaysOnPreferences()
    {
        viewModel.Postcode = "55-220";
        preferences.ProjectName = new string('a', 51);

        viewModel.SaveAndContinueCommand.Execute(null);

        Assert.Equal(WorkflowStep.Preferences, navigation.CurrentStep);
        Assert.True(viewModel.HasProjectNameErrors);
    }

    [Fact]
    public void Save_NotesAssignedInCodeTooLong_StaysOnPreferences()
    {
        viewModel.Postcode = "55-220";
        preferences.Notes = new string('n', 301);

        viewModel.SaveAndContinueCommand.Execute(null);

        Assert.Equal(WorkflowStep.Preferences, navigation.CurrentStep);
    }

    [Fact]
    public void Save_Invalid_RequestsFocusEachTime()
    {
        List<string?> changed = [];
        viewModel.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        viewModel.SaveAndContinueCommand.Execute(null);
        viewModel.SaveAndContinueCommand.Execute(null);

        Assert.Equal(2, viewModel.InvalidSubmitCount);
        Assert.Equal(2, changed.Count(name => name == nameof(PreferencesViewModel.InvalidSubmitCount)));
    }

    [Fact]
    public void Save_InvalidThenCorrected_NavigatesToQuote()
    {
        viewModel.SaveAndContinueCommand.Execute(null);
        Assert.Equal(WorkflowStep.Preferences, navigation.CurrentStep);

        viewModel.Postcode = "55-220";
        viewModel.SaveAndContinueCommand.Execute(null);

        Assert.Equal(WorkflowStep.Quote, navigation.CurrentStep);
        Assert.False(viewModel.HasErrors);
    }

    [Fact]
    public void Save_DoesNotAskForConfirmation()
    {
        viewModel.Postcode = "55-220";

        viewModel.SaveAndContinueCommand.Execute(null);

        Assert.Empty(confirmation.Requests);
    }

    [Fact]
    public void Commands_AreAlwaysExecutable()
    {
        Assert.True(viewModel.ResetToDefaultsCommand.CanExecute(null));
        Assert.True(viewModel.SaveAndContinueCommand.CanExecute(null));
    }

    private void ChangeEverything()
    {
        viewModel.ProjectName = "Sensor board";
        viewModel.Postcode = "55-220";
        viewModel.SelectedMaterial = catalog.Materials[1];
        viewModel.SelectedSolderMaskColor = catalog.SolderMaskColors[1];
        viewModel.SelectedBoardThickness = catalog.BoardThicknesses[1];
        viewModel.Notes = "Rush";
    }
}
