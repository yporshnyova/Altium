using Altium.PcbManufacturing.Models;

namespace Altium.PcbManufacturing.Tests;

internal static class TestCatalogs
{
    public const string ValidJson = """
        {
          "basePriceUsd": 149.00,
          "materials": [
            { "id": "fr4", "name": "FR-4", "priceMultiplier": 1.0, "productionDays": 5 },
            { "id": "rogers", "name": "Rogers", "priceMultiplier": 2.5, "productionDays": 12 }
          ],
          "solderMaskColors": [
            { "id": "green", "name": "Green", "hexColor": "#00AA00" },
            { "id": "black", "name": "Black", "hexColor": "#000000" }
          ],
          "boardThicknesses": [
            { "id": "1.6mm", "millimeters": 1.6 },
            { "id": "0.8mm", "millimeters": 0.8 }
          ],
          "boardSpecification": {
            "widthMm": 120,
            "heightMm": 60,
            "layerCount": 4,
            "copperWeightOz": 2,
            "surfaceFinish": { "id": "enig", "name": "ENIG" },
            "silkscreenColor": { "id": "white", "name": "White" }
          }
        }
        """;

    public static PcbCatalog Create() => new(
        BasePriceUsd: 149.00m,
        Materials:
        [
            new PcbMaterial("fr4", "FR-4", 1.0m, 5),
            new PcbMaterial("rogers", "Rogers", 2.5m, 12),
        ],
        SolderMaskColors:
        [
            new SolderMaskColor("green", "Green", "#00AA00"),
            new SolderMaskColor("black", "Black", "#000000"),
        ],
        BoardThicknesses:
        [
            new BoardThickness("1.6mm", 1.6m),
            new BoardThickness("0.8mm", 0.8m),
        ],
        BoardSpecification: new BoardSpecification(
            WidthMm: 120m,
            HeightMm: 60m,
            LayerCount: 4,
            CopperWeightOz: 2m,
            SurfaceFinish: new CatalogOption("enig", "ENIG"),
            SilkscreenColor: new CatalogOption("white", "White")));
}
