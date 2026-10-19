using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using Altium.PcbManufacturing.Models;
using Altium.PcbManufacturing.Views.Graphics;

namespace Altium.PcbManufacturing.Views.Converters;

/// <summary>
/// Converts a <see cref="BoardSpecification"/> to its top-view <see cref="PcbArtwork"/>:
/// <c>Tag="{Binding Specification, Converter={converters:PcbArtworkConverter}}"</c>.
/// </summary>
public sealed class PcbArtworkConverter : MarkupExtension, IValueConverter
{
    private static readonly PcbArtworkConverter instance = new();

    public override object ProvideValue(IServiceProvider serviceProvider) => instance;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is BoardSpecification specification
            ? PcbArtworkBuilder.Build((double)specification.WidthMm, (double)specification.HeightMm)
            : DependencyProperty.UnsetValue;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
