using System.IO;
using Altium.PcbManufacturing.Localization;
using Altium.PcbManufacturing.Settings;

namespace Altium.PcbManufacturing.Tests;

/// <summary>
/// Persistence of the UI language and theme. Missing or damaged files fall back to defaults.
/// </summary>
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
    public void Save_ThenLoad_RestoresLanguageAndTheme()
    {
        new UiSettingsStore(FilePath).Save(new UiSettings(AppLanguage.Russian, AppTheme.Light));

        Assert.Equal(new UiSettings(AppLanguage.Russian, AppTheme.Light), new UiSettingsStore(FilePath).Load());
    }

    [Fact]
    public void Load_MissingOrDamagedFile_ReturnsDefaults()
    {
        Assert.Equal(UiSettings.Default, new UiSettingsStore(FilePath).Load());

        Write("not json");

        Assert.Equal(UiSettings.Default, new UiSettingsStore(FilePath).Load());
    }

    private void Write(string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        File.WriteAllText(FilePath, content);
    }
}
