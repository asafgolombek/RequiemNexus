using System.Threading;
using Microsoft.AspNetCore.Components;
using RequiemNexus.Data.Models;
using RequiemNexus.Domain.Enums;

namespace RequiemNexus.Web.Components.Pages.Campaigns;

#pragma warning disable SA1201 // Field order matches tracker state layout
public partial class InitiativeTracker
{
    [Parameter]
    public int CampaignId { get; set; }

    [Parameter]
    public int EncounterId { get; set; }

    private readonly Dictionary<int, HashSet<string>> _conditionNamesByCharacterId = new();
    private readonly CancellationTokenSource _disposeCts = new();

    private CombatEncounter? _encounter;
    private bool _accessDenied;
    private List<Character> _campaignCharacters = [];
    private Dictionary<int, List<CharacterTilt>> _activeTilts = [];
    private Dictionary<int, TiltType> _tiltSelections = [];
    private bool _loading = true;
    private bool _isSt;
    private bool _busy;
    private string? _currentUserId;

    private string _addType = "character";
    private int _addCharacterId;
    private string _addNpcName = string.Empty;
    private int _addChronicleNpcId;
    private int _addInitMod;
    private int _addRoll = 1;
    private int _addNpcHealthBoxes = 7;
    private int _addNpcMaxWillpower = 4;
    private int _addNpcMaxVitae;
    private bool _addChronicleTracksVitae;
    private List<ChronicleNpc> _chronicleNpcs = [];

    private int? _lastAnnouncedInitiativeEntryId;
    private bool _initiativeAnnouncePrimed;

    private int? _defeatConfirmEntryId;
    private int? _npcRollEntryId;
    private bool _meleeAttackModalOpen;
    private int _meleeAttackAttackerCharacterId;
    private bool _playerWeaponDamageModalOpen;
    private int _playerWeaponDamageCharacterId;
    private string _addError = string.Empty;
    private string _actionFeedback = string.Empty;
    private string? _cookieHeader;
    private PersistingComponentStateSubscription _persistingSubscription;
    private int _lastLoadedEncounterId = int.MinValue;
    private int _lastLoadedCampaignId = int.MinValue;
    private int? _hubConnectedCampaignId;

    private CancellationTokenSource? _loadEncounterCts;
    private int _loadGeneration;
    private int _fullPageLoadTicket;

    private List<InitiativeEntry> SortedEntries =>
        _encounter?.InitiativeEntries.OrderBy(i => i.Order).ToList() ?? [];

    private InitiativeEntry? CurrentActor =>
        _encounter is { ResolvedAt: null } ? SortedEntries.FirstOrDefault(i => !i.HasActed) : null;

    private int? PlayerOwnedCharacterId =>
        _encounter?.InitiativeEntries
            .FirstOrDefault(e => e.CharacterId.HasValue
                && e.Character != null
                && e.Character.ApplicationUserId == _currentUserId)
            ?.CharacterId;

    private IEnumerable<ChronicleNpc> ChronicleNpcsAvailableForEncounter
    {
        get
        {
            if (_encounter?.InitiativeEntries == null)
            {
                return _chronicleNpcs;
            }

            HashSet<int> taken = _encounter.InitiativeEntries
                .Where(i => i.ChronicleNpcId.HasValue)
                .Select(i => i.ChronicleNpcId!.Value)
                .ToHashSet();
            return _chronicleNpcs.Where(n => !taken.Contains(n.Id));
        }
    }
}
