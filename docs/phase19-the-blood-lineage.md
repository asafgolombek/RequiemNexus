# Phase 19 — The Blood Lineage: Discipline Acquisition Rules & Seed Pipeline

> _"The blood does not forget its origins. Neither should the engine."_

## Overview

Phase 19 does two things that are tightly coupled:

1. **Fixes the broken seed pipeline** — `Disciplines.json` exists in `SeedSource/` but is never read; `DisciplineSeedData.cs` is the actual seed and contains no acquisition metadata.
2. **Enforces the acquisition rules from `DisciplinesRules.txt`** — the engine currently validates only XP and in-clan status; it knows nothing about teachers, Covenant gates, Humanity floors, or bloodline restrictions.

This phase also adds `DisciplinePower.PoolDefinitionJson`, which is the single blocker for **Phase 16b** (Discipline Activation).

**Phase 17** (Humanity & Conditions) is independent and should proceed in parallel on a separate branch. See the coordination note in Group C.

---

## Dependency Graph

```
Phase 19 (this phase)
    └──► Phase 16b: Discipline Activation (needs PoolDefinitionJson)

Phase 17: Humanity & Conditions (independent — parallel branch)
    ← coordination point: DegenerationCheckRequiredEvent and IHumanityService
      must not be duplicated; Phase 19 defines them, Phase 17 consumes them.
      If Phase 17 lands first, it owns the definitions; Phase 19 adds CrúacPurchase
      reason and Crúac cap only.
```

---

## Execution Order

Tasks must be executed in the order listed. Each group can be reviewed and merged independently.

```
Group A — Data Model & Migration (unblocks all downstream work)
  A1. Add acquisition metadata to Discipline entity
  A2. Add PoolDefinitionJson to DisciplinePower entity
  A3. Generate migration Phase19DisciplineAcquisitionMetadata

Group B — Seed Pipeline (depends on A)
  B1. Extend Disciplines.json schema
  B2. Add DisciplineSeedData.LoadFromDocs() — initial seed, bools only (no FKs)
  B3. Add UpdateDisciplineAcquisitionMetadataAsync — second-pass FK resolution
  B4. Wire both methods into DbInitializer in correct order
  B5. Fix Celerity / Resilience / Vigor power names in JSON

Group C — Domain Infrastructure (depends on A; can be done alongside B)
  ⚠ Coordination point with Phase 17 — see note below
  C1. DegenerationCheckRequiredEvent + DegenerationReason enum
  C2. IHumanityService + HumanityService
  C3. DisciplineAcquisitionRequest DTO

Group D — Gate Enforcement (depends on B, C)
  D1. ICharacterDisciplineService: update method signatures
  D2. CharacterDisciplineService: hard gate — bloodline restriction
  D3. CharacterDisciplineService: hard gate — Covenant Status (ST-overridable)
  D4. CharacterDisciplineService: hard gate — Theban Humanity floor
  D5. CharacterDisciplineService: soft gate — out-of-clan teacher + Vitae
  D6. CharacterDisciplineService: Crúac breaking point event
  D7. CharacterDisciplineService: soft gate — Necromancy cultural connection
  D8. Soft gate audit trail in XpLedgerEntry.Notes
  D9. ST-role verification for AcquisitionAcknowledgedByST
  D10. DegenerationCheckRequiredEventHandler + DI registration

Group E — Character Creation Validation (depends on B, C)
  E1. ICharacterCreationService + CharacterCreationService (2-of-3 in-clan rule)
  E2. Creation UI: validation error surfaced inline

Group F — UI (depends on D, E)
  F1. Advancement page: hard gate tooltips + ST confirmation modal for soft gates
  F2. Character sheet: power pool formula display (PoolDefinitionJson populated)
  F3. Character sheet: Crúac Humanity cap badge

Group G — Tests & Documentation
  G1. CharacterDisciplineServiceTests — gate tests (one per gate, D2–D9)
  G2. HumanityServiceTests — cap calculation + stain threshold
  G3. CharacterCreationServiceTests — 2-of-3 rule
  G4. Rules Interpretation Log entries
  G5. dotnet format + .\scripts\test-local.ps1
```

---

## Group A — Data Model & Migration

### A1. `Discipline` entity

**File:** `src/RequiemNexus.Data/Models/Discipline.cs`

Add the following properties. Booleans default to `false`; FK columns are nullable:

```csharp
/// <summary>Gets or sets a value indicating whether this Discipline can be learned without a teacher.</summary>
public bool CanLearnIndependently { get; set; }

/// <summary>Gets or sets a value indicating whether learning this Discipline out-of-clan requires drinking the mentor's Vitae.</summary>
public bool RequiresMentorBloodToLearn { get; set; }

/// <summary>Gets or sets a value indicating whether this Discipline is restricted to a specific Covenant.</summary>
public bool IsCovenantDiscipline { get; set; }

/// <summary>Gets or sets the Covenant that gates access to this Discipline. Null unless IsCovenantDiscipline is true.</summary>
public int? CovenantId { get; set; }

/// <summary>Gets or sets the CovenantDefinition navigation property.</summary>
public virtual CovenantDefinition? Covenant { get; set; }

/// <summary>Gets or sets a value indicating whether this Discipline is restricted to a specific Bloodline.</summary>
public bool IsBloodlineDiscipline { get; set; }

/// <summary>Gets or sets the Bloodline that gates access to this Discipline. Null unless IsBloodlineDiscipline is true.</summary>
public int? BloodlineId { get; set; }

/// <summary>Gets or sets the BloodlineDefinition navigation property.</summary>
public virtual BloodlineDefinition? Bloodline { get; set; }

/// <summary>Gets or sets a value indicating whether this Discipline is Necromancy (requires Mekhet-clan, Necromancy-linked bloodline, or ST acknowledgment).</summary>
public bool IsNecromancy { get; set; }
```

### A2. `DisciplinePower` entity

**File:** `src/RequiemNexus.Data/Models/DisciplinePower.cs`

Add one nullable column:

```csharp
/// <summary>
/// Gets or sets the serialized PoolDefinition for this power's dice pool.
/// Uses the same contract as DevotionDefinition.PoolDefinitionJson.
/// Null for powers that have no rollable pool (passive or narrative-only).
/// Phase 16b reads this column to resolve the dice pool on activation.
/// </summary>
public string? PoolDefinitionJson { get; set; }
```

