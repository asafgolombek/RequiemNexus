using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Application.Services;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Domain.Enums;
using RequiemNexus.Domain.Services;
using Xunit;

namespace RequiemNexus.Application.Tests;

public class CharacterDisciplineServiceTests
{
    private readonly DbContextOptions<ApplicationDbContext> _options;
    private readonly Mock<IBeatLedgerService> _beatLedgerMock = new();
    private readonly ExperienceCostRules _experienceCostRules = new();

    public CharacterDisciplineServiceTests()
    {
        _options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    [Fact]
    public async Task AddDisciplineAsync_InClan_UsesCorrectXp()
    {
        // Arrange
        using var ctx = new ApplicationDbContext(_options);
        var clan = new Clan { Id = 1, Name = "Ventrue" };
        var disc = new Discipline { Id = 1, Name = "Resilience" };
        clan.ClanDisciplines.Add(new ClanDiscipline { ClanId = 1, DisciplineId = 1 });
        ctx.Clans.Add(clan);
        ctx.Disciplines.Add(disc);

        var character = new Character { Id = 1, Name = "Test", ClanId = 1, ExperiencePoints = 100, BloodPotency = 1 };
        character.Clan = clan; // Set for helper
        ctx.Characters.Add(character);
        await ctx.SaveChangesAsync();

        var service = new CharacterDisciplineService(ctx, _beatLedgerMock.Object, _experienceCostRules);

        // Act
        // 1 dot in-clan = 1 * 4 = 4 XP
        await service.AddDisciplineAsync(character, disc.Id, 1, "user1");

        // Assert
        Assert.Equal(96, character.ExperiencePoints);
        Assert.Single(character.Disciplines);
        Assert.Equal(1, character.Disciplines.First().Rating);
        _beatLedgerMock.Verify(l => l.RecordXpSpendAsync(1, null, 4, XpExpense.Discipline, It.IsAny<string>(), "user1"), Times.Once);
    }

    [Fact]
    public async Task AddDisciplineAsync_OutOfClan_UsesCorrectXp()
    {
        // Arrange
        using var ctx = new ApplicationDbContext(_options);
        var clan = new Clan { Id = 1, Name = "Ventrue" };
        var disc = new Discipline { Id = 2, Name = "Vigor" }; // Not in Ventrue
        ctx.Clans.Add(clan);
        ctx.Disciplines.Add(disc);

        var character = new Character { Id = 1, Name = "Test", ClanId = 1, ExperiencePoints = 100, BloodPotency = 1 };
        character.Clan = clan;
        ctx.Characters.Add(character);
        await ctx.SaveChangesAsync();

        var service = new CharacterDisciplineService(ctx, _beatLedgerMock.Object, _experienceCostRules);

        // Act
        // 1 dot out-of-clan = 1 * 5 = 5 XP
        await service.AddDisciplineAsync(character, disc.Id, 1, "user1");

        // Assert
        Assert.Equal(95, character.ExperiencePoints);
        _beatLedgerMock.Verify(l => l.RecordXpSpendAsync(1, null, 5, XpExpense.Discipline, It.IsAny<string>(), "user1"), Times.Once);
    }

    [Fact]
    public async Task TryUpgradeDisciplineAsync_SuccessfulUpgrade_InClan()
    {
        // Arrange
        using var ctx = new ApplicationDbContext(_options);
        var clan = new Clan { Id = 1, Name = "Ventrue" };
        var disc = new Discipline { Id = 1, Name = "Resilience" };
        clan.ClanDisciplines.Add(new ClanDiscipline { ClanId = 1, DisciplineId = 1 });
        ctx.Clans.Add(clan);
        ctx.Disciplines.Add(disc);

        var character = new Character { Id = 1, Name = "Test", ClanId = 1, ExperiencePoints = 100, BloodPotency = 1 };
        character.Clan = clan;
        var cd = new CharacterDiscipline { Id = 1, CharacterId = 1, DisciplineId = 1, Rating = 1 };
        character.Disciplines.Add(cd);
        ctx.Characters.Add(character);
        await ctx.SaveChangesAsync();

        var service = new CharacterDisciplineService(ctx, _beatLedgerMock.Object, _experienceCostRules);

        // Act
        // Upgrade 1 -> 2 in-clan: 2 * 4 = 8 XP
        bool result = await service.TryUpgradeDisciplineAsync(character, 1, 2, "user1");

        // Assert
        Assert.True(result);
        Assert.Equal(92, character.ExperiencePoints);
        Assert.Equal(2, cd.Rating);
        _beatLedgerMock.Verify(l => l.RecordXpSpendAsync(1, null, 8, XpExpense.Discipline, It.IsAny<string>(), "user1"), Times.Once);
    }

    [Fact]
    public async Task TryUpgradeDisciplineAsync_InsufficientXp_ReturnsFalse()
    {
        // Arrange
        using var ctx = new ApplicationDbContext(_options);
        var character = new Character { Id = 1, Name = "Test", ExperiencePoints = 2, BloodPotency = 1 };
        var cd = new CharacterDiscipline { Id = 1, CharacterId = 1, DisciplineId = 1, Rating = 1 };
        character.Disciplines.Add(cd);
        ctx.Characters.Add(character);
        await ctx.SaveChangesAsync();

        var service = new CharacterDisciplineService(ctx, _beatLedgerMock.Object, _experienceCostRules);

        // Act
        // Upgrade 1 -> 2 out-of-clan: 2 * 5 = 10 XP (but only 2 available)
        bool result = await service.TryUpgradeDisciplineAsync(character, 1, 2, "user1");

        // Assert
        Assert.False(result);
        Assert.Equal(2, character.ExperiencePoints);
        Assert.Equal(1, cd.Rating);
    }
}
