using Microsoft.AspNetCore.Components;
using RequiemNexus.Data.Models;
using RequiemNexus.Data.RealTime;
using RequiemNexus.Domain.Enums;
using RequiemNexus.Domain.Services;
using RequiemNexus.Web.Enums;

namespace RequiemNexus.Web.Components.UI;

#pragma warning disable SA1201 // Order mirrors original @code / inject layout
#pragma warning disable SA1202
#pragma warning disable SA1214 // Readonly array fields grouped with related state

/// <summary>Code-behind for the shared dice roller modal (standard pools, extended rites, hub/offline rolls).</summary>
public partial class DiceRollerModal : IDisposable
{
    [Parameter]
    public bool IsVisible { get; set; }

    [Parameter]
    public EventCallback<bool> IsVisibleChanged { get; set; }

    [Parameter]
    public string TraitName { get; set; } = string.Empty;

    [Parameter]
    public int BaseDice { get; set; } = 1;

    [Parameter]
    public Character? Character { get; set; }

    /// <summary>When set, pool size is fixed (e.g. devotion) and associated trait UI is hidden.</summary>
    [Parameter]
    public int? FixedDicePool { get; set; }

    /// <summary>Optional blood rite extended-action cap (unmodified pool). Requires <see cref="FixedDicePool"/> and <see cref="RiteExtendedTargetSuccesses"/>.</summary>
    [Parameter]
    public int? RiteExtendedMaxRolls { get; set; }

    /// <summary>Successes required to complete the rite (shown in the roller).</summary>
    [Parameter]
    public int? RiteExtendedTargetSuccesses { get; set; }

    /// <summary>Minutes per extended roll (30 or 15 per V:tR 2e).</summary>
    [Parameter]
    public int? RiteExtendedMinutesPerRoll { get; set; }

    /// <summary>Tradition for ritual outcome Conditions (Crúac / Theban / Necromancy). Set with extended rite parameters.</summary>
    [Parameter]
    public SorceryType? RiteOutcomeTradition { get; set; }

    /// <summary>Ritual Discipline dots (from begin activation) for optional Potency on exceptional success.</summary>
    [Parameter]
    public int? RiteExtendedRitualDisciplineDots { get; set; }

    private const string _riteExtendedDescriptionMarker = "extended rite";

    private string _associatedTraitName = string.Empty;
    private int _modifier;
    private bool _tenAgain = true;
    private bool _nineAgain;
    private bool _eightAgain;
    private bool _isRote;
    private int? _seed;
    private DiceRollResultDto? _lastResultDto;
    private bool _showTools;

    private readonly string _successCountId = $"success-count-{Guid.NewGuid():N}";
    private string _shakeClass = string.Empty;
    private bool _showBurst;
    private bool _isRolling;
    private bool _sharing;
    private bool _copying;
    private string? _shareSlug;

    private int _riteRollsUsed;
    private int _riteAccumulatedSuccesses;
    private bool _riteFailureNeedsChoice;
    private bool _riteRollInFlight;
    private bool _riteHadExceptionalSuccess;
    private bool _addRiteDisciplineDotsToPotency;
    private bool _rollerWasVisible;

    private IDisposable? _diceRollSubscription;

    private int? _resolverTotal;

    private readonly string[] _attributes = TraitMetadata.AllAttributes.Select(a => a.ToString()).ToArray();
    private readonly string[] _skills = TraitMetadata.AllSkills.Select(s => s.ToString()).ToArray();

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        _diceRollSubscription = SessionClient.SubscribeDiceRollReceived(HandleDiceRollReceived);
    }

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        if (IsVisible && !_rollerWasVisible && IsRiteExtendedMode)
        {
            _riteRollsUsed = 0;
            _riteAccumulatedSuccesses = 0;
            _riteFailureNeedsChoice = false;
            _riteRollInFlight = false;
            _riteHadExceptionalSuccess = false;
            _addRiteDisciplineDotsToPotency = false;
        }

        _rollerWasVisible = IsVisible;
        await RefreshResolverTotalAsync();
    }

    private async Task Close()
    {
        IsVisible = false;
        _lastResultDto = null;
        _shareSlug = null;
        _associatedTraitName = string.Empty;
        _modifier = 0;
        _resolverTotal = null;
        _riteRollsUsed = 0;
        _riteAccumulatedSuccesses = 0;
        _riteFailureNeedsChoice = false;
        _riteRollInFlight = false;
        _riteHadExceptionalSuccess = false;
        _addRiteDisciplineDotsToPotency = false;
        await IsVisibleChanged.InvokeAsync(IsVisible);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _diceRollSubscription?.Dispose();
    }
}
