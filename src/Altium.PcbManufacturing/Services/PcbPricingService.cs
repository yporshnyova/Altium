using Altium.PcbManufacturing.Models;

namespace Altium.PcbManufacturing.Services;

/// <summary>
/// Calculates the estimated price of the sample order from the catalog base price and a material's multiplier.
/// </summary>
/// <remarks>
/// <c>EstimatedPrice = BasePrice × PriceMultiplier</c>, rounded to cents with midpoint values rounded away from zero;
/// <c>MaterialSurcharge = EstimatedPrice − BasePrice</c>. All amounts are <see cref="decimal"/> US dollars.
/// </remarks>
public sealed class PcbPricingService
{
    private const int сurrencyDecimalPlaces = 2;

    public PcbPricingService(PcbCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        BasePrice = catalog.BasePriceUsd;
    }

    public decimal BasePrice { get; }

    public PriceEstimate Estimate(PcbMaterial? material)
    {
        ArgumentNullException.ThrowIfNull(material);

        decimal estimatedPrice = Math.Round(BasePrice * material.PriceMultiplier, сurrencyDecimalPlaces, MidpointRounding.AwayFromZero);
        return new PriceEstimate(BasePrice, estimatedPrice - BasePrice, estimatedPrice);
    }
}
