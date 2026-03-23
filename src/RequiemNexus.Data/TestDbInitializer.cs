using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Data;

/// <summary>
/// Seeds repeatable, deterministic data for integration and E2E tests.
/// </summary>
public static class TestDbInitializer
{
    public const string TestUserEmail = "e2etest@requiemnexus.local";
#pragma warning disable S2068 // "password" detected here, but this is a deterministic test-only credential
    public const string TestUserPassword = "test"; // Used in E2E tests
#pragma warning restore S2068

    public static async Task InitializeAsync(ApplicationDbContext context)
    {
        // Define a test user
        var user = await context.Users.FirstOrDefaultAsync(u => u.Email == TestUserEmail);
        if (user == null)
        {
            var hasher = new PasswordHasher<ApplicationUser>();
            var newUser = new ApplicationUser
            {
                Id = Guid.NewGuid().ToString(),
                UserName = TestUserEmail,
                NormalizedUserName = TestUserEmail.ToUpperInvariant(),
                Email = TestUserEmail,
                NormalizedEmail = TestUserEmail.ToUpperInvariant(),
                EmailConfirmed = true,
                SecurityStamp = Guid.NewGuid().ToString(),
            };

            newUser.PasswordHash = hasher.HashPassword(newUser, TestUserPassword);

            await context.Users.AddAsync(newUser);
            try
            {
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DB SEED ERROR: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"INNER EXCEPTION: {ex.InnerException.Message}");
                }

                throw;
            }
        }

        await SeedPhase12RelationshipSamplesAsync(context);
    }

    /// <summary>
    /// Idempotent sample sire link, Blood Bond, and Ghoul for Phase 12 integration and manual testing (development only).
    /// </summary>
    private static async Task SeedPhase12RelationshipSamplesAsync(ApplicationDbContext context)
    {
        const string seedCampaignName = "__Phase12_IntegrationSeed__";
        if (await context.Campaigns.AnyAsync(c => c.Name == seedCampaignName))
        {
            return;
        }

        ApplicationUser? user = await context.Users.FirstOrDefaultAsync(u => u.Email == TestUserEmail);
        if (user == null)
        {
            return;
        }

        Clan? clan = await context.Clans.OrderBy(c => c.Id).FirstOrDefaultAsync();
        if (clan == null)
        {
            return;
        }

        var campaign = new Campaign
        {
            Name = seedCampaignName,
            StoryTellerId = user.Id,
            Description = "Phase 12 integration — sample lineage, bond, and ghoul",
        };
        await context.Campaigns.AddAsync(campaign);
        await context.SaveChangesAsync();

        var regnant = new Character
        {
            ApplicationUserId = user.Id,
            Name = "Phase12 Regnant",
            ClanId = clan.Id,
            CampaignId = campaign.Id,
        };
        var thrall = new Character
        {
            ApplicationUserId = user.Id,
            Name = "Phase12 Thrall",
            ClanId = clan.Id,
            CampaignId = campaign.Id,
        };
        await context.Characters.AddRangeAsync(regnant, thrall);
        await context.SaveChangesAsync();

        thrall.SireCharacterId = regnant.Id;
        await context.SaveChangesAsync();

        await context.BloodBonds.AddAsync(new BloodBond
        {
            ChronicleId = campaign.Id,
            ThrallCharacterId = thrall.Id,
            RegnantCharacterId = regnant.Id,
            RegnantKey = $"c:{regnant.Id}",
            Stage = 1,
            LastFedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
        });

        await context.Ghouls.AddAsync(new Ghoul
        {
            ChronicleId = campaign.Id,
            Name = "Phase12 Sample Ghoul",
            RegnantCharacterId = regnant.Id,
            LastFedAt = DateTime.UtcNow,
            VitaeInSystem = 1,
            CreatedAt = DateTime.UtcNow,
        });

        await context.SaveChangesAsync();
    }
}
