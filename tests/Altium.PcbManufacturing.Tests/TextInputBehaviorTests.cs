using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using Altium.PcbManufacturing.Views.Behaviors;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Altium.PcbManufacturing.Tests;

/// <summary>
/// The view-only input filters: the postcode mask's editing rules, then both TextBox behaviors driven by real routed
/// input events on an STA thread. The TextBoxes are not shown, so focus, real keyboard modifiers (Ctrl shortcuts) and
/// rendering are left to manual verification. The view models validate whatever text gets through.
/// </summary>
public sealed partial class TextInputBehaviorTests
{
    // Postcode mask rules

    [Fact]
    public void PostcodeMask_TypingFiveDigits_InsertsTheHyphen()
    {
        string text = string.Empty;
        int caret = 0;
        foreach (char digit in "55220")
        {
            Assert.True(PostcodeMask.TryInsert(text, caret, 0, digit.ToString(), out PostcodeEdit edit));
            (text, caret) = (edit.Text, edit.CaretIndex);
        }

        Assert.Equal(new PostcodeEdit("55-220", 6), new PostcodeEdit(text, caret));
    }

    [Theory]
    [InlineData("55-220", 6, 0, "1")] // A sixth digit.
    [InlineData("55-2", 4, 0, "A")]
    [InlineData("", 0, 0, "55A220")] // Pasted text is rejected as a whole, not filtered.
    [InlineData("", 0, 0, "552201")]
    public void PostcodeMask_RejectsInputThatCannotFormAPostcode(string text, int start, int length, string input) =>
        Assert.False(PostcodeMask.TryInsert(text, start, length, input, out _));

    [Theory]
    [InlineData("55-220", 3, 2, "9", "55-90", 4)] // Typing over a selection.
    [InlineData("", 0, 0, " 55-220\r\n", "55-220", 6)] // Pasting a copied postcode.
    public void PostcodeMask_AcceptsValidInput(string text, int start, int length, string input, string expected, int caret)
    {
        Assert.True(PostcodeMask.TryInsert(text, start, length, input, out PostcodeEdit edit));
        Assert.Equal(new PostcodeEdit(expected, caret), edit);
    }

    [Fact]
    public void PostcodeMask_BackspaceAfterTheHyphen_RemovesTheDigitBeforeIt() =>
        Assert.Equal(new PostcodeEdit("52-20", 1), PostcodeMask.Delete("55-220", 3, 0, PostcodeDeletion.Backward));

    // Postcode TextBox

    [Fact]
    public void PostcodeBox_EditsAreFormattedAndReachTheTwoWayBoundSource() => Sta.Run(() =>
    {
        TextBox textBox = CreateTextBox(PostcodeMaskBehavior.SetIsEnabled);
        PostcodeSource source = new();
        textBox.SetBinding(TextBox.TextProperty, new Binding(nameof(PostcodeSource.Postcode))
        {
            Source = source,
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
        });

        foreach (char digit in "5522")
        {
            Type(textBox, digit.ToString());
        }

        Assert.Equal("55-22", source.Postcode);

        Assert.True(Paste(textBox, "A"));
        PressKey(textBox, Key.Back);

        Assert.Equal("55-2", source.Postcode);
        Assert.Equal(4, textBox.CaretIndex);

        // The binding survives the behavior's writes.
        source.Postcode = "invalid";
        Assert.Equal("invalid", textBox.Text);
    });

    [Fact]
    public void PostcodeBox_LeavesNavigationKeysToTheTextBox() => Sta.Run(() =>
    {
        Assert.False(PressKey(CreateTextBox(PostcodeMaskBehavior.SetIsEnabled, "55-220"), Key.Left));
    });

    // Quantity TextBox (digits only)

    [Theory]
    [InlineData("7", false)]
    [InlineData("a", true)]
    [InlineData("-", true)]
    [InlineData(",", true)]
    public void DigitsOnlyBox_BlocksEveryTypedNonDigit(string text, bool blocked) => Sta.Run(() =>
    {
        Assert.Equal(blocked, Type(CreateTextBox(DigitsOnlyInputBehavior.SetIsEnabled, "12"), text));
    });

    [Theory]
    [InlineData("250", false)]
    [InlineData("4a2", true)]
    [InlineData(" 12", true)]
    public void DigitsOnlyBox_RejectsPastedTextWithAnyNonDigitWithoutStrippingIt(string text, bool cancelled) => Sta.Run(() =>
    {
        Assert.Equal(cancelled, Paste(CreateTextBox(DigitsOnlyInputBehavior.SetIsEnabled, "12"), text));
    });

    [Fact]
    public void DigitsOnlyBox_BlocksSpaceButLeavesEditingKeysToTheTextBox() => Sta.Run(() =>
    {
        TextBox textBox = CreateTextBox(DigitsOnlyInputBehavior.SetIsEnabled, "12");

        // Space bypasses PreviewTextInput, so it is blocked as a key.
        Assert.True(PressKey(textBox, Key.Space));
        Assert.False(PressKey(textBox, Key.Back));
        Assert.False(PressKey(textBox, Key.V));
    });

    private static TextBox CreateTextBox(Action<DependencyObject, bool> enable, string text = "")
    {
        TextBox textBox = new() { Text = text };
        enable(textBox, true);
        textBox.Select(text.Length, 0);
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
        DataObjectPastingEventArgs args = new(new DataObject(DataFormats.UnicodeText, text), false, DataFormats.UnicodeText);
        textBox.RaiseEvent(args);
        return args.CommandCancelled;
    }

    private static bool PressKey(TextBox textBox, Key key)
    {
        using HwndSource source = new(new HwndSourceParameters(nameof(TextInputBehaviorTests)));
        KeyEventArgs args = new(Keyboard.PrimaryDevice, source, 0, key) { RoutedEvent = Keyboard.PreviewKeyDownEvent };
        textBox.RaiseEvent(args);
        return args.Handled;
    }

    private sealed partial class PostcodeSource : ObservableObject
    {
        [ObservableProperty]
        private string postcode = string.Empty;
    }
}
