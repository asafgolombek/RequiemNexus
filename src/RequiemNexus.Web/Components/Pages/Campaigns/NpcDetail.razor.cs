using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Data.Models;
using RequiemNexus.Data.Models.Enums;

namespace RequiemNexus.Web.Components.Pages.Campaigns;

/// <summary>
/// ST-only NPC editor. Markup is split under <c>NpcDetailParts/</c>; this partial owns load/save and auth.
/// </summary>
public partial class NpcDetail
{
    private static readonly (string Category, string[] Items)[] _attributeCategories =
    [
        ("Physical", ["Strength", "Dexterity", "Stamina"]),
        ("Mental", ["Intelligence", "Wits", "Resolve"]),
        ("Social", ["Presence", "Manipulation", "Composure"]),
    ];

    private static readonly (string Category, string[] Items)[] _skillCategories =
    [
        ("Physical", ["Athletics", "Brawl", "Drive", "Firearms", "Larceny", "Stealth", "Survival", "Weaponry"]),
        ("Mental", ["Academics", "Computer", "Crafts", "Investigation", "Medicine", "Occult", "Politics", "Science"]),
        ("Social", ["Animal Ken", "Empathy", "Expression", "Intimidation", "Persuasion", "Socialize", "Streetwise", "Subterfuge"]),
    ];

    private ChronicleNpc? _npc;

    private List<CityFaction> _allFactions = [];

    private bool _accessDenied;

    private bool _busy;

    private bool _showConfirmDelete;

    private string? _feedbackMessage;

    private string? _currentUserId;

    private string _editName = string.Empty;

    private string _editTitle = string.Empty;

    private int _editFactionId;

    private string _editRole = string.Empty;

    private string _editPublicDesc = string.Empty;

    private string _editStNotes = string.Empty;

    private int? _editStatBlockId;

    private CreatureType _editCreatureType = CreatureType.Mortal;

    private Dictionary<string, int> _editAttributes = [];

    private Dictionary<string, int> _editSkills = [];

    /// <summary>Gets or sets the campaign id.</summary>
    [Parameter]
    public int CampaignId { get; set; }

    /// <summary>Gets or sets the NPC id.</summary>
    [Parameter]
    public int NpcId { get; set; }

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
        _npc = await NpcService.GetNpcAsync(NpcId);
        if (_npc == null)
        {
            return;
        }

        _editName = _npc.Name;
        _editTitle = _npc.Title ?? string.Empty;
        _editFactionId = _npc.PrimaryFactionId ?? 0;
        _editRole = _npc.RoleInFaction ?? string.Empty;
        _editPublicDesc = _npc.PublicDescription;
        _editStNotes = _npc.StorytellerNotes;
        _editStatBlockId = _npc.LinkedStatBlockId;
        _editCreatureType = _npc.CreatureType;

        _editAttributes = NpcDetailStatsHelper.DeserializeStats(_npc.AttributesJson, _attributeCategories);
        _editSkills = NpcDetailStatsHelper.DeserializeStats(_npc.SkillsJson, _skillCategories);

        _allFactions = await FactionService.GetFactionsAsync(CampaignId);
    }

    private async Task SaveNpc()
    {
        if (string.IsNullOrEmpty(_currentUserId))
        {
            return;
        }

        _busy = true;
        _feedbackMessage = null;
        try
        {
            await NpcService.UpdateNpcAsync(
                NpcId,
                _editName,
                string.IsNullOrWhiteSpace(_editTitle) ? null : _editTitle,
                _editFactionId == 0 ? null : _editFactionId,
                string.IsNullOrWhiteSpace(_editRole) ? null : _editRole,
                _editPublicDesc,
                _editStNotes,
                _editStatBlockId,
                _editCreatureType,
                JsonSerializer.Serialize(_editAttributes),
                JsonSerializer.Serialize(_editSkills),
                _currentUserId);
            await LoadData();
            _feedbackMessage = "NPC saved.";
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task SaveStatBlockLink()
    {
        if (string.IsNullOrEmpty(_currentUserId) || _npc == null)
        {
            return;
        }

        _busy = true;
        _feedbackMessage = null;
        try
        {
            await NpcService.UpdateNpcAsync(
                NpcId,
                _npc.Name,
                _npc.Title,
                _npc.PrimaryFactionId,
                _npc.RoleInFaction,
                _npc.PublicDescription,
                _npc.StorytellerNotes,
                _editStatBlockId,
                _npc.CreatureType,
                _npc.AttributesJson,
                _npc.SkillsJson,
                _currentUserId);
            await LoadData();
            _feedbackMessage = "Stat block link updated.";
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task ToggleAlive(ChangeEventArgs e)
    {
        if (string.IsNullOrEmpty(_currentUserId))
        {
            return;
        }

        bool isAlive = (bool)(e.Value ?? true);
        _busy = true;
        try
        {
            await NpcService.SetNpcAliveAsync(NpcId, isAlive, _currentUserId);
            await LoadData();
            _feedbackMessage = isAlive ? "NPC marked alive." : "NPC marked deceased.";
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
            await NpcService.DeleteNpcAsync(NpcId, _currentUserId);
            NavigationManager.NavigateTo($"/campaigns/{CampaignId}/danse-macabre");
        }
        catch
        {
            _showConfirmDelete = false;
            _busy = false;
        }
    }

    private Task HandleStatsEditedAsync() => InvokeAsync(StateHasChanged);
}
