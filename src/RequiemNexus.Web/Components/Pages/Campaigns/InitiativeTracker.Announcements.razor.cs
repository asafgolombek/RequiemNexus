using RequiemNexus.Data.Models;
using RequiemNexus.Data.RealTime;

namespace RequiemNexus.Web.Components.Pages.Campaigns;

public partial class InitiativeTracker
{
    private async Task AnnounceInitiativeTurnIfChangedAsync()
    {
        if (_encounter == null || _accessDenied)
        {
            return;
        }

        InitiativeEntry? actor = CurrentActor;
        if (actor == null)
        {
            return;
        }

        if (!_initiativeAnnouncePrimed)
        {
            _initiativeAnnouncePrimed = true;
            _lastAnnouncedInitiativeEntryId = actor.Id;
            return;
        }

        if (_lastAnnouncedInitiativeEntryId == actor.Id)
        {
            return;
        }

        _lastAnnouncedInitiativeEntryId = actor.Id;
        string label = actor.Character?.Name ?? actor.NpcName ?? actor.MaskedDisplayName ?? "Participant";
        await Announcer.AnnounceAsync($"Initiative order updated — {label} is now active.");
    }

    private async Task AnnounceConditionDeltasAsync(CharacterUpdateDto patch)
    {
        if (patch.ActiveConditions == null)
        {
            return;
        }

        HashSet<string> incoming = patch.ActiveConditions.ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!_conditionNamesByCharacterId.TryGetValue(patch.CharacterId, out HashSet<string>? prior))
        {
            _conditionNamesByCharacterId[patch.CharacterId] =
                new HashSet<string>(incoming, StringComparer.OrdinalIgnoreCase);
            return;
        }

        foreach (string name in incoming)
        {
            if (!prior.Contains(name))
            {
                await Announcer.AnnounceAsync($"{name} applied to {ResolveCharacterLabel(patch.CharacterId)}");
            }
        }

        _conditionNamesByCharacterId[patch.CharacterId] =
            new HashSet<string>(incoming, StringComparer.OrdinalIgnoreCase);
    }

    private string ResolveCharacterLabel(int characterId)
    {
        if (_encounter?.InitiativeEntries == null)
        {
            return "a character";
        }

        InitiativeEntry? row = _encounter.InitiativeEntries.FirstOrDefault(e => e.CharacterId == characterId);
        return row?.Character?.Name ?? row?.NpcName ?? row?.MaskedDisplayName ?? "a character";
    }
}
