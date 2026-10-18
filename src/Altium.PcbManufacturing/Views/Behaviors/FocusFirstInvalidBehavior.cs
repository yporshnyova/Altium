using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace Altium.PcbManufacturing.Views.Behaviors;

/// <summary>
/// Moves keyboard focus to the first input with a validation error inside the element, each time
/// <c>Request</c> changes: <c>local:FocusFirstInvalidBehavior.Request="{Binding InvalidSubmitCount}"</c>.
/// </summary>
/// <remarks>
/// Lets a view model ask for focus without referencing controls: it only increments a counter. "First" follows the
/// visual tree depth-first, which matches the form's declaration (reading) order.
/// </remarks>
public static class FocusFirstInvalidBehavior
{
    public static readonly DependencyProperty RequestProperty = DependencyProperty.RegisterAttached(
        "Request",
        typeof(int),
        typeof(FocusFirstInvalidBehavior),
        new PropertyMetadata(0, OnRequestChanged));

    public static int GetRequest(DependencyObject element) => (int)element.GetValue(RequestProperty);

    public static void SetRequest(DependencyObject element, int value) => element.SetValue(RequestProperty, value);

    private static void OnRequestChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        // The initial value arrives while a (re)created view is loading; only later changes are focus requests.
        if (d is not FrameworkElement { IsLoaded: true } root)
        {
            return;
        }

        // Defer until bindings have applied the new validation errors.
        root.Dispatcher.BeginInvoke(DispatcherPriority.Input, () =>
        {
            if (FindFirstInvalid(root) is { } target)
            {
                target.Focus();
                target.BringIntoView();
            }
        });
    }

    private static FrameworkElement? FindFirstInvalid(DependencyObject parent)
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(parent, i);
            if (child is FrameworkElement { Focusable: true, IsVisible: true } element && Validation.GetHasError(element))
            {
                return element;
            }

            if (FindFirstInvalid(child) is { } nested)
            {
                return nested;
            }
        }

        return null;
    }
}
