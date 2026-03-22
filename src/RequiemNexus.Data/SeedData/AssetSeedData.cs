using System.Text.Json;
using System.Text.RegularExpressions;
using RequiemNexus.Data.Models;
using RequiemNexus.Data.Models.Enums;

namespace RequiemNexus.Data.SeedData;

/// <summary>
/// Loads Phase 11 asset catalog (TPT) from <c>SeedSource/*.json</c>.
/// </summary>
public static class AssetSeedData
{
    /// <summary>Slug for the non-listed weapon stats row backing the general-item Crowbar.</summary>
    public const string CrowbarWeaponProfileSlug = "vtm2e:wp:crowbar-profile";

    /// <summary>Slug for the purchasable general Crowbar row.</summary>
    public const string CrowbarGeneralSlug = "vtm2e:gi:crowbar";

    /// <summary>
    /// Builds concrete <see cref="Asset"/> rows (TPT) from JSON. Does not assign capability FKs.
    /// </summary>
    public static IReadOnlyList<Asset> LoadCatalogAssets()
    {
        string? dir = SeedSourcePathResolver.GetSeedDirectory();
        if (dir == null)
        {
            return [];
        }

        var list = new List<Asset>();
        string gi = Path.Combine(dir, "generalItems.json");
        if (File.Exists(gi))
        {
            list.AddRange(LoadGeneralItems(gi));
        }

        string wp = Path.Combine(dir, "weapons.json");
        if (File.Exists(wp))
        {
            list.AddRange(LoadWeapons(wp));
        }

        string ar = Path.Combine(dir, "armors.json");
        if (File.Exists(ar))
        {
            list.AddRange(LoadArmors(ar));
        }

        string sv = Path.Combine(dir, "services.json");
        if (File.Exists(sv))
        {
            list.AddRange(LoadServices(sv));
        }

        return list;
    }

    /// <summary>
    /// Capability rows that reference owning asset / profile by slug (resolve IDs after first save).
    /// </summary>
    public static IReadOnlyList<DeferredAssetCapability> LoadDeferredCapabilities()
    {
        string? dir = SeedSourcePathResolver.GetSeedDirectory();
        if (dir == null || !File.Exists(Path.Combine(dir, "generalItems.json")))
        {
            return [];
        }

        return
        [
            new DeferredAssetCapability(
                OwnerAssetSlug: CrowbarGeneralSlug,
                Kind: AssetCapabilityKind.WeaponProfileRef,
                WeaponProfileSlug: CrowbarWeaponProfileSlug),
        ];
    }

