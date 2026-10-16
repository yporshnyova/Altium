namespace Altium.PcbManufacturing.Models;

/// <summary>
/// A predefined finished board thickness.
/// </summary>
/// <param name="Id">Stable catalog identifier.</param>
/// <param name="Millimeters">Finished board thickness in millimeters.</param>
public sealed record BoardThickness(string Id, decimal Millimeters);
