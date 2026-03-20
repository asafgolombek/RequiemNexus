using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Application.DTOs;
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
