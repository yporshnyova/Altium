using Altium.PcbManufacturing.ViewModels;

namespace Altium.PcbManufacturing.Tests;

/// <summary>
/// Navigation rules of <see cref="WorkflowNavigationState"/>, independent of WPF.
/// </summary>
public sealed class WorkflowNavigationStateTests
{
    private readonly WorkflowNavigationState state = new();
    private readonly List<string?> changed = [];

    public WorkflowNavigationStateTests()
    {
        state.PropertyChanged += (_, e) => changed.Add(e.PropertyName);
    }

    // Initial state

    [Fact]
    public void Initially_PreferencesIsCurrentAndTheOnlyUnlockedStep()
    {
        Assert.Equal(WorkflowStep.Preferences, state.CurrentStep);
        Assert.Equal(WorkflowStep.Preferences, state.HighestUnlockedStep);
        Assert.True(state.IsUnlocked(WorkflowStep.Preferences));
        Assert.False(state.IsQuoteUnlocked);
        Assert.False(state.IsOrderUnlocked);
        Assert.False(state.IsPreferencesCompleted);
        Assert.False(state.IsQuoteCompleted);
    }

    // Locked steps

    [Theory]
    [InlineData(WorkflowStep.Quote)]
    [InlineData(WorkflowStep.Order)]
    public void TryNavigateTo_LockedStep_IsRefused(WorkflowStep step)
    {
        Assert.False(state.TryNavigateTo(step));
        Assert.Equal(WorkflowStep.Preferences, state.CurrentStep);
    }

    [Theory]
    [InlineData(WorkflowStep.Quote)]
    [InlineData(WorkflowStep.Order)]
    public void SettingCurrentStepToLockedStep_IsRefusedAndReRaised(WorkflowStep step)
    {
        state.CurrentStep = step;

        Assert.Equal(WorkflowStep.Preferences, state.CurrentStep);
        // Re-raised so a two-way bound view can return to the actual step.
        Assert.Equal([nameof(WorkflowNavigationState.CurrentStep)], changed);
    }

    [Fact]
    public void TryNavigateTo_CurrentStep_SucceedsWithoutNotification()
    {
        Assert.True(state.TryNavigateTo(WorkflowStep.Preferences));
        Assert.Empty(changed);
    }

    // Completing steps

    [Fact]
    public void CompleteStep_Preferences_UnlocksAndOpensQuoteOnly()
    {
        state.CompleteStep(WorkflowStep.Preferences);

        Assert.Equal(WorkflowStep.Quote, state.CurrentStep);
        Assert.True(state.IsPreferencesCompleted);
        Assert.True(state.IsQuoteUnlocked);
        Assert.False(state.IsQuoteCompleted);
        Assert.False(state.IsOrderUnlocked);
    }

    [Fact]
    public void CompleteStep_Preferences_NotifiesProgressAndCurrentStep()
    {
        state.CompleteStep(WorkflowStep.Preferences);

        Assert.Contains(nameof(WorkflowNavigationState.HighestUnlockedStep), changed);
        Assert.Contains(nameof(WorkflowNavigationState.IsPreferencesCompleted), changed);
        Assert.Contains(nameof(WorkflowNavigationState.IsQuoteUnlocked), changed);
        Assert.Contains(nameof(WorkflowNavigationState.CurrentStep), changed);
    }

    [Fact]
    public void CompleteStep_LockedStep_Throws() =>
        Assert.Throws<InvalidOperationException>(() => state.CompleteStep(WorkflowStep.Quote));

    [Fact]
    public void CompleteStep_AgainAfterGoingBack_ReopensQuoteWithoutUnlockingMore()
    {
        state.CompleteStep(WorkflowStep.Preferences);
        state.TryNavigateTo(WorkflowStep.Preferences);

        state.CompleteStep(WorkflowStep.Preferences);

        Assert.Equal(WorkflowStep.Quote, state.CurrentStep);
        Assert.False(state.IsOrderUnlocked);
    }

    [Fact]
    public void CompleteStep_QuoteInTheFuture_UnlocksOrder()
    {
        state.CompleteStep(WorkflowStep.Preferences);

        state.CompleteStep(WorkflowStep.Quote);

        Assert.Equal(WorkflowStep.Order, state.CurrentStep);
        Assert.True(state.IsOrderUnlocked);
        Assert.True(state.IsQuoteCompleted);
    }

    // Moving between unlocked steps

    [Fact]
    public void NavigatingBack_IsAllowedAndKeepsLaterStepsUnlocked()
    {
        state.CompleteStep(WorkflowStep.Preferences);

        Assert.True(state.TryNavigateTo(WorkflowStep.Preferences));

        Assert.Equal(WorkflowStep.Preferences, state.CurrentStep);
        Assert.True(state.IsQuoteUnlocked);
        Assert.True(state.IsPreferencesCompleted);
    }

    // Availability: unlocked and every earlier step currently ready

    [Fact]
    public void Initially_LaterStepsAreUnavailable()
    {
        Assert.True(state.IsAvailable(WorkflowStep.Preferences));
        Assert.False(state.IsQuoteAvailable);
        Assert.False(state.IsOrderAvailable);
    }

    [Fact]
    public void LockedStep_IsUnavailableEvenWhenEarlierStepsAreReady()
    {
        // Valid defaults alone never unlock Quote; only CompleteStep does.
        state.SetReadinessCheck(WorkflowStep.Preferences, () => true);

        Assert.False(state.IsQuoteAvailable);
        Assert.False(state.TryNavigateTo(WorkflowStep.Quote));
    }

