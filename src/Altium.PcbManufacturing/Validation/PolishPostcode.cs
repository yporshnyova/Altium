using System.Text.RegularExpressions;

namespace Altium.PcbManufacturing.Validation;

/// <summary>
/// Postcode format rule for orders.
/// </summary>

public static partial class PolishPostcode
{
    /// <summary>
    /// Anchored pattern for the <c>NN-NNN</c> form, shared by attribute-based validation and <see cref="IsValid"/>.
    /// </summary>
    public const string Pattern = @"^[0-9]{2}-[0-9]{3}\z";

    public static bool IsValid(string? postcode) => postcode is not null && PostcodeRegex().IsMatch(postcode);

    [GeneratedRegex(Pattern)]
    private static partial Regex PostcodeRegex();
}
