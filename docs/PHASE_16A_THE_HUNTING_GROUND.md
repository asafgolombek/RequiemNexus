# Phase 16a — The Hunting Ground (Feeding)

**Status:** ✅ Complete  
**Objective:** Make feeding a first-class mechanical action. Each Predator Type has a canonical hunting pool resolved by `TraitResolver`. Territory quality adds flat bonus dice. Resonance is display-only this phase — static success-threshold mapping, no mechanical effects.

**Related:** Roadmap summary in [`docs/mission.md`](./mission.md) (Phase 16a ✅); dependency context in [`docs/PLAYABILITY_GAP_PLAN.md`](./PLAYABILITY_GAP_PLAN.md). This file is the **authoritative implementation plan** for Phase 16a (shipped).

> Phase 16a is **fully independent** — no upstream dependencies on Phases 14, 15, 17, or 19.

---

## Architectural Decisions

### 1. Hunting is wired to Predator Type

`HuntingService.ExecuteHuntAsync(characterId, userId, territoryId?)` reads the character's `PredatorType`, applies the Masquerade (`userId` ownership), looks up the matching `HuntingPoolDefinition` seed row, resolves the pool via `ITraitResolver`, rolls, maps successes to Vitae, and calls `IVitaeService.GainVitaeAsync`.

No bespoke pool construction in the service — all predator-type pool data lives in seed rows.

### 2. Territory is optional, additive, and campaign-aligned

```csharp
Task<Result<HuntResult>> ExecuteHuntAsync(
    int characterId,
    string userId,
    int? territoryId,
    CancellationToken cancellationToken = default);
```

When `territoryId` is provided, `FeedingTerritory.Rating` (1–5, already modeled) adds that many bonus dice to the pool. Narrative access is Storyteller-trusted, but **campaign alignment is enforced as a data integrity check**: if `territory.CampaignId != character.CampaignId` the service returns `Result.Failure("Territory does not belong to this campaign.")`. If `character.CampaignId` is null and a non-null `territoryId` is passed, this also returns `Result.Failure` — no territory can belong to a null campaign.

### 3. Vitae mapping is seed-data driven

`HuntingPoolDefinition` carries `BaseVitaeGain` (always 0 for standard predator types) and `PerSuccessVitaeGain` (always 1 baseline). The formula is:

```
vitaeGained = BaseVitaeGain + (successes × PerSuccessVitaeGain)
```

Zero successes → zero Vitae gained. `IVitaeService.GainVitaeAsync` is only called when `vitaeGained > 0`.

### 4. Resonance is display only (static thresholds)

A private static method (or small static helper type) maps success count to `ResonanceOutcome`. **No DB seed or JSON table** is required for resonance in Phase 16a — this differs from an earlier sketch in `PLAYABILITY_GAP_PLAN.md` that mentioned a JSON seed; the shipped design keeps resonance logic in code alongside the roll. The `HuntResult` DTO carries the resolved `ResonanceOutcome`; the UI displays it as a label. No mechanical modifier is applied this phase.

Resonance thresholds:

| Successes | Outcome |
|-----------|---------|
| 0 | None |
| 1–2 | Fleeting |
| 3–4 | Weak |
| 5–6 | Functional |
| 7+ | Saturated |

### 5. HuntingRecord mirrors the Beat/XP ledger pattern

Every hunt (success or failure) writes a `HuntingRecord` row: timestamp, pool description, success count, Vitae gained, resonance, optional territory. This is a lightweight audit log — no business logic reads it.

### 6. Dice feed and announcer

`ISessionService.PublishDiceRollAsync` broadcasts the roll to the campaign dice feed, exactly as `FrenzyService` does. The UI uses the existing Phase 13 announcer pattern (`aria-live`) for screen-reader announcement of the hunt outcome.

---

## New Artifacts

### Domain Layer — `src/RequiemNexus.Domain/`

#### `Enums/PredatorType.cs`

```csharp
namespace RequiemNexus.Domain.Enums;

/// <summary>V:tR 2e predator types. Each maps to a canonical hunting pool in HuntingPoolDefinition seed data.</summary>
public enum PredatorType
{
    Alleycat = 1,       // Strength + Brawl
    Bagger = 2,         // Resolve + Streetwise
    Cleaver = 3,        // Wits + Subterfuge
    Consensualist = 4,  // Presence + Persuasion
    Farmer = 5,         // Composure + Animal Ken
    Osiris = 6,         // Presence + Occult
    Sandman = 7,        // Dexterity + Stealth
    SceneQueen = 8,     // Manipulation + Persuasion
    Siren = 9,          // Presence + Subterfuge
}
```

