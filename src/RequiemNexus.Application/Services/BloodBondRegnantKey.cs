namespace RequiemNexus.Application.Services;

/// <summary>
/// Builds the synthetic <c>RegnantKey</c> stored on <see cref="RequiemNexus.Data.Models.BloodBond"/> rows.
/// </summary>
public static class BloodBondRegnantKey
{
    /// <summary>Key for a PC regnant.</summary>
    public static string ForCharacter(int regnantCharacterId) => $"c:{regnantCharacterId}";

    /// <summary>Key for a chronicle NPC regnant.</summary>
    public static string ForNpc(int regnantNpcId) => $"n:{regnantNpcId}";

    /// <summary>Key for a display-name-only regnant (trim + lowercase invariant).</summary>
    public static string ForDisplayName(string displayName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        return $"d:{displayName.Trim().ToLowerInvariant()}";
    }
}
