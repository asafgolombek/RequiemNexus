using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Application.Events;
using RequiemNexus.Application.RealTime;
using RequiemNexus.Application.Services;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Domain.Contracts;
using RequiemNexus.Domain.Services;
using Xunit;

namespace RequiemNexus.Data.Tests;

public class CharacterArchivingTests
{
    private static CharacterManagementService CreateCharacterService(ApplicationDbContext ctx, string databaseName)
    {
        IDbContextFactory<ApplicationDbContext> factory = InMemoryApplicationDbContextFactories.ForDatabaseName(databaseName);
        var auth = new AuthorizationHelper(factory, NullLogger<AuthorizationHelper>.Instance);
        var humanity = new HumanityService(
            ctx,
            Mock.Of<IAuthorizationHelper>(),
            Mock.Of<IDomainEventDispatcher>(),
            Mock.Of<IDiceService>(),
            Mock.Of<ISessionService>(),
            Mock.Of<IConditionService>(),
            NullLogger<HumanityService>.Instance);

        return new CharacterManagementService(
            ctx,
            factory,
            new CharacterCreationRules(),
            new BeatLedgerService(ctx),
            auth,
            new Mock<ISessionService>().Object,
            new CharacterCreationService(),
            humanity);
    }

    private static ApplicationDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task ArchiveCharacterAsync_SetsFlagAndDate()
    {
        // Arrange
        using var ctx = CreateContext(nameof(ArchiveCharacterAsync_SetsFlagAndDate));
        var service = CreateCharacterService(ctx, nameof(ArchiveCharacterAsync_SetsFlagAndDate));
        var character = new Character { ApplicationUserId = "user", Name = "Test" };
        ctx.Characters.Add(character);
        await ctx.SaveChangesAsync();

        // Act
        await service.ArchiveCharacterAsync(character.Id, "user");

        // Assert
        Assert.True(character.IsArchived);
        Assert.NotNull(character.ArchivedAt);
    }

    [Fact]
    public async Task UnarchiveCharacterAsync_ClearsFlagAndDate()
    {
        // Arrange
        using var ctx = CreateContext(nameof(UnarchiveCharacterAsync_ClearsFlagAndDate));
        var service = CreateCharacterService(ctx, nameof(UnarchiveCharacterAsync_ClearsFlagAndDate));
        var character = new Character { ApplicationUserId = "user", Name = "Test", IsArchived = true, ArchivedAt = DateTime.UtcNow };
        ctx.Characters.Add(character);
        await ctx.SaveChangesAsync();

        // Act
        await service.UnarchiveCharacterAsync(character.Id, "user");

        // Assert
        Assert.False(character.IsArchived);
        Assert.Null(character.ArchivedAt);
    }
}
