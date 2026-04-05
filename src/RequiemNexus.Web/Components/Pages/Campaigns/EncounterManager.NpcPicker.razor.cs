using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Data.Models;
using RequiemNexus.Web.Enums;

namespace RequiemNexus.Web.Components.Pages.Campaigns;

/// <summary>
/// Partial: NPC picker modal — stat block, chronicle NPC, and improvised flows for draft and live encounters.
/// </summary>
public partial class EncounterManager
{
    private async Task OpenNpcPickerAsync(int encounterId, bool isDraft, string mode)
    {
        _activeEncounterForNpc = encounterId;
        _npcPickerIsDraft = isDraft;
        _npcPickerMode = mode;
        _selectedBlockId = 0;
        _selectedChronicleNpcId = 0;
        _npcInitMod = 0;
        _npcRoll = 1;
        _chronicleHealthBoxes = 7;
        _chronicleMaxWillpower = 4;
        _chronicleMaxVitae = 0;
        _chronicleTracksVitae = false;
        _chroniclePrepHint = null;
        _chronicleAddError = string.Empty;
        _improvName = string.Empty;
        _improvHealthBoxes = 7;
        _improvMaxWillpower = 4;
        _improvError = string.Empty;
        _excludedChronicleNpcIds.Clear();

        if (isDraft)
        {
            CombatEncounter? draftEnc = _encounters.FirstOrDefault(e => e.Id == encounterId);
            if (draftEnc?.NpcTemplates != null)
            {
                foreach (EncounterNpcTemplate t in draftEnc.NpcTemplates)
                {
                    if (t.ChronicleNpcId is int cid)
                    {
                        _ = _excludedChronicleNpcIds.Add(cid);
                    }
                }
            }
        }
        else if (!string.IsNullOrEmpty(_currentUserId))
        {
            CombatEncounter? live = await EncounterQueryService.GetEncounterAsync(encounterId, _currentUserId);
            if (live?.NpcTemplates != null)
            {
                foreach (EncounterNpcTemplate t in live.NpcTemplates)
                {
                    if (t.ChronicleNpcId is int cid)
                    {
                        _ = _excludedChronicleNpcIds.Add(cid);
                    }
                }
            }

            if (live?.InitiativeEntries != null)
            {
                foreach (InitiativeEntry row in live.InitiativeEntries)
                {
                    if (row.ChronicleNpcId is int iid)
                    {
                        _ = _excludedChronicleNpcIds.Add(iid);
                    }
                }
            }
        }
    }

    private void CloseNpcPicker()
    {
        _activeEncounterForNpc = null;
        _npcPickerMode = null;
        _chroniclePrepHint = null;
        _chronicleAddError = string.Empty;
        _improvError = string.Empty;
    }

    private Task CloseNpcPickerAsync()
    {
        CloseNpcPicker();
        return Task.CompletedTask;
    }

