using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Data.Models;
using RequiemNexus.Domain.Enums;

namespace RequiemNexus.Web.Components.Pages.Campaigns;

/// <summary>
/// ST-only city faction editor. Markup is split under <c>FactionDetailParts/</c>.
/// </summary>
public partial class FactionDetail
{
    private CityFaction? _faction;

    private List<ChronicleNpc> _allNpcs = [];

    private List<FactionRelationship> _relationships = [];

    private List<CityFaction> _otherFactions = [];

    private bool _accessDenied;

    private bool _busy;

    private bool _showConfirmDelete;

    private string? _feedbackMessage;

    private string? _currentUserId;

    private string _editName = string.Empty;

    private FactionType _editType = FactionType.Other;

    private int _editInfluence = 1;

    private string _editAgenda = string.Empty;

    private string _editPublicDesc = string.Empty;

    private string _editStNotes = string.Empty;

    private int _editLeaderNpcId;

    private int _relOtherFactionId;

    private FactionStance _relStance = FactionStance.Neutral;

    private string _relNotes = string.Empty;

    /// <summary>Gets or sets the campaign id.</summary>
    [Parameter]
    public int CampaignId { get; set; }

    /// <summary>Gets or sets the faction id.</summary>
    [Parameter]
    public int FactionId { get; set; }

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        AuthenticationState authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        _currentUserId = authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(_currentUserId))
        {
            _accessDenied = true;
            return;
        }

        Campaign? campaign = await CampaignService.GetCampaignByIdAsync(CampaignId, _currentUserId);
        if (campaign == null || !CampaignService.IsStoryteller(campaign, _currentUserId))
        {
            _accessDenied = true;
            return;
        }

        await LoadData();
    }

    private async Task LoadData()
    {
        _faction = await FactionService.GetFactionAsync(FactionId);
        if (_faction == null)
        {
            return;
        }

        _editName = _faction.Name;
        _editType = _faction.FactionType;
        _editInfluence = _faction.InfluenceRating;
        _editAgenda = _faction.Agenda;
        _editPublicDesc = _faction.PublicDescription;
        _editStNotes = _faction.StorytellerNotes;
        _editLeaderNpcId = _faction.LeaderNpcId ?? 0;

        _allNpcs = await NpcService.GetNpcsAsync(CampaignId, true);
        List<FactionRelationship> all = await RelationshipService.GetRelationshipsAsync(CampaignId);
        _relationships = all.Where(r => r.FactionAId == FactionId || r.FactionBId == FactionId).ToList();

        List<CityFaction> allFactions = await FactionService.GetFactionsAsync(CampaignId);
        _otherFactions = allFactions.Where(f => f.Id != FactionId).ToList();
    }

    private async Task SaveFaction()
    {
        if (string.IsNullOrEmpty(_currentUserId))
        {
            return;
        }

        _busy = true;
        _feedbackMessage = null;
        try
        {
            await FactionService.UpdateFactionAsync(
                FactionId,
                _editName,
                _editType,
                _editInfluence,
                _editPublicDesc,
                _editStNotes,
                _editAgenda,
                _editLeaderNpcId == 0 ? null : _editLeaderNpcId,
                _currentUserId);
            await LoadData();
            _feedbackMessage = "Faction saved.";
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task SaveRelationship()
    {
        if (_relOtherFactionId == 0 || string.IsNullOrEmpty(_currentUserId))
        {
            return;
        }

        _busy = true;
        try
        {
            await RelationshipService.SetRelationshipAsync(CampaignId, FactionId, _relOtherFactionId, _relStance, _relNotes, _currentUserId);
            _relOtherFactionId = 0;
            _relNotes = string.Empty;
            await LoadData();
            _feedbackMessage = "Relationship set.";
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task ConfirmDelete()
    {
        if (string.IsNullOrEmpty(_currentUserId))
        {
            return;
        }

        _busy = true;
        try
        {
            await FactionService.DeleteFactionAsync(FactionId, _currentUserId);
            NavigationManager.NavigateTo($"/campaigns/{CampaignId}/danse-macabre");
        }
        catch
        {
            _showConfirmDelete = false;
            _busy = false;
        }
    }
}
