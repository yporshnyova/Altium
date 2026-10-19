using System.ComponentModel;
using Altium.PcbManufacturing.Formatting;
using Altium.PcbManufacturing.Localization;
using Altium.PcbManufacturing.Models;
using Altium.PcbManufacturing.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Altium.PcbManufacturing.ViewModels;

/// <summary>
/// Quote tab: the board specification, the options chosen on Preferences, the price estimate and the board preview.
/// </summary>
public sealed partial class QuoteViewModel : ObservableObject
{
    private readonly ManufacturingPreferences preferences;
    private readonly PcbPricingService pricing;
    private readonly WorkflowNavigationState navigation;
    private readonly Localizer localizer;
    private readonly decimal thickestBoardMm;

    public QuoteViewModel(
        PcbCatalog catalog,
        ManufacturingPreferences preferences,
        PcbPricingService pricing,
        WorkflowNavigationState navigation,
        Localizer localizer)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(preferences);
        ArgumentNullException.ThrowIfNull(pricing);
        ArgumentNullException.ThrowIfNull(navigation);
        ArgumentNullException.ThrowIfNull(localizer);

        Specification = catalog.BoardSpecification;
        thickestBoardMm = catalog.BoardThicknesses.Max(thickness => thickness.Millimeters);
        this.preferences = preferences;
        this.pricing = pricing;
        this.navigation = navigation;
        this.localizer = localizer;
        this.preferences.PropertyChanged += OnPreferencesPropertyChanged;
        this.localizer.PropertyChanged += OnLocalizerPropertyChanged;
        ParameterGroups = CreateParameterGroups();
    }

    /// <summary>
    /// The read-only parameter table, grouped into Dimensions, Layers, Finish and Board preferences.
    /// </summary>
    /// <remarks>
    /// The groups and rows never change, so this is a plain read-only list; each row reads its value from the
    /// specification or the shared preferences and is refreshed when they or the language change.
    /// </remarks>
    public IReadOnlyList<QuoteParameterGroup> ParameterGroups { get; }

    /// <summary>
    /// The fixed parameters of the sample order (dimensions, layers, finish).
    /// </summary>
    public BoardSpecification Specification { get; }

    public PcbMaterial SelectedMaterial => preferences.SelectedMaterial;

    public SolderMaskColor SelectedSolderMaskColor => preferences.SelectedSolderMaskColor;

    public BoardThickness SelectedBoardThickness => preferences.SelectedBoardThickness;

    /// <summary>
    /// The selected thickness as a fraction of the thickest catalog option, in (0, 1]. The side view scales the board
    /// profile by it, so every catalog thickness fits the space reserved for the thickest one.
    /// </summary>
    public double RelativeBoardThickness => (double)(SelectedBoardThickness.Millimeters / thickestBoardMm);

    /// <summary>
    /// The selected solder mask colour as <c>#RRGGBB</c>; the preview fills the board with it.
    /// </summary>
    public string SolderMaskHexColor => SelectedSolderMaskColor.HexColor;

    /// <summary>
    /// Localized board size, e.g. <c>100 × 80 mm</c>.
    /// </summary>
    public string BoardSizeText => localizer.Format(nameof(Strings.Quote_SizeValue), Specification.WidthMm, Specification.HeightMm);

    /// <summary>
    /// Localized board count, e.g. <c>10 pcs</c> or <c>10 шт.</c>.
    /// </summary>
    public string QuantityText => localizer.Format("Boards_Format", preferences.Quantity);

    /// <summary>
    /// Localized layer count, e.g. <c>2 layers</c> or <c>2 слоя</c>.
    /// </summary>
    public string LayerCountText => localizer.Format("Layers_Format", Specification.LayerCount);

    /// <summary>
    /// Estimated price for the selected material and quantity.
    /// </summary>
    public PriceEstimate PriceEstimate => pricing.Estimate(SelectedMaterial, preferences.Quantity);

    public string BaseUnitPriceText => UsdFormat.Format(pricing.BaseUnitPrice);

    public string MaterialSurchargePerUnitText =>
        UsdFormat.FormatSurcharge(PriceEstimate.MaterialSurchargePerUnit, localizer[nameof(Strings.Pricing_Included)]);

    public string EstimatedUnitPriceText => UsdFormat.Format(PriceEstimate.EstimatedUnitPrice);

    public string EstimatedTotalPriceText => UsdFormat.Format(PriceEstimate.EstimatedTotalPrice);

    public string EstimatedProductionTimeText => localizer.Format("Days_Format", SelectedMaterial.ProductionDays);

    /// <summary>
    /// Returns to Preferences; every value and the unlocked Quote step are kept.
    /// </summary>
    [RelayCommand]
    private void BackToPreferences() => navigation.TryNavigateTo(WorkflowStep.Preferences);

    /// <summary>
    /// Completes the Quote step: Order is unlocked and opened.
    /// </summary>
    [RelayCommand]
    private void ContinueToOrder()
    {
        if (navigation.IsAvailable(WorkflowStep.Quote))
        {
            navigation.CompleteStep(WorkflowStep.Quote);
        }
    }

    private IReadOnlyList<QuoteParameterGroup> CreateParameterGroups() =>
    [
        Group(nameof(Strings.Quote_Dimensions),
            Parameter(nameof(Strings.Quote_Size), () => BoardSizeText),
            Parameter(nameof(Strings.Quote_Quantity), () => QuantityText)),
        Group(nameof(Strings.Quote_Layers),
            Parameter(nameof(Strings.Quote_LayerCount), () => LayerCountText),
            Parameter(nameof(Strings.Quote_CopperWeight), () => localizer.Format(nameof(Strings.Quote_CopperWeightValue), Specification.CopperWeightOz))),
        Group(nameof(Strings.Quote_Finish),
            Parameter(nameof(Strings.Quote_SurfaceFinish), () => CatalogName("SurfaceFinish", Specification.SurfaceFinish.Id, Specification.SurfaceFinish.Name)),
            Parameter(nameof(Strings.Quote_Silkscreen), () => CatalogName("SilkscreenColor", Specification.SilkscreenColor.Id, Specification.SilkscreenColor.Name))),
        Group(nameof(Strings.Preferences_BoardPreferences),
            Parameter(nameof(Strings.Summary_Material), () => SelectedMaterial.Name),
            Parameter(nameof(Strings.Summary_SolderMask), () => CatalogName("SolderMaskColor", SelectedSolderMaskColor.Id, SelectedSolderMaskColor.Name), () => SolderMaskHexColor),
            Parameter(nameof(Strings.Summary_BoardThickness), () => localizer.Format(nameof(Strings.Thickness_Millimeters), SelectedBoardThickness.Millimeters))),
    ];

    private QuoteParameterGroup Group(string titleKey, params QuoteParameter[] parameters) => new(localizer, titleKey, parameters);

    private QuoteParameter Parameter(string labelKey, Func<string> value, Func<string?>? swatchHexColor = null) =>
        new(localizer, labelKey, value, swatchHexColor);

    private string CatalogName(string prefix, string id, string fallback) => localizer.Find(Localizer.CatalogKey(prefix, id)) ?? fallback;

    private void RefreshParameters()
    {
        foreach (QuoteParameterGroup group in ParameterGroups)
        {
            group.Refresh();
        }
    }

    private void OnPreferencesPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(ManufacturingPreferences.SelectedMaterial):
                OnPropertyChanged(nameof(SelectedMaterial));
                OnPropertyChanged(nameof(PriceEstimate));
                OnPropertyChanged(nameof(MaterialSurchargePerUnitText));
                OnPropertyChanged(nameof(EstimatedUnitPriceText));
                OnPropertyChanged(nameof(EstimatedTotalPriceText));
                OnPropertyChanged(nameof(EstimatedProductionTimeText));
                break;
            case nameof(ManufacturingPreferences.Quantity):
                OnPropertyChanged(nameof(QuantityText));
                OnPropertyChanged(nameof(PriceEstimate));
                OnPropertyChanged(nameof(EstimatedTotalPriceText));
                break;
            case nameof(ManufacturingPreferences.SelectedSolderMaskColor):
                OnPropertyChanged(nameof(SelectedSolderMaskColor));
                OnPropertyChanged(nameof(SolderMaskHexColor));
                break;
            case nameof(ManufacturingPreferences.SelectedBoardThickness):
                OnPropertyChanged(nameof(SelectedBoardThickness));
                OnPropertyChanged(nameof(RelativeBoardThickness));
                break;
        }

        RefreshParameters();
    }

    private void OnLocalizerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Localizer.Language))
        {
            OnPropertyChanged(nameof(BoardSizeText));
            OnPropertyChanged(nameof(QuantityText));
            OnPropertyChanged(nameof(LayerCountText));
            OnPropertyChanged(nameof(MaterialSurchargePerUnitText));
            OnPropertyChanged(nameof(EstimatedProductionTimeText));
            RefreshParameters();
        }
    }
}
