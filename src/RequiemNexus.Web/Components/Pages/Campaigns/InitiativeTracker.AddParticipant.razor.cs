using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using RequiemNexus.Application.Contracts;

namespace RequiemNexus.Web.Components.Pages.Campaigns;

public partial class InitiativeTracker
{
    private bool CanAddParticipant()
    {
        return _addType switch
        {
            "character" => _addCharacterId > 0,
            "npc" => !string.IsNullOrWhiteSpace(_addNpcName),
            "chronicle" => _addChronicleNpcId > 0 && ChronicleNpcsAvailableForEncounter.Any(),
            _ => false,
        };
    }

    private void OnAddTypeChanged()
    {
        _addError = string.Empty;
        _addChronicleNpcId = 0;
        _addNpcMaxVitae = 0;
        _addChronicleTracksVitae = false;
    }

    private async Task OnTrackerChronicleSelectChanged(ChangeEventArgs e)
    {
        _addError = string.Empty;
        string? raw = e.Value?.ToString();
        _addChronicleNpcId = int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int id)
            ? id
            : 0;

        if (_addChronicleNpcId == 0 || string.IsNullOrEmpty(_currentUserId))
        {
            return;
        }

        ChronicleNpcEncounterPrepDto? prep =
            await EncounterPrepService.GetChronicleNpcEncounterPrepAsync(_addChronicleNpcId, _currentUserId);
        if (prep != null)
        {
            _addInitMod = prep.SuggestedInitiativeMod;
            _addNpcHealthBoxes = prep.SuggestedHealthBoxes;
            _addNpcMaxWillpower = prep.SuggestedMaxWillpower;
            _addChronicleTracksVitae = prep.TracksVitae;
            _addNpcMaxVitae = prep.TracksVitae ? prep.SuggestedMaxVitae : 0;
        }
    }

    private async Task AddParticipant()
    {
        if (!CanAddParticipant() || _busy)
        {
            return;
        }

        _busy = true;
        _addError = string.Empty;
        try
        {
            if (_addType == "character")
            {
                await EncounterParticipantService.AddCharacterToEncounterAsync(
                    EncounterId, _addCharacterId, _addInitMod, _addRoll, _currentUserId!);
            }
            else if (_addType == "npc")
            {
                await EncounterParticipantService.AddNpcToEncounterAsync(
                    EncounterId,
                    _addNpcName.Trim(),
                    _addInitMod,
                    _addRoll,
                    _currentUserId!,
                    _addNpcHealthBoxes,
                    _addNpcMaxWillpower);
            }
            else
            {
                await EncounterParticipantService.AddNpcToEncounterFromChronicleNpcAsync(
                    EncounterId,
                    _addChronicleNpcId,
                    _addInitMod,
                    _addRoll,
                    _addNpcHealthBoxes,
                    _addNpcMaxWillpower,
                    _addNpcMaxVitae,
                    _currentUserId!);
            }

            _addCharacterId = 0;
            _addNpcName = string.Empty;
            _addChronicleNpcId = 0;
            _addInitMod = 0;
            _addRoll = 1;
            _addNpcHealthBoxes = 7;
            _addNpcMaxWillpower = 4;
            _addNpcMaxVitae = 0;
            _addChronicleTracksVitae = false;

            await LoadEncounter();
        }
        catch (Exception ex)
        {
            _addError = ex.Message;
        }
        finally
        {
            _busy = false;
        }
    }
}
