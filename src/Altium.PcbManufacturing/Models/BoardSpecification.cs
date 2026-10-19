namespace Altium.PcbManufacturing.Models;

/// <summary>
/// A named predefined option identified by a stable id, e.g. a surface finish. The id is also the localization key.
/// </summary>
/// <param name="Id">Stable catalog identifier.</param>
/// <param name="Name">Name shown when no translation exists for <paramref name="Id"/>.</param>
public sealed record CatalogOption(string Id, string Name);

/// <summary>
/// The fixed, predefined manufacturing parameters of the sample order shown on the Quote screen.
/// </summary>
/// <param name="WidthMm">Board width in millimetres.</param>
/// <param name="HeightMm">Board height in millimetres.</param>
/// <param name="LayerCount">Number of copper layers.</param>
/// <param name="CopperWeightOz">Copper weight in ounces per square foot.</param>
/// <param name="SurfaceFinish">Surface finish of exposed copper.</param>
/// <param name="SilkscreenColor">Colour of the printed markings.</param>
public sealed record BoardSpecification(
    decimal WidthMm,
    decimal HeightMm,
    int LayerCount,
    decimal CopperWeightOz,
    CatalogOption SurfaceFinish,
    CatalogOption SilkscreenColor);
