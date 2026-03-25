using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Data.Models;
using RequiemNexus.Data.Models.Enums;
using RequiemNexus.Domain;
using RequiemNexus.Domain.Enums;
using RequiemNexus.Domain.Models;
using RequiemNexus.Web.Services;

namespace RequiemNexus.Web.Components.UI;

/// <summary>
/// Storyteller modal: build attack pool, resolve via <see cref="IAttackService"/>, apply damage to a defender initiative row.
/// </summary>
public partial class MeleeAttackResolveModal : ComponentBase
{
    private Character? _attacker;

    private string _trait1 = string.Empty;

    private string _trait2 = string.Empty;

    private int? _resolvedPool;

    private string _weaponRowId = string.Empty;

    private int _defenderEntryId;

    private int _manualNpcDefense;

    private DamageSource _damageSource = DamageSource.Weapon;

    private AttackResult? _lastResult;

    private bool _busy;

    private string _error = string.Empty;

    private string? _effectiveUserId;

    [Parameter]
    public bool IsOpen { get; set; }

    [Parameter]
    public EventCallback<bool> IsOpenChanged { get; set; }

    [Parameter]
    public int CampaignId { get; set; }

    [Parameter]
    public int EncounterId { get; set; }

    [Parameter]
    public int AttackerCharacterId { get; set; }

    [Parameter]
    public string StoryTellerUserId { get; set; } = string.Empty;

    [Parameter]
    public IReadOnlyList<InitiativeEntry> InitiativeEntries { get; set; } = [];

    [Inject]
    private IAttackService AttackService { get; set; } = default!;

    [Inject]
    private ICharacterService CharacterService { get; set; } = default!;

    [Inject]
    private ITraitResolver TraitResolver { get; set; } = default!;

    [Inject]
    private IDerivedStatService DerivedStatService { get; set; } = default!;

    [Inject]
    private ICharacterHealthService CharacterHealthService { get; set; } = default!;

    [Inject]
    private INpcCombatService NpcCombatService { get; set; } = default!;

    [Inject]
    private ToastService ToastService { get; set; } = default!;

    [Inject]
    private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

    private IEnumerable<CharacterAsset> EquippedWeapons =>
        _attacker?.CharacterAssets?
            .Where(ca => ca.Asset is WeaponAsset w && w.Damage > 0 && ca.IsEquipped && CharacterAssetActiveHelper.IsEquippedAndActive(ca))
            ?? [];

    private IEnumerable<InitiativeEntry> DefenderOptions =>
        InitiativeEntries
            .Where(e => !e.CharacterId.HasValue || e.CharacterId.Value != AttackerCharacterId)
            .OrderBy(e => e.Order);

    private InitiativeEntry? SelectedDefender =>
        InitiativeEntries.FirstOrDefault(e => e.Id == _defenderEntryId);

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        if (!IsOpen || AttackerCharacterId <= 0 || string.IsNullOrEmpty(StoryTellerUserId))
        {
            return;
        }

        AuthenticationState auth = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        _effectiveUserId = auth.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? StoryTellerUserId;

        (Character Character, bool _)? loaded = await CharacterService.GetCharacterWithAccessCheckAsync(AttackerCharacterId, _effectiveUserId);
        _attacker = loaded?.Character;

        List<InitiativeEntry> defenders = DefenderOptions.ToList();
        if (defenders.Count > 0 && defenders.All(d => d.Id != _defenderEntryId))
        {
            _defenderEntryId = defenders[0].Id;
        }

        await RefreshPoolPreviewAsync();
    }

    private async Task OnTrait1ChangedAsync(ChangeEventArgs e)
    {
        _trait1 = e.Value?.ToString() ?? string.Empty;
        await RefreshPoolPreviewAsync();
    }

    private async Task OnTrait2ChangedAsync(ChangeEventArgs e)
    {
        _trait2 = e.Value?.ToString() ?? string.Empty;
        await RefreshPoolPreviewAsync();
    }

    private async Task RefreshPoolPreviewAsync()
    {
        _resolvedPool = null;
        if (_attacker == null || string.IsNullOrEmpty(_trait1) || string.IsNullOrEmpty(_trait2))
        {
            return;
        }

        PoolDefinition? pool = SheetPoolBuilder.TryCreate(_trait1, _trait2);
        if (pool != null)
        {
            _resolvedPool = await TraitResolver.ResolvePoolAsync(_attacker, pool);
        }
    }

    private async Task CloseAsync()
    {
        IsOpen = false;
        await IsOpenChanged.InvokeAsync(false);
        ResetForm();
    }

    private void ResetForm()
    {
        _lastResult = null;
        _error = string.Empty;
        _trait1 = string.Empty;
        _trait2 = string.Empty;
        _weaponRowId = string.Empty;
        _defenderEntryId = 0;
        _manualNpcDefense = 0;
        _resolvedPool = null;
    }

    private async Task ResolveAsync()
    {
        _error = string.Empty;
        _lastResult = null;
        if (_attacker == null || string.IsNullOrEmpty(_effectiveUserId))
        {
            _error = "Attacker could not be loaded.";
            return;
        }

        PoolDefinition? pool = SheetPoolBuilder.TryCreate(_trait1, _trait2);
        if (pool == null)
        {
            _error = "Select two valid traits for the attack pool.";
            return;
        }

        InitiativeEntry? defender = SelectedDefender;
        if (defender == null)
        {
            _error = "Select a defender.";
            return;
        }

        int defense;
        if (defender.CharacterId is int defCharId)
        {
            (Character Character, bool _)? defLoaded =
                await CharacterService.GetCharacterWithAccessCheckAsync(defCharId, _effectiveUserId);
            if (defLoaded == null)
            {
                _error = "Could not load defender character.";
                return;
            }

            defense = await DerivedStatService.GetEffectiveDefenseAsync(defLoaded.Value.Character);
        }
        else
        {
            defense = Math.Max(0, _manualNpcDefense);
        }

        int? weaponId = string.IsNullOrEmpty(_weaponRowId) ? null : int.TryParse(_weaponRowId, out int w) ? w : null;

        _busy = true;
        try
        {
            _lastResult = await AttackService.ResolveMeleeAttackAsync(
                _effectiveUserId,
                EncounterId,
                AttackerCharacterId,
                defense,
                pool,
                weaponId,
                _damageSource);
            ToastService.Show("Attack resolved", "Review results and apply damage if appropriate.", ToastType.Success);
        }
        catch (Exception ex)
        {
            _error = ex.Message;
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task ApplyDamageAsync()
    {
        if (_lastResult == null || string.IsNullOrEmpty(_effectiveUserId))
        {
            return;
        }

        InitiativeEntry? defender = SelectedDefender;
        if (defender == null)
        {
            _error = "Select a defender.";
            return;
        }

        _busy = true;
        _error = string.Empty;
        try
        {
            HealthDamageKind kind = _lastResult.DamageSource.ToHealthDamageKind();
            if (defender.CharacterId is int defId)
            {
                await CharacterHealthService.ApplyDamageFromAttackAsync(defId, _effectiveUserId, _lastResult);
            }
            else
            {
                await NpcCombatService.ApplyNpcDamageBatchAsync(
                    defender.Id,
                    kind,
                    _lastResult.TotalDamageInstances,
                    _effectiveUserId);
            }

            ToastService.Show("Damage applied", $"{_lastResult.TotalDamageInstances} box(es).", ToastType.Success);
            await CloseAsync();
        }
        catch (Exception ex)
        {
            _error = ex.Message;
        }
        finally
        {
            _busy = false;
        }
    }
}
