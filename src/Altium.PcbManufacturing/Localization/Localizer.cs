using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Altium.PcbManufacturing.Localization;

/// <summary>
/// The user interface languages.
/// </summary>
public enum AppLanguage
{
    English,
    Russian,
}

/// <summary>
/// A selectable language: its short <paramref name="Code"/> for the header and its name in that language.
/// </summary>
public sealed record LanguageOption(AppLanguage Language, string Code, string NativeName);

/// <summary>
/// The current user interface language and its strings from <c>Strings.resx</c> (English) and <c>Strings.ru.resx</c>.
/// </summary>
/// <remarks>
/// Only the UI language changes: <see cref="Strings.Culture"/> selects the resources (also for DataAnnotations
/// messages) and <see cref="Culture"/> formats localized text. <see cref="CultureInfo.CurrentCulture"/> is never
/// touched, so prices and the postcode rule are unaffected.
/// </remarks>
public sealed class Localizer : ObservableObject
{
    private static readonly CultureInfo englishCulture = CultureInfo.GetCultureInfo("en");
    private static readonly CultureInfo russianCulture = CultureInfo.GetCultureInfo("ru");

    public Localizer()
    {
        Apply(AppLanguage.English);
    }

    public static Localizer Instance { get; } = new();

    public static IReadOnlyList<LanguageOption> SupportedLanguages { get; } =
    [
        new(AppLanguage.English, "EN", "English"),
        new(AppLanguage.Russian, "RU", "Русский"),
    ];

    public AppLanguage Language { get; private set; }

    public CultureInfo Culture { get; private set; } = englishCulture;

    /// <summary>
    /// The string for <paramref name="key"/> in the current language, falling back to English and then to the key
    /// itself, so a missing resource is visible but never breaks the UI.
    /// </summary>
    public string this[string key] => Strings.ResourceManager.GetString(key, Culture) ?? key;

    /// <summary>
    /// The string for <paramref name="key"/>, or <see langword="null"/> when no language defines it.
    /// </summary>
    public string? Find(string key) => Strings.ResourceManager.GetString(key, Culture);

    /// <summary>
    /// Formats the string for <paramref name="key"/> with the current language's culture.
    /// </summary>
    public string Format(string key, params object?[] args) => string.Format(Culture, this[key], args);

    /// <summary>
    /// The resource key of a catalog entry's translated name: <c>{prefix}_{id}</c> with hyphens replaced by <c>_</c>, the
    /// only character used in catalog ids that is not valid in resource names, e.g. <c>SurfaceFinish_hasl_lead_free</c>.
    /// </summary>
    public static string CatalogKey(string prefix, string id) => $"{prefix}_{id.Replace('-', '_')}";

    public void SetLanguage(AppLanguage language)
    {
        if (language == Language)
        {
            return;
        }

        Apply(language);
        OnPropertyChanged(nameof(Language));
        OnPropertyChanged(nameof(Culture));
        OnPropertyChanged("Item[]");
    }

    private void Apply(AppLanguage language)
    {
        Language = language;
        Culture = language == AppLanguage.Russian ? russianCulture : englishCulture;
        Strings.Culture = Culture;
    }
}
