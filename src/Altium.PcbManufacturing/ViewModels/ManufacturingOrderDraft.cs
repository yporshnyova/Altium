using Altium.PcbManufacturing.Models;
using Altium.PcbManufacturing.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Altium.PcbManufacturing.ViewModels;

/// <summary>
/// The manufacturing order that the wizard builds step by step, shared by every step: the order's values and what is
/// calculated from them.
/// </summary>
/// <remarks>
/// <para>
/// Holds typed values only. Text fields keep the text as entered, even while it is invalid; <see cref="Quantity"/> keeps
/// the last valid number. Raw input that cannot be stored here (such as unparsable quantity text) stays with the step
/// that edits it, and that step decides whether the order may progress.
/// </para>
/// <para>
/// The price is calculated by <see cref="PcbPricingService"/>, never here; changes to the material or the quantity
/// notify the calculated properties.
/// </para>
/// </remarks>
public sealed partial class ManufacturingOrderDraft : ObservableObject
{
    public const string DefaultProjectName = "PCB-001";
    public const int DefaultQuantity = 10;

    private readonly PcbPricingService pricing;
    private readonly PcbMaterial defaultMaterial;
    private readonly SolderMaskColor defaultSolderMaskColor;
    private readonly BoardThickness defaultBoardThickness;

    [ObservableProperty]
    private string projectName = DefaultProjectName;

    [ObservableProperty]
    private string postcode = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PriceEstimate))]
    [NotifyPropertyChangedFor(nameof(TotalPrice))]
    private int quantity = DefaultQuantity;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PriceEstimate))]
    [NotifyPropertyChangedFor(nameof(TotalPrice))]
    [NotifyPropertyChangedFor(nameof(ProductionDays))]
    private PcbMaterial selectedMaterial;

    [ObservableProperty]
    private SolderMaskColor selectedSolderMaskColor;

    [ObservableProperty]
    private BoardThickness selectedBoardThickness;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNotes))]
    private string notes = string.Empty;

    public ManufacturingOrderDraft(PcbCatalog catalog, PcbPricingService pricing)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(pricing);

        this.pricing = pricing;
        defaultMaterial = catalog.Materials[0];
        defaultSolderMaskColor = catalog.SolderMaskColors[0];
        defaultBoardThickness = catalog.BoardThicknesses[0];

        selectedMaterial = defaultMaterial;
        selectedSolderMaskColor = defaultSolderMaskColor;
        selectedBoardThickness = defaultBoardThickness;
    }

    /// <summary>
    /// The estimated price of the selected material and quantity.
    /// </summary>
    public PriceEstimate PriceEstimate => pricing.Estimate(SelectedMaterial, Quantity);

    /// <summary>
    /// The estimated order total, in US dollars.
    /// </summary>
    public decimal TotalPrice => PriceEstimate.EstimatedTotalPrice;

    /// <summary>
    /// The estimated production time in days: the selected material's lead time, independent of the quantity.
    /// </summary>
    public int ProductionDays => SelectedMaterial.ProductionDays;

    public bool HasNotes => !string.IsNullOrWhiteSpace(Notes);

    public bool MatchesDefaults() =>
        ProjectName == DefaultProjectName
        && Postcode.Length == 0
        && Quantity == DefaultQuantity
        && SelectedMaterial == defaultMaterial
        && SelectedSolderMaskColor == defaultSolderMaskColor
        && SelectedBoardThickness == defaultBoardThickness
        && Notes.Length == 0;

    public void ResetToDefaults()
    {
        ProjectName = DefaultProjectName;
        Postcode = string.Empty;
        Quantity = DefaultQuantity;
        SelectedMaterial = defaultMaterial;
        SelectedSolderMaskColor = defaultSolderMaskColor;
        SelectedBoardThickness = defaultBoardThickness;
        Notes = string.Empty;
    }

    partial void OnProjectNameChanging(string value) => ArgumentNullException.ThrowIfNull(value);

    partial void OnPostcodeChanging(string value) => ArgumentNullException.ThrowIfNull(value);

    partial void OnQuantityChanging(int value) => ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);

    partial void OnSelectedMaterialChanging(PcbMaterial value) => ArgumentNullException.ThrowIfNull(value);

    partial void OnSelectedSolderMaskColorChanging(SolderMaskColor value) => ArgumentNullException.ThrowIfNull(value);

    partial void OnSelectedBoardThicknessChanging(BoardThickness value) => ArgumentNullException.ThrowIfNull(value);

    partial void OnNotesChanging(string value) => ArgumentNullException.ThrowIfNull(value);
}
