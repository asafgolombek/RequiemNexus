using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Data.Models.Enums;
using RequiemNexus.Domain.Enums;
using RequiemNexus.Domain.Models;
using RequiemNexus.Domain.Services;

namespace RequiemNexus.Application.Services;

/// <summary>
/// Equipment, service, and weapon passive modifiers plus encumbrance (catalog assets Phase 11).
/// </summary>
public sealed class EquipmentModifierProvider(
    ApplicationDbContext dbContext,
    ILogger<EquipmentModifierProvider> logger) : IModifierProvider
{
    private readonly ApplicationDbContext _dbContext = dbContext;
    private readonly ILogger<EquipmentModifierProvider> _logger = logger;

    /// <inheritdoc />
    public int Order => 40;

    /// <inheritdoc />
    public ModifierSourceType SourceType => ModifierSourceType.Equipment;

    /// <inheritdoc />
    public async Task<IReadOnlyList<PassiveModifier>> GetModifiersAsync(int characterId, CancellationToken cancellationToken = default)
    {
        var modifiers = new List<PassiveModifier>();

        var physAttribs = await _dbContext.CharacterAttributes
            .AsNoTracking()
            .Where(a => a.CharacterId == characterId
                     && (a.Name == nameof(AttributeId.Strength) || a.Name == nameof(AttributeId.Stamina)))
            .Select(a => new { a.Name, a.Rating })
            .ToDictionaryAsync(a => a.Name, cancellationToken);

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

        var characterRow = await _dbContext.Characters
            .AsNoTracking()
            .Where(c => c.Id == characterId)
            .Select(c => new { c.Size })
            .FirstOrDefaultAsync(cancellationToken);

        int sizeRating = characterRow?.Size ?? 0;
        if (sizeRating <= 0)
        {
            sizeRating = 5;
        }

        var equippedRows = await _dbContext.CharacterAssets
            .AsNoTracking()
            .Include(ca => ca.Asset!)
                .ThenInclude(a => a.Capabilities)
            .Include(ca => ca.Modifiers)
                .ThenInclude(m => m.AssetModifier)
            .Where(ca => ca.CharacterId == characterId && ca.IsEquipped)
            .ToListAsync(cancellationToken);

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
            : await _dbContext.WeaponAssets
                .AsNoTracking()
                .Where(w => profileIds.Contains(w.Id))
                .ToDictionaryAsync(w => w.Id, cancellationToken);

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
                    break;
                case WeaponAsset w:
                    AddWeaponDamage(modifiers, w, w.Name, w.Id);
                    AddStrengthPenaltyIfNeeded(modifiers, w, strengthRating, w.Id);
                    itemSize = w.ItemSize ?? 0;
                    break;
                case ArmorAsset ar:
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

            foreach (CharacterAssetModifier cam in ca.Modifiers)
            {
                if (cam.AssetModifier?.ModifierEffectJson is { } modJson && !string.IsNullOrEmpty(modJson))
                {
                    try
                    {
                        List<PassiveModifier>? upgrades = JsonSerializer.Deserialize<List<PassiveModifier>>(modJson, PassiveModifierJsonSerializerOptions.Options);
                        if (upgrades != null)
                        {
                            foreach (PassiveModifier u in upgrades)
                            {
                                PassiveModifier enriched = u with { Source = new ModifierSource(ModifierSourceType.Equipment, ca.AssetId) };
                                modifiers.Add(enriched);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Malformed ModifierEffectJson on AssetModifier {ModId}", cam.AssetModifier.Id);
                    }
                }
            }
        }

        int encumbranceThreshold = strengthRating + staminaRating + sizeRating;
        if (totalEquippedSize > encumbranceThreshold)
        {
            const string encLabel = "Encumbrance (Overloaded)";

            SkillId[] physicalSkills =
            [
                SkillId.Brawl,
                SkillId.Athletics,
                SkillId.Weaponry,
                SkillId.Firearms,
                SkillId.Stealth,
                SkillId.Survival,
                SkillId.Drive,
                SkillId.Larceny,
            ];
            foreach (SkillId skill in physicalSkills)
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

            modifiers.Add(new PassiveModifier(
                ModifierTarget.Speed,
                -2,
                ModifierType.Static,
                encLabel,
                new ModifierSource(ModifierSourceType.Equipment, 0)));
        }

        return modifiers;
    }

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

        int penalty = strengthRating - w.StrengthRequirement.Value;
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