    private static IEnumerable<Asset> LoadGeneralItems(string path)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        foreach (JsonElement el in doc.RootElement.EnumerateArray())
        {
            string name = GetString(el, "Name") ?? string.Empty;
            if (string.IsNullOrEmpty(name))
            {
                continue;
            }

            (int? bonusMin, int? bonusMax) = ParseIntRange(el, "DiceBonus");
            (int? avMin, int? avMax) = ParseIntRange(el, "Availability");
            int availability = avMax ?? avMin ?? 1;
            (int? sizeMin, int? sizeMax) = ParseIntRange(el, "Size");
            (int? durabilityMin, int? durabilityMax) = ParseIntRange(el, "Durability");

            yield return new EquipmentAsset
            {
                Slug = MakeSlug("gi", name),
                Name = name,
                Kind = AssetKind.General,
                Description = GetString(el, "Description"),
                ItemCategory = GetString(el, "Category"),
                AssistsSkillName = GetString(el, "Skill"),
                DiceBonusMin = bonusMin,
                DiceBonusMax = bonusMax ?? bonusMin,
                Cost = availability,
                Availability = availability,
                IsIllicit = el.TryGetProperty("isIllicit", out var ill) && ill.GetBoolean(),
                ItemSize = sizeMax ?? sizeMin,
                ItemDurability = durabilityMax ?? durabilityMin,
            };
        }
    }

    private static IEnumerable<Asset> LoadWeapons(string path)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        foreach (JsonElement el in doc.RootElement.EnumerateArray())
        {
            string name = GetString(el, "Name") ?? string.Empty;
            if (string.IsNullOrEmpty(name))
            {
                continue;
            }

            string type = GetString(el, "Type") ?? "Melee";
            bool ranged = string.Equals(type, "Ranged", StringComparison.OrdinalIgnoreCase);
            string special = GetString(el, "Special") ?? string.Empty;
            int availability = GetInt(el, "Availability") ?? 1;
            int strReq = GetInt(el, "Strength") ?? 1;
            (bool autofire, bool nineAgain, int ap, bool stun) = ParseWeaponSpecialTokens(special);

            bool isCrowbar = name.Equals("Crowbar", StringComparison.OrdinalIgnoreCase);
            string slug = isCrowbar ? CrowbarWeaponProfileSlug : MakeSlug("wp", name);
            bool listed = !isCrowbar;

            yield return new WeaponAsset
            {
                Slug = slug,
                Name = name,
                Kind = AssetKind.Weapon,
                Description = string.IsNullOrEmpty(special) ? null : special,
                Cost = availability,
                Availability = availability,
                IsIllicit = el.TryGetProperty("isIllicit", out var ill) && ill.GetBoolean(),
                IsListedInCatalog = listed,
                Damage = GetInt(el, "Damage") ?? 0,
                InitiativeModifier = GetInt(el, "Initiative"),
                StrengthRequirement = strReq,
                ItemSize = GetInt(el, "Size"),
                Ranges = GetString(el, "Ranges"),
                ClipInfo = GetString(el, "Clip"),
                IsRangedWeapon = ranged,
                UsesBrawlForAttacks = special.Contains("Brawl", StringComparison.OrdinalIgnoreCase),
                WeaponSpecialNotes = string.IsNullOrEmpty(special) ? null : special,
                HasAutofire = autofire,
                HasNineAgain = nineAgain,
                ArmorPiercingRating = ap,
                HasStun = stun,
            };
        }
    }

    private static IEnumerable<Asset> LoadArmors(string path)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        foreach (JsonElement el in doc.RootElement.EnumerateArray())
        {
            string name = GetString(el, "Type") ?? string.Empty;
            if (string.IsNullOrEmpty(name))
            {
                continue;
            }

            string rating = GetString(el, "Rating") ?? "0/0";
            var parts = rating.Split('/', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            int general = parts.Length > 0 && int.TryParse(parts[0], out int g) ? g : 0;
            int ballistic = parts.Length > 1 && int.TryParse(parts[1], out int b) ? b : 0;
            int availability = GetInt(el, "Availability") ?? 1;
            int defense = GetInt(el, "Defense") ?? 0;
            int speed = GetInt(el, "Speed") ?? 0;
            string? notes = GetString(el, "Notes");
            bool concealed = el.TryGetProperty("Concealed", out var c) && c.GetBoolean();

            yield return new ArmorAsset
            {
                Slug = MakeSlug("ar", name),
                Name = name,
                Kind = AssetKind.Armor,
                Description = notes,
                Cost = availability,
                Availability = availability,
                IsIllicit = el.TryGetProperty("isIllicit", out var ill) && ill.GetBoolean(),
                ArmorRating = general,
                ArmorBallisticRating = ballistic,
                ArmorDefenseModifier = defense,
                ArmorSpeedModifier = speed,
                StrengthRequirement = GetInt(el, "Strength"),
                ArmorEra = GetString(el, "Era"),
                ArmorCoverage = GetString(el, "Coverage"),
                ArmorIsConcealable = concealed,
            };
        }
    }

    private static IEnumerable<Asset> LoadServices(string path)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        foreach (JsonElement el in doc.RootElement.EnumerateArray())
        {
            string name = GetString(el, "Service") ?? string.Empty;
            if (string.IsNullOrEmpty(name))
            {
                continue;
            }

            (int? availabilityMin, int? availabilityMax) = ParseIntRange(el, "Availability");
            (int? bonusMin, int? bonusMax) = ParseIntRange(el, "Bonus");
            int availability = availabilityMax ?? availabilityMin ?? 1;
            int bonus = bonusMax ?? bonusMin ?? 0;

            yield return new ServiceAsset
            {
                Slug = MakeSlug("svc", name),
                Name = name,
                Kind = AssetKind.Service,
                AssistsSkillName = GetString(el, "Skill"),
                DiceBonusMin = bonusMin ?? bonus,
                DiceBonusMax = bonusMax ?? bonusMin ?? bonus,
                Cost = availability,
                Availability = availability,
                IsIllicit = el.TryGetProperty("isIllicit", out var ill) && ill.GetBoolean(),
            };
        }
    }

    private static (bool Autofire, bool NineAgain, int Ap, bool Stun) ParseWeaponSpecialTokens(string? special)
    {
        if (string.IsNullOrEmpty(special))
        {
            return (false, false, 0, false);
        }

        string s = special;
        bool autofire = s.Contains("Autofire", StringComparison.OrdinalIgnoreCase);
        bool nine = s.Contains("9-again", StringComparison.OrdinalIgnoreCase) ||
                    s.Contains("9 again", StringComparison.OrdinalIgnoreCase);
        bool stun = s.Contains("Stun", StringComparison.OrdinalIgnoreCase);
        int ap = 0;
        Match m = Regex.Match(s, @"Armor\s*Piercing\s*(\d+)", RegexOptions.IgnoreCase);
        if (m.Success)
        {
            _ = int.TryParse(m.Groups[1].Value, out ap);
        }

        return (autofire, nine, ap, stun);
    }

    private static string MakeSlug(string prefix, string name)
    {
        string s = Regex.Replace(name.ToLowerInvariant(), @"[^a-z0-9]+", "-").Trim('-');
        return $"vtm2e:{prefix}:{s}";
    }

    private static string? GetString(JsonElement el, string prop) =>
        el.TryGetProperty(prop, out var p) && p.ValueKind == JsonValueKind.String ? p.GetString() : null;

    private static int? GetInt(JsonElement el, string prop)
    {
        if (!el.TryGetProperty(prop, out var p))
        {
            return null;
        }

        if (p.ValueKind == JsonValueKind.Number && p.TryGetInt32(out int n))
        {
            return n;
        }

        if (p.ValueKind == JsonValueKind.String && int.TryParse(p.GetString(), out int sv))
        {
            return sv;
        }

        return null;
    }

    private static (int? Minimum, int? Maximum) ParseIntRange(JsonElement el, string prop)
    {
        if (!el.TryGetProperty(prop, out var p))
        {
            return (null, null);
        }

        if (p.ValueKind == JsonValueKind.Number && p.TryGetInt32(out int n))
        {
            return (n, n);
        }

        if (p.ValueKind != JsonValueKind.String)
        {
            return (null, null);
        }

        string? s = p.GetString();
        if (string.IsNullOrWhiteSpace(s) || s.Equals("N/A", StringComparison.OrdinalIgnoreCase))
        {
            return (null, null);
        }

        if (int.TryParse(s, out int single))
        {
            return (single, single);
        }

        Match m = Regex.Match(s, @"^(\d+)\s*(?:to|or)\s*(\d+)$", RegexOptions.IgnoreCase);
        if (m.Success)
        {
            int a = int.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
            int b = int.Parse(m.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture);
            return (Math.Min(a, b), Math.Max(a, b));
        }

        return (null, null);
    }
}
