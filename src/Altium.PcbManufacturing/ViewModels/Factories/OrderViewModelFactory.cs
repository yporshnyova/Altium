using Altium.PcbManufacturing.Localization;
using Altium.PcbManufacturing.Models;
using Altium.PcbManufacturing.Services;

namespace Altium.PcbManufacturing.ViewModels.Factories;

/// <summary>
/// Creates the Order step for the shared <see cref="ManufacturingOrderDraft"/>.
/// </summary>
public sealed class OrderViewModelFactory
{
    private readonly PcbCatalog catalog;
    private readonly IConfirmationService confirmation;
    private readonly Localizer localizer;

    public OrderViewModelFactory(
        PcbCatalog catalog, IConfirmationService confirmation, Localizer localizer)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(confirmation);
        ArgumentNullException.ThrowIfNull(localizer);

        this.catalog = catalog;
        this.confirmation = confirmation;
        this.localizer = localizer;
    }

    public OrderViewModel Create(ManufacturingOrderDraft draft) => new(catalog, draft, confirmation, localizer);
}
