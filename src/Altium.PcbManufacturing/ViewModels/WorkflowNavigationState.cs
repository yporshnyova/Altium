using CommunityToolkit.Mvvm.ComponentModel;

namespace Altium.PcbManufacturing.ViewModels;

/// <summary>
/// The order workflow's steps, in order, one per main window tab.
/// </summary>
public enum WorkflowStep
{
    Preferences,
    Quote,
    Order,
}

/// <summary>
/// The single source of truth for the sequential Preferences → Quote → Order workflow: the current step, how far the
/// user has progressed, and which steps can be opened right now.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="HighestUnlockedStep"/> is the only stored progress: a step is <em>unlocked</em> when it is at or before it
/// and <em>completed</em> when it is before it. Unlocking is permanent; it is what the completion indicators show.
/// </para>
/// <para>
/// A step is <em>available</em> when it is unlocked and every earlier step is currently <em>ready</em>, as reported by
/// that step's readiness check (see <see cref="SetReadinessCheck"/>). So a previously unlocked Quote becomes unavailable
/// while Preferences is invalid, and available again as soon as it is corrected. <see cref="TryNavigateTo"/> and the
/// view's enabled state both use <see cref="IsAvailable"/>, so they always agree. The step owning a readiness check
/// calls <see cref="RefreshAvailability"/> when its data changes.
/// </para>
/// <para>
/// A DI singleton. The main window's TabControl binds its selection to <see cref="CurrentStep"/> two-way; a refused
/// request re-raises <see cref="CurrentStep"/> so the selection returns to the actual step.
/// </para>
/// </remarks>
public sealed class WorkflowNavigationState : ObservableObject
{
    private readonly Dictionary<WorkflowStep, Func<bool>> readinessChecks = [];
    private WorkflowStep currentStep = WorkflowStep.Preferences;

    // Last published availability, kept only to raise notifications when it actually changes.
    private bool isQuoteAvailable;
    private bool isOrderAvailable;

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
    /// The furthest step the user has reached.
    /// </summary>
    public WorkflowStep HighestUnlockedStep { get; private set; } = WorkflowStep.Preferences;

    public bool IsPreferencesCompleted => IsCompleted(WorkflowStep.Preferences);

    public bool IsQuoteUnlocked => IsUnlocked(WorkflowStep.Quote);

    public bool IsQuoteCompleted => IsCompleted(WorkflowStep.Quote);

    public bool IsOrderUnlocked => IsUnlocked(WorkflowStep.Order);

    /// <summary>
    /// Whether the Quote tab can be opened now: unlocked, and Preferences currently ready.
    /// </summary>
    public bool IsQuoteAvailable => IsAvailable(WorkflowStep.Quote);

    /// <summary>
    /// Whether the Order tab can be opened now: unlocked, and both earlier steps currently ready.
    /// </summary>
    public bool IsOrderAvailable => IsAvailable(WorkflowStep.Order);

    public bool IsUnlocked(WorkflowStep step) => step <= HighestUnlockedStep;

    public bool IsCompleted(WorkflowStep step) => step < HighestUnlockedStep;

    /// <summary>
    /// Whether <paramref name="step"/> can be opened now. Preferences, the first step, is always available.
    /// </summary>
    public bool IsAvailable(WorkflowStep step)
    {
        if (!IsUnlocked(step))
        {
            return false;
        }

        for (WorkflowStep earlier = WorkflowStep.Preferences; earlier < step; earlier++)
        {
            if (readinessChecks.TryGetValue(earlier, out Func<bool>? isReady) && !isReady())
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Registers whether <paramref name="step"/>'s data currently allows the following steps to be opened. The check
    /// must have no side effects (no validation messages). One check per step; a later registration replaces the
    /// earlier one.
    /// </summary>
    public void SetReadinessCheck(WorkflowStep step, Func<bool> isReady)
    {
        ArgumentNullException.ThrowIfNull(isReady);
        readinessChecks[step] = isReady;
        RefreshAvailability();
    }

    /// <summary>
    /// Re-evaluates availability after data behind a readiness check changed; notifies only what changed.
    /// </summary>
    public void RefreshAvailability()
    {
        bool quote = IsQuoteAvailable;
        if (quote != isQuoteAvailable)
        {
            isQuoteAvailable = quote;
            OnPropertyChanged(nameof(IsQuoteAvailable));
        }

        bool order = IsOrderAvailable;
        if (order != isOrderAvailable)
        {
            isOrderAvailable = order;
            OnPropertyChanged(nameof(IsOrderAvailable));
        }
    }

    /// <summary>
    /// Moves to <paramref name="step"/> if it is available now.
    /// </summary>
    /// <returns>Whether <paramref name="step"/> is now the current step.</returns>
    public bool TryNavigateTo(WorkflowStep step)
    {
        if (step == currentStep)
        {
            return true;
        }

        if (!IsAvailable(step))
        {
            return false;
        }

        SetCurrentStep(step);
        return true;
    }

    /// <summary>
    /// Marks <paramref name="step"/> as completed: unlocks the next step and makes it current. The caller is the step's
    /// own view model, which has just validated it explicitly (showing any errors).
    /// </summary>
    /// <exception cref="InvalidOperationException"><paramref name="step"/> is still locked.</exception>
    public void CompleteStep(WorkflowStep step)
    {
        if (!IsUnlocked(step))
        {
            throw new InvalidOperationException($"The {step} step is locked and cannot be completed.");
        }

        if (step == WorkflowStep.Order)
        {
            return;
        }

        WorkflowStep next = step + 1;
        if (next > HighestUnlockedStep)
        {
            HighestUnlockedStep = next;
            OnPropertyChanged(nameof(HighestUnlockedStep));
            OnPropertyChanged(nameof(IsPreferencesCompleted));
            OnPropertyChanged(nameof(IsQuoteUnlocked));
            OnPropertyChanged(nameof(IsQuoteCompleted));
            OnPropertyChanged(nameof(IsOrderUnlocked));
            RefreshAvailability();
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
    }
}
