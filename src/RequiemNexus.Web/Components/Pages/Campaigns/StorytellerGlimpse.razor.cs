using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Application.DTOs;
using RequiemNexus.Data.Models;
using RequiemNexus.Data.Models.Enums;
using RequiemNexus.Domain.Enums;
using RequiemNexus.Domain.Models;
using RequiemNexus.Web.Services;

namespace RequiemNexus.Web.Components.Pages.Campaigns;

#pragma warning disable SA1201
#pragma warning disable SA1202
#pragma warning disable SA1204
#pragma warning disable SA1214

public partial class StorytellerGlimpse
{
    /// <summary>Route parameter: campaign id.</summary>
    [Parameter]
    public int Id { get; set; }

    private List<CharacterVitalsDto> _vitals = [];
    private bool _loading = true;
    private bool _accessDenied;
    private bool _awarding;
    private string? _currentUserId;

    // Per-character form state
    private Dictionary<int, string> _beatReasons = [];
    private Dictionary<int, string> _xpReasons = [];
    private Dictionary<int, int> _xpAmounts = [];
    private Dictionary<int, string> _feedbackMessages = [];

    // Coterie award
    private string _coteReason = string.Empty;
    private string _coteMessage = string.Empty;

    // Pending bloodline requests
    private List<BloodlineApplicationDto> _pendingBloodlines = [];
    private int? _rejectingId;
    private string _rejectNote = string.Empty;

    // Pending covenant requests
    private List<CovenantApplicationDto> _pendingCovenants = [];
    private int? _rejectingCovenantCharacterId;
    private string _covenantRejectNote = string.Empty;

    // Pending rite learning requests
    private List<RiteApplicationDto> _pendingRites = [];
    private int? _rejectingRiteId;
    private string _riteRejectNote = string.Empty;

    // Pending Ordo Dracul: Chosen Mystery + Coil purchases
    private List<ChosenMysteryApplicationDto> _pendingChosenMysteries = [];
    private int? _rejectingChosenMysteryCharacterId;
    private List<CoilApplicationDto> _pendingCoils = [];
    private int? _rejectingCoilId;
    private string _coilRejectNote = string.Empty;

    // Social maneuvering (Phase 10)
    private List<SocialManeuver> _socialManeuvers = [];
    private List<ChronicleNpc> _npcs = [];
    private bool _socialBusy;
    private int _newManeuverInitiatorId;
    private int _newManeuverTargetNpcId;
    private string _newManeuverGoal = string.Empty;
    private bool _newManeuverBreakingPoint;
    private bool _newManeuverAspiration;
    private bool _newManeuverVirtueMask;
    private readonly Dictionary<int, int> _openDoorPoolByManeuverId = [];
    private readonly Dictionary<int, int> _forceDoorPoolByManeuverId = [];
    private readonly Dictionary<int, bool> _forceHardLeverageByManeuverId = [];
    private readonly Dictionary<int, int> _forceBpSeverityByManeuverId = [];
    private readonly Dictionary<int, int> _narrativeDoorsDraftByManeuverId = [];

    // Social maneuver — investigation clues (Phase 10.5)
    private int _investigationThresholdDraft = 3;
    private readonly Dictionary<int, int> _bankSuccessesByManeuverId = [];
    private readonly Dictionary<int, string> _manualClueSourceByManeuverId = [];
    private readonly Dictionary<int, ClueLeverageKind> _manualClueLeverageByManeuverId = [];
    private readonly Dictionary<int, string> _spendBenefitByClueId = [];

    protected override async Task OnInitializedAsync()
    {
        AuthenticationState authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        _currentUserId = authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(_currentUserId))
        {
            NavigationManager.NavigateTo("/login");
            return;
        }

