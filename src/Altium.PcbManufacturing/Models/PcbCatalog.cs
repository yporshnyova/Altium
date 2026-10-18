namespace Altium.PcbManufacturing.Models;

/// <summary>
/// The immutable set of predefined manufacturing options. The first entry of each option list is the default selection.
/// </summary>

public sealed record PcbCatalog(
    decimal BasePriceUsd,
    IReadOnlyList<PcbMaterial> Materials,
    IReadOnlyList<SolderMaskColor> SolderMaskColors,
    IReadOnlyList<BoardThickness> BoardThicknesses,
    IReadOnlyList<ManufacturingParameterGroup> ParameterGroups);
