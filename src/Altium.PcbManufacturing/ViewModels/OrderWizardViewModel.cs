using System.ComponentModel;
using Altium.PcbManufacturing.Localization;
using Altium.PcbManufacturing.Models;
using Altium.PcbManufacturing.Services;
using Altium.PcbManufacturing.ViewModels.Factories;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Altium.PcbManufacturing.ViewModels;

/// <summary>
/// The Preferences → Quote → Order wizard: the single owner of the current step, progress, completion and the shared
/// navigation footer. It coordinates the steps' operations and moves on only when they succeed.
/// </summary>

public sealed partial class OrderWizardViewModel : ObservableObject
{
    private readonly ManufacturingOrderDraft draft;
    private readonly Localizer localizer;

    private WorkflowStep currentStep = WorkflowStep.Preferences;
    private WorkflowStep highestUnlockedStep = WorkflowStep.Preferences;

    public OrderWizardViewModel(
        PcbCatalog catalog,
        PcbPricingService pricing,
        PreferencesViewModelFactory preferencesFactory,
        QuoteViewModelFactory quoteFactory,
        OrderViewModelFactory orderFactory,
        Localizer localizer)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(pricing);
        ArgumentNullException.ThrowIfNull(preferencesFactory);
        ArgumentNullException.ThrowIfNull(quoteFactory);
        ArgumentNullException.ThrowIfNull(orderFactory);
        ArgumentNullException.ThrowIfNull(localizer);

        // The wizard session owns its draft: it is created here, once, and shared by every step.
        draft = new ManufacturingOrderDraft(catalog, pricing);

        this.localizer = localizer;

        Preferences = preferencesFactory.Create(draft);
        Quote = quoteFactory.Create(draft);
        Order = orderFactory.Create(draft);

