using System.Globalization;
using Altium.PcbManufacturing.Localization;
using Altium.PcbManufacturing.Models;
using Altium.PcbManufacturing.ViewModels;

namespace Altium.PcbManufacturing.Tests;

[Collection(UiCultureCollection.Name)]
public sealed class LocalizerTests : UiCultureTest
{
    private readonly Localizer localizer = new();

    [Fact]
    public void New_DefaultsToEnglish()
    {
        Assert.Equal(AppLanguage.English, localizer.Language);
        Assert.Equal("en", localizer.Culture.Name);
        Assert.Equal("en", Strings.Culture.Name);
        Assert.Equal("Preferences", localizer[nameof(Strings.Tab_Preferences)]);
    }

    [Fact]
    public void SetLanguage_Russian_ReturnsRussianStrings()
    {
        localizer.SetLanguage(AppLanguage.Russian);

        Assert.Equal("Параметры", localizer[nameof(Strings.Tab_Preferences)]);
        Assert.Equal("Сводка", localizer[nameof(Strings.Summary_Title)]);
    }

    [Fact]
    public void SetLanguage_Russian_SwitchesGeneratedResourceClass()
    {
        localizer.SetLanguage(AppLanguage.Russian);

        Assert.Equal("ru", Strings.Culture.Name);
        Assert.Equal("Введите почтовый индекс.", Strings.Validation_PostcodeRequired);
    }

    [Fact]
    public void SetLanguage_BackToEnglish_ReturnsEnglishStrings()
    {
        localizer.SetLanguage(AppLanguage.Russian);

        localizer.SetLanguage(AppLanguage.English);

        Assert.Equal("Summary", localizer[nameof(Strings.Summary_Title)]);
    }

    [Fact]
    public void Indexer_MissingKey_ReturnsKeyInsteadOfFailing() =>
        Assert.Equal("Does_Not_Exist", localizer["Does_Not_Exist"]);

    [Fact]
    public void Find_MissingKey_ReturnsNull() => Assert.Null(localizer.Find("Does_Not_Exist"));

    [Fact]
    public void SetLanguage_RaisesLanguageCultureAndIndexerNotifications()
    {
        List<string?> changed = [];
        localizer.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        localizer.SetLanguage(AppLanguage.Russian);

        Assert.Equal([nameof(Localizer.Language), nameof(Localizer.Culture), "Item[]"], changed);
    }

    [Fact]
    public void SetLanguage_SameLanguage_DoesNotNotify()
    {
        List<string?> changed = [];
        localizer.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        localizer.SetLanguage(AppLanguage.English);

        Assert.Empty(changed);
    }

    [Theory]
    [InlineData(1, "1 day")]
    [InlineData(2, "2 days")]
    [InlineData(12, "12 days")]
    public void FormatDays_English_UsesSingularOnlyForOne(int days, string expected) =>
        Assert.Equal(expected, localizer.FormatDays(days));

    [Theory]
    [InlineData(1, "1 день")]
    [InlineData(2, "2 дня")]
    [InlineData(4, "4 дня")]
    [InlineData(5, "5 дней")]
    [InlineData(7, "7 дней")]
    [InlineData(11, "11 дней")]
    [InlineData(12, "12 дней")]
    [InlineData(14, "14 дней")]
    [InlineData(21, "21 день")]
    [InlineData(22, "22 дня")]
    [InlineData(25, "25 дней")]
    [InlineData(111, "111 дней")]
    public void FormatDays_Russian_UsesRussianPluralForms(int days, string expected)
    {
        localizer.SetLanguage(AppLanguage.Russian);

        Assert.Equal(expected, localizer.FormatDays(days));
    }

    [Fact]
    public void Format_UsesTheUiLanguageForNumbers()
    {
        Assert.Equal("1.6 mm", localizer.Format(nameof(Strings.Thickness_Millimeters), 1.6m));

        localizer.SetLanguage(AppLanguage.Russian);

        Assert.Equal("1,6 мм", localizer.Format(nameof(Strings.Thickness_Millimeters), 1.6m));
    }

    [Fact]
    public void SetLanguage_DoesNotAffectPricesPostcodeRuleOrFormattingCulture()
    {
        CultureInfo formattingCulture = CultureInfo.CurrentCulture;

        localizer.SetLanguage(AppLanguage.Russian);

        Assert.Same(formattingCulture, CultureInfo.CurrentCulture);
        Assert.Equal("$1,234.50", UsdFormat.Format(1234.5m));
        Assert.Equal("+$208.60", UsdFormat.FormatChange(208.6m));
        Assert.True(PolishPostcode.IsValid("55-220"));
        Assert.False(PolishPostcode.IsValid("55220"));
    }

    [Fact]
    public void SupportedLanguages_AreEnglishAndRussianWithNativeNames() =>
        Assert.Equal(
            [new LanguageOption(AppLanguage.English, "EN", "English"), new LanguageOption(AppLanguage.Russian, "RU", "Русский")],
            Localizer.SupportedLanguages);
}
