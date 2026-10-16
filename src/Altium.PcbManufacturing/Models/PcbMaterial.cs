namespace Altium.PcbManufacturing.Models;

/// <summary>
/// A predefined PCB base material.
/// </summary>
/// <param name="Id">Stable catalog identifier.</param>
/// <param name="Name">Human-readable material name.</param>
/// <param name="PriceMultiplier">Dimensionless factor applied to the base price; 1.0 means no change.</param>
/// <param name="ProductionDays">Production lead time for this material, in days.</param>
public sealed record PcbMaterial(string Id, string Name, decimal PriceMultiplier, int ProductionDays);
