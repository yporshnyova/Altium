using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using Altium.PcbManufacturing.ViewModels;
using Altium.PcbManufacturing.Views.Converters;
using Altium.PcbManufacturing.Views.Markup;
using WpfValidation = System.Windows.Controls.Validation;

namespace Altium.PcbManufacturing.Tests;

/// <summary>
/// Live WPF bindings with the paths the views use: text boxes bound to the Preferences step's raw input show its
/// field-level errors and keep invalid text, while display elements bound to nested <c>Draft.…</c> paths follow the
/// draft's calculated values.
/// </summary>
/// <remarks>Not shown on screen: covers binding and validation logic, not rendering.</remarks>
public sealed class DraftBindingTests
{
    [Fact]
    public void QuantityBox_ShowsTheStepsErrorAndKeepsInvalidText_WhileTheBoundTotalKeepsTheLastValidPrice() => Sta.Run(() =>
    {
        TestWizard wizard = new();
        TextBox quantity = InputBox(wizard.Preferences, nameof(PreferencesViewModel.QuantityInput));
        TextBlock total = Display(wizard.Preferences, "Draft.TotalPrice", UsdConverter());

        quantity.Text = "3";
        Assert.Equal("$447.00", total.Text);
        Assert.False(WpfValidation.GetHasError(quantity));

        quantity.Text = "abc";

        Assert.True(WpfValidation.GetHasError(quantity));
        Assert.Equal("Enter a whole number of boards, 1 or more.", WpfValidation.GetErrors(quantity).Single().ErrorContent.ToString());
        Assert.Equal("abc", quantity.Text);
        Assert.Equal("$447.00", total.Text);
    });

    [Fact]
    public void ErrorShownOnSubmit_AppearsOnTheUntouchedPostcodeBox() => Sta.Run(() =>
    {
        TestWizard wizard = new();
        TextBox postcode = InputBox(wizard.Preferences, nameof(PreferencesViewModel.PostcodeInput));
        Assert.False(WpfValidation.GetHasError(postcode));

        wizard.Wizard.SubmitPreferences();

        Assert.True(WpfValidation.GetHasError(postcode));
    });

    [Fact]
    public void MaterialSelector_EditsTheDraftAndNestedDisplaysFollow() => Sta.Run(() =>
    {
        TestWizard wizard = new();
        ComboBox material = new() { ItemsSource = wizard.Preferences.Materials };
        material.SetBinding(Selector.SelectedItemProperty, new Binding("Draft.SelectedMaterial") { Source = wizard.Preferences, Mode = BindingMode.TwoWay });
        TextBlock unitPrice = Display(wizard.Quote, "Draft.PriceEstimate.EstimatedUnitPrice", UsdConverter());
        TextBlock productionDays = new();
        productionDays.SetBinding(TextBlock.TextProperty, LocalizedFormat("Days_Format", "Draft.ProductionDays", wizard.Order));
        TextBlock surcharge = new();
        surcharge.SetBinding(TextBlock.TextProperty, LocalizedFormat("Pricing_SurchargePerBoard", "Draft.PriceEstimate.MaterialSurchargePerUnit", wizard.Preferences, UsdConverter()));
        Assert.Equal("$149.00", unitPrice.Text);

        material.SelectedIndex = 1;

        Assert.Same(wizard.Catalog.Materials[1], wizard.Draft.SelectedMaterial);
        Assert.Equal("$372.50", unitPrice.Text);
        Assert.Equal("12 days", productionDays.Text);
        Assert.Equal("+$223.50 / board", surcharge.Text);
    });

    private static TextBox InputBox(object source, string path)
    {
        TextBox textBox = new();
        // As in PreferencesView.xaml.
        textBox.SetBinding(TextBox.TextProperty, new Binding(path)
        {
            Source = source,
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
            ValidatesOnNotifyDataErrors = true,
        });
        return textBox;
    }

    private static TextBlock Display(object source, string path, IValueConverter converter)
    {
        TextBlock textBlock = new();
        textBlock.SetBinding(TextBlock.TextProperty, new Binding(path) { Source = source, Mode = BindingMode.OneWay, Converter = converter });
        return textBlock;
    }

    // {loc:Format Key, Path=…, Converter=…} with an explicit source instead of the inherited data context.
    private static MultiBinding LocalizedFormat(string key, string path, object source, IValueConverter? converter = null)
    {
        var binding = (MultiBinding)new FormatExtension(key) { Path = path, Converter = converter }.ProvideValue(null!);
        ((Binding)binding.Bindings[0]).Source = source;
        return binding;
    }

    private static IValueConverter UsdConverter() => (IValueConverter)new UsdConverter().ProvideValue(null!);
}
