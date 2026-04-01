using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Application.RealTime;
using RequiemNexus.Application.Services;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Data.Models.Enums;
using RequiemNexus.Domain.Enums;
using Xunit;

namespace RequiemNexus.Application.Tests;

/// <summary>
/// Theban Sorcery Humanity floor must match <see cref="SorceryActivationService.BeginRiteActivationAsync"/> (P4 backlog).
/// </summary>
public class SorceryServiceThebanHumanityTests
{
    private static Mock<IAuthorizationHelper> CreateAuthMock()
    {
        var auth = new Mock<IAuthorizationHelper>();
        auth
            .Setup(a => a.RequireCharacterOwnerAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        auth
            .Setup(a => a.RequireStorytellerAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        return auth;
    }

    private static async Task<(ApplicationDbContext Context, IAsyncDisposable Teardown)> CreateSqliteContextAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        var ctx = new ApplicationDbContext(options);
        await ctx.Database.EnsureCreatedAsync();

        IAsyncDisposable teardown = new SqliteTeardown(connection, ctx);
        return (ctx, teardown);
    }

    private sealed class SqliteTeardown(SqliteConnection connection, ApplicationDbContext context) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            await context.DisposeAsync();
            await connection.DisposeAsync();
        }
    }

    private static SorceryService CreateSorceryService(ApplicationDbContext ctx, IAuthorizationHelper? auth = null)
    {
        var session = new Mock<ISessionService>();
        session.Setup(s => s.BroadcastCharacterUpdateAsync(It.IsAny<int>())).Returns(Task.CompletedTask);
        var beat = new Mock<IBeatLedgerService>();
        beat
            .Setup(b => b.RecordXpSpendAsync(
                It.IsAny<int>(),
                It.IsAny<int?>(),
                It.IsAny<int>(),
                It.IsAny<XpExpense>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string?>()))
            .Returns(Task.CompletedTask);
        return new SorceryService(
            ctx,
            auth ?? CreateAuthMock().Object,
            beat.Object,
            session.Object,
            new Mock<ILogger<SorceryService>>().Object);
    }

    [Fact]
    public async Task GetEligibleRitesAsync_ExcludesThebanMiraclesAboveHumanity()
    {
        (ApplicationDbContext ctx, IAsyncDisposable teardown) = await CreateSqliteContextAsync();
        await using (teardown)
        {
            ctx.Users.Add(new ApplicationUser
            {
                Id = "p1",
                UserName = "p1",
                NormalizedUserName = "P1",
                Email = "p1@test",
                NormalizedEmail = "P1@TEST",
                EmailConfirmed = true,
            });
            ctx.Campaigns.Add(new Campaign { Id = 1, Name = "C", StoryTellerId = "p1" });
            ctx.CovenantDefinitions.Add(new CovenantDefinition
            {
                Id = 2,
                Name = "Lancea",
                SupportsBloodSorcery = true,
            });
            var theban = new Discipline { Id = 11, Name = "Theban Sorcery" };
            ctx.Disciplines.Add(theban);
            var character = new Character
            {
                Id = 1,
                Name = "LowHumanity",
                ApplicationUserId = "p1",
                CampaignId = 1,
                CovenantId = 2,
                Humanity = 4,
                ExperiencePoints = 20,
            };
            ctx.Characters.Add(character);
            ctx.CharacterDisciplines.Add(new CharacterDiscipline
            {
                CharacterId = 1,
                DisciplineId = 11,
                Rating = 5,
            });
            ctx.SorceryRiteDefinitions.Add(new SorceryRiteDefinition
            {
                Id = 10,
                Name = "Reachable",
                Description = "d",
                Level = 3,
                SorceryType = SorceryType.Theban,
                XpCost = 1,
                TargetSuccesses = 5,
                RequiredCovenantId = 2,
            });
            ctx.SorceryRiteDefinitions.Add(new SorceryRiteDefinition
            {
                Id = 11,
                Name = "TooHigh",
                Description = "d",
                Level = 5,
                SorceryType = SorceryType.Theban,
                XpCost = 1,
                TargetSuccesses = 8,
                RequiredCovenantId = 2,
            });
            await ctx.SaveChangesAsync();

            var sut = CreateSorceryService(ctx);
            var eligible = await sut.GetEligibleRitesAsync(1, "p1");

            Assert.Single(eligible);
            Assert.Equal(10, eligible[0].Id);
        }
    }

    [Fact]
    public async Task RequestLearnRiteAsync_ThebanBelowHumanity_Throws()
    {
        (ApplicationDbContext ctx, IAsyncDisposable teardown) = await CreateSqliteContextAsync();
        await using (teardown)
        {
            ctx.Users.Add(new ApplicationUser
            {
                Id = "p1",
                UserName = "p1",
                NormalizedUserName = "P1",
                Email = "p1@test",
                NormalizedEmail = "P1@TEST",
                EmailConfirmed = true,
            });
            ctx.Campaigns.Add(new Campaign { Id = 1, Name = "C", StoryTellerId = "p1" });
            ctx.CovenantDefinitions.Add(new CovenantDefinition
            {
                Id = 2,
                Name = "Lancea",
                SupportsBloodSorcery = true,
            });
            var theban = new Discipline { Id = 11, Name = "Theban Sorcery" };
            ctx.Disciplines.Add(theban);
            ctx.Characters.Add(new Character
            {
                Id = 1,
                Name = "LowHumanity",
                ApplicationUserId = "p1",
                CampaignId = 1,
                CovenantId = 2,
                Humanity = 4,
                ExperiencePoints = 20,
            });
            ctx.CharacterDisciplines.Add(new CharacterDiscipline
            {
                CharacterId = 1,
                DisciplineId = 11,
                Rating = 5,
            });
            ctx.SorceryRiteDefinitions.Add(new SorceryRiteDefinition
            {
                Id = 11,
                Name = "TooHigh",
                Description = "d",
                Level = 5,
                SorceryType = SorceryType.Theban,
                XpCost = 1,
                TargetSuccesses = 8,
                RequiredCovenantId = 2,
            });
            await ctx.SaveChangesAsync();

            var sut = CreateSorceryService(ctx);
            InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                sut.RequestLearnRiteAsync(1, 11, "p1"));

            Assert.Contains("Humanity 5", ex.Message, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task ApproveRiteLearnAsync_ThebanBelowHumanityAtApproval_Throws()
    {
        (ApplicationDbContext ctx, IAsyncDisposable teardown) = await CreateSqliteContextAsync();
        await using (teardown)
        {
            ctx.Users.Add(new ApplicationUser
            {
                Id = "p1",
                UserName = "p1",
                NormalizedUserName = "P1",
                Email = "p1@test",
                NormalizedEmail = "P1@TEST",
                EmailConfirmed = true,
            });
            ctx.Campaigns.Add(new Campaign { Id = 1, Name = "C", StoryTellerId = "st" });
            ctx.Users.Add(new ApplicationUser
            {
                Id = "st",
                UserName = "st",
                NormalizedUserName = "ST",
                Email = "st@test",
                NormalizedEmail = "ST@TEST",
                EmailConfirmed = true,
            });
            ctx.CovenantDefinitions.Add(new CovenantDefinition
            {
                Id = 2,
                Name = "Lancea",
                SupportsBloodSorcery = true,
            });
            var theban = new Discipline { Id = 11, Name = "Theban Sorcery" };
            ctx.Disciplines.Add(theban);
            ctx.Characters.Add(new Character
            {
                Id = 1,
                Name = "Dropped",
                ApplicationUserId = "p1",
                CampaignId = 1,
                CovenantId = 2,
                Humanity = 4,
                ExperiencePoints = 20,
            });
            ctx.CharacterDisciplines.Add(new CharacterDiscipline
            {
                CharacterId = 1,
                DisciplineId = 11,
                Rating = 5,
            });
            ctx.SorceryRiteDefinitions.Add(new SorceryRiteDefinition
            {
                Id = 11,
                Name = "LevelFive",
                Description = "d",
                Level = 5,
                SorceryType = SorceryType.Theban,
                XpCost = 1,
                TargetSuccesses = 8,
                RequiredCovenantId = 2,
            });
            ctx.CharacterRites.Add(new CharacterRite
            {
                Id = 100,
                CharacterId = 1,
                SorceryRiteDefinitionId = 11,
                Status = RiteLearnStatus.Pending,
                AppliedAt = DateTime.UtcNow,
            });
            await ctx.SaveChangesAsync();

            var sut = CreateSorceryService(ctx);
            InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                sut.ApproveRiteLearnAsync(100, null, "st"));

            Assert.Contains("Humanity 5", ex.Message, StringComparison.Ordinal);
        }
    }
}
