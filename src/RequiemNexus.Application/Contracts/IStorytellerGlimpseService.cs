using RequiemNexus.Application.DTOs;

namespace RequiemNexus.Application.Contracts;

/// <summary>
/// Storyteller-only operations: reading the vital snapshot of all campaign characters
/// and awarding Beats / XP on their behalf.
/// Every method throws <see cref="UnauthorizedAccessException"/> when the caller is not
/// the Storyteller of the specified campaign.
/// </summary>
public interface IStorytellerGlimpseService
{
    /// <summary>
    /// Returns the vital snapshot for every character enrolled in <paramref name="campaignId"/>.
    /// </summary>
    /// <param name="campaignId">Campaign to inspect.</param>
    /// <param name="storyTellerUserId">Must match <c>Campaign.StoryTellerId</c>.</param>
    Task<List<CharacterVitalsDto>> GetCampaignVitalsAsync(int campaignId, string storyTellerUserId);

    /// <summary>
    /// Awards one Beat to a single character and writes a <c>StorytellerAward</c> ledger entry.
    /// Handles the 5-Beat → 1-XP conversion automatically.
    /// </summary>
    /// <param name="campaignId">Campaign the character belongs to.</param>
    /// <param name="characterId">Target character.</param>
    /// <param name="reason">Human-readable justification shown in the ledger.</param>
    /// <param name="storyTellerUserId">Must match <c>Campaign.StoryTellerId</c>.</param>
    Task AwardBeatToCharacterAsync(int campaignId, int characterId, string reason, string storyTellerUserId);

    /// <summary>
    /// Awards one Beat to every character enrolled in the campaign (coterie award).
    /// Each award is written as a separate ledger entry.
    /// </summary>
    /// <param name="campaignId">Target campaign.</param>
    /// <param name="reason">Reason applied to every ledger entry.</param>
    /// <param name="storyTellerUserId">Must match <c>Campaign.StoryTellerId</c>.</param>
    Task AwardBeatToCampaignAsync(int campaignId, string reason, string storyTellerUserId);

    /// <summary>
    /// Grants <paramref name="amount"/> XP directly to a character and writes an
    /// <c>XpSource.StorytellerAward</c> ledger entry.
    /// </summary>
    /// <param name="campaignId">Campaign the character belongs to.</param>
    /// <param name="characterId">Target character.</param>
    /// <param name="amount">Positive XP amount to grant.</param>
    /// <param name="reason">Human-readable justification shown in the ledger.</param>
    /// <param name="storyTellerUserId">Must match <c>Campaign.StoryTellerId</c>.</param>
    Task AwardXpToCharacterAsync(int campaignId, int characterId, int amount, string reason, string storyTellerUserId);
}
