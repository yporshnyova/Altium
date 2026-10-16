using CommunityToolkit.Mvvm.ComponentModel;

namespace Altium.PcbManufacturing.Models;

/// <summary>
/// The user's current manufacturing selections, shared by all tabs.
/// </summary>
/// <remarks>
/// Registered once as a DI singleton. Feature ViewModels receive it through constructor injection,
/// expose it (or wrap its properties) for binding, and subscribe to <see cref="ObservableObject.PropertyChanged"/>
/// to refresh derived values. Selections reference entries of the <see cref="PcbCatalog"/>; they are never copies.
/// Expected to be updated on the UI thread.
/// </remarks>
public sealed partial class ManufacturingPreferences : ObservableObject
{
    [ObservableProperty]
    private PcbMaterial selectedMaterial;

    [ObservableProperty]
    private SolderMaskColor selectedSolderMaskColor;

    [ObservableProperty]
    private BoardThickness selectedBoardThickness;

    [ObservableProperty]
    private string postcode = string.Empty;

    public ManufacturingPreferences(PcbCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        selectedMaterial = catalog.Materials[0];
        selectedSolderMaskColor = catalog.SolderMaskColors[0];
        selectedBoardThickness = catalog.BoardThicknesses[0];
    }
}
