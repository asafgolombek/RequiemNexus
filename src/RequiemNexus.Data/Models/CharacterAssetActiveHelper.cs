namespace RequiemNexus.Data.Models;

/// <summary>
/// Shared predicate for equipped, non-broken inventory rows on the character sheet.
/// </summary>
public static class CharacterAssetActiveHelper
{
    /// <summary>Returns true when the row is equipped and not at structure zero.</summary>
    public static bool IsEquippedAndActive(CharacterAsset ca) =>
        ca.IsEquipped && (ca.CurrentStructure == null || ca.CurrentStructure > 0);
}
