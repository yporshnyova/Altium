namespace Altium.PcbManufacturing.Models;

/// <summary>
/// The immutable set of predefined manufacturing options. The first entry of each option list is the default selection.
/// </summary>
/// <param name="BasePriceUsd">Fixed base manufacturing price of the sample PCB order, in US dollars.</param>
/// <param name="BoardSpecification">The fixed parameters of the sample order shown on the Quote screen.</param>
public sealed record PcbCatalog(
    decimal BasePriceUsd,
    IReadOnlyList<PcbMaterial> Materials,
    IReadOnlyList<SolderMaskColor> SolderMaskColors,
    IReadOnlyList<BoardThickness> BoardThicknesses,
    BoardSpecification BoardSpecification);
