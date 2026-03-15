using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Data.RealTime;

namespace RequiemNexus.Application.Services;

/// <summary>
/// Implementation of PublicRollService for persistent dice roll sharing.
/// </summary>
public class PublicRollService(ApplicationDbContext db) : IPublicRollService
{
    private const string _alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    private readonly ApplicationDbContext _db = db;

    /// <inheritdoc />
    public async Task<string> ShareRollAsync(string userId, int? chronicleId, string poolDescription, DiceRollResultDto roll)
    {
        string slug = GenerateSlug();

        // Ensure slug uniqueness (rare collision possibility)
        while (await _db.PublicRolls.AnyAsync(r => r.Slug == slug))
        {
            slug = GenerateSlug();
        }

        PublicRoll entity = new()
        {
            Slug = slug,
            RolledByUserId = userId,
            CampaignId = chronicleId,
            PoolDescription = poolDescription,
            ResultJson = JsonSerializer.Serialize(roll),
            CreatedAt = DateTimeOffset.UtcNow,
        };

        _db.PublicRolls.Add(entity);
        await _db.SaveChangesAsync();

        return slug;
    }

    /// <inheritdoc />
    public async Task<PublicRoll?> GetRollBySlugAsync(string slug)
    {
        return await _db.PublicRolls
            .Include(r => r.RolledByUser)
            .Include(r => r.Campaign)
            .FirstOrDefaultAsync(r => r.Slug == slug);
    }

    private static string GenerateSlug()
    {
        char[] chars = new char[8];
        for (int i = 0; i < chars.Length; i++)
        {
            chars[i] = _alphabet[RandomNumberGenerator.GetInt32(_alphabet.Length)];
        }

        return new string(chars);
    }
}
