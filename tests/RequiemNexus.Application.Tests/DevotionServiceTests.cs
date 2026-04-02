using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Application.Services;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Domain.Enums;
using Xunit;

namespace RequiemNexus.Application.Tests;

public class DevotionServiceTests
{
    private readonly DbContextOptions<ApplicationDbContext> _options;
    private readonly Mock<IBeatLedgerService> _beatLedgerMock = new();
    private readonly Mock<ILogger<DevotionService>> _loggerMock = new();

    public DevotionServiceTests()
    {
        _options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    [Fact]
    public async Task PurchaseDevotionAsync_MeetsPrerequisites_Success()
    {
        // Arrange
        using var ctx = new ApplicationDbContext(_options);
        var disc = new Discipline { Id = 1, Name = "Vigor" };
        var devotion = new DevotionDefinition { Id = 1, Name = "Test Devotion", XpCost = 2 };
        devotion.Prerequisites.Add(new DevotionPrerequisite { DevotionDefinitionId = 1, DisciplineId = 1, MinimumLevel = 2 });
        ctx.Disciplines.Add(disc);
        ctx.DevotionDefinitions.Add(devotion);

        var character = new Character { Id = 1, Name = "Test", ExperiencePoints = 10, BloodPotency = 1 };
        character.Disciplines.Add(new CharacterDiscipline { CharacterId = 1, DisciplineId = 1, Rating = 2 });
        ctx.Characters.Add(character);
        await ctx.SaveChangesAsync();

        IReferenceDataCache cache = await ReferenceDataCacheTestDoubles.WarmFromAsync(ctx);
        var service = new DevotionService(ctx, _beatLedgerMock.Object, cache, _loggerMock.Object);

        // Act
        var result = await service.PurchaseDevotionAsync(character, 1, "user1");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(8, character.ExperiencePoints);
        Assert.Single(character.Devotions);
        _beatLedgerMock.Verify(
            l => l.RecordXpSpendAsync(1, null, 2, XpExpense.Devotion, It.IsAny<string>(), "user1", null),
            Times.Once);
    }

    [Fact]
    public async Task MeetsPrerequisites_OrGroup_SatisfiedAny()
    {
        // Arrange
        using var ctx = new ApplicationDbContext(_options);
        var devotion = new DevotionDefinition { Id = 1, Name = "OR Devotion" };
        // Vigor 2 (Group 1) OR Resilience 2 (Group 2)
        devotion.Prerequisites.Add(new DevotionPrerequisite { DisciplineId = 1, MinimumLevel = 2, OrGroupId = 1 });
        devotion.Prerequisites.Add(new DevotionPrerequisite { DisciplineId = 2, MinimumLevel = 2, OrGroupId = 2 });

        var character = new Character { Id = 1 };
        character.Disciplines.Add(new CharacterDiscipline { DisciplineId = 2, Rating = 2 }); // Has Resilience 2

        var service = new DevotionService(
            ctx,
            _beatLedgerMock.Object,
            ReferenceDataCacheTestDoubles.EmptyButInitialized(),
            _loggerMock.Object);

        // Act
        bool result = service.MeetsPrerequisites(character, devotion);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task MeetsPrerequisites_BloodlineRequired_FalseIfNoBloodline()
    {
        // Arrange
        using var ctx = new ApplicationDbContext(_options);
        var devotion = new DevotionDefinition { Id = 1, Name = "Bloodline Devotion", RequiredBloodlineId = 5 };

        var character = new Character { Id = 1 };
        // No bloodline

        var service = new DevotionService(
            ctx,
            _beatLedgerMock.Object,
            ReferenceDataCacheTestDoubles.EmptyButInitialized(),
            _loggerMock.Object);

        // Act
        bool result = service.MeetsPrerequisites(character, devotion);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task MeetsPrerequisites_BloodlineRequired_TrueIfActiveBloodline()
    {
        // Arrange
        using var ctx = new ApplicationDbContext(_options);
        var devotion = new DevotionDefinition { Id = 1, Name = "Bloodline Devotion", RequiredBloodlineId = 5 };

        var character = new Character { Id = 1 };
        character.Bloodlines.Add(new CharacterBloodline { BloodlineDefinitionId = 5, Status = RequiemNexus.Data.Models.Enums.BloodlineStatus.Active });

        var service = new DevotionService(
            ctx,
            _beatLedgerMock.Object,
            ReferenceDataCacheTestDoubles.EmptyButInitialized(),
            _loggerMock.Object);

        // Act
        bool result = service.MeetsPrerequisites(character, devotion);

        // Assert
        Assert.True(result);
    }
}
