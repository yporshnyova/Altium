using Altium.PcbManufacturing.Models;
using Altium.PcbManufacturing.ViewModels;

namespace Altium.PcbManufacturing.Tests;

/// <summary>
/// The Quote screen: the grouped parameter table, prices and preview inputs, all derived from the shared state and kept
/// in step with Preferences. Back / Continue are covered by <see cref="WorkflowTests"/>.
/// </summary>
public sealed class QuoteViewModelTests
{
    private readonly TestScreens screens = new();

    private QuoteViewModel ViewModel => screens.Quote;

    private PcbCatalog Catalog => screens.Catalog;

    [Fact]
    public void ParameterTable_ShowsTheSpecificationAndSelectionsInGroups()
    {
        Assert.Equal(["Dimensions", "Layers", "Finish", "Board preferences"], ViewModel.ParameterGroups.Select(group => group.Title));
        // Test catalog: 120 × 60 mm, 4 layers, 2 oz, ENIG, white silkscreen; default selections.
        Assert.Equal(
            [
                ("Size", "120 × 60 mm"),
                ("Quantity", "10 pcs."),
                ("Copper layers", "4 layers"),
                ("Copper weight", "2 oz"),
                ("Surface finish", "ENIG"),
                ("Silkscreen color", "White"),
                ("Material", "FR-4"),
                ("Solder mask", "Green"),
                ("Board thickness", "1.6 mm"),
            ],
            Rows().Select(row => (row.Label, row.Value)));
        Assert.Equal("#00AA00", Row("Solder mask").SwatchHexColor);
    }

    [Fact]
    public void Initially_ShowsTheDefaultPrice()
    {
        Assert.Equal("$149.00", ViewModel.BaseUnitPriceText);
        Assert.Equal("Included", ViewModel.MaterialSurchargePerUnitText);
        Assert.Equal("$149.00", ViewModel.EstimatedUnitPriceText);
        Assert.Equal("$1,490.00", ViewModel.EstimatedTotalPriceText);
        Assert.Equal("5 days", ViewModel.EstimatedProductionTimeText);
    }

    [Fact]
    public void PreferenceChanges_UpdateTheTablePriceAndPreviewInputs()
    {
        List<string?> changed = [];
        ViewModel.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        screens.Preferences.SelectedMaterial = Catalog.Materials[1];
        screens.Preferences.QuantityInput = "4";
        screens.Preferences.SelectedSolderMaskColor = Catalog.SolderMaskColors[1];
        screens.Preferences.SelectedBoardThickness = Catalog.BoardThicknesses[1];

        // Rogers: 149.00 × 2.5 = 372.50 per board, 12 days; × 4 boards.
        Assert.Equal(new PriceEstimate(149.00m, 223.50m, 372.50m, 4, 1490.00m), ViewModel.PriceEstimate);
        Assert.Equal("$1,490.00", ViewModel.EstimatedTotalPriceText);
        Assert.Equal("12 days", ViewModel.EstimatedProductionTimeText);
        Assert.Equal("Rogers", Row("Material").Value);
        Assert.Equal("4 pcs.", Row("Quantity").Value);
        Assert.Equal("0.8 mm", Row("Board thickness").Value);
        Assert.Equal("#000000", Row("Solder mask").SwatchHexColor);
        Assert.Equal("#000000", ViewModel.SolderMaskHexColor);
        // Test catalog: 1.6 mm is the thickest, so 0.8 mm is drawn at half height.
        Assert.Equal(0.5, ViewModel.RelativeBoardThickness);
        Assert.Contains(nameof(QuoteViewModel.EstimatedTotalPriceText), changed);
        Assert.Contains(nameof(QuoteViewModel.SolderMaskHexColor), changed);
        Assert.Contains(nameof(QuoteViewModel.RelativeBoardThickness), changed);
    }

    [Fact]
    public void Prices_MatchThePreferencesSummary()
    {
        screens.Preferences.SelectedMaterial = Catalog.Materials[1];
        screens.Preferences.QuantityInput = "37";

        Assert.Equal(screens.Preferences.PriceEstimate, ViewModel.PriceEstimate);
        Assert.Equal(screens.Preferences.EstimatedTotalPriceText, ViewModel.EstimatedTotalPriceText);
    }

    [Fact]
    public void InvalidQuantityOnPreferences_IsNeverShown()
    {
        screens.Preferences.QuantityInput = "0";

        Assert.Equal("10 pcs.", Row("Quantity").Value);
        Assert.Equal("$1,490.00", ViewModel.EstimatedTotalPriceText);
    }

    private IEnumerable<QuoteParameter> Rows() => ViewModel.ParameterGroups.SelectMany(group => group.Parameters);

    private QuoteParameter Row(string label) => Rows().Single(row => row.Label == label);
}