### A3. Migration

Generate via:

```powershell
dotnet ef migrations add Phase19DisciplineAcquisitionMetadata `
  --project src/RequiemNexus.Data `
  --startup-project src/RequiemNexus.Web
```

The migration adds:
- 5 bool columns + 2 nullable int FK columns to `Disciplines` table
- 2 FK indexes (on `CovenantId`, `BloodlineId`)
- 1 nullable text column (`PoolDefinitionJson`) to `DisciplinePowers` table

All new columns have SQL defaults (`false` / `NULL`), so existing rows remain valid.

---

## Group B — Seed Pipeline

### Seeding Order Context

`DbInitializer.InitializeAsync` calls methods in this order (lines 22–34):
1. `SeedRolesAsync`
2. **`SeedClansAndDisciplinesAsync`** ← disciplines seed here
3. `SeedHuntingPoolDefinitionsAsync`
4. `SeedMeritsAsync`
5. `SeedEquipmentCatalogAsync`
6. `SeedCovenantsAsync` ← covenants seed here
7. `SeedCovenantDefinitionMeritsAsync`
8. **`SeedBloodlinesAsync`** ← bloodlines seed here
9. `SeedDevotionsAsync`
10. `SeedSorceryRitesAsync`
11. `EnsureBloodSorceryPhaseExtensionsAsync`
12. `SeedCoilsAsync`
13. `SeedPrebuiltStatBlocksAsync`

**Key implication:** When disciplines are seeded (step 2), covenants and bloodlines do not yet exist in the DB. Therefore the `CovenantId` and `BloodlineId` FK columns on `Discipline` **cannot** be populated at initial seed time. Two separate passes are required:

- **Pass 1 (B2):** `SeedClansAndDisciplinesAsync` — sets booleans + `PoolDefinitionJson` only; leaves `CovenantId` and `BloodlineId` null.
- **Pass 2 (B3):** `UpdateDisciplineAcquisitionMetadataAsync` — runs after step 8; resolves covenant and bloodline names to IDs and updates existing rows.

### B1. `Disciplines.json` Schema Extension

**File:** `src/RequiemNexus.Data/SeedSource/Disciplines.json`

Extend each object with acquisition metadata. Covenant and bloodline references use string names (resolved in Pass 2). Powers gain `poolDefinitionJson`.

**Full schema for one entry:**

```json
{
  "name": "Animalism",
  "canLearnIndependently": true,
  "requiresMentorBloodToLearn": false,
  "isCovenantDiscipline": false,
  "covenantName": null,
  "isBloodlineDiscipline": false,
  "bloodlineName": null,
  "isNecromancy": false,
  "powers": [
    {
      "name": "Feral Whispers",
      "ranking": 1,
      "description": "...",
      "roll": "Wits + Animal Ken + Animalism",
      "cost": "—",
      "poolDefinitionJson": null
    }
  ]
}
```

**Covenant names must exactly match** the `CovenantDefinition.Name` seed values (with the `"The "` prefix):
- Crúac: `"covenantName": "The Circle of the Crone"`
- Theban Sorcery: `"covenantName": "The Lancea et Sanctum"`

**Acquisition metadata per discipline:**

| Discipline | `canLearnIndependently` | `requiresMentorBloodToLearn` | `isCovenantDiscipline` | `isNecromancy` |
|---|---|---|---|---|
| Animalism | `true` | `false` | `false` | `false` |
| Auspex | `false` | `true` | `false` | `false` |
| Celerity | `true` | `false` | `false` | `false` |
| Dominate | `false` | `true` | `false` | `false` |
| Majesty | `false` | `true` | `false` | `false` |
| Nightmare | `false` | `true` | `false` | `false` |
| Obfuscate | `true` | `false` | `false` | `false` |
| Protean | `false` | `true` | `false` | `false` |
| Resilience | `true` | `false` | `false` | `false` |
| Vigor | `true` | `false` | `false` | `false` |
| Crúac | `false` | `false` | `true` | `false` |
| Theban Sorcery | `false` | `false` | `true` | `false` |
| Necromancy | `false` | `false` | `false` | `true` |

**`poolDefinitionJson` values:** Populate where the rulebook gives a clear pool formula. Leave `null` for powers with no roll. Use the same serialization format as `DevotionDefinition.PoolDefinitionJson` (see `DevotionSeedData.cs` `TryParseDicePool` for the `TraitReference` schema).

All missing JSON keys default to `false` via `ReadBool` (explicit `true`/`false` required; no implicit defaults for new fields).

### B2. `DisciplineSeedData.LoadFromDocs()` — Pass 1

**File:** `src/RequiemNexus.Data/SeedData/DisciplineSeedData.cs`

Add a static method that reads booleans and `PoolDefinitionJson` only. No covenant/bloodline FK resolution at this stage:

