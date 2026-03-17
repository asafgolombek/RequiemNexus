using System.Text.Json;
using StackExchange.Redis;

namespace RequiemNexus.Data.RealTime;

/// <summary>
/// Redis-backed implementation of session state persistence.
/// All data is ephemeral and subject to the session TTL.
/// </summary>
public class SessionStateRepository(IConnectionMultiplexer redis) : ISessionStateRepository
{
    private readonly IDatabase _db = redis.GetDatabase();

    /// <inheritdoc />
    public async Task CreateSessionAsync(int chronicleId, string stUserId, TimeSpan ttl)
    {
        var info = new { StUserId = stUserId, StartedAt = DateTimeOffset.UtcNow };
        await _db.StringSetAsync(InfoKey(chronicleId), JsonSerializer.Serialize(info), ttl);
    }

    /// <inheritdoc />
    public async Task RefreshSessionAsync(int chronicleId, TimeSpan ttl)
    {
        var batch = _db.CreateBatch();
        _ = batch.KeyExpireAsync(InfoKey(chronicleId), ttl);
        _ = batch.KeyExpireAsync(PlayersKey(chronicleId), ttl);
        _ = batch.KeyExpireAsync(RollsKey(chronicleId), ttl);
        _ = batch.KeyExpireAsync(InitiativeKey(chronicleId), ttl);
        batch.Execute();
        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task DeleteSessionAsync(int chronicleId)
    {
        var batch = _db.CreateBatch();
        _ = batch.KeyDeleteAsync(InfoKey(chronicleId));
        _ = batch.KeyDeleteAsync(PlayersKey(chronicleId));
        _ = batch.KeyDeleteAsync(RollsKey(chronicleId));
        _ = batch.KeyDeleteAsync(InitiativeKey(chronicleId));
        batch.Execute();
        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<bool> SessionExistsAsync(int chronicleId)
    {
        return await _db.KeyExistsAsync(InfoKey(chronicleId));
    }

    /// <inheritdoc />
    public async Task AddPlayerAsync(int chronicleId, PlayerPresenceDto player)
    {
        await _db.HashSetAsync(PlayersKey(chronicleId), player.UserId, JsonSerializer.Serialize(player));

        // Ensure the players list has a TTL even if a session isn't active (Lobby presence)
        // We use a 30-minute window for the lobby.
        await _db.KeyExpireAsync(PlayersKey(chronicleId), TimeSpan.FromMinutes(30));
    }

    /// <inheritdoc />
    public async Task RemovePlayerAsync(int chronicleId, string userId)
    {
        await _db.HashDeleteAsync(PlayersKey(chronicleId), userId);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<PlayerPresenceDto>> GetPlayersAsync(int chronicleId)
    {
        var entries = await _db.HashGetAllAsync(PlayersKey(chronicleId));
        return entries.Select(e => JsonSerializer.Deserialize<PlayerPresenceDto>((string)e.Value!)!);
    }

    /// <inheritdoc />
    public async Task MapUserToSessionAsync(string userId, int chronicleId)
    {
        await _db.StringSetAsync(UserSessionKey(userId), chronicleId);
    }

    /// <inheritdoc />
    public async Task UnmapUserFromSessionAsync(string userId)
    {
        await _db.KeyDeleteAsync(UserSessionKey(userId));
    }

    /// <inheritdoc />
    public async Task<int?> GetUserSessionAsync(string userId)
    {
        var val = await _db.StringGetAsync(UserSessionKey(userId));
        return val.HasValue ? (int)val : null;
    }

    /// <inheritdoc />
    public async Task AddRollAsync(int chronicleId, DiceRollResultDto roll)
    {
        var json = JsonSerializer.Serialize(roll);
        await _db.ListLeftPushAsync(RollsKey(chronicleId), json);
        await _db.ListTrimAsync(RollsKey(chronicleId), 0, 99); // Cap at 100
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DiceRollResultDto>> GetRollHistoryAsync(int chronicleId)
    {
        var entries = await _db.ListRangeAsync(RollsKey(chronicleId));
        return entries.Select(e => JsonSerializer.Deserialize<DiceRollResultDto>((string)e!)!);
    }

    /// <inheritdoc />
    public async Task UpdateInitiativeAsync(int chronicleId, IEnumerable<InitiativeEntryDto> entries)
    {
        // For initiative, we overwrite the ZSET to ensure ordering
        await _db.KeyDeleteAsync(InitiativeKey(chronicleId));
        var zEntries = entries.Select((e, i) => new SortedSetEntry(JsonSerializer.Serialize(e), i)).ToArray();
        if (zEntries.Length > 0)
        {
            await _db.SortedSetAddAsync(InitiativeKey(chronicleId), zEntries);
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<InitiativeEntryDto>> GetInitiativeAsync(int chronicleId)
    {
        var entries = await _db.SortedSetRangeByRankAsync(InitiativeKey(chronicleId));
        return entries.Select(e => JsonSerializer.Deserialize<InitiativeEntryDto>((string)e!)!);
    }

    private static string InfoKey(int chronicleId) => $"session:{chronicleId}:info";

    private static string PlayersKey(int chronicleId) => $"session:{chronicleId}:players";

    private static string RollsKey(int chronicleId) => $"session:{chronicleId}:rolls";

    private static string InitiativeKey(int chronicleId) => $"session:{chronicleId}:initiative";

    private static string UserSessionKey(string userId) => $"user:{userId}:session";
}
