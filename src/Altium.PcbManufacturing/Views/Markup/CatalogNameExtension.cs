using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using Altium.PcbManufacturing.Localization;

namespace Altium.PcbManufacturing.Views.Markup;

/// <summary>
/// The localized name of a catalog entry, looked up by <see cref="Localizer.CatalogKey"/> with the catalog name as fallback, so an
/// entry added to the catalog without a translation still shows its catalog name:
/// <c>Text="{loc:CatalogName SolderMaskColor, IdPath=Id, NamePath=Name}"</c>.
/// </summary>
[MarkupExtensionReturnType(typeof(object))]
public sealed class CatalogNameExtension : MarkupExtension
{
    public CatalogNameExtension()
    {
    }

    public CatalogNameExtension(string prefix)
    {
        Prefix = prefix;
    }

    [ConstructorArgument("prefix")]
    public string Prefix { get; set; } = string.Empty;

    public string IdPath { get; set; } = "Id";

    public string NamePath { get; set; } = "Name";

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        MultiBinding binding = new() { Converter = NameConverter.Instance, ConverterParameter = Prefix, Mode = BindingMode.OneWay };
        binding.Bindings.Add(new Binding(IdPath) { Mode = BindingMode.OneWay });
        binding.Bindings.Add(new Binding(NamePath) { Mode = BindingMode.OneWay });
        binding.Bindings.Add(FormatExtension.LanguageBinding());
        return binding.ProvideValue(serviceProvider);
    }

    private sealed class NameConverter : IMultiValueConverter
    {
        public static NameConverter Instance { get; } = new();

        public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values.Length < 2 || values[1] is not string fallback)
            {
                return DependencyProperty.UnsetValue;
            }

            return values[0] is string id && parameter is string prefix
                ? Localizer.Instance.Find(Localizer.CatalogKey(prefix, id)) ?? fallback
                : fallback;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
