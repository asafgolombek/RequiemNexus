using RequiemNexus.Data.Models;

namespace RequiemNexus.Web.Services;

/// <inheritdoc />
public sealed class InitiativeTrackerDragState : IInitiativeTrackerDragState
{
    /// <inheritdoc />
    public InitiativeEntry? DraggedItem { get; private set; }

    /// <inheritdoc />
    public void SetDraggedItem(InitiativeEntry? entry) => DraggedItem = entry;

    /// <inheritdoc />
    public void ClearDrag() => DraggedItem = null;
}
