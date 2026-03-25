using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Application.DTOs;
using RequiemNexus.Application.RealTime;
using RequiemNexus.Data.Models;
using RequiemNexus.Data.RealTime;

namespace RequiemNexus.Web.Hubs;

/// <summary>
/// A thin relay for real-time play sessions.
/// All logic and authorization decisions are delegated to ISessionService and ISessionAuthorizationService.
/// </summary>
[Authorize]
public class SessionHub(
    ISessionService sessionService,
    ISessionAuthorizationService authService,
    IEncounterWeaponDamageRollService encounterWeaponDamageRollService,
    IAuditLogService auditLog) : Hub<ISessionClient>
{
    private string UserId => Context.UserIdentifier ?? throw new HubException("Unauthenticated");

    private string? IpAddress => Context.GetHttpContext()?.Connection.RemoteIpAddress?.ToString();

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
            await LogFailure(nameof(StartSession), chronicleId);
            throw new HubException("Forbidden: Only the Storyteller can start a session");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, chronicleId.ToString());
        await sessionService.StartSessionAsync(UserId, chronicleId);
    }

    /// <summary>
    /// Storyteller method to terminate a play session.
    /// </summary>
    public async Task EndSession(int chronicleId)
    {
        if (!await authService.IsStorytellerAsync(UserId, chronicleId))
        {
            await LogFailure(nameof(EndSession), chronicleId);
            throw new HubException("Forbidden: Only the Storyteller can end a session");
        }

        await sessionService.EndSessionAsync(UserId, chronicleId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, chronicleId.ToString());
    }

    /// <summary>
    /// Player method to join an active session.
    /// </summary>
    public async Task JoinSession(int chronicleId, int? characterId)
    {
        if (!await authService.IsMemberAsync(UserId, chronicleId))
        {
            await LogFailure(nameof(JoinSession), chronicleId, "Not a member");
            throw new HubException("Forbidden: Not a member of this chronicle");
        }

        var userName = Context.User?.Identity?.Name ?? "Unknown Player";

        if (characterId.HasValue)
        {
            if (!await authService.IsCharacterOwnerAsync(UserId, characterId.Value))
            {
                await LogFailure(nameof(JoinSession), chronicleId, $"Not owner of character {characterId.Value}");
                throw new HubException("Forbidden: You do not own this character");
            }

            // SECURITY: Verify character actually belongs to this chronicle
            if (!await authService.IsCharacterInChronicleAsync(characterId.Value, chronicleId))
            {
                await LogFailure(nameof(JoinSession), chronicleId, $"Character {characterId.Value} not in chronicle");
                throw new HubException("Forbidden: Character does not belong to this chronicle");
            }
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, chronicleId.ToString());
        await sessionService.JoinSessionAsync(UserId, userName, chronicleId, characterId, Context.ConnectionId);
    }

    /// <summary>
    /// Broadcasts a server-side dice roll to the chronicle group.
    /// </summary>
    public async Task RollDice(int chronicleId, int? characterId, int pool, string description, bool tenAgain, bool nineAgain, bool eightAgain, bool isRote)
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
            await LogFailure(nameof(RollDice), chronicleId, "Not a member");
            throw new HubException("Forbidden: Not a member of this chronicle");
        }

        if (characterId.HasValue)
        {
            if (!await authService.IsCharacterOwnerAsync(UserId, characterId.Value))
            {
                await LogFailure(nameof(RollDice), chronicleId, $"Not owner of character {characterId.Value}");
                throw new HubException("Forbidden: You do not own this character");
            }

            if (!await authService.IsCharacterInChronicleAsync(characterId.Value, chronicleId))
            {
                await LogFailure(nameof(RollDice), chronicleId, $"Character {characterId.Value} not in chronicle");
                throw new HubException("Forbidden: Character does not belong to this chronicle");
            }
        }

        // Note: The service validates that the pool is non-negative and performs the actual roll.
        await sessionService.RollDiceAsync(UserId, chronicleId, characterId, pool, description, tenAgain, nineAgain, eightAgain, isRote);
    }

    /// <summary>
    /// Character owner rolls melee weapon damage during an active encounter; pool is resolved on the server and broadcast to the chronicle.
    /// </summary>
    public async Task<EncounterWeaponDamageRollOutcomeDto> RollEncounterWeaponDamage(
        int chronicleId,
        int encounterId,
        int characterId,
        int? weaponCharacterAssetId)
    {
        if (chronicleId <= 0 || encounterId <= 0 || characterId <= 0)
        {
            throw new HubException("Invalid parameters.");
        }

        if (!await authService.IsMemberAsync(UserId, chronicleId))
        {
            await LogFailure(nameof(RollEncounterWeaponDamage), chronicleId, "Not a member");
            throw new HubException("Forbidden: Not a member of this chronicle");
        }

        if (!await authService.IsCharacterOwnerAsync(UserId, characterId))
        {
            await LogFailure(nameof(RollEncounterWeaponDamage), chronicleId, $"Not owner of character {characterId}");
            throw new HubException("Forbidden: You do not own this character");
        }

        if (!await authService.IsCharacterInChronicleAsync(characterId, chronicleId))
        {
            await LogFailure(nameof(RollEncounterWeaponDamage), chronicleId, $"Character {characterId} not in chronicle");
            throw new HubException("Forbidden: Character does not belong to this chronicle");
        }

        try
        {
            return await encounterWeaponDamageRollService.RollAndPublishAsync(
                UserId,
                chronicleId,
                encounterId,
                characterId,
                weaponCharacterAssetId);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new HubException(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            throw new HubException(ex.Message);
        }
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
            await LogFailure(nameof(UpdateInitiative), chronicleId);
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
            await LogFailure(nameof(Heartbeat), chronicleId);
            throw new HubException("Forbidden: Only the Storyteller can send heartbeats");
        }

        await sessionService.HeartbeatAsync(UserId, chronicleId);
    }

    private async Task LogFailure(string operation, int chronicleId, string? reason = null)
    {
        await auditLog.LogAsync(
            UserId,
            AuditEventType.HubAuthorizationFailed,
            IpAddress,
            $"Op: {operation}, Chronicle: {chronicleId}{(reason != null ? $", Reason: {reason}" : string.Empty)}");
    }
}
