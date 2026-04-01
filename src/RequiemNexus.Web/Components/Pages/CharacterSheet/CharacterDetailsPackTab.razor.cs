using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using RequiemNexus.Data.Models;
using RequiemNexus.Data.Models.Enums;
using RequiemNexus.Web.Helpers;

namespace RequiemNexus.Web.Components.Pages.CharacterSheet;

/// <summary>
/// Pack (equipment) tab for the interactive character sheet.
/// </summary>
public partial class CharacterDetailsPackTab : ComponentBase
{
    [Parameter]
    [EditorRequired]
    public Character Character { get; set; } = default!;

    [Parameter]
    public IReadOnlyList<Asset> AvailableAssets { get; set; } = [];

    [Parameter]
    public int SelectedAssetId { get; set; }

    [Parameter]
    public EventCallback<int> SelectedAssetIdChanged { get; set; }

    [Parameter]
    public int SelectedAssetQuantity { get; set; }

    [Parameter]
    public EventCallback<int> SelectedAssetQuantityChanged { get; set; }

    [Parameter]
    public bool IsPurchaseDisabled { get; set; }

    [Parameter]
    public string? PurchaseButtonTitle { get; set; }

    [Parameter]
    public EventCallback OnPurchaseClicked { get; set; }

    [Parameter]
    public EventCallback<(CharacterAsset Asset, ChangeEventArgs Args)> OnAssetEquippedChanged { get; set; }

    [Parameter]
    public EventCallback<(CharacterAsset Asset, ChangeEventArgs Args)> OnStructureChanged { get; set; }

    [Parameter]
    public EventCallback<(CharacterAsset Asset, ChangeEventArgs Args)> OnBackpackSlotSelect { get; set; }

    [Parameter]
    public EventCallback<int> OnClearBackpackSlot { get; set; }

    [Parameter]
    public EventCallback<CharacterAsset> OnOpenRepairRoller { get; set; }

    [Parameter]
    public EventCallback<CharacterAsset> OnOpenForgeModal { get; set; }

    [Parameter]
    public EventCallback<int> OnRemoveCharacterAsset { get; set; }

    private async Task OnAssetSelectChanged(ChangeEventArgs e)
    {
        int id = int.TryParse(e.Value?.ToString(), out int n) ? n : 0;
        SelectedAssetId = id;
        await SelectedAssetIdChanged.InvokeAsync(id);
    }

    private async Task OnQuantityChanged(ChangeEventArgs e)
    {
        int q = int.TryParse(e.Value?.ToString(), out int n) ? Math.Max(1, n) : 1;
        SelectedAssetQuantity = q;
        await SelectedAssetQuantityChanged.InvokeAsync(q);
    }
}
