using System.Linq;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Data.Models.Enums;
using RequiemNexus.Web.Helpers;

namespace RequiemNexus.E2E.Tests.Fixtures;

/// <summary>
/// Idempotent PostgreSQL seed for Playwright flows: confirmed user, Storyteller role, campaign, and a playable character.
/// </summary>
public static class E2eTestDataSeed
{
    /// <summary>Login email for seeded E2E identity.</summary>
    public const string PlayerEmail = "e2e-phase13-player@test.requiem";

    /// <summary>Login password for <see cref="PlayerEmail"/>.</summary>
    public const string Password = "E2e-requiem-Phase13!";

    private const string _characterName = "E2E Kindred";

    /// <summary>
    /// Ensures the E2E user, roles, campaign, and character exist. Safe to call on every fixture init.
    /// </summary>
    /// <param name="services">Root service provider (host services).</param>
    /// <returns>Campaign and character identifiers for navigation.</returns>
    public static async Task<(int CampaignId, int CharacterId)> EnsurePlayerCampaignAndCharacterAsync(IServiceProvider services)
    {
        using IServiceScope scope = services.CreateScope();
        ApplicationDbContext db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        UserManager<ApplicationUser> users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        ApplicationUser? user = await users.FindByEmailAsync(PlayerEmail);
        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = PlayerEmail,
                Email = PlayerEmail,
                EmailConfirmed = true,
            };
            IdentityResult created = await users.CreateAsync(user, Password);
            if (!created.Succeeded)
            {
                throw new InvalidOperationException(
                    "Failed to create E2E user: " + string.Join("; ", created.Errors.Select(e => e.Description)));
            }

            user = await users.FindByEmailAsync(PlayerEmail)
                ?? throw new InvalidOperationException("E2E user missing after creation.");
        }

        foreach (string role in new[] { "Player", "Storyteller" })
        {
            if (!await users.IsInRoleAsync(user, role))
            {
                await users.AddToRoleAsync(user, role);
            }
        }

        Character? existingChar = await db.Characters.AsNoTracking()
            .FirstOrDefaultAsync(c => c.ApplicationUserId == user.Id && c.Name == _characterName);
        if (existingChar != null)
        {
            return (existingChar.CampaignId ?? throw new InvalidOperationException("E2E character has no campaign."), existingChar.Id);
        }

        Clan clan = await db.Clans.AsNoTracking().FirstAsync();
        Campaign campaign = new()
        {
            Name = "E2E Test Chronicle",
            StoryTellerId = user.Id,
            Description = string.Empty,
            IsActive = true,
        };
        db.Campaigns.Add(campaign);
        await db.SaveChangesAsync();

        Character character = new()
        {
            ApplicationUserId = user.Id,
            Name = _characterName,
            Concept = "E2E",
            ClanId = clan.Id,
            CampaignId = campaign.Id,
            CreatureType = CreatureType.Vampire,
            MaxHealth = 7,
            CurrentHealth = 7,
            MaxWillpower = 5,
            CurrentWillpower = 5,
            MaxVitae = 10,
            CurrentVitae = 5,
            Humanity = 7,
            BloodPotency = 1,
        };
        CharacterTraitHelper.SeedAttributes(character);
        CharacterTraitHelper.SeedSkills(character);
        db.Characters.Add(character);
        await db.SaveChangesAsync();

        return (campaign.Id, character.Id);
    }
}
