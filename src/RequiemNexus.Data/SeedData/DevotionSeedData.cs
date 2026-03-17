using System.Text.Json;
using System.Text.Json.Serialization;
using RequiemNexus.Data.Models;
using RequiemNexus.Domain;
using RequiemNexus.Domain.Enums;
using RequiemNexus.Domain.Models;

namespace RequiemNexus.Data.SeedData;

/// <summary>
/// Seed data for devotion definitions. Phase 8: additive pools only.
/// </summary>
public static class DevotionSeedData
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>
    /// Creates sample devotion definitions with prerequisites and pool definitions.
    /// Requires disciplines to be seeded first.
    /// </summary>
    public static List<DevotionDefinition> GetSampleDevotions(List<Discipline> disciplines)
    {
        Discipline Disc(string name) => disciplines.First(d => d.Name == name);

        var devotions = new List<DevotionDefinition>();

        // Body of Will: Stamina + Survival + Resilience
        devotions.Add(CreateDevotion(
            name: "Body of Will",
            description: "Ignore wound penalties, free Vigor effects.",
            xpCost: 2,
            activationCost: "●",
            isPassive: false,
            pool: new PoolDefinition(
            [
                new TraitReference(TraitType.Attribute, AttributeId.Stamina, null, null),
                new TraitReference(TraitType.Skill, null, SkillId.Survival, null),
                new TraitReference(TraitType.Discipline, null, null, Disc("Resilience").Id),
            ]),
            prerequisites: [(Disc("Resilience").Id, 3), (Disc("Vigor").Id, 1)],
            orGroupId: 0,
            source: "VTR 2e 142"));

        // Best Served Cold: Stamina + Athletics + Vigor
        devotions.Add(CreateDevotion(
            name: "Best Served Cold",
            description: "Until the end of the chapter or the person who harmed you is defeated, +3 to attack them. Can only have one target at a time.",
            xpCost: 1,
            activationCost: "●●",
            isPassive: false,
            pool: new PoolDefinition(
            [
                new TraitReference(TraitType.Attribute, AttributeId.Stamina, null, null),
                new TraitReference(TraitType.Skill, null, SkillId.Athletics, null),
                new TraitReference(TraitType.Discipline, null, null, Disc("Vigor").Id),
            ]),
            prerequisites: [(Disc("Vigor").Id, 3)],
            orGroupId: 0,
            source: "GTTN 135"));

        // Blood Scenting: Wits + Composure + Auspex
        devotions.Add(CreateDevotion(
            name: "Blood Scenting",
            description: "Identify the target's clan, blood potency and disciplines.",
            xpCost: 1,
            activationCost: "●",
            isPassive: false,
            pool: new PoolDefinition(
            [
                new TraitReference(TraitType.Attribute, AttributeId.Wits, null, null),
                new TraitReference(TraitType.Attribute, AttributeId.Composure, null, null),
                new TraitReference(TraitType.Discipline, null, null, Disc("Auspex").Id),
            ]),
            prerequisites: [(Disc("Auspex").Id, 3)],
            orGroupId: 0,
            source: "GTTN 136"));

        // Bones of the Mountain: Stamina + Survival + Protean
        devotions.Add(CreateDevotion(
            name: "Bones of the Mountain",
            description: "Become living stone for a turn, adding Protean to Resilience, dealing lethal unarmed, and activating Resilience and Vigor for free.",
            xpCost: 5,
            activationCost: "●●●",
            isPassive: false,
            pool: new PoolDefinition(
            [
                new TraitReference(TraitType.Attribute, AttributeId.Stamina, null, null),
                new TraitReference(TraitType.Skill, null, SkillId.Survival, null),
                new TraitReference(TraitType.Discipline, null, null, Disc("Protean").Id),
            ]),
            prerequisites: [(Disc("Protean").Id, 4), (Disc("Resilience").Id, 3), (Disc("Vigor").Id, 3)],
            orGroupId: 0,
            source: "TY 73"));

        return devotions;
    }

    private static DevotionDefinition CreateDevotion(
        string name,
        string description,
        int xpCost,
        string activationCost,
        bool isPassive,
        PoolDefinition pool,
        List<(int DisciplineId, int MinLevel)> prerequisites,
        int orGroupId,
        string source)
    {
        var def = new DevotionDefinition
        {
            Name = name,
            Description = description,
            XpCost = xpCost,
            ActivationCostDescription = activationCost,
            IsPassive = isPassive,
            PoolDefinitionJson = JsonSerializer.Serialize(pool, _jsonOptions),
            Source = source,
        };

        foreach (var (disciplineId, minLevel) in prerequisites)
        {
            def.Prerequisites.Add(new DevotionPrerequisite
            {
                DisciplineId = disciplineId,
                MinimumLevel = minLevel,
                OrGroupId = orGroupId,
            });
        }

        return def;
    }
}
