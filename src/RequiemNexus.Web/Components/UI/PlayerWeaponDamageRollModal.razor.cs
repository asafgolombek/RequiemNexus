using Microsoft.AspNetCore.Components;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Data.Models;
using RequiemNexus.Web.Services;

namespace RequiemNexus.Web.Components.UI;

/// <summary>
/// Player modal: choose equipped weapon (or unarmed) and roll weapon damage for an active encounter via SignalR.
/// </summary>
public partial class PlayerWeaponDamageRollModal : ComponentBase
{
    private Character? _character;

    private string _weaponRowId = string.Empty;

    private bool _busy;

    private string _error = string.Empty;

    /// <summary>Gets or sets whether the modal is visible.</summary>
    [Parameter]
    public bool IsOpen { get; set; }

    /// <summary>Gets or sets the callback when open state changes.</summary>
    [Parameter]
    public EventCallback<bool> IsOpenChanged { get; set; }

    /// <summary>Gets or sets the campaign (chronicle) id.</summary>
    [Parameter]
    public int CampaignId { get; set; }

    /// <summary>Gets or sets the combat encounter id.</summary>
    [Parameter]
    public int EncounterId { get; set; }

    /// <summary>Gets or sets the rolling character id.</summary>
    [Parameter]
    public int CharacterId { get; set; }

    /// <summary>Gets or sets the authenticated user's id (character owner).</summary>
    [Parameter]
    public string UserId { get; set; } = string.Empty;

    [Inject]
    private ICharacterService CharacterService { get; set; } = default!;

    [Inject]
    private SessionClientService SessionClient { get; set; } = default!;

    private IEnumerable<CharacterAsset> EquippedWeapons =>
        _character?.CharacterAssets?
            .Where(ca => ca.Asset is WeaponAsset w && w.Damage > 0 && ca.IsEquipped && CharacterAssetActiveHelper.IsEquippedAndActive(ca))
            ?? [];

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        _error = string.Empty;
        if (!IsOpen || CharacterId <= 0 || string.IsNullOrEmpty(UserId))
        {
            return;
        }

        (Character Character, bool _)? loaded = await CharacterService.GetCharacterWithAccessCheckAsync(CharacterId, UserId);
        _character = loaded?.Character;
        if (_character == null)
        {
            _error = "Character could not be loaded.";
        }
    }

    private async Task CloseAsync()
    {
        IsOpen = false;
        await IsOpenChanged.InvokeAsync(false);
        _weaponRowId = string.Empty;
        _error = string.Empty;
    }

    private async Task RollAsync()
    {
        _error = string.Empty;
        if (_character == null)
        {
            _error = "Character could not be loaded.";
            return;
        }

        int? weaponId = null;
        if (!string.IsNullOrEmpty(_weaponRowId))
        {
            if (!int.TryParse(_weaponRowId, out int w) || w <= 0)
            {
                _error = "Invalid weapon selection.";
                return;
            }

            weaponId = w;
        }

        _busy = true;
        try
        {
            var outcome = await SessionClient.RollEncounterWeaponDamageAsync(
                CampaignId,
                EncounterId,
                CharacterId,
                weaponId);

            if (outcome != null)
            {
                await CloseAsync();
            }
        }
        finally
        {
            _busy = false;
        }
    }
}
