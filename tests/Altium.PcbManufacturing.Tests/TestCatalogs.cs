using Altium.PcbManufacturing.Models;

namespace Altium.PcbManufacturing.Tests;

internal static class TestCatalogs
{
    public const string ValidJson = """
        {
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
          "parameterGroups": [
            {
              "name": "Dimensions",
              "parameters": [
                { "name": "Width", "value": "100 mm" },
                { "name": "Height", "value": "80 mm" }
              ]
            }
          ]
        }
        """;

    public static PcbCatalog Create() => new(
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
        ParameterGroups:
        [
            new ManufacturingParameterGroup("Dimensions", [new ManufacturingParameter("Width", "100 mm")]),
        ]);
}
