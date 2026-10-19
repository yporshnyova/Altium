using Altium.PcbManufacturing.ViewModels;

namespace Altium.PcbManufacturing.Tests;

/// <summary>
/// The Order screen: the final review follows the shared preferences, and Place Order re-validates, asks for
/// confirmation with a summary, and shows the confirmed state only when the user confirms.
/// </summary>
public sealed class OrderViewModelTests
{
    private readonly TestScreens screens = new();

    private OrderViewModel ViewModel => screens.Order;

    [Fact]
    public async Task PlaceOrder_Confirmed_AsksWithTheSummaryAndShowsTheOrderAsPlaced()
    {
        screens.Preferences.ProjectName = "Sensor board";
        screens.CompleteQuote();

        await ViewModel.PlaceOrderCommand.ExecuteAsync(null);

        (string title, string message) = Assert.Single(screens.Confirmation.Requests);
        Assert.Equal("Place Order", title);
        Assert.Contains("Project name: Sensor board", message);
        Assert.Contains("Delivery postcode: 55-220", message);
        Assert.Contains("Quantity: 10 pcs.", message);
        Assert.Contains("Material: FR-4", message);
        Assert.Contains("Estimated total: $1,490.00", message);
        Assert.True(ViewModel.IsOrderCompleted);
        Assert.False(ViewModel.PlaceOrderCommand.CanExecute(null));
    }

    [Fact]
    public async Task PlaceOrder_Cancelled_LeavesTheOrderUnplacedAndEditable()
    {
        screens.CompleteQuote();
        screens.Confirmation.Answer = false;

        await ViewModel.PlaceOrderCommand.ExecuteAsync(null);

        Assert.Single(screens.Confirmation.Requests);
        Assert.False(ViewModel.IsOrderCompleted);
        Assert.True(ViewModel.PlaceOrderCommand.CanExecute(null));
        Assert.False(ViewModel.NewOrderCommand.CanExecute(null));
        Assert.Equal(WorkflowStep.Order, screens.Navigation.CurrentStep);
        Assert.True(screens.Navigation.IsPreferencesAvailable);
        ViewModel.BackToQuoteCommand.Execute(null);
        Assert.Equal(WorkflowStep.Quote, screens.Navigation.CurrentStep);
    }

    // The shared state keeps the last valid quantity, so an invalid quantity field must be caught by re-validation.
    [Theory]
    [InlineData(nameof(PreferencesViewModel.ProjectName), "")]
    [InlineData(nameof(PreferencesViewModel.Postcode), "55-2")]
    [InlineData(nameof(PreferencesViewModel.QuantityInput), "0")]
    public async Task PlaceOrder_WithInvalidInput_IsNotSubmittedAndReturnsToPreferencesWithErrors(string field, string invalid)
    {
        screens.CompleteQuote();
        Edit(field, invalid);
        int focusRequests = screens.Preferences.InvalidSubmitCount;

        await ViewModel.PlaceOrderCommand.ExecuteAsync(null);

        Assert.Empty(screens.Confirmation.Requests);
        Assert.False(ViewModel.IsOrderCompleted);
        Assert.Equal(WorkflowStep.Preferences, screens.Navigation.CurrentStep);
        Assert.NotEmpty(screens.Preferences.GetErrors(field));
        Assert.Equal(focusRequests + 1, screens.Preferences.InvalidSubmitCount);
    }

    // A bound button (or key gesture) asks CanExecute before executing; the command itself does not re-check it.
    [Fact]
    public async Task PlaceOrder_WhileTheConfirmationIsOpen_CannotBeSubmittedAgain()
    {
        screens.CompleteQuote();
        screens.Confirmation.PendingAnswer = new TaskCompletionSource<bool>();
        int canExecuteChanges = 0;
        ViewModel.PlaceOrderCommand.CanExecuteChanged += (_, _) => canExecuteChanges++;

        Task first = ViewModel.PlaceOrderCommand.ExecuteAsync(null);

        Assert.True(ViewModel.PlaceOrderCommand.IsRunning);
        Assert.False(ViewModel.PlaceOrderCommand.CanExecute(null));
        Assert.True(canExecuteChanges > 0);
        Assert.Single(screens.Confirmation.Requests);

        screens.Confirmation.PendingAnswer.SetResult(true);
        await first;

        Assert.True(ViewModel.IsOrderCompleted);
    }

    [Fact]
    public void Summary_FollowsThePreferencesAndMatchesQuotePricing()
    {
        screens.CompleteQuote();
        List<string?> changed = [];
        ViewModel.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        screens.Preferences.SelectedMaterial = screens.Catalog.Materials[1];
        screens.Preferences.QuantityInput = "4";
        screens.Preferences.SelectedSolderMaskColor = screens.Catalog.SolderMaskColors[1];
        screens.Preferences.SelectedBoardThickness = screens.Catalog.BoardThicknesses[1];
        screens.Preferences.Notes = "Panelize 2 × 2";

        Assert.Equal("4 pcs.", ViewModel.QuantityText);
        Assert.Equal("120 × 60 mm · 4 layers", ViewModel.BoardSpecificationText);
        Assert.Equal("Rogers", ViewModel.MaterialName);
        Assert.Equal("Black", ViewModel.SolderMaskText);
        Assert.Equal("#000000", ViewModel.SolderMaskHexColor);
        Assert.Equal("0.8 mm", ViewModel.BoardThicknessText);
        Assert.Equal("Panelize 2 × 2", ViewModel.NotesText);
        Assert.Equal("12 days", ViewModel.EstimatedProductionTimeText);
        Assert.Equal(screens.Quote.PriceEstimate, ViewModel.PriceEstimate);
        Assert.Equal("$372.50", ViewModel.EstimatedUnitPriceText);
        Assert.Equal("$1,490.00", ViewModel.EstimatedTotalPriceText);
        Assert.Contains(nameof(OrderViewModel.EstimatedTotalPriceText), changed);
        Assert.Contains(nameof(OrderViewModel.MaterialName), changed);
    }

