using System.Text.Json;
using Microsoft.Extensions.Logging;
using RequiemNexus.Application.Contracts;

namespace RequiemNexus.Application.Services;

/// <summary>
/// Orchestrates JSON pack export and import across all homebrew entity types.
/// </summary>
public class HomebrewPackService(
    IHomebrewDisciplineService disciplineService,
    IHomebrewMeritService meritService,
    IHomebrewClanService clanService,
    ILogger<HomebrewPackService> logger) : IHomebrewPackService
{
    private readonly IHomebrewDisciplineService _disciplineService = disciplineService;
    private readonly IHomebrewMeritService _meritService = meritService;
    private readonly IHomebrewClanService _clanService = clanService;
    private readonly ILogger<HomebrewPackService> _logger = logger;

    /// <inheritdoc />
    public async Task<string> ExportHomebrewPackAsync(string userId)
    {
        var disciplines = await _disciplineService.GetHomebrewDisciplinesAsync(userId);
        var merits = await _meritService.GetHomebrewMeritsAsync(userId);
        var clans = await _clanService.GetHomebrewClansAsync(userId);

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
            await _disciplineService.CreateHomebrewDisciplineAsync(dto.Name, dto.Description, userId);
            count++;
        }

        foreach (HomebrewMeritDto dto in pack.Merits)
        {
            await _meritService.CreateHomebrewMeritAsync(dto.Name, dto.Description, dto.ValidRatings, dto.RequiresSpecification, userId);
            count++;
        }

        foreach (HomebrewClanDto dto in pack.Clans)
        {
            await _clanService.CreateHomebrewClanAsync(dto.Name, dto.Description, userId);
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
