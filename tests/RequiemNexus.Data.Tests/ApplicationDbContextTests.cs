using Microsoft.EntityFrameworkCore;
using RequiemNexus.Data.Models;
using Xunit;
namespace RequiemNexus.Data.Tests;

public class ApplicationDbContextTests
{
    private static ApplicationDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public void DbContext_AllExpectedDbSets_AreAccessible()
    {
        using var ctx = CreateContext(nameof(DbContext_AllExpectedDbSets_AreAccessible));

        Assert.NotNull(ctx.Characters);
        Assert.NotNull(ctx.Clans);
        Assert.NotNull(ctx.Disciplines);
        Assert.NotNull(ctx.DisciplinePowers);
        Assert.NotNull(ctx.Merits);
        Assert.NotNull(ctx.CharacterMerits);
        Assert.NotNull(ctx.CharacterDisciplines);
        Assert.NotNull(ctx.CharacterEquipments);
        Assert.NotNull(ctx.CharacterAspirations);
        Assert.NotNull(ctx.CharacterBanes);
        Assert.NotNull(ctx.Equipment);
        Assert.NotNull(ctx.Campaigns);
    }

    [Fact]
    public async Task SaveCharacter_PersistsToDatabase()
    {
        using var ctx = CreateContext(nameof(SaveCharacter_PersistsToDatabase));

        ctx.Characters.Add(new Character
        {
            ApplicationUserId = "user-1",
            Name = "Lestat",
            MaxHealth = 6,
            CurrentHealth = 6,
            MaxWillpower = 4,
            CurrentWillpower = 4,
            MaxVitae = 10,
            CurrentVitae = 10
        });
        await ctx.SaveChangesAsync();

        var saved = await ctx.Characters.FirstOrDefaultAsync(c => c.Name == "Lestat");
        Assert.NotNull(saved);
        Assert.Equal("user-1", saved.ApplicationUserId);
    }

    [Fact]
    public async Task DeleteCharacter_CascadesAspirations()
    {
        using var ctx = CreateContext(nameof(DeleteCharacter_CascadesAspirations));

        var character = new Character
        {
            ApplicationUserId = "user-cascade",
            Name = "Armand",
            MaxHealth = 6, CurrentHealth = 6,
            MaxWillpower = 4, CurrentWillpower = 4,
            MaxVitae = 10, CurrentVitae = 10
        };
        ctx.Characters.Add(character);
        await ctx.SaveChangesAsync();

        ctx.CharacterAspirations.Add(new CharacterAspiration
        {
            CharacterId = character.Id,
            Description = "Find a childe"
        });
        await ctx.SaveChangesAsync();

        Assert.Equal(1, await ctx.CharacterAspirations.CountAsync());

        ctx.Characters.Remove(character);
        await ctx.SaveChangesAsync();

        Assert.Equal(0, await ctx.CharacterAspirations.CountAsync());
    }

    [Fact]
    public async Task DeleteCharacter_CascadesBanes()
    {
        using var ctx = CreateContext(nameof(DeleteCharacter_CascadesBanes));

        var character = new Character
        {
            ApplicationUserId = "user-bane",
            Name = "Louis",
            MaxHealth = 6, CurrentHealth = 6,
            MaxWillpower = 4, CurrentWillpower = 4,
            MaxVitae = 10, CurrentVitae = 10
        };
        ctx.Characters.Add(character);
        await ctx.SaveChangesAsync();

        ctx.CharacterBanes.Add(new CharacterBane
        {
            CharacterId = character.Id,
            Description = "Sunlight aversion"
        });
        await ctx.SaveChangesAsync();

        Assert.Equal(1, await ctx.CharacterBanes.CountAsync());

        ctx.Characters.Remove(character);
        await ctx.SaveChangesAsync();

        Assert.Equal(0, await ctx.CharacterBanes.CountAsync());
    }
}
