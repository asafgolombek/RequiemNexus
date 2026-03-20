using Microsoft.EntityFrameworkCore;
using Moq;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Application.Services;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Data.Models.Enums;
using RequiemNexus.Domain;
using RequiemNexus.Domain.Enums;
using RequiemNexus.Web.Helpers;
using Xunit;

namespace RequiemNexus.Application.Tests;

/// <summary>
/// Integration tests for CharacterMeritService merit prerequisite validation.
/// </summary>
public class CharacterMeritServiceTests
{
    private static ApplicationDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new ApplicationDbContext(options);
    }

    private static CharacterMeritService CreateService(ApplicationDbContext ctx)
    {
        return new CharacterMeritService(ctx, new Mock<IBeatLedgerService>().Object);
    }

    private static Character BuildCharacter(int id = 1)
    {
        var c = new Character
        {
            Id = id,
            ApplicationUserId = "user-1",
            Name = "Test Vampire",
            ExperiencePoints = 20,
            ClanId = 1,
            CreatureType = CreatureType.Vampire,
        };
        CharacterTraitHelper.SeedAttributes(c);
        CharacterTraitHelper.SeedSkills(c);
        return c;
    }

    [Fact]
    public async Task AddMeritAsync_PrerequisitesNotMet_Throws()
    {
        using var ctx = CreateContext(nameof(AddMeritAsync_PrerequisitesNotMet_Throws));
        var merit = new Merit { Id = 1, Name = "Trained Observer", ValidRatings = "\u2022 or \u2022\u2022\u2022" };
        merit.Prerequisites.Add(new MeritPrerequisite
        {
            MeritId = 1,
            PrerequisiteType = MeritPrerequisiteType.Attribute,
            ReferenceId = (int)AttributeId.Wits,
            MinimumRating = 3,
            OrGroupId = 1,
        });
        ctx.Merits.Add(merit);

        var character = BuildCharacter();
        CharacterTraitHelper.SetTraitValue(character, "Wits", 2);
        ctx.Characters.Add(character);
        await ctx.SaveChangesAsync();

        var service = CreateService(ctx);
        var loadedChar = await ctx.Characters
            .Include(c => c.Attributes)
            .Include(c => c.Skills)
            .Include(c => c.Merits).ThenInclude(m => m.Merit)
            .Include(c => c.Disciplines)
            .FirstAsync(c => c.Id == character.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.AddMeritAsync(loadedChar, 1, null, 1, 1));
    }

    [Fact]
    public async Task AddMeritAsync_PrerequisitesMet_Succeeds()
    {
        using var ctx = CreateContext(nameof(AddMeritAsync_PrerequisitesMet_Succeeds));
        var merit = new Merit { Id = 1, Name = "Trained Observer", ValidRatings = "\u2022 or \u2022\u2022\u2022" };
        merit.Prerequisites.Add(new MeritPrerequisite
        {
            MeritId = 1,
            PrerequisiteType = MeritPrerequisiteType.Attribute,
            ReferenceId = (int)AttributeId.Wits,
            MinimumRating = 3,
            OrGroupId = 1,
        });
        ctx.Merits.Add(merit);

        var character = BuildCharacter();
        CharacterTraitHelper.SetTraitValue(character, "Wits", 3);
        ctx.Characters.Add(character);
        await ctx.SaveChangesAsync();

        var service = CreateService(ctx);
        var loadedChar = await ctx.Characters
            .Include(c => c.Attributes)
            .Include(c => c.Skills)
            .Include(c => c.Merits).ThenInclude(m => m.Merit)
            .Include(c => c.Disciplines)
            .FirstAsync(c => c.Id == character.Id);

        var result = await service.AddMeritAsync(loadedChar, 1, null, 1, 1);

        Assert.NotNull(result);
        Assert.Equal(1, result.MeritId);
        Assert.Equal(19, loadedChar.ExperiencePoints);
    }

    [Fact]
    public async Task GetAvailableMeritsAsync_FiltersByPrerequisites()
    {
        using var ctx = CreateContext(nameof(GetAvailableMeritsAsync_FiltersByPrerequisites));
        var meritWithPrereq = new Merit { Id = 1, Name = "Trained Observer", ValidRatings = "\u2022" };
        meritWithPrereq.Prerequisites.Add(new MeritPrerequisite
        {
            MeritId = 1,
            PrerequisiteType = MeritPrerequisiteType.Attribute,
            ReferenceId = (int)AttributeId.Wits,
            MinimumRating = 3,
            OrGroupId = 1,
        });
        var meritNoPrereq = new Merit { Id = 2, Name = "Acute Senses", ValidRatings = "\u2022" };
        ctx.Merits.AddRange(meritWithPrereq, meritNoPrereq);

        var character = BuildCharacter();
        CharacterTraitHelper.SetTraitValue(character, "Wits", 2);
        ctx.Characters.Add(character);
        await ctx.SaveChangesAsync();

        var service = CreateService(ctx);
        var loadedChar = await ctx.Characters
            .Include(c => c.Attributes)
            .Include(c => c.Skills)
            .Include(c => c.Merits).ThenInclude(m => m.Merit)
            .Include(c => c.Disciplines)
            .AsNoTracking()
            .FirstAsync(c => c.Id == character.Id);

        var available = await service.GetAvailableMeritsAsync(loadedChar);

        Assert.Single(available);
        Assert.Equal(2, available[0].Id);
        Assert.Equal("Acute Senses", available[0].Name);
    }
}
