using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using Altium.PcbManufacturing.Formatting;

namespace Altium.PcbManufacturing.Views.Converters;

/// <summary>
/// Formats a US dollar amount with <see cref="UsdFormat"/>, whatever the UI language:
/// <c>Text="{Binding Draft.TotalPrice, Converter={converters:UsdConverter}}"</c>.
/// </summary>
public sealed class UsdConverter : MarkupExtension, IValueConverter
{
    private static readonly UsdConverter instance = new();

    public override object ProvideValue(IServiceProvider serviceProvider) => instance;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is decimal amount ? UsdFormat.Format(amount) : DependencyProperty.UnsetValue;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
