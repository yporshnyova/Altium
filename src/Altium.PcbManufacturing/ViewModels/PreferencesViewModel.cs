using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using Altium.PcbManufacturing.Localization;
using Altium.PcbManufacturing.Models;
using Altium.PcbManufacturing.Services;
using Altium.PcbManufacturing.Validation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Altium.PcbManufacturing.ViewModels;

/// <summary>
/// Preferences step: the catalog options, the text the user types, its validation feedback, and Reset to Defaults.
/// </summary>

public sealed partial class PreferencesViewModel : ObservableValidator
{
    private readonly IConfirmationService confirmation;
    private readonly Localizer localizer;

    private string projectNameInput = string.Empty;
    private string postcodeInput = string.Empty;
    private string quantityInput = string.Empty;
    private string notesInput = string.Empty;

    // Incremented on each rejected submission; the view moves focus to the first invalid field when it changes.
    [ObservableProperty]
    private int invalidSubmitCount;

    [ObservableProperty]
    private bool isInputValid;

    public PreferencesViewModel(
        PcbCatalog catalog,
        ManufacturingOrderDraft draft,
        IConfirmationService confirmation,
        Localizer localizer)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(draft);
        ArgumentNullException.ThrowIfNull(confirmation);
        ArgumentNullException.ThrowIfNull(localizer);