#### `Enums/ResonanceOutcome.cs`

```csharp
namespace RequiemNexus.Domain.Enums;

/// <summary>Resonance intensity of Vitae gained from a hunt. Display-only in Phase 16a.</summary>
public enum ResonanceOutcome
{
    None = 0,
    Fleeting = 1,
    Weak = 2,
    Functional = 3,
    Saturated = 4,
}
```

---

### Data Layer — `src/RequiemNexus.Data/`

#### `Models/HuntingPoolDefinition.cs`

```csharp
using System.ComponentModel.DataAnnotations;
using RequiemNexus.Domain.Enums;

namespace RequiemNexus.Data.Models;

/// <summary>
/// Seed row: one entry per PredatorType, containing the canonical hunting pool and Vitae scaling.
/// </summary>
public class HuntingPoolDefinition
{
    [Key]
    public int Id { get; set; }

    public PredatorType PredatorType { get; set; }

    /// <summary>
    /// JSON array of TraitReference objects consumed by ITraitResolver.ResolvePoolAsync.
    /// Format: [{"type":"Attribute","traitId":"Strength"},{"type":"Skill","traitId":"Brawl"}]
    /// </summary>
    [Required]
    public string PoolDefinitionJson { get; set; } = string.Empty;

    /// <summary>Vitae awarded regardless of successes (normally 0).</summary>
    public int BaseVitaeGain { get; set; }

    /// <summary>Vitae awarded per success (normally 1).</summary>
    public int PerSuccessVitaeGain { get; set; } = 1;

    /// <summary>Short narrative description shown in the hunt result UI.</summary>
    [Required]
    [MaxLength(400)]
    public string NarrativeDescription { get; set; } = string.Empty;
}
```

#### `Models/HuntingRecord.cs`

```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using RequiemNexus.Domain.Enums;

namespace RequiemNexus.Data.Models;

/// <summary>Audit ledger row written after every hunt attempt.</summary>
public class HuntingRecord
{
    [Key]
    public int Id { get; set; }

    public int CharacterId { get; set; }

    [ForeignKey(nameof(CharacterId))]
    public virtual Character? Character { get; set; }

    public int? TerritoryId { get; set; }

    [ForeignKey(nameof(TerritoryId))]
    public virtual FeedingTerritory? Territory { get; set; }

    public PredatorType PredatorType { get; set; }

    /// <summary>Human-readable pool description (e.g. "Alleycat: Strength + Brawl, pool 5 dice").</summary>
    [Required]
    [MaxLength(300)]
    public string PoolDescription { get; set; } = string.Empty;

    public int Successes { get; set; }

    public int VitaeGained { get; set; }

    public ResonanceOutcome Resonance { get; set; }

    public DateTime HuntedAt { get; set; } = DateTime.UtcNow;
}
```

#### `ApplicationDbContext.cs` — add DbSets

```csharp
public DbSet<HuntingPoolDefinition> HuntingPoolDefinitions => Set<HuntingPoolDefinition>();
public DbSet<HuntingRecord> HuntingRecords => Set<HuntingRecord>();
```

#### EF Core entity configuration — unique index

`HuntingPoolDefinition` requires a unique index on `PredatorType` to prevent duplicate seed rows from manual edits or re-run bugs. Add in `OnModelCreating` (or a dedicated `IEntityTypeConfiguration`):

```csharp
modelBuilder.Entity<HuntingPoolDefinition>()
    .HasIndex(h => h.PredatorType)
    .IsUnique();
```

#### `Models/Character.cs` — add field

```csharp
/// <summary>V:tR 2e Predator Type. Null until set during character creation or ST assignment.</summary>
public PredatorType? PredatorType { get; set; }
```

#### Migration

Unix:
```bash
dotnet ef migrations add Phase16a_HuntingGround \
  --project src/RequiemNexus.Data \
  --startup-project src/RequiemNexus.Web
```

PowerShell (Windows):
```powershell
dotnet ef migrations add Phase16a_HuntingGround --project src/RequiemNexus.Data --startup-project src/RequiemNexus.Web
```

New migration adds:
- `HuntingPoolDefinitions` table
- `HuntingRecords` table
- `PredatorType` nullable int column on `Characters`

---

### Application Layer — `src/RequiemNexus.Application/`

#### `Models/HuntResult.cs`

```csharp
using RequiemNexus.Domain.Enums;

namespace RequiemNexus.Application.Models;

/// <summary>Outcome of a single hunt attempt.</summary>
public record HuntResult(
    int Successes,
    int VitaeGained,
    ResonanceOutcome Resonance,
    string PoolDescription,
    string NarrativeDescription,
    bool TerritoryBonusApplied);
```

