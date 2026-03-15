using Microsoft.EntityFrameworkCore;
using RequiemNexus.Data;
using RequiemNexus.Data.RealTime;

namespace RequiemNexus.Application.RealTime;

/// <summary>
/// Implementation of SignalR hub authorization using the Masquerade pattern.
/// Ensures that only authorized players and Storytellers can interact with a session.
/// </summary>
public class SessionAuthorizationService(ApplicationDbContext db) : ISessionAuthorizationService
{
    /// <inheritdoc />
    public async Task<bool> IsMemberAsync(string userId, int chronicleId)
    {
        // 1. Identify: userId provided by hub
        // 2. Load: Check campaign membership
        // 3. Verify: Is the user the ST or a player?
        return await db.Campaigns
            .AnyAsync(c => c.Id == chronicleId && (c.StoryTellerId == userId || c.Characters.Any(p => p.ApplicationUserId == userId)));
    }

    /// <inheritdoc />
    public async Task<bool> IsStorytellerAsync(string userId, int chronicleId)
    {
        return await db.Campaigns
            .AnyAsync(c => c.Id == chronicleId && c.StoryTellerId == userId);
    }

    /// <inheritdoc />
    public async Task<bool> IsCharacterOwnerAsync(string userId, int characterId)
    {
        return await db.Characters
            .AnyAsync(c => c.Id == characterId && c.ApplicationUserId == userId);
    }

    /// <inheritdoc />
    public async Task<bool> IsCharacterOwnerInChronicleAsync(string userId, int characterId, int chronicleId)
    {
        return await db.Characters
            .AnyAsync(c => c.Id == characterId && c.ApplicationUserId == userId && c.CampaignId == chronicleId);
    }
}
