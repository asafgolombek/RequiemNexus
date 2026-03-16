using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RequiemNexus.Application.RealTime;
using RequiemNexus.Application.Services;
using RequiemNexus.Data.Models;
using RequiemNexus.Domain;
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
    private static CharacterManagementService CreateCharacterService(ApplicationDbContext ctx)
    {
        // These tests never call GetCharactersByUserIdAsync / GetArchivedCharactersAsync,
        // so the factory is only needed to satisfy the constructor. It uses a throwaway
        // database name that is never accessed.
        ServiceCollection services = new();
        services.AddDbContextFactory<ApplicationDbContext>(
            o => o.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        IDbContextFactory<ApplicationDbContext> factory = services.BuildServiceProvider()
            .GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
        return new(ctx, factory, new CharacterCreationRules(), new BeatLedgerService(ctx), new Mock<ISessionService>().Object);
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
        var service = CreateCharacterService(ctx);
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
    public async Task DeleteCharacterAsync_RemovesFromDatabase()
    {
        // Arrange
        using var ctx = CreateContext(nameof(DeleteCharacterAsync_RemovesFromDatabase));
        var service = CreateCharacterService(ctx);
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
        var service = CreateCharacterService(ctx);
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
        var service = CreateCharacterService(ctx);
        var character = BuildNewCharacter();
        character.Beats = 4;
        ctx.Characters.Add(character);
        await ctx.SaveChangesAsync();

        // Act
        await service.AddBeatAsync(character);

        // Assert
        Assert.Equal(0, character.Beats);
        Assert.Equal(1, character.ExperiencePoints);
        Assert.Equal(1, character.TotalExperiencePoints);
        Assert.Equal(1, await ctx.BeatLedger.CountAsync()); // 1 for AddBeat
        Assert.Equal(1, await ctx.XpLedger.CountAsync()); // 1 for Conversion to XP
    }

    [Fact]
    public async Task AddXPAsync_IncrementsExperience()
    {
        // Arrange
        using var ctx = CreateContext(nameof(AddXPAsync_IncrementsExperience));
        var service = CreateCharacterService(ctx);
        var character = BuildNewCharacter();
        ctx.Characters.Add(character);
        await ctx.SaveChangesAsync();

        // Act
        await service.AddXPAsync(character);

        // Assert
        Assert.Equal(1, character.ExperiencePoints);
        Assert.Equal(1, character.TotalExperiencePoints);
        Assert.Equal(1, await ctx.XpLedger.CountAsync());
    }

    [Fact]
    public async Task GetCharacterWithAccessCheckAsync_ReturnsCharacter_ForOwner()
    {
        // Arrange
        using var ctx = CreateContext(nameof(GetCharacterWithAccessCheckAsync_ReturnsCharacter_ForOwner));
        var service = CreateCharacterService(ctx);
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
        var service = CreateCharacterService(ctx);

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
        var service = CreateCharacterService(ctx);
        var character = BuildNewCharacter("owner-id");
        ctx.Characters.Add(character);
        await ctx.SaveChangesAsync();

        // Act
        var result = await service.GetCharacterWithAccessCheckAsync(character.Id, "wrong-user");

        // Assert
        Assert.Null(result);
    }
}
