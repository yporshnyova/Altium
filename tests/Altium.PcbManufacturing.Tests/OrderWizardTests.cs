using Altium.PcbManufacturing.ViewModels;

namespace Altium.PcbManufacturing.Tests;

/// <summary>
/// The Preferences → Quote → Order wizard: how it obtains its steps, the guarded transitions, the shared footer, the
/// pending confirmation, completion and New Order.
/// </summary>
public sealed class OrderWizardTests
{
    private readonly TestWizard wizard = new();

    private OrderWizardViewModel Wizard => wizard.Wizard;

    // Steps and their construction

    [Fact]
    public void SharedFactories_KeepDifferentWizardDraftsIndependent()
    {
        OrderWizardViewModel other = new(wizard.Catalog, wizard.Pricing, wizard.PreferencesFactory,
            wizard.QuoteFactory, wizard.OrderFactory, wizard.Localizer);
        ManufacturingOrderDraft otherDraft = other.Preferences.Draft;

        Assert.Same(otherDraft, other.Quote.Draft);
        Assert.Same(otherDraft, other.Order.Draft);
        Assert.NotSame(Wizard.Preferences.Draft, otherDraft);

        other.Preferences.QuantityInput = "4";
        Assert.Equal(4, otherDraft.Quantity);
        Assert.Equal(ManufacturingOrderDraft.DefaultQuantity, wizard.Draft.Quantity);
        Assert.False(Wizard.IsQuoteAvailable);
    }

    [Fact]
    public async Task Steps_AreCreatedOnceAndKeptThroughNavigationAndNewOrders()
    {
        (PreferencesViewModel preferences, QuoteViewModel quote, OrderViewModel order) = (Wizard.Preferences, Wizard.Quote, Wizard.Order);
        await wizard.CompleteOrderAsync();
        Assert.True(Wizard.StartNewOrder());
        wizard.CompleteQuote();
        Wizard.GoBack();
        Wizard.GoBack();
        Wizard.TryNavigateTo(WorkflowStep.Order);

        Assert.Same(preferences, Wizard.Preferences);
        Assert.Same(quote, Wizard.Quote);
        Assert.Same(order, Wizard.Order);
    }

    [Theory]
    [InlineData(WizardState.Preferences, false, "", "Save & Continue")]
    [InlineData(WizardState.Quote, true, "Back to Preferences", "Continue to Order")]
    [InlineData(WizardState.Order, true, "Back to Quote", "Place Order")]
    [InlineData(WizardState.Completed, false, "", "New Order")]
    public async Task Footer_ShowsTheActionsOfEachStep(WizardState state, bool backVisible, string backText, string primaryText)
    {
        await ReachAsync(state);

        Assert.Equal(backVisible, Wizard.IsBackVisible);
        Assert.Equal(backText, Wizard.BackActionText);
        Assert.Equal(backVisible, Wizard.BackCommand.CanExecute(null));
        Assert.Equal(primaryText, Wizard.PrimaryActionText);
        Assert.True(Wizard.PrimaryActionCommand.CanExecute(null));
    }

    [Fact]
    public void Initially_OnlyPreferencesIsAvailable()
    {
        Assert.Equal(WorkflowStep.Preferences, Wizard.CurrentStep);
        Assert.True(Wizard.IsPreferencesAvailable);
        Assert.False(Wizard.IsQuoteAvailable);
        Assert.False(Wizard.IsOrderAvailable);
        Assert.False(Wizard.TryNavigateTo(WorkflowStep.Quote));
        Assert.False(Wizard.TryNavigateTo(WorkflowStep.Order));
        Assert.False(Wizard.TryNavigateTo((WorkflowStep)(-1)));
    }

    [Fact]
    public async Task OperationsOfAnotherStep_AreRejected()
    {
        Assert.False(Wizard.ContinueToOrder());
        Assert.False(await Wizard.PlaceOrderAsync());
        Assert.False(Wizard.StartNewOrder());
        Assert.False(Wizard.GoBack());

        wizard.CompletePreferences();

        Assert.False(Wizard.SubmitPreferences());
        Assert.False(await Wizard.PlaceOrderAsync());
        Assert.Equal(WorkflowStep.Quote, Wizard.CurrentStep);
        Assert.Empty(wizard.Confirmation.Requests);
    }