    [Fact]
    public void UnlockedQuote_IsAvailableWhilePreferencesIsReady()
    {
        state.SetReadinessCheck(WorkflowStep.Preferences, () => true);
        state.CompleteStep(WorkflowStep.Preferences);
        state.TryNavigateTo(WorkflowStep.Preferences);

        Assert.True(state.IsQuoteAvailable);
        Assert.True(state.TryNavigateTo(WorkflowStep.Quote));
    }

    [Fact]
    public void UnlockedQuote_BecomesUnavailableWhenPreferencesIsNotReady()
    {
        bool ready = true;
        state.SetReadinessCheck(WorkflowStep.Preferences, () => ready);
        state.CompleteStep(WorkflowStep.Preferences);
        state.TryNavigateTo(WorkflowStep.Preferences);
        changed.Clear();

        ready = false;
        state.RefreshAvailability();

        Assert.False(state.IsQuoteAvailable);
        Assert.Equal([nameof(WorkflowNavigationState.IsQuoteAvailable)], changed);
        // Progress is kept: Quote stays unlocked and Preferences completed.
        Assert.True(state.IsQuoteUnlocked);
        Assert.True(state.IsPreferencesCompleted);
    }

    [Fact]
    public void UnavailableQuote_RefusesNavigationAndReRaisesCurrentStep()
    {
        bool ready = true;
        state.SetReadinessCheck(WorkflowStep.Preferences, () => ready);
        state.CompleteStep(WorkflowStep.Preferences);
        state.TryNavigateTo(WorkflowStep.Preferences);
        ready = false;
        state.RefreshAvailability();
        changed.Clear();

        state.CurrentStep = WorkflowStep.Quote;

        Assert.Equal(WorkflowStep.Preferences, state.CurrentStep);
        Assert.Equal([nameof(WorkflowNavigationState.CurrentStep)], changed);
    }

    [Fact]
    public void Quote_BecomesAvailableAgainWhenPreferencesIsReadyAgain()
    {
        bool ready = true;
        state.SetReadinessCheck(WorkflowStep.Preferences, () => ready);
        state.CompleteStep(WorkflowStep.Preferences);
        state.TryNavigateTo(WorkflowStep.Preferences);
        ready = false;
        state.RefreshAvailability();
        changed.Clear();

        ready = true;
        state.RefreshAvailability();

        Assert.True(state.IsQuoteAvailable);
        Assert.Equal([nameof(WorkflowNavigationState.IsQuoteAvailable)], changed);
        Assert.True(state.TryNavigateTo(WorkflowStep.Quote));
    }

    [Fact]
    public void RefreshAvailability_WithoutChange_DoesNotNotify()
    {
        state.SetReadinessCheck(WorkflowStep.Preferences, () => true);
        state.CompleteStep(WorkflowStep.Preferences);
        changed.Clear();

        state.RefreshAvailability();
        state.RefreshAvailability();

        Assert.Empty(changed);
    }

    [Fact]
    public void CompleteStep_NotifiesQuoteAvailability()
    {
        state.CompleteStep(WorkflowStep.Preferences);

        Assert.True(state.IsQuoteAvailable);
        Assert.Contains(nameof(WorkflowNavigationState.IsQuoteAvailable), changed);
    }

    [Fact]
    public void Preferences_StaysAvailableWhenItIsNotReady()
    {
        state.SetReadinessCheck(WorkflowStep.Preferences, () => false);
        state.CompleteStep(WorkflowStep.Preferences);

        Assert.True(state.IsAvailable(WorkflowStep.Preferences));
        Assert.True(state.TryNavigateTo(WorkflowStep.Preferences));
    }

    [Fact]
    public void Order_RequiresEveryEarlierStepToBeReady()
    {
        bool preferencesReady = true;
        state.SetReadinessCheck(WorkflowStep.Preferences, () => preferencesReady);
        state.CompleteStep(WorkflowStep.Preferences);
        state.CompleteStep(WorkflowStep.Quote);

        preferencesReady = false;
        state.RefreshAvailability();

        Assert.False(state.IsOrderAvailable);
        Assert.False(state.IsQuoteAvailable);
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(false, false)]
    [InlineData(true, true)]
    [InlineData(true, false)]
    public void NavigationAndAvailability_AlwaysAgree(bool quoteUnlocked, bool preferencesReady)
    {
        state.SetReadinessCheck(WorkflowStep.Preferences, () => preferencesReady);
        if (quoteUnlocked)
        {
            state.CompleteStep(WorkflowStep.Preferences);
            state.TryNavigateTo(WorkflowStep.Preferences);
        }

        foreach (WorkflowStep step in Enum.GetValues<WorkflowStep>())
        {
            Assert.Equal(state.IsAvailable(step), state.TryNavigateTo(step));
            state.TryNavigateTo(WorkflowStep.Preferences);
        }
    }

    [Fact]
    public void OrderStaysLockedAfterPreferences_EvenWhenRequestedFromQuote()
    {
        state.CompleteStep(WorkflowStep.Preferences);

        Assert.False(state.TryNavigateTo(WorkflowStep.Order));
        Assert.Equal(WorkflowStep.Quote, state.CurrentStep);
    }
}
