using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;

namespace Altium.PcbManufacturing.Views.Behaviors;

/// <summary>
/// Keeps a selector's selection equal to its two-way bound <see cref="Selector.SelectedValue"/> source when the source
/// refuses a change: <c>local:SelectionFollowsSourceBehavior.IsEnabled="True"</c>.
/// </summary>

public static class SelectionFollowsSourceBehavior
{
    public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
        "IsEnabled",
        typeof(bool),
        typeof(SelectionFollowsSourceBehavior),
        new PropertyMetadata(false, OnIsEnabledChanged));

    public static bool GetIsEnabled(DependencyObject element) => (bool)element.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(DependencyObject element, bool value) => element.SetValue(IsEnabledProperty, value);

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Selector selector)
        {
            return;
        }

        // Attached to the selector itself, so the handler never outlives it.
        if ((bool)e.NewValue)
        {
            selector.SelectionChanged += OnSelectionChanged;
        }
        else
        {
            selector.SelectionChanged -= OnSelectionChanged;
        }
    }

    private static void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // SelectionChanged bubbles: ignore ComboBoxes and other selectors inside the tab content.
        if (!ReferenceEquals(e.OriginalSource, sender))
        {
            return;
        }

        BindingOperations.GetBindingExpression((Selector)sender, Selector.SelectedValueProperty)?.UpdateTarget();
    }
}