```csharp
/// <summary>
/// Loads Discipline definitions from Disciplines.json.
/// Sets boolean acquisition flags and PoolDefinitionJson.
/// CovenantId and BloodlineId are left null — resolved in a second pass by
/// UpdateDisciplineAcquisitionMetadataAsync after covenants and bloodlines are seeded.
/// Falls back to GetAll() if the file is missing or unparseable.
/// </summary>
public static List<Discipline> LoadFromDocs(ILogger logger)
{
    using JsonDocument? doc = SeedDataLoader.TryLoadJson("Disciplines.json", logger);
    if (doc == null)
        return GetAll();

    var result = new List<Discipline>();
    foreach (JsonElement el in doc.RootElement.EnumerateArray())
    {
        string name = el.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
        if (string.IsNullOrWhiteSpace(name)) continue;

        var discipline = new Discipline
        {
            Name = name,
            Description = el.TryGetProperty("description", out var d) ? d.GetString() ?? "" : "",
            CanLearnIndependently = ReadBool(el, "canLearnIndependently"),
            RequiresMentorBloodToLearn = ReadBool(el, "requiresMentorBloodToLearn"),
            IsCovenantDiscipline = ReadBool(el, "isCovenantDiscipline"),
            IsBloodlineDiscipline = ReadBool(el, "isBloodlineDiscipline"),
            IsNecromancy = ReadBool(el, "isNecromancy"),
            // CovenantId and BloodlineId intentionally left null — see UpdateDisciplineAcquisitionMetadataAsync
            Powers = [],
        };

        if (el.TryGetProperty("powers", out var powers))
        {
            int rank = 0;
            foreach (JsonElement p in powers.EnumerateArray())
            {
                rank++;
                string powerName = p.TryGetProperty("name", out var pn) ? pn.GetString() ?? $"{name} {rank}" : $"{name} {rank}";
                int level = p.TryGetProperty("ranking", out var rv) && rv.TryGetInt32(out int ri) ? ri : rank;
                string pool = p.TryGetProperty("roll", out var pr) ? pr.GetString() ?? "" : "";
                string cost = p.TryGetProperty("cost", out var pc) ? pc.GetString() ?? "—" : "—";
                string? poolJson = p.TryGetProperty("poolDefinitionJson", out var pj) && pj.ValueKind != JsonValueKind.Null
                    ? pj.GetString()
                    : null;

                discipline.Powers.Add(new DisciplinePower
                {
                    Level = level,
                    Name = powerName,
                    Description = p.TryGetProperty("description", out var pd) ? pd.GetString() ?? "" : "",
                    DicePool = pool,
                    Cost = cost,
                    PoolDefinitionJson = poolJson,
                });
            }
        }

        result.Add(discipline);
    }

    return result.Count > 0 ? result : GetAll();
}

// Private to DisciplineSeedData — DbInitializer has its own copy (see B3).
private static bool ReadBool(JsonElement el, string propertyName) =>
    el.TryGetProperty(propertyName, out var p) && p.ValueKind == JsonValueKind.True;
```

### B3. `UpdateDisciplineAcquisitionMetadataAsync` — Pass 2

**File:** `src/RequiemNexus.Data/DbInitializer.cs`

Add a new private static method that runs **after step 8 (`SeedBloodlinesAsync`)** and syncs **all** Phase 19 fields from `Disciplines.json` onto existing non-homebrew discipline rows: booleans, `CovenantId`, `BloodlineId`, and `DisciplinePower.PoolDefinitionJson`. This covers both fresh installs and existing databases — migration defaults all new columns to `false`/`null`, which would leave Animalism's `CanLearnIndependently` wrong on an upgraded DB without this pass.

The method is idempotent — it updates every matching row unconditionally (values are deterministic from JSON).

```csharp
/// <summary>
/// Second-pass sync: applies all Phase 19 acquisition metadata from Disciplines.json
/// onto non-homebrew Discipline rows and their child DisciplinePower rows.
/// Runs after covenants and bloodlines are seeded so FK lookups succeed.
/// Runs unconditionally — safe on fresh installs and existing databases alike.
/// </summary>
private static async Task UpdateDisciplineAcquisitionMetadataAsync(
    ApplicationDbContext context, ILogger logger)
{
    using JsonDocument? doc = SeedDataLoader.TryLoadJson("Disciplines.json", logger);
    if (doc == null) return;

    Dictionary<string, int> covenantByName = await context.CovenantDefinitions
        .AsNoTracking()
        .ToDictionaryAsync(c => c.Name, c => c.Id, StringComparer.OrdinalIgnoreCase);

    Dictionary<string, int> bloodlineByName = await context.BloodlineDefinitions
        .AsNoTracking()
        .ToDictionaryAsync(b => b.Name, b => b.Id, StringComparer.OrdinalIgnoreCase);

    bool anyChanged = false;
    foreach (JsonElement el in doc.RootElement.EnumerateArray())
    {
        string name = el.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
        if (string.IsNullOrWhiteSpace(name)) continue;

        Discipline? discipline = await context.Disciplines
            .Include(d => d.Powers)
            .FirstOrDefaultAsync(d => d.Name == name && !d.IsHomebrew);
        if (discipline == null) continue;

        // -- booleans --
        discipline.CanLearnIndependently = ReadBool(el, "canLearnIndependently");
        discipline.RequiresMentorBloodToLearn = ReadBool(el, "requiresMentorBloodToLearn");
        discipline.IsCovenantDiscipline = ReadBool(el, "isCovenantDiscipline");
        discipline.IsBloodlineDiscipline = ReadBool(el, "isBloodlineDiscipline");
        discipline.IsNecromancy = ReadBool(el, "isNecromancy");

        // -- FK resolution --
        discipline.CovenantId = el.TryGetProperty("covenantName", out var cov) &&
            cov.ValueKind == JsonValueKind.String &&
            covenantByName.TryGetValue(cov.GetString()!, out int cId) ? cId : null;

        discipline.BloodlineId = el.TryGetProperty("bloodlineName", out var bl) &&
            bl.ValueKind == JsonValueKind.String &&
            bloodlineByName.TryGetValue(bl.GetString()!, out int bId) ? bId : null;

        // -- child powers: PoolDefinitionJson --
        if (el.TryGetProperty("powers", out var powers))
        {
            foreach (JsonElement p in powers.EnumerateArray())
            {
                string powerName = p.TryGetProperty("name", out var pn) ? pn.GetString() ?? "" : "";
                if (string.IsNullOrWhiteSpace(powerName)) continue;

                DisciplinePower? power = discipline.Powers
                    .FirstOrDefault(pw => string.Equals(pw.Name, powerName, StringComparison.OrdinalIgnoreCase));
                if (power == null) continue;

                power.PoolDefinitionJson = p.TryGetProperty("poolDefinitionJson", out var pj) &&
                    pj.ValueKind != JsonValueKind.Null ? pj.GetString() : null;
            }
        }

    }

    // EF tracks only properties that actually changed value.
    // ChangeTracker.HasChanges() is false on a fully-synced DB, so this is a no-op on every
    // subsequent startup — no unnecessary write occurs.
    if (context.ChangeTracker.HasChanges())
        await context.SaveChangesAsync();
}

private static bool ReadBool(JsonElement el, string propertyName) =>
    el.TryGetProperty(propertyName, out var p) && p.ValueKind == JsonValueKind.True;
```

> **Note:** `ReadBool` can be defined as a local static helper at the bottom of `DbInitializer.cs` (or as an extension), since `DisciplineSeedData.cs` has its own private copy.

### B4. Wire Into `DbInitializer`