#### `Contracts/IHuntingService.cs`

```csharp
using RequiemNexus.Application.Models;
using RequiemNexus.Domain.Models;

namespace RequiemNexus.Application.Contracts;

/// <summary>
/// Executes a hunt roll for a character based on their Predator Type.
/// Resolves the canonical pool, applies optional territory bonus, gains Vitae, records the result.
/// </summary>
public interface IHuntingService
{
    /// <summary>
    /// Rolls a hunt for <paramref name="characterId"/>.
    /// If <paramref name="territoryId"/> is provided, <see cref="FeedingTerritory.Rating"/> adds bonus dice.
    /// Masquerade: caller must own the character.
    /// </summary>
    Task<Result<HuntResult>> ExecuteHuntAsync(
        int characterId,
        string userId,
        int? territoryId = null,
        CancellationToken cancellationToken = default);
}
```

#### `Services/HuntingService.cs` — logic walkthrough

```
1. RequireCharacterAccessAsync (Masquerade)
2. Load Character with Attributes + Skills; fail if null or PredatorType is null
3. Load HuntingPoolDefinition for character.PredatorType; fail if not found
4. ResolvePoolAsync(character, definition.PoolDefinitionJson)
5. If territoryId provided:
     a. Load FeedingTerritory; fail if not found
     b. Fail if territory.CampaignId != character.CampaignId (or character.CampaignId is null)
     c. Add territory.Rating to pool size
6. Clamp pool size to minimum 1 (pool floor, applied after all bonuses)
7. Roll(poolSize, tenAgain: true)
8. Compute vitaeGained = definition.BaseVitaeGain + (roll.Successes × definition.PerSuccessVitaeGain)
9. If vitaeGained > 0: GainVitaeAsync(characterId, userId, vitaeGained, "Hunt")
10. Map roll.Successes → ResonanceOutcome (static threshold table)
11. Write HuntingRecord row, SaveChangesAsync
12. PublishDiceRollAsync (campaign dice feed, skipped if character has no CampaignId)
    — uses Character.CampaignId as chronicle scope, consistent with FrenzyService
13. Log structured event: Log.Information("Hunt executed {@HuntEvent}", new { CharacterId, CampaignId, PredatorType, PoolSize, Successes, VitaeGained, Resonance, TerritoryId })
14. Return Result<HuntResult>.Success(...)
```

Key implementation notes:
- A hunt with 0 successes still writes a `HuntingRecord` (audit log completeness)
- Pool description format: `"{PredatorType}: {pool traits}, pool {N} dice{territoryLine}"` where `territoryLine` is `" (+{rating} territory bonus)"` when applicable
- Resonance resolution lives in a private static method, not a separate service

#### Registration — `src/RequiemNexus.Web/Extensions/ApplicationServiceExtensions.cs`

```csharp
services.AddScoped<IHuntingService, HuntingService>();
```

---

### Seed Data — `src/RequiemNexus.Data/SeedData/HuntingPoolDefinitionSeedData.cs`

One row per predator type. PoolDefinitionJson uses the same `TraitReference` format as Discipline/Devotion power pools.

| PredatorType | Pool | Narrative |
|---|---|---|
| Alleycat | Strength + Brawl | Predatory ambush in the city's shadows — force and ferocity over finesse. |
| Bagger | Resolve + Streetwise | Working hospital supply chains, blood banks, and underground markets. |
| Cleaver | Wits + Subterfuge | Feeding from kine who believe you are still mortal — family or close circle. |
| Consensualist | Presence + Persuasion | Charming willing vessels into a mutually beneficial arrangement. |
| Farmer | Composure + Animal Ken | Drawing sustenance from animals — calm, patient, and without mortal risk. |
| Osiris | Presence + Occult | A cult of devoted followers — feeding among rites they believe to be sacred. |
| Sandman | Dexterity + Stealth | Entering sleeping prey's home unseen, feeding before dawn. |
| Scene Queen | Manipulation + Persuasion | Working a social scene — clubs, galas, or Elysium periphery. |
| Siren | Presence + Subterfuge | Seduction and false intimacy as the approach to a vessel. |

`BaseVitaeGain = 0`, `PerSuccessVitaeGain = 1` for all types.

Seed guard: `if (await context.HuntingPoolDefinitions.AnyAsync()) return;`

---

### Web Layer — `src/RequiemNexus.Web/`

#### `Components/Pages/CharacterSheet/HuntPanel.razor`

