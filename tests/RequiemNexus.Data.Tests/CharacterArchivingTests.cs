using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RequiemNexus.Application.RealTime;
using RequiemNexus.Application.Services;
using RequiemNexus.Data.Models;
using RequiemNexus.Domain;
using Xunit;

namespace RequiemNexus.Data.Tests;

public class CharacterArchivingTests
{
    private static CharacterManagementService CreateCharacterService(ApplicationDbContext ctx)
    {
        ServiceCollection services = new();
        services.AddDbContextFactory<ApplicationDbContext>(o => o.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        var factory = services.BuildServiceProvider().GetRequiredService<IDbContextFactory<ApplicationDbContext>>();

        var auth = new AuthorizationHelper(ctx, NullLogger<AuthorizationHelper>.Instance);

        return new CharacterManagementService(
            ctx,
            factory,
            new CharacterCreationRules(),
            new BeatLedgerService(ctx),
            auth,
            new Mock<ISessionService>().Object);
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
        var service = CreateCharacterService(ctx);
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
        var service = CreateCharacterService(ctx);
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