**File:** `src/RequiemNexus.Data/DbInitializer.cs`

**Two changes:**

1. In `SeedClansAndDisciplinesAsync` — switch `DisciplineSeedData.GetAll()` to `DisciplineSeedData.LoadFromDocs(logger)`. Pass `logger` through (add the parameter).

   The guard condition `!hasClansAndDisciplines` remains — this method only runs for empty tables. The existing pattern checks both `Clans` and `Disciplines`:
   ```csharp
   bool hasClansAndDisciplines = await context.Clans.AnyAsync() && await context.Disciplines.AnyAsync();
   if (!hasClansAndDisciplines) { ... }
   ```

2. Add a call to `UpdateDisciplineAcquisitionMetadataAsync` **after** `SeedBloodlinesAsync` (step 8):
   ```csharp
   await SeedBloodlinesAsync(context, logger);
   await UpdateDisciplineAcquisitionMetadataAsync(context, logger); // ← new, step 8.5
   await SeedDevotionsAsync(context, logger);
   ```

This second-pass approach correctly handles both **fresh installs** and **existing databases** — the update is by-name and idempotent regardless of whether disciplines were just seeded or existed for months.

### B5. Fix Power Names

In `Disciplines.json`, replace placeholder power names for Celerity, Resilience, and Vigor with VtR 2e core rulebook names. Verify names against the physical book before commit; record source page in the commit message.

---

## Group C — Domain Infrastructure

> **⚠ Phase 17 coordination:** `DegenerationCheckRequiredEvent`, `DegenerationReason`, and `IHumanityService` must be defined once. If **Phase 17 lands before Phase 19**, these already exist — Phase 19 only adds `DegenerationReason.CrúacPurchase` and calls `GetEffectiveMaxHumanity`. If **Phase 19 lands first**, Phase 17 picks up the existing definitions and adds the stain-threshold wiring. Whoever lands second: grep for these names before creating them.

### C1. `DegenerationCheckRequiredEvent` + `DegenerationReason`

**File:** `src/RequiemNexus.Domain/Events/DegenerationCheckRequiredEvent.cs`

```csharp
namespace RequiemNexus.Domain.Events;

/// <summary>Reasons that can trigger a degeneration check for a character.</summary>
public enum DegenerationReason
{
    /// <summary>Humanity stains have crossed the threshold for the current Humanity dot.</summary>
    StainsThreshold,

    /// <summary>The character has purchased their first dot of Crúac at Humanity 4 or higher.</summary>
    CrúacPurchase,
}

/// <summary>
/// Raised when a character requires a degeneration (Resolve + (7 − Humanity)) check.
/// Phase 17 handles StainsThreshold; Phase 19 raises CrúacPurchase.
/// </summary>
public record DegenerationCheckRequiredEvent(int CharacterId, DegenerationReason Reason);
```

### C2. `IHumanityService` + `HumanityService`

**File:** `src/RequiemNexus.Application/Contracts/IHumanityService.cs`

```csharp
namespace RequiemNexus.Application.Contracts;

/// <summary>Manages Humanity tracking, Crúac caps, and degeneration triggers.</summary>
public interface IHumanityService
{
    /// <summary>
    /// Returns the effective maximum Humanity for a character.
    /// Crúac permanently caps Humanity at 10 − CrúacRating.
    /// </summary>
    int GetEffectiveMaxHumanity(Character character);

    /// <summary>
    /// Evaluates whether the character's current stains cross the degeneration threshold
    /// for their Humanity dot. If so, raises <see cref="DegenerationCheckRequiredEvent"/>.
    /// </summary>
    Task EvaluateStainsAsync(int characterId, string userId);
}
```

**File:** `src/RequiemNexus.Application/Services/HumanityService.cs`

Key implementation:
- `GetEffectiveMaxHumanity`: `return 10 - character.GetDisciplineRating("Crúac");` — uses the existing `Character.GetDisciplineRating(string name)` helper.
- `EvaluateStainsAsync`: load character, check `HumanityStains >= character.Humanity`, dispatch `DegenerationCheckRequiredEvent(characterId, DegenerationReason.StainsThreshold)` via `IDomainEventDispatcher`.
- Apply `IAuthorizationHelper.RequireCharacterAccessAsync` before any read.

### C3. `DisciplineAcquisitionRequest` DTO

**File:** `src/RequiemNexus.Application/DTOs/DisciplineAcquisitionRequest.cs`

```csharp
namespace RequiemNexus.Application.DTOs;

/// <summary>
/// Carries the parameters for a Discipline purchase or upgrade.
/// Replaces bare (characterId, disciplineId, rating) parameters on ICharacterDisciplineService.
/// </summary>
public sealed record DisciplineAcquisitionRequest(
    int CharacterId,
    int DisciplineId,
    int TargetRating,

    /// <summary>
    /// When true, the Storyteller has verbally acknowledged a soft gate requirement
    /// (out-of-clan teacher, Covenant access "stolen secrets", Necromancy cultural connection).
    /// The Application layer must verify the requesting user holds the Storyteller role
    /// for the character's campaign before this flag is honoured — see D9.
    /// Hard gates (bloodline restriction, Theban Humanity floor) are never bypassed.
    /// </summary>
    bool AcquisitionAcknowledgedByST = false);
```

---

## Group D — Gate Enforcement

### D1. `ICharacterDisciplineService` — Update Signatures

**File:** `src/RequiemNexus.Application/Contracts/ICharacterDisciplineService.cs`

Replace `AddDisciplineAsync` and `TryUpgradeDisciplineAsync` with `Result<T>`-returning versions that accept `DisciplineAcquisitionRequest`:

```csharp
/// <summary>Adds a new Discipline at the given rating, enforcing all acquisition gates.</summary>
Task<Result<CharacterDiscipline>> AddDisciplineAsync(
    DisciplineAcquisitionRequest request, string userId);

/// <summary>Upgrades an existing Discipline to the target rating, enforcing all acquisition gates.</summary>
Task<Result<CharacterDiscipline>> TryUpgradeDisciplineAsync(
    DisciplineAcquisitionRequest request, string userId);
```

Both return `Result<CharacterDiscipline>`. Hard gate failures return `Result.Failure(message)`. Soft gate failures also return `Result.Failure` unless `AcquisitionAcknowledgedByST = true` (and the ST role check in D9 passes).

