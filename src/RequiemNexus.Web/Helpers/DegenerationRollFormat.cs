namespace RequiemNexus.Web.Helpers;

/// <summary>
/// Shared copy for Storyteller Glimpse degeneration UI (banners, modals).
/// </summary>
public static class DegenerationRollFormat
{
    /// <summary>Human-readable pool summary for a degeneration save.</summary>
    public static string PoolHint(int humanity, int resolve)
    {
        if (humanity <= 0)
        {
            return "chance die (Humanity 0)";
        }

        int pool = resolve + (7 - humanity);
        return $"{resolve} + (7 − {humanity}) = {pool} dice";
    }
}
