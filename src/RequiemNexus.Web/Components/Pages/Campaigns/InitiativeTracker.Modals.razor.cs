using RequiemNexus.Data.Models;
using RequiemNexus.Domain.Enums;

namespace RequiemNexus.Web.Components.Pages.Campaigns;

public partial class InitiativeTracker
{
    private void OpenMeleeAttackModal(int attackerCharacterId)
    {
        _meleeAttackAttackerCharacterId = attackerCharacterId;
        _meleeAttackModalOpen = true;
    }

    private async Task OnMeleeAttackModalOpenChanged(bool open)
    {
        _meleeAttackModalOpen = open;
        if (!open)
        {
            await LoadEncounter(showFullPageSpinner: false);
        }
    }

    private void OpenPlayerWeaponDamageModal(int characterId)
    {
        _playerWeaponDamageCharacterId = characterId;
        _playerWeaponDamageModalOpen = true;
    }

    private Task OnPlayerWeaponDamageModalOpenChanged(bool open)
    {
        _playerWeaponDamageModalOpen = open;
        return Task.CompletedTask;
    }

    private Task OnNpcHealthTrackFromListAsync((int EntryId, string Track) args) =>
        OnNpcHealthTrackChangedAsync(args.EntryId, args.Track);

    private Task OpenPlayerWeaponDamageFromListAsync(int characterId)
    {
        OpenPlayerWeaponDamageModal(characterId);
        return Task.CompletedTask;
    }

    private async Task OnTiltSelectionFromListAsync((int CharacterId, TiltType Tilt) args)
    {
        _tiltSelections[args.CharacterId] = args.Tilt;
        await InvokeAsync(StateHasChanged);
    }

    private async Task RequestDefeatConfirmAsync(int entryId)
    {
        _defeatConfirmEntryId = entryId;
        await InvokeAsync(StateHasChanged);
    }

    private Task OpenMeleeAttackFromListAsync(int attackerCharacterId)
    {
        OpenMeleeAttackModal(attackerCharacterId);
        return Task.CompletedTask;
    }
}
