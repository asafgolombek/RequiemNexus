using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Application.Services;

/// <summary>
/// Storyteller-only CRUD for encounter templates.
/// </summary>
public class EncounterTemplateService(
    ApplicationDbContext dbContext,
    ILogger<EncounterTemplateService> logger,
    IAuthorizationHelper authHelper,
    IEncounterService encounterService) : IEncounterTemplateService
{
    private readonly ApplicationDbContext _dbContext = dbContext;
    private readonly ILogger<EncounterTemplateService> _logger = logger;
    private readonly IAuthorizationHelper _authHelper = authHelper;
    private readonly IEncounterService _encounterService = encounterService;

    /// <inheritdoc />
    public async Task<EncounterTemplate> CreateTemplateAsync(int campaignId, string name, string storyTellerUserId)
    {
        await _authHelper.RequireStorytellerAsync(campaignId, storyTellerUserId, "manage encounter templates");

        EncounterTemplate template = new()
        {
            CampaignId = campaignId,
            Name = name,
        };

        _dbContext.Set<EncounterTemplate>().Add(template);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Encounter template {Name} (Id={TemplateId}) created for campaign {CampaignId}",
            name,
            template.Id,
            campaignId);

        return template;
    }

    /// <inheritdoc />
    public async Task AddNpcToTemplateAsync(
        int templateId,
        string name,
        int initiativeMod,
        int healthBoxes,
        int maxWillpower,
        bool isRevealedByDefault,
        string? defaultMaskedName,
        string storyTellerUserId)
    {
        EncounterTemplate template = await LoadTemplateAsync(templateId);
        await _authHelper.RequireStorytellerAsync(template.CampaignId, storyTellerUserId, "manage encounter templates");

        int will = Math.Clamp(maxWillpower, 1, 20);
        EncounterTemplateNpc npc = new()
        {
            TemplateId = templateId,
            Name = name,
            InitiativeMod = initiativeMod,
            HealthBoxes = healthBoxes,
            MaxWillpower = will,
            IsRevealedByDefault = isRevealedByDefault,
            DefaultMaskedName = defaultMaskedName,
        };

        _dbContext.Set<EncounterTemplateNpc>().Add(npc);
        await _dbContext.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<List<EncounterTemplate>> GetTemplatesAsync(int campaignId, string storyTellerUserId)
    {
        await _authHelper.RequireStorytellerAsync(campaignId, storyTellerUserId, "manage encounter templates");

        return await _dbContext.Set<EncounterTemplate>()
            .AsNoTracking()
            .Include(t => t.Npcs)
            .Where(t => t.CampaignId == campaignId)
            .OrderBy(t => t.Name)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<CombatEncounter> CreateDraftEncounterFromTemplateAsync(
        int templateId,
        string encounterName,
        string storyTellerUserId)
    {
        EncounterTemplate template = await _dbContext.Set<EncounterTemplate>()
            .Include(t => t.Npcs)
            .FirstOrDefaultAsync(t => t.Id == templateId)
            ?? throw new InvalidOperationException($"Template {templateId} not found.");

        await _authHelper.RequireStorytellerAsync(template.CampaignId, storyTellerUserId, "manage encounter templates");

        CombatEncounter draft = await _encounterService.CreateDraftEncounterAsync(
            template.CampaignId,
            encounterName,
            storyTellerUserId);

        foreach (EncounterTemplateNpc npc in template.Npcs)
        {
            await _encounterService.AddNpcTemplateAsync(
                draft.Id,
                npc.Name,
                npc.InitiativeMod,
                npc.HealthBoxes,
                npc.MaxWillpower < 1 ? 4 : npc.MaxWillpower,
                notes: null,
                isRevealed: npc.IsRevealedByDefault,
                defaultMaskedName: npc.DefaultMaskedName,
                storyTellerUserId);
        }

        _logger.LogInformation(
            "Draft encounter {EncounterId} created from template {TemplateId}",
            draft.Id,
            templateId);

        return draft;
    }

    /// <inheritdoc />
    public async Task DeleteTemplateAsync(int templateId, string storyTellerUserId)
    {
        EncounterTemplate template = await LoadTemplateAsync(templateId);
        await _authHelper.RequireStorytellerAsync(template.CampaignId, storyTellerUserId, "manage encounter templates");

        _dbContext.Set<EncounterTemplate>().Remove(template);
        await _dbContext.SaveChangesAsync();
    }

    private async Task<EncounterTemplate> LoadTemplateAsync(int templateId) =>
        await _dbContext.Set<EncounterTemplate>()
            .FirstOrDefaultAsync(t => t.Id == templateId)
            ?? throw new InvalidOperationException($"Template {templateId} not found.");
}
