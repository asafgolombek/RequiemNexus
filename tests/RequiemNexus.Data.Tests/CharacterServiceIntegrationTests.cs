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
using RequiemNexus.Web.Helpers;
using RequiemNexus.Web.Services;
using Xunit;

namespace RequiemNexus.Data.Tests;

/// <summary>
/// Integration tests for CharacterService wired to an EF Core InMemory database.
/// Each test creates its own DbContext with a unique database name for full isolation.
/// </summary>
public class CharacterServiceIntegrationTests
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

    private static Character BuildNewCharacter(string userId = "test-user")
    {
        var character = new Character
        {
            ApplicationUserId = userId,
            Name = "Test Vampire",
            Size = 5
        };

        CharacterTraitHelper.SeedAttributes(character);
        CharacterTraitHelper.SeedSkills(character);

        // Set baseline attributes for tests
        character.Attributes.First(a => a.Name == "Stamina").Rating = 2;
        character.Attributes.First(a => a.Name == "Resolve").Rating = 2;
        character.Attributes.First(a => a.Name == "Composure").Rating = 2;

        return character;
    }

    [Fact]
    public async Task EmbraceCharacterAsync_CalculatesDerivedStats()
    {
        // Arrange
        using var ctx = CreateContext(nameof(EmbraceCharacterAsync_CalculatesDerivedStats));
        var service = CreateCharacterService(ctx, nameof(EmbraceCharacterAsync_CalculatesDerivedStats));
        var character = BuildNewCharacter();

        // Act
        var result = await service.EmbraceCharacterAsync(character);

        // Assert
        Assert.Equal(7, result.MaxHealth);
        Assert.Equal(7, result.CurrentHealth);
        Assert.Equal(4, result.MaxWillpower);
        Assert.Equal(4, result.CurrentWillpower);
        Assert.Equal(1, result.BloodPotency);
        Assert.Equal(10, result.MaxVitae);
        Assert.Equal(10, result.CurrentVitae);
    }

    [Fact]
    public async Task EmbraceCharacterAsync_PersistsMeritsAndAspirations()
    {
        string dbName = nameof(EmbraceCharacterAsync_PersistsMeritsAndAspirations);
        using var ctx = CreateContext(dbName);
        var service = CreateCharacterService(ctx, dbName);

        var merit = new Merit
        {
            Name = "Integration Test Merit",
            ValidRatings = "1,2,3",
            IsHomebrew = false,
        };
        ctx.Merits.Add(merit);
        await ctx.SaveChangesAsync();

        Character character = BuildNewCharacter();
        character.Merits.Add(new CharacterMerit { MeritId = merit.Id, Rating = 2, Specification = string.Empty });
        character.Aspirations.Add(new CharacterAspiration { Description = "Find a haven" });

        Character result = await service.EmbraceCharacterAsync(character);

        Assert.Single(await ctx.CharacterMerits.Where(cm => cm.CharacterId == result.Id).ToListAsync());
        Assert.Single(await ctx.CharacterAspirations.Where(a => a.CharacterId == result.Id).ToListAsync());

        Character? loaded = await service.GetCharacterByIdAsync(result.Id, character.ApplicationUserId);
        Assert.NotNull(loaded);
        Assert.Single(loaded.Merits);
        Assert.Equal(merit.Id, loaded.Merits.First().MeritId);
        Assert.Equal(2, loaded.Merits.First().Rating);
        Assert.Single(loaded.Aspirations);
        Assert.Equal("Find a haven", loaded.Aspirations.First().Description);
    }

    [Fact]
    public async Task DeleteCharacterAsync_RemovesFromDatabase()
    {
        // Arrange
        using var ctx = CreateContext(nameof(DeleteCharacterAsync_RemovesFromDatabase));
        var service = CreateCharacterService(ctx, nameof(DeleteCharacterAsync_RemovesFromDatabase));
        var character = BuildNewCharacter();
        ctx.Characters.Add(character);
        await ctx.SaveChangesAsync();

        // Act
        await service.DeleteCharacterAsync(character.Id, character.ApplicationUserId);

        // Assert
        Assert.Null(await ctx.Characters.FindAsync(character.Id));
    }

    [Fact]
    public async Task SaveAsync_PersistsChanges()
    {
        // Arrange
        using var ctx = CreateContext(nameof(SaveAsync_PersistsChanges));
        var service = CreateCharacterService(ctx, nameof(SaveAsync_PersistsChanges));
        var character = BuildNewCharacter();
        ctx.Characters.Add(character);
        await ctx.SaveChangesAsync();

        // Act
        character.Name = "Updated Name";
        await service.SaveAsync(character);

        // Assert
        var dbChar = await ctx.Characters.FindAsync(character.Id);
        Assert.Equal("Updated Name", dbChar!.Name);
    }

    [Fact]
    public async Task AddBeatAsync_IncrementsBeatsAndConvertsToXp()
    {
        // Arrange
        using var ctx = CreateContext(nameof(AddBeatAsync_IncrementsBeatsAndConvertsToXp));
        var service = CreateCharacterService(ctx, nameof(AddBeatAsync_IncrementsBeatsAndConvertsToXp));
        var character = BuildNewCharacter();
        character.Beats = 4;
        ctx.Characters.Add(character);
        await ctx.SaveChangesAsync();

        // Act
        await service.AddBeatAsync(character.Id, character.ApplicationUserId);

        // Assert
        var dbChar = await ctx.Characters.FindAsync(character.Id);
        Assert.NotNull(dbChar);
        Assert.Equal(0, dbChar.Beats);
        Assert.Equal(1, dbChar.ExperiencePoints);
        Assert.Equal(1, dbChar.TotalExperiencePoints);
        Assert.Equal(1, await ctx.BeatLedger.CountAsync()); // 1 for AddBeat
        Assert.Equal(1, await ctx.XpLedger.CountAsync()); // 1 for Conversion to XP
    }

    [Fact]
    public async Task AddXPAsync_IncrementsExperience()
    {
        // Arrange
        using var ctx = CreateContext(nameof(AddXPAsync_IncrementsExperience));
        var service = CreateCharacterService(ctx, nameof(AddXPAsync_IncrementsExperience));
        var character = BuildNewCharacter();
        ctx.Characters.Add(character);
        await ctx.SaveChangesAsync();

        // Act
        await service.AddXPAsync(character.Id, character.ApplicationUserId);

        // Assert
        var dbChar = await ctx.Characters.FindAsync(character.Id);
        Assert.NotNull(dbChar);
        Assert.Equal(1, dbChar.ExperiencePoints);
        Assert.Equal(1, dbChar.TotalExperiencePoints);
        Assert.Equal(1, await ctx.XpLedger.CountAsync());
    }

    [Fact]
    public async Task GetCharacterWithAccessCheckAsync_ReturnsCharacter_ForOwner()
    {
        // Arrange
        using var ctx = CreateContext(nameof(GetCharacterWithAccessCheckAsync_ReturnsCharacter_ForOwner));
        var service = CreateCharacterService(ctx, nameof(GetCharacterWithAccessCheckAsync_ReturnsCharacter_ForOwner));
        var character = BuildNewCharacter("owner-id");
        ctx.Characters.Add(character);
        await ctx.SaveChangesAsync();

        // Act
        var result = await service.GetCharacterWithAccessCheckAsync(character.Id, "owner-id");

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Value.IsOwner);
        Assert.Equal(character.Id, result.Value.Character.Id);
    }

    [Fact]
    public async Task GetCharacterWithAccessCheckAsync_ReturnsReadOnly_ForCampaignMember()
    {
        // Arrange
        using var ctx = CreateContext(nameof(GetCharacterWithAccessCheckAsync_ReturnsReadOnly_ForCampaignMember));
        var service = CreateCharacterService(ctx, nameof(GetCharacterWithAccessCheckAsync_ReturnsReadOnly_ForCampaignMember));

        var campaign = new Campaign { Name = "Test Campaign", StoryTellerId = "st-id" };
        ctx.Campaigns.Add(campaign);
        await ctx.SaveChangesAsync();

        var character = BuildNewCharacter("owner-id");
        character.CampaignId = campaign.Id;
        ctx.Characters.Add(character);
        await ctx.SaveChangesAsync();

        // Act
        var result = await service.GetCharacterWithAccessCheckAsync(character.Id, "st-id");

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Value.IsOwner);
        Assert.Equal(character.Id, result.Value.Character.Id);
    }

    [Fact]
    public async Task GetCharacterWithAccessCheckAsync_ReturnsNull_ForNonMember()
    {
        // Arrange
        using var ctx = CreateContext(nameof(GetCharacterWithAccessCheckAsync_ReturnsNull_ForNonMember));
        var service = CreateCharacterService(ctx, nameof(GetCharacterWithAccessCheckAsync_ReturnsNull_ForNonMember));
        var character = BuildNewCharacter("owner-id");
        ctx.Characters.Add(character);
        await ctx.SaveChangesAsync();

        // Act
        var result = await service.GetCharacterWithAccessCheckAsync(character.Id, "wrong-user");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task EmbraceCharacterAsync_ThrowsWhenNecromancyOnNonMekhet()
    {
        string dbName = nameof(EmbraceCharacterAsync_ThrowsWhenNecromancyOnNonMekhet);
        using var ctx = CreateContext(dbName);
        var service = CreateCharacterService(ctx, dbName);

        var animalism = new Discipline { Name = "Animalism" };
        var dominate = new Discipline { Name = "Dominate" };
        var resilience = new Discipline { Name = "Resilience" };
        var necromancy = new Discipline { Name = "Necromancy", IsNecromancy = true };
        ctx.Disciplines.AddRange(animalism, dominate, resilience, necromancy);
        await ctx.SaveChangesAsync();

        var ventrue = new Clan { Name = "Ventrue" };
        ctx.Clans.Add(ventrue);
        await ctx.SaveChangesAsync();

        ventrue.ClanDisciplines.Add(new ClanDiscipline { ClanId = ventrue.Id, DisciplineId = animalism.Id });
        ventrue.ClanDisciplines.Add(new ClanDiscipline { ClanId = ventrue.Id, DisciplineId = dominate.Id });
        ventrue.ClanDisciplines.Add(new ClanDiscipline { ClanId = ventrue.Id, DisciplineId = resilience.Id });
        await ctx.SaveChangesAsync();

        Character character = BuildNewCharacter();
        character.ClanId = ventrue.Id;
        character.Disciplines.Add(new CharacterDiscipline { DisciplineId = animalism.Id, Rating = 2 });
        character.Disciplines.Add(new CharacterDiscipline { DisciplineId = necromancy.Id, Rating = 1 });

        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.EmbraceCharacterAsync(character));

        Assert.Contains("Necromancy", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SaveAsync_ClampsHumanityToCruacCap()
    {
        string dbName = nameof(SaveAsync_ClampsHumanityToCruacCap);
        using var ctx = CreateContext(dbName);
        var service = CreateCharacterService(ctx, dbName);

        var cruac = new Discipline { Name = "Crúac" };
        ctx.Disciplines.Add(cruac);
        Character character = BuildNewCharacter();
        character.Humanity = 9;
        ctx.Characters.Add(character);
        await ctx.SaveChangesAsync();

        ctx.CharacterDisciplines.Add(new CharacterDiscipline
        {
            CharacterId = character.Id,
            DisciplineId = cruac.Id,
            Rating = 3,
        });
        await ctx.SaveChangesAsync();

        Character tracked = await ctx.Characters.FirstAsync(c => c.Id == character.Id);
        tracked.Humanity = 9;
        await service.SaveAsync(tracked);

        Character reloaded = await ctx.Characters.AsNoTracking().FirstAsync(c => c.Id == character.Id);
        Assert.Equal(7, reloaded.Humanity);
    }
}
