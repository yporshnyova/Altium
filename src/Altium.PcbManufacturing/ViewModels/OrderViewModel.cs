using System.ComponentModel;
using System.Text;
using Altium.PcbManufacturing.Formatting;
using Altium.PcbManufacturing.Localization;
using Altium.PcbManufacturing.Models;
using Altium.PcbManufacturing.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Altium.PcbManufacturing.ViewModels;

/// <summary>
/// Order tab: a final review of the shared preferences and their price, and the Place Order action.
/// </summary>
/// <remarks>
/// There is no order backend. Nothing is sent or stored.
/// </remarks>
public sealed partial class OrderViewModel : ObservableObject
{
    // The texts derived from the shared preferences or the language; all are refreshed together when either changes.
    private static readonly string[] summaryProperties =
    [
        nameof(ProjectName),
        nameof(Postcode),
        nameof(QuantityText),
        nameof(BoardSpecificationText),
        nameof(MaterialName),
        nameof(SolderMaskText),
        nameof(SolderMaskHexColor),
        nameof(BoardThicknessText),
        nameof(HasNotes),
        nameof(NotesText),
        nameof(PriceEstimate),
        nameof(EstimatedUnitPriceText),
        nameof(EstimatedTotalPriceText),
        nameof(EstimatedProductionTimeText),
    ];

    private readonly BoardSpecification specification;
    private readonly ManufacturingPreferences preferences;
    private readonly PreferencesViewModel preferencesScreen;
    private readonly PcbPricingService pricing;
    private readonly WorkflowNavigationState navigation;
    private readonly IConfirmationService confirmation;
    private readonly Localizer localizer;

    public OrderViewModel(
        PcbCatalog catalog,
        ManufacturingPreferences preferences,
        PreferencesViewModel preferencesScreen,
        PcbPricingService pricing,
        WorkflowNavigationState navigation,
        IConfirmationService confirmation,
        Localizer localizer)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(preferences);
        ArgumentNullException.ThrowIfNull(preferencesScreen);
        ArgumentNullException.ThrowIfNull(pricing);
        ArgumentNullException.ThrowIfNull(navigation);
        ArgumentNullException.ThrowIfNull(confirmation);
        ArgumentNullException.ThrowIfNull(localizer);

