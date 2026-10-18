using Altium.PcbManufacturing.Services;

namespace Altium.PcbManufacturing.Tests;

/// <summary>
/// Records confirmation requests and answers them with <see cref="Answer"/>.
/// </summary>
internal sealed class FakeConfirmationService : IConfirmationService
{
    public bool Answer { get; set; } = true;

    public List<(string Title, string Message)> Requests { get; } = [];

    public bool Confirm(string title, string message)
    {
        Requests.Add((title, message));
        return Answer;
    }
}