Responsibilities:
- "Hunt" button visible only when `character.PredatorType` is set
- Optional territory picker: dropdown of `FeedingTerritory` rows for the campaign (empty option = no territory)
- On click: call `IHuntingService.ExecuteHuntAsync`, show loading state
- On result: display
  - Vitae gained (delta badge on Vitae track)
  - Resonance label (color-coded: None → grey, Fleeting → white, Weak → yellow, Functional → orange, Saturated → red)
  - Narrative description from `HuntResult.NarrativeDescription`
  - Success count
- `aria-live="polite"` announcer for screen readers (Phase 13 pattern); **one polite sentence** that states successes, Vitae gained, and resonance together (same pattern as other roll announcements)
- Error state: display `result.Error` in red

#### Character Sheet Integration

Add `<HuntPanel />` to the character sheet, collocated near the Vitae track (same tab as health / Vitae / Willpower).

**Blazor server-side only:** `HuntPanel` calls `IHuntingService` directly (no HTTP API). If an HTTP endpoint is added later, a response DTO should map from `HuntResult` to avoid leaking internal error strings.

**Ledger volume:** `HuntingRecord` is append-only. No pruning in Phase 16a. Pagination / pruning policy is a later backlog item.

---

## Task Checklist

```
- [x] Domain: PredatorType enum
      File: src/RequiemNexus.Domain/Enums/PredatorType.cs
      9 values, matching V:tR 2e predator types, integer-backed starting at 1.

- [x] Domain: ResonanceOutcome enum
      File: src/RequiemNexus.Domain/Enums/ResonanceOutcome.cs
      5 values: None(0), Fleeting(1), Weak(2), Functional(3), Saturated(4).

- [x] Data: HuntingPoolDefinition entity
      File: src/RequiemNexus.Data/Models/HuntingPoolDefinition.cs
      Fields: Id, PredatorType, PoolDefinitionJson, BaseVitaeGain, PerSuccessVitaeGain, NarrativeDescription.

- [x] Data: HuntingRecord entity
      File: src/RequiemNexus.Data/Models/HuntingRecord.cs
      Fields: Id, CharacterId (FK), TerritoryId (FK nullable), PredatorType, PoolDescription,
              Successes, VitaeGained, Resonance, HuntedAt.

- [x] Data: Character.PredatorType column
      File: src/RequiemNexus.Data/Models/Character.cs
      PredatorType? PredatorType (nullable).

- [x] Data: ApplicationDbContext DbSets
      File: src/RequiemNexus.Data/ApplicationDbContext.cs
      Add HuntingPoolDefinitions and HuntingRecords DbSets.

- [x] Data: HuntingPoolDefinition unique index
      File: src/RequiemNexus.Data/EntityConfigurations/HuntingPoolDefinitionConfiguration.cs
      HasIndex(h => h.PredatorType).IsUnique() — prevents duplicate seed rows.

- [x] Data: EF Core migration
      Adds HuntingPoolDefinitions table, HuntingRecords table, PredatorType column on Characters.

- [x] Data: HuntingPoolDefinitionSeedData
      File: src/RequiemNexus.Data/SeedData/HuntingPoolDefinitionSeedData.cs
      9 rows (one per PredatorType), idempotent guard on AnyAsync.

- [x] Data: DbInitializer.SeedHuntingPoolDefinitionsAsync
      File: src/RequiemNexus.Data/DbInitializer.cs
      Call after SeedClansAndDisciplinesAsync. Idempotent.

- [x] Application: HuntResult DTO
      File: src/RequiemNexus.Application/Models/HuntResult.cs
      Record with Successes, VitaeGained, Resonance, PoolDescription, NarrativeDescription, TerritoryBonusApplied.

- [x] Application: IHuntingService interface
      File: src/RequiemNexus.Application/Contracts/IHuntingService.cs
      Single method: ExecuteHuntAsync(characterId, userId, territoryId?, cancellationToken).

- [x] Application: HuntingService implementation
      File: src/RequiemNexus.Application/Services/HuntingService.cs
      Masquerade → resolve pool → territory campaign check → pool floor clamp → roll → Vitae gain → resonance → record → dice feed → structured log.

- [x] Application: Register IHuntingService
      File: src/RequiemNexus.Web/Extensions/ApplicationServiceExtensions.cs
      services.AddScoped<IHuntingService, HuntingService>();

- [x] Tests: HuntingService unit tests
      Project: tests/RequiemNexus.Application.Tests/HuntingServiceTests.cs
      Cover: successful hunt, null PredatorType failure, null pool definition failure,
             territory bonus, territory campaign mismatch failure, zero-success (no GainVitaeAsync call),
             Masquerade rejection, pool floor clamp (1 die minimum).
      Mock: IDiceService, ITraitResolver, IVitaeService.

- [x] Web: HuntPanel.razor component
      File: src/RequiemNexus.Web/Components/Pages/CharacterSheet/HuntPanel.razor
      Hunt button, optional territory picker, result display (Vitae delta, resonance label, narrative, successes).
      aria-live announcer.

- [x] Web: Integrate HuntPanel into character sheet
      CharacterVitals.razor — <HuntPanel /> collocated with Vitae track.

- [x] Rules: docs/rules-interpretations.md
      Phase 16a section merged.

- [x] Verify: dotnet build; HuntingServiceTests pass; run .\scripts\test-local.ps1 before merge.
```

