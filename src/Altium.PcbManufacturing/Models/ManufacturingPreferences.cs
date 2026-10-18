using CommunityToolkit.Mvvm.ComponentModel;

namespace Altium.PcbManufacturing.Models;

/// <summary>
/// The user's current manufacturing selections, shared by all tabs.
/// </summary>
/// <remarks>
/// Registered once as a DI singleton. Feature ViewModels receive it through constructor injection,
/// expose it (or wrap its properties) for binding, and subscribe to <see cref="ObservableObject.PropertyChanged"/>
/// to refresh derived values. Selections reference entries of the <see cref="PcbCatalog"/>; they are never copies.
/// It is also the single owner of the default values. Expected to be updated on the UI thread.
/// </remarks>
public sealed partial class ManufacturingPreferences : ObservableObject
{
    public const string DefaultProjectName = "PCB-001";

    private readonly PcbMaterial defaultMaterial;
    private readonly SolderMaskColor defaultSolderMaskColor;
    private readonly BoardThickness defaultBoardThickness;

    [ObservableProperty]
    private string projectName = DefaultProjectName;

    [ObservableProperty]
    private PcbMaterial selectedMaterial;

    [ObservableProperty]
    private SolderMaskColor selectedSolderMaskColor;

    [ObservableProperty]
    private BoardThickness selectedBoardThickness;

    [ObservableProperty]
    private string postcode = string.Empty;

    [ObservableProperty]
    private string notes = string.Empty;

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
        ProjectName == DefaultProjectName
        && Postcode.Length == 0
        && SelectedMaterial == defaultMaterial
        && SelectedSolderMaskColor == defaultSolderMaskColor
        && SelectedBoardThickness == defaultBoardThickness
        && Notes.Length == 0;

    public void ResetToDefaults()
    {
        ProjectName = DefaultProjectName;
        Postcode = string.Empty;
        SelectedMaterial = defaultMaterial;
        SelectedSolderMaskColor = defaultSolderMaskColor;
        SelectedBoardThickness = defaultBoardThickness;
        Notes = string.Empty;
    }
}
