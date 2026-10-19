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
