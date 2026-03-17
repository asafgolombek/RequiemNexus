using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Application.RealTime;
using RequiemNexus.Application.Services;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Domain.Contracts;
using RequiemNexus.Domain.Enums;
using Xunit;

namespace RequiemNexus.Data.Tests;

public class ConditionServiceTests
{
    private static ConditionService CreateConditionService(ApplicationDbContext ctx)
    {
        var logger = new Mock<ILogger<ConditionService>>().Object;
        var rules = new Mock<IConditionRules>().Object;
        var beatLedger = new Mock<IBeatLedgerService>().Object;
        var authHelper = new Mock<IAuthorizationHelper>().Object;
        var creationRules = new Mock<ICharacterCreationRules>().Object;

        return new ConditionService(ctx, rules, beatLedger, logger, authHelper, creationRules, new Mock<ISessionService>().Object);
    }

    private static ApplicationDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task ApplyConditionAsync_CreatesActiveCondition()
    {
        // Arrange
        using var ctx = CreateContext(nameof(ApplyConditionAsync_CreatesActiveCondition));
        var service = CreateConditionService(ctx);

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
        using var ctx = CreateContext(nameof(ResolveConditionAsync_SetsResolvedFlag));
        var service = CreateConditionService(ctx);

        var character = new Character { Name = "Test", ApplicationUserId = "user" };
        ctx.Characters.Add(character);
        await ctx.SaveChangesAsync();

        var cond = new CharacterCondition { CharacterId = character.Id, ConditionType = ConditionType.Guilty };
        ctx.CharacterConditions.Add(cond);
        await ctx.SaveChangesAsync();

        // Act
        await service.ResolveConditionAsync(cond.Id, "user");

        // Assert
        var dbCond = await ctx.CharacterConditions.FindAsync(cond.Id);
        Assert.NotNull(dbCond);
        Assert.True(dbCond.IsResolved);
        Assert.NotNull(dbCond.ResolvedAt);
    }
}
