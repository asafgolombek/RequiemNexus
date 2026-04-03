using RequiemNexus.Application.Contracts;
using RequiemNexus.Application.DTOs;
using RequiemNexus.Data.Models;
using RequiemNexus.Domain.Enums;

namespace RequiemNexus.Application.Services;

/// <summary>
/// Necromancy activation rules (no bonus Vitae; degeneration check at Humanity 7+).
/// </summary>
public sealed class NecromancyActivationStrategy : IRiteActivationStrategy
{
    /// <inheritdoc />
    public SorceryType Tradition => SorceryType.Necromancy;

    /// <inheritdoc />
    public int GetTraditionDisciplineDots(Character character) => character.GetDisciplineRating("Necromancy");

    /// <inheritdoc />
    public void ValidateTraditionRules(Character character, SorceryRiteDefinition def)
    {
        ArgumentNullException.ThrowIfNull(character);
        ArgumentNullException.ThrowIfNull(def);
    }

    /// <inheritdoc />
    public int ResolveExtraVitaeSpend(BeginRiteActivationRequest request)
    {
        if (request.ExtraVitae != 0)
        {
            throw new InvalidOperationException("Extra Vitae may only be spent on Crúac rituals.");
        }

        return 0;
    }

    /// <inheritdoc />
    public int AdjustRitualBloodSympathyBonus(int baseBonus) => baseBonus;

    /// <inheritdoc />
    public bool ShouldRaiseDegenerationCheck(Character character)
    {
        ArgumentNullException.ThrowIfNull(character);
        return character.Humanity >= 7;
    }
}
