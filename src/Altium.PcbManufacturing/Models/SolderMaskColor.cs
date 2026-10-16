namespace Altium.PcbManufacturing.Models;

/// <summary>
/// A predefined solder mask color.
/// </summary>
/// <param name="Id">Stable catalog identifier.</param>
/// <param name="Name">Human-readable color name.</param>
/// <param name="HexColor">RGB color in <c>#RRGGBB</c> form, kept UI-framework neutral.</param>
public sealed record SolderMaskColor(string Id, string Name, string HexColor);