    [Theory]
    [InlineData("", "55-220", "10")]
    [InlineData("PCB-001", "55-2", "10")]
    [InlineData("PCB-001", "55-220", "0")]
    public async Task SaveAndContinue_Invalid_StaysOnPreferencesWithErrorsAndAFocusRequest(string projectName, string postcode, string quantity)
    {
        wizard.Preferences.ProjectNameInput = projectName;
        wizard.Preferences.PostcodeInput = postcode;
        wizard.Preferences.QuantityInput = quantity;

        await wizard.ClickPrimaryAsync();

        Assert.Equal(WorkflowStep.Preferences, Wizard.CurrentStep);
        Assert.False(Wizard.IsQuoteAvailable);
        Assert.True(wizard.Preferences.HasErrors);
        Assert.Equal(1, wizard.Preferences.InvalidSubmitCount);
    }

    // The draft keeps the last valid quantity, so the raw input must be what blocks progress.
    [Theory]
    [InlineData(nameof(PreferencesViewModel.ProjectNameInput), "", "Sensor board")]
    [InlineData(nameof(PreferencesViewModel.PostcodeInput), "55-2", "55-220")]
    [InlineData(nameof(PreferencesViewModel.QuantityInput), "", "10")] // "" → "10" never changes the draft.
    public void InvalidPreferences_MakeLaterStepsUnavailableUntilCorrected(string field, string invalid, string valid)
    {
        wizard.CompleteQuote();
        Assert.True(Wizard.GoBack());
        Assert.True(Wizard.GoBack());
        List<string?> changed = [];
        Wizard.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        Edit(field, invalid);

        Assert.False(Wizard.IsQuoteAvailable);
        Assert.False(Wizard.IsOrderAvailable);
        Assert.False(Wizard.TryNavigateTo(WorkflowStep.Quote));
        Assert.Contains(nameof(Wizard.IsQuoteAvailable), changed);
        Assert.True(Wizard.IsQuoteCompleted); // Progress is kept.

        Edit(field, valid);

        Assert.True(Wizard.TryNavigateTo(WorkflowStep.Order));
    }

    [Fact]
    public void Back_KeepsEveryValueAndTheProgress()
    {
        wizard.Preferences.ProjectNameInput = "Sensor board";
        wizard.Preferences.QuantityInput = "007";
        wizard.Preferences.NotesInput = "Rush";
        wizard.Draft.SelectedMaterial = wizard.Catalog.Materials[1];
        wizard.CompleteQuote();

        ClickBack();
        Assert.Equal(WorkflowStep.Quote, Wizard.CurrentStep);
        ClickBack();

        Assert.Equal(WorkflowStep.Preferences, Wizard.CurrentStep);
        Assert.True(Wizard.IsQuoteAvailable);
        Assert.True(Wizard.IsOrderAvailable);
        Assert.Equal("Sensor board", wizard.Preferences.ProjectNameInput);
        Assert.Equal("55-220", wizard.Preferences.PostcodeInput);
        Assert.Equal("007", wizard.Preferences.QuantityInput);
        Assert.Equal("Rush", wizard.Draft.Notes);
        Assert.Equal(7, wizard.Draft.Quantity);
        Assert.Same(wizard.Catalog.Materials[1], wizard.Draft.SelectedMaterial);
        Assert.False(wizard.Preferences.HasErrors);
    }

    // Place Order

    [Theory]
    [InlineData(nameof(PreferencesViewModel.ProjectNameInput), "")]
    [InlineData(nameof(PreferencesViewModel.PostcodeInput), "55-2")]
    [InlineData(nameof(PreferencesViewModel.QuantityInput), "0")]
    public async Task PlaceOrder_WithInvalidInput_IsNotSubmittedAndReturnsToPreferencesWithErrors(string field, string invalid)
    {
        wizard.CompleteQuote();
        // Edited in code while Order is shown: the coordinator re-validates instead of trusting the earlier steps.
        Edit(field, invalid);
        int focusRequests = wizard.Preferences.InvalidSubmitCount;

        Assert.False(await Wizard.PlaceOrderAsync());

        Assert.Empty(wizard.Confirmation.Requests);
        Assert.False(Wizard.IsCompleted);
        Assert.Equal(WorkflowStep.Preferences, Wizard.CurrentStep);
        Assert.NotEmpty(wizard.Preferences.GetErrors(field));
        Assert.Equal(focusRequests + 1, wizard.Preferences.InvalidSubmitCount);
    }

