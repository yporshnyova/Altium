using System.Text.RegularExpressions;

namespace Altium.PcbManufacturing.Views.Behaviors;

/// <summary>
/// The text and caret position of a postcode field after an edit.
/// </summary>
public readonly record struct PostcodeEdit(string Text, int CaretIndex);

public enum PostcodeDeletion
{
    /// <summary>Backspace: the digit before the caret.</summary>
    Backward,

    /// <summary>Delete: the digit after the caret.</summary>
    Forward,

    /// <summary>Ctrl+Backspace: every digit before the caret.</summary>
    ToStart,

    /// <summary>Ctrl+Delete: every digit after the caret.</summary>
    ToEnd,
}

/// <summary>
/// UI-independent editing rules of the Polish <c>NN-NNN</c> postcode input mask.
/// </summary>
/// <remarks>
/// Edits are computed on the digits only and the result is re-formatted, so the hyphen is never edited directly: it
/// appears once two digits are entered and deleting next to it removes the neighbouring digit. Input that is not a
/// digit sequence or an <c>NN-NNN</c> fragment is rejected as a whole, never filtered into a different postcode.
/// Values that do not come from these edits (e.g. assigned in code) are left to view model validation.
/// </remarks>
public static partial class PostcodeMask
{
    public const int MaxDigits = 5;
    public const int MaxLength = MaxDigits + 1;

    private const int hyphenIndex = 2;

    /// <summary>
    /// Formats up to five digits as <c>NN-NNN</c>; the hyphen is added once there are two digits.
    /// </summary>
    public static string Format(string digits) =>
        digits.Length >= hyphenIndex ? string.Concat(digits.AsSpan(0, hyphenIndex), "-", digits.AsSpan(hyphenIndex)) : digits;

    /// <summary>
    /// Replaces the selection with typed or pasted <paramref name="input"/>.
    /// </summary>
    /// <returns><see langword="false"/> when the input is rejected; the field must then stay unchanged.</returns>
    public static bool TryInsert(string text, int selectionStart, int selectionLength, string input, out PostcodeEdit edit)
    {
        (selectionStart, selectionLength) = Clamp(text, selectionStart, selectionLength);

        // Surrounding whitespace is a common clipboard artifact; it carries no digits, so trimming changes nothing else.
        string candidate = input.Trim();
        if (candidate == "-")
        {
            // Typing the hyphen the mask already shows just steps over it.
            bool atHyphen = selectionLength == 0 && selectionStart == hyphenIndex && text.Length > hyphenIndex && text[hyphenIndex] == '-';
            edit = new PostcodeEdit(text, atHyphen ? hyphenIndex + 1 : selectionStart);
            return atHyphen;
        }

        string inserted;
        if (candidate.Length > 0 && candidate.All(char.IsAsciiDigit))
        {
            inserted = candidate;
        }
        else if (FormattedFragment().IsMatch(candidate))
        {
            inserted = candidate.Remove(hyphenIndex, 1);
        }
        else
        {
            edit = default;
            return false;
        }

        string before = DigitsOf(text.AsSpan(0, selectionStart));
        string after = DigitsOf(text.AsSpan(selectionStart + selectionLength));
        if (before.Length + inserted.Length + after.Length > MaxDigits)
        {
            edit = default;
            return false;
        }

        edit = Compose(before, inserted, after);
        return true;
    }

    /// <summary>
    /// Deletes the selection, or the digit(s) next to the caret when nothing is selected.
    /// </summary>
    /// <returns>The resulting edit; equal to the current text and caret when there is nothing to delete.</returns>
    public static PostcodeEdit Delete(string text, int selectionStart, int selectionLength, PostcodeDeletion deletion)
    {
        (selectionStart, selectionLength) = Clamp(text, selectionStart, selectionLength);

        string before = DigitsOf(text.AsSpan(0, selectionStart));
        string after = DigitsOf(text.AsSpan(selectionStart + selectionLength));
        if (selectionLength > 0)
        {
            return Compose(before, string.Empty, after);
        }

        (string newBefore, string newAfter) = deletion switch
        {
            PostcodeDeletion.Backward => (before.Length > 0 ? before[..^1] : before, after),
            PostcodeDeletion.Forward => (before, after.Length > 0 ? after[1..] : after),
            PostcodeDeletion.ToStart => (string.Empty, after),
            PostcodeDeletion.ToEnd => (before, string.Empty),
            _ => throw new ArgumentOutOfRangeException(nameof(deletion), deletion, null),
        };

        return newBefore == before && newAfter == after
            ? new PostcodeEdit(text, selectionStart)
            : Compose(newBefore, string.Empty, newAfter);
    }

    private static PostcodeEdit Compose(string before, string inserted, string after)
    {
        int caretDigits = before.Length + inserted.Length;

        // Once a hyphen is shown (two or more digits), positions from the third digit on shift right by one.
        int caretIndex = caretDigits >= hyphenIndex ? caretDigits + 1 : caretDigits;
        return new PostcodeEdit(Format(before + inserted + after), caretIndex);
    }

    private static string DigitsOf(ReadOnlySpan<char> text)
    {
        Span<char> digits = stackalloc char[text.Length];
        int count = 0;
        foreach (char c in text)
        {
            if (char.IsAsciiDigit(c))
            {
                digits[count++] = c;
            }
        }

        return new string(digits[..count]);
    }

    private static (int Start, int Length) Clamp(string text, int selectionStart, int selectionLength)
    {
        int start = Math.Clamp(selectionStart, 0, text.Length);
        return (start, Math.Clamp(selectionLength, 0, text.Length - start));
    }

    // A pasted, already formatted value or its beginning: "55-", "55-2" … "55-220".
    [GeneratedRegex("^[0-9]{2}-[0-9]{0,3}$")]
    private static partial Regex FormattedFragment();
}
