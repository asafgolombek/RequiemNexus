using RequiemNexus.Data.Models;

namespace RequiemNexus.Web.Services;

/// <summary>
/// Scoped drag state for the initiative tracker so reorder UI does not rely on component fields alone.
/// </summary>
public interface IInitiativeTrackerDragState
{
    /// <summary>Gets the entry currently being dragged, if any.</summary>
    InitiativeEntry? DraggedItem { get; }

    /// <summary>Sets the entry being dragged (or clears when null).</summary>
    void SetDraggedItem(InitiativeEntry? entry);

    /// <summary>Clears any in-progress drag.</summary>
    void ClearDrag();
}
