using System.Windows;
using Altium.PcbManufacturing.ViewModels;

namespace Altium.PcbManufacturing.Views;

public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        InitializeComponent();
        DataContext = viewModel;
    }
}
