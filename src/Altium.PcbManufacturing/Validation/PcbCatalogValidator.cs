using System.Text.RegularExpressions;
using Altium.PcbManufacturing.Models;

namespace Altium.PcbManufacturing.Validation;

/// <summary>
/// Checks catalog integrity after deserialization, since JSON may omit values the model declares non-nullable.
/// </summary>

internal static partial class PcbCatalogValidator
{
    public static IReadOnlyList<string> Validate(PcbCatalog catalog)
    {
        List<string> errors = [];

        if (catalog.BasePriceUsd <= 0)
        {
            errors.Add("basePriceUsd must be greater than zero.");
        }

        if (HasEntries(catalog.Materials, "materials", errors))
        {
            ValidateMaterials(catalog, errors);
        }

        if (HasEntries(catalog.SolderMaskColors, "solderMaskColors", errors))
        {
            ValidateSolderMaskColors(catalog, errors);
        }

        if (HasEntries(catalog.BoardThicknesses, "boardThicknesses", errors))
        {
            ValidateBoardThicknesses(catalog, errors);
        }

        BoardSpecification specification = catalog.BoardSpecification;
        RequirePositive(specification.WidthMm, "boardSpecification.widthMm", errors);
        RequirePositive(specification.HeightMm, "boardSpecification.heightMm", errors);
        RequirePositive(specification.LayerCount, "boardSpecification.layerCount", errors);
        RequirePositive(specification.CopperWeightOz, "boardSpecification.copperWeightOz", errors);
        RequireOption(specification.SurfaceFinish, "boardSpecification.surfaceFinish", errors);
        RequireOption(specification.SilkscreenColor, "boardSpecification.silkscreenColor", errors);

        return errors;
    }

    private static void ValidateBoardThicknesses(PcbCatalog catalog, List<string> errors)
    {
        ValidateIds(catalog.BoardThicknesses, t => t?.Id, "boardThicknesses", errors);
        for (int i = 0; i < catalog.BoardThicknesses.Count; i++)
        {
            if (catalog.BoardThicknesses[i] is { Millimeters: <= 0 })
            {
                errors.Add($"boardThicknesses[{i}].millimeters must be greater than zero.");
            }
        }
    }

    private static void ValidateSolderMaskColors(PcbCatalog catalog, List<string> errors)
    {
        ValidateIds(catalog.SolderMaskColors, c => c?.Id, "solderMaskColors", errors);
        for (int i = 0; i < catalog.SolderMaskColors.Count; i++)
        {
            SolderMaskColor color = catalog.SolderMaskColors[i];
            string path = $"solderMaskColors[{i}]";
            if (color is null)
            {
                continue;
            }

            RequireText(color.Name, $"{path}.name", errors);
            if (color.HexColor is null || !HexColorPattern().IsMatch(color.HexColor))
            {
                errors.Add($"{path}.hexColor must be in #RRGGBB format.");
            }
        }
    }

    private static void ValidateMaterials(PcbCatalog catalog, List<string> errors)
    {
        ValidateIds(catalog.Materials, m => m?.Id, "materials", errors);
        for (int i = 0; i < catalog.Materials.Count; i++)
        {
            PcbMaterial material = catalog.Materials[i];
            string path = $"materials[{i}]";
            if (material is null)
            {
                continue;
            }

            RequireText(material.Name, $"{path}.name", errors);
            if (material.PriceMultiplier <= 0)
            {
                errors.Add($"{path}.priceMultiplier must be greater than zero.");
            }

            if (material.ProductionDays <= 0)
            {
                errors.Add($"{path}.productionDays must be greater than zero.");
            }
        }
    }

    private static bool HasEntries<T>(IReadOnlyList<T>? items, string path, List<string> errors)
    {
        if (items is null || items.Count == 0)
        {
            errors.Add($"{path} must contain at least one entry.");
            return false;
        }

        return true;
    }

    private static void ValidateIds<T>(IReadOnlyList<T> items, Func<T, string?> getId, string path, List<string> errors)
    {
        HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] is null)
            {
                errors.Add($"{path}[{i}] is missing.");
                continue;
            }

            string? id = getId(items[i]);
            if (string.IsNullOrWhiteSpace(id))
            {
                errors.Add($"{path}[{i}].id is required.");
            }
            else if (!seen.Add(id))
            {
                errors.Add($"{path}[{i}].id '{id}' is duplicated.");
            }
        }
    }

    private static void RequirePositive(decimal value, string path, List<string> errors)
    {
        if (value <= 0)
        {
            errors.Add($"{path} must be greater than zero.");
        }
    }

    private static void RequireOption(CatalogOption? option, string path, List<string> errors)
    {
        if (option is null)
        {
            errors.Add($"{path} is required.");
            return;
        }

        RequireText(option.Id, $"{path}.id", errors);
        RequireText(option.Name, $"{path}.name", errors);
    }

    private static void RequireText(string? value, string path, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add($"{path} is required.");
        }
    }

    [GeneratedRegex("^#[0-9A-Fa-f]{6}$")]
    private static partial Regex HexColorPattern();
}
