using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RequiemNexus.Application.Services;
using RequiemNexus.Data.Models;
using RequiemNexus.Domain;
using RequiemNexus.Domain.Enums;
using Xunit;

namespace RequiemNexus.Data.Tests;

/// <summary>
/// Integration tests for <see cref="BeatLedgerService"/> and the ledger-writing paths of
/// <see cref="CharacterManagementService"/>. Each test gets its own InMemory database.
/// </summary>
public class BeatLedgerServiceTests
{
    private static ApplicationDbContext CreateContext(string dbName)
    {
        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new ApplicationDbContext(options);
    }

    private static BeatLedgerService CreateLedger(ApplicationDbContext ctx) => new(ctx);

    private static CharacterManagementService CreateCharacterService(ApplicationDbContext ctx)
    {
        // These tests never call GetCharactersByUserIdAsync / GetArchivedCharactersAsync,
        // so the factory only needs to satisfy the constructor.
        ServiceCollection services = new();
        services.AddDbContextFactory<ApplicationDbContext>(
            o => o.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        IDbContextFactory<ApplicationDbContext> factory = services.BuildServiceProvider()
            .GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
        return new(ctx, factory, new CharacterCreationRules(), CreateLedger(ctx));
    }

    // -----------------------------------------------------------------------
    // BeatLedgerService — direct API
    // -----------------------------------------------------------------------

    [Fact]
    public async Task RecordBeatAsync_PersistsEntry()
    {
        using ApplicationDbContext ctx = CreateContext(nameof(RecordBeatAsync_PersistsEntry));
        Character character = new() { Name = "V", ApplicationUserId = "u1" };
        ctx.Characters.Add(character);
        await ctx.SaveChangesAsync();

        BeatLedgerService ledger = CreateLedger(ctx);
        await ledger.RecordBeatAsync(character.Id, null, BeatSource.StorytellerAward, "Good RP", "st-1");

        List<BeatLedgerEntry> entries = await ledger.GetBeatLedgerAsync(character.Id);
        Assert.Single(entries);
        Assert.Equal(BeatSource.StorytellerAward, entries[0].Source);
        Assert.Equal("Good RP", entries[0].Reason);
        Assert.Equal("st-1", entries[0].AwardedByUserId);
    }

    [Fact]
    public async Task RecordXpCreditAsync_PersistsPositiveDelta()
    {
        using ApplicationDbContext ctx = CreateContext(nameof(RecordXpCreditAsync_PersistsPositiveDelta));
        Character character = new() { Name = "V", ApplicationUserId = "u1" };
        ctx.Characters.Add(character);
        await ctx.SaveChangesAsync();

        BeatLedgerService ledger = CreateLedger(ctx);
        await ledger.RecordXpCreditAsync(character.Id, null, 1, XpSource.BeatConversion, "5 Beats → 1 XP", null);

        List<XpLedgerEntry> entries = await ledger.GetXpLedgerAsync(character.Id);
        Assert.Single(entries);
        Assert.Equal(1, entries[0].Delta);
        Assert.Equal(XpSource.BeatConversion, entries[0].Source);
        Assert.Null(entries[0].Expense);
    }

    [Fact]
    public async Task RecordXpSpendAsync_PersistsNegativeDelta()
    {
        using ApplicationDbContext ctx = CreateContext(nameof(RecordXpSpendAsync_PersistsNegativeDelta));
        Character character = new() { Name = "V", ApplicationUserId = "u1" };
        ctx.Characters.Add(character);
        await ctx.SaveChangesAsync();

        BeatLedgerService ledger = CreateLedger(ctx);
        await ledger.RecordXpSpendAsync(character.Id, null, 3, XpExpense.Attribute, "Upgraded Strength to 3", "u1");

        List<XpLedgerEntry> entries = await ledger.GetXpLedgerAsync(character.Id);
        Assert.Single(entries);
        Assert.Equal(-3, entries[0].Delta);
        Assert.Equal(XpExpense.Attribute, entries[0].Expense);
        Assert.Null(entries[0].Source);
    }

    [Fact]
    public async Task GetBeatLedgerAsync_ReturnsNewestFirst()
    {
        using ApplicationDbContext ctx = CreateContext(nameof(GetBeatLedgerAsync_ReturnsNewestFirst));
        Character character = new() { Name = "V", ApplicationUserId = "u1" };
        ctx.Characters.Add(character);
        await ctx.SaveChangesAsync();

        BeatLedgerService ledger = CreateLedger(ctx);
        ctx.BeatLedger.AddRange(
            new BeatLedgerEntry { CharacterId = character.Id, Source = BeatSource.SessionAward, Reason = "A", OccurredAt = DateTime.UtcNow.AddMinutes(-10) },
            new BeatLedgerEntry { CharacterId = character.Id, Source = BeatSource.StorytellerAward, Reason = "B", OccurredAt = DateTime.UtcNow });
        await ctx.SaveChangesAsync();

        List<BeatLedgerEntry> entries = await ledger.GetBeatLedgerAsync(character.Id);
        Assert.Equal("B", entries[0].Reason);
        Assert.Equal("A", entries[1].Reason);
    }

    // -----------------------------------------------------------------------
    // CharacterManagementService — ledger side-effects
    // -----------------------------------------------------------------------

    [Fact]
    public async Task AddBeatAsync_WritesLedgerEntry()
    {
        using ApplicationDbContext ctx = CreateContext(nameof(AddBeatAsync_WritesLedgerEntry));
        Character character = new() { Name = "V", ApplicationUserId = "u1" };
        ctx.Characters.Add(character);
        await ctx.SaveChangesAsync();

        CharacterManagementService svc = CreateCharacterService(ctx);
        await svc.AddBeatAsync(character);

        List<BeatLedgerEntry> beats = await ctx.BeatLedger.Where(b => b.CharacterId == character.Id).ToListAsync();
        Assert.Single(beats);
        Assert.Equal(BeatSource.ManualAdjustment, beats[0].Source);
    }

    [Fact]
    public async Task AddBeatAsync_WhenConversionTriggered_WritesXpCreditEntry()
    {
        using ApplicationDbContext ctx = CreateContext(nameof(AddBeatAsync_WhenConversionTriggered_WritesXpCreditEntry));
        // Start at 4 Beats so the next +1 triggers conversion
        Character character = new() { Name = "V", ApplicationUserId = "u1", Beats = 4 };
        ctx.Characters.Add(character);
        await ctx.SaveChangesAsync();

        CharacterManagementService svc = CreateCharacterService(ctx);
        await svc.AddBeatAsync(character);

        Assert.Equal(0, character.Beats);
        Assert.Equal(1, character.ExperiencePoints);

        List<XpLedgerEntry> xp = await ctx.XpLedger.Where(x => x.CharacterId == character.Id).ToListAsync();
        Assert.Single(xp);
        Assert.Equal(1, xp[0].Delta);
        Assert.Equal(XpSource.BeatConversion, xp[0].Source);
    }

    [Fact]
    public async Task AddXPAsync_WritesXpCreditLedgerEntry()
    {
        using ApplicationDbContext ctx = CreateContext(nameof(AddXPAsync_WritesXpCreditLedgerEntry));
        Character character = new() { Name = "V", ApplicationUserId = "u1" };
        ctx.Characters.Add(character);
        await ctx.SaveChangesAsync();

        CharacterManagementService svc = CreateCharacterService(ctx);
        await svc.AddXPAsync(character);

        List<XpLedgerEntry> xp = await ctx.XpLedger.Where(x => x.CharacterId == character.Id).ToListAsync();
        Assert.Single(xp);
        Assert.Equal(1, xp[0].Delta);
        Assert.Equal(XpSource.ManualAdjustment, xp[0].Source);
    }

    [Fact]
    public async Task RemoveXPAsync_WritesXpDebitLedgerEntry()
    {
        using ApplicationDbContext ctx = CreateContext(nameof(RemoveXPAsync_WritesXpDebitLedgerEntry));
        Character character = new() { Name = "V", ApplicationUserId = "u1", ExperiencePoints = 3 };
        ctx.Characters.Add(character);
        await ctx.SaveChangesAsync();

        CharacterManagementService svc = CreateCharacterService(ctx);
        await svc.RemoveXPAsync(character);

        List<XpLedgerEntry> xp = await ctx.XpLedger.Where(x => x.CharacterId == character.Id).ToListAsync();
        Assert.Single(xp);
        Assert.Equal(-1, xp[0].Delta);
        Assert.Equal(XpExpense.ManualAdjustment, xp[0].Expense);
    }
}
