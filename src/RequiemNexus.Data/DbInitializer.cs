using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RequiemNexus.Data.Models;
using RequiemNexus.Data.SeedData;

namespace RequiemNexus.Data;

public static class DbInitializer
{
    /// <summary>Default activation cost when no structured requirements are specified (Phase 9.5).</summary>
    private const string _defaultRiteRequirementsJson = """[{"type":"InternalVitae","value":1,"isConsumed":true}]""";

    public static async Task InitializeAsync(ApplicationDbContext context, RoleManager<IdentityRole> roleManager, ILogger logger, bool runMigrations = false)
    {
        if (runMigrations)
        {
            await context.Database.MigrateAsync();
        }

        await SeedRolesAsync(roleManager);
        await SeedClansAndDisciplinesAsync(context);
        await SeedHuntingPoolDefinitionsAsync(context);
        await SeedMeritsAsync(context, logger);
        await SeedEquipmentCatalogAsync(context);
        await SeedCovenantsAsync(context, logger);
        await SeedCovenantDefinitionMeritsAsync(context);
        await SeedBloodlinesAsync(context, logger);
        await SeedDevotionsAsync(context, logger);
        await SeedSorceryRitesAsync(context, logger);
        await EnsureBloodSorceryPhaseExtensionsAsync(context);
        await SeedCoilsAsync(context, logger);
        await SeedPrebuiltStatBlocksAsync(context);
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        string[] roles = ["Player", "Storyteller", "Admin"];
        foreach (string role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }

    private static async Task SeedClansAndDisciplinesAsync(ApplicationDbContext context)
    {
        bool hasClansAndDisciplines = await context.Clans.AnyAsync() && await context.Disciplines.AnyAsync();

        if (!hasClansAndDisciplines)
        {
            List<Clan> clans = ClanSeedData.GetAllClans();
            await context.Clans.AddRangeAsync(clans);
            await context.SaveChangesAsync();

            List<Discipline> disciplinesList = DisciplineSeedData.GetAll();
            await context.Disciplines.AddRangeAsync(disciplinesList);
            await context.SaveChangesAsync();

            List<ClanDiscipline> clanDisciplines = ClanSeedData.GetClanDisciplineMappings(clans, disciplinesList);
            await context.ClanDisciplines.AddRangeAsync(clanDisciplines);
            await context.SaveChangesAsync();
        }
    }

    private static async Task SeedHuntingPoolDefinitionsAsync(ApplicationDbContext context)
    {
        await HuntingPoolDefinitionSeedData.SeedAsync(context);
    }

    private static async Task SeedMeritsAsync(ApplicationDbContext context, ILogger logger)
    {
        // Idempotent: never remove CharacterMerits or re-seed on every startup — that wiped player selections
        // after each host restart. Refreshing the official catalog requires an explicit migration or tooling.
        if (await context.Merits.AnyAsync(m => !m.IsHomebrew))
        {
            return;
        }

        var merits = MeritSeedData.LoadFromDocs(logger);
        await context.Merits.AddRangeAsync(merits);
        await context.SaveChangesAsync();

        var meritIdsByName = (await context.Merits.Where(m => !m.IsHomebrew).ToListAsync())
            .ToDictionary(m => m.Name, m => m.Id, StringComparer.OrdinalIgnoreCase);
        var prereqs = MeritPrerequisiteSeedData.GetPrerequisitesToSeed(meritIdsByName);
        if (prereqs.Count > 0)
        {
            await context.MeritPrerequisites.AddRangeAsync(prereqs);
            await context.SaveChangesAsync();
        }
    }

    private static async Task SeedCovenantsAsync(ApplicationDbContext context, ILogger logger)
    {
        if (await context.CovenantDefinitions.AnyAsync())
        {
            return;
        }

        var covenants = CovenantSeedData.LoadFromDocs(logger);
        await context.CovenantDefinitions.AddRangeAsync(covenants);
        await context.SaveChangesAsync();
    }

    private static async Task SeedCovenantDefinitionMeritsAsync(ApplicationDbContext context)
    {
        var covenants = await context.CovenantDefinitions.ToListAsync();
        var merits = await context.Merits.ToListAsync();
        var existing = await context.CovenantDefinitionMerits
            .Select(cdm => new { cdm.CovenantDefinitionId, cdm.MeritId })
            .ToListAsync();
        var existingSet = new HashSet<(int, int)>(existing.Select(e => (e.CovenantDefinitionId, e.MeritId)));

        var covenantByName = covenants.ToDictionary(c => c.Name, StringComparer.OrdinalIgnoreCase);
        var meritByName = merits.ToDictionary(m => m.Name, StringComparer.OrdinalIgnoreCase);

        var links = new List<(string Covenant, string Merit)>
        {
            ("The Carthian Movement", "Status (Carthian)"),
            ("The Carthian Movement", "Carthian Pull"),
            ("The Carthian Movement", "Plausible Deniability"),
            ("The Carthian Movement", "Strength of Resolution"),
            ("The Carthian Movement", "Mandate from the Masses"),
            ("The Circle of the Crone", "Status (Crone)"),
            ("The Circle of the Crone", "Altar"),
            ("The Circle of the Crone", "The Mother-Daughter Bond"),
            ("The Circle of the Crone", "Undead Menses"),
            ("The Invictus", "Status (Invictus)"),
            ("The Invictus", "Attaché"),
            ("The Invictus", "Friends in High Places"),
            ("The Invictus", "Invested"),
            ("The Invictus", "Notary"),
            ("The Invictus", "Oath of Fealty"),
            ("The Invictus", "Oath of Penance"),
            ("The Invictus", "Oath of Serfdom"),
            ("The Lancea et Sanctum", "Status (Lancea)"),
            ("The Lancea et Sanctum", "Anointed"),
            ("The Ordo Dracul", "Status (Ordo)"),
            ("The Ordo Dracul", "Sworn"),
        };

        var toAdd = new List<CovenantDefinitionMerit>();
        foreach (var (covenantName, meritName) in links)
        {
            if (!covenantByName.TryGetValue(covenantName, out var covenant) ||
                !meritByName.TryGetValue(meritName, out var merit))
            {
                continue;
            }

            if (existingSet.Contains((covenant.Id, merit.Id)))
            {
                continue;
            }

            toAdd.Add(new CovenantDefinitionMerit
            {
                CovenantDefinitionId = covenant.Id,
                MeritId = merit.Id,
            });
            existingSet.Add((covenant.Id, merit.Id));
        }

        if (toAdd.Count > 0)
        {
            await context.CovenantDefinitionMerits.AddRangeAsync(toAdd);
            await context.SaveChangesAsync();
        }
    }

    private static async Task SeedBloodlinesAsync(ApplicationDbContext context, ILogger logger)
    {
        if (await context.BloodlineDefinitions.AnyAsync())
        {
            return;
        }

        var clans = await context.Clans.ToListAsync();
        var disciplines = await context.Disciplines.ToListAsync();
        var bloodlines = BloodlineSeedData.LoadFromDocs(clans, disciplines, logger);
        await context.BloodlineDefinitions.AddRangeAsync(bloodlines);
        await context.SaveChangesAsync();
    }

    private static async Task SeedDevotionsAsync(ApplicationDbContext context, ILogger logger)
    {
        if (await context.DevotionDefinitions.AnyAsync())
        {
            return;
        }

        var disciplines = await context.Disciplines.ToListAsync();
        var devotions = DevotionSeedData.LoadFromDocs(disciplines, logger);
        await context.DevotionDefinitions.AddRangeAsync(devotions);
        await context.SaveChangesAsync();
    }

    private static async Task SeedSorceryRitesAsync(ApplicationDbContext context, ILogger logger)
    {
        if (await context.SorceryRiteDefinitions.AnyAsync())
        {
            return;
        }

        var covenants = await context.CovenantDefinitions.ToListAsync();
        var disciplines = await context.Disciplines.ToListAsync();

        var crone = covenants.FirstOrDefault(c => c.Name == "The Circle of the Crone");
        var lancea = covenants.FirstOrDefault(c => c.Name == "The Lancea et Sanctum");
        var cruacDisc = disciplines.FirstOrDefault(d => d.Name == "Crúac");
        var thebanDisc = disciplines.FirstOrDefault(d => d.Name == "Theban Sorcery");

        if (crone == null || lancea == null || cruacDisc == null || thebanDisc == null)
        {
            return;
        }

        var entries = SorceryRiteSeedData.LoadFromDocs(logger);
        var rites = new List<SorceryRiteDefinition>();

        foreach (var (name, rating, prerequisites, effect, sorceryType) in entries)
        {
            int requiredCovenantId = sorceryType == Domain.Enums.SorceryType.Cruac ? crone.Id : lancea.Id;
            int disciplineId = sorceryType == Domain.Enums.SorceryType.Cruac ? cruacDisc.Id : thebanDisc.Id;
            string? poolJson = BuildSorceryPoolJson(disciplineId);

            rites.Add(new SorceryRiteDefinition
            {
                Name = name,
                Description = effect,
                Level = rating,
                SorceryType = sorceryType,
                XpCost = rating,
                PoolDefinitionJson = poolJson,
                ActivationCostDescription = "1 Vitae",
                RequiredCovenantId = requiredCovenantId,
                RequirementsJson = _defaultRiteRequirementsJson,
                Prerequisites = prerequisites,
                Effect = effect,
            });
        }

        await context.SorceryRiteDefinitions.AddRangeAsync(rites);
        await context.SaveChangesAsync();
    }

    private static string? BuildSorceryPoolJson(int disciplineId)
    {
        var traits = new List<object>
        {
            new { Type = 2, AttributeId = (int?)null, SkillId = (int?)null, DisciplineId = disciplineId, MinimumLevel = (int?)null },
            new { Type = 0, AttributeId = 0, SkillId = (int?)null, DisciplineId = (int?)null, MinimumLevel = (int?)null },
            new { Type = 1, AttributeId = (int?)null, SkillId = 5, DisciplineId = (int?)null, MinimumLevel = (int?)null },
        };
        return System.Text.Json.JsonSerializer.Serialize(new { Traits = traits });
    }

    /// <summary>
    /// Ensures Phase 9.5/9.6 disciplines, covenant flags, default requirements JSON, and sample Necromancy/Ordo rites exist.
    /// </summary>
    private static async Task EnsureBloodSorceryPhaseExtensionsAsync(ApplicationDbContext context)
    {
        await EnsureDisciplineExistsAsync(context, "Necromancy", "Death sorcery associated with the Mekhet — corpses, shades, and the other side.");
        await EnsureDisciplineExistsAsync(context, "Ordo Sorcery", "Covenant rituals of the Ordo Dracul; used for unified dice pools in Requiem Nexus.");

        CovenantDefinition? ordoCovenant = await context.CovenantDefinitions.FirstOrDefaultAsync(c => c.Name == "The Ordo Dracul");
        if (ordoCovenant != null && !ordoCovenant.SupportsOrdoRituals)
        {
            ordoCovenant.SupportsOrdoRituals = true;
            await context.SaveChangesAsync();
        }

        List<SorceryRiteDefinition> missingReq = await context.SorceryRiteDefinitions
            .Where(r => r.RequirementsJson == null || r.RequirementsJson == string.Empty)
            .ToListAsync();
        foreach (SorceryRiteDefinition row in missingReq)
        {
            row.RequirementsJson = _defaultRiteRequirementsJson;
        }

        if (missingReq.Count > 0)
        {
            await context.SaveChangesAsync();
        }

        Clan? mekhet = await context.Clans.AsNoTracking().FirstOrDefaultAsync(c => c.Name == "Mekhet");
        Discipline? necromancy = await context.Disciplines.AsNoTracking().FirstOrDefaultAsync(d => d.Name == "Necromancy");
        Discipline? ordoSorcery = await context.Disciplines.AsNoTracking().FirstOrDefaultAsync(d => d.Name == "Ordo Sorcery");
        CovenantDefinition? ordo = await context.CovenantDefinitions.AsNoTracking().FirstOrDefaultAsync(c => c.Name == "The Ordo Dracul");

        if (mekhet != null && necromancy != null
            && !await context.SorceryRiteDefinitions.AnyAsync(r => r.Name == "Corrupting the Corpse"))
        {
            string? poolN = BuildSorceryPoolJson(necromancy.Id);
            context.SorceryRiteDefinitions.Add(new SorceryRiteDefinition
            {
                Name = "Corrupting the Corpse",
                Description = "Warp a corpse so it resists identification and sanctified rest.",
                Level = 1,
                SorceryType = Domain.Enums.SorceryType.Necromancy,
                XpCost = 1,
                PoolDefinitionJson = poolN,
                ActivationCostDescription = "1 Vitae + focus",
                RequiredCovenantId = null,
                RequiredClanId = mekhet.Id,
                RequirementsJson = """[{"type":"MaterialFocus","value":1,"isConsumed":false},{"type":"InternalVitae","value":1,"isConsumed":true}]""",
                Prerequisites = "Corpse present; narrative focus required (acknowledge in app).",
                Effect = "Prepares the remains for further necromantic workings.",
            });
        }

        if (ordo != null && ordoSorcery != null
            && !await context.SorceryRiteDefinitions.AnyAsync(r => r.Name == "Dragon's Own Fire"))
        {
            string? poolO = BuildSorceryPoolJson(ordoSorcery.Id);
            context.SorceryRiteDefinitions.Add(new SorceryRiteDefinition
            {
                Name = "Dragon's Own Fire",
                Description = "Kindle supernatural flame from the character's Vitae.",
                Level = 2,
                SorceryType = Domain.Enums.SorceryType.OrdoDraculRitual,
                XpCost = 2,
                PoolDefinitionJson = poolO,
                ActivationCostDescription = "2 Vitae",
                RequiredCovenantId = ordo.Id,
                RequirementsJson = """[{"type":"InternalVitae","value":2,"isConsumed":true}]""",
                Prerequisites = "Member of the Ordo Dracul.",
                Effect = "Produces draconic flame; combat and duration resolved at the table.",
            });
        }

        await context.SaveChangesAsync();
    }

    private static async Task EnsureDisciplineExistsAsync(ApplicationDbContext context, string name, string description)
    {
        if (await context.Disciplines.AnyAsync(d => d.Name == name))
        {
            return;
        }

        context.Disciplines.Add(new Discipline { Name = name, Description = description });
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Ensures Scale and Coil definitions exist for every entry in coil seed data.
    /// Inserts only scales missing by name so existing databases pick up new Mysteries when seed JSON grows.
    /// </summary>
    private static async Task SeedCoilsAsync(ApplicationDbContext context, ILogger logger)
    {
        var entries = CoilSeedData.LoadFromDocs(logger);
        List<string> existingNames = await context.ScaleDefinitions
            .AsNoTracking()
            .Select(s => s.Name)
            .ToListAsync();
        HashSet<string> existingScaleNames = existingNames.ToHashSet(StringComparer.Ordinal);

        foreach ((ScaleDefinition scale, List<CoilDefinition> coils) in entries)
        {
            if (existingScaleNames.Contains(scale.Name))
            {
                continue;
            }

            context.ScaleDefinitions.Add(scale);
            await context.SaveChangesAsync();
            existingScaleNames.Add(scale.Name);

            // Resolve prerequisite chain — coils reference each other in memory;
            // assign ScaleId and save level-by-level so FK to prerequisiteCoilId resolves correctly.
            foreach (CoilDefinition coil in coils.OrderBy(c => c.Level))
            {
                coil.ScaleId = scale.Id;
                if (coil.PrerequisiteCoil != null && coil.PrerequisiteCoil.Id > 0)
                {
                    coil.PrerequisiteCoilId = coil.PrerequisiteCoil.Id;
                    coil.PrerequisiteCoil = null; // avoid EF tracking conflicts
                }

                context.CoilDefinitions.Add(coil);
                await context.SaveChangesAsync();
            }
        }
    }

    private static async Task SeedEquipmentCatalogAsync(ApplicationDbContext context)
    {
        IReadOnlyList<Asset> catalog = AssetSeedData.LoadCatalogAssets();
        if (catalog.Count == 0)
        {
            return;
        }

        HashSet<string> existing = (await context.Assets
                .Where(a => a.Slug != null)
                .Select(a => a.Slug!)
                .ToListAsync())
            .ToHashSet(StringComparer.Ordinal);

        List<Asset> toAdd = catalog
            .Where(a => a.Slug != null && !existing.Contains(a.Slug))
            .ToList();
        if (toAdd.Count == 0)
        {
            await SeedDeferredAssetCapabilitiesAsync(context);
            return;
        }

        await context.Assets.AddRangeAsync(toAdd);
        await context.SaveChangesAsync();
        await SeedDeferredAssetCapabilitiesAsync(context);
    }

    private static async Task SeedDeferredAssetCapabilitiesAsync(ApplicationDbContext context)
    {
        IReadOnlyList<DeferredAssetCapability> deferred = AssetSeedData.LoadDeferredCapabilities();
        if (deferred.Count == 0)
        {
            return;
        }

        Dictionary<string, int> idBySlug = await context.Assets
            .Where(a => a.Slug != null)
            .Select(a => new { a.Slug, a.Id })
            .ToDictionaryAsync(x => x.Slug!, x => x.Id, StringComparer.Ordinal);

        foreach (DeferredAssetCapability d in deferred)
        {
            if (!idBySlug.TryGetValue(d.OwnerAssetSlug, out int ownerId))
            {
                continue;
            }

            bool already = await context.AssetCapabilities.AnyAsync(c => c.AssetId == ownerId && c.Kind == d.Kind);
            if (already)
            {
                continue;
            }

            int? profileId = null;
            if (d.WeaponProfileSlug != null && idBySlug.TryGetValue(d.WeaponProfileSlug, out int pid))
            {
                profileId = pid;
            }

            context.AssetCapabilities.Add(new AssetCapability
            {
                AssetId = ownerId,
                Kind = d.Kind,
                AssistsSkillName = d.AssistsSkillName,
                DiceBonusMin = d.DiceBonusMin,
                DiceBonusMax = d.DiceBonusMax,
                WeaponProfileAssetId = profileId,
            });
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedPrebuiltStatBlocksAsync(ApplicationDbContext context)
    {
        if (await context.NpcStatBlocks.AnyAsync(s => s.IsPrebuilt))
        {
            return;
        }

        NpcStatBlock[] blocks =
        [
            new()
            {
                Name = "Mortal",
                Concept = "Average human",
                Size = 5,
                Health = 7,
                Willpower = 3,
                BludgeoningArmor = 0,
                LethalArmor = 0,
                AttributesJson = "{\"Strength\":2,\"Dexterity\":2,\"Stamina\":2,\"Intelligence\":2,\"Wits\":2,\"Resolve\":2,\"Presence\":2,\"Manipulation\":2,\"Composure\":2}",
                SkillsJson = "{\"Brawl\":1,\"Athletics\":1,\"Drive\":1}",
                DisciplinesJson = "{}",
                Notes = "Standard human. No supernatural traits.",
                IsPrebuilt = true,
            },
            new()
            {
                Name = "Ghoul",
                Concept = "Vitae-bound servant",
                Size = 5,
                Health = 8,
                Willpower = 4,
                BludgeoningArmor = 0,
                LethalArmor = 0,
                AttributesJson = "{\"Strength\":3,\"Dexterity\":2,\"Stamina\":3,\"Intelligence\":2,\"Wits\":2,\"Resolve\":2,\"Presence\":2,\"Manipulation\":2,\"Composure\":2}",
                SkillsJson = "{\"Brawl\":2,\"Athletics\":2,\"Firearms\":1,\"Stealth\":1}",
                DisciplinesJson = "{\"Vigor\":1}",
                Notes = "Fed vampiric vitae. Has one dot of a regnant's Discipline. Resists frenzy-like impulses.",
                IsPrebuilt = true,
            },
            new()
            {
                Name = "Neonate",
                Concept = "Newly Embraced vampire",
                Size = 5,
                Health = 8,
                Willpower = 5,
                BludgeoningArmor = 1,
                LethalArmor = 0,
                AttributesJson = "{\"Strength\":3,\"Dexterity\":3,\"Stamina\":3,\"Intelligence\":2,\"Wits\":3,\"Resolve\":2,\"Presence\":2,\"Manipulation\":2,\"Composure\":3}",
                SkillsJson = "{\"Brawl\":2,\"Athletics\":2,\"Stealth\":2,\"Intimidation\":1}",
                DisciplinesJson = "{\"Clan Discipline 1\":2,\"Clan Discipline 2\":1}",
                Notes = "Blood Potency 1. Two clan Disciplines at 2 and 1 dots. Replaces clan name with specific one at the table.",
                IsPrebuilt = true,
            },
            new()
            {
                Name = "Elder",
                Concept = "Ancient and formidable vampire",
                Size = 5,
                Health = 10,
                Willpower = 8,
                BludgeoningArmor = 2,
                LethalArmor = 1,
                AttributesJson = "{\"Strength\":4,\"Dexterity\":4,\"Stamina\":4,\"Intelligence\":4,\"Wits\":4,\"Resolve\":4,\"Presence\":4,\"Manipulation\":4,\"Composure\":4}",
                SkillsJson = "{\"Brawl\":4,\"Athletics\":3,\"Intimidation\":4,\"Persuasion\":4,\"Occult\":4,\"Stealth\":3}",
                DisciplinesJson = "{\"Primary Discipline\":5,\"Secondary Discipline\":3,\"Tertiary Discipline\":2}",
                Notes = "Blood Potency 5+. Treat as a major threat. Customize Disciplines to match covenant.",
                IsPrebuilt = true,
            },
            new()
            {
                Name = "Coterie Member",
                Concept = "PC-caliber allied vampire",
                Size = 5,
                Health = 8,
                Willpower = 6,
                BludgeoningArmor = 1,
                LethalArmor = 0,
                AttributesJson = "{\"Strength\":3,\"Dexterity\":3,\"Stamina\":3,\"Intelligence\":3,\"Wits\":3,\"Resolve\":3,\"Presence\":3,\"Manipulation\":3,\"Composure\":3}",
                SkillsJson = "{\"Brawl\":2,\"Athletics\":2,\"Stealth\":2,\"Persuasion\":2,\"Subterfuge\":2}",
                DisciplinesJson = "{\"Clan Discipline\":3}",
                Notes = "Use as a friendly NPC coterie member or as a baseline for PC-equivalent antagonist.",
                IsPrebuilt = true,
            },
            new()
            {
                Name = "Sheriff's Deputy",
                Concept = "Law enforcer of the Danse Macabre",
                Size = 5,
                Health = 9,
                Willpower = 6,
                BludgeoningArmor = 1,
                LethalArmor = 0,
                AttributesJson = "{\"Strength\":4,\"Dexterity\":3,\"Stamina\":4,\"Intelligence\":2,\"Wits\":3,\"Resolve\":3,\"Presence\":3,\"Manipulation\":2,\"Composure\":3}",
                SkillsJson = "{\"Brawl\":3,\"Athletics\":3,\"Intimidation\":3,\"Firearms\":2,\"Stealth\":2}",
                DisciplinesJson = "{\"Vigor\":2,\"Resilience\":2}",
                Notes = "Enforcer of Elysium law. Authorized to use violence. Reports directly to the Sheriff.",
                IsPrebuilt = true,
            },
            new()
            {
                Name = "Primogen",
                Concept = "Covenant representative on the council",
                Size = 5,
                Health = 8,
                Willpower = 7,
                BludgeoningArmor = 1,
                LethalArmor = 0,
                AttributesJson = "{\"Strength\":3,\"Dexterity\":3,\"Stamina\":3,\"Intelligence\":4,\"Wits\":4,\"Resolve\":4,\"Presence\":4,\"Manipulation\":4,\"Composure\":4}",
                SkillsJson = "{\"Persuasion\":4,\"Subterfuge\":4,\"Politics\":4,\"Empathy\":3,\"Intimidation\":3}",
                DisciplinesJson = "{\"Dominate\":3,\"Auspex\":2}",
                Notes = "Political powerhouse. Rarely acts in person — prefers proxies and leverage.",
                IsPrebuilt = true,
            },
            new()
            {
                Name = "Hound",
                Concept = "Prince's investigator and executioner",
                Size = 5,
                Health = 9,
                Willpower = 7,
                BludgeoningArmor = 2,
                LethalArmor = 1,
                AttributesJson = "{\"Strength\":4,\"Dexterity\":4,\"Stamina\":4,\"Intelligence\":3,\"Wits\":4,\"Resolve\":3,\"Presence\":3,\"Manipulation\":3,\"Composure\":4}",
                SkillsJson = "{\"Brawl\":4,\"Athletics\":3,\"Stealth\":3,\"Investigation\":3,\"Intimidation\":3,\"Firearms\":2}",
                DisciplinesJson = "{\"Celerity\":3,\"Vigor\":2,\"Auspex\":2}",
                Notes = "Operates on the Prince's authority. Combines investigator and enforcer roles.",
                IsPrebuilt = true,
            },
            new()
            {
                Name = "Revenant",
                Concept = "Dhampir-like hereditary ghoul",
                Size = 5,
                Health = 9,
                Willpower = 5,
                BludgeoningArmor = 1,
                LethalArmor = 0,
                AttributesJson = "{\"Strength\":4,\"Dexterity\":3,\"Stamina\":4,\"Intelligence\":2,\"Wits\":3,\"Resolve\":3,\"Presence\":2,\"Manipulation\":2,\"Composure\":3}",
                SkillsJson = "{\"Brawl\":3,\"Athletics\":2,\"Stealth\":2,\"Survival\":2}",
                DisciplinesJson = "{\"Vigor\":2,\"Resilience\":1}",
                Notes = "Born of a ghoul bloodline. Generates own vitae slowly. More feral than a standard ghoul.",
                IsPrebuilt = true,
            },
            new()
            {
                Name = "Blood Doll",
                Concept = "Willing human vessel",
                Size = 5,
                Health = 7,
                Willpower = 2,
                BludgeoningArmor = 0,
                LethalArmor = 0,
                AttributesJson = "{\"Strength\":2,\"Dexterity\":2,\"Stamina\":2,\"Intelligence\":2,\"Wits\":2,\"Resolve\":1,\"Presence\":3,\"Manipulation\":2,\"Composure\":1}",
                SkillsJson = "{\"Persuasion\":1,\"Socialize\":2,\"Expression\":1}",
                DisciplinesJson = "{}",
                Notes = "Willingly seeks out vampires for feeding. Low Resolve and Composure — easily Dominated or addicted to the Kiss.",
                IsPrebuilt = true,
            },
            new()
            {
                Name = "Thin-Blood",
                Concept = "Fifteenth generation vampire",
                Size = 5,
                Health = 7,
                Willpower = 4,
                BludgeoningArmor = 0,
                LethalArmor = 0,
                AttributesJson = "{\"Strength\":2,\"Dexterity\":2,\"Stamina\":2,\"Intelligence\":2,\"Wits\":3,\"Resolve\":2,\"Presence\":2,\"Manipulation\":3,\"Composure\":2}",
                SkillsJson = "{\"Athletics\":1,\"Stealth\":2,\"Streetwise\":2,\"Subterfuge\":2}",
                DisciplinesJson = "{\"Thin-Blood Alchemy\":1}",
                Notes = "Blood Potency 0. Weak vampire — not truly Kindred. Can walk in daylight with effort. Often hunted by the city's enforcers.",
                IsPrebuilt = true,
            },
            new()
            {
                Name = "Hunter (Witch)",
                Concept = "Occult vampire hunter",
                Size = 5,
                Health = 7,
                Willpower = 7,
                BludgeoningArmor = 0,
                LethalArmor = 0,
                AttributesJson = "{\"Strength\":2,\"Dexterity\":3,\"Stamina\":2,\"Intelligence\":4,\"Wits\":4,\"Resolve\":4,\"Presence\":2,\"Manipulation\":3,\"Composure\":4}",
                SkillsJson = "{\"Occult\":4,\"Investigation\":3,\"Crafts\":3,\"Stealth\":2,\"Firearms\":2,\"Athletics\":2}",
                DisciplinesJson = "{}",
                Notes = "Mortal hunter with occult expertise. Carries crafted wards, blessed rounds, or alchemical weapons. High Resolve makes Dominate and Majesty harder to land.",
                IsPrebuilt = true,
            },
        ];

        await context.NpcStatBlocks.AddRangeAsync(blocks);
        await context.SaveChangesAsync();
    }
}
