namespace Altium.PcbManufacturing.Services;

/// <summary>
/// Asks the user a yes/no question, keeping dialog windows out of view models.
/// </summary>
public interface IConfirmationService
{
    /// <returns><see langword="true"/> when the user confirms.</returns>
    bool Confirm(string title, string message);
}
