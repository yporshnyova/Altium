using System.IO;

namespace Altium.PcbManufacturing.Tests;

/// <summary>
/// Locates application source files from the test output folder, for checks over XAML and resource files.
/// </summary>
internal static class SourceFiles
{
    private static readonly Lazy<string> projectDirectory = new(() =>
    {
        for (DirectoryInfo? directory = new(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(System.IO.Path.Combine(directory.FullName, "Altium.sln")))
            {
                return System.IO.Path.Combine(directory.FullName, "src", "Altium.PcbManufacturing");
            }
        }

        throw new InvalidOperationException("Altium.sln was not found above the test output folder.");
    });

    public static string Path(params string[] relativeParts) =>
        System.IO.Path.Combine([projectDirectory.Value, .. relativeParts]);

    public static IEnumerable<string> Xaml() =>
        Directory.EnumerateFiles(projectDirectory.Value, "*.xaml", SearchOption.AllDirectories)
            .Where(file => !file.Contains($"{System.IO.Path.DirectorySeparatorChar}obj{System.IO.Path.DirectorySeparatorChar}")
                && !file.Contains($"{System.IO.Path.DirectorySeparatorChar}bin{System.IO.Path.DirectorySeparatorChar}"));
}
