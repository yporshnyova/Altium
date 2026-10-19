using Altium.PcbManufacturing.ViewModels;

namespace Altium.PcbManufacturing.Tests;

/// <summary>
/// The Preferences → Quote → Order wizard: first the rules of <see cref="WorkflowNavigationState"/> on its own, then
/// the same workflow driven through the real Preferences and Quote screens.
/// </summary>
public sealed class WorkflowTests
{
    // Navigation rules

    [Fact]
    public void Initially_OnlyPreferencesIsUnlocked()
    {
        WorkflowNavigationState state = new();

        Assert.Equal(WorkflowStep.Preferences, state.CurrentStep);
        Assert.False(state.IsQuoteAvailable);
        Assert.False(state.IsOrderAvailable);
        Assert.False(state.TryNavigateTo(WorkflowStep.Quote));
        Assert.False(state.TryNavigateTo(WorkflowStep.Order));
    }

    [Fact]
    public void RefusedRequest_ReRaisesCurrentStepSoABoundTabReturns()
    {
        WorkflowNavigationState state = new();
        List<string?> changed = [];
        state.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        state.CurrentStep = WorkflowStep.Quote;

        Assert.Equal(WorkflowStep.Preferences, state.CurrentStep);
        Assert.Equal([nameof(WorkflowNavigationState.CurrentStep)], changed);
    }

    [Fact]
    public void CompletingSteps_UnlocksThemInOrder()
    {
        WorkflowNavigationState state = new();
        Assert.Throws<InvalidOperationException>(() => state.CompleteStep(WorkflowStep.Quote));

        state.CompleteStep(WorkflowStep.Preferences);

        Assert.Equal(WorkflowStep.Quote, state.CurrentStep);
        Assert.True(state.IsPreferencesCompleted);
        Assert.False(state.TryNavigateTo(WorkflowStep.Order));

        state.CompleteStep(WorkflowStep.Quote);

        Assert.Equal(WorkflowStep.Order, state.CurrentStep);
        Assert.True(state.IsQuoteCompleted);
    }

    [Fact]
    public void UnlockedStep_IsAvailableOnlyWhileEveryEarlierStepIsReady()
    {
        WorkflowNavigationState state = new();
        bool preferencesReady = true;
        state.SetReadinessCheck(WorkflowStep.Preferences, () => preferencesReady);
        state.CompleteStep(WorkflowStep.Preferences);
        state.CompleteStep(WorkflowStep.Quote);
        state.TryNavigateTo(WorkflowStep.Preferences);

        preferencesReady = false;
        state.RefreshAvailability();

        Assert.False(state.IsQuoteAvailable);
        Assert.False(state.IsOrderAvailable);
        Assert.False(state.TryNavigateTo(WorkflowStep.Quote));
        Assert.True(state.IsQuoteUnlocked); // Progress is kept.

        preferencesReady = true;
        state.RefreshAvailability();

        Assert.True(state.IsOrderAvailable);
        Assert.True(state.TryNavigateTo(WorkflowStep.Quote));
    }

    // Through the screens

    [Fact]
    public void ValidPreferences_DoNotUnlockQuoteUntilSaved()
    {
        TestScreens screens = new();
        screens.Preferences.Postcode = "55-220";

        Assert.False(screens.Navigation.IsQuoteAvailable);

        screens.Preferences.SaveAndContinueCommand.Execute(null);

        Assert.Equal(WorkflowStep.Quote, screens.Navigation.CurrentStep);
        Assert.True(screens.Navigation.IsPreferencesCompleted);
    }

    [Fact]
    public void BackToPreferences_KeepsEveryValueAndTheProgress()
    {
        TestScreens screens = new();
        screens.Preferences.SelectedMaterial = screens.Catalog.Materials[1];
        screens.Preferences.QuantityInput = "7";
        screens.CompletePreferences();

        screens.Quote.BackToPreferencesCommand.Execute(null);

        Assert.Equal(WorkflowStep.Preferences, screens.Navigation.CurrentStep);
        Assert.True(screens.Navigation.IsQuoteAvailable);
        Assert.Equal("55-220", screens.Preferences.Postcode);
        Assert.Equal("7", screens.Preferences.QuantityInput);
        Assert.Same(screens.Catalog.Materials[1], screens.State.SelectedMaterial);
    }

    // Quantity "" → "10" never changes the stored quantity, so availability must follow the field itself.
    [Theory]
    [InlineData(nameof(PreferencesViewModel.ProjectName), "", "Sensor board")]
    [InlineData(nameof(PreferencesViewModel.Postcode), "55-2", "55-220")]
    [InlineData(nameof(PreferencesViewModel.QuantityInput), "", "10")]
    public void InvalidPreferences_DisableQuoteUntilCorrected(string field, string invalid, string valid)
    {
        TestScreens screens = new();
        screens.CompletePreferences();
        screens.Quote.BackToPreferencesCommand.Execute(null);

        Edit(screens.Preferences, field, invalid);

        Assert.False(screens.Navigation.IsQuoteAvailable);
        Assert.False(screens.Navigation.TryNavigateTo(WorkflowStep.Quote));

        Edit(screens.Preferences, field, valid);

        Assert.True(screens.Navigation.TryNavigateTo(WorkflowStep.Quote));
    }

    [Fact]
    public void ResetAfterReachingQuote_KeepsQuoteUnlockedButUnavailableUntilThePostcodeIsEntered()
    {
        TestScreens screens = new();
        screens.CompletePreferences();
        screens.Quote.BackToPreferencesCommand.Execute(null);

        screens.Preferences.ResetToDefaultsCommand.Execute(null);

        // The default empty postcode is invalid, yet no message appears until Save is clicked.
        Assert.True(screens.Navigation.IsQuoteUnlocked);
        Assert.False(screens.Navigation.IsQuoteAvailable);
        Assert.False(screens.Preferences.HasErrors);

        screens.Preferences.Postcode = "55-220";

        Assert.True(screens.Navigation.TryNavigateTo(WorkflowStep.Quote));
    }

    [Fact]
    public void ContinueToOrder_CompletesQuoteAndOpensOrder()
    {
        TestScreens screens = new();
        screens.CompletePreferences();

        screens.Quote.ContinueToOrderCommand.Execute(null);

        Assert.Equal(WorkflowStep.Order, screens.Navigation.CurrentStep);
        Assert.True(screens.Navigation.IsQuoteCompleted);
    }

    [Fact]
    public void ContinueToOrder_WhilePreferencesIsInvalid_DoesNothing()
    {
        TestScreens screens = new();
        screens.CompletePreferences();
        screens.Quote.BackToPreferencesCommand.Execute(null);
        screens.Preferences.QuantityInput = "0";

        screens.Quote.ContinueToOrderCommand.Execute(null);

        Assert.Equal(WorkflowStep.Preferences, screens.Navigation.CurrentStep);
        Assert.False(screens.Navigation.IsOrderUnlocked);
    }

    private static void Edit(PreferencesViewModel preferences, string field, string value)
    {
        switch (field)
        {
            case nameof(PreferencesViewModel.ProjectName):
                preferences.ProjectName = value;
                break;
            case nameof(PreferencesViewModel.Postcode):
                preferences.Postcode = value;
                break;
            case nameof(PreferencesViewModel.QuantityInput):
                preferences.QuantityInput = value;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(field), field, null);
        }
    }
}
