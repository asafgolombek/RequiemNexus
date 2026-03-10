using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Application.Services;

/// <summary>
/// Application service for managing homebrew content — custom Disciplines, Merits, and
/// Clans/Bloodlines scoped to a user. Provides JSON pack export and import.
/// </summary>
public class HomebrewService(
    ApplicationDbContext dbContext,
    ILogger<HomebrewService> logger) : IHomebrewService
{
    private readonly ApplicationDbContext _dbContext = dbContext;
    private readonly ILogger<HomebrewService> _logger = logger;

    /// <inheritdoc />
    public async Task<List<Discipline>> GetHomebrewDisciplinesAsync(string userId)
    {
        return await _dbContext.Disciplines
            .Where(d => d.IsHomebrew && d.HombrewAuthorUserId == userId)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<Discipline> CreateHomebrewDisciplineAsync(string name, string description, string userId)
    {
        Discipline discipline = new()
        {
            Name = name,
            Description = description,
            IsHomebrew = true,
            HombrewAuthorUserId = userId,
        };

        _dbContext.Disciplines.Add(discipline);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Homebrew discipline '{Name}' (Id={DisciplineId}) created by user {UserId}",
            discipline.Name,
            discipline.Id,
            userId);

        return discipline;
    }

    /// <inheritdoc />
    public async Task DeleteHomebrewDisciplineAsync(int disciplineId, string userId)
    {
        Discipline discipline = await _dbContext.Disciplines.FindAsync(disciplineId)
            ?? throw new InvalidOperationException($"Discipline {disciplineId} not found.");

        if (!discipline.IsHomebrew || discipline.HombrewAuthorUserId != userId)
        {
            throw new UnauthorizedAccessException("Only the homebrew author may delete this discipline.");
        }

        _dbContext.Disciplines.Remove(discipline);
        await _dbContext.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<List<Merit>> GetHomebrewMeritsAsync(string userId)
    {
        return await _dbContext.Merits
            .Where(m => m.IsHomebrew && m.HombrewAuthorUserId == userId)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<Merit> CreateHomebrewMeritAsync(string name, string description, string validRatings, bool requiresSpecification, string userId)
    {
        Merit merit = new()
        {
            Name = name,
            Description = description,
            ValidRatings = validRatings,
            RequiresSpecification = requiresSpecification,
            IsHomebrew = true,
            HombrewAuthorUserId = userId,
        };

        _dbContext.Merits.Add(merit);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Homebrew merit '{Name}' (Id={MeritId}) created by user {UserId}",
            merit.Name,
            merit.Id,
            userId);

        return merit;
    }

    /// <inheritdoc />
    public async Task DeleteHomebrewMeritAsync(int meritId, string userId)
    {
        Merit merit = await _dbContext.Merits.FindAsync(meritId)
            ?? throw new InvalidOperationException($"Merit {meritId} not found.");

        if (!merit.IsHomebrew || merit.HombrewAuthorUserId != userId)
        {
            throw new UnauthorizedAccessException("Only the homebrew author may delete this merit.");
        }

        _dbContext.Merits.Remove(merit);
        await _dbContext.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<List<Clan>> GetHomebrewClansAsync(string userId)
    {
        return await _dbContext.Clans
            .Where(c => c.IsHomebrew && c.HombrewAuthorUserId == userId)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<Clan> CreateHomebrewClanAsync(string name, string description, string userId)
    {
        Clan clan = new()
        {
            Name = name,
            Description = description,
            IsHomebrew = true,
            HombrewAuthorUserId = userId,
        };

        _dbContext.Clans.Add(clan);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Homebrew clan/bloodline '{Name}' (Id={ClanId}) created by user {UserId}",
            clan.Name,
            clan.Id,
            userId);

        return clan;
    }

    /// <inheritdoc />
    public async Task DeleteHomebrewClanAsync(int clanId, string userId)
    {
        Clan clan = await _dbContext.Clans.FindAsync(clanId)
            ?? throw new InvalidOperationException($"Clan {clanId} not found.");

        if (!clan.IsHomebrew || clan.HombrewAuthorUserId != userId)
        {
            throw new UnauthorizedAccessException("Only the homebrew author may delete this clan.");
        }

        _dbContext.Clans.Remove(clan);
        await _dbContext.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<string> ExportHomebrewPackAsync(string userId)
    {
        List<Discipline> disciplines = await GetHomebrewDisciplinesAsync(userId);
        List<Merit> merits = await GetHomebrewMeritsAsync(userId);
        List<Clan> clans = await GetHomebrewClansAsync(userId);

        HomebrewPack pack = new()
        {
            Disciplines = disciplines.Select(d => new HomebrewDisciplineDto(d.Name, d.Description)).ToList(),
            Merits = merits.Select(m => new HomebrewMeritDto(m.Name, m.Description, m.ValidRatings, m.RequiresSpecification)).ToList(),
            Clans = clans.Select(c => new HomebrewClanDto(c.Name, c.Description)).ToList(),
        };

        return JsonSerializer.Serialize(pack, new JsonSerializerOptions { WriteIndented = true });
    }

    /// <inheritdoc />
    public async Task<int> ImportHomebrewPackAsync(string json, string userId)
    {
        HomebrewPack pack = JsonSerializer.Deserialize<HomebrewPack>(json)
            ?? throw new InvalidOperationException("Invalid homebrew pack JSON.");

        int count = 0;

        foreach (HomebrewDisciplineDto dto in pack.Disciplines)
        {
            await CreateHomebrewDisciplineAsync(dto.Name, dto.Description, userId);
            count++;
        }

        foreach (HomebrewMeritDto dto in pack.Merits)
        {
            await CreateHomebrewMeritAsync(dto.Name, dto.Description, dto.ValidRatings, dto.RequiresSpecification, userId);
            count++;
        }

        foreach (HomebrewClanDto dto in pack.Clans)
        {
            await CreateHomebrewClanAsync(dto.Name, dto.Description, userId);
            count++;
        }

        _logger.LogInformation(
            "Homebrew pack imported by user {UserId}: {Count} items",
            userId,
            count);

        return count;
    }

    private sealed record HomebrewPack
    {
        public List<HomebrewDisciplineDto> Disciplines { get; init; } = [];

        public List<HomebrewMeritDto> Merits { get; init; } = [];

        public List<HomebrewClanDto> Clans { get; init; } = [];
    }

    private sealed record HomebrewDisciplineDto(string Name, string Description);

    private sealed record HomebrewMeritDto(string Name, string Description, string ValidRatings, bool RequiresSpecification);

    private sealed record HomebrewClanDto(string Name, string Description);
}
