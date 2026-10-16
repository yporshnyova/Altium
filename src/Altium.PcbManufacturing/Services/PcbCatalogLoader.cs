using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Altium.PcbManufacturing.Models;

namespace Altium.PcbManufacturing.Services;

/// <summary>
/// Loads and validates the predefined manufacturing catalog from a local JSON file.
/// </summary>
public sealed class PcbCatalogLoader
{
    private static readonly JsonSerializerOptions serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        ReadCommentHandling = JsonCommentHandling.Skip,
    };

    private readonly string catalogPath;

    public PcbCatalogLoader(string catalogPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(catalogPath);
        this.catalogPath = catalogPath;
    }

    /// <summary>
    /// The catalog shipped next to the application binaries, independent of the working directory.
    /// </summary>
    public static string DefaultCatalogPath { get; } = Path.Combine(AppContext.BaseDirectory, "Data", "pcb-catalog.json");

    /// <exception cref="CatalogLoadException">The file is missing, unreadable, malformed or contains invalid entries.</exception>
    public async Task<PcbCatalog> LoadAsync(CancellationToken cancellationToken = default)
    {
        PcbCatalog? catalog;
        try
        {
            await using FileStream stream = new(
                catalogPath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);
            catalog = await JsonSerializer.DeserializeAsync<PcbCatalog>(stream, serializerOptions, cancellationToken);
        }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
        {
            throw new CatalogLoadException($"Catalog file '{catalogPath}' was not found.", ex);
        }
        catch (JsonException ex)
        {
            throw new CatalogLoadException($"Catalog file '{catalogPath}' is not valid JSON: {ex.Message}", ex);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new CatalogLoadException($"Catalog file '{catalogPath}' could not be read: {ex.Message}", ex);
        }

        if (catalog is null)
        {
            throw new CatalogLoadException($"Catalog file '{catalogPath}' does not contain a catalog.");
        }

        IReadOnlyList<string> errors = PcbCatalogValidator.Validate(catalog);
        if (errors.Count > 0)
        {
            throw new CatalogLoadException(
                $"Catalog file '{catalogPath}' is invalid:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}");
        }

        return catalog;
    }
}
