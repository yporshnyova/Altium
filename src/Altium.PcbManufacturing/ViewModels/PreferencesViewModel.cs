using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Altium.PcbManufacturing.Localization;
using Altium.PcbManufacturing.Models;
using Altium.PcbManufacturing.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Altium.PcbManufacturing.ViewModels;

/// <summary>
/// Preferences tab: exposes the catalog options and edits the shared <see cref="ManufacturingPreferences"/>.
/// </summary>
/// <remarks>
/// Holds no copy of the selections. Every editable property reads and writes the shared state, and changes made to
/// the shared state from anywhere are re-raised here together with the values derived from them. Prices are derived
/// on read through <see cref="PcbPricingService"/> and never stored. 
/// </remarks>
/// 
public sealed partial class PreferencesViewModel : ObservableValidator
{
    public const int ProjectNameMaxLength = 50;
    public const int NotesMaxLength = 300;

    private readonly ManufacturingPreferences preferences;
    private readonly PcbPricingService pricing;
    private readonly WorkflowNavigationState navigation;
    private readonly IConfirmationService confirmation;
    private readonly Localizer localizer;

    // Incremented on each rejected Save & Continue; the view moves focus to the first invalid field when it changes.
    [ObservableProperty]
    private int invalidSubmitCount;

    public PreferencesViewModel(
        PcbCatalog catalog,
        ManufacturingPreferences preferences,
        PcbPricingService pricing,
        WorkflowNavigationState navigation,
        IConfirmationService confirmation,
        Localizer localizer)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(preferences);
        ArgumentNullException.ThrowIfNull(pricing);
        ArgumentNullException.ThrowIfNull(navigation);
        ArgumentNullException.ThrowIfNull(confirmation);
        ArgumentNullException.ThrowIfNull(localizer);

        Materials = catalog.Materials;
        SolderMaskColors = catalog.SolderMaskColors;
        BoardThicknesses = catalog.BoardThicknesses;

