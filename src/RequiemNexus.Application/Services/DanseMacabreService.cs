using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Domain.Enums;

namespace RequiemNexus.Application.Services;

/// <summary>
/// Application service for the Danse Macabre subsystem — city factions, chronicle NPCs,
/// feeding territories, and faction relationships. All mutations are Storyteller-only.
/// </summary>
public class DanseMacabreService(
    ApplicationDbContext dbContext,
    ILogger<DanseMacabreService> logger) : IDanseMacabreService
{
    private readonly ApplicationDbContext _dbContext = dbContext;
    private readonly ILogger<DanseMacabreService> _logger = logger;

    /// <inheritdoc />
    public async Task<List<CityFaction>> GetFactionsAsync(int campaignId)
    {
        return await _dbContext.CityFactions
            .Include(f => f.LeaderNpc)
            .Include(f => f.Members)
            .Include(f => f.ControlledTerritories)
            .Where(f => f.CampaignId == campaignId)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<CityFaction?> GetFactionAsync(int factionId)
    {
        return await _dbContext.CityFactions
            .Include(f => f.LeaderNpc)
            .Include(f => f.Members)
            .Include(f => f.ControlledTerritories)
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == factionId);
    }

    /// <inheritdoc />
    public async Task<CityFaction> CreateFactionAsync(
        int campaignId,
        string name,
        FactionType type,
        int influenceRating,
        string publicDescription,
        string stUserId)
    {
        await RequireStAsync(campaignId, stUserId);

        CityFaction faction = new()
        {
            CampaignId = campaignId,
            Name = name,
            FactionType = type,
            InfluenceRating = influenceRating,
            PublicDescription = publicDescription,
        };

        _dbContext.CityFactions.Add(faction);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Faction '{Name}' (Id={FactionId}) created in campaign {CampaignId} by ST {UserId}",
            faction.Name,
            faction.Id,
            campaignId,
            stUserId);

        return faction;
    }

    /// <inheritdoc />
    public async Task UpdateFactionAsync(
        int factionId,
        string name,
        FactionType type,
        int influenceRating,
        string publicDescription,
        string storytellerNotes,
        string agenda,
        int? leaderNpcId,
        string stUserId)
    {
        CityFaction faction = await _dbContext.CityFactions.FindAsync(factionId)
            ?? throw new InvalidOperationException($"Faction {factionId} not found.");

        await RequireStAsync(faction.CampaignId, stUserId);

        faction.Name = name;
        faction.FactionType = type;
        faction.InfluenceRating = influenceRating;
        faction.PublicDescription = publicDescription;
        faction.StorytellerNotes = storytellerNotes;
        faction.Agenda = agenda;
        faction.LeaderNpcId = leaderNpcId;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Faction {FactionId} updated by ST {UserId}",
            factionId,
            stUserId);
    }

    /// <inheritdoc />
    public async Task DeleteFactionAsync(int factionId, string stUserId)
    {
        CityFaction faction = await _dbContext.CityFactions.FindAsync(factionId)
            ?? throw new InvalidOperationException($"Faction {factionId} not found.");

        await RequireStAsync(faction.CampaignId, stUserId);

        _dbContext.CityFactions.Remove(faction);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Faction {FactionId} deleted by ST {UserId}",
            factionId,
            stUserId);
    }

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
        string stUserId)
    {
        await RequireStAsync(campaignId, stUserId);

        ChronicleNpc npc = new()
        {
            CampaignId = campaignId,
            Name = name,
            Title = title,
            PrimaryFactionId = primaryFactionId,
            RoleInFaction = roleInFaction,
            PublicDescription = publicDescription,
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
        bool isVampire,
        string attributesJson,
        string skillsJson,
        string stUserId)
    {
        ChronicleNpc npc = await _dbContext.ChronicleNpcs.FindAsync(npcId)
            ?? throw new InvalidOperationException($"NPC {npcId} not found.");

        await RequireStAsync(npc.CampaignId, stUserId);

        npc.Name = name;
        npc.Title = title;
        npc.PrimaryFactionId = primaryFactionId;
        npc.RoleInFaction = roleInFaction;
        npc.PublicDescription = publicDescription;
        npc.StorytellerNotes = storytellerNotes;
        npc.LinkedStatBlockId = linkedStatBlockId;
        npc.IsVampire = isVampire;
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

        await RequireStAsync(npc.CampaignId, stUserId);

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

        await RequireStAsync(npc.CampaignId, stUserId);

        _dbContext.ChronicleNpcs.Remove(npc);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "NPC {NpcId} deleted by ST {UserId}",
            npcId,
            stUserId);
    }

    /// <inheritdoc />
    public async Task<List<FeedingTerritory>> GetTerritoriesAsync(int campaignId)
    {
        return await _dbContext.FeedingTerritories
            .Include(t => t.ControlledByFaction)
            .Where(t => t.CampaignId == campaignId)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<FeedingTerritory> CreateTerritoryAsync(
        int campaignId,
        string name,
        string description,
        int rating,
        int? controlledByFactionId,
        string stUserId)
    {
        await RequireStAsync(campaignId, stUserId);

        FeedingTerritory territory = new()
        {
            CampaignId = campaignId,
            Name = name,
            Description = description,
            Rating = rating,
            ControlledByFactionId = controlledByFactionId,
        };

        _dbContext.FeedingTerritories.Add(territory);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Territory '{Name}' (Id={TerritoryId}) created in campaign {CampaignId} by ST {UserId}",
            territory.Name,
            territory.Id,
            campaignId,
            stUserId);

        return territory;
    }

    /// <inheritdoc />
    public async Task UpdateTerritoryAsync(
        int territoryId,
        string name,
        string description,
        int rating,
        int? controlledByFactionId,
        string stUserId)
    {
        FeedingTerritory territory = await _dbContext.FeedingTerritories.FindAsync(territoryId)
            ?? throw new InvalidOperationException($"Territory {territoryId} not found.");

        await RequireStAsync(territory.CampaignId, stUserId);

        territory.Name = name;
        territory.Description = description;
        territory.Rating = rating;
        territory.ControlledByFactionId = controlledByFactionId;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Territory {TerritoryId} updated by ST {UserId}",
            territoryId,
            stUserId);
    }

    /// <inheritdoc />
    public async Task DeleteTerritoryAsync(int territoryId, string stUserId)
    {
        FeedingTerritory territory = await _dbContext.FeedingTerritories.FindAsync(territoryId)
            ?? throw new InvalidOperationException($"Territory {territoryId} not found.");

        await RequireStAsync(territory.CampaignId, stUserId);

        _dbContext.FeedingTerritories.Remove(territory);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Territory {TerritoryId} deleted by ST {UserId}",
            territoryId,
            stUserId);
    }

    /// <inheritdoc />
    public async Task<List<FactionRelationship>> GetRelationshipsAsync(int campaignId)
    {
        return await _dbContext.FactionRelationships
            .Include(r => r.FactionA)
            .Include(r => r.FactionB)
            .Where(r => r.CampaignId == campaignId)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<FactionRelationship> SetRelationshipAsync(
        int campaignId,
        int factionAId,
        int factionBId,
        FactionStance stance,
        string notes,
        string stUserId)
    {
        await RequireStAsync(campaignId, stUserId);

        FactionRelationship? existing = await _dbContext.FactionRelationships
            .FirstOrDefaultAsync(r => r.CampaignId == campaignId
                                   && r.FactionAId == factionAId
                                   && r.FactionBId == factionBId);

        if (existing is not null)
        {
            existing.StanceFromA = stance;
            existing.Notes = notes;
            await _dbContext.SaveChangesAsync();
            return existing;
        }

        FactionRelationship relationship = new()
        {
            CampaignId = campaignId,
            FactionAId = factionAId,
            FactionBId = factionBId,
            StanceFromA = stance,
            Notes = notes,
        };

        _dbContext.FactionRelationships.Add(relationship);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Faction relationship {FactionAId} → {FactionBId} set to {Stance} in campaign {CampaignId} by ST {UserId}",
            factionAId,
            factionBId,
            stance,
            campaignId,
            stUserId);

        return relationship;
    }

    /// <inheritdoc />
    public async Task DeleteRelationshipAsync(int relationshipId, string stUserId)
    {
        FactionRelationship relationship = await _dbContext.FactionRelationships.FindAsync(relationshipId)
            ?? throw new InvalidOperationException($"Relationship {relationshipId} not found.");

        await RequireStAsync(relationship.CampaignId, stUserId);

        _dbContext.FactionRelationships.Remove(relationship);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Faction relationship {RelationshipId} deleted by ST {UserId}",
            relationshipId,
            stUserId);
    }

    private async Task RequireStAsync(int campaignId, string stUserId)
    {
        Campaign? campaign = await _dbContext.Campaigns.FindAsync(campaignId)
            ?? throw new InvalidOperationException($"Campaign {campaignId} not found.");

        if (campaign.StoryTellerId != stUserId)
        {
            _logger.LogWarning(
                "Unauthorized Danse Macabre mutation on campaign {CampaignId} by user {UserId}",
                campaignId,
                stUserId);
            throw new UnauthorizedAccessException("Only the Storyteller may modify the Danse Macabre.");
        }
    }
}
