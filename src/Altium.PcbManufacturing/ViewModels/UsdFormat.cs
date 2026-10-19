using System.Globalization;

namespace Altium.PcbManufacturing.ViewModels;

/// <summary>
/// Culture-independent US dollar display text, e.g. <c>$1,234.56</c>, regardless of the user's regional settings.
/// </summary>
public static class UsdFormat
{
    /// <summary>Formats an amount as <c>$149.00</c>, or <c>-$5.00</c> when negative.</summary>
    public static string Format(decimal amount) =>
        (amount < 0 ? "-$" : "$") + Math.Abs(amount).ToString("N2", CultureInfo.InvariantCulture);

    /// <summary>Formats a price change as <c>+$52.15</c>, <c>-$5.00</c> or <c>$0.00</c>.</summary>
    public static string FormatChange(decimal amount) => amount > 0 ? "+" + Format(amount) : Format(amount);

    /// <summary>
    /// Formats a material surcharge: <paramref name="includedText"/> (e.g. a localized "Included") when it is zero,
    /// otherwise the change, e.g. <c>+$52.15</c>.
    /// </summary>
    public static string FormatSurcharge(decimal amount, string includedText) => amount == 0 ? includedText : FormatChange(amount);
}
