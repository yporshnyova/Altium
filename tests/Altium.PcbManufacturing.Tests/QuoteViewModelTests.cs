using Altium.PcbManufacturing.Models;
using Altium.PcbManufacturing.ViewModels;

namespace Altium.PcbManufacturing.Tests;

/// <summary>
/// The Quote step: the grouped parameter table and the preview inputs it derives from the draft. Prices are the
/// draft's (see <see cref="ManufacturingOrderDraftTests"/>); Continue and Back are the wizard's.
/// </summary>
public sealed class QuoteViewModelTests
{
    private readonly TestWizard wizard = new();

    private QuoteViewModel ViewModel => wizard.Quote;

    private PcbCatalog Catalog => wizard.Catalog;

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
    public void DraftChanges_UpdateTheTableAndThePreviewInputs()
    {
        List<string?> changed = [];
        ViewModel.PropertyChanged += (_, e) => changed.Add(e.PropertyName);
        List<string?> quantityRowChanges = [];
        Row("Quantity").PropertyChanged += (_, e) => quantityRowChanges.Add(e.PropertyName);

        wizard.Draft.SelectedMaterial = Catalog.Materials[1];
        wizard.Preferences.QuantityInput = "4";
        wizard.Draft.SelectedSolderMaskColor = Catalog.SolderMaskColors[1];
        wizard.Draft.SelectedBoardThickness = Catalog.BoardThicknesses[1];

        Assert.Equal("Rogers", Row("Material").Value);
        Assert.Equal("4 pcs.", Row("Quantity").Value);
        Assert.Contains(nameof(QuoteParameter.Value), quantityRowChanges);
        Assert.Equal("0.8 mm", Row("Board thickness").Value);
        Assert.Equal("Black", Row("Solder mask").Value);
        Assert.Equal("#000000", Row("Solder mask").SwatchHexColor);
        // Test catalog: 1.6 mm is the thickest, so 0.8 mm is drawn at half height.
        Assert.Equal(0.5, ViewModel.RelativeBoardThickness);
        Assert.Contains(nameof(QuoteViewModel.RelativeBoardThickness), changed);
    }

    private IEnumerable<QuoteParameter> Rows() => ViewModel.ParameterGroups.SelectMany(group => group.Parameters);

    private QuoteParameter Row(string label) => Rows().Single(row => row.Label == label);
}
