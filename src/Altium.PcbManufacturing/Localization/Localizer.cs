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
    /// A number of days with the grammatically correct word, e.g. "1 day" / "5 days", "1 день" / "3 дня" / "5 дней".
    /// </summary>
    public string FormatDays(int days) => Format(DaysKey(days), days);

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

    private string DaysKey(int days)
    {
        if (Language != AppLanguage.Russian)
        {
            return days == 1 ? nameof(Strings.Days_One) : nameof(Strings.Days_Many);
        }

        // Russian plural rules: 1, 21, 31… → день; 2–4, 22–24… → дня; everything else (incl. 11–14) → дней.
        int lastTwo = Math.Abs(days) % 100;
        int last = lastTwo % 10;
        return (lastTwo is >= 11 and <= 14) ? nameof(Strings.Days_Many)
            : last == 1 ? nameof(Strings.Days_One)
            : last is >= 2 and <= 4 ? nameof(Strings.Days_Few)
            : nameof(Strings.Days_Many);
    }
}
