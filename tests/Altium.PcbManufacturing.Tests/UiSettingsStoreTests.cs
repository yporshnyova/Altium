using System.IO;
using Altium.PcbManufacturing.Localization;
using Altium.PcbManufacturing.Settings;

namespace Altium.PcbManufacturing.Tests;

public sealed class UiSettingsStoreTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), $"ui-settings-{Guid.NewGuid():N}");

    private string FilePath => Path.Combine(directory, "nested", "ui-settings.json");

    public void Dispose()
    {
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Default_IsEnglishAndLight() => Assert.Equal(new UiSettings(AppLanguage.English, AppTheme.Light), UiSettings.Default);

    [Fact]
    public void Load_MissingFile_ReturnsDefaults() => Assert.Equal(UiSettings.Default, new UiSettingsStore(FilePath).Load());

    [Fact]
    public void Save_ThenLoad_RestoresLanguageAndTheme()
    {
        UiSettingsStore store = new(FilePath);

        store.Save(new UiSettings(AppLanguage.Russian, AppTheme.Dark));

        Assert.Equal(new UiSettings(AppLanguage.Russian, AppTheme.Dark), new UiSettingsStore(FilePath).Load());
    }

    [Fact]
    public void Save_WritesReadableNamesAndCreatesTheFolder()
    {
        new UiSettingsStore(FilePath).Save(new UiSettings(AppLanguage.Russian, AppTheme.Dark));

        Assert.Equal("""{"language":"Russian","theme":"Dark"}""", File.ReadAllText(FilePath));
    }

    [Theory]
    [InlineData("""{"language":"russian","theme":"DARK"}""")]
    [InlineData("""{ "theme": "Dark", "language": "Russian", "extra": 1 }""")]
    public void Load_IsCaseInsensitiveAndIgnoresUnknownProperties(string json)
    {
        Write(json);

        Assert.Equal(new UiSettings(AppLanguage.Russian, AppTheme.Dark), new UiSettingsStore(FilePath).Load());
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("")]
    [InlineData("[]")]
    [InlineData("null")]
    public void Load_InvalidFile_ReturnsDefaults(string content)
    {
        Write(content);

        Assert.Equal(UiSettings.Default, new UiSettingsStore(FilePath).Load());
    }

    [Theory]
    [InlineData("""{"language":"Klingon","theme":"Dark"}""", AppLanguage.English, AppTheme.Dark)]
    [InlineData("""{"language":"Russian","theme":"Sepia"}""", AppLanguage.Russian, AppTheme.Light)]
    [InlineData("""{"language":"1","theme":"1"}""", AppLanguage.English, AppTheme.Light)]
    [InlineData("""{"language":1,"theme":true}""", AppLanguage.English, AppTheme.Light)]
    [InlineData("""{"theme":"Dark"}""", AppLanguage.English, AppTheme.Dark)]
    public void Load_InvalidValue_FallsBackForThatValueOnly(string json, AppLanguage language, AppTheme theme)
    {
        Write(json);

        Assert.Equal(new UiSettings(language, theme), new UiSettingsStore(FilePath).Load());
    }

    [Fact]
    public void Save_WhenThePathCannotBeWritten_DoesNotThrow()
    {
        // A directory occupies the file path, so writing the file fails.
        Directory.CreateDirectory(FilePath);

        new UiSettingsStore(FilePath).Save(new UiSettings(AppLanguage.Russian, AppTheme.Dark));

        Assert.Equal(UiSettings.Default, new UiSettingsStore(FilePath).Load());
    }

    [Fact]
    public void DefaultFilePath_IsPerUserLocalAppData() =>
        Assert.StartsWith(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            UiSettingsStore.DefaultFilePath,
            StringComparison.OrdinalIgnoreCase);

    private void Write(string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        File.WriteAllText(FilePath, content);
    }
}
