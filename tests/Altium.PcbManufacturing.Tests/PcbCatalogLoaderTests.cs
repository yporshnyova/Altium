using System.IO;
using System.Text.Json.Nodes;
using Altium.PcbManufacturing.Models;
using Altium.PcbManufacturing.Services;

namespace Altium.PcbManufacturing.Tests;

public sealed class PcbCatalogLoaderTests : IDisposable
{
    private readonly string filePath = Path.Combine(Path.GetTempPath(), $"pcb-catalog-{Guid.NewGuid():N}.json");

    public void Dispose() => File.Delete(filePath);

    [Fact]
    public async Task LoadAsync_ValidCatalog_LoadsMaterials()
    {
        PcbCatalog catalog = await LoadAsync(TestCatalogs.ValidJson);

        Assert.Equal(
            [new PcbMaterial("fr4", "FR-4", 1.0m, 5), new PcbMaterial("rogers", "Rogers", 2.5m, 12)],
            catalog.Materials);
    }

    [Fact]
    public async Task LoadAsync_ValidCatalog_LoadsSolderMaskColors()
    {
        PcbCatalog catalog = await LoadAsync(TestCatalogs.ValidJson);

        Assert.Equal(
            [new SolderMaskColor("green", "Green", "#00AA00"), new SolderMaskColor("black", "Black", "#000000")],
            catalog.SolderMaskColors);
    }

    [Fact]
    public async Task LoadAsync_ValidCatalog_LoadsBoardThicknesses()
    {
        PcbCatalog catalog = await LoadAsync(TestCatalogs.ValidJson);

        Assert.Equal([new BoardThickness("1.6mm", 1.6m), new BoardThickness("0.8mm", 0.8m)], catalog.BoardThicknesses);
    }

    [Fact]
    public async Task LoadAsync_ValidCatalog_LoadsParameterGroups()
    {
        PcbCatalog catalog = await LoadAsync(TestCatalogs.ValidJson);

        ManufacturingParameterGroup group = Assert.Single(catalog.ParameterGroups);
        Assert.Equal("Dimensions", group.Name);
        Assert.Equal(
            [new ManufacturingParameter("Width", "100 mm"), new ManufacturingParameter("Height", "80 mm")],
            group.Parameters);
    }

    [Fact]
    public async Task LoadAsync_ShippedCatalog_IsCopiedToOutputAndValid()
    {
        PcbCatalog catalog = await new PcbCatalogLoader(PcbCatalogLoader.DefaultCatalogPath)
            .LoadAsync(TestContext.Current.CancellationToken);

        Assert.InRange(catalog.Materials.Count, 2, 3);
        Assert.InRange(catalog.SolderMaskColors.Count, 2, 3);
        Assert.InRange(catalog.BoardThicknesses.Count, 2, 3);
        Assert.NotEmpty(catalog.ParameterGroups);
    }

    [Fact]
    public async Task LoadAsync_MissingFile_ThrowsWithPath()
    {
        var loader = new PcbCatalogLoader(filePath);

        CatalogLoadException ex = await Assert.ThrowsAsync<CatalogLoadException>(
            () => loader.LoadAsync(TestContext.Current.CancellationToken));

        Assert.Contains("was not found", ex.Message);
        Assert.Contains(filePath, ex.Message);
    }

    [Theory]
    [InlineData("{ \"materials\": [ ")]
    [InlineData("not json")]
    [InlineData("")]
    public async Task LoadAsync_MalformedJson_Throws(string json)
    {
        CatalogLoadException ex = await LoadInvalidAsync(json);

        Assert.Contains("is not valid JSON", ex.Message);
    }

    [Fact]
    public async Task LoadAsync_NullDocument_Throws()
    {
        CatalogLoadException ex = await LoadInvalidAsync("null");

        Assert.Contains("does not contain a catalog", ex.Message);
    }

    [Fact]
    public async Task LoadAsync_UnknownProperty_Throws()
    {
        CatalogLoadException ex = await LoadInvalidAsync(Mutate(c => c["materials"]![0]!["priceMultipler"] = 1.1));

        Assert.Contains("priceMultipler", ex.Message);
    }

    public static TheoryData<string, string> InvalidEntries => new()
    {
        { "missing materials", "materials must contain at least one entry." },
        { "empty solder mask colors", "solderMaskColors must contain at least one entry." },
        { "missing material id", "materials[0].id is required." },
        { "duplicate material id", "materials[1].id 'fr4' is duplicated." },
        { "blank material name", "materials[0].name is required." },
        { "zero price multiplier", "materials[0].priceMultiplier must be greater than zero." },
        { "negative production days", "materials[1].productionDays must be greater than zero." },
        { "invalid hex color", "solderMaskColors[0].hexColor must be in #RRGGBB format." },
        { "zero thickness", "boardThicknesses[0].millimeters must be greater than zero." },
        { "null thickness entry", "boardThicknesses[1] is missing." },
        { "empty parameter group", "parameterGroups[0].parameters must contain at least one entry." },
        { "blank parameter value", "parameterGroups[0].parameters[1].value is required." },
    };

    [Theory]
    [MemberData(nameof(InvalidEntries))]
    public async Task LoadAsync_InvalidEntry_ThrowsDescribingEntry(string scenario, string expectedError)
    {
        CatalogLoadException ex = await LoadInvalidAsync(Mutate(scenario switch
        {
            "missing materials" => c => c.Remove("materials"),
            "empty solder mask colors" => c => c["solderMaskColors"] = new JsonArray(),
            "missing material id" => c => c["materials"]![0]!.AsObject().Remove("id"),
            "duplicate material id" => c => c["materials"]![1]!["id"] = "FR4",
            "blank material name" => c => c["materials"]![0]!["name"] = " ",
            "zero price multiplier" => c => c["materials"]![0]!["priceMultiplier"] = 0,
            "negative production days" => c => c["materials"]![1]!["productionDays"] = -1,
            "invalid hex color" => c => c["solderMaskColors"]![0]!["hexColor"] = "green",
            "zero thickness" => c => c["boardThicknesses"]![0]!["millimeters"] = 0,
            "null thickness entry" => c => c["boardThicknesses"]![1] = null,
            "empty parameter group" => c => c["parameterGroups"]![0]!["parameters"] = new JsonArray(),
            "blank parameter value" => c => c["parameterGroups"]![0]!["parameters"]![1]!["value"] = "",
            _ => throw new ArgumentOutOfRangeException(nameof(scenario), scenario, null),
        }));

        Assert.Contains("is invalid", ex.Message);
        Assert.Contains(expectedError, ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static string Mutate(Action<JsonObject> mutation)
    {
        JsonObject catalog = JsonNode.Parse(TestCatalogs.ValidJson)!.AsObject();
        mutation(catalog);
        return catalog.ToJsonString();
    }

    private async Task<PcbCatalog> LoadAsync(string json)
    {
        await File.WriteAllTextAsync(filePath, json, TestContext.Current.CancellationToken);
        return await new PcbCatalogLoader(filePath).LoadAsync(TestContext.Current.CancellationToken);
    }

    private async Task<CatalogLoadException> LoadInvalidAsync(string json) =>
        await Assert.ThrowsAsync<CatalogLoadException>(() => LoadAsync(json));
}
