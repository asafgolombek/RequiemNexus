// Blazor partial: blood sorcery rite prep and extended activation rolls for CharacterDetails.
using RequiemNexus.Application.DTOs;
using RequiemNexus.Data.Models;
using RequiemNexus.Domain.Enums;
using RequiemNexus.Domain.Models;
using RequiemNexus.Domain.Services;
using RequiemNexus.Web.Enums;

namespace RequiemNexus.Web.Components.Pages;

public partial class CharacterDetails
{
    private static string TraditionLabel(SorceryType t) =>
        t switch
        {
            SorceryType.Cruac => "Crúac",
            SorceryType.Theban => "Theban",
            SorceryType.Necromancy => "Necromancy",
            _ => t.ToString(),
        };

    private void HandleRitePrepModalOpenChanged(bool open)
    {
        _isRitePrepModalOpen = open;
        if (!open)
        {
            _pendingRiteForPrep = null;
            _pendingRiteRequirements = [];
        }
    }

    private async Task OpenRiteRoller(CharacterRite cr)
    {
        if (_character == null || string.IsNullOrEmpty(_currentUserId))
        {
            return;
        }

        try
        {
            _pendingRiteForPrep = cr;
            Result<IReadOnlyList<RiteRequirement>> parsed =
                RiteRequirementValidator.ParseRequirements(cr.SorceryRiteDefinition?.RequirementsJson);
            _pendingRiteRequirements = parsed.IsSuccess ? parsed.Value! : [];
            _riteKinTargets = await CharacterService.GetCampaignKindredTargetsForRitesAsync(_character.Id, _currentUserId);
            _isRitePrepModalOpen = true;
        }
        catch (Exception ex)
        {
            ToastService.Show("Rite activation", ex.Message, ToastType.Error);
        }
    }

    private async Task HandleRitePrepContinueAsync(RiteActivationPrepResult prep)
    {
        CharacterRite? cr = _pendingRiteForPrep;
        if (cr == null || _character == null || string.IsNullOrEmpty(_currentUserId))
        {
            return;
        }

        try
        {
            await ExecuteRiteActivationRollAsync(cr, prep);
        }
        catch (Exception ex)
        {
            ToastService.Show("Rite activation", ex.Message, ToastType.Error);
        }
    }

    private async Task ExecuteRiteActivationRollAsync(CharacterRite cr, RiteActivationPrepResult prep)
    {
        if (_character == null || string.IsNullOrEmpty(_currentUserId))
        {
            return;
        }

        var def = cr.SorceryRiteDefinition;

        var request = new BeginRiteActivationRequest(
            AcknowledgePhysicalSacrament: prep.AcknowledgePhysicalSacrament,
            AcknowledgeHeart: prep.AcknowledgeHeart,
            AcknowledgeMaterialOffering: prep.AcknowledgeMaterialOffering,
            AcknowledgeMaterialFocus: prep.AcknowledgeMaterialFocus,
            ExtraVitae: prep.ExtraVitae,
            TargetCharacterId: prep.TargetCharacterId);

        BeginRiteActivationResult activation = await SorceryActivationService.BeginRiteActivationAsync(
            _character.Id,
            cr.Id,
            _currentUserId,
            request);
        _character = await CharacterService.ReloadCharacterAsync(_character.Id, _currentUserId);
        if (activation.NecromancyDegenerationCheckRaised && _character != null)
        {
            string chronicleNote = _character.CampaignId.HasValue
                ? "Your Storyteller's Glimpse dashboard has been updated with a pending degeneration alert for this character."
                : "Join a chronicle so your Storyteller can receive the pending degeneration alert on the Glimpse dashboard.";
            ToastService.Show(
                "Necromancy breaking point",
                "At Humanity 7 or higher, using Kindred Necromancy calls for a degeneration check. " + chronicleNote,
                ToastType.Info,
                8000);
        }

        _rollerTraitName = cr.SorceryRiteDefinition?.Name ?? "Rite";
        _rollerBaseDice = activation.DicePool;
        _rollerFixedDicePool = activation.DicePool;
        _rollerRiteMaxRolls = activation.MaxExtendedRolls;
        _rollerRiteTargetSuccesses = activation.TargetSuccesses;
        _rollerRiteMinutesPerRoll = activation.MinutesPerRoll;
        _rollerRiteRitualDisciplineDots = activation.RitualDisciplineDots;
        _rollerRiteSorceryType = def?.SorceryType;
        _isRollerOpen = true;
    }
}
