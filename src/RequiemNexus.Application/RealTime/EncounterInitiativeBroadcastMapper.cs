using RequiemNexus.Data.Models;
using RequiemNexus.Data.RealTime;

namespace RequiemNexus.Application.RealTime;

/// <summary>
/// Maps a loaded <see cref="CombatEncounter"/> to wire-safe <see cref="InitiativeEntryDto"/> values
/// (masked NPC names do not leak the true name on the SignalR payload).
/// </summary>
public static class EncounterInitiativeBroadcastMapper
{
    /// <summary>
    /// Builds initiative DTOs in encounter <see cref="InitiativeEntry.Order"/> sequence.
    /// </summary>
    /// <param name="encounter">Encounter with initiative entries (and character names when applicable).</param>
    /// <returns>Ordered DTO list suitable for Redis and SignalR.</returns>
    public static IReadOnlyList<InitiativeEntryDto> Map(CombatEncounter encounter)
    {
        List<InitiativeEntry> sorted = encounter.InitiativeEntries.OrderBy(e => e.Order).ToList();
        InitiativeEntry? current = sorted.FirstOrDefault(e => !e.HasActed);
        int round = encounter.CurrentRound <= 0 ? 1 : encounter.CurrentRound;

        return sorted
            .Select(e => MapEntry(e, current?.Id == e.Id, round))
            .ToList();
    }

    private static InitiativeEntryDto MapEntry(InitiativeEntry entry, bool isActiveTurn, int round)
    {
        string realName = entry.Character?.Name ?? entry.NpcName ?? "Unknown";
        string wireName = BuildWireName(entry, realName);

        return new InitiativeEntryDto(
            entry.CharacterId,
            wireName,
            entry.Total,
            isActiveTurn,
            entry.CharacterId == null,
            entry.Order,
            entry.HasActed,
            round,
            wireName,
            entry.IsRevealed,
            entry.Id);
    }

    private static string BuildWireName(InitiativeEntry entry, string realName)
    {
        if (entry.CharacterId != null)
        {
            return realName;
        }

        if (entry.IsRevealed)
        {
            return realName;
        }

        return string.IsNullOrWhiteSpace(entry.MaskedDisplayName) ? "Unknown" : entry.MaskedDisplayName.Trim();
    }
}