### D2–D8. Gate Implementations in `CharacterDisciplineService`

**File:** `src/RequiemNexus.Application/Services/CharacterDisciplineService.cs`

Add a private `ValidateAcquisitionGatesAsync` method. Return the first `Result.Failure` encountered. On success, proceed with XP deduction and character update.

**Gate order and logic:**

```
Gate 1 — Bloodline restriction (HARD, never bypassed)
  if (discipline.IsBloodlineDiscipline)
    require character.Bloodlines.Any(b => b.BloodlineDefinitionId == discipline.BloodlineId)
    → Result.Failure("This Discipline is restricted to members of the {bloodlineName} bloodline.")

Gate 2 — Covenant Status (HARD gate, soft bypass with ST acknowledgment)
  if (discipline.IsCovenantDiscipline)
    check character has active CovenantMembership matching discipline.CovenantId
    if not and !request.AcquisitionAcknowledgedByST:
      → Result.Failure("Covenant Status in {covenantName} is required. A Storyteller may override for 'stolen secrets'.")
    if not and request.AcquisitionAcknowledgedByST:
      → append audit note (see D8)

Gate 3 — Theban Humanity floor (HARD, never bypassed)
  if discipline is Theban Sorcery (IsCovenantDiscipline && CovenantId matches Lancea et Sanctum)
    if request.TargetRating > character.Humanity:
      → Result.Failure("Theban Sorcery •{targetRating} requires Humanity {targetRating} or higher.")

Gate 4 — Out-of-clan teacher + Vitae (SOFT)
  if (discipline.RequiresMentorBloodToLearn && !character.IsDisciplineInClan(disciplineId))
    if !request.AcquisitionAcknowledgedByST:
      → Result.Failure("Out-of-clan Disciplines require a teacher and must drink their Vitae. Storyteller must acknowledge.")
    else:
      → append audit note (see D8)

Gate 5 — Crúac breaking point (EVENT, not a blocking gate)
  if discipline.IsCovenantDiscipline
     && discipline.Name == "Crúac" (or match by CovenantId == Circle-id and this is the sorcery track)
     && character.GetDisciplineRating("Crúac") == 0
     && character.Humanity >= 4:
    _domainEventDispatcher.Dispatch(new DegenerationCheckRequiredEvent(characterId, DegenerationReason.CrúacPurchase))
  Purchase proceeds — the event triggers an async ST check, not a purchase block.

Gate 6 — Necromancy cultural connection (SOFT)
  if (discipline.IsNecromancy)
    bool isMekhet = character.Clan?.Name == "Mekhet"
    // BloodlineDefinition links to a Discipline via FourthDisciplineId.
    // A character qualifies if any of their active bloodlines has FourthDisciplineId == discipline.Id.
    bool hasNecromancyBloodline = character.Bloodlines
        .Any(cb => cb.BloodlineDefinition?.FourthDisciplineId == discipline.Id)
    if (!isMekhet && !hasNecromancyBloodline)
      if !request.AcquisitionAcknowledgedByST:
        → Result.Failure("Necromancy requires Mekhet-clan membership, a Necromancy-linked bloodline, or ST-acknowledged cultural connection.")
      else:
        → append audit note (see D8)
```

**Discipline identity strategy:** Prefer **ID comparison** wherever possible (covenant ID, bloodline FourthDisciplineId). Use `discipline.Name` string comparison only for Crúac-specific logic (Gate 5) where the name is stable and canonical. Document the string used in `rules-interpretations.md` to prevent accent/locale drift.

**Crúac Humanity cap enforcement:** When Crúac is purchased and `GetEffectiveMaxHumanity(character) < character.Humanity`, **clamp `character.Humanity`** to the cap at purchase time. Invalid state must not persist in the DB. Record this behavior in `rules-interpretations.md`.

### D8. Soft Gate Audit Trail

When a soft gate is bypassed (`AcquisitionAcknowledgedByST = true`), append to `XpLedgerEntry.Notes` on the spend record:

```
" | gate-override:{gate-name} stUserId={userId} {timestamp:O}"
```

Where `{gate-name}` is one of `covenant`, `teacher`, `necromancy`. ISO 8601 timestamp. Include the gate name for auditing without needing to parse surrounding text.

### D9. ST-Role Verification for `AcquisitionAcknowledgedByST`

**This is a security gate, not a soft feature.** A player must not be able to bypass acquisition rules by submitting `AcquisitionAcknowledgedByST = true` from the client.

`IAuthorizationHelper` only exposes throwing methods (`RequireStorytellerAsync` throws on failure). Do **not** use try/catch for control flow here. Add a non-throwing predicate to the interface and implementation:

**File:** `src/RequiemNexus.Application/Contracts/IAuthorizationHelper.cs`
```csharp
/// <summary>Returns true if the user is the Storyteller for the given campaign. Does not throw.</summary>
Task<bool> IsStorytellerAsync(int campaignId, string userId);
```

**File:** `src/RequiemNexus.Application/Services/AuthorizationHelper.cs`
```csharp
public async Task<bool> IsStorytellerAsync(int campaignId, string userId)
{
    await using ApplicationDbContext dbContext = await _dbContextFactory.CreateDbContextAsync();
    return await dbContext.Campaigns
        .AnyAsync(c => c.Id == campaignId && c.StoryTellerId == userId);
}
```

This reuses the same query already inside `RequireStorytellerAsync` — no new DB logic.

In `ValidateAcquisitionGatesAsync`, resolve the flag before any gate runs:

```csharp
// character.CampaignId is nullable. Unassigned characters cannot have ST acknowledgment.
bool stAcknowledged = request.AcquisitionAcknowledgedByST
    && character.CampaignId.HasValue
    && await _authorizationHelper.IsStorytellerAsync(character.CampaignId.Value, userId);
```

Use `stAcknowledged` (not `request.AcquisitionAcknowledgedByST`) in all subsequent gate logic. If `character.CampaignId` is null, `stAcknowledged` is `false` — soft gate bypass is unavailable until the character is assigned to a campaign.

### D10. `DegenerationCheckRequiredEventHandler` + DI Registration

**File:** `src/RequiemNexus.Application/Events/Handlers/DegenerationCheckRequiredEventHandler.cs`

