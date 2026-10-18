using Altium.PcbManufacturing.Views.Behaviors;

namespace Altium.PcbManufacturing.Tests;

public sealed class PostcodeMaskTests
{
    // Format

    [Theory]
    [InlineData("", "")]
    [InlineData("5", "5")]
    [InlineData("55", "55-")]
    [InlineData("552", "55-2")]
    [InlineData("55220", "55-220")]
    public void Format_InsertsHyphenAfterTwoDigits(string digits, string expected) =>
        Assert.Equal(expected, PostcodeMask.Format(digits));

    // Typing

    [Fact]
    public void TryInsert_TypingFiveDigits_ProducesFormattedPostcode()
    {
        string text = string.Empty;
        int caret = 0;
        foreach (char digit in "55220")
        {
            Assert.True(PostcodeMask.TryInsert(text, caret, 0, digit.ToString(), out PostcodeEdit edit));
            (text, caret) = (edit.Text, edit.CaretIndex);
        }

        Assert.Equal("55-220", text);
        Assert.Equal(6, caret);
    }

    [Fact]
    public void TryInsert_SecondDigit_AddsHyphenAndPlacesCaretAfterIt() =>
        AssertInsert("5", 1, 0, "5", "55-", 3);

    [Fact]
    public void TryInsert_SixthDigit_IsRejected() => AssertRejected("55-220", 6, 0, "1");

    [Theory]
    [InlineData("A")]
    [InlineData("z")]
    [InlineData("/")]
    [InlineData("٥")]
    public void TryInsert_NonDigitCharacter_IsRejected(string input) => AssertRejected("55-2", 4, 0, input);

    [Fact]
    // With two or more digits before it, the caret is placed after the hyphen.
    public void TryInsert_DigitInTheMiddle_ShiftsFollowingDigits() =>
        AssertInsert("55-22", 1, 0, "9", "59-522", 3);

    [Fact]
    public void TryInsert_DigitBeforeHyphen_PlacesCaretAfterHyphen() =>
        AssertInsert("55-22", 2, 0, "9", "55-922", 4);

    [Fact]
    public void TryInsert_DigitReplacingSelection_ReplacesSelectedDigits() =>
        AssertInsert("55-220", 3, 2, "9", "55-90", 4);

    [Fact]
    public void TryInsert_DigitReplacingSelectionAcrossHyphen_ReplacesSelectedDigits() =>
        AssertInsert("55-220", 1, 3, "7", "57-20", 3);

    [Fact]
    public void TryInsert_DigitIntoFullPostcodeWithSelection_IsAccepted() =>
        AssertInsert("55-220", 0, 6, "1", "1", 1);

    [Fact]
    public void TryInsert_HyphenAtHyphenPosition_StepsOverIt() =>
        AssertInsert("55-22", 2, 0, "-", "55-22", 3);

    [Theory]
    [InlineData("5", 1)]
    [InlineData("55-22", 4)]
    public void TryInsert_HyphenElsewhere_IsRejected(string text, int caret) => AssertRejected(text, caret, 0, "-");

    // Paste

    [Theory]
    [InlineData("55220")]
    [InlineData("55-220")]
    [InlineData(" 55-220\r\n")]
    public void TryInsert_PastingCompletePostcode_IntoEmptyField_ProducesFormattedPostcode(string pasted) =>
        AssertInsert(string.Empty, 0, 0, pasted, "55-220", 6);

    [Fact]
    public void TryInsert_PastingPostcodeOverWholeSelection_ReplacesIt() =>
        AssertInsert("12-345", 0, 6, "55-220", "55-220", 6);

    [Fact]
    public void TryInsert_PastingPartialFormattedValue_IsAccepted() =>
        AssertInsert(string.Empty, 0, 0, "55-2", "55-2", 4);

    [Theory]
    [InlineData("55A220")]
    [InlineData("ABC")]
    [InlineData("55-220ABC")]
    [InlineData("55 220")]
    [InlineData("5-5220")]
    [InlineData("55--220")]
    [InlineData("552-20")]
    [InlineData("")]
    [InlineData("   ")]
    public void TryInsert_PastingInvalidText_IsRejectedWithoutFiltering(string pasted) =>
        AssertRejected(string.Empty, 0, 0, pasted);