        // Availability of the later steps depends on whether the Preferences input is valid.
        Preferences.PropertyChanged += OnPreferencesPropertyChanged;
        this.localizer.PropertyChanged += OnLocalizerPropertyChanged;
    }

    public PreferencesViewModel Preferences { get; }

    public QuoteViewModel Quote { get; }

    public OrderViewModel Order { get; }

    /// <summary>
    /// The step shown. Bound two-way to the tabs: setting it navigates if the step is available, and a refused request
    /// re-raises the property so the selection returns to the actual step.
    /// </summary>
    public WorkflowStep CurrentStep
    {
        get => currentStep;
        set
        {
            if (!TryNavigateTo(value))
            {
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Whether the order has been placed. The workflow is then finished: only <see cref="StartNewOrder"/> follows.
    /// </summary>
    public bool IsCompleted { get; private set; }

    /// <summary>
    /// Whether an asynchronous transition (the order confirmation) is pending; navigation is disabled meanwhile.
    /// </summary>
    public bool IsBusy { get; private set; }

    public bool IsPreferencesCompleted => IsStepCompleted(WorkflowStep.Preferences);

    public bool IsQuoteCompleted => IsStepCompleted(WorkflowStep.Quote);

    public bool IsPreferencesAvailable => IsAvailable(WorkflowStep.Preferences);

    public bool IsQuoteAvailable => IsAvailable(WorkflowStep.Quote);

    public bool IsOrderAvailable => IsAvailable(WorkflowStep.Order);

    // The shared footer
    public string PrimaryActionText => localizer[
        IsCompleted ? nameof(Strings.Actions_NewOrder)
        : currentStep switch
        {
            WorkflowStep.Preferences => nameof(Strings.Actions_SaveAndContinue),
            WorkflowStep.Quote => nameof(Strings.Actions_ContinueToOrder),
            WorkflowStep.Order => nameof(Strings.Actions_PlaceOrder),
            _ => throw new ArgumentOutOfRangeException()
        }];

    public bool IsBackVisible => !IsCompleted && currentStep != WorkflowStep.Preferences;

    public string BackActionText => !IsBackVisible
        ? string.Empty
        : localizer[currentStep == WorkflowStep.Quote ? nameof(Strings.Actions_BackToPreferences) : nameof(Strings.Actions_BackToQuote)];

    /// <summary>
    /// Whether <paramref name="step"/> can be opened now; see the class remarks.
    /// </summary>
    public bool IsAvailable(WorkflowStep step)
    {
        if (IsCompleted)
        {
            return step == WorkflowStep.Order;
        }

        if (step == currentStep)
        {
            return true;
        }

        return !IsBusy
            && step <= highestUnlockedStep
            && (step == WorkflowStep.Preferences || Preferences.IsInputValid);
    }

    /// <summary>
    /// Opens <paramref name="step"/> if it is available; nothing entered is changed.
    /// </summary>
    /// <returns>Whether <paramref name="step"/> is now the current step.</returns>
    public bool TryNavigateTo(WorkflowStep step)
    {
        if (step == currentStep)
        {
            return true;
        }

        if (!Enum.IsDefined(step) || !IsAvailable(step))
        {
            return false;
        }

        SetCurrentStep(step);
        return true;
    }

    /// <summary>
    /// Save &amp; Continue: validates the Preferences input with visible messages and, when it is valid, unlocks and
    /// opens Quote. Nothing is written to disk.
    /// </summary>
    public bool SubmitPreferences()
    {
        if (!IsEditable(WorkflowStep.Preferences) || !Preferences.ValidateAndShowErrors())
        {
            return false;
        }

        Advance();
        return true;
    }

    /// <summary>
    /// Continue to Order: unlocks and opens Order, provided the Preferences input is still valid.
    /// </summary>
    public bool ContinueToOrder()
    {
        if (!IsEditable(WorkflowStep.Quote) || !Preferences.IsInputValid)
        {
            return false;
        }

        Advance();
        return true;
    }

    /// <summary>
    /// Place Order: re-validates the Preferences input and, when it is valid, lets the Order step ask for confirmation;
    /// the workflow is completed only when the user confirms. Invalid input is never submitted: Preferences is opened
    /// with its errors shown. Navigation is disabled until the confirmation is answered, failed or not.
    /// </summary>
    public async Task<bool> PlaceOrderAsync()
    {
        if (!IsEditable(WorkflowStep.Order))
        {
            return false;
        }

        if (!Preferences.ValidateAndShowErrors())
        {
            SetCurrentStep(WorkflowStep.Preferences);
            return false;
        }

        SetBusy(true);
        try
        {
            if (!await Order.PlaceOrderAsync())
            {
                return false;
            }

            IsCompleted = true;
            RefreshState();
            return true;
        }
        finally
        {
            SetBusy(false);
        }
    }

    /// <summary>
    /// After a completed order, starts a new one: the draft's defaults, Preferences input without validation messages,
    /// an unplaced order, and only Preferences unlocked. Language and theme are application settings and are kept.
    /// </summary>
    public bool StartNewOrder()
    {
        if (!IsCompleted || IsBusy)
        {
            return false;
        }

        draft.ResetToDefaults();
        Preferences.ResetInput();
        IsCompleted = false;
        highestUnlockedStep = WorkflowStep.Preferences;
        SetCurrentStep(WorkflowStep.Preferences);
        return true;
    }

    /// <summary>
    /// The footer's primary action for the current state: Save &amp; Continue, Continue to Order, Place Order or New Order.
    /// It cannot run again while it is running.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanExecutePrimaryAction))]
    private async Task PrimaryActionAsync()
    {
        if (IsCompleted)
        {
            StartNewOrder();
            return;
        }

        switch (currentStep)
        {
            case WorkflowStep.Preferences:
                SubmitPreferences();
                break;
            case WorkflowStep.Quote:
                ContinueToOrder();
                break;
            case WorkflowStep.Order:
                await PlaceOrderAsync();
                break;
        }
    }

    private bool CanExecutePrimaryAction() => !IsBusy;

    /// <summary>
    /// Back: opens the previous step; everything entered is kept.
    /// </summary>
    /// <returns>Whether the previous step was opened.</returns>
    public bool GoBack() => CanGoBack() && TryNavigateTo(currentStep - 1);

    /// <summary>
    /// The footer's Back action.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanGoBack))]
    private void Back() => GoBack();

    private bool CanGoBack() => IsBackVisible && IsAvailable(currentStep - 1);

    private bool IsStepCompleted(WorkflowStep step) => IsCompleted || step < highestUnlockedStep;

    private bool IsEditable(WorkflowStep step) => !IsCompleted && !IsBusy && currentStep == step;

    // Completes the current step: unlocks the next one and opens it.
    private void Advance()
    {
        WorkflowStep next = currentStep + 1;
        if (next > highestUnlockedStep)
        {
            highestUnlockedStep = next;
        }

        SetCurrentStep(next);
    }

    private void SetCurrentStep(WorkflowStep step)
    {
        if (step != currentStep)
        {
            currentStep = step;
            OnPropertyChanged(nameof(CurrentStep));
        }

        RefreshState();
    }

    private void SetBusy(bool value)
    {
        IsBusy = value;
        RefreshState();
    }

    // Everything below derives from the step, progress, completion, busy state and Preferences validity; it is cheap,
    // so all of it is re-announced together whenever one of those changes.
    private void RefreshState()
    {
        OnPropertyChanged(nameof(IsCompleted));
        OnPropertyChanged(nameof(IsBusy));
        OnPropertyChanged(nameof(IsPreferencesCompleted));
        OnPropertyChanged(nameof(IsQuoteCompleted));
        OnPropertyChanged(nameof(IsPreferencesAvailable));
        OnPropertyChanged(nameof(IsQuoteAvailable));
        OnPropertyChanged(nameof(IsOrderAvailable));
        OnPropertyChanged(nameof(PrimaryActionText));
        OnPropertyChanged(nameof(IsBackVisible));
        OnPropertyChanged(nameof(BackActionText));
        PrimaryActionCommand.NotifyCanExecuteChanged();
        BackCommand.NotifyCanExecuteChanged();
    }

    private void OnPreferencesPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PreferencesViewModel.IsInputValid))
        {
            RefreshState();
        }
    }

    private void OnLocalizerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Localizer.Language))
        {
            OnPropertyChanged(nameof(PrimaryActionText));
            OnPropertyChanged(nameof(BackActionText));
        }
    }
}