Phase 19 scope: log the event. Phase 17 will wire the full ST banner + roll UI. Omit `ISessionService` from the constructor until Phase 17 adds the notification logic to avoid unused-parameter analyzer noise:

```csharp
public sealed class DegenerationCheckRequiredEventHandler(
    ILogger<DegenerationCheckRequiredEventHandler> logger)
    : IDomainEventHandler<DegenerationCheckRequiredEvent>
{
    public void Handle(DegenerationCheckRequiredEvent domainEvent)
    {
        logger.LogInformation(
            "Degeneration check required for Character {CharacterId}. Reason: {Reason}",
            domainEvent.CharacterId,
            domainEvent.Reason);
        // Phase 17 wires the ST Glimpse banner and roll UI here.
    }
}
```

**DI registration in** `src/RequiemNexus.Web/Extensions/ApplicationServiceExtensions.cs`:

```csharp
services.AddScoped<IDomainEventHandler<DegenerationCheckRequiredEvent>, DegenerationCheckRequiredEventHandler>();
services.AddScoped<IHumanityService, HumanityService>();
services.AddScoped<ICharacterCreationService, CharacterCreationService>();
```

---

## Group E — Character Creation Discipline Validation

### Scope Note: Coils vs. Discipline Dots

The "3 creation dots, ≥2 must be in-clan" rule applies to **`CharacterDiscipline` rows only**. Coils of the Dragon are `CharacterCoil` entities managed by `CoilService` — they are not Discipline dots in this context. The creation validation in E1 does not touch Coils; the existing covenant-status prompt for Coils flows through `CoilService`.

The "third creation dot targeting Coils" covenant gate is handled by the existing `SeedCoilsAsync` / coil purchase flow, not by `CharacterCreationService`.

### E1. `ICharacterCreationService` + `CharacterCreationService`

**File:** `src/RequiemNexus.Application/Contracts/ICharacterCreationService.cs`

```csharp
/// <summary>Validates discipline assignments during character creation.</summary>
public interface ICharacterCreationService
{
    /// <summary>
    /// Validates that at least 2 of the 3 creation Discipline dots are in-clan.
    /// Call this on each discipline change during the creation flow, and again on final submit.
    /// Returns Result.Failure with a descriptive message if the rule is violated.
    /// </summary>
    Result<bool> ValidateCreationDisciplines(Character character);
}
```

**File:** `src/RequiemNexus.Application/Services/CharacterCreationService.cs`

```csharp
public Result<bool> ValidateCreationDisciplines(Character character)
{
    int totalDots = character.Disciplines.Sum(d => d.Rating);
    if (totalDots < 3) return Result<bool>.Success(true); // not yet at creation minimum; validate on submit

    int inClanDots = character.Disciplines
        .Where(d => character.IsDisciplineInClan(d.DisciplineId))
        .Sum(d => d.Rating);

    int outOfClanDots = totalDots - inClanDots;
    if (outOfClanDots > 1)
        return Result<bool>.Failure(
            "At least 2 of your 3 starting Discipline dots must be in-clan Disciplines.");

    return Result<bool>.Success(true);
}
```

**Validation timing:** Run on each discipline change AND on final submit. The character object reflects only starting dots during creation (no XP-based dots yet). If creation is multi-step and dots are added incrementally, validate reactively but only block progression at the final submit step.

### E2. Creation UI

Surface the failure message from `ValidateCreationDisciplines` as an inline validation error below the discipline selector. Block the "Next" / "Create" button until resolved.

**Crúac / Theban at creation ("stolen secrets"):** If the creation flow allows assigning Crúac or Theban Sorcery as the third dot, the Covenant Status gate (D3) must be checked at creation time too — not only at Advancement. The creation UI should show the same ST acknowledgment modal (F1) if the character has no matching Covenant membership. This applies only when the discipline selector allows those disciplines as options during creation; if creation restricts the catalogue to non-covenant disciplines, this is moot. Verify against the creation flow implementation and document the chosen behaviour in `rules-interpretations.md`.

---

## Group F — UI

### F1. Advancement Page — Gate Feedback

**File:** Advancement Razor page

**Hard gate:** When `Result.Failure` is returned, show the error string as a `<div class="gate-error">` below the upgrade button. The button remains disabled while the gate is unsatisfied.

**Soft gate:** When `Result.Failure` message indicates ST acknowledgment is needed, show a `<dialog>` with:
- The full failure message (quoting the rule verbatim)
- "Storyteller Acknowledges" button → re-submits with `AcquisitionAcknowledgedByST = true`
- "Cancel" button

This modal is structurally identical to the existing Blood Sorcery sacrifice acknowledgment modal.

### F2. Character Sheet — Power Pool Display

**File:** Character sheet Disciplines section

For each `DisciplinePower` where `PoolDefinitionJson != null`:
- Resolve the pool via `ITraitResolver.ResolvePoolAsync(character, pool)` in `OnInitializedAsync`
- Display the resolved integer next to the power name: `"Feral Whispers — Pool: 6"`
- Reuse the same display pattern as Devotion pool display

For powers where `PoolDefinitionJson == null`: display the raw `DicePool` string (human-readable, not resolved).

### F3. Character Sheet — Crúac Humanity Cap Badge

**File:** Character sheet Humanity section

When `character.GetDisciplineRating("Crúac") > 0`:

```razor
@{
    int crucRating = character.GetDisciplineRating("Crúac");
    int effectiveMaxHumanity = _humanityService.GetEffectiveMaxHumanity(character);
    string dots = new string('•', crucRating);
}
<span class="cap-badge" title="Crúac permanently caps Humanity">
    Max Humanity: @effectiveMaxHumanity (capped by Crúac @dots)
</span>
```

Display the badge in red if `character.Humanity >= effectiveMaxHumanity`.

---

## Group G — Tests & Documentation

### G1. `CharacterDisciplineServiceTests` — Gate Tests

**File:** `tests/RequiemNexus.Application.Tests/CharacterDisciplineServiceTests.cs`

Add one test per gate. Follow the SQLite in-memory pattern from `HuntingServiceTests`.

Required tests:

