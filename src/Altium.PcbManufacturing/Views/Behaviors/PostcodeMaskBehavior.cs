using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Altium.PcbManufacturing.Views.Behaviors;

/// <summary>
/// Applies the <see cref="PostcodeMask"/> rules to a <see cref="TextBox"/>: <c>local:PostcodeMaskBehavior.IsEnabled="True"</c>.
/// </summary>
/// <remarks>
/// Every way user input can change the text is routed through the mask: typed and IME text (PreviewTextInput, with the
/// IME disabled since only digits are valid), Space (which bypasses PreviewTextInput), Backspace/Delete and their Ctrl
/// variants, paste, and cut. Drag-and-drop and undo are disabled, as both would insert text the mask has not checked.
/// The result is written with SetCurrentValue so the TwoWay binding is kept and updates its source. View-only input
/// handling: the business rule itself is validated by the view model.
/// </remarks>
public static class PostcodeMaskBehavior
{
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
        var textBox = (TextBox)sender;
        if (PostcodeMask.TryInsert(textBox.Text, textBox.SelectionStart, textBox.SelectionLength, e.Text, out PostcodeEdit edit))
        {
            Apply(textBox, edit);
        }

        e.Handled = true;
    }

    private static void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        var textBox = (TextBox)sender;
        bool control = (Keyboard.Modifiers & ModifierKeys.Control) != 0;
        PostcodeDeletion deletion;
        switch (e.Key)
        {
            case Key.Space:
                e.Handled = true;
                return;
            case Key.Back when (Keyboard.Modifiers & ModifierKeys.Alt) == 0:
                deletion = control ? PostcodeDeletion.ToStart : PostcodeDeletion.Backward;
                break;
            // Shift+Delete is Cut; it is handled as a command in OnPreviewExecuted.
            case Key.Delete when (Keyboard.Modifiers & ModifierKeys.Shift) == 0:
                deletion = control ? PostcodeDeletion.ToEnd : PostcodeDeletion.Forward;
                break;
            default:
                return;
        }

        Apply(textBox, PostcodeMask.Delete(textBox.Text, textBox.SelectionStart, textBox.SelectionLength, deletion));
        e.Handled = true;
    }

    private static void OnPasting(object sender, DataObjectPastingEventArgs e)
    {
        var textBox = (TextBox)sender;
        e.CancelCommand();
        if (e.SourceDataObject.GetData(DataFormats.UnicodeText, true) is string pasted
            && PostcodeMask.TryInsert(textBox.Text, textBox.SelectionStart, textBox.SelectionLength, pasted, out PostcodeEdit edit))
        {
            Apply(textBox, edit);
        }
    }

    private static void OnPreviewExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        if (e.Command != ApplicationCommands.Cut)
        {
            return;
        }

        var textBox = (TextBox)sender;
        e.Handled = true;
        if (textBox.SelectionLength == 0)
        {
            return;
        }

        try
        {
            Clipboard.SetText(textBox.SelectedText);
        }
        catch (ExternalException)
        {
            // The clipboard is held by another process; keep the text rather than lose the cut selection.
            return;
        }

        Apply(textBox, PostcodeMask.Delete(textBox.Text, textBox.SelectionStart, textBox.SelectionLength, PostcodeDeletion.Backward));
    }

    private static void Apply(TextBox textBox, PostcodeEdit edit)
    {
        if (textBox.Text != edit.Text)
        {
            textBox.SetCurrentValue(TextBox.TextProperty, edit.Text);
        }

        textBox.Select(edit.CaretIndex, 0);
    }
}
