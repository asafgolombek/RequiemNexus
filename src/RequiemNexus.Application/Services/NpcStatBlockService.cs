using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Application.Services;

/// <summary>
/// Application service for managing NPC stat blocks — pre-built canonical entries and
/// campaign-specific custom blocks created by the Storyteller.
/// </summary>
public class NpcStatBlockService(
    ApplicationDbContext dbContext,
    ILogger<NpcStatBlockService> logger) : INpcStatBlockService
{
    private readonly ApplicationDbContext _dbContext = dbContext;
    private readonly ILogger<NpcStatBlockService> _logger = logger;

    /// <inheritdoc />
    public async Task<List<NpcStatBlock>> GetPrebuiltBlocksAsync()
    {
        return await _dbContext.NpcStatBlocks
            .Where(s => s.IsPrebuilt)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<NpcStatBlock>> GetCampaignBlocksAsync(int campaignId)
    {
        return await _dbContext.NpcStatBlocks
            .Where(s => !s.IsPrebuilt && s.CampaignId == campaignId)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<NpcStatBlock>> GetAvailableBlocksAsync(int campaignId)
    {
        return await _dbContext.NpcStatBlocks
            .Where(s => s.IsPrebuilt || s.CampaignId == campaignId)
            .AsNoTracking()
            .OrderBy(s => !s.IsPrebuilt)
            .ThenBy(s => s.Name)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<NpcStatBlock?> GetBlockAsync(int statBlockId)
    {
        return await _dbContext.NpcStatBlocks
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == statBlockId);
    }

    /// <inheritdoc />
    public async Task<NpcStatBlock> CreateBlockAsync(
        int campaignId,
        string name,
        string concept,
        int size,
        int health,
        int willpower,
        int bludgeoningArmor,
        int lethalArmor,
        string attributesJson,
        string skillsJson,
        string disciplinesJson,
        string notes,
        string stUserId)
    {
        await RequireStAsync(campaignId, stUserId);

        NpcStatBlock block = new()
        {
            CampaignId = campaignId,
            Name = name,
            Concept = concept,
            Size = size,
            Health = health,
            Willpower = willpower,
            BludgeoningArmor = bludgeoningArmor,
            LethalArmor = lethalArmor,
            AttributesJson = attributesJson,
            SkillsJson = skillsJson,
            DisciplinesJson = disciplinesJson,
            Notes = notes,
            IsPrebuilt = false,
        };

        _dbContext.NpcStatBlocks.Add(block);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Custom stat block '{Name}' (Id={BlockId}) created in campaign {CampaignId} by ST {UserId}",
            block.Name,
            block.Id,
            campaignId,
            stUserId);

        return block;
    }

    /// <inheritdoc />
    public async Task UpdateBlockAsync(
        int statBlockId,
        string name,
        string concept,
        int size,
        int health,
        int willpower,
        int bludgeoningArmor,
        int lethalArmor,
        string attributesJson,
        string skillsJson,
        string disciplinesJson,
        string notes,
        string stUserId)
    {
        NpcStatBlock block = await _dbContext.NpcStatBlocks.FindAsync(statBlockId)
            ?? throw new InvalidOperationException($"Stat block {statBlockId} not found.");

        if (block.IsPrebuilt)
        {
            throw new UnauthorizedAccessException("Pre-built stat blocks cannot be modified.");
        }

        await RequireStAsync(block.CampaignId!.Value, stUserId);

        block.Name = name;
        block.Concept = concept;
        block.Size = size;
        block.Health = health;
        block.Willpower = willpower;
        block.BludgeoningArmor = bludgeoningArmor;
        block.LethalArmor = lethalArmor;
        block.AttributesJson = attributesJson;
        block.SkillsJson = skillsJson;
        block.DisciplinesJson = disciplinesJson;
        block.Notes = notes;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Stat block {BlockId} updated by ST {UserId}",
            statBlockId,
            stUserId);
    }

    /// <inheritdoc />
    public async Task DeleteBlockAsync(int statBlockId, string stUserId)
    {
        NpcStatBlock block = await _dbContext.NpcStatBlocks.FindAsync(statBlockId)
            ?? throw new InvalidOperationException($"Stat block {statBlockId} not found.");

        if (block.IsPrebuilt)
        {
            throw new UnauthorizedAccessException("Pre-built stat blocks cannot be deleted.");
        }

        await RequireStAsync(block.CampaignId!.Value, stUserId);

        _dbContext.NpcStatBlocks.Remove(block);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Stat block {BlockId} deleted by ST {UserId}",
            statBlockId,
            stUserId);
    }

    private async Task RequireStAsync(int campaignId, string stUserId)
    {
        Campaign? campaign = await _dbContext.Campaigns.FindAsync(campaignId)
            ?? throw new InvalidOperationException($"Campaign {campaignId} not found.");

        if (campaign.StoryTellerId != stUserId)
        {
            _logger.LogWarning(
                "Unauthorized stat block mutation on campaign {CampaignId} by user {UserId}",
                campaignId,
                stUserId);
            throw new UnauthorizedAccessException("Only the Storyteller may manage stat blocks.");
        }
    }
}
