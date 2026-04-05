using RequiemNexus.Application.Contracts;
using RequiemNexus.Application.DTOs;
using RequiemNexus.Data.Models;
using RequiemNexus.Domain.Enums;

namespace RequiemNexus.Application.Services;

/// <summary>
/// Crúac-specific activation rules (bonus Vitae, doubled Blood Sympathy ritual dice).
/// </summary>
public sealed class CruacActivationStrategy : IRiteActivationStrategy
{
    /// <inheritdoc />
    public SorceryType Tradition => SorceryType.Cruac;

    /// <inheritdoc />
    public int GetTraditionDisciplineDots(Character character) => character.GetDisciplineRating("Crúac");

    /// <inheritdoc />
    public void ValidateTraditionRules(Character character, SorceryRiteDefinition def)
    {
        ArgumentNullException.ThrowIfNull(character);
        ArgumentNullException.ThrowIfNull(def);
    }

    /// <inheritdoc />
    public int ResolveExtraVitaeSpend(BeginRiteActivationRequest request) => Math.Clamp(request.ExtraVitae, 0, 5);

    /// <inheritdoc />
    public int AdjustRitualBloodSympathyBonus(int baseBonus) => baseBonus * 2;

    /// <inheritdoc />
    public bool ShouldRaiseDegenerationCheck(Character character)
    {
        ArgumentNullException.ThrowIfNull(character);
        return false;
    }
}
