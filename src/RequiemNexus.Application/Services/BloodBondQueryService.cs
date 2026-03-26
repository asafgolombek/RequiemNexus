using Microsoft.EntityFrameworkCore;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Application.DTOs;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Domain;
using RequiemNexus.Domain.Contracts;

namespace RequiemNexus.Application.Services;

/// <summary>
/// Read-only queries for Blood Bonds: thrall view, chronicle view, and fading alerts.
/// Mutations are handled by <see cref="BloodBondService"/>.
/// </summary>
public class BloodBondQueryService(
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    IAuthorizationHelper authHelper,
    IConditionRules conditionRules) : IBloodBondQueryService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory = dbContextFactory;
    private readonly IAuthorizationHelper _authHelper = authHelper;
    private readonly IConditionRules _conditionRules = conditionRules;

    /// <inheritdoc />
    public async Task<IReadOnlyList<BloodBondDto>> GetBondsForThrallAsync(int characterId, string userId)
    {
        await _authHelper.RequireCharacterAccessAsync(characterId, userId, "view Blood Bonds");
        await using ApplicationDbContext db = await _dbContextFactory.CreateDbContextAsync();
        DateTime now = DateTime.UtcNow;
        List<int> ids = await db.BloodBonds.AsNoTracking()
            .Where(b => b.ThrallCharacterId == characterId)
            .Select(b => b.Id)
            .ToListAsync();

        List<BloodBondDto> list = [];
        foreach (int id in ids)
        {
            list.Add(await MapToDtoAsync(db, id, now));
        }

        return list;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<BloodBondDto>> GetBondsInChronicleAsync(int chronicleId, string userId)
    {
        await _authHelper.RequireStorytellerAsync(chronicleId, userId, "list Blood Bonds");
        await using ApplicationDbContext db = await _dbContextFactory.CreateDbContextAsync();
        DateTime now = DateTime.UtcNow;
        List<int> ids = await db.BloodBonds.AsNoTracking()
            .Where(b => b.ChronicleId == chronicleId)
            .OrderBy(b => b.ThrallCharacterId)
            .ThenBy(b => b.RegnantKey)
            .Select(b => b.Id)
            .ToListAsync();

        List<BloodBondDto> list = [];
        foreach (int id in ids)
        {
            list.Add(await MapToDtoAsync(db, id, now));
        }

        return list;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<BloodBondDto>> GetFadingAlertsAsync(int chronicleId, string userId)
    {
        await _authHelper.RequireStorytellerAsync(chronicleId, userId, "view Blood Bond fading alerts");
        await using ApplicationDbContext db = await _dbContextFactory.CreateDbContextAsync();
        DateTime now = DateTime.UtcNow;
        List<BloodBondDto> all = [];
        List<int> ids = await db.BloodBonds.AsNoTracking()
            .Where(b => b.ChronicleId == chronicleId)
            .Select(b => b.Id)
            .ToListAsync();

        foreach (int id in ids)
        {
            BloodBondDto dto = await MapToDtoAsync(db, id, now);
            if (dto.IsFading)
            {
                all.Add(dto);
            }
        }

        return all;
    }

    private static string ResolveRegnantLabel(BloodBond bond) =>
        bond.RegnantCharacter?.Name
        ?? bond.RegnantNpc?.Name
        ?? bond.RegnantDisplayName
        ?? string.Empty;

    private async Task<BloodBondDto> MapToDtoAsync(ApplicationDbContext db, int bondId, DateTime now)
    {
        BloodBond bond = await db.BloodBonds
            .AsNoTracking()
            .Include(b => b.ThrallCharacter)
            .Include(b => b.RegnantCharacter)
            .Include(b => b.RegnantNpc)
            .FirstAsync(b => b.Id == bondId);

        string regnantLabel = ResolveRegnantLabel(bond);
        string activeName = _conditionRules.GetConditionDescription(BloodBondRules.ConditionForStage(bond.Stage));

        return new BloodBondDto(
            bond.Id,
            bond.ChronicleId,
            bond.ThrallCharacterId,
            bond.ThrallCharacter?.Name ?? "?",
            bond.RegnantCharacterId,
            bond.RegnantNpcId,
            regnantLabel,
            bond.Stage,
            bond.LastFedAt,
            BloodBondRules.IsFading(bond.LastFedAt, now),
            activeName);
    }
}
