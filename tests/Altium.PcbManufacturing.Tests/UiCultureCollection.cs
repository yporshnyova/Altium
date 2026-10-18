using System.Globalization;
using Altium.PcbManufacturing.Localization;

namespace Altium.PcbManufacturing.Tests;

/// <summary>
/// Tests that switch the UI language. The resource culture (<see cref="Strings.Culture"/>) is process-wide, like the UI
/// language of a WPF application, so these tests run alone, after all parallel tests, and restore English afterwards.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class UiCultureCollection
{
    public const string Name = "UI culture";
}

/// <summary>
/// Base class for <see cref="UiCultureCollection"/> tests: restores the English resource culture after each test.
/// </summary>
public abstract class UiCultureTest : IDisposable
{
    public void Dispose()
    {
        Strings.Culture = CultureInfo.GetCultureInfo("en");
        GC.SuppressFinalize(this);
    }
}