    [Fact]
    public async Task PlaceOrder_Confirmed_LocksTheWorkflowOnOrderAgainstEveryBackwardCommand()
    {
        screens.CompleteQuote();

        await ViewModel.PlaceOrderCommand.ExecuteAsync(null);

        Assert.True(screens.Navigation.IsCompleted(WorkflowStep.Order));
        Assert.False(screens.Navigation.IsPreferencesAvailable);
        Assert.False(screens.Navigation.IsQuoteAvailable);
        Assert.True(screens.Navigation.IsOrderAvailable);
        ViewModel.BackToQuoteCommand.Execute(null);
        screens.Quote.BackToPreferencesCommand.Execute(null);
        screens.Navigation.CurrentStep = WorkflowStep.Preferences;
        screens.Preferences.SaveAndContinueCommand.Execute(null);
        screens.Quote.ContinueToOrderCommand.Execute(null);
        Assert.Equal(WorkflowStep.Order, screens.Navigation.CurrentStep);
        Assert.True(ViewModel.IsOrderCompleted);
        Assert.False(ViewModel.PlaceOrderCommand.CanExecute(null));
        Assert.True(ViewModel.NewOrderCommand.CanExecute(null));
    }

    [Fact]
    public async Task NewOrder_RestoresTheDefaultsAndRestartsTheWorkflow()
    {
        screens.Preferences.ProjectName = "Sensor board";
        screens.Preferences.SelectedMaterial = screens.Catalog.Materials[1];
        screens.Preferences.SelectedSolderMaskColor = screens.Catalog.SolderMaskColors[1];
        screens.Preferences.SelectedBoardThickness = screens.Catalog.BoardThicknesses[1];
        screens.Preferences.QuantityInput = "4";
        screens.Preferences.Notes = "Rush";
        screens.CompleteQuote();
        await ViewModel.PlaceOrderCommand.ExecuteAsync(null);

        ViewModel.NewOrderCommand.Execute(null);

        // Preferences: the defaults, without validation messages (the empty postcode is reported on Save, as at start).
        Assert.True(screens.State.MatchesDefaults());
        Assert.Equal("10", screens.Preferences.QuantityInput);
        Assert.False(screens.Preferences.HasErrors);
        // Workflow: only Preferences is unlocked, and it is current.
        Assert.Equal(WorkflowStep.Preferences, screens.Navigation.CurrentStep);
        Assert.False(screens.Navigation.IsOrderCompleted);
        Assert.False(screens.Navigation.IsPreferencesCompleted);
        Assert.False(screens.Navigation.IsQuoteUnlocked);
        Assert.False(screens.Navigation.IsOrderUnlocked);
        Assert.False(screens.Navigation.TryNavigateTo(WorkflowStep.Quote));
        Assert.False(screens.Navigation.TryNavigateTo(WorkflowStep.Order));
        // Derived values follow the defaults.
        Assert.Equal("PCB-001", ViewModel.ProjectName);
        Assert.Equal("FR-4", ViewModel.MaterialName);
        Assert.Equal("1.6 mm", ViewModel.BoardThicknessText);
        Assert.Equal("$1,490.00", ViewModel.EstimatedTotalPriceText);
        Assert.Equal("$1,490.00", screens.Quote.EstimatedTotalPriceText);
        Assert.False(ViewModel.IsOrderCompleted);
        Assert.True(ViewModel.PlaceOrderCommand.CanExecute(null));
        Assert.False(ViewModel.NewOrderCommand.CanExecute(null));
    }

    [Fact]
    public async Task NewOrder_AllowsASecondOrderThroughTheWholeWorkflow()
    {
        screens.CompleteQuote();
        await ViewModel.PlaceOrderCommand.ExecuteAsync(null);
        ViewModel.NewOrderCommand.Execute(null);

        screens.Preferences.QuantityInput = "3";
        screens.CompleteQuote();
        await ViewModel.PlaceOrderCommand.ExecuteAsync(null);

        Assert.Equal(2, screens.Confirmation.Requests.Count);
        Assert.Contains("Quantity: 3 pcs.", screens.Confirmation.Requests[1].Message);
        Assert.True(ViewModel.IsOrderCompleted);
        Assert.Equal(WorkflowStep.Order, screens.Navigation.CurrentStep);
    }

    [Fact]
    public void BackToQuote_OpensQuote()
    {
        screens.CompleteQuote();

        ViewModel.BackToQuoteCommand.Execute(null);

        Assert.Equal(WorkflowStep.Quote, screens.Navigation.CurrentStep);
    }

    private void Edit(string field, string value)
    {
        switch (field)
        {
            case nameof(PreferencesViewModel.ProjectName):
                screens.Preferences.ProjectName = value;
                break;
            case nameof(PreferencesViewModel.Postcode):
                screens.Preferences.Postcode = value;
                break;
            case nameof(PreferencesViewModel.QuantityInput):
                screens.Preferences.QuantityInput = value;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(field), field, null);
        }
    }
}
