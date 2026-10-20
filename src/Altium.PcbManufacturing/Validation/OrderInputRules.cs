using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Altium.PcbManufacturing.Localization;

namespace Altium.PcbManufacturing.Validation;

/// <summary>
/// The rules for text entered into the order: the length limits and how quantity text is read.
/// Shared by the validation attributes, the input fields' limits and the conversion of valid input to draft values.
/// </summary>
public static class OrderInputRules
{
    public const int ProjectNameMaxLength = 50;
    public const int NotesMaxLength = 300;

    public static string FormatQuantity(int quantity) => quantity.ToString(CultureInfo.InvariantCulture);

    /// <summary>
    /// Reads a board quantity: a positive whole number written with ASCII digits only (leading zeros allowed) that fits
    /// in <see cref="int"/>.
    /// </summary>
    public static bool TryParseQuantity(string? text, out int quantity)
    {
        quantity = 0;
        return IsDigitsOnly(text)
            && int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out quantity)
            && quantity > 0;
    }

    /// <summary>
    /// <see cref="CustomValidationAttribute"/> method for quantity text; blank text is left to <see cref="RequiredAttribute"/>.
    /// </summary>
    public static ValidationResult? ValidateQuantity(string? text, ValidationContext context)
    {
        if (string.IsNullOrWhiteSpace(text) || TryParseQuantity(text, out _))
        {
            return ValidationResult.Success;
        }

        return new ValidationResult(
            IsQuantityTooLarge(text) ? Strings.Validation_QuantityTooLarge : Strings.Validation_QuantityWholeNumber,
            context.MemberName is null ? null : [context.MemberName]);
    }

    private static bool IsQuantityTooLarge(string text) =>
        IsDigitsOnly(text) && text.Any(c => c != '0') && !int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out _);

    private static bool IsDigitsOnly(string? text) => !string.IsNullOrEmpty(text) && text.All(char.IsAsciiDigit);
}
