namespace Altium.PcbManufacturing.Exceptions;

/// <summary>
/// Thrown when the predefined manufacturing catalog cannot be read, parsed or validated.
/// </summary>
public sealed class CatalogLoadException : Exception
{
    public CatalogLoadException(string message)
        : base(message)
    {
    }

    public CatalogLoadException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
