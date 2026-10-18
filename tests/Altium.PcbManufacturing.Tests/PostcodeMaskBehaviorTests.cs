using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using Altium.PcbManufacturing.Views.Behaviors;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Altium.PcbManufacturing.Tests;

/// <summary>
/// Raises real WPF routed input events on a live TextBox to verify the behavior's wiring: text, caret and binding.
/// </summary>
/// <remarks>
/// The TextBox is not shown, so this does not cover rendering, focus or OS keyboard state (Keyboard.Modifiers is read
/// from the real keyboard and is assumed to be idle). The editing rules themselves are covered by PostcodeMaskTests.
/// </remarks>
public sealed partial class PostcodeMaskBehaviorTests
{
    [Fact]
    public void TypingDigits_FormatsPostcodeAndMovesCaret() => OnStaThread(() =>
    {
        TextBox textBox = CreateTextBox();

        foreach (char digit in "55220")
        {
            Type(textBox, digit.ToString());
        }

        Assert.Equal("55-220", textBox.Text);
        Assert.Equal(6, textBox.CaretIndex);
    });

    [Theory]
    [InlineData("A")]
    [InlineData("1")]
    public void TypingIntoCompletePostcode_IsRejected(string input) => OnStaThread(() =>
    {
        TextBox textBox = CreateTextBox("55-220", caret: 6);

        bool handled = Type(textBox, input);

        Assert.True(handled);
        Assert.Equal("55-220", textBox.Text);
    });

    [Fact]
    public void TypingInTheMiddle_InsertsDigitAndKeepsCaretAfterIt() => OnStaThread(() =>
    {
        TextBox textBox = CreateTextBox("55-22", caret: 1);

        Type(textBox, "9");

        Assert.Equal("59-522", textBox.Text);
        Assert.Equal(3, textBox.CaretIndex);
    });

    [Fact]
    public void TypingOverSelection_ReplacesSelectedDigits() => OnStaThread(() =>
    {
        TextBox textBox = CreateTextBox("55-220");
        textBox.Select(3, 2);

        Type(textBox, "9");

        Assert.Equal("55-90", textBox.Text);
        Assert.Equal(4, textBox.CaretIndex);
    });

    [Theory]
    [InlineData("55220")]
    [InlineData("55-220")]
    public void PastingValidPostcode_FormatsIt(string pasted) => OnStaThread(() =>
    {
        TextBox textBox = CreateTextBox();

        bool cancelled = Paste(textBox, pasted);

        Assert.True(cancelled);
        Assert.Equal("55-220", textBox.Text);
        Assert.Equal(6, textBox.CaretIndex);
    });

    [Theory]
    [InlineData("55A220")]
    [InlineData("ABC")]
    [InlineData("552201")]
    [InlineData("55-220ABC")]
    public void PastingInvalidText_LeavesFieldUnchanged(string pasted) => OnStaThread(() =>
    {
        TextBox textBox = CreateTextBox("12", caret: 2);

        bool cancelled = Paste(textBox, pasted);

        Assert.True(cancelled);
        Assert.Equal("12", textBox.Text);
    });

    [Fact]
    public void Backspace_AfterHyphen_RemovesDigitBeforeHyphen() => OnStaThread(() =>
    {
        TextBox textBox = CreateTextBox("55-220", caret: 3);

        bool handled = PressKey(textBox, Key.Back);

        Assert.True(handled);
        Assert.Equal("52-20", textBox.Text);
        Assert.Equal(1, textBox.CaretIndex);
    });

    [Fact]
    public void Delete_BeforeHyphen_RemovesDigitAfterHyphen() => OnStaThread(() =>
    {
        TextBox textBox = CreateTextBox("55-220", caret: 2);

        PressKey(textBox, Key.Delete);

        Assert.Equal("55-20", textBox.Text);
        Assert.Equal(3, textBox.CaretIndex);
    });

