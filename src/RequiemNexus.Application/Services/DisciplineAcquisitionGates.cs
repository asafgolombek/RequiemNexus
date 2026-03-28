using RequiemNexus.Data.Models;
using RequiemNexus.Data.Models.Enums;

namespace RequiemNexus.Application.Services;

/// <summary>
/// Shared discipline acquisition rules for character creation and post-chargen purchases.
/// </summary>
public static class DisciplineAcquisitionGates
{
    /// <summary>
    /// Returns a player-facing error when the character cannot select this discipline at chargen (no Storyteller overrides).
    /// </summary>
    /// <param name="character">The in-progress character; <see cref="Character.Clan"/> should be populated for clan checks.</param>
    /// <param name="discipline">The discipline row with covenant/bloodline navigations when applicable.</param>
    /// <returns>Null when the selection is allowed; otherwise a failure message.</returns>
    public static string? GetCharacterCreationBlockingReason(Character character, Discipline discipline)
    {
        // Gate 1 — Bloodline-only disciplines: no bloodline is chosen at embrace.
        if (discipline.IsBloodlineDiscipline && discipline.BloodlineId.HasValue)
        {
            string blName = discipline.Bloodline?.Name ?? "the required bloodline";
            return $"This Discipline is restricted to members of the {blName} bloodline.";
        }

        // Gate 2 — Covenant disciplines: new characters are unaligned until post-creation play.
        if (discipline.IsCovenantDiscipline && discipline.CovenantId.HasValue
            && !HasApprovedCovenantMembership(character, discipline.CovenantId.Value))
        {
            string covName = discipline.Covenant?.Name ?? "the covenant";
            return $"Covenant Status in {covName} is required to select this Discipline at character creation.";
        }

        // Gate 4 — Mentor Vitae: no ST acknowledgment at chargen.
        if (discipline.RequiresMentorBloodToLearn && !character.IsDisciplineInClan(discipline.Id))
        {
            return "Out-of-clan Disciplines require a teacher and must drink their Vitae. Storyteller must acknowledge.";
        }

        // Gate 6 — Necromancy: only Mekhet at chargen (bloodlines are not selected during embrace).
        if (discipline.IsNecromancy
            && !string.Equals(character.Clan?.Name, "Mekhet", StringComparison.Ordinal))
        {
            return "Necromancy requires Mekhet-clan membership at character creation, or join a Necromancy-linked bloodline after play begins.";
        }

        return null;
    }

    /// <summary>
    /// Returns true when the character has approved membership in the given covenant (not pending application).
    /// </summary>
    public static bool HasApprovedCovenantMembership(Character character, int requiredCovenantId)
    {
        if (character.CovenantId != requiredCovenantId)
        {
            return false;
        }

        if (character.CovenantJoinStatus == CovenantJoinStatus.Pending)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Gate 1 — Bloodline restriction (acquisition). Returns an error message or null.
    /// </summary>
    public static string? TryBloodlineGateAcquisition(Character character, Discipline discipline)
    {
        if (discipline.IsBloodlineDiscipline && discipline.BloodlineId.HasValue)
        {
            bool hasBloodline = character.Bloodlines.Any(b =>
                b.Status == BloodlineStatus.Active && b.BloodlineDefinitionId == discipline.BloodlineId.Value);

            if (!hasBloodline)
            {
                string blName = discipline.Bloodline?.Name ?? "the required bloodline";
                return $"This Discipline is restricted to members of the {blName} bloodline.";
            }
        }

        return null;
    }

    /// <summary>
    /// Gate 2 — Covenant status (acquisition). Returns an error message or null; may append to <paramref name="auditSegments"/> when ST overrides.
    /// </summary>
    public static string? TryCovenantGateAcquisition(
        Character character,
        Discipline discipline,
        bool stAcknowledged,
        List<string> auditSegments)
    {
        if (discipline.IsCovenantDiscipline && discipline.CovenantId.HasValue)
        {
            if (!HasApprovedCovenantMembership(character, discipline.CovenantId.Value))
            {
                if (!stAcknowledged)
                {
                    string covName = discipline.Covenant?.Name ?? "the covenant";
                    return $"Covenant Status in {covName} is required. A Storyteller may override for 'stolen secrets'.";
                }

                auditSegments.Add("covenant");
            }
        }

        return null;
    }

    /// <summary>
    /// Gate 4 — Out-of-clan teacher + Vitae (acquisition).
    /// </summary>
    public static string? TryMentorBloodGateAcquisition(
        Character character,
        Discipline discipline,
        bool stAcknowledged,
        List<string> auditSegments)
    {
        if (discipline.RequiresMentorBloodToLearn && !character.IsDisciplineInClan(discipline.Id))
        {
            if (!stAcknowledged)
            {
                return "Out-of-clan Disciplines require a teacher and must drink their Vitae. Storyteller must acknowledge.";
            }

            auditSegments.Add("teacher");
        }

        return null;
    }

    /// <summary>
    /// Gate 6 — Necromancy cultural connection (acquisition).
    /// </summary>
    public static string? TryNecromancyGateAcquisition(
        Character character,
        Discipline discipline,
        bool stAcknowledged,
        List<string> auditSegments)
    {
        if (discipline.IsNecromancy)
        {
            bool isMekhet = string.Equals(character.Clan?.Name, "Mekhet", StringComparison.Ordinal);
            bool hasNecromancyBloodline = character.Bloodlines.Any(b =>
                b.Status == BloodlineStatus.Active
                && b.BloodlineDefinition?.FourthDisciplineId == discipline.Id);

            if (!isMekhet && !hasNecromancyBloodline)
            {
                if (!stAcknowledged)
                {
                    return "Necromancy requires Mekhet-clan membership, a Necromancy-linked bloodline, or ST-acknowledged cultural connection.";
                }

                auditSegments.Add("necromancy");
            }
        }

        return null;
    }
}
