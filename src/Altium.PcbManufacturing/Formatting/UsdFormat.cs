using System.Globalization;

namespace Altium.PcbManufacturing.Formatting;

/// <summary>
/// Formats USD amounts consistently, regardless of the user's regional settings.
/// </summary>

public static class UsdFormat
{
    private static readonly CultureInfo usCulture = CultureInfo.GetCultureInfo("en-US");
    private static readonly string format = "C2";

    public static string Format(decimal amount) =>
        amount.ToString(format, usCulture);
}
