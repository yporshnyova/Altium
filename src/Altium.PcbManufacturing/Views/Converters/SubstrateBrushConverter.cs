using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;
using Altium.PcbManufacturing.Models;

namespace Altium.PcbManufacturing.Views.Converters;

/// <summary>
/// Converts a <see cref="PcbMaterial"/> to the fill of the board core in the preview's side view:
/// <c>Background="{Binding SelectedMaterial, Converter={converters:SubstrateBrushConverter}}"</c>.
/// </summary>

public sealed class SubstrateBrushConverter : MarkupExtension, IValueConverter
{
    private static readonly SubstrateBrushConverter instance = new();

    private static readonly Dictionary<string, Brush> brushesByMaterialId = new(StringComparer.Ordinal)
    {
        ["fr4-standard"] = CreateBrush(0xCF, 0xA9, 0x5E),
        ["fr4-high-tg"] = CreateBrush(0xA9, 0x7A, 0x3B),
        ["rogers-4350b"] = CreateBrush(0xD9, 0xD3, 0xC4),
    };

    /// <summary>
    /// Fill used for a material without a visual mapping.
    /// </summary>
    public static Brush FallbackBrush { get; } = CreateBrush(0x8F, 0x93, 0x99);

    public override object ProvideValue(IServiceProvider serviceProvider) => instance;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is PcbMaterial material && brushesByMaterialId.TryGetValue(material.Id, out Brush? brush) ? brush : FallbackBrush;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();

    private static SolidColorBrush CreateBrush(byte red, byte green, byte blue)
    {
        var brush = new SolidColorBrush(Color.FromRgb(red, green, blue));
        brush.Freeze();
        return brush;
    }
}