    [Fact]
    public void Space_IsRejected() => OnStaThread(() =>
    {
        TextBox textBox = CreateTextBox("55", caret: 2);

        bool handled = PressKey(textBox, Key.Space);

        Assert.True(handled);
        Assert.Equal("55", textBox.Text);
    });

    [Theory]
    [InlineData(Key.Tab)]
    [InlineData(Key.Left)]
    [InlineData(Key.Home)]
    public void NavigationKeys_AreNotHandled(Key key) => OnStaThread(() =>
    {
        TextBox textBox = CreateTextBox("55-220", caret: 3);

        Assert.False(PressKey(textBox, key));
    });

    [Fact]
    public void Edits_UpdateTwoWayBoundSourceAndKeepBinding() => OnStaThread(() =>
    {
        TextBox textBox = CreateTextBox();
        PostcodeSource source = new();
        textBox.SetBinding(TextBox.TextProperty, new Binding(nameof(PostcodeSource.Postcode))
        {
            Source = source,
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
        });

        Paste(textBox, "55220");
        Assert.Equal("55-220", source.Postcode);

        PressKey(textBox, Key.Back);
        Assert.Equal("55-22", source.Postcode);

        // The binding survives the behavior's writes: a source change still reaches the TextBox, unformatted.
        source.Postcode = "invalid";
        Assert.Equal("invalid", textBox.Text);
        Assert.NotNull(BindingOperations.GetBindingExpression(textBox, TextBox.TextProperty));
    });

    [Fact]
    public void Enabling_DisablesImeDropAndUndo() => OnStaThread(() =>
    {
        TextBox textBox = CreateTextBox();

        Assert.False(InputMethod.GetIsInputMethodEnabled(textBox));
        Assert.False(textBox.AllowDrop);
        Assert.False(textBox.IsUndoEnabled);
    });

    [Fact]
    public void Disabling_DetachesMaskAndRestoresDefaults() => OnStaThread(() =>
    {
        TextBox textBox = CreateTextBox("55", caret: 2);

        PostcodeMaskBehavior.SetIsEnabled(textBox, false);

        Assert.False(Type(textBox, "A"));
        TextBox defaults = new();
        Assert.Equal(defaults.AllowDrop, textBox.AllowDrop);
        Assert.Equal(defaults.IsUndoEnabled, textBox.IsUndoEnabled);
        Assert.Equal(InputMethod.GetIsInputMethodEnabled(defaults), InputMethod.GetIsInputMethodEnabled(textBox));
    });

    private static TextBox CreateTextBox(string text = "", int? caret = null)
    {
        TextBox textBox = new() { Text = text };
        PostcodeMaskBehavior.SetIsEnabled(textBox, true);
        textBox.Select(caret ?? text.Length, 0);
        return textBox;
    }

    private static bool Type(TextBox textBox, string text)
    {
        TextCompositionEventArgs args = new(Keyboard.PrimaryDevice, new TextComposition(InputManager.Current, textBox, text))
        {
            RoutedEvent = UIElement.PreviewTextInputEvent,
        };
        textBox.RaiseEvent(args);
        return args.Handled;
    }

    private static bool Paste(TextBox textBox, string text)
    {
        DataObject data = new(DataFormats.UnicodeText, text);
        DataObjectPastingEventArgs args = new(data, false, DataFormats.UnicodeText);
        textBox.RaiseEvent(args);
        return args.CommandCancelled;
    }

    private static bool PressKey(TextBox textBox, Key key)
    {
        using HwndSource source = new(new HwndSourceParameters("PostcodeMaskBehaviorTests"));
        KeyEventArgs args = new(Keyboard.PrimaryDevice, source, 0, key) { RoutedEvent = Keyboard.PreviewKeyDownEvent };
        textBox.RaiseEvent(args);
        return args.Handled;
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

    private sealed partial class PostcodeSource : ObservableObject
    {
        [ObservableProperty]
        private string postcode = string.Empty;
    }
}
