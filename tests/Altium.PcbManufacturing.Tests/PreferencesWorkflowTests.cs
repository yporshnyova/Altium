using Altium.PcbManufacturing.Localization;
using Altium.PcbManufacturing.Models;
using Altium.PcbManufacturing.Services;
using Altium.PcbManufacturing.ViewModels;

namespace Altium.PcbManufacturing.Tests;

/// <summary>
/// The Preferences step inside the Preferences → Quote → Order workflow.
/// </summary>
public sealed class PreferencesWorkflowTests
{
    private readonly PcbCatalog catalog = TestCatalogs.Create();
    private readonly ManufacturingPreferences preferences;
    private readonly WorkflowNavigationState navigation = new();
    private readonly FakeConfirmationService confirmation = new();
    private readonly PreferencesViewModel viewModel;

    public PreferencesWorkflowTests()
    {
        preferences = new ManufacturingPreferences(catalog);
        viewModel = new PreferencesViewModel(
            catalog, preferences, new PcbPricingService(catalog), navigation, confirmation, new Localizer());
    }

    // Save & Continue

    [Fact]
    public void SaveAndContinue_StaysExecutableWhileTheFormIsInvalid()
    {
        viewModel.Postcode = "55-2";

        Assert.True(viewModel.HasErrors);
        Assert.True(viewModel.SaveAndContinueCommand.CanExecute(null));
    }

    [Fact]
    public void SaveAndContinue_Valid_CompletesPreferencesAndOpensQuote()
    {
        viewModel.Postcode = "55-220";

        viewModel.SaveAndContinueCommand.Execute(null);

        Assert.Equal(WorkflowStep.Quote, navigation.CurrentStep);
        Assert.True(navigation.IsPreferencesCompleted);
        Assert.True(navigation.IsQuoteUnlocked);
        Assert.False(navigation.IsOrderUnlocked);
    }

    [Theory]
    [InlineData("", "55-220")]
    [InlineData("PCB-001", "")]
    [InlineData("PCB-001", "55-2")]
    public void SaveAndContinue_Invalid_KeepsQuoteLockedAndRequestsFocus(string projectName, string postcode)
    {
        viewModel.ProjectName = projectName;
        preferences.Postcode = postcode;

        viewModel.SaveAndContinueCommand.Execute(null);
        viewModel.SaveAndContinueCommand.Execute(null);

        Assert.Equal(WorkflowStep.Preferences, navigation.CurrentStep);
        Assert.False(navigation.IsQuoteUnlocked);
        Assert.True(viewModel.HasErrors);
        Assert.Equal(2, viewModel.InvalidSubmitCount);
    }

    // Navigating back and forward

    [Fact]
    public void GoingBackToPreferences_KeepsEveryValueAndTheEstimate()
    {
        ProgressToQuoteWithChangedPreferences();

        Assert.True(navigation.TryNavigateTo(WorkflowStep.Preferences));

        Assert.Equal("Sensor board", viewModel.ProjectName);
        Assert.Equal("55-220", viewModel.Postcode);
        Assert.Same(catalog.Materials[1], viewModel.SelectedMaterial);
        Assert.Same(catalog.SolderMaskColors[1], viewModel.SelectedSolderMaskColor);
        Assert.Same(catalog.BoardThicknesses[1], viewModel.SelectedBoardThickness);
        Assert.Equal("Rush", viewModel.Notes);
        Assert.Equal("$372.50", viewModel.EstimatedPriceText);
        Assert.True(navigation.IsQuoteUnlocked);
    }

    [Fact]
    public void ForwardTabNavigation_WithValidPreferences_ReturnsToQuote()
    {
        ProgressToQuoteWithChangedPreferences();
        navigation.TryNavigateTo(WorkflowStep.Preferences);

        navigation.CurrentStep = WorkflowStep.Quote;

        Assert.Equal(WorkflowStep.Quote, navigation.CurrentStep);
        Assert.Equal(0, viewModel.InvalidSubmitCount);
    }

    [Fact]
    public void EditingAFieldInvalid_MakesQuoteUnavailableImmediately()
    {
        ProgressToQuoteWithChangedPreferences();
        navigation.TryNavigateTo(WorkflowStep.Preferences);
        List<string?> changed = [];
        navigation.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        viewModel.Postcode = "55-2";

        Assert.False(navigation.IsQuoteAvailable);
        Assert.Equal([nameof(WorkflowNavigationState.IsQuoteAvailable)], changed);
        // Nothing is cleared, progress is kept, and Order stays locked.
        Assert.Equal("55-2", preferences.Postcode);
        Assert.True(navigation.IsQuoteUnlocked);
        Assert.True(navigation.IsPreferencesCompleted);
        Assert.False(navigation.IsOrderAvailable);
    }

    [Fact]
    public void ForwardTabNavigation_AfterPreferencesBecameInvalid_IsRefused()
    {
        ProgressToQuoteWithChangedPreferences();
        navigation.TryNavigateTo(WorkflowStep.Preferences);
        viewModel.Postcode = "55-2";

        navigation.CurrentStep = WorkflowStep.Quote;

        Assert.Equal(WorkflowStep.Preferences, navigation.CurrentStep);
        // The disabled tab explains itself; explicit validation feedback stays with Save & Continue.
        Assert.Equal(0, viewModel.InvalidSubmitCount);
    }

