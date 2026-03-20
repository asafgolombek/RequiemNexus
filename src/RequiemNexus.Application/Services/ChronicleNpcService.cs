using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Application.Services;

/// <summary>
/// Application service for chronicle NPC CRUD. All mutations are Storyteller-only.
/// </summary>
public class ChronicleNpcService(
    ApplicationDbContext dbContext,
    ILogger<ChronicleNpcService> logger,
    IAuthorizationHelper authHelper) : IChronicleNpcService
{
    private readonly ApplicationDbContext _dbContext = dbContext;
    private readonly ILogger<ChronicleNpcService> _logger = logger;
    private readonly IAuthorizationHelper _authHelper = authHelper;

    /// <inheritdoc />
    public async Task<List<ChronicleNpc>> GetNpcsAsync(int campaignId, bool includeDeceased = false)
    {
        return await _dbContext.ChronicleNpcs
            .Include(n => n.PrimaryFaction)
            .Where(n => n.CampaignId == campaignId && (includeDeceased || n.IsAlive))
            .AsNoTracking()
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<ChronicleNpc?> GetNpcAsync(int npcId)
    {
        return await _dbContext.ChronicleNpcs
            .Include(n => n.PrimaryFaction)
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.Id == npcId);
    }

    /// <inheritdoc />
    public async Task<ChronicleNpc> CreateNpcAsync(
        int campaignId,
        string name,
        string? title,
        int? primaryFactionId,
        string? roleInFaction,
        string publicDescription,
        Data.Models.Enums.CreatureType creatureType,
        string stUserId)
    {
        await _authHelper.RequireStorytellerAsync(campaignId, stUserId, "modify the Danse Macabre");

        ChronicleNpc npc = new()
        {
            CampaignId = campaignId,
            Name = name,
            Title = title,
            PrimaryFactionId = primaryFactionId,
            RoleInFaction = roleInFaction,
            PublicDescription = publicDescription,
            CreatureType = creatureType,
            IsVampire = creatureType == Data.Models.Enums.CreatureType.Vampire,
        };

        _dbContext.ChronicleNpcs.Add(npc);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "NPC '{Name}' (Id={NpcId}) created in campaign {CampaignId} by ST {UserId}",
            npc.Name,
            npc.Id,
            campaignId,
            stUserId);

        return npc;
    }

    /// <inheritdoc />
    public async Task UpdateNpcAsync(
        int npcId,
        string name,
        string? title,
        int? primaryFactionId,
        string? roleInFaction,
        string publicDescription,
        string storytellerNotes,
        int? linkedStatBlockId,
        Data.Models.Enums.CreatureType creatureType,
        string attributesJson,
        string skillsJson,
        string stUserId)
    {
        ChronicleNpc npc = await _dbContext.ChronicleNpcs.FindAsync(npcId)
            ?? throw new InvalidOperationException($"NPC {npcId} not found.");

        await _authHelper.RequireStorytellerAsync(npc.CampaignId, stUserId, "modify the Danse Macabre");

        npc.Name = name;
        npc.Title = title;
        npc.PrimaryFactionId = primaryFactionId;
        npc.RoleInFaction = roleInFaction;
        npc.PublicDescription = publicDescription;
        npc.StorytellerNotes = storytellerNotes;
        npc.LinkedStatBlockId = linkedStatBlockId;
        npc.CreatureType = creatureType;
        npc.IsVampire = creatureType == Data.Models.Enums.CreatureType.Vampire;
        npc.AttributesJson = attributesJson;
        npc.SkillsJson = skillsJson;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "NPC {NpcId} updated by ST {UserId}",
            npcId,
            stUserId);
    }

    /// <inheritdoc />
    public async Task SetNpcAliveAsync(int npcId, bool isAlive, string stUserId)
    {
        ChronicleNpc npc = await _dbContext.ChronicleNpcs.FindAsync(npcId)
            ?? throw new InvalidOperationException($"NPC {npcId} not found.");

        await _authHelper.RequireStorytellerAsync(npc.CampaignId, stUserId, "modify the Danse Macabre");

        npc.IsAlive = isAlive;
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "NPC {NpcId} marked {Status} by ST {UserId}",
            npcId,
            isAlive ? "alive" : "deceased",
            stUserId);
    }

    /// <inheritdoc />
    public async Task DeleteNpcAsync(int npcId, string stUserId)
    {
        ChronicleNpc npc = await _dbContext.ChronicleNpcs.FindAsync(npcId)
            ?? throw new InvalidOperationException($"NPC {npcId} not found.");

        await _authHelper.RequireStorytellerAsync(npc.CampaignId, stUserId, "modify the Danse Macabre");

        _dbContext.ChronicleNpcs.Remove(npc);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "NPC {NpcId} deleted by ST {UserId}",
            npcId,
            stUserId);
    }
}