| Test | Scenario | Assert |
|---|---|---|
| `AddDiscipline_BloodlineRestriction_Fails` | Character has no matching bloodline | `IsSuccess == false`, message mentions bloodline name |
| `AddDiscipline_BloodlineRestriction_CannotBypassWithST` | Same + `AcquisitionAcknowledgedByST = true` | Still `IsSuccess == false` (hard gate, no bypass) |
| `AddDiscipline_CovenantGate_NoMembership_Fails` | No matching covenant membership | `IsSuccess == false` |
| `AddDiscipline_CovenantGate_PlayerBypassAttempt_Fails` | Player submits `AcquisitionAcknowledgedByST = true` | `IsSuccess == false` (ST role check fails) |
| `AddDiscipline_CovenantGate_STBypass_Succeeds_WithAuditNote` | Verified ST submits with flag | `IsSuccess == true`; `XpLedgerEntry.Notes` contains `"gate-override:covenant"` |
| `AddDiscipline_ThebanFloor_Fails` | Theban Sorcery ••• with Humanity 2 | `IsSuccess == false`, message mentions Humanity |
| `AddDiscipline_ThebanFloor_CannotBypassWithST` | Same + `AcquisitionAcknowledgedByST = true` | Still `IsSuccess == false` (hard gate) |
| `AddDiscipline_TeacherGate_OutOfClan_Fails` | No ST acknowledgment | `IsSuccess == false` |
| `AddDiscipline_TeacherGate_STAcknowledged_Succeeds` | Verified ST submits with flag | `IsSuccess == true` |
| `AddDiscipline_Cruac_AtHumanity4_DispatchesDegenEvent` | First Crúac dot, Humanity = 4 | `DegenerationCheckRequiredEvent` dispatched with `CrúacPurchase` |
| `AddDiscipline_Cruac_BelowHumanity4_NoDegenEvent` | First Crúac dot, Humanity = 3 | Event not dispatched |
| `AddDiscipline_Necromancy_NotMekhet_NoBloodline_Fails` | Not Mekhet, no qualifying bloodline | `IsSuccess == false` |
| `AddDiscipline_Necromancy_MekhetClan_Succeeds` | Mekhet character | `IsSuccess == true` |
| `AddDiscipline_Necromancy_NecromancyBloodline_Succeeds` | Bloodline with `FourthDisciplineId == necromancyId` | `IsSuccess == true` |

> **Test fixture note for the Necromancy bloodline test:** Confirm that an existing seed bloodline in test DB has `FourthDisciplineId` pointing to the Necromancy discipline, or create a minimal fixture directly in the test (seed a `BloodlineDefinition` with `FourthDisciplineId = necromancyDiscipline.Id` and assign it to the test character).

### G2. `HumanityServiceTests`

**File:** `tests/RequiemNexus.Application.Tests/HumanityServiceTests.cs`

| Test | Scenario | Assert |
|---|---|---|
| `GetEffectiveMaxHumanity_NoCruac_Returns10` | No Crúac dots | `== 10` |
| `GetEffectiveMaxHumanity_CruacDot3_Returns7` | Crúac ••• | `== 7` |
| `EvaluateStains_BelowThreshold_NoEvent` | Stains = 1, Humanity = 7 | Event not dispatched |
| `EvaluateStains_AtThreshold_DispatchesEvent` | Stains = 7, Humanity = 7 | `DegenerationCheckRequiredEvent(StainsThreshold)` dispatched |

### G2b. `AuthorizationHelperTests` — `IsStorytellerAsync`

**File:** `tests/RequiemNexus.Application.Tests/AuthorizationHelperTests.cs` (add to existing file if present, or create)

| Test | Scenario | Assert |
|---|---|---|
| `IsStorytellerAsync_IsST_ReturnsTrue` | `userId == campaign.StoryTellerId` | `true` |
| `IsStorytellerAsync_IsNotST_ReturnsFalse` | Different userId | `false` |
| `IsStorytellerAsync_CampaignNotFound_ReturnsFalse` | Invalid campaignId | `false` |

These mirror the query already tested implicitly by `RequireStorytellerAsync`, ensuring the new non-throwing overload stays in sync.

### G3. `CharacterCreationServiceTests`

**File:** `tests/RequiemNexus.Application.Tests/CharacterCreationServiceTests.cs`

| Test | Scenario | Assert |
|---|---|---|
| `Validate_AllInClan_Succeeds` | 3 dots, all in-clan | `IsSuccess == true` |
| `Validate_TwoInClan_OneFree_Succeeds` | 2 in-clan, 1 out | `IsSuccess == true` |
| `Validate_TwoOutOfClan_Fails` | 1 in-clan, 2 out | `IsSuccess == false`, message mentions "2 of your 3" |
| `Validate_LessThanThreeDots_NoValidation` | Only 2 total dots assigned | `IsSuccess == true` (validation deferred) |

### G4. Rules Interpretation Log

**File:** `docs/rules-interpretations.md`

Append a **Phase 19** section with entries for:

1. **Soft vs. hard gate choices** — Teacher presence and Vitae-drinking cannot be verified by the app (soft); bloodline membership and Theban Humanity are mechanically deterministic (hard). Covenant Status is hard-mechanically but ST may override for narrative "stolen secrets" — both gate type and override path documented here.
2. **Crúac breaking-point threshold** — Purchases at Humanity 4+ trigger `DegenerationCheckRequired`. Purchases below Humanity 4 are not breaking points per the rules. Page reference: verify and fill in before closing the phase.
3. **Theban floor formula** — `TargetRating <= character.Humanity`. The book states each dot requires "a dot of Humanity." Interpreted as current Humanity, not starting Humanity.
4. **Crúac Humanity cap clamping** — If purchasing Crúac would place the effective max Humanity below the character's current Humanity, `character.Humanity` is clamped to the cap at purchase time to prevent invalid DB state.
5. **`DisciplineSeedData.cs` → JSON migration rationale** — `GetAll()` was dead weight; `Disciplines.json` was present but ignored. Promoted to authoritative source. Two-pass seeding required because disciplines seed before covenants/bloodlines in `DbInitializer` init chain.
6. **Covenant gate ST-override audit format** — `" | gate-override:{gate-name} stUserId={userId} {timestamp:O}"` appended to `XpLedgerEntry.Notes`. Gate name is `covenant`, `teacher`, or `necromancy`.
7. **Necromancy bloodline check** — A character qualifies for the bloodline path if any active `CharacterBloodline` has `BloodlineDefinition.FourthDisciplineId == necromancyDiscipline.Id`. This is the correct data model path — `BloodlineDefinition` has no `IsBloodlineDiscipline` flag.
8. **Discipline identity in gate logic** — FK/ID comparison preferred for all gates. String match `discipline.Name == "Crúac"` used only in Gate 5 (Crúac breaking point) where no FK is available at the check site; name is canonical and recorded here to catch future renames.
9. **Coils vs. creation dots** — The 2-of-3 in-clan validation applies to `CharacterDiscipline` rows only. Coils are `CharacterCoil` entities with a separate flow; they do not count as discipline dots for creation validation.
10. **Crúac / Theban at character creation** — Record whether the creation discipline selector exposes covenant disciplines (Crúac, Theban Sorcery). If yes: the same Covenant Status gate (D3) and ST acknowledgment modal (F1) apply at creation. If the catalogue excludes them: document that the creation flow intentionally restricts choices to non-covenant disciplines, and the gate fires at Advancement instead.

