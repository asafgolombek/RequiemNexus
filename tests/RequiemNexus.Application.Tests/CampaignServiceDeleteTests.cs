using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Application.RealTime;
using RequiemNexus.Application.Services;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Domain.Enums;
using Xunit;

namespace RequiemNexus.Application.Tests;

/// <summary>
/// SQLite-backed tests for <see cref="CampaignService.DeleteCampaignAsync"/> so real FK constraints apply
/// (EF InMemory does not enforce them; <see cref="Microsoft.EntityFrameworkCore.RelationalQueryableExtensions.ExecuteUpdateAsync{TSource}"/> is relational-only).
/// </summary>
public class CampaignServiceDeleteTests
{
    private sealed class MatchingDbContextFactory(DbContextOptions<ApplicationDbContext> options) : IDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext() => new(options);

        public ValueTask<ApplicationDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(new ApplicationDbContext(options));
    }

    private static CampaignService CreateCampaignService(ApplicationDbContext ctx, DbContextOptions<ApplicationDbContext> options)
    {
        var logger = new Mock<ILogger<CampaignService>>().Object;
        var factory = new MatchingDbContextFactory(options);
        var authHelper = new AuthorizationHelper(factory, NullLogger<AuthorizationHelper>.Instance);
        return new CampaignService(ctx, factory, logger, authHelper, new Mock<ISessionService>().Object);
    }

    private static async Task<(ApplicationDbContext Context, IAsyncDisposable Teardown, DbContextOptions<ApplicationDbContext> Options)> CreateSqliteAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        var ctx = new ApplicationDbContext(options);
        await ctx.Database.EnsureCreatedAsync();

        IAsyncDisposable teardown = new SqliteTeardown(connection, ctx);
        return (ctx, teardown, options);
    }

    private sealed class SqliteTeardown(SqliteConnection connection, ApplicationDbContext context) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            await context.DisposeAsync();
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task DeleteCampaignAsync_ClearsBeatLedgerCampaignFkAndRemovesCampaign_OnSqlite()
    {
        (ApplicationDbContext ctx, IAsyncDisposable teardown, DbContextOptions<ApplicationDbContext> options) = await CreateSqliteAsync();
        await using (teardown)
        {
            ctx.Users.AddRange(
                new ApplicationUser
                {
                    Id = "st-user",
                    UserName = "st@test",
                    NormalizedUserName = "ST@TEST",
                    Email = "st@test.com",
                    NormalizedEmail = "ST@TEST.COM",
                    EmailConfirmed = true,
                },
                new ApplicationUser
                {
                    Id = "player",
                    UserName = "p@test",
                    NormalizedUserName = "P@TEST",
                    Email = "p@test.com",
                    NormalizedEmail = "P@TEST.COM",
                    EmailConfirmed = true,
                });
            await ctx.SaveChangesAsync();

            var campaign = new Campaign { Name = "testing", StoryTellerId = "st-user" };
            ctx.Campaigns.Add(campaign);
            var character = new Character { Name = "PC", ApplicationUserId = "player" };
            ctx.Characters.Add(character);
            await ctx.SaveChangesAsync();

            CampaignService service = CreateCampaignService(ctx, options);
            await service.AddCharacterToCampaignAsync(campaign.Id, character.Id, "player");

            ctx.BeatLedger.Add(new BeatLedgerEntry
            {
                CharacterId = character.Id,
                CampaignId = campaign.Id,
                Source = BeatSource.StorytellerAward,
                Reason = "test",
            });
            await ctx.SaveChangesAsync();

            await service.DeleteCampaignAsync(campaign.Id, "st-user");

            Assert.False(await ctx.Campaigns.AnyAsync(c => c.Id == campaign.Id));
            BeatLedgerEntry entry = await ctx.BeatLedger.AsNoTracking().SingleAsync(e => e.CharacterId == character.Id);
            Assert.Null(entry.CampaignId);
            Character reloaded = await ctx.Characters.AsNoTracking().SingleAsync(c => c.Id == character.Id);
            Assert.Null(reloaded.CampaignId);
        }
    }
}
