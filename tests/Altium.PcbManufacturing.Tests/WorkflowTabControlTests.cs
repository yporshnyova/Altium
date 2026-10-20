using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using Altium.PcbManufacturing.ViewModels;
using Altium.PcbManufacturing.Views.Behaviors;

namespace Altium.PcbManufacturing.Tests;

/// <summary>
/// A live WPF TabControl bound to <see cref="OrderWizardViewModel"/> the way MainWindow binds it: tab navigation obeys
/// the same coordinator rules as the footer, the selection snaps back when a request is refused, and each tab is
/// enabled only while its step is available.
/// </summary>
/// <remarks>Not shown on screen: covers binding and selection logic, not mouse, keyboard or rendering.</remarks>
public sealed class WorkflowTabControlTests
{
    [Fact]
    public void SelectingALockedTab_SnapsBackToTheCurrentStep() => Sta.Run(() =>
    {
        (TabControl tabs, TestWizard wizard) = CreateBoundTabs();

        tabs.SelectedIndex = 1;

        Assert.Equal(WorkflowStep.Preferences, wizard.Wizard.CurrentStep);
        Assert.Equal(0, tabs.SelectedIndex);
    });

    [Fact]
    public void CompletedOrder_KeepsTheSelectionOnOrderUntilNewOrder() => Sta.Run(() =>
    {
        (TabControl tabs, TestWizard wizard) = CreateBoundTabs();
        wizard.CompleteOrderAsync().GetAwaiter().GetResult();

        tabs.SelectedIndex = 0;
        Assert.Equal(2, tabs.SelectedIndex);
        tabs.SelectedIndex = 1;
        Assert.Equal(2, tabs.SelectedIndex);
        Assert.False(((TabItem)tabs.Items[0]).IsEnabled);
        Assert.False(((TabItem)tabs.Items[1]).IsEnabled);

        wizard.Wizard.StartNewOrder();

        Assert.Equal(0, tabs.SelectedIndex);
        Assert.True(((TabItem)tabs.Items[0]).IsEnabled);
        Assert.False(((TabItem)tabs.Items[1]).IsEnabled);
        Assert.False(((TabItem)tabs.Items[2]).IsEnabled);
        tabs.SelectedIndex = 2;
        Assert.Equal(0, tabs.SelectedIndex);
    });

    private static (TabControl Tabs, TestWizard Wizard) CreateBoundTabs()
    {
        TestWizard wizard = new();
        OrderWizardViewModel source = wizard.Wizard;
        TabControl tabs = new() { SelectedValuePath = nameof(TabItem.Tag) };
        tabs.Items.Add(new TabItem { Tag = WorkflowStep.Preferences });
        tabs.Items.Add(new TabItem { Tag = WorkflowStep.Quote });
        tabs.Items.Add(new TabItem { Tag = WorkflowStep.Order });
        tabs.SetBinding(Selector.SelectedValueProperty, new Binding(nameof(OrderWizardViewModel.CurrentStep))
        {
            Source = source,
            Mode = BindingMode.TwoWay,
        });
        // As in MainWindow.xaml: a tab is enabled only while its step is available.
        ((TabItem)tabs.Items[0]).SetBinding(UIElement.IsEnabledProperty, new Binding(nameof(OrderWizardViewModel.IsPreferencesAvailable)) { Source = source });
        ((TabItem)tabs.Items[1]).SetBinding(UIElement.IsEnabledProperty, new Binding(nameof(OrderWizardViewModel.IsQuoteAvailable)) { Source = source });
        ((TabItem)tabs.Items[2]).SetBinding(UIElement.IsEnabledProperty, new Binding(nameof(OrderWizardViewModel.IsOrderAvailable)) { Source = source });
        SelectionFollowsSourceBehavior.SetIsEnabled(tabs, true);
        return (tabs, wizard);
    }
}
