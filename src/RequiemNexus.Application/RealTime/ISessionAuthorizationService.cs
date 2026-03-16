namespace RequiemNexus.Application.RealTime;

/// <summary>
/// Encapsulates all authorization checks for SignalR hub methods.
/// Follows the Masquerade pattern: Identify -> Load -> Verify -> Proceed.
/// </summary>
public interface ISessionAuthorizationService
{
    /// <summary>
    /// Verifies that the user is a member of the chronicle.
    /// </summary>
    Task<bool> IsMemberAsync(string userId, int chronicleId);

    /// <summary>
    /// Verifies that the user is the Storyteller for the chronicle.
    /// </summary>
    Task<bool> IsStorytellerAsync(string userId, int chronicleId);

    /// <summary>
    /// Verifies that the user owns the specified character.
    /// </summary>
    Task<bool> IsCharacterOwnerAsync(string userId, int characterId);

    /// <summary>
    /// Verifies that the user owns the specified character and it belongs to the chronicle.
    /// </summary>
    Task<bool> IsCharacterOwnerInChronicleAsync(string userId, int characterId, int chronicleId);

    /// <summary>
    /// Verifies that the character belongs to the specified chronicle.
    /// </summary>
    Task<bool> IsCharacterInChronicleAsync(int characterId, int chronicleId);
}