    private void OnNpcModalKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Escape")
        {
            CloseNpcPicker();
        }
    }

    private Task OnNpcModalKeyDownAsync(KeyboardEventArgs e)
    {
        OnNpcModalKeyDown(e);
        return Task.CompletedTask;
    }

    private string GetNpcModalTitle() =>
        _npcPickerMode switch
        {
            _sourcePicker => "Add NPC",
            _statPicker => "Add NPC — stat block",
            _chroniclePicker => "Add NPC — Danse Macabre",
            _improvisedPicker => "Add NPC — Improvised",
            _ => "Add NPC",
        };

    private async Task OnChronicleNpcSelectChanged(ChangeEventArgs e)
    {
        _chronicleAddError = string.Empty;
        string? raw = e.Value?.ToString();
        _selectedChronicleNpcId = int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int id)
            ? id
            : 0;
        await RefreshChroniclePrepAsync();
    }

    private async Task RefreshChroniclePrepAsync()
    {
        _chroniclePrepHint = null;
        if (_selectedChronicleNpcId == 0 || string.IsNullOrEmpty(_currentUserId))
        {
            return;
        }

        ChronicleNpcEncounterPrepDto? prep =
            await EncounterPrepService.GetChronicleNpcEncounterPrepAsync(_selectedChronicleNpcId, _currentUserId);
        if (prep == null)
        {
            return;
        }

        _npcInitMod = prep.SuggestedInitiativeMod;
        _chronicleHealthBoxes = prep.SuggestedHealthBoxes;
        _chronicleMaxWillpower = prep.SuggestedMaxWillpower;
        _chronicleTracksVitae = prep.TracksVitae;
        _chronicleMaxVitae = prep.TracksVitae ? prep.SuggestedMaxVitae : 0;
        string vitaeHint = prep.TracksVitae ? $", vitae {prep.SuggestedMaxVitae} (Blood Potency)." : string.Empty;
        _chroniclePrepHint = string.IsNullOrEmpty(prep.LinkedStatBlockName)
            ? $"Suggested from sheet: mod {prep.SuggestedInitiativeMod} (Wits + Composure), health {prep.SuggestedHealthBoxes}, willpower {prep.SuggestedMaxWillpower} (Resolve + Composure){vitaeHint}"
            : $"Linked stat block \"{prep.LinkedStatBlockName}\": mod {prep.SuggestedInitiativeMod}, health {prep.SuggestedHealthBoxes}, willpower {prep.SuggestedMaxWillpower}{vitaeHint}";
    }

    private async Task AddNpcFromChronicle()
    {
        _chronicleAddError = string.Empty;
        if (_selectedChronicleNpcId == 0)
        {
            _chronicleAddError = "Select a chronicle NPC from the list.";
            return;
        }

        if (!_activeEncounterForNpc.HasValue || _busy || string.IsNullOrEmpty(_currentUserId))
        {
            return;
        }

        _busy = true;
        try
        {
            if (_npcPickerIsDraft)
            {
                await EncounterPrepService.AddNpcTemplateFromChronicleNpcAsync(
                    _activeEncounterForNpc.Value,
                    _selectedChronicleNpcId,
                    _npcInitMod,
                    _chronicleHealthBoxes,
                    _chronicleMaxWillpower,
                    _chronicleMaxVitae,
                    isRevealed: true,
                    defaultMaskedName: null,
                    storyTellerUserId: _currentUserId);
            }
            else
            {
                await EncounterParticipantService.AddNpcToEncounterFromChronicleNpcAsync(
                    _activeEncounterForNpc.Value,
                    _selectedChronicleNpcId,
                    _npcInitMod,
                    _npcRoll,
                    _chronicleHealthBoxes,
                    _chronicleMaxWillpower,
                    _chronicleMaxVitae,
                    _currentUserId);
            }

            CloseNpcPicker();
            await LoadEncounters();
        }
        catch (Exception ex)
        {
            ToastService.Show("Encounter", ex.Message, ToastType.Error);
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task AddNpcFromStatBlock()
    {
        if (_selectedBlockId == 0 || !_activeEncounterForNpc.HasValue || _busy)
        {
            return;
        }

        _busy = true;
        try
        {
            NpcStatBlock? block = await NpcStatBlockService.GetBlockAsync(_selectedBlockId);
            if (block is null)
            {
                return;
            }

            int hp = Math.Max(1, block.Health);
            int wp = Math.Max(1, block.Willpower);

            if (_npcPickerIsDraft)
            {
                await EncounterPrepService.AddNpcTemplateAsync(
                    _activeEncounterForNpc.Value,
                    block.Name,
                    _npcInitMod,
                    hp,
                    wp,
                    null,
                    true,
                    null,
                    _currentUserId!);
            }
            else
            {
                await EncounterParticipantService.AddNpcToEncounterAsync(
                    _activeEncounterForNpc.Value,
                    block.Name,
                    _npcInitMod,
                    _npcRoll,
                    _currentUserId!,
                    hp,
                    wp);
            }

            CloseNpcPicker();
            await LoadEncounters();
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task AddImprovisedNpc()
    {
        if (!_activeEncounterForNpc.HasValue || _busy || string.IsNullOrEmpty(_currentUserId))
        {
            return;
        }

        _improvError = string.Empty;
        if (string.IsNullOrWhiteSpace(_improvName))
        {
            _improvError = "Name is required.";
            return;
        }

        if (_improvHealthBoxes < 1 || _improvHealthBoxes > 50)
        {
            _improvError = "Health must be between 1 and 50.";
            return;
        }

        if (_improvMaxWillpower < 1 || _improvMaxWillpower > 20)
        {
            _improvError = "Willpower must be between 1 and 20.";
            return;
        }

        _busy = true;
        try
        {
            await EncounterPrepService.AddNpcTemplateAsync(
                _activeEncounterForNpc.Value,
                _improvName.Trim(),
                0,
                _improvHealthBoxes,
                _improvMaxWillpower,
                null,
                true,
                null,
                _currentUserId);
            CloseNpcPicker();
            await LoadEncounters();
        }
        catch (Exception ex)
        {
            ToastService.Show("Encounter", ex.Message, ToastType.Error);
        }
        finally
        {
            _busy = false;
        }
    }
}
