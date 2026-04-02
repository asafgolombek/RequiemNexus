using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Data.Seeding;
using RequiemNexus.Web.Helpers;
using Xunit;

namespace RequiemNexus.Data.Tests;

/// <summary>
/// Integration tests for DbInitializer — verifies Bloodline and Devotion definitions are seeded correctly.
/// </summary>
public class DbInitializerTests
{
    private static ServiceProvider CreateServiceProvider(string dbName)
    {
        var services = new ServiceCollection();

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(dbName));

        services.AddLogging();

        services.AddIdentityCore<ApplicationUser>()
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>();

        services.AddRequiemDataSeeders();

        return services.BuildServiceProvider();
    }

    private static async Task RunDbInitializeAsync(IServiceScope scope)
    {
        ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        RoleManager<IdentityRole> roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        IEnumerable<ISeeder> seeders = scope.ServiceProvider.GetServices<ISeeder>();
        await DbInitializer.InitializeAsync(context, roleManager, NullLogger.Instance, seeders, runMigrations: false);
    }

    [Fact]
    public async Task InitializeAsync_SeedsBloodlineDefinitions()
    {
        var provider = CreateServiceProvider(nameof(InitializeAsync_SeedsBloodlineDefinitions));
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await RunDbInitializeAsync(scope);

        var bloodlines = await context.BloodlineDefinitions
            .Include(b => b.AllowedParentClans)
            .ToListAsync();

        Assert.True(bloodlines.Count >= 8, $"Expected at least 8 bloodlines, got {bloodlines.Count}");

        // Representative bloodlines from SeedSource/bloodlines.json (set evolves with content packs).
        var expectedNames = new[]
        {
            "Ankou", "Icelus", "Lidérc", "Nosoi", "Vardyvle", "Vilseduire", "Sangiovanni", "Burakumin",
        };
        foreach (var name in expectedNames)
        {
            var b = bloodlines.FirstOrDefault(x => x.Name == name);
            Assert.NotNull(b);
            Assert.True(b.FourthDisciplineId > 0);
            Assert.False(string.IsNullOrEmpty(b.BaneOverride));
            Assert.True(b.PrerequisiteBloodPotency >= 2);
        }

        var icelus = bloodlines.First(b => b.Name == "Icelus");
        Assert.Single(icelus.AllowedParentClans);

        var vilseduire = bloodlines.First(b => b.Name == "Vilseduire");
        Assert.Single(vilseduire.AllowedParentClans);
    }

    [Fact]
    public async Task InitializeAsync_SeedsDevotionDefinitions()
    {
        var provider = CreateServiceProvider(nameof(InitializeAsync_SeedsDevotionDefinitions));
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await RunDbInitializeAsync(scope);

        var devotions = await context.DevotionDefinitions
            .Include(d => d.Prerequisites)
            .ToListAsync();

        Assert.True(devotions.Count >= 4, $"Expected at least 4 devotions, got {devotions.Count}");

        var expectedNames = new[] { "Body of Will", "Best Served Cold", "Blood Scenting", "Bones of the Mountain" };
        foreach (var name in expectedNames)
        {
            var d = devotions.FirstOrDefault(x => x.Name == name);
            Assert.NotNull(d);
            Assert.False(string.IsNullOrEmpty(d.PoolDefinitionJson));
            Assert.True(d.XpCost > 0);
        }

        var bonesOfMountain = devotions.First(d => d.Name == "Bones of the Mountain");
        Assert.True(bonesOfMountain.Prerequisites.Count >= 3);
    }

    [Fact]
    public async Task InitializeAsync_BloodlinesDependOnClansAndDisciplines()
    {
        var provider = CreateServiceProvider(nameof(InitializeAsync_BloodlinesDependOnClansAndDisciplines));
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await RunDbInitializeAsync(scope);

        var clanIds = await context.Clans.Select(c => c.Id).ToHashSetAsync();
        var disciplineIds = await context.Disciplines.Select(d => d.Id).ToHashSetAsync();

        var bloodlines = await context.BloodlineDefinitions
            .Include(b => b.AllowedParentClans)
            .ToListAsync();

        foreach (var b in bloodlines)
        {
            Assert.True(disciplineIds.Contains(b.FourthDisciplineId), $"Bloodline {b.Name} has invalid FourthDisciplineId {b.FourthDisciplineId}");
            foreach (var bc in b.AllowedParentClans)
            {
                Assert.True(clanIds.Contains(bc.ClanId), $"Bloodline {b.Name} has invalid ClanId {bc.ClanId}");
            }
        }
    }

    [Fact]
    public async Task InitializeAsync_Idempotent_DoesNotDuplicateOnSecondRun()
    {
        var provider = CreateServiceProvider(nameof(InitializeAsync_Idempotent_DoesNotDuplicateOnSecondRun));
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await RunDbInitializeAsync(scope);
        var bloodlineCount1 = await context.BloodlineDefinitions.CountAsync();
        var devotionCount1 = await context.DevotionDefinitions.CountAsync();

        await RunDbInitializeAsync(scope);
        var bloodlineCount2 = await context.BloodlineDefinitions.CountAsync();
        var devotionCount2 = await context.DevotionDefinitions.CountAsync();

        Assert.Equal(bloodlineCount1, bloodlineCount2);
        Assert.Equal(devotionCount1, devotionCount2);
    }

    [Fact]
    public async Task InitializeAsync_SecondRun_DoesNotRemoveCharacterMerits()
    {
        var provider = CreateServiceProvider(nameof(InitializeAsync_SecondRun_DoesNotRemoveCharacterMerits));
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await RunDbInitializeAsync(scope);

        Merit? anyOfficialMerit = await context.Merits.AsNoTracking().FirstOrDefaultAsync(m => !m.IsHomebrew);
        Assert.NotNull(anyOfficialMerit);

        var character = new Character
        {
            ApplicationUserId = "player-1",
            Name = "Kindred With Merit",
            Size = 5,
        };
        CharacterTraitHelper.SeedAttributes(character);
        CharacterTraitHelper.SeedSkills(character);
        character.Attributes.First(a => a.Name == "Stamina").Rating = 2;
        character.Attributes.First(a => a.Name == "Resolve").Rating = 2;
        character.Attributes.First(a => a.Name == "Composure").Rating = 2;

        character.Merits.Add(new CharacterMerit
        {
            MeritId = anyOfficialMerit.Id,
            Rating = 1,
            Specification = string.Empty,
        });
        context.Characters.Add(character);
        await context.SaveChangesAsync();

        int meritLinksBefore = await context.CharacterMerits.CountAsync(cm => cm.CharacterId == character.Id);
        Assert.Equal(1, meritLinksBefore);

        await RunDbInitializeAsync(scope);

        int meritLinksAfter = await context.CharacterMerits.CountAsync(cm => cm.CharacterId == character.Id);
        Assert.Equal(1, meritLinksAfter);
    }
}
