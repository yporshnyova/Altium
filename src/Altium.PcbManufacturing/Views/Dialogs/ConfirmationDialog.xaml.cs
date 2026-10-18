using System.Windows;

namespace Altium.PcbManufacturing.Views.Dialogs;

/// <summary>
/// Modal Yes/No confirmation shown by <see cref="Services.DialogConfirmationService"/>
/// </summary>
public partial class ConfirmationDialog : Window
{
    public ConfirmationDialog(string title, string message)
    {
        InitializeComponent();
        Title = title;
        MessageText.Text = message;
        Loaded += (_, _) => NoButton.Focus();
    }

    private void OnYesClick(object sender, RoutedEventArgs e) => DialogResult = true;
}
