using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Altium.PcbManufacturing.Views.Behaviors;

/// <summary>
/// Lets a <see cref="TextBox"/> receive only ASCII digits (<c>0</c>–<c>9</c>): <c>local:DigitsOnlyInputBehavior.IsEnabled="True"</c>.
/// </summary>
public static class DigitsOnlyInputBehavior
{
    public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
        "IsEnabled",
        typeof(bool),
        typeof(DigitsOnlyInputBehavior),
        new PropertyMetadata(false, OnIsEnabledChanged));

    public static bool GetIsEnabled(DependencyObject element) => (bool)element.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(DependencyObject element, bool value) => element.SetValue(IsEnabledProperty, value);

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBox textBox)
        {
            return;
        }

        // Handlers are attached to the TextBox itself, so they never outlive it.
        if ((bool)e.NewValue)
        {
            textBox.PreviewTextInput += OnPreviewTextInput;
            textBox.PreviewKeyDown += OnPreviewKeyDown;
            DataObject.AddPastingHandler(textBox, OnPasting);
            InputMethod.SetIsInputMethodEnabled(textBox, false);
        }
        else
        {
            textBox.PreviewTextInput -= OnPreviewTextInput;
            textBox.PreviewKeyDown -= OnPreviewKeyDown;
            DataObject.RemovePastingHandler(textBox, OnPasting);
            textBox.ClearValue(InputMethod.IsInputMethodEnabledProperty);
        }
    }

    private static void OnPreviewTextInput(object sender, TextCompositionEventArgs e) => e.Handled = !IsDigits(e.Text);

    private static void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Space)
        {
            e.Handled = true;
        }
    }

    private static void OnPasting(object sender, DataObjectPastingEventArgs e)
    {
        if (e.DataObject.GetData(DataFormats.UnicodeText, true) is not string pasted || !IsDigits(pasted))
        {
            e.CancelCommand();
        }
    }

    private static bool IsDigits(string? text) => !string.IsNullOrEmpty(text) && text.All(char.IsAsciiDigit);
}
