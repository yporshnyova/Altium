using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace Altium.PcbManufacturing.Views.Converters;

/// <summary>
/// Multiplies a number by the converter parameter, e.g. a relative size by a length in device-independent pixels:
/// <c>Height="{Binding Fraction, Converter={converters:ScaleConverter}, ConverterParameter=24}"</c>.
/// </summary>

public sealed class ScaleConverter : MarkupExtension, IValueConverter
{
    private static readonly ScaleConverter instance = new();

    public override object ProvideValue(IServiceProvider serviceProvider) => instance;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null || value == DependencyProperty.UnsetValue)
        {
            return DependencyProperty.UnsetValue;
        }

        try
        {
            return System.Convert.ToDouble(value, CultureInfo.InvariantCulture)
                * System.Convert.ToDouble(parameter, CultureInfo.InvariantCulture);
        }
        catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException)
        {
            return DependencyProperty.UnsetValue;
        }
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
