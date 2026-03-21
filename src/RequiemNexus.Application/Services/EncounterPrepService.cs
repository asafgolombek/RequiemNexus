using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Application.DTOs;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Data.Models.Enums;
using RequiemNexus.Domain.Contracts;

namespace RequiemNexus.Application.Services;

/// <summary>
/// Implements <see cref="IEncounterPrepService"/>: manages draft encounters and NPC templates.
/// </summary>
public class EncounterPrepService(
    ApplicationDbContext dbContext,
    ILogger<EncounterPrepService> logger,
    IAuthorizationHelper authHelper,
    ICharacterCreationRules creationRules) : IEncounterPrepService
{
    private readonly ApplicationDbContext _dbContext = dbContext;
    private readonly ILogger<EncounterPrepService> _logger = logger;
    private readonly IAuthorizationHelper _authHelper = authHelper;
    private readonly ICharacterCreationRules _creationRules = creationRules;

    /// <inheritdoc />
    public async Task<CombatEncounter> CreateDraftEncounterAsync(int campaignId, string name, string storyTellerUserId)
    {
        await _authHelper.RequireStorytellerAsync(campaignId, storyTellerUserId, "manage encounters");

        CombatEncounter encounter = new()
        {
            CampaignId = campaignId,
            Name = name,
            IsDraft = true,
            IsActive = false,
            IsPaused = false,
            CurrentRound = 1,
            CreatedAt = DateTime.UtcNow,
        };

        _dbContext.CombatEncounters.Add(encounter);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Draft encounter {EncounterName} (Id={EncounterId}) created in campaign {CampaignId} by ST {UserId}",
            name,
            encounter.Id,
            campaignId,
            storyTellerUserId);

        return encounter;
    }

    /// <inheritdoc />
    public async Task UpdateDraftEncounterNameAsync(int encounterId, string name, string storyTellerUserId)
    {
        CombatEncounter encounter = await _dbContext.CombatEncounters
            .FirstOrDefaultAsync(e => e.Id == encounterId)
            ?? throw new InvalidOperationException($"Encounter {encounterId} not found.");

        if (!encounter.IsDraft)
        {
            throw new InvalidOperationException("Only draft encounters can be renamed.");
        }

        if (encounter.ResolvedAt != null)
        {
            throw new InvalidOperationException($"Encounter {encounterId} is already resolved.");
        }

        await _authHelper.RequireStorytellerAsync(encounter.CampaignId, storyTellerUserId, "manage encounters");

        string trimmed = (name ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            throw new ArgumentException("Encounter name is required.", nameof(name));
        }

        if (trimmed.Length > 200)
        {
            throw new ArgumentException("Encounter name must be at most 200 characters.", nameof(name));
        }

        encounter.Name = trimmed;
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Draft encounter {EncounterId} renamed to '{Name}' by ST {UserId}",
            encounterId,
            trimmed,
            storyTellerUserId);
    }

    /// <inheritdoc />
    public async Task<ChronicleNpcEncounterPrepDto?> GetChronicleNpcEncounterPrepAsync(
        int chronicleNpcId,
        string storyTellerUserId)
    {
        ChronicleNpc? npc = await _dbContext.ChronicleNpcs
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.Id == chronicleNpcId);

        if (npc == null)
        {
            return null;
        }

        await _authHelper.RequireStorytellerAsync(npc.CampaignId, storyTellerUserId, "manage encounters");

        (int wits, int composure) = SocialManeuveringAttributeParser.ReadWitsComposure(npc.AttributesJson);
        int suggestedMod = wits + composure;
        int suggestedHealth = 7;
        int suggestedWill = 4;
        string? linkedStatBlockName = null;

        (int resolve, int composureAttr) = SocialManeuveringAttributeParser.ReadResolveComposure(npc.AttributesJson);
        suggestedWill = Math.Max(1, resolve + composureAttr);

        if (npc.LinkedStatBlockId is int blockId)
        {
            NpcStatBlock? block = await _dbContext.NpcStatBlocks
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == blockId);
            if (block != null)
            {
                suggestedHealth = Math.Max(1, block.Health);
                suggestedWill = Math.Max(1, block.Willpower);
                linkedStatBlockName = block.Name;
            }
        }

        bool tracksVitae = npc.CreatureType == CreatureType.Vampire || npc.IsVampire;
        int suggestedMaxVitae = 0;
        if (tracksVitae)
        {
            int bp = SocialManeuveringAttributeParser.ReadBloodPotency(npc.AttributesJson);
            (_, int maxV, _) = _creationRules.CalculateInitialBloodPotencyAndVitae(bp);
            suggestedMaxVitae = maxV;
        }

        return new ChronicleNpcEncounterPrepDto(
            npc.Name,
            suggestedMod,
            suggestedHealth,
            linkedStatBlockName,
            suggestedWill,
            tracksVitae,
            suggestedMaxVitae);
    }

    /// <inheritdoc />
    public async Task<EncounterNpcTemplate> AddNpcTemplateFromChronicleNpcAsync(
        int encounterId,
        int chronicleNpcId,
        int initiativeMod,
        int healthBoxes,
        int maxWillpower,
        int maxVitae,
        bool isRevealed,
        string? defaultMaskedName,
        string storyTellerUserId)
    {
        ChronicleNpc? npc = await _dbContext.ChronicleNpcs
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.Id == chronicleNpcId)
            ?? throw new InvalidOperationException($"Chronicle NPC {chronicleNpcId} not found.");

        CombatEncounter encounter = await LoadDraftEncounterAsync(encounterId);

        if (encounter.CampaignId != npc.CampaignId)
        {
            throw new InvalidOperationException("Chronicle NPC does not belong to this encounter's campaign.");
        }

        await _authHelper.RequireStorytellerAsync(encounter.CampaignId, storyTellerUserId, "manage encounters");

        bool templateDup = await _dbContext.EncounterNpcTemplates
            .AnyAsync(t => t.EncounterId == encounterId && t.ChronicleNpcId == chronicleNpcId);
        if (templateDup)
        {
            throw new InvalidOperationException(
                "This Danse Macabre NPC is already listed in this encounter prep.");
        }

        int boxes = ClampNpcHealthBoxes(healthBoxes);
        int will = ClampNpcMaxWillpower(maxWillpower);
        int vitaeCap = ResolveChronicleNpcMaxVitae(npc, maxVitae);

        return await AddNpcTemplateAsync(
            encounterId,
            npc.Name,
            initiativeMod,
            boxes,
            will,
            notes: null,
            isRevealed,
            defaultMaskedName,
            storyTellerUserId,
            chronicleNpcId,
            vitaeCap);
    }

    /// <inheritdoc />
    public async Task<EncounterNpcTemplate> AddNpcTemplateAsync(
        int encounterId,
        string name,
        int initiativeMod,
        int healthBoxes,
        int maxWillpower,
        string? notes,
        bool isRevealed,
        string? defaultMaskedName,
        string storyTellerUserId,
        int? chronicleNpcId = null,
        int maxVitae = 0)
    {
        CombatEncounter encounter = await LoadDraftEncounterAsync(encounterId);
        await _authHelper.RequireStorytellerAsync(encounter.CampaignId, storyTellerUserId, "manage encounters");

        string npcName = (name ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(npcName))
        {
            throw new ArgumentException("NPC name is required.", nameof(name));
        }

        EncounterNpcTemplate template = new()
        {
            EncounterId = encounterId,
            ChronicleNpcId = chronicleNpcId,
            Name = npcName,
            InitiativeMod = initiativeMod,
            HealthBoxes = ClampNpcHealthBoxes(healthBoxes),
            MaxWillpower = ClampNpcMaxWillpower(maxWillpower),
            MaxVitae = ClampNpcMaxVitae(maxVitae),
            Notes = notes,
            IsRevealed = isRevealed,
            MaskedDisplayName = string.IsNullOrWhiteSpace(defaultMaskedName) ? null : defaultMaskedName.Trim(),
        };

        _dbContext.EncounterNpcTemplates.Add(template);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "NPC Template '{NpcName}' added to draft encounter {EncounterId} by ST {UserId}",
            npcName,
            encounterId,
            storyTellerUserId);

        return template;
    }

    /// <inheritdoc />
    public async Task RemoveNpcTemplateAsync(int templateId, string storyTellerUserId)
    {
        EncounterNpcTemplate template = await _dbContext.EncounterNpcTemplates
            .Include(t => t.Encounter)
            .FirstOrDefaultAsync(t => t.Id == templateId)
            ?? throw new InvalidOperationException($"Encounter NPC template {templateId} not found.");

        if (template.Encounter == null)
        {
            throw new InvalidOperationException("Template is not associated with an encounter.");
        }

        if (!template.Encounter.IsDraft)
        {
            throw new InvalidOperationException("Templates can only be removed from draft encounters.");
        }

        await _authHelper.RequireStorytellerAsync(template.Encounter.CampaignId, storyTellerUserId, "manage encounters");

        _dbContext.EncounterNpcTemplates.Remove(template);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "NPC Template {TemplateId} removed from encounter {EncounterId} by ST {UserId}",
            templateId,
            template.EncounterId,
            storyTellerUserId);
    }

    // ── Private helpers ───────────────────────────────────────────────────────
    private static int ClampNpcHealthBoxes(int healthBoxes) => Math.Clamp(healthBoxes, 1, 50);

    private static int ClampNpcMaxWillpower(int maxWillpower) => Math.Clamp(maxWillpower, 1, 20);

    private static int ClampNpcMaxVitae(int maxVitae) => Math.Clamp(maxVitae, 0, 100);

    private static int ResolveChronicleNpcMaxVitae(ChronicleNpc npc, int maxVitae)
    {
        bool kindred = npc.CreatureType == CreatureType.Vampire || npc.IsVampire;
        if (!kindred)
        {
            return 0;
        }

        return ClampNpcMaxVitae(maxVitae);
    }

    private async Task<CombatEncounter> LoadDraftEncounterAsync(int encounterId)
    {
        CombatEncounter encounter = await _dbContext.CombatEncounters
            .FirstOrDefaultAsync(e => e.Id == encounterId)
            ?? throw new InvalidOperationException($"Encounter {encounterId} not found.");

        if (!encounter.IsDraft)
        {
            throw new InvalidOperationException($"Encounter {encounterId} is not a draft.");
        }

        if (encounter.ResolvedAt != null)
        {
            throw new InvalidOperationException($"Encounter {encounterId} is already resolved.");
        }

        return encounter;
    }
}
