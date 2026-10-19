using Altium.PcbManufacturing.Models;

namespace Altium.PcbManufacturing.Services;

/// <summary>
/// Calculates the estimated order price from the catalog base price per board, a material's multiplier and the
/// number of boards.
/// </summary>

public sealed class PcbPricingService
{
    private const int сurrencyDecimalPlaces = 2;

    public PcbPricingService(PcbCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        BaseUnitPrice = catalog.BasePriceUsd;
    }

    public decimal BaseUnitPrice { get; }

    public PriceEstimate Estimate(PcbMaterial? material, int quantity)
    {
        ArgumentNullException.ThrowIfNull(material);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);

        decimal unitPrice = Math.Round(BaseUnitPrice * material.PriceMultiplier, сurrencyDecimalPlaces, MidpointRounding.AwayFromZero);
        return new PriceEstimate(BaseUnitPrice, unitPrice - BaseUnitPrice, unitPrice, quantity, unitPrice * quantity);
    }
}
