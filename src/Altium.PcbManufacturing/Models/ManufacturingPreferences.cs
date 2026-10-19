using CommunityToolkit.Mvvm.ComponentModel;

namespace Altium.PcbManufacturing.Models;

/// <summary>
/// The user's current manufacturing selections, shared by all tabs.
/// </summary>

public sealed partial class ManufacturingPreferences : ObservableObject
{
    private const string defaultProjectName = "PCB-001";
    private readonly PcbMaterial defaultMaterial;
    private readonly SolderMaskColor defaultSolderMaskColor;
    private readonly BoardThickness defaultBoardThickness;

    [ObservableProperty]
    private string projectName = defaultProjectName;

    [ObservableProperty]
    private PcbMaterial selectedMaterial;

    [ObservableProperty]
    private SolderMaskColor selectedSolderMaskColor;

    [ObservableProperty]
    private BoardThickness selectedBoardThickness;

    [ObservableProperty]
    private string postcode = string.Empty;

    [ObservableProperty]
    private int quantity = DefaultQuantity;

    [ObservableProperty]
    private string notes = string.Empty;

    public const int DefaultQuantity = 10;

    public ManufacturingPreferences(PcbCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        defaultMaterial = catalog.Materials[0];
        defaultSolderMaskColor = catalog.SolderMaskColors[0];
        defaultBoardThickness = catalog.BoardThicknesses[0];

        selectedMaterial = defaultMaterial;
        selectedSolderMaskColor = defaultSolderMaskColor;
        selectedBoardThickness = defaultBoardThickness;
    }

    public bool MatchesDefaults() =>
        ProjectName == defaultProjectName
        && Postcode.Length == 0
        && Quantity == DefaultQuantity
        && SelectedMaterial == defaultMaterial
        && SelectedSolderMaskColor == defaultSolderMaskColor
        && SelectedBoardThickness == defaultBoardThickness
        && Notes.Length == 0;

    public void ResetToDefaults()
    {
        ProjectName = defaultProjectName;
        Postcode = string.Empty;
        Quantity = DefaultQuantity;
        SelectedMaterial = defaultMaterial;
        SelectedSolderMaskColor = defaultSolderMaskColor;
        SelectedBoardThickness = defaultBoardThickness;
        Notes = string.Empty;
    }

    partial void OnQuantityChanging(int value) => ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
}
