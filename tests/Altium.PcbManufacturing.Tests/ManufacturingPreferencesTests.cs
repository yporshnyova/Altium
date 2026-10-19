using Altium.PcbManufacturing.Models;

namespace Altium.PcbManufacturing.Tests;

/// <summary>
/// The shared manufacturing state that every screen reads and edits: its defaults, reset and the quantity invariant.
/// </summary>
public sealed class ManufacturingPreferencesTests
{
    private readonly PcbCatalog catalog = TestCatalogs.Create();

    [Fact]
    public void Defaults_AreTheFirstCatalogEntriesAndTheDocumentedValues()
    {
        ManufacturingPreferences preferences = new(catalog);

        Assert.Same(catalog.Materials[0], preferences.SelectedMaterial);
        Assert.Same(catalog.SolderMaskColors[0], preferences.SelectedSolderMaskColor);
        Assert.Same(catalog.BoardThicknesses[0], preferences.SelectedBoardThickness);
        Assert.Equal("PCB-001", preferences.ProjectName);
        Assert.Equal(10, preferences.Quantity);
        Assert.Equal(string.Empty, preferences.Postcode);
        Assert.Equal(string.Empty, preferences.Notes);
        Assert.True(preferences.MatchesDefaults());
    }

    [Fact]
    public void ResetToDefaults_RestoresEveryValue()
    {
        ManufacturingPreferences preferences = new(catalog)
        {
            ProjectName = "Other",
            Postcode = "55-220",
            Quantity = 25,
            SelectedMaterial = catalog.Materials[1],
            SelectedSolderMaskColor = catalog.SolderMaskColors[1],
            SelectedBoardThickness = catalog.BoardThicknesses[1],
            Notes = "Rush",
        };
        Assert.False(preferences.MatchesDefaults());

        preferences.ResetToDefaults();

        Assert.True(preferences.MatchesDefaults());
    }

    [Fact]
    public void Quantity_RejectsNonPositiveValues()
    {
        ManufacturingPreferences preferences = new(catalog);

        Assert.Throws<ArgumentOutOfRangeException>(() => preferences.Quantity = 0);
        Assert.Equal(10, preferences.Quantity);
    }

    [Theory]
    [InlineData("55-220", true)]
    [InlineData("55220", false)]
    [InlineData(" 55-220", false)]
    [InlineData("٥٥-٢٢٠", false)] // Non-ASCII digits.
    public void PolishPostcode_AcceptsOnlyTheNNNNNFormat(string postcode, bool valid) =>
        Assert.Equal(valid, PolishPostcode.IsValid(postcode));
}