### G5. Verification

```powershell
dotnet format --verify-no-changes   # Must produce zero changes
.\scripts\test-local.ps1            # All tests must pass
```

Manual smoke test checklist:
- [ ] Create character → assign 2 out-of-clan Discipline dots → expect inline creation error
- [ ] Advancement: add Crúac without Circle covenant status → hard gate error, button disabled
- [ ] Advancement: add Crúac — player sends `AcquisitionAcknowledgedByST = true` → rejected (not ST role)
- [ ] Advancement: add Crúac — Storyteller sends `AcquisitionAcknowledgedByST = true` → succeeds; XP ledger note contains `"gate-override:covenant"`
- [ ] Advancement: add Crúac at Humanity 4 → purchase succeeds; log entry for `DegenerationCheckRequired` with `CrúacPurchase`
- [ ] Advancement: add Theban Sorcery ••• with Humanity 2 → hard gate failure "requires Humanity 3"
- [ ] Advancement: add Necromancy as non-Mekhet with no qualifying bloodline → soft gate failure
- [ ] Character sheet: Discipline power with `PoolDefinitionJson` populated → shows resolved integer
- [ ] Character sheet: Crúac ••• → shows "Max Humanity: 7 (capped by Crúac •••)"
- [ ] Crúac purchase when `character.Humanity > 10 - crucRating` → `character.Humanity` clamped in DB
- [ ] `dotnet ef migrations list` — `Phase19DisciplineAcquisitionMetadata` shows as applied

---

## Files to Create

| File | Layer | Purpose |
|---|---|---|
| `src/RequiemNexus.Domain/Events/DegenerationCheckRequiredEvent.cs` | Domain | Event record + `DegenerationReason` enum |
| `src/RequiemNexus.Application/DTOs/DisciplineAcquisitionRequest.cs` | Application | Gate parameters DTO |
| `src/RequiemNexus.Application/Contracts/IHumanityService.cs` | Application | Contract |
| `src/RequiemNexus.Application/Services/HumanityService.cs` | Application | Crúac cap + stain evaluation |
| `src/RequiemNexus.Application/Contracts/ICharacterCreationService.cs` | Application | Contract |
| `src/RequiemNexus.Application/Services/CharacterCreationService.cs` | Application | 2-of-3 in-clan rule |
| `src/RequiemNexus.Application/Events/Handlers/DegenerationCheckRequiredEventHandler.cs` | Application | Stub event handler (Phase 17 completes) |
| `tests/RequiemNexus.Application.Tests/HumanityServiceTests.cs` | Tests | Cap + stain threshold |
| `tests/RequiemNexus.Application.Tests/CharacterCreationServiceTests.cs` | Tests | 2-of-3 validation |

## Files to Modify

| File | Change |
|---|---|
| `src/RequiemNexus.Data/Models/Discipline.cs` | +7 acquisition metadata fields + FK nav properties |
| `src/RequiemNexus.Data/Models/DisciplinePower.cs` | +`PoolDefinitionJson` (`string?`) |
| `src/RequiemNexus.Data/SeedData/DisciplineSeedData.cs` | Add `LoadFromDocs()` (bool fields only) |
| `src/RequiemNexus.Data/SeedSource/Disciplines.json` | Extend schema (acquisition flags + covenant/bloodline names + `poolDefinitionJson`) |
| `src/RequiemNexus.Data/DbInitializer.cs` | Switch to `LoadFromDocs()`; add `UpdateDisciplineAcquisitionMetadataAsync` call after step 8 |
| `src/RequiemNexus.Application/Contracts/ICharacterDisciplineService.cs` | Update method signatures to `DisciplineAcquisitionRequest` |
| `src/RequiemNexus.Application/Contracts/IAuthorizationHelper.cs` | Add `IsStorytellerAsync(int campaignId, string userId)` (non-throwing bool predicate) |
| `src/RequiemNexus.Application/Services/AuthorizationHelper.cs` | Implement `IsStorytellerAsync` (same query as `RequireStorytellerAsync`) |
| `src/RequiemNexus.Application/Services/CharacterDisciplineService.cs` | Add `ValidateAcquisitionGatesAsync` with all gates + D9 ST-role check |
| `src/RequiemNexus.Web/Extensions/ApplicationServiceExtensions.cs` | Register `IHumanityService`, `ICharacterCreationService`, event handler |
| `docs/rules-interpretations.md` | Phase 19 section |
| `tests/RequiemNexus.Application.Tests/CharacterDisciplineServiceTests.cs` | Gate tests |
| Advancement Razor page | Gate tooltips + ST confirmation modal |
| Character sheet Disciplines section | Pool formula display |
| Character sheet Humanity section | Crúac Humanity cap badge |

---

## What Phase 16b Needs From This Phase

Phase 16b (Discipline Activation) is unblocked as soon as:
1. Migration `Phase19DisciplineAcquisitionMetadata` is applied — adds `PoolDefinitionJson` to `DisciplinePower`
2. `Disciplines.json` is populated with `poolDefinitionJson` values for rollable powers

Phase 16b then wires `DisciplineActivationService.ActivatePowerAsync` → reads `PoolDefinitionJson` → `TraitResolver` → deduct cost → post to dice feed. No further Phase 19 work is needed for that handoff.
