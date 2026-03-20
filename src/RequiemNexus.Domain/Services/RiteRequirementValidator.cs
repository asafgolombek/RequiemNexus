using System.Text.Json;
using System.Text.Json.Serialization;
using RequiemNexus.Domain.Enums;
using RequiemNexus.Domain.Models;

namespace RequiemNexus.Domain.Services;

/// <summary>
/// Parses and validates structured rite requirements (Phase 9.5). Pure logic; no I/O.
/// </summary>
public static class RiteRequirementValidator
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>
    /// Parses <paramref name="requirementsJson"/> into a requirement list. Empty or null JSON yields an empty list.
    /// </summary>
    /// <param name="requirementsJson">JSON array of <see cref="RiteRequirement"/>.</param>
    /// <returns>Failure when JSON is invalid; success with list (possibly empty).</returns>
    public static Result<IReadOnlyList<RiteRequirement>> ParseRequirements(string? requirementsJson)
    {
        if (string.IsNullOrWhiteSpace(requirementsJson))
        {
            return Result<IReadOnlyList<RiteRequirement>>.Success([]);
        }

        try
        {
            var list = JsonSerializer.Deserialize<List<RiteRequirement>>(requirementsJson, _jsonOptions);
            if (list == null)
            {
                return Result<IReadOnlyList<RiteRequirement>>.Success([]);
            }

            foreach (RiteRequirement r in list)
            {
                if (r.Value < 0)
                {
                    return Result<IReadOnlyList<RiteRequirement>>.Failure("Rite requirement Value cannot be negative.");
                }
            }

            return Result<IReadOnlyList<RiteRequirement>>.Success(list);
        }
        catch (JsonException ex)
        {
            return Result<IReadOnlyList<RiteRequirement>>.Failure($"Invalid rite requirements JSON: {ex.Message}");
        }
    }

    /// <summary>
    /// Ensures narrative sacrifice types are acknowledged by the player.
    /// </summary>
    public static Result<bool> ValidateAcknowledgments(
        IReadOnlyList<RiteRequirement> requirements,
        RiteActivationAcknowledgment acknowledgment)
    {
        foreach (RiteRequirement r in requirements)
        {
            switch (r.Type)
            {
                case SacrificeType.PhysicalSacrament:
                    if (!acknowledgment.AcknowledgePhysicalSacrament)
                    {
                        return Result<bool>.Failure("This rite requires acknowledgment of the physical sacrament.");
                    }

                    break;
                case SacrificeType.Heart:
                    if (!acknowledgment.AcknowledgeHeart)
                    {
                        return Result<bool>.Failure("This rite requires acknowledgment of the heart (or equivalent) sacrifice.");
                    }

                    break;
                case SacrificeType.MaterialOffering:
                    if (!acknowledgment.AcknowledgeMaterialOffering)
                    {
                        return Result<bool>.Failure("This rite requires acknowledgment of the material offering.");
                    }

                    break;
                case SacrificeType.MaterialFocus:
                    if (!acknowledgment.AcknowledgeMaterialFocus)
                    {
                        return Result<bool>.Failure("This rite requires acknowledgment of the material focus.");
                    }

                    break;
            }
        }

        return Result<bool>.Success(true);
    }

    /// <summary>
    /// Validates that the character can pay all internal resource costs.
    /// </summary>
    public static Result<bool> ValidateResources(
        IReadOnlyList<RiteRequirement> requirements,
        RiteActivationResourceSnapshot resources)
    {
        int vitae = 0;
        int willpower = 0;
        int stains = 0;

        foreach (RiteRequirement r in requirements)
        {
            switch (r.Type)
            {
                case SacrificeType.InternalVitae:
                case SacrificeType.SpilledVitae:
                    vitae += r.Value;
                    break;
                case SacrificeType.Willpower:
                    willpower += r.Value;
                    break;
                case SacrificeType.HumanityStain:
                    stains += r.Value;
                    break;
            }
        }

        if (resources.CurrentVitae < vitae)
        {
            return Result<bool>.Failure($"Insufficient Vitae. This rite requires {vitae} Vitae.");
        }

        if (resources.CurrentWillpower < willpower)
        {
            return Result<bool>.Failure($"Insufficient Willpower. This rite requires {willpower} Willpower.");
        }

        if (stains > 0 && resources.HumanityStains + stains > 100)
        {
            return Result<bool>.Failure("Humanity stain total would exceed the allowed maximum (100).");
        }

        return Result<bool>.Success(true);
    }

    /// <summary>
    /// Returns true when any requirement needs narrative acknowledgment.
    /// </summary>
    public static bool RequiresExternalAcknowledgment(IReadOnlyList<RiteRequirement> requirements)
    {
        return requirements.Any(r => r.Type is SacrificeType.PhysicalSacrament
            or SacrificeType.Heart
            or SacrificeType.MaterialOffering
            or SacrificeType.MaterialFocus);
    }

    /// <summary>
    /// Aggregates internal costs for transactional updates.
    /// </summary>
    public static (int Vitae, int Willpower, int Stains) AggregateInternalCosts(IReadOnlyList<RiteRequirement> requirements)
    {
        int vitae = 0;
        int willpower = 0;
        int stains = 0;

        foreach (RiteRequirement r in requirements)
        {
            switch (r.Type)
            {
                case SacrificeType.InternalVitae:
                case SacrificeType.SpilledVitae:
                    vitae += r.Value;
                    break;
                case SacrificeType.Willpower:
                    willpower += r.Value;
                    break;
                case SacrificeType.HumanityStain:
                    stains += r.Value;
                    break;
            }
        }

        return (vitae, willpower, stains);
    }
}
