using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Web.Components.Pages.Campaigns;

#pragma warning disable SA1201 // Parameter property first matches Blazor page convention
public partial class DanseMacabre
{
    [Parameter]
    public int CampaignId { get; set; }

    private bool _loading = true;
    private bool _accessDenied;
    private bool _busy;
    private int _activeTab;
    private string? _currentUserId;
    private string? _feedbackMessage;

    private List<FeedingTerritory> _territories = [];
    private List<CityFaction> _factions = [];
    private List<ChronicleNpc> _npcs = [];

    private bool _territoryModalOpen;
    private bool _factionModalOpen;
    private bool _npcModalOpen;
    private bool _showDeceased;

    protected override async Task OnInitializedAsync()
    {
        AuthenticationState authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        _currentUserId = authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(_currentUserId))
        {
            _accessDenied = true;
            _loading = false;
            return;
        }

        Campaign? campaign = await CampaignService.GetCampaignByIdAsync(CampaignId, _currentUserId);
        if (campaign == null || !CampaignService.IsStoryteller(campaign, _currentUserId))
        {
            _accessDenied = true;
            _loading = false;
            return;
        }

        await LoadAll();
        _loading = false;
    }

    private void SetFeedback(string message)
    {
        _feedbackMessage = message;
        _ = Task.Delay(3000).ContinueWith(
            _ =>
            {
                _feedbackMessage = null;
                InvokeAsync(StateHasChanged);
            },
            TaskScheduler.Default);
    }

    private async Task LoadAll()
    {
        _territories = await TerritoryService.GetTerritoriesAsync(CampaignId);
        _factions = await FactionService.GetFactionsAsync(CampaignId);
        await LoadNpcs();
    }

    private async Task LoadNpcs()
    {
        _npcs = await NpcService.GetNpcsAsync(CampaignId, _showDeceased);
    }

    private string TabClass(int tab) => _activeTab == tab ? "btn-rn-primary" : "btn-rn-ghost";

    private void OpenTerritoryModal() => _territoryModalOpen = true;

    private void OpenFactionModal() => _factionModalOpen = true;

    private void OpenNpcModal() => _npcModalOpen = true;

    private async Task OnShowDeceasedChangedAsync(bool value)
    {
        _showDeceased = value;
        await LoadNpcs();
        await InvokeAsync(StateHasChanged);
    }

    private async Task OnTerritoryCreatedAsync()
    {
        _territories = await TerritoryService.GetTerritoriesAsync(CampaignId);
        SetFeedback("Territory added.");
    }

    private async Task DeleteTerritory(int id)
    {
        if (string.IsNullOrEmpty(_currentUserId))
        {
            return;
        }

        _busy = true;
        try
        {
            await TerritoryService.DeleteTerritoryAsync(id, _currentUserId);
            _territories = await TerritoryService.GetTerritoriesAsync(CampaignId);
            SetFeedback("Territory deleted.");
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task OnFactionCreatedAsync()
    {
        _factions = await FactionService.GetFactionsAsync(CampaignId);
        SetFeedback("Faction added.");
    }

    private async Task DeleteNpc(int id)
    {
        if (string.IsNullOrEmpty(_currentUserId))
        {
            return;
        }

        _busy = true;
        try
        {
            await NpcService.DeleteNpcAsync(id, _currentUserId);
            await LoadNpcs();
            SetFeedback("NPC deleted.");
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task OnNpcCreatedAsync()
    {
        await LoadNpcs();
        SetFeedback("NPC added.");
    }
}
