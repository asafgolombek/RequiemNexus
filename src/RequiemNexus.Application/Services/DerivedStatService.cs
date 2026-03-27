using RequiemNexus.Application.Contracts;
using RequiemNexus.Data.Models;
using RequiemNexus.Domain.Enums;
using RequiemNexus.Domain.Models;
using RequiemNexus.Domain.Services;

namespace RequiemNexus.Application.Services;

/// <summary>
/// Computes derived stats with passive modifiers applied on demand.
/// </summary>
public class DerivedStatService(IModifierService modifierService) : IDerivedStatService
{
    /// <inheritdoc />
    public async Task<int> GetEffectiveDefenseAsync(Character character)
    {
        int baseDefense = Math.Min(
                character.GetAttributeRating(AttributeId.Wits),
                character.GetAttributeRating(AttributeId.Dexterity))
            + character.GetSkillRating(SkillId.Athletics);

        var modifiers = await modifierService.GetModifiersForCharacterAsync(character.Id);
        int delta = modifiers
            .Where(m => m.Target == ModifierTarget.Defense && m.ModifierType != ModifierType.RuleBreaking)
            .Sum(m => m.Value);

        return Math.Max(0, baseDefense + delta);
    }

    /// <inheritdoc />
    public async Task<int> GetEffectiveSpeedAsync(Character character)
    {
        int baseSpeed = character.GetAttributeRating(AttributeId.Strength)
            + character.GetAttributeRating(AttributeId.Dexterity)
            + character.Size;

        var modifiers = await modifierService.GetModifiersForCharacterAsync(character.Id);
        int delta = modifiers
            .Where(m => m.Target == ModifierTarget.Speed && m.ModifierType != ModifierType.RuleBreaking)
            .Sum(m => m.Value);

        return Math.Max(0, baseSpeed + delta);
    }

    /// <inheritdoc />
    public async Task<int> GetEffectiveMaxHealthAsync(Character character)
    {
        int baseHealth = character.Size + character.GetAttributeRating(AttributeId.Stamina);

        var modifiers = await modifierService.GetModifiersForCharacterAsync(character.Id);
        int delta = modifiers
            .Where(m => m.Target == ModifierTarget.MaxHealth && m.ModifierType != ModifierType.RuleBreaking)
            .Sum(m => m.Value);

        return Math.Max(0, baseHealth + delta);
    }
}
