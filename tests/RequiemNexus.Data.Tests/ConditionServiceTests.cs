using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Application.RealTime;
using RequiemNexus.Application.Services;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Data.RealTime;
using RequiemNexus.Domain.Contracts;
using RequiemNexus.Domain.Enums;
using Xunit;

namespace RequiemNexus.Data.Tests;

public class ConditionServiceTests
{
    private sealed class TestDbContextFactory : IDbContextFactory<ApplicationDbContext>
    {
        private readonly DbContextOptions<ApplicationDbContext> _options;

        public TestDbContextFactory(DbContextOptions<ApplicationDbContext> options) => _options = options;

        public ApplicationDbContext CreateDbContext() => new(_options);
    }

    private static ConditionService CreateConditionService(IDbContextFactory<ApplicationDbContext> factory)
    {
        var logger = new Mock<ILogger<ConditionService>>().Object;
        var rules = new Mock<IConditionRules>().Object;
        var beatLedger = new Mock<IBeatLedgerService>().Object;
        var authHelper = new Mock<IAuthorizationHelper>().Object;
        var creationRules = new Mock<ICharacterCreationRules>().Object;

        var session = new Mock<ISessionService>();
        session.Setup(s => s.BroadcastCharacterUpdateAsync(It.IsAny<int>())).Returns(Task.CompletedTask);
        session
            .Setup(s => s.NotifyConditionToastAsync(It.IsAny<string>(), It.IsAny<ConditionNotificationDto>()))
            .Returns(Task.CompletedTask);

        return new ConditionService(factory, rules, beatLedger, logger, authHelper, creationRules, session.Object);
    }

    private static DbContextOptions<ApplicationDbContext> CreateOptions(string dbName) =>
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

    private static ApplicationDbContext CreateContext(string dbName) => new(CreateOptions(dbName));

    [Fact]
    public async Task ApplyConditionAsync_CreatesActiveCondition()
    {
        // Arrange
        string dbName = nameof(ApplyConditionAsync_CreatesActiveCondition);
        var factory = new TestDbContextFactory(CreateOptions(dbName));
        var service = CreateConditionService(factory);

        // Act
        var result = await service.ApplyConditionAsync(1, ConditionType.Guilty, "Custom", "Desc", "user");

        // Assert
        Assert.Equal(1, result.CharacterId);
        Assert.Equal(ConditionType.Guilty, result.ConditionType);
        Assert.False(result.IsResolved);
    }

    [Fact]
    public async Task ResolveConditionAsync_SetsResolvedFlag()
    {
        // Arrange
        string dbName = nameof(ResolveConditionAsync_SetsResolvedFlag);
        using var ctx = CreateContext(dbName);
        var factory = new TestDbContextFactory(CreateOptions(dbName));
        var service = CreateConditionService(factory);

        var character = new Character { Name = "Test", ApplicationUserId = "user" };
        ctx.Characters.Add(character);
        await ctx.SaveChangesAsync();

        var cond = new CharacterCondition { CharacterId = character.Id, ConditionType = ConditionType.Guilty };
        ctx.CharacterConditions.Add(cond);
        await ctx.SaveChangesAsync();

        // Act
        await service.ResolveConditionAsync(cond.Id, "user");

        // Assert — resolution used a different factory context; clear local tracker to read store state.
        ctx.ChangeTracker.Clear();
        CharacterCondition? dbCond = await ctx.CharacterConditions.AsNoTracking().FirstOrDefaultAsync(c => c.Id == cond.Id);
        Assert.NotNull(dbCond);
        Assert.True(dbCond.IsResolved);
        Assert.NotNull(dbCond.ResolvedAt);
    }
}
