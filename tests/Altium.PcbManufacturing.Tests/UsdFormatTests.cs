using System.Globalization;
using Altium.PcbManufacturing.ViewModels;

namespace Altium.PcbManufacturing.Tests;

public sealed class UsdFormatTests
{
    [Theory]
    [InlineData("149", "$149.00")]
    [InlineData("357.6", "$357.60")]
    [InlineData("0", "$0.00")]
    [InlineData("1234.5", "$1,234.50")]
    [InlineData("-5", "-$5.00")]
    public void Format_UsesDollarSignAndTwoDecimals(string amount, string expected) =>
        Assert.Equal(expected, UsdFormat.Format(Parse(amount)));

    [Theory]
    [InlineData("52.15", "+$52.15")]
    [InlineData("208.6", "+$208.60")]
    [InlineData("0", "$0.00")]
    [InlineData("-5", "-$5.00")]
    public void FormatChange_PrefixesIncreasesWithPlus(string amount, string expected) =>
        Assert.Equal(expected, UsdFormat.FormatChange(Parse(amount)));

    [Fact]
    public void Format_IgnoresCurrentCulture()
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

    private static decimal Parse(string amount) => decimal.Parse(amount, CultureInfo.InvariantCulture);
}
