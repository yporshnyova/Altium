using System.ComponentModel;
using Altium.PcbManufacturing.Localization;
using Altium.PcbManufacturing.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Altium.PcbManufacturing.ViewModels;

/// <summary>
/// Quote step: the board specification, the grouped parameter table and the board preview inputs. Prices and the
/// selections are bound from <see cref="Draft"/> directly.
/// </summary>
public sealed partial class QuoteViewModel : ObservableObject
{
    private readonly Localizer localizer;
    private readonly decimal thickestBoardMm;

    public QuoteViewModel(PcbCatalog catalog, ManufacturingOrderDraft draft, Localizer localizer)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(draft);
        ArgumentNullException.ThrowIfNull(localizer);

        Specification = catalog.BoardSpecification;
        thickestBoardMm = catalog.BoardThicknesses.Max(thickness => thickness.Millimeters);
        Draft = draft;
        this.localizer = localizer;
        // The parameter rows and the relative thickness are derived from the draft here, so they follow it.
        Draft.PropertyChanged += OnDraftPropertyChanged;
        this.localizer.PropertyChanged += OnLocalizerPropertyChanged;
        ParameterGroups = CreateParameterGroups();
    }

    public ManufacturingOrderDraft Draft { get; }

    /// <summary>
    /// The read-only parameter table, grouped into Dimensions, Layers, Finish and Board preferences.
    /// </summary>
    /// <remarks>
    /// The groups and rows never change, so this is a plain read-only list; each row reads its value from the
    /// specification or the draft and is refreshed when either the draft or the language changes.
    /// </remarks>
    public IReadOnlyList<QuoteParameterGroup> ParameterGroups { get; }

    /// <summary>
    /// The fixed parameters of the sample order (dimensions, layers, finish).
    /// </summary>
    public BoardSpecification Specification { get; }

    /// <summary>
    /// The selected thickness as a fraction of the thickest catalog option, in (0, 1]. The side view scales the board
    /// profile by it, so every catalog thickness fits the space reserved for the thickest one.
    /// </summary>
    public double RelativeBoardThickness => (double)(Draft.SelectedBoardThickness.Millimeters / thickestBoardMm);

    /// <summary>
    /// Localized board size, e.g. <c>100 × 80 mm</c>.
    /// </summary>
    public string BoardSizeText => localizer.Format(nameof(Strings.Quote_SizeValue), Specification.WidthMm, Specification.HeightMm);

    /// <summary>
    /// Localized layer count, e.g. <c>2 layers</c> or <c>2 слоя</c>.
    /// </summary>
    public string LayerCountText => localizer.Format("Layers_Format", Specification.LayerCount);

    private IReadOnlyList<QuoteParameterGroup> CreateParameterGroups() =>
    [
        Group(nameof(Strings.Quote_Dimensions),
            Parameter(nameof(Strings.Quote_Size), () => BoardSizeText),
            Parameter(nameof(Strings.Quote_Quantity), () => localizer.Format("Boards_Format", Draft.Quantity))),
        Group(nameof(Strings.Quote_Layers),
            Parameter(nameof(Strings.Quote_LayerCount), () => LayerCountText),
            Parameter(nameof(Strings.Quote_CopperWeight), () => localizer.Format(nameof(Strings.Quote_CopperWeightValue), Specification.CopperWeightOz))),
        Group(nameof(Strings.Quote_Finish),
            Parameter(nameof(Strings.Quote_SurfaceFinish), () => CatalogName("SurfaceFinish", Specification.SurfaceFinish.Id, Specification.SurfaceFinish.Name)),
            Parameter(nameof(Strings.Quote_Silkscreen), () => CatalogName("SilkscreenColor", Specification.SilkscreenColor.Id, Specification.SilkscreenColor.Name))),
        Group(nameof(Strings.Preferences_BoardPreferences),
            Parameter(nameof(Strings.Summary_Material), () => Draft.SelectedMaterial.Name),
            Parameter(nameof(Strings.Summary_SolderMask), () => CatalogName("SolderMaskColor", Draft.SelectedSolderMaskColor.Id, Draft.SelectedSolderMaskColor.Name), () => Draft.SelectedSolderMaskColor.HexColor),
            Parameter(nameof(Strings.Summary_BoardThickness), () => localizer.Format(nameof(Strings.Thickness_Millimeters), Draft.SelectedBoardThickness.Millimeters))),
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

    private void OnDraftPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(ManufacturingOrderDraft.Quantity):
            case nameof(ManufacturingOrderDraft.SelectedMaterial):
            case nameof(ManufacturingOrderDraft.SelectedSolderMaskColor):
                RefreshParameters();
                break;
            case nameof(ManufacturingOrderDraft.SelectedBoardThickness):
                OnPropertyChanged(nameof(RelativeBoardThickness));
                RefreshParameters();
                break;
        }
    }

    private void OnLocalizerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Localizer.Language))
        {
            OnPropertyChanged(nameof(BoardSizeText));
            OnPropertyChanged(nameof(LayerCountText));
            RefreshParameters();
        }
    }
}
