using System.ComponentModel;
using System.Text;
using Altium.PcbManufacturing.Formatting;
using Altium.PcbManufacturing.Localization;
using Altium.PcbManufacturing.Models;
using Altium.PcbManufacturing.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Altium.PcbManufacturing.ViewModels;

/// <summary>
/// Order step: a final review of <see cref="Draft"/> and the order confirmation.
/// </summary>
/// <remarks>
/// There is no order backend. Nothing is sent or stored. Whether the order may be placed (valid input, available step)
/// is decided by the wizard before it calls <see cref="PlaceOrderAsync"/>.
/// </remarks>
public sealed class OrderViewModel : ObservableObject
{
    private readonly BoardSpecification specification;
    private readonly IConfirmationService confirmation;
    private readonly Localizer localizer;

    public OrderViewModel(
        PcbCatalog catalog,
        ManufacturingOrderDraft draft,
        IConfirmationService confirmation,
        Localizer localizer)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(draft);
        ArgumentNullException.ThrowIfNull(confirmation);
        ArgumentNullException.ThrowIfNull(localizer);

        specification = catalog.BoardSpecification;
        Draft = draft;
        this.confirmation = confirmation;
        this.localizer = localizer;
        this.localizer.PropertyChanged += OnLocalizerPropertyChanged;
    }

    public ManufacturingOrderDraft Draft { get; }

    /// <summary>
    /// The fixed board size and layer count, e.g. <c>100 × 80 mm · 2 layers</c>.
    /// </summary>
    public string BoardSpecificationText =>
        $"{localizer.Format(nameof(Strings.Quote_SizeValue), specification.WidthMm, specification.HeightMm)} · {localizer.Format("Layers_Format", specification.LayerCount)}";

    /// <summary>
    /// Asks the user to confirm the summarized order. The wizard owns completion and prevents repeat submission.
    /// </summary>
    /// <returns>Whether the user confirmed the order.</returns>
    public Task<bool> PlaceOrderAsync() =>
        confirmation.ConfirmAsync(localizer[nameof(Strings.Order_ConfirmTitle)], CreateConfirmationMessage());

    private string CreateConfirmationMessage()
    {
        SolderMaskColor solderMask = Draft.SelectedSolderMaskColor;
        StringBuilder message = new();
        AppendLine(nameof(Strings.Summary_ProjectName), Draft.ProjectName);
        AppendLine(nameof(Strings.Order_DeliveryPostcode), Draft.Postcode);
        AppendLine(nameof(Strings.Summary_Quantity), localizer.Format("Boards_Format", Draft.Quantity));
        AppendLine(nameof(Strings.Summary_Material), Draft.SelectedMaterial.Name);
        AppendLine(nameof(Strings.Summary_SolderMask), localizer.Find(Localizer.CatalogKey("SolderMaskColor", solderMask.Id)) ?? solderMask.Name);
        AppendLine(nameof(Strings.Summary_BoardThickness), localizer.Format(nameof(Strings.Thickness_Millimeters), Draft.SelectedBoardThickness.Millimeters));
        AppendLine(nameof(Strings.Summary_EstimatedProductionTime), localizer.Format("Days_Format", Draft.ProductionDays));
        AppendLine(nameof(Strings.Summary_EstimatedTotal), UsdFormat.Format(Draft.TotalPrice));
        message.AppendLine().Append(localizer[nameof(Strings.Order_ConfirmQuestion)]);
        return message.ToString();

        void AppendLine(string labelKey, string value) => message.Append(localizer[labelKey]).Append(": ").AppendLine(value);
    }

    private void OnLocalizerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Localizer.Language))
        {
            OnPropertyChanged(nameof(BoardSpecificationText));
        }
    }
}
