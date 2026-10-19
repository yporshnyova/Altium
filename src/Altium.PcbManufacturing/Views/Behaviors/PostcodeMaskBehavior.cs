using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Altium.PcbManufacturing.Views.Behaviors;

/// <summary>
/// Masks a <see cref="TextBox"/> as a Polish <c>NN-NNN</c> postcode: <c>local:PostcodeMaskBehavior.IsEnabled="True"</c>.
/// </summary>
/// <remarks>
/// Every edit is applied to the digits only and the result is re-formatted, so the hyphen appears after two digits and is
/// never edited directly. Typed or pasted text must be digits, optionally with the hyphen in its place (<c>55-220</c>);
/// anything else is rejected as a whole. Drag-and-drop and undo are disabled, as they would bypass the mask. The
/// business rule itself is validated by the view model.
/// </remarks>
public static class PostcodeMaskBehavior
{
    private const int maxDigits = 5;
    private const int hyphenIndex = 2;

    public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
        "IsEnabled",
        typeof(bool),
        typeof(PostcodeMaskBehavior),
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
            CommandManager.AddPreviewExecutedHandler(textBox, OnPreviewExecuted);
            InputMethod.SetIsInputMethodEnabled(textBox, false);
            textBox.AllowDrop = false;
            textBox.IsUndoEnabled = false;
        }
        else
        {
            textBox.PreviewTextInput -= OnPreviewTextInput;
            textBox.PreviewKeyDown -= OnPreviewKeyDown;
            DataObject.RemovePastingHandler(textBox, OnPasting);
            CommandManager.RemovePreviewExecutedHandler(textBox, OnPreviewExecuted);
            textBox.ClearValue(InputMethod.IsInputMethodEnabledProperty);
            textBox.ClearValue(UIElement.AllowDropProperty);
            textBox.ClearValue(TextBoxBase.IsUndoEnabledProperty);
        }
    }

    private static void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        Insert((TextBox)sender, e.Text);
        e.Handled = true;
    }

    private static void OnPasting(object sender, DataObjectPastingEventArgs e)
    {
        e.CancelCommand();
        if (e.DataObject.GetData(DataFormats.UnicodeText, true) is string pasted)
        {
            Insert((TextBox)sender, pasted.Trim());
        }
    }

    private static void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        // Shift+Delete is Cut, handled in OnPreviewExecuted.
        if (e.Key == Key.Delete && (Keyboard.Modifiers & ModifierKeys.Shift) != 0)
        {
            return;
        }

        var textBox = (TextBox)sender;
        string text = textBox.Text;
        int start = textBox.SelectionStart;
        switch (e.Key)
        {
            // Space bypasses PreviewTextInput.
            case Key.Space:
                break;
            case Key.Back or Key.Delete when textBox.SelectionLength > 0:
                Replace(textBox, start, textBox.SelectionLength, string.Empty);
                break;

            // With no selection, Backspace and Delete step over the hyphen and remove the digit beyond it.
            case Key.Back when start > 0:
                int from = Math.Max(0, start - (text[start - 1] == '-' ? 2 : 1));
                Replace(textBox, from, start - from, string.Empty);
                break;
            case Key.Delete when start < text.Length:
                int to = Math.Min(text.Length, start + (text[start] == '-' ? 2 : 1));
                Replace(textBox, start, to - start, string.Empty);
                break;
            default:
                return;
        }

        e.Handled = true;
    }

    private static void OnPreviewExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        if (e.Command == ApplicationCommands.Cut)
        {
            var textBox = (TextBox)sender;
            ApplicationCommands.Copy.Execute(null, textBox);
            Replace(textBox, textBox.SelectionStart, textBox.SelectionLength, string.Empty);
            e.Handled = true;
        }
    }

    private static void Insert(TextBox textBox, string input)
    {
        if (input.Length > hyphenIndex && input[hyphenIndex] == '-')
        {
            input = input.Remove(hyphenIndex, 1);
        }

        if (input.Length > 0 && input.All(char.IsAsciiDigit))
        {
            Replace(textBox, textBox.SelectionStart, textBox.SelectionLength, input);
        }
    }

    private static void Replace(TextBox textBox, int start, int length, string digits)
    {
        string text = textBox.Text;
        string beforeCaret = DigitsOf(text[..start]) + digits;
        string all = beforeCaret + DigitsOf(text[(start + length)..]);
        if (all.Length > maxDigits)
        {
            return;
        }

        // SetCurrentValue keeps the TwoWay binding, which then updates its source.
        textBox.SetCurrentValue(TextBox.TextProperty, all.Length < hyphenIndex ? all : all.Insert(hyphenIndex, "-"));
        textBox.Select(beforeCaret.Length < hyphenIndex ? beforeCaret.Length : beforeCaret.Length + 1, 0);
    }

    private static string DigitsOf(string text) => string.Concat(text.Where(char.IsAsciiDigit));
}
