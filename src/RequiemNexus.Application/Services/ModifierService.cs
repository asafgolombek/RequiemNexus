using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Data.Models.Enums;
using RequiemNexus.Domain;
using RequiemNexus.Domain.Enums;
using RequiemNexus.Domain.Models;

namespace RequiemNexus.Application.Services;

/// <summary>
/// Aggregates passive modifiers from all active sources (Coils, Devotions, Covenant benefits, equipped catalog gear).
/// Modifiers are never applied permanently; derived values are computed on demand.
/// </summary>
public class ModifierService(ApplicationDbContext dbContext, ILogger<ModifierService> logger) : IModifierService
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() },
    };

    /// <inheritdoc />
    public async Task<IReadOnlyList<PassiveModifier>> GetModifiersForCharacterAsync(int characterId)
    {
        var modifiers = new List<PassiveModifier>();

        var approvedCoils = await dbContext.CharacterCoils
            .AsNoTracking()
            .Include(cc => cc.CoilDefinition)
            .Where(cc => cc.CharacterId == characterId && cc.Status == CoilLearnStatus.Approved)
            .ToListAsync();

        foreach (var cc in approvedCoils)
        {
            if (cc.CoilDefinition?.ModifiersJson is not { } json || string.IsNullOrEmpty(json))
            {
                continue;
            }

            try
            {
                var coilModifiers = JsonSerializer.Deserialize<List<PassiveModifier>>(json, _jsonOptions);
                if (coilModifiers != null)
                {
                    modifiers.AddRange(coilModifiers);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex,
                    "Malformed ModifiersJson on CoilDefinition {CoilId}; modifier skipped.",
                    cc.CoilDefinition.Id);
            }
        }

        // Batch Strength + Stamina into one round-trip.
        var physAttribs = await dbContext.CharacterAttributes
            .AsNoTracking()
            .Where(a => a.CharacterId == characterId
                     && (a.Name == nameof(AttributeId.Strength) || a.Name == nameof(AttributeId.Stamina)))
            .Select(a => new { a.Name, a.Rating })
            .ToDictionaryAsync(a => a.Name);

        int strengthRating = physAttribs.TryGetValue(nameof(AttributeId.Strength), out var str) ? str.Rating : 0;
        if (strengthRating <= 0)
        {
            strengthRating = 1;
        }

        int staminaRating = physAttribs.TryGetValue(nameof(AttributeId.Stamina), out var sta) ? sta.Rating : 0;
        if (staminaRating <= 0)
        {
            staminaRating = 1;
        }

        var characterRow = await dbContext.Characters
            .AsNoTracking()
            .Where(c => c.Id == characterId)
            .Select(c => new { c.Size, c.HealthDamage })
            .FirstOrDefaultAsync();

        int sizeRating = characterRow?.Size ?? 0;
        if (sizeRating <= 0)
        {
            sizeRating = 5;
        }

        string healthDamage = characterRow?.HealthDamage ?? string.Empty;

        var equippedRows = await dbContext.CharacterAssets
            .AsNoTracking()
            .Include(ca => ca.Asset!)
                .ThenInclude(a => a.Capabilities)
            .Include(ca => ca.Modifiers)
                .ThenInclude(m => m.AssetModifier)
            .Where(ca => ca.CharacterId == characterId && ca.IsEquipped)
            .ToListAsync();

        var activeRows = equippedRows
            .Where(ca => ca.CurrentStructure == null || ca.CurrentStructure > 0)
            .ToList();

        var profileIds = activeRows
            .SelectMany(ca => ca.Asset!.Capabilities)
            .Where(c => c.WeaponProfileAssetId.HasValue)
            .Select(c => c.WeaponProfileAssetId!.Value)
            .Distinct()
            .ToList();

        Dictionary<int, WeaponAsset> weaponProfiles = profileIds.Count == 0
            ? new Dictionary<int, WeaponAsset>()
            : await dbContext.WeaponAssets
                .AsNoTracking()
                .Where(w => profileIds.Contains(w.Id))
                .ToDictionaryAsync(w => w.Id);

        int totalEquippedSize = 0;

        foreach (CharacterAsset ca in activeRows)
        {
            Asset asset = ca.Asset!;
            int itemSize = 0;

            switch (asset)
            {
                case EquipmentAsset eq:
                    AddSkillAssistEquipment(modifiers, eq.AssistsSkillName, eq.DiceBonusMin, eq.DiceBonusMax, eq.Name, eq.Id);
                    itemSize = eq.ItemSize ?? 0;
                    break;
                case ServiceAsset svc:
                    AddSkillAssistEquipment(modifiers, svc.AssistsSkillName, svc.DiceBonusMin, svc.DiceBonusMax, svc.Name, svc.Id);

                    // Services don't have physical Size for encumbrance.
                    break;
                case WeaponAsset w:
                    AddWeaponDamage(modifiers, w, w.Name, w.Id);
                    AddStrengthPenaltyIfNeeded(modifiers, w, strengthRating, w.Id);
                    itemSize = w.ItemSize ?? 0;
                    break;
                case ArmorAsset ar:
                    // Armor defense/speed penalties are applied via Character derived stats.
                    // Track Size for encumbrance.
                    itemSize = ar.ItemSize ?? 0;
                    break;
            }

            totalEquippedSize += itemSize;

            foreach (AssetCapability cap in asset.Capabilities)
            {
                if (cap.Kind == AssetCapabilityKind.WeaponProfileRef
                    && cap.WeaponProfileAssetId is int pid
                    && weaponProfiles.TryGetValue(pid, out WeaponAsset? profile))
                {
                    AddWeaponDamage(modifiers, profile, $"{asset.Name} ({profile.Name})", asset.Id);
                    AddStrengthPenaltyIfNeeded(modifiers, profile, strengthRating, asset.Id);
                }
                else if (cap.Kind == AssetCapabilityKind.SkillAssist
                         && !string.IsNullOrEmpty(cap.AssistsSkillName))
                {
                    AddSkillAssistEquipment(modifiers, cap.AssistsSkillName, cap.DiceBonusMin, cap.DiceBonusMax, asset.Name, asset.Id);
                }
            }

            // --- Phase 11 Refinement: Apply AssetModifiers (Upgrades) ---
            foreach (var cam in ca.Modifiers)
            {
                if (cam.AssetModifier?.ModifierEffectJson is { } modJson && !string.IsNullOrEmpty(modJson))
                {
                    try
                    {
                        var upgrades = JsonSerializer.Deserialize<List<PassiveModifier>>(modJson, _jsonOptions);
                        if (upgrades != null)
                        {
                            foreach (var u in upgrades)
                            {
                                // Attach source info
                                var enriched = u with { Source = new ModifierSource(ModifierSourceType.Equipment, ca.AssetId) };
                                modifiers.Add(enriched);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Malformed ModifierEffectJson on AssetModifier {ModId}", cam.AssetModifier.Id);
                    }
                }
            }
        }

        int maxHealthTrack = Math.Max(1, sizeRating + staminaRating);
        int woundPenaltyDice = WoundPenaltyResolver.GetWoundPenaltyDice(healthDamage, maxHealthTrack);
        if (woundPenaltyDice != 0)
        {
            modifiers.Add(new PassiveModifier(
                ModifierTarget.WoundPenalty,
                woundPenaltyDice,
                ModifierType.Static,
                "Wound penalty",
                new ModifierSource(ModifierSourceType.WoundTrack, characterId)));
        }

        // --- Phase 11 Refinement: Encumbrance (p. 179) ---
        int encumbranceThreshold = strengthRating + staminaRating + sizeRating;
        if (totalEquippedSize > encumbranceThreshold)
        {
            const string encLabel = "Encumbrance (Overloaded)";

            // -1 to all Physical pools (Fatigued condition effect)
            SkillId[] physicalSkills = [SkillId.Brawl, SkillId.Athletics, SkillId.Weaponry, SkillId.Firearms, SkillId.Stealth, SkillId.Survival, SkillId.Drive, SkillId.Larceny];
            foreach (var skill in physicalSkills)
            {
                modifiers.Add(new PassiveModifier(
                    ModifierTarget.SkillPool,
                    -1,
                    ModifierType.Static,
                    encLabel,
                    new ModifierSource(ModifierSourceType.Equipment, 0))
                {
                    AppliesToSkill = skill,
                });
            }

            // -2 to Speed
            modifiers.Add(new PassiveModifier(
                ModifierTarget.Speed,
                -2,
                ModifierType.Static,
                encLabel,
                new ModifierSource(ModifierSourceType.Equipment, 0)));
        }

        return modifiers.AsReadOnly();
    }

    /// <summary>
    /// Emits a per-weapon Strength penalty to the weapon's specific pool (Brawl/Weaponry/Firearms).
    /// Penalty = Strength - StrengthRequirement (a negative value). Each weapon is tracked independently
    /// so the UI can show which item caused the shortfall.
    /// </summary>
    private static void AddStrengthPenaltyIfNeeded(
        List<PassiveModifier> modifiers,
        WeaponAsset w,
        int strengthRating,
        int sourceId)
    {
        if (!w.StrengthRequirement.HasValue || strengthRating >= w.StrengthRequirement.Value)
        {
            return;
        }

        int penalty = strengthRating - w.StrengthRequirement.Value; // negative
        ModifierTarget target = w.IsRangedWeapon
            ? ModifierTarget.Firearms
            : w.UsesBrawlForAttacks
                ? ModifierTarget.Brawl
                : ModifierTarget.Weaponry;

        modifiers.Add(new PassiveModifier(
            target,
            penalty,
            ModifierType.Static,
            $"Strength requirement ({w.Name})",
            new ModifierSource(ModifierSourceType.Equipment, sourceId)));
    }

    private static void AddSkillAssistEquipment(
        List<PassiveModifier> modifiers,
        string? assistsSkillName,
        int? diceMin,
        int? diceMax,
        string assetName,
        int assetId)
    {
        int bonus = diceMax ?? diceMin ?? 0;
        if (bonus == 0 || string.IsNullOrEmpty(assistsSkillName))
        {
            return;
        }

        if (!SkillBookNameParser.TryParseBookName(assistsSkillName, out SkillId skillId))
        {
            return;
        }

        modifiers.Add(new PassiveModifier(
            ModifierTarget.SkillPool,
            bonus,
            ModifierType.Static,
            assetName,
            new ModifierSource(ModifierSourceType.Equipment, assetId))
        {
            AppliesToSkill = skillId,
        });
    }

    private static void AddWeaponDamage(
        List<PassiveModifier> modifiers,
        WeaponAsset w,
        string displayName,
        int sourceId)
    {
        if (w.Damage == 0)
        {
            return;
        }

        ModifierTarget combatTarget = w.IsRangedWeapon
            ? ModifierTarget.Firearms
            : w.UsesBrawlForAttacks
                ? ModifierTarget.Brawl
                : ModifierTarget.Weaponry;
        modifiers.Add(new PassiveModifier(
            combatTarget,
            w.Damage,
            ModifierType.Static,
            displayName,
            new ModifierSource(ModifierSourceType.Equipment, sourceId)));
    }
}
