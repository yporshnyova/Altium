using Altium.PcbManufacturing.Models;
using Altium.PcbManufacturing.Services;

namespace Altium.PcbManufacturing.Tests;

public sealed class PcbPricingServiceTests
{
    private readonly PcbPricingService pricing = new(TestCatalogs.Create());

    [Fact]
    public void BasePrice_ComesFromCatalog() => Assert.Equal(149.00m, pricing.BasePrice);

    [Theory]
    [InlineData("1.00", "149.00", "0.00")]
    [InlineData("1.25", "186.25", "37.25")]
    [InlineData("1.35", "201.15", "52.15")]
    [InlineData("2.40", "357.60", "208.60")]
    public void Estimate_AppliesMaterialMultiplierToBasePrice(string multiplier, string estimatedPrice, string surcharge)
    {
        PriceEstimate estimate = pricing.Estimate(Material(decimal.Parse(multiplier, System.Globalization.CultureInfo.InvariantCulture)));

        Assert.Equal(149.00m, estimate.BasePrice);
        Assert.Equal(decimal.Parse(estimatedPrice, System.Globalization.CultureInfo.InvariantCulture), estimate.EstimatedPrice);
        Assert.Equal(decimal.Parse(surcharge, System.Globalization.CultureInfo.InvariantCulture), estimate.MaterialSurcharge);
    }

    [Fact]
    public void Estimate_SurchargeIsEstimatedPriceMinusBasePrice()
    {
        PriceEstimate estimate = pricing.Estimate(Material(1.333m));

        Assert.Equal(estimate.EstimatedPrice - estimate.BasePrice, estimate.MaterialSurcharge);
    }

    [Fact]
    public void Estimate_RoundsToCents()
    {
        // 149 × 1.333 = 198.617
        PriceEstimate estimate = pricing.Estimate(Material(1.333m));

        Assert.Equal(198.62m, estimate.EstimatedPrice);
        Assert.Equal(49.62m, estimate.MaterialSurcharge);
    }

    [Fact]
    public void Estimate_RoundsMidpointAwayFromZero()
    {
        // 149 × 1.005 = 149.745 exactly in decimal; banker's rounding would give 149.74.
        PriceEstimate estimate = pricing.Estimate(Material(1.005m));

        Assert.Equal(149.75m, estimate.EstimatedPrice);
    }

    [Fact]
    public void Estimate_UsesExactDecimalArithmetic()
    {
        // 0.1 has no exact binary representation; double arithmetic would yield 14.900000000000002.
        PriceEstimate estimate = pricing.Estimate(Material(0.1m));

        Assert.Equal(14.90m, estimate.EstimatedPrice);
        Assert.Equal(-134.10m, estimate.MaterialSurcharge);
    }

    [Fact]
    public void Estimate_IsDeterministic() => Assert.Equal(pricing.Estimate(Material(2.40m)), pricing.Estimate(Material(2.40m)));

    [Fact]
    public void Estimate_NullMaterial_Throws() => Assert.Throws<ArgumentNullException>(() => pricing.Estimate(null!));

    private static PcbMaterial Material(decimal priceMultiplier) => new("test", "Test", priceMultiplier, 5);
}
