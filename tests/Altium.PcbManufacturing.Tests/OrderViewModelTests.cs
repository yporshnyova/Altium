using Altium.PcbManufacturing.ViewModels;

namespace Altium.PcbManufacturing.Tests;

/// <summary>
/// The Order step on its own: the confirmation with the order summary. When it may be placed,
/// and what follows, is the wizard's (see <see cref="OrderWizardTests"/>).
/// </summary>
public sealed class OrderViewModelTests
{
    private readonly TestWizard wizard = new();

    private OrderViewModel ViewModel => wizard.Order;

    [Fact]
    public async Task PlaceOrder_Confirmed_AsksWithTheDraftSummaryAndReturnsTrue()
    {
        wizard.Draft.ProjectName = "Sensor board";
        wizard.Draft.Postcode = "55-220";
        wizard.Draft.Quantity = 4;
        wizard.Draft.SelectedMaterial = wizard.Catalog.Materials[1];
        wizard.Draft.SelectedSolderMaskColor = wizard.Catalog.SolderMaskColors[1];
        wizard.Draft.SelectedBoardThickness = wizard.Catalog.BoardThicknesses[1];

        Assert.True(await ViewModel.PlaceOrderAsync());

        (string title, string message) = Assert.Single(wizard.Confirmation.Requests);
        Assert.Equal("Place Order", title);
        Assert.Contains("Project name: Sensor board", message);
        Assert.Contains("Delivery postcode: 55-220", message);
        Assert.Contains("Quantity: 4 pcs.", message);
        Assert.Contains("Material: Rogers", message);
        Assert.Contains("Solder mask: Black", message);
        Assert.Contains("Board thickness: 0.8 mm", message);
        Assert.Contains("Estimated production time: 12 days", message);
        Assert.Contains("Estimated total: $1,490.00", message);
    }

}
