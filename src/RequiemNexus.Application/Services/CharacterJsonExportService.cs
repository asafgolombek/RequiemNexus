using System.Text.Json;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Application.Services;

/// <inheritdoc />
public sealed class CharacterJsonExportService(ICharacterExportCharacterLoader loader) : ICharacterJsonExportService
{
    private readonly ICharacterExportCharacterLoader _loader = loader;
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    /// <inheritdoc />
    public async Task<string> ExportCharacterAsJsonAsync(int characterId, string userId, CancellationToken cancellationToken = default)
    {
        Character? character = await _loader.LoadOwnedCharacterAsync(characterId, userId, cancellationToken);

        if (character == null)
        {
            return "{}";
        }

        return await CharacterExportConcurrency.RunThrottledAsync(
            () => Task.Run(() => ExportCharacterAsJson(character), cancellationToken),
            cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public string ExportCharacterAsJson(Character character)
    {
        var data = new
        {
            character.Id,
            character.Name,
            character.Concept,
            character.Mask,
            character.Dirge,
            character.Touchstone,
            character.Backstory,
            character.Height,
            character.EyeColor,
            character.HairColor,
            clan = character.Clan?.Name,
            character.BloodPotency,
            character.Humanity,
            character.MaxHealth,
            character.CurrentHealth,
            character.MaxWillpower,
            character.CurrentWillpower,
            character.MaxVitae,
            character.CurrentVitae,
            character.ExperiencePoints,
            character.TotalExperiencePoints,
            character.Beats,
            character.Size,
            character.Speed,
            character.Defense,
            character.Armor,
            attributes = character.Attributes.Select(a => new { a.Name, a.Rating }),
            skills = character.Skills.Select(s => new { s.Name, s.Rating }),
            merits = character.Merits.Select(m => new { m.Merit?.Name, m.Specification, m.Rating }),
            disciplines = character.Disciplines.Select(d => new { d.Discipline?.Name, d.Rating }),
            aspirations = character.Aspirations.Select(a => new { a.Description }),
            banes = character.Banes.Select(b => new { b.Description }),
        };

        return JsonSerializer.Serialize(data, _jsonOptions);
    }

    /// <inheritdoc />
    public Task<string> ExportCharacterAsJsonAsync(Character character, CancellationToken cancellationToken = default) =>
        Task.Run(() => ExportCharacterAsJson(character), cancellationToken);
}