        this.pricing = pricing;
        this.navigation = navigation;
        this.confirmation = confirmation;
        this.localizer = localizer;
        this.localizer.PropertyChanged += OnLocalizerPropertyChanged;
        this.preferences = preferences;
        this.preferences.PropertyChanged += OnPreferencesPropertyChanged;
        ErrorsChanged += OnErrorsChanged;
        this.navigation.SetReadinessCheck(WorkflowStep.Preferences, AreAllFieldsValid);
    }

    public IReadOnlyList<PcbMaterial> Materials { get; }

    public IReadOnlyList<SolderMaskColor> SolderMaskColors { get; }

    public IReadOnlyList<BoardThickness> BoardThicknesses { get; }

    /// <summary>
    /// Validated on every change. Letters, digits, spaces and any other characters are accepted; only presence and
    /// length are checked.
    /// </summary>
    [Required(ErrorMessageResourceType = typeof(Strings), ErrorMessageResourceName = nameof(Strings.Validation_ProjectNameRequired))]
    [StringLength(ProjectNameMaxLength, ErrorMessageResourceType = typeof(Strings), ErrorMessageResourceName = nameof(Strings.Validation_MaxLength))]
    public string ProjectName
    {
        get => preferences.ProjectName;
        set => preferences.ProjectName = value ?? string.Empty;
    }

    /// <summary>
    /// The postcode as typed. The raw text is stored in the shared state even when invalid, so the user's input is never
    /// lost; consumers such as the future Order step must check it with <see cref="PolishPostcode.IsValid"/>.
    /// </summary>
    /// <remarks>Validated on every change. The initial empty value is not reported until the user edits it.</remarks>
    [Required(ErrorMessageResourceType = typeof(Strings), ErrorMessageResourceName = nameof(Strings.Validation_PostcodeRequired))]
    [RegularExpression(PolishPostcode.Pattern, ErrorMessageResourceType = typeof(Strings), ErrorMessageResourceName = nameof(Strings.Validation_PostcodeFormat))]
    public string Postcode
    {
        get => preferences.Postcode;
        set => preferences.Postcode = value ?? string.Empty;
    }

    public bool HasProjectNameErrors => GetErrors(nameof(ProjectName)).Any();

    public bool HasPostcodeErrors => GetErrors(nameof(Postcode)).Any();

    public bool IsProjectNameBlank => string.IsNullOrWhiteSpace(ProjectName);

    public bool IsPostcodeBlank => string.IsNullOrWhiteSpace(Postcode);

    public PcbMaterial SelectedMaterial
    {
        get => preferences.SelectedMaterial;
        set => preferences.SelectedMaterial =
            value ?? throw new ArgumentNullException(nameof(value));
    }

    public SolderMaskColor? SelectedSolderMaskColor
    {
        get => preferences.SelectedSolderMaskColor;
        set
        {
            if (value is not null)
            {
                preferences.SelectedSolderMaskColor = value;
            }
        }
    }

    public BoardThickness? SelectedBoardThickness
    {
        get => preferences.SelectedBoardThickness;
        set
        {
            if (value is not null)
            {
                preferences.SelectedBoardThickness = value;
            }
        }
    }

    /// <summary>
    /// Optional order notes; validated on every change. Line breaks count as typed (a TextBox Enter inserts CR LF).
    /// </summary>
    [StringLength(NotesMaxLength, ErrorMessageResourceType = typeof(Strings), ErrorMessageResourceName = nameof(Strings.Validation_MaxLength))]
    public string Notes
    {
        get => preferences.Notes;
        set => preferences.Notes = value ?? string.Empty;
    }

    /// <summary>
    /// Character counter shown below the notes field, e.g. <c>12 / 300</c>; it reflects the actual string length.
    /// </summary>
    public string NotesCounterText => $"{Notes.Length} / {NotesMaxLength}";

    /// <summary>
    /// Estimated price of the sample order with the selected material.
    /// </summary>
    public PriceEstimate PriceEstimate => pricing.Estimate(SelectedMaterial);

    public string BasePriceText => UsdFormat.Format(pricing.BasePrice);

    /// <summary>
    /// The selected material's monetary impact, e.g. <c>+$208.60</c>, or the localized "Included" when there is none.
    /// </summary>
    public string MaterialSurchargeText =>
        PriceEstimate.MaterialSurcharge == 0
            ? localizer[nameof(Strings.Pricing_Included)]
            : UsdFormat.FormatChange(PriceEstimate.MaterialSurcharge);

    public string EstimatedPriceText => UsdFormat.Format(PriceEstimate.EstimatedPrice);

    /// <summary>
    /// The selected material's catalog production time, in days.
    /// </summary>
    public int EstimatedProductionDays => SelectedMaterial.ProductionDays;

    /// <summary>
    /// The production time as localized text with the correct plural, e.g. <c>12 days</c> or <c>12 дней</c>.
    /// </summary>
    public string EstimatedProductionTimeText => localizer.FormatDays(EstimatedProductionDays);

    /// <summary>
    /// Restores the default preferences, asking first when that would discard changes.
    /// </summary>
    [RelayCommand]
    private void ResetToDefaults()
    {
        if (!preferences.MatchesDefaults() && !confirmation.Confirm(
                localizer[nameof(Strings.Reset_ConfirmTitle)], localizer[nameof(Strings.Reset_ConfirmMessage)]))
        {
            return;
        }

        preferences.ResetToDefaults();
        ClearErrors();
    }

    /// <summary>
    /// Validates every field and, when all are valid, completes the Preferences step: Quote is unlocked and opened.
    /// </summary>
    /// <remarks>Nothing is written to disk; the preferences stay in the shared state for this session.</remarks>
    [RelayCommand]
    private void SaveAndContinue()
    {
        if (ValidateBeforeContinuing())
        {
            navigation.CompleteStep(WorkflowStep.Preferences);
        }
    }

    // Explicit validation for Save & Continue: every field is validated with visible messages and, if any is invalid,
    // the view is asked to focus the first error.
    private bool ValidateBeforeContinuing()
    {
        ValidateAllProperties();
        if (HasErrors)
        {
            InvalidSubmitCount++;
            return false;
        }

        return true;
    }

    private bool AreAllFieldsValid() =>
        Validator.TryValidateObject(this, new ValidationContext(this), validationResults: null, validateAllProperties: true);

    private void OnPreferencesPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(ManufacturingPreferences.ProjectName):
                OnPropertyChanged(nameof(ProjectName));
                OnPropertyChanged(nameof(IsProjectNameBlank));
                ValidateProperty(ProjectName, nameof(ProjectName));
                break;
            case nameof(ManufacturingPreferences.Postcode):
                OnPropertyChanged(nameof(Postcode));
                OnPropertyChanged(nameof(IsPostcodeBlank));
                ValidateProperty(Postcode, nameof(Postcode));
                break;
            case nameof(ManufacturingPreferences.Notes):
                OnPropertyChanged(nameof(Notes));
                OnPropertyChanged(nameof(NotesCounterText));
                ValidateProperty(Notes, nameof(Notes));
                break;
            case nameof(ManufacturingPreferences.SelectedMaterial):
                OnPropertyChanged(nameof(SelectedMaterial));
                OnPropertyChanged(nameof(PriceEstimate));
                OnPropertyChanged(nameof(MaterialSurchargeText));
                OnPropertyChanged(nameof(EstimatedPriceText));
                OnPropertyChanged(nameof(EstimatedProductionDays));
                OnPropertyChanged(nameof(EstimatedProductionTimeText));
                break;
            case nameof(ManufacturingPreferences.SelectedSolderMaskColor):
                OnPropertyChanged(nameof(SelectedSolderMaskColor));
                break;
            case nameof(ManufacturingPreferences.SelectedBoardThickness):
                OnPropertyChanged(nameof(SelectedBoardThickness));
                break;
        }

        navigation.RefreshAvailability();
    }

    // Localized texts follow the language. Fields that currently show an error are re-validated so the message is
    // re-created in the new language; untouched fields stay unvalidated.
    private void OnLocalizerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(Localizer.Language))
        {
            return;
        }

        OnPropertyChanged(nameof(MaterialSurchargeText));
        OnPropertyChanged(nameof(EstimatedProductionTimeText));
        RevalidateIfInvalid(ProjectName, nameof(ProjectName));
        RevalidateIfInvalid(Postcode, nameof(Postcode));
        RevalidateIfInvalid(Notes, nameof(Notes));
    }

    private void RevalidateIfInvalid(object value, string propertyName)
    {
        if (GetErrors(propertyName).Any())
        {
            ValidateProperty(value, propertyName);
        }
    }

    private void OnErrorsChanged(object? sender, DataErrorsChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(ProjectName):
                OnPropertyChanged(nameof(HasProjectNameErrors));
                break;
            case nameof(Postcode):
                OnPropertyChanged(nameof(HasPostcodeErrors));
                break;
        }
    }
}
