using Altium.PcbManufacturing.Models;
using Altium.PcbManufacturing.ViewModels;
using Altium.PcbManufacturing.Services;

namespace Altium.PcbManufacturing.Tests;

/// <summary>
/// The shared draft every step reads and edits: its defaults, invariants, and the calculated values with their
/// change notifications.
/// </summary>
public sealed class ManufacturingOrderDraftTests
{
    private readonly PcbCatalog catalog = TestCatalogs.Create();
    private readonly PcbPricingService pricing;
    private readonly ManufacturingOrderDraft draft;

    public ManufacturingOrderDraftTests()
    {
        pricing = new PcbPricingService(catalog);
        draft = new ManufacturingOrderDraft(catalog, pricing);
    }

    [Fact]
    public void Defaults_AreTheFirstCatalogEntriesAndTheDocumentedValues()
    {
        Assert.Same(catalog.Materials[0], draft.SelectedMaterial);
        Assert.Same(catalog.SolderMaskColors[0], draft.SelectedSolderMaskColor);
        Assert.Same(catalog.BoardThicknesses[0], draft.SelectedBoardThickness);
        Assert.Equal("PCB-001", draft.ProjectName);
        Assert.Equal(10, draft.Quantity);
        Assert.Equal(string.Empty, draft.Postcode);
        Assert.Equal(string.Empty, draft.Notes);
        Assert.False(draft.HasNotes);
        Assert.True(draft.MatchesDefaults());
        // Test catalog FR-4: $149.00 per board, 5 days.
        Assert.Equal(1490.00m, draft.TotalPrice);
        Assert.Equal(5, draft.ProductionDays);
    }

    [Fact]
    public void InvalidValues_AreRejectedAndTheCurrentValueIsKept()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => draft.Quantity = 0);
        Assert.Throws<ArgumentNullException>(() => draft.SelectedMaterial = null!);
        Assert.Throws<ArgumentNullException>(() => draft.SelectedSolderMaskColor = null!);
        Assert.Throws<ArgumentNullException>(() => draft.Postcode = null!);

        Assert.Equal(10, draft.Quantity);
        Assert.Same(catalog.Materials[0], draft.SelectedMaterial);
        Assert.Same(catalog.SolderMaskColors[0], draft.SelectedSolderMaskColor);
        Assert.Equal(string.Empty, draft.Postcode);
    }

    [Fact]
    public void MaterialChange_RecalculatesThePriceAndProductionTimeAndNotifiesThem()
    {
        List<string?> changed = Record(draft);

        draft.SelectedMaterial = catalog.Materials[1];

        // The pricing service is the only place the price is calculated.
        Assert.Equal(pricing.Estimate(catalog.Materials[1], 10), draft.PriceEstimate);
        Assert.Equal(3725.00m, draft.TotalPrice);
        Assert.Equal(12, draft.ProductionDays);
        Assert.Contains(nameof(draft.PriceEstimate), changed);
        Assert.Contains(nameof(draft.TotalPrice), changed);
        Assert.Contains(nameof(draft.ProductionDays), changed);
    }

    [Fact]
    public void QuantityChange_RecalculatesAndNotifiesTheTotal()
    {
        List<string?> changed = Record(draft);

        draft.Quantity = 3;

        Assert.Equal(447.00m, draft.TotalPrice);
        Assert.Contains(nameof(draft.PriceEstimate), changed);
        Assert.Contains(nameof(draft.TotalPrice), changed);
    }

    private static List<string?> Record(ManufacturingOrderDraft draft)
    {
        List<string?> changed = [];
        draft.PropertyChanged += (_, e) => changed.Add(e.PropertyName);
        return changed;
    }
}
