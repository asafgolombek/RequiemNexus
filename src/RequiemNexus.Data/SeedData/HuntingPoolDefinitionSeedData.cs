using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using RequiemNexus.Data.Models;
using RequiemNexus.Domain.Enums;
using RequiemNexus.Domain.Models;

namespace RequiemNexus.Data.SeedData;

/// <summary>
/// Seed rows for canonical hunting pools per <see cref="PredatorType"/>.
/// </summary>
public static class HuntingPoolDefinitionSeedData
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>
    /// Inserts hunting pool definitions when the table is empty.
    /// </summary>
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        if (await context.HuntingPoolDefinitions.AnyAsync())
        {
            return;
        }

        foreach (HuntingPoolDefinition row in GetDefinitions())
        {
            context.HuntingPoolDefinitions.Add(row);
        }

        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Builds the nine predator-type rows (for tests or tooling).
    /// </summary>
    public static IReadOnlyList<HuntingPoolDefinition> GetDefinitions()
    {
        static string Json(PoolDefinition pool) => JsonSerializer.Serialize(pool, _jsonOptions);

        return
        [
            new HuntingPoolDefinition
            {
                Id = 1,
                PredatorType = PredatorType.Alleycat,
                PoolDefinitionJson = Json(new PoolDefinition(
                [
                    new TraitReference(TraitType.Attribute, AttributeId.Strength, null, null),
                    new TraitReference(TraitType.Skill, null, SkillId.Brawl, null),
                ])),
                BaseVitaeGain = 0,
                PerSuccessVitaeGain = 1,
                NarrativeDescription =
                    "Predatory ambush in the city's shadows — force and ferocity over finesse.",
            },
            new HuntingPoolDefinition
            {
                Id = 2,
                PredatorType = PredatorType.Bagger,
                PoolDefinitionJson = Json(new PoolDefinition(
                [
                    new TraitReference(TraitType.Attribute, AttributeId.Resolve, null, null),
                    new TraitReference(TraitType.Skill, null, SkillId.Streetwise, null),
                ])),
                BaseVitaeGain = 0,
                PerSuccessVitaeGain = 1,
                NarrativeDescription =
                    "Working hospital supply chains, blood banks, and underground markets.",
            },
            new HuntingPoolDefinition
            {
                Id = 3,
                PredatorType = PredatorType.Cleaver,
                PoolDefinitionJson = Json(new PoolDefinition(
                [
                    new TraitReference(TraitType.Attribute, AttributeId.Wits, null, null),
                    new TraitReference(TraitType.Skill, null, SkillId.Subterfuge, null),
                ])),
                BaseVitaeGain = 0,
                PerSuccessVitaeGain = 1,
                NarrativeDescription =
                    "Feeding from kine who believe you are still mortal — family or close circle.",
            },
            new HuntingPoolDefinition
            {
                Id = 4,
                PredatorType = PredatorType.Consensualist,
                PoolDefinitionJson = Json(new PoolDefinition(
                [
                    new TraitReference(TraitType.Attribute, AttributeId.Presence, null, null),
                    new TraitReference(TraitType.Skill, null, SkillId.Persuasion, null),
                ])),
                BaseVitaeGain = 0,
                PerSuccessVitaeGain = 1,
                NarrativeDescription =
                    "Charming willing vessels into a mutually beneficial arrangement.",
            },
            new HuntingPoolDefinition
            {
                Id = 5,
                PredatorType = PredatorType.Farmer,
                PoolDefinitionJson = Json(new PoolDefinition(
                [
                    new TraitReference(TraitType.Attribute, AttributeId.Composure, null, null),
                    new TraitReference(TraitType.Skill, null, SkillId.AnimalKen, null),
                ])),
                BaseVitaeGain = 0,
                PerSuccessVitaeGain = 1,
                NarrativeDescription =
                    "Drawing sustenance from animals — calm, patient, and without mortal risk.",
            },
            new HuntingPoolDefinition
            {
                Id = 6,
                PredatorType = PredatorType.Osiris,
                PoolDefinitionJson = Json(new PoolDefinition(
                [
                    new TraitReference(TraitType.Attribute, AttributeId.Presence, null, null),
                    new TraitReference(TraitType.Skill, null, SkillId.Occult, null),
                ])),
                BaseVitaeGain = 0,
                PerSuccessVitaeGain = 1,
                NarrativeDescription =
                    "A cult of devoted followers — feeding among rites they believe to be sacred.",
            },
            new HuntingPoolDefinition
            {
                Id = 7,
                PredatorType = PredatorType.Sandman,
                PoolDefinitionJson = Json(new PoolDefinition(
                [
                    new TraitReference(TraitType.Attribute, AttributeId.Dexterity, null, null),
                    new TraitReference(TraitType.Skill, null, SkillId.Stealth, null),
                ])),
                BaseVitaeGain = 0,
                PerSuccessVitaeGain = 1,
                NarrativeDescription =
                    "Entering sleeping prey's home unseen, feeding before dawn.",
            },
            new HuntingPoolDefinition
            {
                Id = 8,
                PredatorType = PredatorType.SceneQueen,
                PoolDefinitionJson = Json(new PoolDefinition(
                [
                    new TraitReference(TraitType.Attribute, AttributeId.Manipulation, null, null),
                    new TraitReference(TraitType.Skill, null, SkillId.Persuasion, null),
                ])),
                BaseVitaeGain = 0,
                PerSuccessVitaeGain = 1,
                NarrativeDescription =
                    "Working a social scene — clubs, galas, or Elysium periphery.",
            },
            new HuntingPoolDefinition
            {
                Id = 9,
                PredatorType = PredatorType.Siren,
                PoolDefinitionJson = Json(new PoolDefinition(
                [
                    new TraitReference(TraitType.Attribute, AttributeId.Presence, null, null),
                    new TraitReference(TraitType.Skill, null, SkillId.Subterfuge, null),
                ])),
                BaseVitaeGain = 0,
                PerSuccessVitaeGain = 1,
                NarrativeDescription =
                    "Seduction and false intimacy as the approach to a vessel.",
            },
        ];
    }
}
