using RequiemNexus.Application.Contracts;
using RequiemNexus.Application.DTOs;
using RequiemNexus.Data.Models;
using RequiemNexus.Domain.Enums;

namespace RequiemNexus.Application.Services;

/// <summary>
/// Theban Sorcery activation rules (Humanity floor vs. miracle rating, no bonus Vitae).
/// </summary>
public sealed class ThebanActivationStrategy : IRiteActivationStrategy
{
    /// <inheritdoc />
    public SorceryType Tradition => SorceryType.Theban;

    /// <inheritdoc />
    public int GetTraditionDisciplineDots(Character character) => character.GetDisciplineRating("Theban Sorcery");

    /// <inheritdoc />
    public void ValidateTraditionRules(Character character, SorceryRiteDefinition def)
    {
        ArgumentNullException.ThrowIfNull(character);
        ArgumentNullException.ThrowIfNull(def);
        if (character.Humanity < def.Level)
        {
            throw new InvalidOperationException(
                $"Theban Sorcery requires Humanity {def.Level} or higher to cast this miracle (character has Humanity {character.Humanity}).");
        }
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
        return false;
    }
}
