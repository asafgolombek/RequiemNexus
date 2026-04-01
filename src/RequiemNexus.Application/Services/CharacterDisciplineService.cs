using Microsoft.EntityFrameworkCore;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Application.DTOs;
using RequiemNexus.Application.Events;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Domain.Contracts;
using RequiemNexus.Domain.Enums;
using RequiemNexus.Domain.Events;
using RequiemNexus.Domain.Models;

namespace RequiemNexus.Application.Services;

/// <summary>
/// Application service for managing character discipline purchases, including XP deduction, acquisition gates, and ledger recording.
/// </summary>
public class CharacterDisciplineService(
    ApplicationDbContext dbContext,
    IBeatLedgerService beatLedger,
    IExperienceCostRules experienceCostRules,
    IAuthorizationHelper authorizationHelper,
    IDomainEventDispatcher domainEventDispatcher,
    IHumanityService humanityService) : ICharacterDisciplineService
{
    private const string _cruacDisciplineName = "Crúac";
    private const string _lanceaCovenantName = "The Lancea et Sanctum";

    private readonly ApplicationDbContext _dbContext = dbContext;
    private readonly IBeatLedgerService _beatLedger = beatLedger;
    private readonly IExperienceCostRules _experienceCostRules = experienceCostRules;
    private readonly IAuthorizationHelper _authorizationHelper = authorizationHelper;
    private readonly IDomainEventDispatcher _domainEventDispatcher = domainEventDispatcher;
    private readonly IHumanityService _humanityService = humanityService;

    /// <inheritdoc />
    public async Task<List<Discipline>> GetAvailableDisciplinesAsync()
    {
        return await _dbContext.Disciplines.OrderBy(d => d.Name).AsNoTracking().ToListAsync();
    }

    /// <inheritdoc />
    public async Task<Result<CharacterDiscipline>> AddDisciplineAsync(DisciplineAcquisitionRequest request, string userId)
    {
        await _authorizationHelper.RequireCharacterAccessAsync(request.CharacterId, userId, "purchase a Discipline");

        Character? character = await LoadCharacterForAcquisitionAsync(request.CharacterId);
        if (character == null)
        {
            return Result<CharacterDiscipline>.Failure($"Character {request.CharacterId} not found.");
        }

        Discipline? discipline = await LoadDisciplineForGatesAsync(request.DisciplineId);
        if (discipline == null)
        {
            return Result<CharacterDiscipline>.Failure($"Discipline {request.DisciplineId} not found.");
        }

        if (character.Disciplines.Any(d => d.DisciplineId == request.DisciplineId))
        {
            return Result<CharacterDiscipline>.Failure("This character already has that Discipline. Use upgrade instead.");
        }

        bool stAcknowledged = request.AcquisitionAcknowledgedByST
            && character.CampaignId.HasValue
            && await _authorizationHelper.IsStorytellerAsync(character.CampaignId.Value, userId);

        var auditSegments = new List<string>();
        string? gateError = await ValidateAcquisitionGatesAsync(character, discipline, request.TargetRating, stAcknowledged, auditSegments);
        if (gateError != null)
        {
            return Result<CharacterDiscipline>.Failure(gateError);
        }

        bool isInClan = character.IsDisciplineInClan(request.DisciplineId);
        int xpCost = _experienceCostRules.CalculateDisciplineUpgradeCost(0, request.TargetRating, isInClan);

        if (character.ExperiencePoints < xpCost)
        {
            return Result<CharacterDiscipline>.Failure(
                $"Insufficient XP. Required: {xpCost}, Available: {character.ExperiencePoints}");
        }

        MaybeDispatchCruacLearnedDegeneration(character, discipline, previousRating: 0, newRating: request.TargetRating);

        character.ExperiencePoints -= xpCost;

        var cd = new CharacterDiscipline
        {
            CharacterId = character.Id,
            DisciplineId = request.DisciplineId,
            Rating = request.TargetRating,
            Discipline = discipline,
        };
        character.Disciplines.Add(cd);
        _dbContext.CharacterDisciplines.Add(cd);

        ClampHumanityForCruacIfNeeded(character);

        string? ledgerNotes = BuildLedgerNotes(auditSegments, userId);
        await _beatLedger.RecordXpSpendAsync(
            character.Id,
            character.CampaignId,
            xpCost,
            XpExpense.Discipline,
            $"Purchased Discipline (Id={request.DisciplineId}, rating={request.TargetRating}, in-clan={isInClan})",
            userId,
            ledgerNotes);

        await _dbContext.SaveChangesAsync();
        return Result<CharacterDiscipline>.Success(cd);
    }

    /// <inheritdoc />
    public async Task<Result<CharacterDiscipline>> TryUpgradeDisciplineAsync(DisciplineAcquisitionRequest request, string userId)
    {
        await _authorizationHelper.RequireCharacterAccessAsync(request.CharacterId, userId, "upgrade a Discipline");

        Character? character = await LoadCharacterForAcquisitionAsync(request.CharacterId);
        if (character == null)
        {
            return Result<CharacterDiscipline>.Failure($"Character {request.CharacterId} not found.");
        }

        CharacterDiscipline? cd = character.Disciplines.FirstOrDefault(d => d.DisciplineId == request.DisciplineId);
        if (cd == null || request.TargetRating <= cd.Rating)
        {
            return Result<CharacterDiscipline>.Failure("No eligible Discipline upgrade for that target rating.");
        }

        Discipline? discipline = await LoadDisciplineForGatesAsync(request.DisciplineId);
        if (discipline == null)
        {
            return Result<CharacterDiscipline>.Failure($"Discipline {request.DisciplineId} not found.");
        }

        cd.Discipline = discipline;

        bool stAcknowledged = request.AcquisitionAcknowledgedByST
            && character.CampaignId.HasValue
            && await _authorizationHelper.IsStorytellerAsync(character.CampaignId.Value, userId);

        var auditSegments = new List<string>();
        string? gateError = await ValidateAcquisitionGatesAsync(character, discipline, request.TargetRating, stAcknowledged, auditSegments);
        if (gateError != null)
        {
            return Result<CharacterDiscipline>.Failure(gateError);
        }

        bool isInClan = character.IsDisciplineInClan(cd.DisciplineId);
        int xpCost = _experienceCostRules.CalculateDisciplineUpgradeCost(cd.Rating, request.TargetRating, isInClan);

        if (character.ExperiencePoints < xpCost)
        {
            return Result<CharacterDiscipline>.Failure(
                $"Insufficient XP. Required: {xpCost}, Available: {character.ExperiencePoints}");
        }

        int previousRating = cd.Rating;
        MaybeDispatchCruacLearnedDegeneration(character, discipline, previousRating, request.TargetRating);

        character.ExperiencePoints -= xpCost;
        cd.Rating = request.TargetRating;

        ClampHumanityForCruacIfNeeded(character);

        string? ledgerNotes = BuildLedgerNotes(auditSegments, userId);
        await _beatLedger.RecordXpSpendAsync(
            character.Id,
            character.CampaignId,
            xpCost,
            XpExpense.Discipline,
            $"Upgraded Discipline {discipline.Name} to {request.TargetRating} (in-clan={isInClan})",
            userId,
            ledgerNotes);

        await _dbContext.SaveChangesAsync();
        return Result<CharacterDiscipline>.Success(cd);
    }

    private static string? BuildLedgerNotes(IReadOnlyList<string> auditSegments, string userId)
    {
        if (auditSegments.Count == 0)
        {
            return null;
        }

        string ts = DateTime.UtcNow.ToString("O", System.Globalization.CultureInfo.InvariantCulture);
        var parts = auditSegments.Select(g => $" | gate-override:{g} stUserId={userId} {ts}");
        return string.Concat(parts);
    }

    private async Task<Character?> LoadCharacterForAcquisitionAsync(int characterId)
    {
        return await _dbContext.Characters
            .Include(c => c.Clan)!.ThenInclude(cl => cl!.ClanDisciplines)
            .Include(c => c.Bloodlines).ThenInclude(b => b.BloodlineDefinition)
            .Include(c => c.Disciplines).ThenInclude(d => d.Discipline)
            .Include(c => c.Covenant)
            .FirstOrDefaultAsync(c => c.Id == characterId);
    }

    private async Task<Discipline?> LoadDisciplineForGatesAsync(int disciplineId)
    {
        return await _dbContext.Disciplines
            .Include(d => d.Covenant)
            .Include(d => d.Bloodline)
            .FirstOrDefaultAsync(d => d.Id == disciplineId);
    }

    private async Task<string?> ValidateAcquisitionGatesAsync(
        Character character,
        Discipline discipline,
        int targetRating,
        bool stAcknowledged,
        List<string> auditSegments)
    {
        string? bloodlineErr = DisciplineAcquisitionGates.TryBloodlineGateAcquisition(character, discipline);
        if (bloodlineErr != null)
        {
            return bloodlineErr;
        }

        string? covenantErr = DisciplineAcquisitionGates.TryCovenantGateAcquisition(character, discipline, stAcknowledged, auditSegments);
        if (covenantErr != null)
        {
            return covenantErr;
        }

        // Gate 3 — Theban Humanity floor (hard)
        if (discipline.IsCovenantDiscipline
            && string.Equals(discipline.Covenant?.Name, _lanceaCovenantName, StringComparison.Ordinal)
            && targetRating > character.Humanity)
        {
            return $"Theban Sorcery •{targetRating} requires Humanity {targetRating} or higher.";
        }

        string? mentorErr = DisciplineAcquisitionGates.TryMentorBloodGateAcquisition(character, discipline, stAcknowledged, auditSegments);
        if (mentorErr != null)
        {
            return mentorErr;
        }

        string? necromancyErr = DisciplineAcquisitionGates.TryNecromancyGateAcquisition(character, discipline, stAcknowledged, auditSegments);
        if (necromancyErr != null)
        {
            return necromancyErr;
        }

        await Task.CompletedTask;
        return null;
    }

    /// <summary>
    /// V:tR 2e: learning a new Crúac dot at Humanity 4+ is a breaking point. Fires on first purchase and on each dot increase.
    /// </summary>
    private void MaybeDispatchCruacLearnedDegeneration(
        Character character,
        Discipline discipline,
        int previousRating,
        int newRating)
    {
        if (!string.Equals(discipline.Name, _cruacDisciplineName, StringComparison.Ordinal))
        {
            return;
        }

        if (newRating <= previousRating || newRating < 1)
        {
            return;
        }

        if (character.Humanity < 4)
        {
            return;
        }

        _domainEventDispatcher.Dispatch(
            new DegenerationCheckRequiredEvent(character.Id, DegenerationReason.CrúacPurchase));
    }

    private void ClampHumanityForCruacIfNeeded(Character character)
    {
        int maxHum = _humanityService.GetEffectiveMaxHumanity(character);
        if (character.Humanity > maxHum)
        {
            character.Humanity = maxHum;
        }
    }
}
