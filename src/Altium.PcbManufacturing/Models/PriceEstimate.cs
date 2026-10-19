namespace Altium.PcbManufacturing.Models;

/// <summary>
/// Estimated manufacturing price of an order: the per-board amounts and the order total, in US dollars.
/// </summary>
/// <param name="BaseUnitPrice">The catalog base price of one board.</param>
/// <param name="MaterialSurchargePerUnit">The amount the material adds to one board; zero for a 1.00 multiplier.</param>
/// <param name="EstimatedUnitPrice">The base unit price plus the material surcharge per board.</param>
/// <param name="Quantity">The number of boards.</param>
/// <param name="EstimatedTotalPrice">The estimated unit price multiplied by the quantity.</param>
public sealed record PriceEstimate(
    decimal BaseUnitPrice,
    decimal MaterialSurchargePerUnit,
    decimal EstimatedUnitPrice,
    int Quantity,
    decimal EstimatedTotalPrice);
