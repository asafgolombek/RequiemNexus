using Microsoft.EntityFrameworkCore;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Domain.Enums;

namespace RequiemNexus.Application.Services;

/// <summary>
/// Writes and reads the immutable Beat and XP audit ledgers.
/// No existing ledger row is ever modified after creation.
/// </summary>
public class BeatLedgerService(ApplicationDbContext dbContext) : IBeatLedgerService
{
    private readonly ApplicationDbContext _dbContext = dbContext;

    /// <inheritdoc />
    public async Task RecordBeatAsync(
        int characterId,
        int? campaignId,
        BeatSource source,
        string reason,
        string? awardedByUserId)
    {
        _dbContext.BeatLedger.Add(new BeatLedgerEntry
        {
            CharacterId = characterId,
            CampaignId = campaignId,
            Source = source,
            Reason = reason,
            OccurredAt = DateTime.UtcNow,
            AwardedByUserId = awardedByUserId,
        });

        await _dbContext.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task RecordXpCreditAsync(
        int characterId,
        int? campaignId,
        int amount,
        XpSource source,
        string reason,
        string? actingUserId)
    {
        _dbContext.XpLedger.Add(new XpLedgerEntry
        {
            CharacterId = characterId,
            CampaignId = campaignId,
            Delta = amount,
            Source = source,
            Reason = reason,
            OccurredAt = DateTime.UtcNow,
            ActingUserId = actingUserId,
        });

        await _dbContext.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task RecordXpSpendAsync(
        int characterId,
        int? campaignId,
        int cost,
        XpExpense expense,
        string reason,
        string? actingUserId,
        string? notes = null)
    {
        _dbContext.XpLedger.Add(new XpLedgerEntry
        {
            CharacterId = characterId,
            CampaignId = campaignId,
            Delta = -cost,
            Expense = expense,
            Reason = reason,
            Notes = notes,
            OccurredAt = DateTime.UtcNow,
            ActingUserId = actingUserId,
        });

        await _dbContext.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<List<BeatLedgerEntry>> GetBeatLedgerAsync(int characterId)
    {
        return await _dbContext.BeatLedger
            .Where(b => b.CharacterId == characterId)
            .OrderByDescending(b => b.OccurredAt)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<XpLedgerEntry>> GetXpLedgerAsync(int characterId)
    {
        return await _dbContext.XpLedger
            .Where(x => x.CharacterId == characterId)
            .OrderByDescending(x => x.OccurredAt)
            .AsNoTracking()
            .ToListAsync();
    }
}