        Materials = catalog.Materials;
        SolderMaskColors = catalog.SolderMaskColors;
        BoardThicknesses = catalog.BoardThicknesses;
        Draft = draft;
        this.confirmation = confirmation;
        this.localizer = localizer;
        this.localizer.PropertyChanged += OnLocalizerPropertyChanged;
        ErrorsChanged += OnErrorsChanged;
        ResetInput();
    }

    public ManufacturingOrderDraft Draft { get; }

    public IReadOnlyList<PcbMaterial> Materials { get; }

    public IReadOnlyList<SolderMaskColor> SolderMaskColors { get; }

    public IReadOnlyList<BoardThickness> BoardThicknesses { get; }

    /// <summary>
    /// Validated on every change. Letters, digits, spaces and any other characters are accepted; only presence and
    /// length are checked.
    /// </summary>
    [Required(ErrorMessageResourceType = typeof(Strings), ErrorMessageResourceName = nameof(Strings.Validation_ProjectNameRequired))]
    [StringLength(OrderInputRules.ProjectNameMaxLength, ErrorMessageResourceType = typeof(Strings), ErrorMessageResourceName = nameof(Strings.Validation_MaxLength))]
    public string ProjectNameInput
    {
        get => projectNameInput;
        set
        {
            if (SetInput(ref projectNameInput, value))
            {
                OnPropertyChanged(nameof(IsProjectNameBlank));
                Draft.ProjectName = projectNameInput;
            }
        }
    }

    /// <summary>
    /// The postcode as typed. It is written to the draft even when invalid, so the user's input is never lost; whether
    /// it is valid is reported here and by <see cref="IsInputValid"/>.
    /// </summary>
    [Required(ErrorMessageResourceType = typeof(Strings), ErrorMessageResourceName = nameof(Strings.Validation_PostcodeRequired))]
    [RegularExpression(PolishPostcode.Pattern, ErrorMessageResourceType = typeof(Strings), ErrorMessageResourceName = nameof(Strings.Validation_PostcodeFormat))]
    public string PostcodeInput
    {
        get => postcodeInput;
        set
        {
            if (SetInput(ref postcodeInput, value))
            {
                OnPropertyChanged(nameof(IsPostcodeBlank));
                Draft.Postcode = postcodeInput;
            }
        }
    }

    /// <summary>
    /// The board quantity as typed. Any text is accepted and kept, so invalid input is never lost or thrown away by the
    /// binding; only a valid whole number (see <see cref="OrderInputRules.TryParseQuantity"/>) is written to the draft.
    /// </summary>
    [Required(ErrorMessageResourceType = typeof(Strings), ErrorMessageResourceName = nameof(Strings.Validation_QuantityRequired))]
    [CustomValidation(typeof(OrderInputRules), nameof(OrderInputRules.ValidateQuantity))]
    public string QuantityInput
    {
        get => quantityInput;
        set
        {
            if (SetInput(ref quantityInput, value))
            {
                OnPropertyChanged(nameof(IsQuantityBlank));
                if (OrderInputRules.TryParseQuantity(quantityInput, out int quantity))
                {
                    Draft.Quantity = quantity;
                }
            }
        }
    }

    /// <summary>
    /// Optional order notes; validated on every change. Line breaks count as typed (a TextBox Enter inserts CR LF).
    /// </summary>
    [StringLength(OrderInputRules.NotesMaxLength, ErrorMessageResourceType = typeof(Strings), ErrorMessageResourceName = nameof(Strings.Validation_MaxLength))]
    public string NotesInput
    {
        get => notesInput;
        set
        {
            if (SetInput(ref notesInput, value))
            {
                OnPropertyChanged(nameof(NotesCounterText));
                Draft.Notes = notesInput;
            }
        }
    }

    public bool HasProjectNameErrors => GetErrors(nameof(ProjectNameInput)).Any();

    public bool HasPostcodeErrors => GetErrors(nameof(PostcodeInput)).Any();

    public bool HasQuantityErrors => GetErrors(nameof(QuantityInput)).Any();

    public bool IsProjectNameBlank => string.IsNullOrWhiteSpace(ProjectNameInput);

    public bool IsPostcodeBlank => string.IsNullOrWhiteSpace(PostcodeInput);

    public bool IsQuantityBlank => string.IsNullOrWhiteSpace(QuantityInput);

    /// <summary>
    /// Character counter shown below the notes field, e.g. <c>12 / 300</c>; it reflects the actual string length.
    /// </summary>
    public string NotesCounterText => $"{NotesInput.Length} / {OrderInputRules.NotesMaxLength}";

    /// <summary>
    /// Validates every field with visible messages and, if any is invalid, asks the view to focus the first error.
    /// </summary>
    /// <returns>Whether every field is valid.</returns>
    public bool ValidateAndShowErrors()
    {
        ValidateAllProperties();
        if (HasErrors)
        {
            InvalidSubmitCount++;
            return false;
        }

        return true;
    }

    /// <summary>
    /// Discards the typed input and its validation messages and shows the draft's current values, as for a fresh start:
    /// nothing is reported until a field is edited or the step is submitted.
    /// </summary>
    public void ResetInput()
    {
        projectNameInput = Draft.ProjectName;
        postcodeInput = Draft.Postcode;
        quantityInput = OrderInputRules.FormatQuantity(Draft.Quantity);
        notesInput = Draft.Notes;
        ClearErrors();
        UpdateInputValidity();

        OnPropertyChanged(nameof(ProjectNameInput));
        OnPropertyChanged(nameof(PostcodeInput));
        OnPropertyChanged(nameof(QuantityInput));
        OnPropertyChanged(nameof(NotesInput));
        OnPropertyChanged(nameof(IsProjectNameBlank));
        OnPropertyChanged(nameof(IsPostcodeBlank));
        OnPropertyChanged(nameof(IsQuantityBlank));
        OnPropertyChanged(nameof(NotesCounterText));
    }

    /// <summary>
    /// Restores the default order values, asking first when that would discard changes.
    /// </summary>
    [RelayCommand]
    private void ResetToDefaults()
    {
        bool matchesDefaults = Draft.MatchesDefaults() && QuantityInput == OrderInputRules.FormatQuantity(Draft.Quantity);
        if (!matchesDefaults && !confirmation.Confirm(
                localizer[nameof(Strings.Reset_ConfirmTitle)], localizer[nameof(Strings.Reset_ConfirmMessage)]))
        {
            return;
        }

        Draft.ResetToDefaults();
        ResetInput();
    }

    private bool SetInput(ref string field, string? value, [CallerMemberName] string propertyName = "")
    {
        if (!SetProperty(ref field, value ?? string.Empty, validate: true, propertyName))
        {
            return false;
        }

        UpdateInputValidity();
        return true;
    }

    // Side-effect free: no validation messages are shown for fields the user has not touched yet.
    private void UpdateInputValidity() =>
        IsInputValid = Validator.TryValidateObject(this, new ValidationContext(this), validationResults: null, validateAllProperties: true);

    // Fields that currently show an error are re-validated so the message is re-created in the new language; untouched
    // fields stay unvalidated.
    private void OnLocalizerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(Localizer.Language))
        {
            return;
        }

        RevalidateIfInvalid(ProjectNameInput, nameof(ProjectNameInput));
        RevalidateIfInvalid(PostcodeInput, nameof(PostcodeInput));
        RevalidateIfInvalid(QuantityInput, nameof(QuantityInput));
        RevalidateIfInvalid(NotesInput, nameof(NotesInput));
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
            case nameof(ProjectNameInput):
                OnPropertyChanged(nameof(HasProjectNameErrors));
                break;
            case nameof(PostcodeInput):
                OnPropertyChanged(nameof(HasPostcodeErrors));
                break;
            case nameof(QuantityInput):
                OnPropertyChanged(nameof(HasQuantityErrors));
                break;
        }
    }
}
