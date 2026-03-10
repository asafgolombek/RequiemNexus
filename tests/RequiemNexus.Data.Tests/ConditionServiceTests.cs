using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using RequiemNexus.Application.Services;
using RequiemNexus.Data.Models;
using RequiemNexus.Domain;
using RequiemNexus.Domain.Enums;
using Xunit;

namespace RequiemNexus.Data.Tests;

/// <summary>
/// Integration tests for <see cref="ConditionService"/> wired to an EF Core InMemory database.
/// </summary>
public class ConditionServiceTests
{
    private static ApplicationDbContext CreateContext(string dbName)
    {
        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new ApplicationDbContext(options);
    }

    private static ConditionService CreateService(ApplicationDbContext ctx)
        => new(ctx, new ConditionRules(), new BeatLedgerService(ctx), NullLogger<ConditionService>.Instance);

    // ── ApplyConditionAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task ApplyCondition_ByOwner_Succeeds()
    {
        using ApplicationDbContext ctx = CreateContext(nameof(ApplyCondition_ByOwner_Succeeds));
        Character character = new() { Name = "V", ApplicationUserId = "player-1" };
        ctx.Characters.Add(character);
        await ctx.SaveChangesAsync();

        ConditionService svc = CreateService(ctx);
        CharacterCondition condition = await svc.ApplyConditionAsync(
            character.Id, ConditionType.Guilty, null, null, "player-1");

        Assert.False(condition.IsResolved);
        Assert.True(condition.AwardsBeat);
        Assert.Equal(ConditionType.Guilty, condition.ConditionType);
    }

    [Fact]
    public async Task ApplyCondition_ByStoryteller_Succeeds()
    {
        using ApplicationDbContext ctx = CreateContext(nameof(ApplyCondition_ByStoryteller_Succeeds));
        Campaign campaign = new() { Name = "C", StoryTellerId = "st-1" };
        ctx.Campaigns.Add(campaign);
        await ctx.SaveChangesAsync();

        Character character = new() { Name = "V", ApplicationUserId = "player-1", CampaignId = campaign.Id };
        ctx.Characters.Add(character);
        await ctx.SaveChangesAsync();

        ConditionService svc = CreateService(ctx);
        CharacterCondition condition = await svc.ApplyConditionAsync(
            character.Id, ConditionType.Shaken, null, null, "st-1");

        Assert.Equal(ConditionType.Shaken, condition.ConditionType);
    }

