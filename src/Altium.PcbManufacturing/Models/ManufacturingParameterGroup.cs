namespace Altium.PcbManufacturing.Models;

/// <summary>
/// A named group of fixed manufacturing parameters, such as Dimensions or Layers.
/// </summary>
public sealed record ManufacturingParameterGroup(string Name, IReadOnlyList<ManufacturingParameter> Parameters);

/// <summary>
/// A single fixed manufacturing parameter with its predefined value.
/// </summary>
public sealed record ManufacturingParameter(string Name, string Value);
