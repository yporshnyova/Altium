using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace Altium.PcbManufacturing.Settings;

/// <summary>
/// Reads and writes <see cref="UiSettings"/> as a small JSON file, e.g. <c>{ "language": "Russian", "theme": "Dark" }</c>.
/// </summary>

public sealed class UiSettingsStore
{
    public UiSettingsStore(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        FilePath = filePath;
    }

    /// <summary>
    /// <c>%LOCALAPPDATA%\Altium\PcbManufacturing\ui-settings.json</c>: per user and per machine.
    /// </summary>
    public static string DefaultFilePath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Altium", "PcbManufacturing", "ui-settings.json");

    public string FilePath { get; }

    public UiSettings Load()
    {
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(FilePath));
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return UiSettings.Default;
            }

            return new UiSettings(
                ReadEnum(document.RootElement, "language", UiSettings.Default.Language),
                ReadEnum(document.RootElement, "theme", UiSettings.Default.Theme));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return UiSettings.Default;
        }
    }

    public void Save(UiSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(
                FilePath,
                JsonSerializer.Serialize(new { language = settings.Language.ToString(), theme = settings.Theme.ToString() }));
        }
        catch (IOException ex)
        {
            Trace.TraceWarning(
                "Failed to save UI settings to '{0}'. {1}",
                FilePath,
                ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            Trace.TraceWarning(
                "Access denied while saving UI settings to '{0}'. {1}",
                FilePath,
                ex);
        }
    }

    private static TEnum ReadEnum<TEnum>(JsonElement root, string name, TEnum fallback)
        where TEnum : struct, Enum
    {
        return root.TryGetProperty(name, out JsonElement value)
            && value.ValueKind == JsonValueKind.String
            && Enum.GetNames<TEnum>().FirstOrDefault(n => n.Equals(value.GetString(), StringComparison.OrdinalIgnoreCase)) is { } match
            ? Enum.Parse<TEnum>(match)
            : fallback;
    }
}
