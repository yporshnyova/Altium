namespace Altium.PcbManufacturing.Models;

/// <summary>
/// Estimated manufacturing price of the sample order for one material, in US dollars rounded to cents.
/// </summary>
/// <remarks>An estimate from catalog data only; it is not a final commercial quote.</remarks>
/// <param name="BasePrice">The catalog base price.</param>
/// <param name="MaterialSurcharge">The amount the material adds to the base price; zero for a 1.00 multiplier.</param>
/// <param name="EstimatedPrice">The base price plus the material surcharge.</param>
public sealed record PriceEstimate(decimal BasePrice, decimal MaterialSurcharge, decimal EstimatedPrice);
