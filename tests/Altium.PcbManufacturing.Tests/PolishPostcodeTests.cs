using Altium.PcbManufacturing.Models;

namespace Altium.PcbManufacturing.Tests;

public sealed class PolishPostcodeTests
{
    [Theory]
    [InlineData("55-220")]
    [InlineData("00-001")]
    [InlineData("99-999")]
    public void IsValid_WellFormedPostcode_ReturnsTrue(string postcode) => Assert.True(PolishPostcode.IsValid(postcode));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("55220")]
    [InlineData("55-22")]
    [InlineData("555-220")]
    [InlineData("55_220")]
    [InlineData(" 55-220")]
    [InlineData("55-220\n")]
    [InlineData("٥٥-٢٢٠")]
    public void IsValid_MalformedPostcode_ReturnsFalse(string? postcode) => Assert.False(PolishPostcode.IsValid(postcode));
}
