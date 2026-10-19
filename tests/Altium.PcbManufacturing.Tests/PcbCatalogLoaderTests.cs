using System.IO;
using System.Text.Json.Nodes;
using Altium.PcbManufacturing.Exceptions;
using Altium.PcbManufacturing.Models;
using Altium.PcbManufacturing.Services;

namespace Altium.PcbManufacturing.Tests;

/// <summary>
/// Loading the JSON preset catalog: the shipped file, a valid file, and representative invalid files.
/// </summary>
public sealed class PcbCatalogLoaderTests : IDisposable
{
    private readonly string filePath = Path.Combine(Path.GetTempPath(), $"pcb-catalog-{Guid.NewGuid():N}.json");

    public void Dispose() => File.Delete(filePath);

    [Fact]
    public async Task ShippedCatalog_IsCopiedToOutputAndValid()
    {
        PcbCatalog catalog = await new PcbCatalogLoader(PcbCatalogLoader.DefaultCatalogPath).LoadAsync(TestContext.Current.CancellationToken);

        // The assignment asks for 2–3 predefined options of each kind.
        Assert.InRange(catalog.Materials.Count, 2, 3);
        Assert.InRange(catalog.SolderMaskColors.Count, 2, 3);
        Assert.InRange(catalog.BoardThicknesses.Count, 2, 3);
    }

    [Fact]
    public async Task ValidCatalog_LoadsEverySection()
    {
        PcbCatalog catalog = await LoadAsync(TestCatalogs.ValidJson);

        Assert.Equal(TestCatalogs.Create().BasePriceUsd, catalog.BasePriceUsd);
        Assert.Equal(TestCatalogs.Create().Materials, catalog.Materials);
        Assert.Equal(TestCatalogs.Create().SolderMaskColors, catalog.SolderMaskColors);
        Assert.Equal(TestCatalogs.Create().BoardThicknesses, catalog.BoardThicknesses);
        Assert.Equal(TestCatalogs.Create().BoardSpecification, catalog.BoardSpecification);
    }

    [Fact]
    public async Task MissingFile_ThrowsWithThePath()
    {
        CatalogLoadException ex = await Assert.ThrowsAsync<CatalogLoadException>(
            () => new PcbCatalogLoader(filePath).LoadAsync(TestContext.Current.CancellationToken));

        Assert.Contains(filePath, ex.Message);
    }

    [Fact]
    public async Task MalformedJson_Throws()
    {
        CatalogLoadException ex = await Assert.ThrowsAsync<CatalogLoadException>(() => LoadAsync("not json"));

        Assert.Contains("is not valid JSON", ex.Message);
    }

    public static TheoryData<string, string> InvalidEntries => new()
    {
        { "missing base price", "basePriceUsd must be greater than zero." },
        { "duplicate material id", "materials[1].id 'fr4' is duplicated." },
        { "zero price multiplier", "materials[0].priceMultiplier must be greater than zero." },
        { "invalid hex color", "solderMaskColors[0].hexColor must be in #RRGGBB format." },
        { "missing board specification", "boardSpecification is required." },
    };

    [Theory]
    [MemberData(nameof(InvalidEntries))]
    public async Task InvalidEntry_ThrowsDescribingTheEntry(string scenario, string expectedError)
    {
        JsonObject json = JsonNode.Parse(TestCatalogs.ValidJson)!.AsObject();
        Action<JsonObject> mutate = scenario switch
        {
            "missing base price" => c => c.Remove("basePriceUsd"),
            "duplicate material id" => c => c["materials"]![1]!["id"] = "FR4",
            "zero price multiplier" => c => c["materials"]![0]!["priceMultiplier"] = 0,
            "invalid hex color" => c => c["solderMaskColors"]![0]!["hexColor"] = "green",
            "missing board specification" => c => c.Remove("boardSpecification"),
            _ => throw new ArgumentOutOfRangeException(nameof(scenario), scenario, null),
        };
        mutate(json);

        CatalogLoadException ex = await Assert.ThrowsAsync<CatalogLoadException>(() => LoadAsync(json.ToJsonString()));

        Assert.Contains(expectedError, ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<PcbCatalog> LoadAsync(string json)
    {
        await File.WriteAllTextAsync(filePath, json, TestContext.Current.CancellationToken);
        return await new PcbCatalogLoader(filePath).LoadAsync(TestContext.Current.CancellationToken);
    }
}
