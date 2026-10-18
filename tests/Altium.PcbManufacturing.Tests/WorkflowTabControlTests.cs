using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using Altium.PcbManufacturing.ViewModels;
using Altium.PcbManufacturing.Views.Behaviors;

namespace Altium.PcbManufacturing.Tests;

/// <summary>
/// A live WPF TabControl bound to <see cref="WorkflowNavigationState"/> the way MainWindow binds it: the selection must
/// follow the workflow and return to the current step when a request is refused.
/// </summary>
/// <remarks>Not shown on screen: covers binding and selection logic, not mouse, keyboard or rendering.</remarks>
public sealed class WorkflowTabControlTests
{
    [Fact]
    public void Initially_SelectsPreferences() => OnStaThread(() =>
    {
        (TabControl tabs, WorkflowNavigationState state) = CreateBoundTabs();

        Assert.Equal(0, tabs.SelectedIndex);
        Assert.Equal(WorkflowStep.Preferences, state.CurrentStep);
    });

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void SelectingLockedTab_SnapsBackToCurrentStep(int index) => OnStaThread(() =>
    {
        (TabControl tabs, WorkflowNavigationState state) = CreateBoundTabs();

        tabs.SelectedIndex = index;

        Assert.Equal(WorkflowStep.Preferences, state.CurrentStep);
        Assert.Equal(0, tabs.SelectedIndex);
        Assert.Equal(WorkflowStep.Preferences, tabs.SelectedValue);
    });

    [Fact]
    public void CompletingPreferences_SelectsQuoteTab() => OnStaThread(() =>
    {
        (TabControl tabs, WorkflowNavigationState state) = CreateBoundTabs();

        state.CompleteStep(WorkflowStep.Preferences);

        Assert.Equal(1, tabs.SelectedIndex);
    });

    [Fact]
    public void SelectingUnlockedEarlierTab_NavigatesBack() => OnStaThread(() =>
    {
        (TabControl tabs, WorkflowNavigationState state) = CreateBoundTabs();
        state.CompleteStep(WorkflowStep.Preferences);

        tabs.SelectedIndex = 0;

        Assert.Equal(WorkflowStep.Preferences, state.CurrentStep);
    });

    [Fact]
    public void SelectingUnlockedButUnavailableTab_SnapsBack() => OnStaThread(() =>
    {
        (TabControl tabs, WorkflowNavigationState state) = CreateBoundTabs();
        bool preferencesReady = true;
        state.SetReadinessCheck(WorkflowStep.Preferences, () => preferencesReady);
        state.CompleteStep(WorkflowStep.Preferences);
        tabs.SelectedIndex = 0;
        preferencesReady = false;
        state.RefreshAvailability();

        tabs.SelectedIndex = 1;

        Assert.Equal(WorkflowStep.Preferences, state.CurrentStep);
        Assert.Equal(0, tabs.SelectedIndex);
    });

    [Fact]
    public void QuoteTabEnabledState_FollowsAvailability() => OnStaThread(() =>
    {
        (TabControl tabs, WorkflowNavigationState state) = CreateBoundTabs();
        var quote = (TabItem)tabs.Items[1];
        bool preferencesReady = true;
        state.SetReadinessCheck(WorkflowStep.Preferences, () => preferencesReady);
        Assert.False(quote.IsEnabled);

        state.CompleteStep(WorkflowStep.Preferences);
        tabs.SelectedIndex = 0;
        Assert.True(quote.IsEnabled);

        preferencesReady = false;
        state.RefreshAvailability();
        Assert.False(quote.IsEnabled);

        preferencesReady = true;
        state.RefreshAvailability();
        Assert.True(quote.IsEnabled);
        Assert.False(((TabItem)tabs.Items[2]).IsEnabled);
    });

    [Fact]
    public void SelectingOrderAfterQuoteUnlocked_StillSnapsBack() => OnStaThread(() =>
    {
        (TabControl tabs, WorkflowNavigationState state) = CreateBoundTabs();
        state.CompleteStep(WorkflowStep.Preferences);

        tabs.SelectedIndex = 2;

        Assert.Equal(WorkflowStep.Quote, state.CurrentStep);
        Assert.Equal(1, tabs.SelectedIndex);
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

    private static void OnStaThread(Action test)
    {
        ExceptionDispatchInfo? failure = null;
        Thread thread = new(() =>
        {
            try
            {
                test();
            }
            catch (Exception ex)
            {
                failure = ExceptionDispatchInfo.Capture(ex);
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        failure?.Throw();
    }
}
