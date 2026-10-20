using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Altium.PcbManufacturing.Services;
using Altium.PcbManufacturing.Settings;
using Altium.PcbManufacturing.ViewModels;
using Altium.PcbManufacturing.Views;
using Altium.PcbManufacturing.Views.Dialogs;

namespace Altium.PcbManufacturing.Tests;

/// <summary>
/// Tests that create the WPF <see cref="Application"/>. There is one per process and it outlives the test, so these
/// tests run alone, after all parallel tests, and nothing else in the process may create one.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class WpfApplicationCollection
{
    public const string Name = "WPF application";
}

/// <summary>
/// The real application resources (App.xaml) with every view and its own Views/*.Styles.xaml: a missing static
/// resource, a broken BasedOn chain or a template that fails to apply only shows up when the views are loaded and laid
/// out, which a build does not do.
/// </summary>
/// <remarks>Not shown on screen: covers resource resolution and template application, not rendering.</remarks>
[Collection(WpfApplicationCollection.Name)]
public sealed class ViewResourceTests
{
    private static readonly Size windowSize = new(1024, 800);

    [Fact]
    public void Views_LoadAndApplyTheirStylesAndTemplates_InBothThemes() => Sta.Run(() =>
    {
        App app = new();
        app.InitializeComponent();
        ThemeService themes = new(app.Resources);
        TestWizard wizard = new();
        string settingsPath = Path.Combine(Path.GetTempPath(), $"ui-settings-{Guid.NewGuid():N}.json");
        MainWindowViewModel main = new(wizard.Wizard, wizard.Localizer, new FakeThemeService(), new UiSettingsStore(settingsPath));

        foreach (AppTheme theme in new[] { AppTheme.Dark, AppTheme.Light })
        {
            themes.Apply(theme);

            // A window that is never shown has no visual tree above its content, so the content is laid out on its own.
            MainWindow window = new(main);
            var windowContent = (FrameworkElement)window.Content;
            LayOut(windowContent);
            Button themeToggle = Single<Button>(windowContent, button => button.Style == window.FindResource("ThemeToggleButtonStyle"));
            Assert.IsAssignableFrom<FrameworkElement>(themeToggle.Content);
            // The selected Preferences tab shows its view through Mappings.xaml, above the wizard's footer, where
            // Reset to Defaults takes the place of Back.
            Assert.Single(Descendants<PreferencesView>(windowContent));
            Assert.Equal(Visibility.Collapsed, Single<Button>(windowContent, button => button.Style == window.FindResource("BackButtonStyle")).Visibility);
            Assert.Equal(Visibility.Visible, Single<Button>(windowContent, button => button.Style == window.FindResource("ResetToDefaultsButtonStyle")).Visibility);
            window.Close();

            PreferencesView preferences = new() { DataContext = wizard.Preferences };
            LayOut(preferences);
            TextBlock projectName = Single<TextBlock>(preferences, text => text.Style == preferences.FindResource("ProjectNameSummaryValueTextStyle"));
            Assert.False(string.IsNullOrEmpty(projectName.Text));

            QuoteView quote = new() { DataContext = wizard.Quote };
            LayOut(quote);
            List<Border> dividers = [.. Descendants<Border>(quote).Where(border => border.Name == "GroupDivider")];
            Assert.Equal(wizard.Quote.ParameterGroups.Count, dividers.Count);
            Assert.Single(dividers, divider => divider.Visibility == Visibility.Collapsed);
            TextBlock surcharge = Single<TextBlock>(quote, text => text.Style == quote.FindResource("MaterialSurchargeValueTextStyle"));
            Assert.False(string.IsNullOrEmpty(surcharge.Text));

            OrderView order = new() { DataContext = wizard.Order };
            LayOut(order);
            TextBlock notes = Single<TextBlock>(order, text => text.Style == order.FindResource("NotesValueTextStyle"));
            Assert.False(string.IsNullOrEmpty(notes.Text));

            ConfirmationDialog dialog = new("Title", "Message");
            LayOut((FrameworkElement)dialog.Content);
            dialog.Close();
        }

        File.Delete(settingsPath);
    });

    private static void LayOut(FrameworkElement element)
    {
        element.Measure(windowSize);
        element.Arrange(new Rect(windowSize));
        element.UpdateLayout();
    }

    private static T Single<T>(DependencyObject root, Func<T, bool> predicate)
        where T : DependencyObject =>
        Assert.Single(Descendants<T>(root), element => predicate(element));

    private static IEnumerable<T> Descendants<T>(DependencyObject root)
        where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(root, i);
            if (child is T match)
            {
                yield return match;
            }

            foreach (T descendant in Descendants<T>(child))
            {
                yield return descendant;
            }
        }
    }
}
