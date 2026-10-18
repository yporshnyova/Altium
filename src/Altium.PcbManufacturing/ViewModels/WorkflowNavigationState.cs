using CommunityToolkit.Mvvm.ComponentModel;

namespace Altium.PcbManufacturing.ViewModels;

/// <summary>
/// The order workflow's steps, one per main window tab.
/// </summary>
public enum WorkflowStep
{
    Preferences,
    Quote,
    Order,
}

/// <summary>
/// The single source of truth for which workflow step (tab) is shown.
/// </summary>
/// <remarks>
/// A DI singleton: the main window's TabControl binds its selection to <see cref="CurrentStep"/> two-way, so both
/// tab clicks and view model actions such as Save &amp; Continue go through this one value.
/// </remarks>
public sealed partial class WorkflowNavigationState : ObservableObject
{
    [ObservableProperty]
    private WorkflowStep currentStep = WorkflowStep.Preferences;
}
