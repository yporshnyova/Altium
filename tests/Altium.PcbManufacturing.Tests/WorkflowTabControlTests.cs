using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using Altium.PcbManufacturing.ViewModels;
using Altium.PcbManufacturing.Views.Behaviors;

namespace Altium.PcbManufacturing.Tests;

/// <summary>
/// A live WPF TabControl bound to <see cref="WorkflowNavigationState"/> the way MainWindow binds it: the selection must
/// follow the workflow and snap back when a request is refused, and each tab is enabled only while it is available.
/// </summary>
/// <remarks>Not shown on screen: covers binding and selection logic, not mouse, keyboard or rendering.</remarks>
public sealed class WorkflowTabControlTests
{
    [Fact]
    public void SelectingALockedTab_SnapsBackToTheCurrentStep() => Sta.Run(() =>
    {
        (TabControl tabs, WorkflowNavigationState state) = CreateBoundTabs();

        tabs.SelectedIndex = 1;

        Assert.Equal(WorkflowStep.Preferences, state.CurrentStep);
        Assert.Equal(0, tabs.SelectedIndex);
    });

    [Fact]
    public void CompletingPreferences_SelectsQuoteAndBackNavigationWorks() => Sta.Run(() =>
    {
        (TabControl tabs, WorkflowNavigationState state) = CreateBoundTabs();

        state.CompleteStep(WorkflowStep.Preferences);
        Assert.Equal(1, tabs.SelectedIndex);

        tabs.SelectedIndex = 0;
        Assert.Equal(WorkflowStep.Preferences, state.CurrentStep);
    });

    [Fact]
    public void UnavailableQuoteTab_IsDisabledAndCannotBeSelected() => Sta.Run(() =>
    {
        (TabControl tabs, WorkflowNavigationState state) = CreateBoundTabs();
        var quote = (TabItem)tabs.Items[1];
        bool preferencesReady = true;
        state.SetReadinessCheck(WorkflowStep.Preferences, () => preferencesReady);
        state.CompleteStep(WorkflowStep.Preferences);
        tabs.SelectedIndex = 0;
        Assert.True(quote.IsEnabled);

        preferencesReady = false;
        state.RefreshAvailability();
        tabs.SelectedIndex = 1;

        Assert.False(quote.IsEnabled);
        Assert.Equal(0, tabs.SelectedIndex);
        Assert.Equal(WorkflowStep.Preferences, state.CurrentStep);
    });

    private static (TabControl Tabs, WorkflowNavigationState State) CreateBoundTabs()
    {
        WorkflowNavigationState state = new();
        TabControl tabs = new() { SelectedValuePath = nameof(TabItem.Tag) };
        tabs.Items.Add(new TabItem { Tag = WorkflowStep.Preferences });
        tabs.Items.Add(new TabItem { Tag = WorkflowStep.Quote });
        tabs.Items.Add(new TabItem { Tag = WorkflowStep.Order });
        tabs.SetBinding(Selector.SelectedValueProperty, new Binding(nameof(WorkflowNavigationState.CurrentStep))
        {
            Source = state,
            Mode = BindingMode.TwoWay,
        });
        // As in MainWindow.xaml: a tab is enabled only while its step is available.
        ((TabItem)tabs.Items[1]).SetBinding(UIElement.IsEnabledProperty, new Binding(nameof(WorkflowNavigationState.IsQuoteAvailable)) { Source = state });
        ((TabItem)tabs.Items[2]).SetBinding(UIElement.IsEnabledProperty, new Binding(nameof(WorkflowNavigationState.IsOrderAvailable)) { Source = state });
        SelectionFollowsSourceBehavior.SetIsEnabled(tabs, true);
        return (tabs, state);
    }
}
