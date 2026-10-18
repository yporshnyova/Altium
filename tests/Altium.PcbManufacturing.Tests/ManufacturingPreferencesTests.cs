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
    public void Constructor_StartsWithDefaultProjectName() => Assert.Equal("PCB-001", preferences.ProjectName);

    [Fact]
    public void Constructor_StartsWithEmptyNotes() => Assert.Equal(string.Empty, preferences.Notes);

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
    public void ProjectName_Changed_UpdatesStateAndNotifies()
    {
        preferences.ProjectName = "Sensor board";

        Assert.Equal("Sensor board", preferences.ProjectName);
        Assert.Equal([nameof(ManufacturingPreferences.ProjectName)], changedProperties);
    }

    [Fact]
    public void Notes_Changed_UpdatesStateAndNotifies()
    {
        preferences.Notes = "Rush order";

        Assert.Equal("Rush order", preferences.Notes);
        Assert.Equal([nameof(ManufacturingPreferences.Notes)], changedProperties);
    }

    [Fact]
    public void MatchesDefaults_Initially_IsTrue() => Assert.True(preferences.MatchesDefaults());

    [Fact]
    public void MatchesDefaults_AfterAnyChange_IsFalse()
    {
        Action<ManufacturingPreferences>[] changes =
        [
            p => p.ProjectName = "Other",
            p => p.Postcode = "55-220",
            p => p.SelectedMaterial = catalog.Materials[1],
            p => p.SelectedSolderMaskColor = catalog.SolderMaskColors[1],
            p => p.SelectedBoardThickness = catalog.BoardThicknesses[1],
            p => p.Notes = "x",
        ];

        foreach (Action<ManufacturingPreferences> change in changes)
        {
            ManufacturingPreferences changed = new(catalog);
            change(changed);
            Assert.False(changed.MatchesDefaults());
        }
    }

    [Fact]
    public void ResetToDefaults_RestoresDefaultsAndNotifiesChangedValues()
    {
        preferences.ProjectName = "Other";
        preferences.SelectedMaterial = catalog.Materials[1];
        preferences.Notes = "x";
        changedProperties.Clear();

        preferences.ResetToDefaults();

        Assert.True(preferences.MatchesDefaults());
        Assert.Equal(
            [
                nameof(ManufacturingPreferences.ProjectName),
                nameof(ManufacturingPreferences.SelectedMaterial),
                nameof(ManufacturingPreferences.Notes),
            ],
            changedProperties);
    }

    [Fact]
    public void AssigningUnchangedValues_DoesNotNotify()
    {
        preferences.Notes = string.Empty;
        preferences.ProjectName = ManufacturingPreferences.DefaultProjectName;
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