    [Fact]
    public async Task PlaceOrder_Cancelled_LeavesTheWorkflowEditable()
    {
        wizard.CompleteQuote();
        wizard.Confirmation.Answer = false;

        await wizard.ClickPrimaryAsync();

        Assert.Single(wizard.Confirmation.Requests);
        Assert.False(Wizard.IsCompleted);
        Assert.Equal(WorkflowStep.Order, Wizard.CurrentStep);
        Assert.Equal("Place Order", Wizard.PrimaryActionText);
        Assert.True(Wizard.PrimaryActionCommand.CanExecute(null));
        Assert.True(Wizard.IsPreferencesAvailable);
        Assert.True(Wizard.GoBack());
    }

    [Fact]
    public async Task PlaceOrder_WhileTheConfirmationIsOpen_DisablesNavigationAndCannotRunTwice()
    {
        wizard.CompleteQuote();
        wizard.Confirmation.PendingAnswer = new TaskCompletionSource<bool>();

        Task pending = wizard.ClickPrimaryAsync();

        Assert.True(Wizard.IsBusy);
        Assert.False(Wizard.PrimaryActionCommand.CanExecute(null));
        Assert.False(Wizard.BackCommand.CanExecute(null));
        Assert.False(Wizard.IsPreferencesAvailable);
        Assert.False(Wizard.IsQuoteAvailable);
        Assert.True(Wizard.IsOrderAvailable); // The current step stays selected.
        Assert.False(Wizard.TryNavigateTo(WorkflowStep.Quote));
        Assert.False(Wizard.GoBack());
        Assert.False(await Wizard.PlaceOrderAsync());
        await Wizard.PrimaryActionCommand.ExecuteAsync(null); // Even when executed directly.
        Assert.Single(wizard.Confirmation.Requests);

        wizard.Confirmation.PendingAnswer.SetResult(true);
        await pending;

        Assert.False(Wizard.IsBusy);
        Assert.True(Wizard.IsCompleted);
    }

    [Fact]
    public async Task PlaceOrder_CancelledAfterWaiting_RestoresNavigation()
    {
        wizard.CompleteQuote();
        wizard.Confirmation.PendingAnswer = new TaskCompletionSource<bool>();
        Task pending = wizard.ClickPrimaryAsync();

        wizard.Confirmation.PendingAnswer.SetResult(false);
        await pending;

        Assert.False(Wizard.IsBusy);
        Assert.True(Wizard.PrimaryActionCommand.CanExecute(null));
        Assert.True(Wizard.BackCommand.CanExecute(null));
        Assert.True(Wizard.IsQuoteAvailable);
        Assert.False(Wizard.IsCompleted);
    }

    [Fact]
    public async Task PlaceOrder_Failure_RestoresNavigation()
    {
        wizard.CompleteQuote();
        wizard.Confirmation.PendingAnswer = new TaskCompletionSource<bool>();
        wizard.Confirmation.PendingAnswer.SetException(new InvalidOperationException("Dialog failed."));

        await Assert.ThrowsAsync<InvalidOperationException>(() => Wizard.PlaceOrderAsync());

        Assert.False(Wizard.IsBusy);
        Assert.False(Wizard.IsCompleted);
        Assert.True(Wizard.PrimaryActionCommand.CanExecute(null));
        Assert.True(Wizard.GoBack());
    }

    [Fact]
    public async Task PlaceOrder_Confirmed_CompletesTheWorkflowAndAllowsOnlyANewOrder()
    {
        wizard.CompleteQuote();

        await wizard.ClickPrimaryAsync();

        Assert.True(Wizard.IsCompleted);
        Assert.Equal(WorkflowStep.Order, Wizard.CurrentStep);
        Assert.False(Wizard.IsPreferencesAvailable);
        Assert.False(Wizard.IsQuoteAvailable);
        Assert.True(Wizard.IsOrderAvailable);
        Assert.False(Wizard.TryNavigateTo(WorkflowStep.Preferences));
        Assert.False(Wizard.GoBack());
        Assert.False(await Wizard.PlaceOrderAsync());
        Assert.Equal(WorkflowStep.Order, Wizard.CurrentStep);
        Assert.Single(wizard.Confirmation.Requests);
    }

