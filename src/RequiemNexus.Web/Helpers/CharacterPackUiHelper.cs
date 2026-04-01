using RequiemNexus.Data.Models;

namespace RequiemNexus.Web.Helpers;

/// <summary>
/// Presentation-only ordering and display helpers for the character Pack tab.
/// </summary>
public static class CharacterPackUiHelper
{
    /// <summary>
    /// Orders inventory rows for display (worn first, then backpack slots, then stash).
    /// </summary>
    public static IEnumerable<CharacterAsset> PackInventoryOrder(ICollection<CharacterAsset> assets)
    {
        return assets
            .OrderByDescending(ca => ca.IsEquipped)
            .ThenBy(ca => ca.BackpackSlotIndex ?? 100)
            .ThenBy(ca => ca.Asset?.Name ?? string.Empty);
    }

    /// <summary>
    /// Returns the select value for a character asset's backpack slot (empty string when unassigned).
    /// </summary>
    public static string BackpackSlotSelectValue(CharacterAsset ca) =>
        ca.BackpackSlotIndex.HasValue ? ca.BackpackSlotIndex.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) : string.Empty;
}