    [Theory]
    [InlineData("552201")]
    [InlineData("55-2201")]
    public void TryInsert_PastingMoreThanFiveDigits_IsRejected(string pasted) => AssertRejected(string.Empty, 0, 0, pasted);

    [Fact]
    public void TryInsert_PastingDigitsThatWouldOverflowExistingText_IsRejected() => AssertRejected("55-2", 4, 0, "200");

    // Deletion

    [Fact]
    public void Delete_BackspaceAtEnd_RemovesLastDigit() =>
        AssertDelete("55-220", 6, 0, PostcodeDeletion.Backward, "55-22", 5);

    [Fact]
    public void Delete_BackspaceAfterHyphen_RemovesDigitBeforeHyphen() =>
        AssertDelete("55-220", 3, 0, PostcodeDeletion.Backward, "52-20", 1);

    [Fact]
    public void Delete_BackspaceOnTrailingHyphen_RemovesSecondDigit() =>
        AssertDelete("55-", 3, 0, PostcodeDeletion.Backward, "5", 1);

    [Fact]
    public void Delete_BackspaceInTheMiddle_ShiftsFollowingDigits() =>
        AssertDelete("55-220", 5, 0, PostcodeDeletion.Backward, "55-20", 4);

    [Fact]
    public void Delete_BackspaceAtStart_ChangesNothing() =>
        AssertDelete("55-220", 0, 0, PostcodeDeletion.Backward, "55-220", 0);

    [Fact]
    public void Delete_ForwardBeforeHyphen_RemovesDigitAfterHyphen() =>
        AssertDelete("55-220", 2, 0, PostcodeDeletion.Forward, "55-20", 3);

    [Fact]
    public void Delete_ForwardAtStart_RemovesFirstDigit() =>
        AssertDelete("55-220", 0, 0, PostcodeDeletion.Forward, "52-20", 0);

    [Fact]
    public void Delete_ForwardAtEnd_ChangesNothing() =>
        AssertDelete("55-220", 6, 0, PostcodeDeletion.Forward, "55-220", 6);

    [Theory]
    [InlineData(PostcodeDeletion.Backward)]
    [InlineData(PostcodeDeletion.Forward)]
    public void Delete_WithSelection_RemovesSelectedDigits(PostcodeDeletion deletion) =>
        AssertDelete("55-220", 1, 4, deletion, "50-", 1);

    [Fact]
    public void Delete_ToStart_RemovesDigitsBeforeCaret() =>
        AssertDelete("55-220", 4, 0, PostcodeDeletion.ToStart, "20-", 0);

    [Fact]
    public void Delete_ToEnd_RemovesDigitsAfterCaret() =>
        AssertDelete("55-220", 4, 0, PostcodeDeletion.ToEnd, "55-2", 4);

    // Robustness

    [Fact]
    public void TryInsert_OutOfRangeSelection_IsClamped() =>
        AssertInsert("55", 10, 10, "2", "55-2", 4);

    [Fact]
    public void TryInsert_IntoValueAssignedInCode_KeepsOnlyItsDigits() =>
        AssertInsert("x1y", 3, 0, "2", "12-", 3);

    private static void AssertInsert(string text, int start, int length, string input, string expectedText, int expectedCaret)
    {
        Assert.True(PostcodeMask.TryInsert(text, start, length, input, out PostcodeEdit edit));
        Assert.Equal(new PostcodeEdit(expectedText, expectedCaret), edit);
    }

    private static void AssertRejected(string text, int start, int length, string input) =>
        Assert.False(PostcodeMask.TryInsert(text, start, length, input, out _));

    private static void AssertDelete(
        string text, int start, int length, PostcodeDeletion deletion, string expectedText, int expectedCaret) =>
        Assert.Equal(new PostcodeEdit(expectedText, expectedCaret), PostcodeMask.Delete(text, start, length, deletion));
}
