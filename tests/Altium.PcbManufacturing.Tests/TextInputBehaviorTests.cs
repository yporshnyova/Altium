using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using Altium.PcbManufacturing.Views.Behaviors;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Altium.PcbManufacturing.Tests;

/// <summary>
/// The view-only input filters: both TextBox behaviors driven by real routed input events on an STA thread. The
/// TextBoxes are not shown, so focus, real keyboard modifiers (Ctrl shortcuts), the clipboard (Cut) and rendering are
/// left to manual verification. The view models validate whatever text gets through.
/// </summary>
public sealed partial class TextInputBehaviorTests
{
    // Postcode TextBox

    [Theory]
    [InlineData("55-220", "1")] // A sixth digit.
    [InlineData("55-2", "A")]
    [InlineData("55-2", "-")]
    [InlineData("", "55A220")] // Rejected as a whole, not filtered.
    [InlineData("", "5-5220")]
    [InlineData("", "552201")]
    public void PostcodeBox_RejectsInputThatCannotFormAPostcode(string text, string input) => Sta.Run(() =>
    {
        TextBox typed = CreateTextBox(PostcodeMaskBehavior.SetIsEnabled, text);
        TextBox pasted = CreateTextBox(PostcodeMaskBehavior.SetIsEnabled, text);

        Type(typed, input);
        Paste(pasted, input);

        Assert.Equal(text, typed.Text);
        Assert.Equal(text, pasted.Text);
    });

    [Theory]
    [InlineData("", 0, 0, " 55-220\r\n", "55-220", 6)] // Pasting a copied postcode.
    [InlineData("", 0, 0, "55220", "55-220", 6)]
    [InlineData("55-220", 3, 2, "9", "55-90", 4)] // Replacing a selection.
    [InlineData("55-220", 0, 6, "12", "12-", 3)]
    public void PostcodeBox_PastesOverTheSelection(string text, int start, int length, string input, string expected, int caret) => Sta.Run(() =>
    {
        TextBox textBox = CreateTextBox(PostcodeMaskBehavior.SetIsEnabled, text);
        textBox.Select(start, length);

        Paste(textBox, input);

        Assert.Equal(expected, textBox.Text);
        Assert.Equal(caret, textBox.CaretIndex);
    });

    [Theory]
    [InlineData(Key.Back, 3, 0, "52-20", 1)] // Steps over the hyphen.
    [InlineData(Key.Back, 6, 0, "55-22", 5)]
    [InlineData(Key.Delete, 2, 0, "55-20", 3)] // Steps over the hyphen.
    [InlineData(Key.Delete, 1, 3, "52-0", 1)] // Deletes the selection.
    public void PostcodeBox_DeletesDigitsAndReformats(Key key, int start, int length, string expected, int caret) => Sta.Run(() =>
    {
        TextBox textBox = CreateTextBox(PostcodeMaskBehavior.SetIsEnabled, "55-220");
        textBox.Select(start, length);

        Assert.True(PressKey(textBox, key));
        Assert.Equal(expected, textBox.Text);
        Assert.Equal(caret, textBox.CaretIndex);
    });

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

        foreach (char digit in "55220")
        {
            Type(textBox, digit.ToString());
        }

        Assert.Equal("55-220", source.Postcode);
        Assert.Equal(6, textBox.CaretIndex);

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
