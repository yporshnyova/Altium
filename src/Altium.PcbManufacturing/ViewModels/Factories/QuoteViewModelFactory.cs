using Altium.PcbManufacturing.Localization;
using Altium.PcbManufacturing.Models;

namespace Altium.PcbManufacturing.ViewModels.Factories;

/// <summary>
/// Creates the Quote step for the shared <see cref="ManufacturingOrderDraft"/>.
/// </summary>
public sealed class QuoteViewModelFactory
{
    private readonly PcbCatalog catalog;
    private readonly Localizer localizer;

    public QuoteViewModelFactory(PcbCatalog catalog, Localizer localizer)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(localizer);

        this.catalog = catalog;
        this.localizer = localizer;
    }

    public QuoteViewModel Create(ManufacturingOrderDraft draft) => new(catalog, draft, localizer);
}