    [Fact]
    public void CorrectingTheField_MakesQuoteAvailableAgainWithoutSaving()
    {
        ProgressToQuoteWithChangedPreferences();
        navigation.TryNavigateTo(WorkflowStep.Preferences);
        viewModel.Postcode = "55-2";
        List<string?> changed = [];
        navigation.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        viewModel.Postcode = "55-221";

        Assert.True(navigation.IsQuoteAvailable);
        Assert.Equal([nameof(WorkflowNavigationState.IsQuoteAvailable)], changed);
    }

    [Fact]
    public void ForwardTabNavigation_AfterCorrection_IsAllowedAgain()
    {
        ProgressToQuoteWithChangedPreferences();
        navigation.TryNavigateTo(WorkflowStep.Preferences);
        viewModel.Postcode = "55-2";
        navigation.CurrentStep = WorkflowStep.Quote;

        viewModel.Postcode = "55-221";
        navigation.CurrentStep = WorkflowStep.Quote;

        Assert.Equal(WorkflowStep.Quote, navigation.CurrentStep);
        Assert.Equal("55-221", preferences.Postcode);
    }

    [Fact]
    public void ProgrammaticNavigationToOrder_IsRefusedAtEveryStage()
    {
        Assert.False(navigation.TryNavigateTo(WorkflowStep.Order));

        ProgressToQuoteWithChangedPreferences();

        navigation.CurrentStep = WorkflowStep.Order;
        Assert.Equal(WorkflowStep.Quote, navigation.CurrentStep);
        Assert.False(navigation.IsOrderUnlocked);
    }

    [Theory]
    [InlineData("", "55-220", "")]
    [InlineData("PCB-001", "", "")]
    [InlineData("PCB-001", "55-2", "")]
    [InlineData("PCB-001", "55-220", "LONG")]
    [InlineData("PCB-001", "55-220", "")]
    public void QuoteAvailability_AndNavigation_AlwaysAgree(string projectName, string postcode, string notes)
    {
        ProgressToQuoteWithChangedPreferences();
        navigation.TryNavigateTo(WorkflowStep.Preferences);
        preferences.ProjectName = projectName;
        preferences.Postcode = postcode;
        preferences.Notes = notes == "LONG" ? new string('n', PreferencesViewModel.NotesMaxLength + 1) : notes;

        bool available = navigation.IsQuoteAvailable;

        Assert.Equal(available, navigation.TryNavigateTo(WorkflowStep.Quote));
    }

    [Fact]
    public void ValidDefaultsOnFirstLaunch_DoNotUnlockQuote()
    {
        viewModel.Postcode = "55-220";

        Assert.False(viewModel.HasErrors);
        Assert.False(navigation.IsQuoteUnlocked);
        Assert.False(navigation.IsQuoteAvailable);
        Assert.False(navigation.TryNavigateTo(WorkflowStep.Quote));
    }

    [Fact]
    public void EditingFieldsBeforeTheFirstSave_RaisesNoAvailabilityNotifications()
    {
        List<string?> changed = [];
        navigation.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        viewModel.Postcode = "55-2";
        viewModel.Postcode = "55-220";

        Assert.DoesNotContain(nameof(WorkflowNavigationState.IsQuoteAvailable), changed);
    }

    // Reset to Defaults within the workflow

    [Fact]
    public void ResetAfterReachingQuote_RestoresDefaultsAndKeepsQuoteUnlocked()
    {
        ProgressToQuoteWithChangedPreferences();
        navigation.TryNavigateTo(WorkflowStep.Preferences);

        viewModel.ResetToDefaultsCommand.Execute(null);

        Assert.True(preferences.MatchesDefaults());
        Assert.False(viewModel.HasErrors);
        Assert.True(navigation.IsQuoteUnlocked);
        Assert.Equal(WorkflowStep.Preferences, navigation.CurrentStep);
    }

    [Fact]
    public void ResetAfterReachingQuote_MakesQuoteUnavailableWithoutShowingErrors()
    {
        ProgressToQuoteWithChangedPreferences();
        navigation.TryNavigateTo(WorkflowStep.Preferences);
        viewModel.ResetToDefaultsCommand.Execute(null);

        navigation.CurrentStep = WorkflowStep.Quote;

        // The default empty postcode is invalid, so Quote is unavailable, yet no message appears until Save is clicked.
        Assert.Equal(WorkflowStep.Preferences, navigation.CurrentStep);
        Assert.False(navigation.IsQuoteAvailable);
        Assert.False(viewModel.HasErrors);
    }

    [Fact]
    public void ResetAfterReachingQuote_EnteringAValidPostcode_RestoresQuote()
    {
        ProgressToQuoteWithChangedPreferences();
        navigation.TryNavigateTo(WorkflowStep.Preferences);
        viewModel.ResetToDefaultsCommand.Execute(null);

        viewModel.Postcode = "55-220";

        Assert.True(navigation.IsQuoteAvailable);
        Assert.True(navigation.TryNavigateTo(WorkflowStep.Quote));
    }

    private void ProgressToQuoteWithChangedPreferences()
    {
        viewModel.ProjectName = "Sensor board";
        viewModel.Postcode = "55-220";
        viewModel.SelectedMaterial = catalog.Materials[1];
        viewModel.SelectedSolderMaskColor = catalog.SolderMaskColors[1];
        viewModel.SelectedBoardThickness = catalog.BoardThicknesses[1];
        viewModel.Notes = "Rush";
        viewModel.SaveAndContinueCommand.Execute(null);
        Assert.Equal(WorkflowStep.Quote, navigation.CurrentStep);
    }
}