    [Fact]
    public async Task ApplyCondition_ByOutsider_ThrowsUnauthorized()
    {
        using ApplicationDbContext ctx = CreateContext(nameof(ApplyCondition_ByOutsider_ThrowsUnauthorized));
        Character character = new() { Name = "V", ApplicationUserId = "player-1" };
        ctx.Characters.Add(character);
        await ctx.SaveChangesAsync();

        ConditionService svc = CreateService(ctx);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => svc.ApplyConditionAsync(character.Id, ConditionType.Guilty, null, null, "outsider"));
    }

    [Fact]
    public async Task ApplyCustomCondition_SetsAwardsBeatFalse()
    {
        using ApplicationDbContext ctx = CreateContext(nameof(ApplyCustomCondition_SetsAwardsBeatFalse));
        Character character = new() { Name = "V", ApplicationUserId = "player-1" };
        ctx.Characters.Add(character);
        await ctx.SaveChangesAsync();

        ConditionService svc = CreateService(ctx);
        CharacterCondition condition = await svc.ApplyConditionAsync(
            character.Id, ConditionType.Custom, "Haunted", null, "player-1");

        Assert.False(condition.AwardsBeat);
        Assert.Equal("Haunted", condition.CustomName);
    }

    // ── ResolveConditionAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task ResolveCondition_AwardsBeat_WhenFlagIsTrue()
    {
        using ApplicationDbContext ctx = CreateContext(nameof(ResolveCondition_AwardsBeat_WhenFlagIsTrue));
        Character character = new() { Name = "V", ApplicationUserId = "player-1" };
        ctx.Characters.Add(character);
        await ctx.SaveChangesAsync();

        ConditionService svc = CreateService(ctx);
        CharacterCondition condition = await svc.ApplyConditionAsync(
            character.Id, ConditionType.Guilty, null, null, "player-1");

        await svc.ResolveConditionAsync(condition.Id, "player-1");

        // Condition should be resolved
        CharacterCondition? resolved = await ctx.CharacterConditions.FindAsync(condition.Id);
        Assert.NotNull(resolved);
        Assert.True(resolved!.IsResolved);
        Assert.NotNull(resolved.ResolvedAt);

        // Beat ledger should have an entry
        List<BeatLedgerEntry> beats = await ctx.BeatLedger
            .Where(b => b.CharacterId == character.Id)
            .ToListAsync();
        Assert.Single(beats);
        Assert.Equal(BeatSource.ConditionResolved, beats[0].Source);

        // Character's Beats should be incremented
        Character? updated = await ctx.Characters.FindAsync(character.Id);
        Assert.Equal(1, updated!.Beats);
    }

    [Fact]
    public async Task ResolveCondition_DoesNotAwardBeat_ForCustomCondition()
    {
        using ApplicationDbContext ctx = CreateContext(nameof(ResolveCondition_DoesNotAwardBeat_ForCustomCondition));
        Character character = new() { Name = "V", ApplicationUserId = "player-1" };
        ctx.Characters.Add(character);
        await ctx.SaveChangesAsync();

        ConditionService svc = CreateService(ctx);
        CharacterCondition condition = await svc.ApplyConditionAsync(
            character.Id, ConditionType.Custom, "Haunted", null, "player-1");

        await svc.ResolveConditionAsync(condition.Id, "player-1");

        int beatCount = await ctx.BeatLedger.CountAsync(b => b.CharacterId == character.Id);
        Assert.Equal(0, beatCount);

        Character? updated = await ctx.Characters.FindAsync(character.Id);
        Assert.Equal(0, updated!.Beats);
    }

    [Fact]
    public async Task ResolveCondition_ThrowsInvalidOperation_WhenAlreadyResolved()
    {
        using ApplicationDbContext ctx = CreateContext(nameof(ResolveCondition_ThrowsInvalidOperation_WhenAlreadyResolved));
        Character character = new() { Name = "V", ApplicationUserId = "player-1" };
        ctx.Characters.Add(character);
        await ctx.SaveChangesAsync();

        ConditionService svc = CreateService(ctx);
        CharacterCondition condition = await svc.ApplyConditionAsync(
            character.Id, ConditionType.Shaken, null, null, "player-1");

        await svc.ResolveConditionAsync(condition.Id, "player-1");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.ResolveConditionAsync(condition.Id, "player-1"));
    }

    // ── Tilts ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ApplyTilt_ByOwner_Succeeds()
    {
        using ApplicationDbContext ctx = CreateContext(nameof(ApplyTilt_ByOwner_Succeeds));
        Character character = new() { Name = "V", ApplicationUserId = "player-1" };
        ctx.Characters.Add(character);
        await ctx.SaveChangesAsync();

        ConditionService svc = CreateService(ctx);
        CharacterTilt tilt = await svc.ApplyTiltAsync(
            character.Id, TiltType.Stunned, null, null, "player-1");

        Assert.True(tilt.IsActive);
        Assert.Equal(TiltType.Stunned, tilt.TiltType);
    }

    [Fact]
    public async Task RemoveTilt_SetsInactive()
    {
        using ApplicationDbContext ctx = CreateContext(nameof(RemoveTilt_SetsInactive));
        Character character = new() { Name = "V", ApplicationUserId = "player-1" };
        ctx.Characters.Add(character);
        await ctx.SaveChangesAsync();

        ConditionService svc = CreateService(ctx);
        CharacterTilt tilt = await svc.ApplyTiltAsync(
            character.Id, TiltType.KnockedDown, null, null, "player-1");

        await svc.RemoveTiltAsync(tilt.Id, "player-1");

        CharacterTilt? updated = await ctx.CharacterTilts.FindAsync(tilt.Id);
        Assert.False(updated!.IsActive);
        Assert.NotNull(updated.RemovedAt);
    }

    [Fact]
    public async Task GetActiveTilts_ReturnsOnlyActive()
    {
        using ApplicationDbContext ctx = CreateContext(nameof(GetActiveTilts_ReturnsOnlyActive));
        Character character = new() { Name = "V", ApplicationUserId = "player-1" };
        ctx.Characters.Add(character);
        await ctx.SaveChangesAsync();

        ConditionService svc = CreateService(ctx);
        CharacterTilt tilt1 = await svc.ApplyTiltAsync(character.Id, TiltType.Blinded, null, null, "player-1");
        CharacterTilt tilt2 = await svc.ApplyTiltAsync(character.Id, TiltType.Stunned, null, null, "player-1");
        await svc.RemoveTiltAsync(tilt1.Id, "player-1");

        List<CharacterTilt> active = await svc.GetActiveTiltsAsync(character.Id);
        Assert.Single(active);
        Assert.Equal(tilt2.Id, active[0].Id);
    }
}