    // New Order

    [Fact]
    public async Task NewOrder_ResetsTheDraftInputValidationAndProgress()
    {
        wizard.Preferences.ProjectNameInput = "Sensor board";
        wizard.Preferences.QuantityInput = "4";
        wizard.Preferences.NotesInput = "Rush";
        wizard.Draft.SelectedMaterial = wizard.Catalog.Materials[1];
        wizard.Draft.SelectedSolderMaskColor = wizard.Catalog.SolderMaskColors[1];
        wizard.Draft.SelectedBoardThickness = wizard.Catalog.BoardThicknesses[1];
        await wizard.CompleteOrderAsync();

        await wizard.ClickPrimaryAsync(); // New Order

        // Draft and input: the defaults, without validation messages (the empty postcode is reported on Save, as at start).
        Assert.True(wizard.Draft.MatchesDefaults());
        Assert.Equal("10", wizard.Preferences.QuantityInput);
        Assert.Equal(string.Empty, wizard.Preferences.PostcodeInput);
        Assert.False(wizard.Preferences.HasErrors);
        Assert.False(wizard.Preferences.IsInputValid);
        Assert.Equal(1490.00m, wizard.Draft.TotalPrice);
        // Progress and completion.
        Assert.Equal(WorkflowStep.Preferences, Wizard.CurrentStep);
        Assert.False(Wizard.IsCompleted);
        Assert.False(Wizard.IsPreferencesCompleted);
        Assert.False(Wizard.IsQuoteCompleted);
        Assert.False(Wizard.TryNavigateTo(WorkflowStep.Quote));
        Assert.False(Wizard.TryNavigateTo(WorkflowStep.Order));
        Assert.Equal("Save & Continue", Wizard.PrimaryActionText);
        Assert.False(Wizard.IsBackVisible);
        Assert.Equal("Green", wizard.Quote.ParameterGroups.SelectMany(g => g.Parameters).Single(p => p.Label == "Solder mask").Value);
    }

    [Fact]
    public async Task NewOrder_ASecondOrderCompletesWithTheSameWizard()
    {
        await wizard.CompleteOrderAsync();
        Assert.True(Wizard.StartNewOrder());

        wizard.Preferences.PostcodeInput = "00-950";
        wizard.Preferences.QuantityInput = "3";
        await wizard.ClickPrimaryAsync();
        await wizard.ClickPrimaryAsync();
        await wizard.ClickPrimaryAsync();

        Assert.Equal(2, wizard.Confirmation.Requests.Count);
        Assert.Contains("Quantity: 3 pcs.", wizard.Confirmation.Requests[1].Message);
        Assert.True(Wizard.IsCompleted);
        Assert.Equal(WorkflowStep.Order, Wizard.CurrentStep);
    }

    public enum WizardState
    {
        Preferences,
        Quote,
        Order,
        Completed,
    }

    private async Task ReachAsync(WizardState state)
    {
        switch (state)
        {
            case WizardState.Quote:
                wizard.CompletePreferences();
                break;
            case WizardState.Order:
                wizard.CompleteQuote();
                break;
            case WizardState.Completed:
                await wizard.CompleteOrderAsync();
                break;
        }
    }

    // As a bound button does: only when it can execute.
    private void ClickBack()
    {
        Assert.True(Wizard.BackCommand.CanExecute(null), "Back is disabled.");
        Wizard.BackCommand.Execute(null);
    }

    private void Edit(string field, string value)
    {
        switch (field)
        {
            case nameof(PreferencesViewModel.ProjectNameInput):
                wizard.Preferences.ProjectNameInput = value;
                break;
            case nameof(PreferencesViewModel.PostcodeInput):
                wizard.Preferences.PostcodeInput = value;
                break;
            case nameof(PreferencesViewModel.QuantityInput):
                wizard.Preferences.QuantityInput = value;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(field), field, null);
        }
    }
}
