using Microsoft.EntityFrameworkCore;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Data.RealTime;
using RequiemNexus.Domain.Contracts;

namespace RequiemNexus.Application.RealTime;

/// <summary>
/// Orchestrates all real-time session operations, combining Redis state
/// with domain logic and SignalR broadcasting.
/// </summary>
public class SessionService(
    ISessionStateRepository repository,
    ISessionPublisher publisher,
    IDiceService diceService,
    RealTimeMetrics metrics,
    ApplicationDbContext db,
    IAuditLogService auditLog) : ISessionService
{
    private static readonly TimeSpan _sessionTtl = TimeSpan.FromMinutes(15);

    private readonly ISessionStateRepository _repository = repository;
    private readonly ISessionPublisher _publisher = publisher;
    private readonly IDiceService _diceService = diceService;
    private readonly RealTimeMetrics _metrics = metrics;
    private readonly ApplicationDbContext _db = db;
    private readonly IAuditLogService _auditLog = auditLog;

    /// <inheritdoc />
    public async Task StartSessionAsync(string userId, int chronicleId)
    {
        // 1. Identify: ST userId provided
        // 2. Load: Check if session already exists
        if (await _repository.SessionExistsAsync(chronicleId))
        {
            return; // Idempotent
        }

        // 3. Verify: StartSession already authorized by SessionHub calling ISessionAuthorizationService
        // 4. Proceed: Create ephemeral state in Redis and notify group
        await _repository.CreateSessionAsync(chronicleId, userId, _sessionTtl);
        _metrics.SessionStarted();
        await _publisher.Group(chronicleId).SessionStarted();
        await _auditLog.LogAsync(userId, AuditEventType.SessionStarted, details: $"Chronicle: {chronicleId}");
    }

    /// <inheritdoc />
    public async Task EndSessionAsync(string userId, int chronicleId)
    {
        await _repository.DeleteSessionAsync(chronicleId);
        _metrics.SessionEnded();
        await _publisher.Group(chronicleId).SessionEnded("Storyteller closed the session.");
        await _publisher.Group(chronicleId).ReceivePresenceUpdate(new List<PlayerPresenceDto>());
        await _auditLog.LogAsync(userId, AuditEventType.SessionEnded, details: $"Chronicle: {chronicleId}");
    }

    /// <inheritdoc />
    public async Task JoinSessionAsync(string userId, string userName, int chronicleId, int? characterId, string connectionId)
    {
        // 1. Identify: User joining
        // 2. Load: Use provided userName (Performance: avoid DB query)
        var presence = new PlayerPresenceDto(userId, userName, characterId, true);

        // 3. Verify: Join authorized by SessionHub
        // 4. Proceed: Update Redis and notify group
        await _repository.AddPlayerAsync(chronicleId, presence);
        await _repository.MapUserToSessionAsync(userId, chronicleId);
        _metrics.PlayerJoined();

        // Broadcast presence update to everyone
        var allPlayers = await _repository.GetPlayersAsync(chronicleId);
        await _publisher.Group(chronicleId).ReceivePresenceUpdate(allPlayers);

        // Hydrate the joining client with history
        var rollHistory = await _repository.GetRollHistoryAsync(chronicleId);
        await _publisher.User(userId).ReceiveRollHistory(rollHistory);

        var initiative = await _repository.GetInitiativeAsync(chronicleId);
        await _publisher.User(userId).ReceiveInitiativeUpdate(initiative);
    }

    /// <inheritdoc />
    public async Task LeaveSessionAsync(string userId, string connectionId)
    {
        var chronicleId = await _repository.GetUserSessionAsync(userId);
        if (chronicleId.HasValue)
        {
            await _repository.RemovePlayerAsync(chronicleId.Value, userId);
            await _repository.UnmapUserFromSessionAsync(userId);
            _metrics.PlayerLeft();

            // Notify group of player departure
            await _publisher.Group(chronicleId.Value).PlayerLeft(userId);

            // Broadcast updated presence list
            var allPlayers = await _repository.GetPlayersAsync(chronicleId.Value);
            await _publisher.Group(chronicleId.Value).ReceivePresenceUpdate(allPlayers);
        }
    }

    /// <inheritdoc />
    public async Task RollDiceAsync(string userId, int chronicleId, int? characterId, int pool, string description, bool tenAgain, bool nineAgain, bool eightAgain, bool isRote)
    {
        // 1. Identify: User rolling
        // 2. Load: Get user name
        var userName = await _db.Users
            .Where(u => u.Id == userId)
            .Select(u => u.UserName)
            .FirstOrDefaultAsync() ?? "Unknown Player";

        // 3. Verify: Authorized by SessionHub
        // 4. Proceed: Server-side roll (Integrity)
        var result = _diceService.Roll(pool, tenAgain, nineAgain, eightAgain, isRote);

        var rollDto = new DiceRollResultDto(
            userName,
            userId,
            characterId,
            description,
            result.Successes,
            result.IsExceptionalSuccess,
            result.IsDramaticFailure,
            result.DiceRolled,
            DateTimeOffset.UtcNow);

        await _repository.AddRollAsync(chronicleId, rollDto);
        _metrics.RecordRoll();
        await _publisher.Group(chronicleId).ReceiveDiceRoll(rollDto);
    }

    /// <inheritdoc />
    public async Task UpdateInitiativeAsync(string userId, int chronicleId, IEnumerable<InitiativeEntryDto> entries)
    {
        await _repository.UpdateInitiativeAsync(chronicleId, entries);
        await _publisher.Group(chronicleId).ReceiveInitiativeUpdate(entries);
    }

    /// <inheritdoc />
    public async Task HeartbeatAsync(string userId, int chronicleId)
    {
        await _repository.RefreshSessionAsync(chronicleId, _sessionTtl);
    }

    /// <inheritdoc />
    public async Task BroadcastCharacterUpdateAsync(int characterId)
    {
        var character = await _db.Characters
            .FirstOrDefaultAsync(c => c.Id == characterId);

        if (character?.CampaignId.HasValue == true)
        {
            var activeConditions = await _db.CharacterConditions
                .Where(c => c.CharacterId == characterId && !c.IsResolved)
                .Select(c => c.CustomName ?? c.ConditionType.ToString())
                .ToListAsync();

            var activeTilts = await _db.CharacterTilts
                .Where(t => t.CharacterId == characterId && t.IsActive)
                .Select(t => t.CustomName ?? t.TiltType.ToString())
                .ToListAsync();

            var combinedConditions = activeConditions.Concat(activeTilts).ToList();

            var update = new CharacterUpdateDto(
                characterId,
                CurrentHealth: character.CurrentHealth,
                MaxHealth: character.MaxHealth,
                CurrentWillpower: character.CurrentWillpower,
                MaxWillpower: character.MaxWillpower,
                CurrentVitae: character.CurrentVitae,
                MaxVitae: character.MaxVitae,
                Humanity: character.Humanity,
                Armor: character.Armor,
                ActiveConditions: combinedConditions);

            await _publisher.Group(character.CampaignId.Value).ReceiveCharacterUpdate(update);
        }
    }

    /// <inheritdoc />
    public async Task BroadcastChronicleUpdateAsync(ChronicleUpdateDto patch)
    {
        await _publisher.Group(patch.ChronicleId).ReceiveChronicleUpdate(patch);
    }

    /// <inheritdoc />
    public async Task<SessionStateDto?> GetSessionStateAsync(int chronicleId)
    {
        if (!await _repository.SessionExistsAsync(chronicleId))
        {
            return null;
        }

        var presence = await _repository.GetPlayersAsync(chronicleId);
        var rollHistory = await _repository.GetRollHistoryAsync(chronicleId);
        var initiative = await _repository.GetInitiativeAsync(chronicleId);

        return new SessionStateDto(chronicleId, presence, rollHistory, initiative);
    }
}
