// Blazor partial: field declarations for CharacterDetails (see CharacterDetails.razor.cs for lifecycle and sheet handlers).
#pragma warning disable SA1201 // Order of fields, properties, and methods
#pragma warning disable SA1214 // Readonly fields before non-readonly

using Microsoft.AspNetCore.Components;
using RequiemNexus.Application.DTOs;
using RequiemNexus.Data.Models;
using RequiemNexus.Domain.Enums;
using RequiemNexus.Domain.Models;

namespace RequiemNexus.Web.Components.Pages;

public partial class CharacterDetails
{
    private Character? _character;
    private bool _isSheetLoading = true;
    private string? _sheetLoadError;
    private string? _currentUserId;
    private string? _cookieHeader;

    /// <summary>
    /// After local Beats/XP adjustments, the server broadcasts to the chronicle group and this connection receives it.
    /// The scoped character graph is already updated; skip one full reload to avoid duplicate heavy Include queries.
    /// </summary>
    private int _skipIncomingCharacterHubReloadCount;
    private PersistingComponentStateSubscription _persistingSubscription;
    private List<Asset> _availableAssets = [];
    private int _selectedAssetId = 0;
    private int _selectedAssetQuantity = 1;

    private bool _isRollerOpen = false;
    private string _rollerTraitName = string.Empty;
    private int _rollerBaseDice = 1;
    private int? _rollerFixedDicePool;
    private int? _rollerRiteMaxRolls;
    private int? _rollerRiteTargetSuccesses;
    private int? _rollerRiteMinutesPerRoll;
    private int? _rollerRiteRitualDisciplineDots;
    private SorceryType? _rollerRiteSorceryType;
    private bool _isApplyBloodlineModalOpen = false;
    private bool _removingBloodline = false;
    private List<BloodlineSummaryDto> _eligibleBloodlines = [];
    private bool _isApplyCovenantModalOpen = false;
    private bool _isApplyLearnRiteModalOpen = false;

    /// <summary>Name of the modal-open handler currently awaiting data (e.g. <c>nameof(OpenLearnRiteModal)</c> in markup).</summary>
    private string? _pendingModal;
    private List<SorceryRiteSummaryDto> _eligibleRites = [];
    private bool _isChosenMysteryModalOpen = false;
    private bool _isLearnCoilModalOpen = false;
    private bool _isRitePrepModalOpen;
    private CharacterRite? _pendingRiteForPrep;
    private IReadOnlyList<RiteRequirement> _pendingRiteRequirements = [];
    private IReadOnlyList<CampaignKindredTargetDto> _riteKinTargets = [];
    private List<ScaleSummaryDto> _eligibleScales = [];
    private List<CoilSummaryDto> _eligibleCoils = [];
    private bool _requestingLeave = false;
    private List<CovenantSummaryDto> _eligibleCovenants = [];
    private HashSet<int> _expandedMeritIds = [];

    private bool _isAdvancementMode = false;
    private bool _isFreeEditMode = false;
    private string _activeTab = "sheet";
    private bool _isEditingName = false;
    private bool _isExporting = false;

    private readonly HashSet<int> _expandedDisciplines = new HashSet<int>();

    /// <summary>Resolved dice pools for discipline powers that define <c>PoolDefinitionJson</c>.</summary>
    private readonly Dictionary<int, int> _disciplinePowerResolvedPools = [];

    private bool _isDisciplineActivateModalOpen;
    private DisciplinePower? _disciplineActivatePower;
    private int _disciplineActivatePool;

    private bool _degRollPlayerModalOpen;

    private bool _isForgeModalOpen = false;
    private CharacterAsset? _forgeTarget;
    private List<AssetModifier> _availableModifiers = [];
}

#pragma warning restore SA1214
#pragma warning restore SA1201
