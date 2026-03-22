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

        int strengthRating = await dbContext.CharacterAttributes
            .AsNoTracking()
            .Where(a => a.CharacterId == characterId && a.Name == nameof(AttributeId.Strength))
            .Select(a => a.Rating)
            .FirstOrDefaultAsync();
        if (strengthRating <= 0)
        {
            strengthRating = 1;
        }

        var equippedRows = await dbContext.CharacterAssets
            .AsNoTracking()
            .Include(ca => ca.Asset!)
                .ThenInclude(a => a.Capabilities)
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

        bool anyWeaponTooHeavy = false;

        foreach (CharacterAsset ca in activeRows)
        {
            Asset asset = ca.Asset!;
            switch (asset)
            {
                case EquipmentAsset eq:
                    AddSkillAssistEquipment(modifiers, eq.AssistsSkillName, eq.DiceBonusMin, eq.DiceBonusMax, eq.Name, eq.Id);
                    break;
                case ServiceAsset svc:
                    AddSkillAssistEquipment(modifiers, svc.AssistsSkillName, svc.DiceBonusMin, svc.DiceBonusMax, svc.Name, svc.Id);
                    break;
                case WeaponAsset w:
                    AddWeaponDamage(modifiers, w, w.Name, w.Id);
                    ConsiderStrength(w.StrengthRequirement, strengthRating, ref anyWeaponTooHeavy);
                    break;
            }

            foreach (AssetCapability cap in asset.Capabilities)
            {
                if (cap.Kind == AssetCapabilityKind.WeaponProfileRef
                    && cap.WeaponProfileAssetId is int pid
                    && weaponProfiles.TryGetValue(pid, out WeaponAsset? profile))
                {
                    AddWeaponDamage(modifiers, profile, $"{asset.Name} ({profile.Name})", asset.Id);
                    ConsiderStrength(profile.StrengthRequirement, strengthRating, ref anyWeaponTooHeavy);
                }
                else if (cap.Kind == AssetCapabilityKind.SkillAssist
                         && !string.IsNullOrEmpty(cap.AssistsSkillName))
                {
                    AddSkillAssistEquipment(modifiers, cap.AssistsSkillName, cap.DiceBonusMin, cap.DiceBonusMax, asset.Name, asset.Id);
                }
            }
        }

        if (anyWeaponTooHeavy)
        {
            const string heavyLabel = "Strength requirement (equipped weapons)";
            modifiers.Add(new PassiveModifier(
                ModifierTarget.Brawl,
                -1,
                ModifierType.Static,
                heavyLabel,
                new ModifierSource(ModifierSourceType.Equipment, 0)));
            modifiers.Add(new PassiveModifier(
                ModifierTarget.Weaponry,
                -1,
                ModifierType.Static,
                heavyLabel,
                new ModifierSource(ModifierSourceType.Equipment, 0)));
            modifiers.Add(new PassiveModifier(
                ModifierTarget.Firearms,
                -1,
                ModifierType.Static,
                heavyLabel,
                new ModifierSource(ModifierSourceType.Equipment, 0)));
        }

        return modifiers.AsReadOnly();
    }

    private static void ConsiderStrength(int? requirement, int strengthRating, ref bool anyTooHeavy)
    {
        if (requirement.HasValue && strengthRating < requirement.Value)
        {
            anyTooHeavy = true;
        }
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
