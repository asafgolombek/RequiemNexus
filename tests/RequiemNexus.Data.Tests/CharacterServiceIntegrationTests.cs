using Microsoft.EntityFrameworkCore;
using RequiemNexus.Data.Models;
using RequiemNexus.Web.Services;
using Xunit;
namespace RequiemNexus.Data.Tests;

/// <summary>
/// Integration tests for CharacterService wired to an EF Core InMemory database.
/// Each test creates its own DbContext with a unique database name for full isolation.
/// </summary>
public class CharacterServiceIntegrationTests
{
    private static ApplicationDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new ApplicationDbContext(options);
    }

    private static Character BuildNewCharacter(string userId = "test-user") => new()
    {
        ApplicationUserId = userId,
        Name = "Test Vampire",
        Size = 5,
        Stamina = 2,
        Resolve = 2,
        Composure = 2
    };

    // -----------------------------------------------------------------------
    // EmbraceCharacterAsync — derived stat initialisation
    // -----------------------------------------------------------------------

    [Fact]
    public async Task EmbraceCharacter_SetsMaxHealthCorrectly()
    {
        using var ctx = CreateContext(nameof(EmbraceCharacter_SetsMaxHealthCorrectly));
        var svc = new CharacterService(ctx);
        var character = BuildNewCharacter();
        character.Size = 5;
        character.Stamina = 2;

        var result = await svc.EmbraceCharacterAsync(character);

        // MaxHealth = Size + Stamina = 5 + 2 = 7
        Assert.Equal(7, result.MaxHealth);
        Assert.Equal(7, result.CurrentHealth);
    }

    [Fact]
    public async Task EmbraceCharacter_SetsMaxWillpowerCorrectly()
    {
        using var ctx = CreateContext(nameof(EmbraceCharacter_SetsMaxWillpowerCorrectly));
        var svc = new CharacterService(ctx);
        var character = BuildNewCharacter();
        character.Resolve = 2;
        character.Composure = 3;

        var result = await svc.EmbraceCharacterAsync(character);

        // MaxWillpower = Resolve + Composure = 2 + 3 = 5
        Assert.Equal(5, result.MaxWillpower);
        Assert.Equal(5, result.CurrentWillpower);
    }

    [Fact]
    public async Task EmbraceCharacter_SetsBloodPotencyAndVitae()
    {
        using var ctx = CreateContext(nameof(EmbraceCharacter_SetsBloodPotencyAndVitae));
        var svc = new CharacterService(ctx);

        var result = await svc.EmbraceCharacterAsync(BuildNewCharacter());

        Assert.Equal(1, result.BloodPotency);
        Assert.Equal(10, result.MaxVitae);
        Assert.Equal(10, result.CurrentVitae);
    }

    [Fact]
    public async Task EmbraceCharacter_PersistsCharacterToDatabase()
    {
        using var ctx = CreateContext(nameof(EmbraceCharacter_PersistsCharacterToDatabase));
        var svc = new CharacterService(ctx);

        await svc.EmbraceCharacterAsync(BuildNewCharacter());

        Assert.Equal(1, await ctx.Characters.CountAsync());
    }

    // -----------------------------------------------------------------------
    // Beat / XP flow
    // -----------------------------------------------------------------------

    [Fact]
    public async Task AddBeat_FourBeats_NoXpConversion()
    {
        using var ctx = CreateContext(nameof(AddBeat_FourBeats_NoXpConversion));
        var svc = new CharacterService(ctx);
        var character = await svc.EmbraceCharacterAsync(BuildNewCharacter());

        for (int i = 0; i < 4; i++)
            await svc.AddBeatAsync(character);

        Assert.Equal(4, character.Beats);
        Assert.Equal(0, character.ExperiencePoints);
    }

    [Fact]
    public async Task AddBeat_FifthBeat_ConvertsToOneXp()
    {
        using var ctx = CreateContext(nameof(AddBeat_FifthBeat_ConvertsToOneXp));
        var svc = new CharacterService(ctx);
        var character = await svc.EmbraceCharacterAsync(BuildNewCharacter());

        for (int i = 0; i < 5; i++)
            await svc.AddBeatAsync(character);

        Assert.Equal(0, character.Beats);
        Assert.Equal(1, character.ExperiencePoints);
        Assert.Equal(1, character.TotalExperiencePoints);
    }

    [Fact]
    public async Task RemoveBeat_WhenBeatsIsZero_NoChange()
    {
        using var ctx = CreateContext(nameof(RemoveBeat_WhenBeatsIsZero_NoChange));
        var svc = new CharacterService(ctx);
        var character = await svc.EmbraceCharacterAsync(BuildNewCharacter());

        await svc.RemoveBeatAsync(character);

        Assert.Equal(0, character.Beats);
    }

    [Fact]
    public async Task AddXP_IncrementsCurrentAndTotalXp()
    {
        using var ctx = CreateContext(nameof(AddXP_IncrementsCurrentAndTotalXp));
        var svc = new CharacterService(ctx);
        var character = await svc.EmbraceCharacterAsync(BuildNewCharacter());

        await svc.AddXPAsync(character);

        Assert.Equal(1, character.ExperiencePoints);
        Assert.Equal(1, character.TotalExperiencePoints);
    }

    [Fact]
    public async Task RemoveXP_DecrementsXp()
    {
        using var ctx = CreateContext(nameof(RemoveXP_DecrementsXp));
        var svc = new CharacterService(ctx);
        var character = await svc.EmbraceCharacterAsync(BuildNewCharacter());
        await svc.AddXPAsync(character);

        await svc.RemoveXPAsync(character);

        Assert.Equal(0, character.ExperiencePoints);
    }

    [Fact]
    public async Task RemoveXP_WhenXpIsZero_NoChange()
    {
        using var ctx = CreateContext(nameof(RemoveXP_WhenXpIsZero_NoChange));
        var svc = new CharacterService(ctx);
        var character = await svc.EmbraceCharacterAsync(BuildNewCharacter());

        await svc.RemoveXPAsync(character);

        Assert.Equal(0, character.ExperiencePoints);
    }

    // -----------------------------------------------------------------------
    // CRUD
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetCharacterByIdAsync_ReturnsCharacterForCorrectUser()
    {
        using var ctx = CreateContext(nameof(GetCharacterByIdAsync_ReturnsCharacterForCorrectUser));
        var svc = new CharacterService(ctx);
        var character = await svc.EmbraceCharacterAsync(BuildNewCharacter("owner-1"));

        var fetched = await svc.GetCharacterByIdAsync(character.Id, "owner-1");

        Assert.NotNull(fetched);
        Assert.Equal(character.Id, fetched.Id);
    }

    [Fact]
    public async Task GetCharacterByIdAsync_ReturnsNullForWrongUser()
    {
        using var ctx = CreateContext(nameof(GetCharacterByIdAsync_ReturnsNullForWrongUser));
        var svc = new CharacterService(ctx);
        var character = await svc.EmbraceCharacterAsync(BuildNewCharacter("owner-1"));

        var fetched = await svc.GetCharacterByIdAsync(character.Id, "different-user");

        Assert.Null(fetched);
    }

    [Fact]
    public async Task DeleteCharacterAsync_RemovesFromDatabase()
    {
        using var ctx = CreateContext(nameof(DeleteCharacterAsync_RemovesFromDatabase));
        var svc = new CharacterService(ctx);
        var character = await svc.EmbraceCharacterAsync(BuildNewCharacter());

        await svc.DeleteCharacterAsync(character.Id);

        Assert.Equal(0, await ctx.Characters.CountAsync());
    }
}
