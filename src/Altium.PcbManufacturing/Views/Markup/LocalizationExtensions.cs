using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using Altium.PcbManufacturing.Localization;

namespace Altium.PcbManufacturing.Views.Markup;

/// <summary>
/// A localized string that follows language changes: <c>Text="{loc:Text Summary_Title}"</c>.
/// </summary>
[MarkupExtensionReturnType(typeof(object))]
public sealed class TextExtension : MarkupExtension
{
    public TextExtension()
    {
    }

    public TextExtension(string key)
    {
        Key = key;
    }

    [ConstructorArgument("key")]
    public string Key { get; set; } = string.Empty;

    // Binds to the Localizer indexer, which raises "Item[]" when the language changes.
    public override object ProvideValue(IServiceProvider serviceProvider) =>
        new Binding($"[{Key}]") { Source = Localizer.Instance, Mode = BindingMode.OneWay }.ProvideValue(serviceProvider);
}

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

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        MultiBinding binding = new() { Converter = FormatConverter.Instance, ConverterParameter = Key, Mode = BindingMode.OneWay };
        binding.Bindings.Add(new Binding(Path) { Mode = BindingMode.OneWay, ValidatesOnNotifyDataErrors = false });
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
