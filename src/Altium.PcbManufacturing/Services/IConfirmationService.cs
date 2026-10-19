namespace Altium.PcbManufacturing.Services;

/// <summary>
/// Asks the user a yes/no question, keeping dialog windows out of view models.
/// </summary>
public interface IConfirmationService
{
    /// <returns><see langword="true"/> when the user confirms.</returns>
    bool Confirm(string title, string message);

    /// <summary>
    /// Asks without blocking the caller: for asynchronous commands, which stay in their running state (and cannot be
    /// executed again) until the user has answered.
    /// </summary>
    /// <returns><see langword="true"/> when the user confirms.</returns>
    Task<bool> ConfirmAsync(string title, string message);
}
