using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Altium.PcbManufacturing.Localization;

namespace Altium.PcbManufacturing.Tests;

/// <summary>
/// Keeps the English and Russian resources, the generated <see cref="Strings"/> class and the XAML in sync.
/// </summary>
public sealed partial class LocalizationResourceTests
{
    private static readonly Dictionary<string, string> english = ReadResx("Strings.resx");
    private static readonly Dictionary<string, string> russian = ReadResx("Strings.ru.resx");

    [Fact]
    public void Russian_TranslatesExactlyTheEnglishKeys() =>
        Assert.Equal(english.Keys.Order(), russian.Keys.Order());

    [Fact]
    public void Resources_HaveNoEmptyValues()
    {
        Assert.DoesNotContain(english, entry => string.IsNullOrWhiteSpace(entry.Value));
        Assert.DoesNotContain(russian, entry => string.IsNullOrWhiteSpace(entry.Value));
    }

    [Fact]
    public void Resources_HaveNoAccessKeyMarkers()
    {
        // The UI uses no access keys; an underscore would otherwise be swallowed by Label/AccessText rendering.
        Assert.DoesNotContain(english, entry => entry.Value.Contains('_'));
        Assert.DoesNotContain(russian, entry => entry.Value.Contains('_'));
    }

    [Fact]
    public void Russian_KeepsThePlaceholdersOfEachEnglishString()
    {
        foreach ((string key, string englishText) in english)
        {
            Assert.True(
                Placeholders(englishText).SetEquals(Placeholders(russian[key])),
                $"'{key}' placeholders differ: '{englishText}' vs '{russian[key]}'.");
        }
    }

    [Fact]
    public void GeneratedClass_ExposesExactlyTheResourceKeys()
    {
        IEnumerable<string> properties = typeof(Strings)
            .GetProperties(BindingFlags.Public | BindingFlags.Static)
            .Where(property => property.PropertyType == typeof(string))
            .Select(property => property.Name);

        Assert.Equal(english.Keys.Order(), properties.Order());
    }

    [Fact]
    public void Xaml_UsesOnlyExistingKeys()
    {
        foreach (string file in SourceFiles.Xaml())
        {
            foreach (Match match in XamlKeyUsage().Matches(File.ReadAllText(file)))
            {
                string key = match.Groups["key"].Value;
                Assert.True(english.ContainsKey(key), $"{Path.GetFileName(file)} uses missing resource key '{key}'.");
            }
        }
    }

    [Fact]
    public void ShippedSolderMaskColors_AreTranslated()
    {
        using var catalog = JsonDocument.Parse(File.ReadAllText(SourceFiles.Path("Data", "pcb-catalog.json")));
        foreach (JsonElement color in catalog.RootElement.GetProperty("solderMaskColors").EnumerateArray())
        {
            string key = $"SolderMaskColor_{color.GetProperty("id").GetString()}";
            Assert.True(english.ContainsKey(key) && russian.ContainsKey(key), $"Missing translation '{key}'.");
        }
    }

    [Fact]
    public void Russian_DoesNotTranslateProductIdentifiersOrPostcodeFormat()
    {
        Assert.Contains("NN-NNN", russian[nameof(Strings.Validation_PostcodeFormat)]);
        Assert.Contains("55-220", russian[nameof(Strings.Preferences_PostcodeHint)]);
    }

    private static Dictionary<string, string> ReadResx(string fileName) =>
        XDocument.Load(SourceFiles.Path("Localization", fileName))
            .Root!.Elements("data")
            .ToDictionary(data => (string)data.Attribute("name")!, data => (string)data.Element("value")!);

    private static HashSet<string> Placeholders(string text) =>
        [.. FormatPlaceholder().Matches(text).Select(match => match.Value)];

    // {0}, {1:0.0#} … in composite format strings.
    [GeneratedRegex(@"\{\d+(:[^}]*)?\}")]
    private static partial Regex FormatPlaceholder();

    // {loc:Text Key}, {loc:Format Key, …}; CatalogName takes a key prefix and is checked against the catalog instead.
    [GeneratedRegex(@"\{loc:(Text|Format)\s+(?<key>\w+)")]
    private static partial Regex XamlKeyUsage();
}