        specification = catalog.BoardSpecification;
        this.preferences = preferences;
        this.preferencesScreen = preferencesScreen;
        this.pricing = pricing;
        this.navigation = navigation;
        this.confirmation = confirmation;
        this.localizer = localizer;
        this.preferences.PropertyChanged += OnPreferencesPropertyChanged;
        this.localizer.PropertyChanged += OnLocalizerPropertyChanged;
        this.navigation.PropertyChanged += OnNavigationPropertyChanged;
    }

    /// <summary>
    /// Whether the user has confirmed the order. The workflow is then finished: the summary is final and only a new order
    /// can follow. Read from the workflow, which owns it.
    /// </summary>
    public bool IsOrderCompleted => navigation.IsOrderCompleted;

    public string ProjectName => preferences.ProjectName;

    public string Postcode => preferences.Postcode;

    /// <summary>
    /// Localized board count, e.g. <c>10 pcs.</c> or <c>10 шт.</c>.
    /// </summary>
    public string QuantityText => localizer.Format("Boards_Format", preferences.Quantity);

    /// <summary>
    /// The fixed board size and layer count, e.g. <c>100 × 80 mm · 2 layers</c>.
    /// </summary>
    public string BoardSpecificationText =>
        $"{localizer.Format(nameof(Strings.Quote_SizeValue), specification.WidthMm, specification.HeightMm)} · {localizer.Format("Layers_Format", specification.LayerCount)}";

    public string MaterialName => preferences.SelectedMaterial.Name;

    public string SolderMaskText =>
        localizer.Find(Localizer.CatalogKey("SolderMaskColor", preferences.SelectedSolderMaskColor.Id)) ?? preferences.SelectedSolderMaskColor.Name;

    /// <summary>
    /// The selected solder mask colour as <c>#RRGGBB</c>, for the swatch.
    /// </summary>
    public string SolderMaskHexColor => preferences.SelectedSolderMaskColor.HexColor;

    public string BoardThicknessText => localizer.Format(nameof(Strings.Thickness_Millimeters), preferences.SelectedBoardThickness.Millimeters);

    public bool HasNotes => !string.IsNullOrWhiteSpace(preferences.Notes);

    public string NotesText => HasNotes ? preferences.Notes : localizer[nameof(Strings.Summary_NotEntered)];

    /// <summary>
    /// The same estimate as Quote: the selected material and quantity priced by the shared pricing service.
    /// </summary>
    public PriceEstimate PriceEstimate => pricing.Estimate(preferences.SelectedMaterial, preferences.Quantity);

    public string EstimatedUnitPriceText => UsdFormat.Format(PriceEstimate.EstimatedUnitPrice);

    public string EstimatedTotalPriceText => UsdFormat.Format(PriceEstimate.EstimatedTotalPrice);

    public string EstimatedProductionTimeText => localizer.Format("Days_Format", preferences.SelectedMaterial.ProductionDays);

    /// <summary>
    /// Returns to Quote; nothing is changed. Refused once the order is completed.
    /// </summary>
    [RelayCommand]
    private void BackToQuote() => navigation.TryNavigateTo(WorkflowStep.Quote);

    /// <summary>
    /// After a completed order, starts new workflow: default preferences without validation messages, and only
    /// Preferences unlocked. Language and theme are application settings and are kept.
    /// </summary>
    [RelayCommand(CanExecute = nameof(IsOrderCompleted))]
    private void NewOrder()
    {
        preferencesScreen.RestoreDefaults();
        navigation.Reset();
    }

    /// <summary>
    /// Re-validates the current input and, when it is valid, asks the user to confirm the summarized order. Invalid input
    /// is never submitted: Preferences is opened with its errors shown. The generated command cannot run again while the
    /// confirmation is open.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanPlaceOrder))]
    private async Task PlaceOrderAsync()
    {
        if (!preferencesScreen.ValidateAndShowErrors())
        {
            navigation.TryNavigateTo(WorkflowStep.Preferences);
            return;
        }

        if (!navigation.IsAvailable(WorkflowStep.Order))
        {
            return;
        }

        if (await confirmation.ConfirmAsync(localizer[nameof(Strings.Order_ConfirmTitle)], CreateConfirmationMessage()))
        {
            navigation.CompleteStep(WorkflowStep.Order);
        }
    }

    private bool CanPlaceOrder() => !IsOrderCompleted;

    private string CreateConfirmationMessage()
    {
        StringBuilder message = new();
        AppendLine(nameof(Strings.Summary_ProjectName), ProjectName);
        AppendLine(nameof(Strings.Order_DeliveryPostcode), Postcode);
        AppendLine(nameof(Strings.Summary_Quantity), QuantityText);
        AppendLine(nameof(Strings.Summary_Material), MaterialName);
        AppendLine(nameof(Strings.Summary_SolderMask), SolderMaskText);
        AppendLine(nameof(Strings.Summary_BoardThickness), BoardThicknessText);
        AppendLine(nameof(Strings.Summary_EstimatedProductionTime), EstimatedProductionTimeText);
        AppendLine(nameof(Strings.Summary_EstimatedTotal), EstimatedTotalPriceText);
        message.AppendLine().Append(localizer[nameof(Strings.Order_ConfirmQuestion)]);
        return message.ToString();

        void AppendLine(string labelKey, string value) => message.Append(localizer[labelKey]).Append(": ").AppendLine(value);
    }

    private void RefreshSummary()
    {
        foreach (string property in summaryProperties)
        {
            OnPropertyChanged(property);
        }
    }

    private void OnPreferencesPropertyChanged(object? sender, PropertyChangedEventArgs e) => RefreshSummary();

    private void OnNavigationPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(WorkflowNavigationState.IsOrderCompleted))
        {
            OnPropertyChanged(nameof(IsOrderCompleted));
            PlaceOrderCommand.NotifyCanExecuteChanged();
            NewOrderCommand.NotifyCanExecuteChanged();
        }
    }

    private void OnLocalizerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Localizer.Language))
        {
            RefreshSummary();
        }
    }
}
