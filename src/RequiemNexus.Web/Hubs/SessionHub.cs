using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using RequiemNexus.Application.RealTime;
using RequiemNexus.Data.RealTime;

namespace RequiemNexus.Web.Hubs;

/// <summary>
/// A thin relay for real-time play sessions.
/// All logic and authorization decisions are delegated to ISessionService and ISessionAuthorizationService.
/// </summary>
[Authorize]
public class SessionHub(ISessionService sessionService, ISessionAuthorizationService authService) : Hub<ISessionClient>
{
    private string UserId => Context.UserIdentifier ?? throw new HubException("Unauthenticated");

    /// <summary>
    /// Removes the player from any active sessions on disconnect.
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await sessionService.LeaveSessionAsync(UserId, Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Storyteller method to initialize a play session.
    /// </summary>
    public async Task StartSession(int chronicleId)
    {
        if (!await authService.IsStorytellerAsync(UserId, chronicleId))
        {
            throw new HubException("Forbidden: Only the Storyteller can start a session");
        }

        await sessionService.StartSessionAsync(UserId, chronicleId);
    }

    /// <summary>
    /// Storyteller method to terminate a play session.
    /// </summary>
    public async Task EndSession(int chronicleId)
    {
        if (!await authService.IsStorytellerAsync(UserId, chronicleId))
        {
            throw new HubException("Forbidden: Only the Storyteller can end a session");
        }

        await sessionService.EndSessionAsync(UserId, chronicleId);
    }

    /// <summary>
    /// Player method to join an active session.
    /// </summary>
    public async Task JoinSession(int chronicleId, int? characterId)
    {
        if (!await authService.IsMemberAsync(UserId, chronicleId))
        {
            throw new HubException("Forbidden: Not a member of this chronicle");
        }

        if (characterId.HasValue && !await authService.IsCharacterOwnerAsync(UserId, characterId.Value))
        {
            throw new HubException("Forbidden: You do not own this character");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, chronicleId.ToString());
        await sessionService.JoinSessionAsync(UserId, chronicleId, characterId, Context.ConnectionId);
    }

    /// <summary>
    /// Broadcasts a server-side dice roll to the chronicle group.
    /// </summary>
    public async Task RollDice(int chronicleId, int pool, string description, bool tenAgain, bool nineAgain, bool eightAgain, bool isRote)
    {
        if (chronicleId <= 0)
        {
            throw new HubException("Invalid ChronicleId");
        }

        if (pool < 0 || pool > 50)
        {
            throw new HubException("Pool must be between 0 and 50");
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new HubException("Description is required");
        }

        if (description.Length > 100)
        {
            throw new HubException("Description is too long");
        }

        if (!await authService.IsMemberAsync(UserId, chronicleId))
        {
            throw new HubException("Forbidden: Not a member of this chronicle");
        }

        // Note: The service validates that the pool is non-negative and performs the actual roll.
        await sessionService.RollDiceAsync(UserId, chronicleId, pool, description, tenAgain, nineAgain, eightAgain, isRote);
    }

    /// <summary>
    /// Updates the shared initiative tracker. ST only.
    /// </summary>
    public async Task UpdateInitiative(int chronicleId, IEnumerable<InitiativeEntryDto> entries)
    {
        if (chronicleId <= 0)
        {
            throw new HubException("Invalid ChronicleId");
        }

        if (entries == null)
        {
            throw new HubException("Entries are required");
        }

        var entryList = entries.ToList();
        if (entryList.Count > 100)
        {
            throw new HubException("Too many initiative entries");
        }

        if (!await authService.IsStorytellerAsync(UserId, chronicleId))
        {
            throw new HubException("Forbidden: Only the Storyteller can update initiative");
        }

        await sessionService.UpdateInitiativeAsync(UserId, chronicleId, entryList);
    }

    /// <summary>
    /// Storyteller heartbeat to prevent session auto-termination.
    /// </summary>
    public async Task Heartbeat(int chronicleId)
    {
        if (!await authService.IsStorytellerAsync(UserId, chronicleId))
        {
            throw new HubException("Forbidden: Only the Storyteller can send heartbeats");
        }

        await sessionService.HeartbeatAsync(UserId, chronicleId);
    }
}
