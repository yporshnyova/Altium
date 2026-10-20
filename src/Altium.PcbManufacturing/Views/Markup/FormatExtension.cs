using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using Altium.PcbManufacturing.Localization;

namespace Altium.PcbManufacturing.Views.Markup;

/// <summary>
/// A bound value placed into a localized format string: <c>Text="{loc:Format Thickness_Millimeters, Path=Millimeters}"</c>.
/// </summary>
/// <remarks>
/// The value is formatted with the UI language's culture (e.g. <c>1,6 мм</c>) and re-formatted when the language changes.
/// The value binding ignores validation errors: this is display-only text.
/// </remarks>
[MarkupExtensionReturnType(typeof(object))]
public sealed class FormatExtension : MarkupExtension
{
    public FormatExtension()
    {
    }

    public FormatExtension(string key)
    {
        Key = key;
    }

    [ConstructorArgument("key")]
    public string Key { get; set; } = string.Empty;

    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Optional converter applied to the bound value before it is placed into the format string, e.g. a US dollar amount:
    /// <c>{loc:Format Pricing_SurchargePerBoard, Path=Amount, Converter={converters:UsdConverter}}</c>.
    /// </summary>
    public IValueConverter? Converter { get; set; }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        MultiBinding binding = new() { Converter = FormatConverter.Instance, ConverterParameter = Key, Mode = BindingMode.OneWay };
        binding.Bindings.Add(new Binding(Path) { Mode = BindingMode.OneWay, ValidatesOnNotifyDataErrors = false, Converter = Converter });
        binding.Bindings.Add(LanguageBinding());
        return binding.ProvideValue(serviceProvider);
    }

    internal static Binding LanguageBinding() =>
        new(nameof(Localizer.Language)) { Source = Localizer.Instance, Mode = BindingMode.OneWay };

    private sealed class FormatConverter : IMultiValueConverter
    {
        public static FormatConverter Instance { get; } = new();

        public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture) =>
            values.Length == 0 || values[0] is null || values[0] == DependencyProperty.UnsetValue || parameter is not string key
                ? DependencyProperty.UnsetValue
                : Localizer.Instance.Format(key, values[0]);

        public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
