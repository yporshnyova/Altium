using System.ComponentModel;
using Altium.PcbManufacturing.Models;

namespace Altium.PcbManufacturing.Tests;

public sealed class ManufacturingPreferencesTests
{
    private readonly PcbCatalog catalog = TestCatalogs.Create();
    private readonly ManufacturingPreferences preferences;
    private readonly List<string?> changedProperties = [];

    public ManufacturingPreferencesTests()
    {
        preferences = new ManufacturingPreferences(catalog);
        preferences.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName);
    }

    [Fact]
    public void Constructor_SelectsFirstCatalogEntryOfEachOption()
    {
        Assert.Same(catalog.Materials[0], preferences.SelectedMaterial);
        Assert.Same(catalog.SolderMaskColors[0], preferences.SelectedSolderMaskColor);
        Assert.Same(catalog.BoardThicknesses[0], preferences.SelectedBoardThickness);
    }

    [Fact]
    public void Constructor_StartsWithEmptyPostcode() => Assert.Equal(string.Empty, preferences.Postcode);

    [Fact]
    public void SelectedMaterial_Changed_UpdatesStateAndNotifies()
    {
        preferences.SelectedMaterial = catalog.Materials[1];

        Assert.Same(catalog.Materials[1], preferences.SelectedMaterial);
        Assert.Equal([nameof(ManufacturingPreferences.SelectedMaterial)], changedProperties);
    }

    [Fact]
    public void SelectedSolderMaskColor_Changed_UpdatesStateAndNotifies()
    {
        preferences.SelectedSolderMaskColor = catalog.SolderMaskColors[1];

        Assert.Same(catalog.SolderMaskColors[1], preferences.SelectedSolderMaskColor);
        Assert.Equal([nameof(ManufacturingPreferences.SelectedSolderMaskColor)], changedProperties);
    }

    [Fact]
    public void SelectedBoardThickness_Changed_UpdatesStateAndNotifies()
    {
        preferences.SelectedBoardThickness = catalog.BoardThicknesses[1];

        Assert.Same(catalog.BoardThicknesses[1], preferences.SelectedBoardThickness);
        Assert.Equal([nameof(ManufacturingPreferences.SelectedBoardThickness)], changedProperties);
    }

    [Fact]
    public void Postcode_Changed_UpdatesStateAndNotifies()
    {
        preferences.Postcode = "12345";

        Assert.Equal("12345", preferences.Postcode);
        Assert.Equal([nameof(ManufacturingPreferences.Postcode)], changedProperties);
    }

    [Fact]
    public void AssigningUnchangedValues_DoesNotNotify()
    {
        preferences.SelectedMaterial = catalog.Materials[0];
        preferences.SelectedSolderMaskColor = catalog.SolderMaskColors[0];
        preferences.SelectedBoardThickness = catalog.BoardThicknesses[0];
        preferences.Postcode = string.Empty;

        Assert.Empty(changedProperties);
    }

    [Fact]
    public void PropertyChanged_IsRaisedAfterValueIsUpdated()
    {
        PcbMaterial? observed = null;
        preferences.PropertyChanged += (sender, _) => observed = ((ManufacturingPreferences)sender!).SelectedMaterial;

        preferences.SelectedMaterial = catalog.Materials[1];

        Assert.Same(catalog.Materials[1], observed);
    }
}
