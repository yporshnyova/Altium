using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Altium.PcbManufacturing.Tests;

/// <summary>
/// Checks that both theme palettes are complete; this does not verify how the themes render.
/// </summary>
public sealed partial class ThemeResourceTests
{
    private static readonly XNamespace xamlNamespace = "http://schemas.microsoft.com/winfx/2006/xaml";

    [Fact]
    public void LightAndDark_DefineTheSameKeys() =>
        Assert.Equal(PaletteKeys("Light.xaml").Order(), PaletteKeys("Dark.xaml").Order());

    [Fact]
    public void Palettes_ContainOnlyFrozenSolidColorBrushes()
    {
        foreach (string theme in new[] { "Light.xaml", "Dark.xaml" })
        {
            foreach (XElement resource in Palette(theme).Root!.Elements())
            {
                Assert.Equal("SolidColorBrush", resource.Name.LocalName);
                Assert.Equal("True", (string?)resource.Attributes().SingleOrDefault(a => a.Name.LocalName == "Freeze"));
            }
        }
    }

    [Fact]
    public void Xaml_DynamicResources_AreDefinedByThePalette()
    {
        HashSet<string> keys = PaletteKeys("Light.xaml");
        foreach (string file in SourceFiles.Xaml())
        {
            foreach (Match match in DynamicResourceUsage().Matches(File.ReadAllText(file)))
            {
                string key = match.Groups["key"].Value;
                Assert.True(keys.Contains(key), $"{Path.GetFileName(file)} uses undefined theme resource '{key}'.");
            }
        }
    }

    [Fact]
    public void Xaml_DoesNotReferencePaletteBrushesStatically()
    {
        HashSet<string> keys = PaletteKeys("Light.xaml");
        foreach (string file in SourceFiles.Xaml())
        {
            foreach (Match match in StaticResourceUsage().Matches(File.ReadAllText(file)))
            {
                Assert.False(
                    keys.Contains(match.Groups["key"].Value),
                    $"{Path.GetFileName(file)} uses StaticResource for theme brush '{match.Groups["key"].Value}'; it would not follow theme changes.");
            }
        }
    }

    private static XDocument Palette(string fileName) => XDocument.Load(SourceFiles.Path("Resources", "Themes", fileName));

    private static HashSet<string> PaletteKeys(string fileName) =>
        [.. Palette(fileName).Root!.Elements().Select(element => (string)element.Attribute(xamlNamespace + "Key")!)];

    [GeneratedRegex(@"\{DynamicResource\s+(?<key>\w+)\}")]
    private static partial Regex DynamicResourceUsage();

    [GeneratedRegex(@"\{StaticResource\s+(?<key>\w+)\}")]
    private static partial Regex StaticResourceUsage();
}
