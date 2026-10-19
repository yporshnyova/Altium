using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Altium.PcbManufacturing.Localization;

namespace Altium.PcbManufacturing.Tests;

/// <summary>
/// Static checks over the source resource files, for mistakes that compile but fail silently at runtime: a missing
/// translation or placeholder, an unknown key in XAML, or a theme brush that does not follow theme changes.
/// </summary>
public sealed partial class ResourceFileTests
{
    private static readonly XNamespace xamlNamespace = "http://schemas.microsoft.com/winfx/2006/xaml";
    private static readonly string projectDirectory = FindProjectDirectory();
    private static readonly Dictionary<string, string> english = ReadResx("Strings.resx");
    private static readonly Dictionary<string, string> russian = ReadResx("Strings.ru.resx");

    // Localization

    [Fact]
    public void Russian_TranslatesExactlyTheEnglishKeysWithTheSamePlaceholders()
    {
        Assert.Equal(english.Keys.Order(), russian.Keys.Order());
        foreach ((string key, string englishText) in english)
        {
            Assert.True(
                Placeholders(englishText).SetEquals(Placeholders(russian[key])),
                $"'{key}' placeholders differ: '{englishText}' vs '{russian[key]}'.");
        }
    }

    [Fact]
    public void Xaml_UsesOnlyExistingKeys()
    {
        foreach (string file in XamlFiles())
        {
            foreach (Match match in XamlKeyUsage().Matches(File.ReadAllText(file)))
            {
                Assert.True(english.ContainsKey(match.Groups["key"].Value), $"{Path.GetFileName(file)} uses missing key '{match.Groups["key"].Value}'.");
            }
        }
    }

    [Fact]
    public void ShippedCatalogNames_AreTranslated()
    {
        using var catalog = JsonDocument.Parse(File.ReadAllText(SourcePath("Data", "pcb-catalog.json")));
        JsonElement specification = catalog.RootElement.GetProperty("boardSpecification");
        List<string> keys =
        [
            .. catalog.RootElement.GetProperty("solderMaskColors").EnumerateArray()
                .Select(color => Localizer.CatalogKey("SolderMaskColor", color.GetProperty("id").GetString()!)),
            Localizer.CatalogKey("SurfaceFinish", specification.GetProperty("surfaceFinish").GetProperty("id").GetString()!),
            Localizer.CatalogKey("SilkscreenColor", specification.GetProperty("silkscreenColor").GetProperty("id").GetString()!),
        ];

        Assert.All(keys, key => Assert.True(english.ContainsKey(key) && russian.ContainsKey(key), $"Missing translation '{key}'."));
    }

    // Themes

    [Fact]
    public void LightAndDark_DefineTheSameBrushes() =>
        Assert.Equal(PaletteKeys("Light.xaml").Order(), PaletteKeys("Dark.xaml").Order());

    [Fact]
    public void Xaml_UsesThemeBrushesOnlyAsDefinedDynamicResources()
    {
        HashSet<string> palette = PaletteKeys("Light.xaml");
        // Per-instance resources such as the unavailable-step tooltip are dynamic too and live in AppStyles.xaml.
        HashSet<string> dynamicKeys = [.. palette];
        dynamicKeys.UnionWith(XDocument.Load(SourcePath("Resources", "Styles", "AppStyles.xaml"))
            .Root!.Elements().Select(element => (string?)element.Attribute(xamlNamespace + "Key")).OfType<string>());

        foreach (string file in XamlFiles())
        {
            string xaml = File.ReadAllText(file);
            foreach (Match match in DynamicResourceUsage().Matches(xaml))
            {
                Assert.True(dynamicKeys.Contains(match.Groups["key"].Value), $"{Path.GetFileName(file)} uses undefined dynamic resource '{match.Groups["key"].Value}'.");
            }

            foreach (Match match in StaticResourceUsage().Matches(xaml))
            {
                Assert.False(palette.Contains(match.Groups["key"].Value), $"{Path.GetFileName(file)} uses theme brush '{match.Groups["key"].Value}' statically; it would not follow theme changes.");
            }
        }
    }

    private static string FindProjectDirectory()
    {
        for (DirectoryInfo? directory = new(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Altium.sln")))
            {
                return Path.Combine(directory.FullName, "src", "Altium.PcbManufacturing");
            }
        }

        throw new InvalidOperationException("Altium.sln was not found above the test output folder.");
    }

    private static string SourcePath(params string[] relativeParts) => Path.Combine([projectDirectory, .. relativeParts]);

    private static IEnumerable<string> XamlFiles() =>
        Directory.EnumerateFiles(projectDirectory, "*.xaml", SearchOption.AllDirectories)
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                && !file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"));

    private static Dictionary<string, string> ReadResx(string fileName) =>
        XDocument.Load(SourcePath("Localization", fileName))
            .Root!.Elements("data")
            .ToDictionary(data => (string)data.Attribute("name")!, data => (string)data.Element("value")!);

    private static HashSet<string> PaletteKeys(string fileName) =>
        [.. XDocument.Load(SourcePath("Resources", "Themes", fileName)).Root!.Elements().Select(element => (string)element.Attribute(xamlNamespace + "Key")!)];

    private static HashSet<string> Placeholders(string text) => [.. FormatPlaceholder().Matches(text).Select(match => match.Value)];

    // {0}, {1:0.0#} … in composite format strings.
    [GeneratedRegex(@"\{\d+(:[^}]*)?\}")]
    private static partial Regex FormatPlaceholder();

    // {loc:Text Key}, {loc:Format Key, …}; CatalogName takes a key prefix and is checked against the catalog instead.
    [GeneratedRegex(@"\{loc:(Text|Format)\s+(?<key>\w+)")]
    private static partial Regex XamlKeyUsage();

    [GeneratedRegex(@"\{DynamicResource\s+(?<key>\w+)\}")]
    private static partial Regex DynamicResourceUsage();

    [GeneratedRegex(@"\{StaticResource\s+(?<key>\w+)\}")]
    private static partial Regex StaticResourceUsage();
}
