using System.Windows;
using Altium.PcbManufacturing.Views.Dialogs;

namespace Altium.PcbManufacturing.Services;

/// <summary>
/// Shows confirmations as a themed, localized modal dialog over the active window.
/// </summary>
public sealed class DialogConfirmationService : IConfirmationService
{
    public bool Confirm(string title, string message)
    {
        ConfirmationDialog dialog = new(title, message)
        {
            Owner = Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive) ?? Application.Current?.MainWindow,
        };

        return dialog.ShowDialog() == true;
    }
}