---

## Rules Interpretation Log

*Merged into [`docs/rules-interpretations.md`](./rules-interpretations.md) under **Phase 16a — The Hunting Ground (feeding)**. Duplicated here for phase-doc completeness.*

- **Hunting pool per Predator Type:** V:tR 2e lists primary pools per type (p. 104–107). Where the book offers a choice of two pools (e.g. Brawl or Weaponry for Alleycat), we select the first listed option for automation. Tables can use the alternate by seeding a custom `HuntingPoolDefinition` row or via ST override.
- **Resonance thresholds:** The corebook describes resonance as a narrative quality tied to Blood Potency and circumstances, not a pure success-count table. For Phase 16a display automation we map success count to the four intensity labels (Fleeting / Weak / Functional / Saturated), with thresholds 1–2 / 3–4 / 5–6 / 7+ respectively. These thresholds are interpretive; the ST may override the displayed resonance outside the app. Mechanical resonance effects (Diablerie, Sorcery, Disciplines) are out of scope until a later phase.
- **Territory bonus formula:** `FeedingTerritory.Rating` (1–5) is added as flat bonus dice to the resolved pool. V:tR 2e does not specify a territory-bonus formula; this is a table convenience to reward holding high-quality hunting grounds. No cap applied — pool floor is 1 die regardless. Territory must belong to the same campaign as the character; cross-campaign territory IDs return `Result.Failure`.
- **Pool floor enforcement:** After `ResolvePoolAsync` + territory bonus, if the total pool is less than 1, it is clamped to 1 before rolling. A resolver returning 0 (e.g. missing traits) is a data setup issue — the character still rolls one die rather than receiving a hard failure. This preserves audit log completeness.
- **Zero-success hunts:** 0 successes = 0 Vitae gained. The hunt is not a botch unless a dramatic failure rule applies (the app does not automate dramatic failures in Phase 16a). A `HuntingRecord` is still written to preserve the ledger.
- **PredatorType null guard:** Characters without a PredatorType cannot initiate a hunt through the UI. The service also returns `Result.Failure` on null PredatorType. The ST must assign one (character creation or sheet edit — future task).
- **Vitae cap at MaxVitae:** `IVitaeService.GainVitaeAsync` already caps at `character.MaxVitae`. Overflow is silently discarded (same as existing behavior).

---

## Verification

1. **Seed check:** After `dotnet run`, query `HuntingPoolDefinitions` — expect 9 rows, one per PredatorType.
2. **Null guard:** Character with `PredatorType = null` → Hunt button hidden in UI; direct `ExecuteHuntAsync` invocation (e.g. test or future endpoint) returns `Result.Failure("Predator Type not set.")`.
3. **Successful hunt:** Character with PredatorType = Alleycat (Strength + Brawl), no territory → `ExecuteHuntAsync` → Vitae increases by successes, `HuntingRecord` row written, dice feed entry published.
4. **Territory bonus:** Same character, `territoryId` = territory with `Rating = 3` → pool is 3 larger than baseline; `HuntingRecord.PoolDescription` includes "+3 territory bonus".
5. **Zero-success hunt:** Force a 1-die pool → check `HuntingRecord.VitaeGained = 0`, `Resonance = None`, no `GainVitaeAsync` call.
6. **Resonance display:** 5 successes → `ResonanceOutcome.Functional`, UI shows "Functional" label.
7. **Vitae cap:** Character at `MaxVitae` → `GainVitaeAsync` not called (or call succeeds with 0 delta) — `CurrentVitae` does not exceed `MaxVitae`.
8. **Masquerade:** Another player's userId on a character they don't own → `Result.Failure` from authorization.
9. **Campaign mismatch:** Character in campaign A, territory in campaign B → `Result.Failure("Territory does not belong to this campaign.")`.
10. **No-campaign character + territory:** Character with `CampaignId = null`, non-null `territoryId` → `Result.Failure`.
11. **Pool floor:** Trait resolution returning 0 dice → pool clamped to 1 before roll, not a failure.
12. **dotnet format + test-local.ps1 pass.**
