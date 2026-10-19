using System.Globalization;
using Altium.PcbManufacturing.Formatting;
using Altium.PcbManufacturing.Models;
using Altium.PcbManufacturing.Services;

namespace Altium.PcbManufacturing.Tests;

/// <summary>
/// The price rules: base price × material multiplier per board, rounded to cents, × quantity; and USD display.
/// </summary>
public sealed class PricingTests
{
    private readonly PcbPricingService pricing = new(TestCatalogs.Create());

    [Theory]
    [InlineData("1.00", "149.00", "0.00")]
    [InlineData("2.40", "357.60", "208.60")]
    public void Estimate_AppliesTheMaterialMultiplierToTheBasePrice(string multiplier, string unitPrice, string surcharge)
    {
        PriceEstimate estimate = pricing.Estimate(Material(decimal.Parse(multiplier, CultureInfo.InvariantCulture)), 1);

        Assert.Equal(149.00m, estimate.BaseUnitPrice);
        Assert.Equal(decimal.Parse(unitPrice, CultureInfo.InvariantCulture), estimate.EstimatedUnitPrice);
        Assert.Equal(decimal.Parse(surcharge, CultureInfo.InvariantCulture), estimate.MaterialSurchargePerUnit);
    }

    [Fact]
    public void Estimate_RogersQuantityTen_MatchesTheSpecifiedExample()
    {
        PriceEstimate estimate = pricing.Estimate(Material(2.40m), 10);

        Assert.Equal(new PriceEstimate(149.00m, 208.60m, 357.60m, 10, 3576.00m), estimate);
    }

    [Fact]
    public void Estimate_RoundsTheUnitPriceToCentsBeforeMultiplying()
    {
        // 149 × 1.333 = 198.617 → 198.62 per board; × 3 = 595.86 (595.851 if the unrounded price were multiplied).
        PriceEstimate estimate = pricing.Estimate(Material(1.333m), 3);

        Assert.Equal(198.62m, estimate.EstimatedUnitPrice);
        Assert.Equal(595.86m, estimate.EstimatedTotalPrice);
    }

    [Fact]
    public void Estimate_RoundsMidpointAwayFromZero()
    {
        // 149 × 1.005 = 149.745 exactly in decimal; banker's rounding would give 149.74.
        Assert.Equal(149.75m, pricing.Estimate(Material(1.005m), 1).EstimatedUnitPrice);
    }

    [Fact]
    public void Estimate_MaximumQuantity_DoesNotOverflow() =>
        Assert.Equal(357.60m * int.MaxValue, pricing.Estimate(Material(2.40m), int.MaxValue).EstimatedTotalPrice);

    [Fact]
    public void Estimate_ZeroQuantity_Throws() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => pricing.Estimate(Material(1.00m), 0));

    [Fact]
    public void UsdFormat_IsTheSameInEveryCulture()
    {
        CultureInfo original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("pl-PL");

            Assert.Equal("$1,234.50", UsdFormat.Format(1234.5m));
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    private static PcbMaterial Material(decimal priceMultiplier) => new("test", "Test", priceMultiplier, 5);
}