        await LoadVitals();
    }

    private async Task LoadVitals()
    {
        _loading = true;
        try
        {
            _vitals = await GlimpseService.GetCampaignVitalsAsync(Id, _currentUserId!);
            _pendingBloodlines = await BloodlineService.GetPendingBloodlineApplicationsAsync(Id, _currentUserId!);
            _pendingCovenants = await CovenantService.GetPendingCovenantApplicationsAsync(Id, _currentUserId!);
            _pendingRites = await SorceryService.GetPendingRiteApplicationsAsync(Id, _currentUserId!);
            _pendingChosenMysteries = await CoilService.GetPendingChosenMysteryApplicationsAsync(Id, _currentUserId!);
            _pendingCoils = await CoilService.GetPendingCoilApplicationsAsync(Id, _currentUserId!);
            _accessDenied = false;

            await LoadSocialManeuversAsync();

            // Initialise per-character state dictionaries
            foreach (CharacterVitalsDto v in _vitals)
            {
                _beatReasons.TryAdd(v.CharacterId, string.Empty);
                _xpReasons.TryAdd(v.CharacterId, string.Empty);
                _xpAmounts.TryAdd(v.CharacterId, 1);
                _feedbackMessages.TryAdd(v.CharacterId, string.Empty);
            }
        }
        catch (UnauthorizedAccessException)
        {
            _accessDenied = true;
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task LoadSocialManeuversAsync()
    {
        if (string.IsNullOrEmpty(_currentUserId))
        {
            return;
        }

        try
        {
            _socialManeuvers = (await SocialManeuveringService.ListForCampaignAsync(Id, _currentUserId!)).ToList();
            _npcs = await ChronicleNpcService.GetNpcsAsync(Id);

            Campaign? camp = await CampaignService.GetCampaignByIdAsync(Id, _currentUserId!);
            if (camp != null)
            {
                _investigationThresholdDraft = camp.SocialManeuverInvestigationSuccessesPerClue;
            }

            foreach (SocialManeuver m in _socialManeuvers)
            {
                _openDoorPoolByManeuverId.TryAdd(m.Id, 5);
                _forceDoorPoolByManeuverId.TryAdd(m.Id, 5);
                _forceHardLeverageByManeuverId.TryAdd(m.Id, false);
                _forceBpSeverityByManeuverId.TryAdd(m.Id, 7);
                _narrativeDoorsDraftByManeuverId[m.Id] = m.RemainingDoors;
                _bankSuccessesByManeuverId.TryAdd(m.Id, 1);
                _manualClueSourceByManeuverId.TryAdd(m.Id, string.Empty);
                _manualClueLeverageByManeuverId.TryAdd(m.Id, ClueLeverageKind.Soft);
                foreach (ManeuverClue clue in m.Clues)
                {
                    _spendBenefitByClueId.TryAdd(clue.Id, string.Empty);
                }
            }
        }
        catch (UnauthorizedAccessException)
        {
            // Same gate as vitals; ignore
        }
    }

    private int GetNarrativeDoorsDraft(int maneuverId, int fallback) =>
        _narrativeDoorsDraftByManeuverId.GetValueOrDefault(maneuverId, fallback);

    private void SetNarrativeDoorsDraft(int maneuverId, ChangeEventArgs e)
    {
        if (int.TryParse(e.Value?.ToString(), out int v))
        {
            _narrativeDoorsDraftByManeuverId[maneuverId] = v;
        }
    }

    private int GetOpenPool(int id) => _openDoorPoolByManeuverId.GetValueOrDefault(id, 5);

    private void SetOpenPool(int id, ChangeEventArgs e)
    {
        if (int.TryParse(e.Value?.ToString(), out int v))
        {
            _openDoorPoolByManeuverId[id] = v;
        }
    }

    private int GetForcePool(int id) => _forceDoorPoolByManeuverId.GetValueOrDefault(id, 5);

    private void SetForcePool(int id, ChangeEventArgs e)
    {
        if (int.TryParse(e.Value?.ToString(), out int v))
        {
            _forceDoorPoolByManeuverId[id] = v;
        }
    }

    private bool GetForceHard(int id) => _forceHardLeverageByManeuverId.GetValueOrDefault(id);

    private void SetForceHard(int id, ChangeEventArgs e)
    {
        _forceHardLeverageByManeuverId[id] = e.Value switch
        {
            bool b => b,
            string s when bool.TryParse(s, out bool p) => p,
            _ => false,
        };
    }

    private int GetForceBp(int id) => _forceBpSeverityByManeuverId.GetValueOrDefault(id, 7);

    private void SetForceBp(int id, ChangeEventArgs e)
    {
        if (int.TryParse(e.Value?.ToString(), out int v))
        {
            _forceBpSeverityByManeuverId[id] = v;
        }
    }

    private async Task CreateSocialManeuverAsync()
    {
        if (_socialBusy || string.IsNullOrWhiteSpace(_currentUserId) || string.IsNullOrWhiteSpace(_newManeuverGoal))
        {
            return;
        }

        _socialBusy = true;
        try
        {
            await SocialManeuveringService.CreateAsync(
                Id,
                _newManeuverInitiatorId,
                _newManeuverTargetNpcId,
                _newManeuverGoal.Trim(),
                _newManeuverBreakingPoint,
                _newManeuverAspiration,
                _newManeuverVirtueMask,
                _currentUserId!);
            _newManeuverGoal = string.Empty;
            _newManeuverBreakingPoint = false;
            _newManeuverAspiration = false;
            _newManeuverVirtueMask = false;
            ToastService.Show("Created", "Social maneuver created.", ToastType.Success);
            await LoadSocialManeuversAsync();
        }
        catch (Exception ex)
        {
            ToastService.Show("Error", ex.Message, ToastType.Error);
        }
        finally
        {
            _socialBusy = false;
        }
    }

    private async Task OnSocialImpressionChangeAsync(int maneuverId, ChangeEventArgs e)
    {
        if (!int.TryParse(e.Value?.ToString(), out int raw) || !Enum.IsDefined(typeof(ImpressionLevel), raw))
        {
            return;
        }

        _socialBusy = true;
        try
        {
            await SocialManeuveringService.SetImpressionAsync(maneuverId, (ImpressionLevel)raw, _currentUserId!);
            await LoadSocialManeuversAsync();
        }
        catch (Exception ex)
        {
            ToastService.Show("Error", ex.Message, ToastType.Error);
        }
        finally
        {
            _socialBusy = false;
        }
    }

    private async Task ApplyNarrativeDoorsAsync(int maneuverId)
    {
        int doors = _narrativeDoorsDraftByManeuverId.GetValueOrDefault(maneuverId);

        _socialBusy = true;
        try
        {
            await SocialManeuveringService.SetRemainingDoorsNarrativeAsync(maneuverId, doors, _currentUserId!);
            await LoadSocialManeuversAsync();
        }
        catch (Exception ex)
        {
            ToastService.Show("Error", ex.Message, ToastType.Error);
        }
        finally
        {
            _socialBusy = false;
        }
    }

    private static int GetClueThreshold(SocialManeuver m) =>
        m.Campaign?.SocialManeuverInvestigationSuccessesPerClue ?? 3;

    private int GetBankSuccesses(int maneuverId) => _bankSuccessesByManeuverId.GetValueOrDefault(maneuverId, 1);

    private void SetBankSuccesses(int maneuverId, ChangeEventArgs e)
    {
        if (int.TryParse(e.Value?.ToString(), out int v) && v >= 1)
        {
            _bankSuccessesByManeuverId[maneuverId] = v;
        }
    }

    private string GetManualClueSource(int maneuverId) =>
        _manualClueSourceByManeuverId.GetValueOrDefault(maneuverId, string.Empty);

    private void SetManualClueSource(int maneuverId, ChangeEventArgs e) =>
        _manualClueSourceByManeuverId[maneuverId] = e.Value?.ToString() ?? string.Empty;

    private ClueLeverageKind GetManualClueLeverage(int maneuverId) =>
        _manualClueLeverageByManeuverId.GetValueOrDefault(maneuverId, ClueLeverageKind.Soft);

    private void SetManualClueLeverage(int maneuverId, ChangeEventArgs e)
    {
        if (int.TryParse(e.Value?.ToString(), out int raw)
            && Enum.IsDefined(typeof(ClueLeverageKind), raw))
        {
            _manualClueLeverageByManeuverId[maneuverId] = (ClueLeverageKind)raw;
        }
    }

    private string GetSpendBenefit(int clueId) => _spendBenefitByClueId.GetValueOrDefault(clueId, string.Empty);

    private void SetSpendBenefit(int clueId, ChangeEventArgs e) =>
        _spendBenefitByClueId[clueId] = e.Value?.ToString() ?? string.Empty;

    private async Task SaveInvestigationThresholdAsync()
    {
        if (string.IsNullOrEmpty(_currentUserId))
        {
            return;
        }

        _socialBusy = true;
        try
        {
            await SocialManeuveringService.SetInvestigationSuccessesPerClueAsync(
                Id,
                _investigationThresholdDraft,
                _currentUserId!);
            ToastService.Show("Saved", "Investigation clue threshold updated.", ToastType.Success);
            await LoadSocialManeuversAsync();
        }
        catch (Exception ex)
        {
            ToastService.Show("Error", ex.Message, ToastType.Error);
        }
        finally
        {
            _socialBusy = false;
        }
    }

    private async Task BankInvestigationAsync(int maneuverId)
    {
        _socialBusy = true;
        try
        {
            int n = GetBankSuccesses(maneuverId);
            await SocialManeuveringService.BankInvestigationSuccessesAsync(maneuverId, n, _currentUserId!);
            ToastService.Show("Investigation", $"Banked {n} success(es).", ToastType.Success);
            await LoadSocialManeuversAsync();
        }
        catch (Exception ex)
        {
            ToastService.Show("Error", ex.Message, ToastType.Error);
        }
        finally
        {
            _socialBusy = false;
        }
    }

    private async Task AddManualClueAsync(int maneuverId)
    {
        string src = GetManualClueSource(maneuverId).Trim();
        if (string.IsNullOrEmpty(src))
        {
            ToastService.Show("Clue", "Enter a source description.", ToastType.Warning);
            return;
        }

        _socialBusy = true;
        try
        {
            await SocialManeuveringService.AddManeuverClueAsync(
                maneuverId,
                src,
                GetManualClueLeverage(maneuverId),
                _currentUserId!);
            _manualClueSourceByManeuverId[maneuverId] = string.Empty;
            ToastService.Show("Clue", "Maneuver clue added.", ToastType.Success);
            await LoadSocialManeuversAsync();
        }
        catch (Exception ex)
        {
            ToastService.Show("Error", ex.Message, ToastType.Error);
        }
        finally
        {
            _socialBusy = false;
        }
    }

    private async Task SpendClueAsync(int clueId)
    {
        string benefit = GetSpendBenefit(clueId).Trim();
        if (string.IsNullOrEmpty(benefit))
        {
            ToastService.Show("Clue", "Enter the recorded benefit before spending.", ToastType.Warning);
            return;
        }

        _socialBusy = true;
        try
        {
            await SocialManeuveringService.SpendManeuverClueAsync(clueId, benefit, _currentUserId!);
            _spendBenefitByClueId[clueId] = string.Empty;
            ToastService.Show("Clue", "Clue spent.", ToastType.Success);
            await LoadSocialManeuversAsync();
        }
        catch (Exception ex)
        {
            ToastService.Show("Error", ex.Message, ToastType.Error);
        }
        finally
        {
            _socialBusy = false;
        }
    }

    private async Task RollOpenDoorAsync(int maneuverId)
    {
        _socialBusy = true;
        try
        {
            int pool = GetOpenPool(maneuverId);
            (_, RollResult roll, int opened) = await SocialManeuveringService.RollOpenDoorAsync(
                maneuverId,
                pool,
                _currentUserId!);
            string openDetail = $"Opened {opened} door(s). Successes: {roll.Successes}";
            openDetail += roll.IsExceptionalSuccess ? " (exceptional)" : string.Empty;
            openDetail += roll.IsDramaticFailure ? " (dramatic failure)" : string.Empty;
            ToastService.Show("Open Door", openDetail, ToastType.Info);
            await LoadSocialManeuversAsync();
        }
        catch (Exception ex)
        {
            ToastService.Show("Error", ex.Message, ToastType.Error);
        }
        finally
        {
            _socialBusy = false;
        }
    }

    private async Task RollForceDoorsAsync(int maneuverId)
    {
        bool ok = await JSRuntime.InvokeAsync<bool>(
            "rnConfirm",
            "Force Doors: on failure this PC can never use Social maneuvering against this NPC again (Burnt). Continue?");
        if (!ok)
        {
            return;
        }

        _socialBusy = true;
        try
        {
            int pool = GetForcePool(maneuverId);
            bool hard = GetForceHard(maneuverId);
            int bp = GetForceBp(maneuverId);
            (_, RollResult roll, bool forcedOk) = await SocialManeuveringService.RollForceDoorsAsync(
                maneuverId,
                pool,
                hard,
                bp,
                _currentUserId!);
            string msg = forcedOk
                ? $"Success. Successes: {roll.Successes}"
                : $"Failed — relationship Burnt. Successes: {roll.Successes}";
            ToastService.Show("Force Doors", msg, forcedOk ? ToastType.Success : ToastType.Warning);
            await LoadSocialManeuversAsync();
        }
        catch (Exception ex)
        {
            ToastService.Show("Error", ex.Message, ToastType.Error);
        }
        finally
        {
            _socialBusy = false;
        }
    }

    private async Task AwardBeat(int characterId)
    {
        string reason = _beatReasons.GetValueOrDefault(characterId, string.Empty);
        if (string.IsNullOrWhiteSpace(reason) || _awarding)
        {
            return;
        }

        _awarding = true;
        try
        {
            await GlimpseService.AwardBeatToCharacterAsync(Id, characterId, reason, _currentUserId!);
            _beatReasons[characterId] = string.Empty;
            _feedbackMessages[characterId] = "Beat awarded.";
            await LoadVitals();
        }
        finally
        {
            _awarding = false;
        }
    }

    private async Task AwardXp(int characterId)
    {
        int amount = _xpAmounts.GetValueOrDefault(characterId, 0);
        string reason = _xpReasons.GetValueOrDefault(characterId, string.Empty);
        if (amount <= 0 || string.IsNullOrWhiteSpace(reason) || _awarding)
        {
            return;
        }

        _awarding = true;
        try
        {
            await GlimpseService.AwardXpToCharacterAsync(Id, characterId, amount, reason, _currentUserId!);
            _xpReasons[characterId] = string.Empty;
            _xpAmounts[characterId] = 1;
            _feedbackMessages[characterId] = $"{amount} XP awarded.";
            await LoadVitals();
        }
        finally
        {
            _awarding = false;
        }
    }

    private async Task AwardCoterieBeat()
    {
        if (string.IsNullOrWhiteSpace(_coteReason) || _awarding)
        {
            return;
        }

        _awarding = true;
        try
        {
            await GlimpseService.AwardBeatToCampaignAsync(Id, _coteReason, _currentUserId!);
            _coteMessage = $"Beat awarded to all {_vitals.Count} characters.";
            _coteReason = string.Empty;
            await LoadVitals();
        }
        finally
        {
            _awarding = false;
        }
    }

    /// <summary>Returns a 0–100 percentage, clamped, for CSS bar widths.</summary>
    private static int BarPct(int current, int max) =>
        max <= 0 ? 0 : Math.Clamp(current * 100 / max, 0, 100);

    private void StartReject(int characterBloodlineId)
    {
        _rejectingId = characterBloodlineId;
        _rejectNote = string.Empty;
    }

    private void CancelReject()
    {
        _rejectingId = null;
        _rejectNote = string.Empty;
    }

    private async Task ApproveBloodline(int characterBloodlineId)
    {
        if (_awarding)
        {
            return;
        }

        _awarding = true;
        try
        {
            await BloodlineService.ApproveBloodlineAsync(characterBloodlineId, null, _currentUserId!);
            ToastService.Show("Approved", "Bloodline application approved.", RequiemNexus.Web.Services.ToastType.Success);
            await LoadVitals();
        }
        catch (Exception ex)
        {
            ToastService.Show("Error", ex.Message, RequiemNexus.Web.Services.ToastType.Error);
        }
        finally
        {
            _awarding = false;
        }
    }

    private async Task ConfirmRejectBloodline(int characterBloodlineId)
    {
        if (_awarding)
        {
            return;
        }

        _awarding = true;
        try
        {
            await BloodlineService.RejectBloodlineAsync(characterBloodlineId, string.IsNullOrWhiteSpace(_rejectNote) ? null : _rejectNote, _currentUserId!);
            _rejectingId = null;
            _rejectNote = string.Empty;
            ToastService.Show("Rejected", "Bloodline application rejected.", RequiemNexus.Web.Services.ToastType.Info);
            await LoadVitals();
        }
        catch (Exception ex)
        {
            ToastService.Show("Error", ex.Message, RequiemNexus.Web.Services.ToastType.Error);
        }
        finally
        {
            _awarding = false;
        }
    }

    private void StartRejectCovenant(int characterId)
    {
        _rejectingCovenantCharacterId = characterId;
        _covenantRejectNote = string.Empty;
    }

    private void CancelRejectCovenant()
    {
        _rejectingCovenantCharacterId = null;
        _covenantRejectNote = string.Empty;
    }

    private void StartRejectRite(int characterRiteId)
    {
        _rejectingRiteId = characterRiteId;
        _riteRejectNote = string.Empty;
    }

    private void CancelRejectRite()
    {
        _rejectingRiteId = null;
        _riteRejectNote = string.Empty;
    }

    private async Task ApproveRite(int characterRiteId)
    {
        if (_awarding)
        {
            return;
        }

        _awarding = true;
        try
        {
            await SorceryService.ApproveRiteLearnAsync(characterRiteId, null, _currentUserId!);
            ToastService.Show("Approved", "Rite learning approved.", RequiemNexus.Web.Services.ToastType.Success);
            await LoadVitals();
        }
        catch (Exception ex)
        {
            ToastService.Show("Error", ex.Message, RequiemNexus.Web.Services.ToastType.Error);
        }
        finally
        {
            _awarding = false;
        }
    }

    private async Task ConfirmRejectRite(int characterRiteId)
    {
        if (_awarding)
        {
            return;
        }

        _awarding = true;
        try
        {
            await SorceryService.RejectRiteLearnAsync(characterRiteId, string.IsNullOrWhiteSpace(_riteRejectNote) ? null : _riteRejectNote, _currentUserId!);
            _rejectingRiteId = null;
            _riteRejectNote = string.Empty;
            ToastService.Show("Rejected", "Rite learning rejected.", RequiemNexus.Web.Services.ToastType.Info);
            await LoadVitals();
        }
        catch (Exception ex)
        {
            ToastService.Show("Error", ex.Message, RequiemNexus.Web.Services.ToastType.Error);
        }
        finally
        {
            _awarding = false;
        }
    }

    private async Task ApproveCovenant(int characterId)
    {
        if (_awarding)
        {
            return;
        }

        _awarding = true;
        try
        {
            await CovenantService.ApproveCovenantAsync(characterId, null, _currentUserId!);
            ToastService.Show("Approved", "Covenant application approved.", RequiemNexus.Web.Services.ToastType.Success);
            await LoadVitals();
        }
        catch (Exception ex)
        {
            ToastService.Show("Error", ex.Message, RequiemNexus.Web.Services.ToastType.Error);
        }
        finally
        {
            _awarding = false;
        }
    }

    private async Task ConfirmRejectCovenant(int characterId)
    {
        if (_awarding)
        {
            return;
        }

        _awarding = true;
        try
        {
            await CovenantService.RejectCovenantAsync(characterId, string.IsNullOrWhiteSpace(_covenantRejectNote) ? null : _covenantRejectNote, _currentUserId!);
            _rejectingCovenantCharacterId = null;
            _covenantRejectNote = string.Empty;
            ToastService.Show("Rejected", "Covenant application rejected.", RequiemNexus.Web.Services.ToastType.Info);
            await LoadVitals();
        }
        catch (Exception ex)
        {
            ToastService.Show("Error", ex.Message, RequiemNexus.Web.Services.ToastType.Error);
        }
        finally
        {
            _awarding = false;
        }
    }

    private void StartRejectChosenMystery(int characterId)
    {
        _rejectingChosenMysteryCharacterId = characterId;
    }

    private void CancelRejectChosenMystery()
    {
        _rejectingChosenMysteryCharacterId = null;
    }

    private async Task ApproveChosenMystery(int characterId)
    {
        if (_awarding)
        {
            return;
        }

        _awarding = true;
        try
        {
            await CoilService.ApproveChosenMysteryAsync(characterId, _currentUserId!);
            ToastService.Show("Approved", "Chosen Mystery approved.", RequiemNexus.Web.Services.ToastType.Success);
            await LoadVitals();
        }
        catch (Exception ex)
        {
            ToastService.Show("Error", ex.Message, RequiemNexus.Web.Services.ToastType.Error);
        }
        finally
        {
            _awarding = false;
        }
    }

    private async Task ConfirmRejectChosenMystery(int characterId)
    {
        if (_awarding)
        {
            return;
        }

        _awarding = true;
        try
        {
            await CoilService.RejectChosenMysteryAsync(characterId, _currentUserId!);
            _rejectingChosenMysteryCharacterId = null;
            ToastService.Show("Rejected", "Chosen Mystery request rejected.", RequiemNexus.Web.Services.ToastType.Info);
            await LoadVitals();
        }
        catch (Exception ex)
        {
            ToastService.Show("Error", ex.Message, RequiemNexus.Web.Services.ToastType.Error);
        }
        finally
        {
            _awarding = false;
        }
    }

    private void StartRejectCoil(int characterCoilId)
    {
        _rejectingCoilId = characterCoilId;
        _coilRejectNote = string.Empty;
    }

    private void CancelRejectCoil()
    {
        _rejectingCoilId = null;
        _coilRejectNote = string.Empty;
    }

    private async Task ApproveCoil(int characterCoilId)
    {
        if (_awarding)
        {
            return;
        }

        _awarding = true;
        try
        {
            await CoilService.ApproveCoilLearnAsync(characterCoilId, null, _currentUserId!);
            ToastService.Show("Approved", "Coil purchase approved.", RequiemNexus.Web.Services.ToastType.Success);
            await LoadVitals();
        }
        catch (Exception ex)
        {
            ToastService.Show("Error", ex.Message, RequiemNexus.Web.Services.ToastType.Error);
        }
        finally
        {
            _awarding = false;
        }
    }

    private async Task ConfirmRejectCoil(int characterCoilId)
    {
        if (_awarding)
        {
            return;
        }

        _awarding = true;
        try
        {
            await CoilService.RejectCoilLearnAsync(
                characterCoilId,
                string.IsNullOrWhiteSpace(_coilRejectNote) ? null : _coilRejectNote,
                _currentUserId!);
            _rejectingCoilId = null;
            _coilRejectNote = string.Empty;
            ToastService.Show("Rejected", "Coil purchase rejected.", RequiemNexus.Web.Services.ToastType.Info);
            await LoadVitals();
        }
        catch (Exception ex)
        {
            ToastService.Show("Error", ex.Message, RequiemNexus.Web.Services.ToastType.Error);
        }
        finally
        {
            _awarding = false;
        }
    }
}

#pragma warning restore SA1214
#pragma warning restore SA1204
#pragma warning restore SA1202
#pragma warning restore SA1201
