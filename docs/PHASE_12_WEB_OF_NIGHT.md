# 📅 Phase 12 — The Web of Night (Relationship Webs)

**Status:** Active
**Depends on:** Phase 11 (Assets & Armory) — complete
**Roadmap entry:** `docs/mission.md` Phase 12

---

## 🎯 Objective

Model the invisible threads that bind Kindred to one another: blood-family lineage, supernatural addiction, predatory dominance, and ghoul dependency. These mechanics are relational by nature — they require linking characters and NPCs, enforcing multi-stage state, and projecting consequences onto derived stats and Conditions.

> *The Web of Night is not a feature. It is a law of physics for the undead.*

---

## 🧱 Architectural Decisions

These decisions apply across all four subsystems in this phase.

### 1. Content vs. Behavior — Not applicable here

Phase 12 introduces no seed-data definitions analogous to `BloodlineDefinition` or `SorceryRiteDefinition`. The "content" of this phase is **relationships between characters**, which are runtime data, not seed data. All logic lives in stateless domain services and application orchestrators.

### 2. Dual FK Pattern (PC + NPC counterparties)

Blood Ties, Blood Bonds, and Ghoul management all require linking a subject Character to either a PC (`Character` entity) or an NPC (`ChronicleNpc` entity). Every cross-reference follows the same nullable dual-FK pattern:

```csharp
// Exactly one of these is non-null; the other is null.
public int? RegnantCharacterId { get; set; }   // FK → Character (PC)
public int? RegnantNpcId { get; set; }         // FK → ChronicleNpc (NPC)

// Human-readable fallback for external/unlinked counterparties (e.g., ancient sires not in the system).
[MaxLength(150)]
public string? RegnantDisplayName { get; set; }
```

**Invariant:** `(RegnantCharacterId != null) XOR (RegnantNpcId != null) OR (both null AND RegnantDisplayName != null)`.
This invariant is enforced in the Application layer, not EF Core. Document any violation in `rules-interpretations.md`.

### 3. Chronicle Scope for All New Entities

All new entities (`BloodBond`, `Ghoul`, `PredatoryAuraContest`, sire linkage) are scoped to a `ChronicleId`. Cross-chronicle data access is forbidden. The Masquerade 4-step sequence applies to every mutation.

### 4. Predatory Aura Is Not a Pool-Resolver Roll

Blood Potency is a first-class `Character` property, not a `TraitReference` type. The Predatory Aura contest uses Blood Potency directly as dice count, bypassing the generic `TraitResolver`. This is a deliberate exception documented here and in `rules-interpretations.md`. The `PredatoryAuraService` calls `DiceService.RollAsync(character.BloodPotency, ...)` directly.

### 5. Ghoul Is Not a Character

Ghouls are modeled as a lightweight `Ghoul` entity managed by the Storyteller. They do **not** have full character sheets, `CharacterAttribute` rows, or XP ledgers. Their Discipline access is stored as a JSON list of Discipline IDs at rating 1. Full ghoul playable character support is explicitly deferred.

### 6. SignalR Broadcast for Cross-Player State Changes

Blood Bond stage changes and Predatory Aura contest results directly affect another player's character. These mutations must broadcast a `ReceiveRelationshipUpdate` event to the affected player's client group in addition to the ST. The existing `SessionHub` / `ISessionClient` pattern is extended for this.

**Separation from `ReceiveConditionNotification`:** `ISessionClient` already carries `ReceiveConditionNotification` for condition/tilt toasts. When a relationship event *also* applies a Condition (e.g., Bond Stage escalation → `Addicted`), **both events fire**:
1. `ReceiveConditionNotification` — surfaces the condition toast to the player (existing behavior, unchanged).
2. `ReceiveRelationshipUpdate` — signals the sheet/context to refresh its relationship state.

These are distinct concerns. Implementors must not suppress either to avoid the other.

---

## 🩸 Subsystem A — Blood Ties & Sympathy

### Objective

Track the genealogical lineage of Kindred (Sire and Childer) and expose the **Blood Sympathy** mechanic: a supernatural sense and bonus dice pool that activates between related vampires.

---

### A1. Data Model

**Character extension** (no new table — add nullable columns to existing `Characters` table):

| Column | Type | Notes |
|--------|------|-------|
| `SireCharacterId` | `int?` | FK → `Characters.Id` (self-referential, nullable) |
| `SireNpcId` | `int?` | FK → `ChronicleNpcs.Id` (nullable) |
| `SireDisplayName` | `nvarchar(150)` | Free-text fallback for unlinked or external sires |

Navigation properties on `Character`:
- `SireCharacter` → `Character?` (the sire, if a PC in the system)
- `SireNpc` → `ChronicleNpc?` (the sire, if an NPC)
- `Childer` → `ICollection<Character>` (all characters whose `SireCharacterId` points here)

**Rules:** At most one of (`SireCharacterId`, `SireNpcId`) is non-null. `SireDisplayName` is only meaningful when both FKs are null. These are **application-layer** constraints, not DB constraints (to allow partial linkage during chronicle creation).

**Lineage integrity — explicit validation errors returned as `Result.Failure` by `IKindredLineageService`:**
- **Self-sire**: `characterId == sireCharacterId` → rejected.
- **Cycle**: sire's own sire chain already contains `characterId` → rejected. The service walks the existing chain up to depth 10 before returning failure (prevents infinite loops in corrupt data).
- **Cross-chronicle**: the proposed sire character's `CampaignId` must match the subject character's `CampaignId` → rejected if different.
- **NPC cross-chronicle**: the proposed sire NPC's `CampaignId` must match → rejected if different.

---

### A2. Domain Logic — `BloodSympathyRules` (Domain layer)

New stateless class in `RequiemNexus.Domain`:

```csharp
/// <summary>
/// Stateless rules for Blood Sympathy per V:tR 2e pp. 120–121.
/// </summary>
public static class BloodSympathyRules
{
    /// <summary>
    /// Returns the Blood Sympathy rating for a character.
    /// Rating = Blood Potency ÷ 2 (rounded down). Minimum 0.
    /// Characters with Blood Potency below 2 have no Blood Sympathy (rating 0).
    /// </summary>
    public static int ComputeRating(int bloodPotency) => bloodPotency < 2 ? 0 : bloodPotency / 2;

    /// <summary>
    /// Returns the maximum degrees of separation at which Blood Sympathy is active.
    /// This is the minimum of both participants' ratings.
    /// </summary>
    public static int EffectiveRange(int ratingA, int ratingB) => Math.Min(ratingA, ratingB);

    /// <summary>
    /// Returns the bonus dice granted when assisting a kin at the given degree of separation.
    /// Degree 1 = parent/child; degree 2 = grandparent/grandchild; etc.
    /// Bonus dice = rating ÷ degree (rounded down, minimum 0).
    /// </summary>
    public static int BonusDiceForDegree(int rating, int degree) =>
        degree <= 0 ? 0 : rating / degree;
}
```

**Blood Sympathy roll pool:**
- Wits (Attribute) + Empathy (Skill) + Blood Sympathy Rating
- Resolved via the existing `TraitResolver` for Wits and Empathy; Blood Sympathy Rating is an integer computed by `BloodSympathyRules.ComputeRating` and passed as a flat bonus.
- This is a **flat bonus injection** pattern: the Application service computes the rating, constructs the `PoolDefinition` with Wits + Empathy, then calls `DiceService.RollAsync(pool + rating, ...)`. Document in `rules-interpretations.md`.

---

### A3. Application Service — `IKindredLineageService`

```csharp
public interface IKindredLineageService
{
    /// <summary>Links a sire (PC) to a character. Replaces any existing sire link.</summary>
    Task<Result<Unit>> SetSireCharacterAsync(int characterId, int sireCharacterId, string userId);

    /// <summary>Links an NPC sire to a character.</summary>
    Task<Result<Unit>> SetSireNpcAsync(int characterId, int sireNpcId, string userId);

    /// <summary>Sets a free-text sire name for unlinked / external sires.</summary>
    Task<Result<Unit>> SetSireDisplayNameAsync(int characterId, string? name, string userId);

    /// <summary>Clears all sire linkage from a character.</summary>
    Task<Result<Unit>> ClearSireAsync(int characterId, string userId);

    /// <summary>Returns the full lineage graph (sire + childer) for a character.</summary>
    Task<LineageGraphDto> GetLineageGraphAsync(int characterId, string userId);

    /// <summary>Rolls the Blood Sympathy pool for a character attempting to locate kin.</summary>
    Task<Result<RollResult>> RollBloodSympathyAsync(int characterId, int targetCharacterId, string userId);
}
```

**`LineageGraphDto`** (Application Contracts):

```csharp
public record LineageGraphDto(
    int CharacterId,
    string CharacterName,
    int BloodPotency,
    int BloodSympathyRating,
    KinNodeDto? Sire,   // null if no sire set
    IReadOnlyList<KinNodeDto> Childer
);

public record KinNodeDto(
    int? CharacterId,   // null if NPC or unlinked
    int? NpcId,         // null if PC or unlinked
    string DisplayName,
    int? BloodPotency,  // null for NPCs without stat blocks
    int? BloodSympathyRating,
    int DegreeOfSeparation
);
```

**Blood Sympathy roll validation:** `RollBloodSympathyAsync` validates that both characters are in the same chronicle; if they are not, returns `Result.Failure`. It also validates that the effective range (`BloodSympathyRules.EffectiveRange`) covers the degree of separation between the two characters; if the target is out of range, returns `Result.Failure` with a descriptive message. There is no ST override path in the service — the Storyteller may bypass the range check by using the manual dice roller.

**Authorization:**
- Sire mutations: `RequireStorytellerAsync(campaignId, userId)`.
- Lineage read: `RequireCampaignMemberAsync(campaignId, userId)`.
- Blood Sympathy roll: `RequireCharacterAccessAsync(characterId, userId)`.

---

### A4. UI

**Character Sheet — new "Lineage" section:**
- Displays: Sire name (linked or text), list of Childer, Blood Sympathy Rating (derived, not editable).
- "Roll Blood Sympathy" button → opens `DiceRollerModal` pre-loaded with pool (Wits + Empathy + BSR).
- If Blood Potency < 2: display a note: *"Blood Sympathy inactive — Blood Potency must reach 2."*

**Edit Character / Storyteller Glimpse — Lineage management:**
- Storyteller sees a "Set Sire" dropdown (PCs in chronicle) + "NPC Sire" picker + "External Sire" text input.
- On change: calls `IKindredLineageService.SetSireCharacterAsync` / `SetSireNpcAsync` / `SetSireDisplayNameAsync`.

**Component:** `LineageSection.razor` (player-facing, read-only). `EditLineageModal.razor` (ST-facing, mutation).

---

## 🩸 Subsystem B — Blood Bond Tracker

### Objective

Track the three-stage Vinculum between a regnant and their thrall. Automate Condition application per stage and flag bonds that are fading.

---

### B1. Data Model — `BloodBond` (new table)

```
BloodBonds
├── Id                    int PK
├── ChronicleId           int FK → Campaigns.Id NOT NULL
├── ThrallCharacterId     int FK → Characters.Id NOT NULL
├── RegnantCharacterId    int? FK → Characters.Id
├── RegnantNpcId          int? FK → ChronicleNpcs.Id
├── RegnantDisplayName    nvarchar(150)?
├── RegnantKey            nvarchar(200) NOT NULL  ← synthetic uniqueness key (see below)
├── Stage                 int NOT NULL (1, 2, or 3)
├── LastFedAt             datetime? (UTC — when the thrall last drank from this regnant)
├── CreatedAt             datetime NOT NULL (UTC)
├── Notes                 nvarchar(1000)?
```

**`RegnantKey` — synthetic uniqueness column:**

PostgreSQL and SQLite both treat `NULL != NULL` in unique indexes, so a DB-level unique constraint on the nullable FK columns would allow multiple display-name-only bonds from the same thrall to the "same" unlinked regnant. To make uniqueness enforceable at the DB level without a partial index, `RegnantKey` is a computed-at-write synthetic key set by the Application layer:

| Regnant type | `RegnantKey` value | Max length |
|---|---|---|
| PC character | `"c:{RegnantCharacterId}"` | ~12 chars |
| NPC | `"n:{RegnantNpcId}"` | ~12 chars |
| Display name only | `"d:{RegnantDisplayName.Trim().ToLowerInvariant()}"` | 152 chars (`"d:"` + 150) |

`nvarchar(200)` covers all cases with headroom. `nvarchar(50)` would silently truncate display-name keys.

**Display-name deduplication:** Two display-name values that normalize to the same string (e.g. `"Mira"` and `"  mira  "`) are treated as the **same** regnant — the unique index will reject the second insert. This is intentional: if the ST enters the same name twice, it is treated as re-feeding on the same bond, not a separate one. Unicode normalization (combining characters, diacritics) is out of scope; names are normalized only with `Trim()` + `ToLowerInvariant()`.

`BloodBondService` sets this key before insert/update. `BloodBondConfiguration` defines:
```csharp
builder.HasIndex(b => new { b.ChronicleId, b.ThrallCharacterId, b.RegnantKey })
       .IsUnique();
```
This provides a single, unambiguous unique constraint regardless of which FK flavor is in use.

**EF Entity Configuration:** `BloodBondConfiguration` — cascade delete on `ThrallCharacterId` (remove character → remove bonds); restrict on `RegnantCharacterId` (regnant leaving chronicle does not auto-delete bonds; ST must handle manually).

---

### B2. New ConditionType Values

**`CharacterCondition.SourceTag` — new nullable column (added in `Phase12WebOfNight` migration):**

`CharacterCondition` currently has no way to distinguish a `Swooned` row applied by Social Maneuvering from one applied by Blood Bond Stage 2. Without a discriminator, the Bond service cannot safely resolve its own Condition without risk of removing a socially-applied `Swooned` row, or leaving a stale bond `Swooned` when the bond fades.

Add `SourceTag nvarchar(100)?` to `CharacterCondition`. The Bond service writes `"bloodbond:{bondId}"` into this column when applying bond-attributed Conditions. When the bond escalates or fades, the service queries:
```sql
WHERE CharacterId = @thrall AND ConditionType = @type AND IsResolved = false AND SourceTag = 'bloodbond:{bondId}'
```
This ensures only the bond's own Condition row is resolved, leaving any independently-applied Social Maneuvering `Swooned` rows untouched.

**`SourceTag` contract:**
- Bond service writes: `"bloodbond:{bondId}"`
- All other callers leave `SourceTag = null` (no change to existing code)
- The field is informational for existing services; no existing behavior changes

**Re-feeding idempotency (same stage):**
- Re-feeding when the bond is already at Stage 3: only updates `LastFedAt`. No new Condition row is created. No `ReceiveRelationshipUpdate` broadcast (nothing changed except the timestamp).
- Re-feeding at Stage 1 or 2: escalates stage, resolves the prior Condition row (by `SourceTag`), applies the new stage Condition.

Two new entries appended to `ConditionType` enum **after `Inspired`** to preserve stored ordinal values:

```csharp
/// <summary>
/// Vampire Blood Bond Stage 1. The thrall craves the regnant's blood above all else.
/// Persistent: fades when the Blood Bond drops below Stage 1 (every 30 days if untreated).
/// Does not award a Beat on removal — it is an addiction, not a drama resolution.
/// </summary>
Addicted,

/// <summary>
/// Vampire Blood Bond Stage 3 (Full Vinculum). The thrall cannot voluntarily act against
/// the regnant. Persistent Condition — resolves only when the bond fully fades.
/// Awards a Beat on resolution.
/// </summary>
Bound,
```

**Stage → Condition mapping:**

| Stage | Auto-Applied Condition | Notes |
|-------|----------------------|-------|
| 1 | `Addicted` | New |
| 2 | `Swooned` | Reuses existing — same name, escalated narrative |
| 3 | `Bound` | New Persistent Condition |

When stage **increases**, the previous stage's Condition is resolved (without a Beat) and the new stage's Condition is applied. When stage **decreases** (fading), the current Condition is resolved (with a Beat only for `Bound`).

---

### B3. Domain Logic — `BloodBondRules` (Domain layer)

```csharp
/// <summary>
/// Stateless rules for the Blood Bond (Vinculum) per V:tR 2e p. 154.
/// </summary>
public static class BloodBondRules
{
    /// <summary>
    /// The bond begins fading if the thrall has not fed from the regnant
    /// for longer than <see cref="FadingThreshold"/>.
    /// Interpretation: fixed 30-day interval (see rules-interpretations.md §Phase 12).
    /// </summary>
    public static readonly TimeSpan FadingThreshold = TimeSpan.FromDays(30);

    /// <summary>Returns true when the bond is past the fading threshold.</summary>
    public static bool IsFading(DateTime? lastFedAt, DateTime now) =>
        lastFedAt is null || (now - lastFedAt.Value) > FadingThreshold;

    /// <summary>
    /// Returns the ConditionType that should be active for a given bond stage.
    /// </summary>
    public static ConditionType ConditionForStage(int stage) => stage switch
    {
        1 => ConditionType.Addicted,
        2 => ConditionType.Swooned,
        3 => ConditionType.Bound,
        _ => throw new ArgumentOutOfRangeException(nameof(stage), "Stage must be 1, 2, or 3."),
    };

    /// <summary>Whether resolving the Condition for a given stage awards a Beat.</summary>
    public static bool StageResolutionAwardsBeat(int stage) => stage == 3;
}
```

---

### B4. Application Service — `IBloodBondService`

```csharp
public interface IBloodBondService
{
    /// <summary>
    /// Records a feeding event. If no bond exists, creates Stage 1.
    /// If a Stage 1 or 2 bond exists, escalates by one stage.
    /// Stage 3 bonds do not escalate further.
    /// Applies the appropriate Condition to the thrall and resolves the prior stage Condition.
    /// </summary>
    Task<Result<BloodBondDto>> RecordFeedingAsync(RecordFeedingRequest request, string userId);

    /// <summary>Returns all bonds where the given character is the thrall.</summary>
    Task<IReadOnlyList<BloodBondDto>> GetBondsForThrallAsync(int characterId, string userId);

    /// <summary>Returns all bonds in a chronicle (ST view).</summary>
    Task<IReadOnlyList<BloodBondDto>> GetBondsInChronicleAsync(int chronicleId, string userId);

    /// <summary>
    /// Manually decrements a bond by one stage (e.g., story resolution, time passing).
    /// If Stage 1, removes the bond entirely.
    /// Resolves the departing Condition (awarding a Beat for Stage 3 → 2 transition).
    /// </summary>
    Task<Result<Unit>> FadeBondAsync(int bondId, string userId);

    /// <summary>Returns all bonds in the chronicle whose <see cref="BloodBondRules.IsFading"/> is true.</summary>
    Task<IReadOnlyList<BloodBondDto>> GetFadingAlertsAsync(int chronicleId, string userId);
}
```

**`RecordFeedingRequest`:**
```csharp
public record RecordFeedingRequest(
    int ChronicleId,
    int ThrallCharacterId,
    int? RegnantCharacterId,
    int? RegnantNpcId,
    string? RegnantDisplayName,
    string? Notes
);
```

**`BloodBondDto`:**
```csharp
public record BloodBondDto(
    int Id,
    int ChronicleId,
    int ThrallCharacterId,
    string ThrallName,
    int? RegnantCharacterId,
    int? RegnantNpcId,
    string RegnantDisplayName,
    int Stage,
    DateTime? LastFedAt,
    bool IsFading,
    string ActiveConditionName
);
```

**`RecordFeedingAsync` is find-or-update, not insert-only.** The service must first look up any existing `BloodBond` by `(ChronicleId, ThrallCharacterId, RegnantKey)`. If found: escalate stage (or refresh `LastFedAt` at Stage 3). If not found: insert Stage 1. A blind insert on a repeat feeding will hit the unique index constraint. Implementors must never assume the feeding creates a new row.

**Authorization:**
- `RecordFeedingAsync`, `FadeBondAsync`: `RequireStorytellerAsync(chronicleId, userId)`.
- `GetBondsForThrallAsync`: `RequireCharacterAccessAsync(characterId, userId)` (owner or ST).
- `GetBondsInChronicleAsync`, `GetFadingAlertsAsync`: `RequireStorytellerAsync(chronicleId, userId)`.

**SignalR broadcast:** After any stage mutation, broadcast `ReceiveRelationshipUpdate` (see §SignalR) to the thrall's player client group.

---

### B5. UI

**Storyteller Glimpse — new "Blood Bonds" panel:**
- Table: Thrall | Regnant | Stage (dot-scale 1–3) | Last Fed | Fading? (warning icon if yes)
- "Record Feeding" button → `RecordFeedingModal.razor` (select or type regnant, confirm thrall)
- "Fade Bond" button (one-step decrement with confirmation)
- Fading bonds highlighted in amber; a badge count on the panel header.

**Character Sheet — new "Blood Bonds" read-only section:**
- Displays bonds where the character is the thrall.
- Shows: Regnant name, Stage (dot-scale), Last Fed, current Condition imposed.
- Read-only for players; ST sees the full panel.

**Component list:** `BloodBondsPanel.razor`, `RecordFeedingModal.razor`, `BloodBondStageIndicator.razor`.

---

## 🩸 Subsystem C — Predatory Aura

### Objective

Automate the contested Blood Potency challenge between two vampires, applying `Beaten Down` Tilt or `Shaken` Condition to the loser, and broadcasting the result in real time.

---

### C1. New TiltType Value

Appended **after `Custom`** in `TiltType` (preserves stored ordinal values — same pattern as `Inspired` in `ConditionType`):

```csharp
/// <summary>
/// Predatory Aura defeat. The character is cowed and hesitant.
/// −2 to all attack and contested rolls. Spend one action and succeed on a
/// Resolve + Composure roll (difficulty 1) to clear this Tilt.
/// </summary>
BeatenDown,
```

---

### C2. Data Model — `PredatoryAuraContest` (new table)

Lightweight audit record. Not used for derived stats or Conditions (those live in `CharacterCondition` and `CharacterTilt`).

```
PredatoryAuraContests
├── Id                    int PK
├── ChronicleId           int FK → Campaigns.Id NOT NULL
├── AttackerCharacterId   int FK → Characters.Id NOT NULL
├── DefenderCharacterId   int FK → Characters.Id NOT NULL
├── AttackerBloodPotency  int NOT NULL
├── DefenderBloodPotency  int NOT NULL
├── AttackerSuccesses     int NOT NULL
├── DefenderSuccesses     int NOT NULL
├── WinnerId              int? FK → Characters.Id (null = true draw — both BP equal)
├── OutcomeApplied        nvarchar(50) NOT NULL  (e.g. "BeatenDown", "Shaken", "Draw")
├── ResolvedAt            datetime NOT NULL (UTC)
├── IsLashOut             bool NOT NULL DEFAULT true
```

**`WinnerId` write contract:** `PredatoryAuraService` maps `PredatoryAuraOutcome` → `WinnerId` when persisting the audit row:

| `PredatoryAuraOutcome` | `WinnerId` written |
|---|---|
| `AttackerWins` | `AttackerCharacterId` |
| `DefenderWins` | `DefenderCharacterId` |
| `Draw` | `null` |

`Draw` only occurs when both rolled successes are equal **and** both Blood Potency values are equal. In a `Draw`, no Condition or Tilt is applied to either party.

**`IsLashOut` — Phase 12 scope note:** In Phase 12 only deliberate Lash Out contests (`IsLashOut = true`) are implemented via `ResolveLashOutAsync`. The passive first-meeting encounter contest (where two vampires automatically lock auras on first sight) is **deferred**. All rows written in Phase 12 will have `IsLashOut = true`. The column is retained so a future passive-contest path does not require a migration.

---

### C3. Domain Logic — `PredatoryAuraRules` (Domain layer)

```csharp
/// <summary>
/// Stateless rules for the Predatory Aura per V:tR 2e pp. 89–90.
/// </summary>
public static class PredatoryAuraRules
{
    /// <summary>
    /// Determines the winner of a contested aura roll.
    /// Ties are broken by higher Blood Potency; if BP is also tied, the result is a draw.
    /// </summary>
    public static PredatoryAuraOutcome ResolveContest(
        int attackerSuccesses, int attackerBP,
        int defenderSuccesses, int defenderBP)
    {
        if (attackerSuccesses > defenderSuccesses) return PredatoryAuraOutcome.AttackerWins;
        if (defenderSuccesses > attackerSuccesses) return PredatoryAuraOutcome.DefenderWins;
        // Tie → higher Blood Potency breaks it
        if (attackerBP > defenderBP) return PredatoryAuraOutcome.AttackerWins;
        if (defenderBP > attackerBP) return PredatoryAuraOutcome.DefenderWins;
        return PredatoryAuraOutcome.Draw;
    }
}
```

New Domain enum (one file each):

```csharp
public enum PredatoryAuraOutcome { AttackerWins, DefenderWins, Draw }
```

---

### C4. Application Service — `IPredatoryAuraService`

```csharp
public interface IPredatoryAuraService
{
    /// <summary>
    /// Executes a Predatory Aura contest between two characters.
    /// Rolls Blood Potency dice for each character (server-side via DiceService).
    /// Applies BeatenDown Tilt or Shaken Condition to the loser.
    /// Broadcasts ReceiveRelationshipUpdate to both players.
    /// </summary>
    Task<Result<PredatoryAuraContestResultDto>> ResolveLashOutAsync(
        int chronicleId, int attackerCharacterId, int defenderCharacterId, string userId);
}
```

**Pool construction (Application layer, not TraitResolver):**
```
attacker pool = character.BloodPotency (raw integer, passed directly to DiceService)
defender pool = character.BloodPotency (raw integer, passed directly to DiceService)
```

This is an explicit exception to the TraitResolver pipeline. The reason: Blood Potency is a scalar on the Character root entity and is not a `TraitReference` type. Routing it through the resolver would require adding a special-case `TraitType.BloodPotency` branch, which violates the resolver's generic contract. Document in `rules-interpretations.md`.

**Outcome application:**
- Loser receives ST's choice of `BeatenDown` Tilt OR `Shaken` Condition.
- Since automation must pick one deterministically: default to `Shaken` Condition for social/non-combat contexts. Storytellers can override by manually applying the `BeatenDown` Tilt via existing Tilt management UI.
- Document this interpretation in `rules-interpretations.md`.

**Authorization and IDOR prevention:**
- The service validates that **both** attacker and defender belong to `chronicleId` before rolling. A crafted request with characters from different campaigns must be rejected with `Result.Failure` — this is a BOLA/IDOR concern, not just a logic guard.
- Attacker is the initiator's own character: `RequireCharacterOwnerAsync(attackerCharacterId, userId)`.
- Storyteller can initiate any contest: also accepted if `RequireStorytellerAsync(chronicleId, userId)` passes.
- A player cannot set themselves as the defender — the service rejects if the caller owns the defender character but not the attacker.
- **Defender consent is narrative only.** The defender cannot refuse via the system. The code enforces only who may *initiate*; table consent is a social contract outside the application.

---

### C5. UI

**Character Sheet — "Predatory Aura" action button:**
- Visible to the character's owner and the Storyteller.
- Opens `PredatoryAuraChallengeModal.razor`: picks the defender from chronicle characters, confirms, rolls.
- Displays result with successes, winner, and applied Condition/Tilt.

**Storyteller Glimpse — contest history per character:**
- Small "Aura History" accordion listing recent contests (from `PredatoryAuraContest` table), linked to the encounter area.

**Component list:** `PredatoryAuraChallengeModal.razor`, `AuraContestResultDisplay.razor`.

---

## 🩸 Subsystem D — Ghoul Management

### Objective

Track mortal retainers (Ghouls) — their Vitae dependency, Discipline access, and monthly aging requirement. Alert the Storyteller when ghouls are overdue for feeding.

---

### D1. Data Model — `Ghoul` (new table)

```
Ghouls
├── Id                      int PK
├── ChronicleId             int FK → Campaigns.Id NOT NULL
├── Name                    nvarchar(150) NOT NULL
├── RegnantCharacterId      int? FK → Characters.Id
├── RegnantNpcId            int? FK → ChronicleNpcs.Id
├── RegnantDisplayName      nvarchar(150)?
├── LastFedAt               datetime? (UTC — last time the ghoul received Vitae)
├── VitaeInSystem           int NOT NULL DEFAULT 0 (0 or 1 for mortals)
├── ApparentAge             int? (the age the ghoul appears; null if not tracked)
├── ActualAge               int? (the ghoul's true biological age)
├── AccessibleDisciplinesJson nvarchar(2000)? (JSON: int[] of DisciplineIds at rating 1)
├── Notes                   nvarchar(2000)?
├── IsReleased              bool NOT NULL DEFAULT false
├── ReleasedAt              datetime? (UTC)
├── CreatedAt               datetime NOT NULL (UTC)
```

---

### D2. Domain Logic — `GhoulAgingRules` (Domain layer)

```csharp
/// <summary>
/// Stateless aging rules for ghouls per V:tR 2e p. 210.
/// </summary>
public static class GhoulAgingRules
{
    /// <summary>
    /// The maximum time a ghoul can go without Vitae before aging begins.
    /// Interpretation: fixed 30-day interval (see rules-interpretations.md §Phase 12).
    /// </summary>
    public static readonly TimeSpan FeedingInterval = TimeSpan.FromDays(30);

    /// <summary>
    /// Returns true when the ghoul is overdue for feeding and aging damage is pending.
    /// </summary>
    public static bool IsAgingDue(DateTime? lastFedAt, DateTime now) =>
        lastFedAt is null || (now - lastFedAt.Value) > FeedingInterval;

    /// <summary>
    /// Returns the number of full months the ghoul has gone without Vitae.
    /// Each full overdue month represents one potential lethal damage level
    /// equal to (ActualAge − ApparentAge) if tracked, otherwise 1 per month.
    /// The ST determines and applies the actual damage.
    /// </summary>
    public static int OverdueMonths(DateTime lastFedAt, DateTime now)
    {
        var elapsed = now - lastFedAt;
        if (elapsed <= FeedingInterval) return 0;
        return (int)((elapsed - FeedingInterval).TotalDays / 30);
    }
}
```

**Aging damage application** is intentionally left to the Storyteller as a narrative action. Ghouls are **not** `Character` entities and have no health track in the system — there is no character row to apply damage to. The domain returns *how overdue* the ghoul is via `OverdueMonths`; the ST uses the `Notes` field to track consequences, or applies damage to a linked PC character if the ghoul is played as a full character (outside this system's scope). This is documented in `rules-interpretations.md`.

---

### D3. Application Service — `IGhoulManagementService`

```csharp
public interface IGhoulManagementService
{
    /// <summary>Creates a new ghoul record for the chronicle.</summary>
    Task<Result<GhoulDto>> CreateGhoulAsync(CreateGhoulRequest request, string userId);

    /// <summary>Updates a ghoul's details (name, notes, apparent/actual age).</summary>
    Task<Result<GhoulDto>> UpdateGhoulAsync(UpdateGhoulRequest request, string userId);

    /// <summary>
    /// Records a feeding event (regnant provides Vitae to ghoul).
    /// Updates LastFedAt to now. Sets VitaeInSystem to 1.
    /// </summary>
    Task<Result<GhoulDto>> FeedGhoulAsync(int ghoulId, string userId);

    /// <summary>Releases the ghoul from service. Sets IsReleased = true and ReleasedAt = now.</summary>
    Task<Result<Unit>> ReleaseGhoulAsync(int ghoulId, string userId);

    /// <summary>
    /// Sets the Discipline IDs the ghoul can access (at rating 1).
    /// Validation (Application layer, returns Result.Failure if violated):
    /// - Each ID must be one of the regnant character's in-clan Disciplines.
    /// - disciplineIds.Count must not exceed the regnant character's BloodPotency.
    ///   (A ghoul gains access to up to [regnant BP] of the regnant's in-clan Disciplines.)
    /// - If the regnant is an NPC or display-name-only, validation is skipped and
    ///   the ST is trusted; no cap is enforced.
    /// </summary>
    Task<Result<Unit>> SetDisciplineAccessAsync(int ghoulId, IReadOnlyList<int> disciplineIds, string userId);

    /// <summary>Returns all active (non-released) ghouls in the chronicle.</summary>
    Task<IReadOnlyList<GhoulDto>> GetGhoulsForChronicleAsync(int chronicleId, string userId);

    /// <summary>Returns ghouls that are overdue for feeding (aging alerts).</summary>
    Task<IReadOnlyList<GhoulAgingAlertDto>> GetAgingAlertsAsync(int chronicleId, string userId);
}
```

**`GhoulDto`:**
```csharp
public record GhoulDto(
    int Id,
    int ChronicleId,
    string Name,
    int? RegnantCharacterId,
    int? RegnantNpcId,
    string RegnantDisplayName,
    DateTime? LastFedAt,
    int VitaeInSystem,
    int? ApparentAge,
    int? ActualAge,
    IReadOnlyList<string> AccessibleDisciplineNames,
    string? Notes,
    bool IsReleased,
    bool IsAgingDue
);
```

**`GhoulAgingAlertDto`:**
```csharp
public record GhoulAgingAlertDto(
    int GhoulId,
    string GhoulName,
    string RegnantDisplayName,
    DateTime? LastFedAt,
    int OverdueMonths
);
```

**Authorization:**
- All mutations: `RequireStorytellerAsync(chronicleId, userId)`.
- Reads (chronicle list, aging alerts): `RequireStorytellerAsync(chronicleId, userId)`.
- Reads (regnant's own ghouls, via character sheet): `RequireCharacterAccessAsync(regnantCharacterId, userId)`.

---

### D4. UI

**Chronicle Hub / Storyteller Glimpse — new "Ghouls" tab:**
- Table: Name | Regnant | Last Fed | Aging Due? | Actions
- Aging-due rows highlighted in amber; a badge count on the tab label.
- "Add Ghoul" button → `CreateGhoulModal.razor`
- Row actions: "Feed", "Edit", "Release", "Set Disciplines"
- "Disciplines" cell shows dot icons for accessible Disciplines.

**Player view:** Character sheet shows a "Ghouls" section listing ghouls bound to that character (regnant view), with last-fed timestamps and aging alerts.

**Component list:** `GhoulsTab.razor`, `CreateGhoulModal.razor`, `EditGhoulModal.razor`, `GhoulAgingBadge.razor`, `GhoulDisciplineAccessEditor.razor`.

---

## 📡 SignalR Extension

### New Client Method — `ReceiveRelationshipUpdate`

Add to `ISessionClient`:

```csharp
/// <summary>
/// Pushed when a relationship state affecting this client's character changes:
/// Blood Bond stage mutation, Predatory Aura contest result, or lineage link change.
/// </summary>
Task ReceiveRelationshipUpdate(RelationshipUpdateDto update);
```

**`RelationshipUpdateType` enum** (new file: `RequiemNexus.Data/RealTime/RelationshipUpdateType.cs`):
```csharp
/// <summary>Discriminator for <see cref="RelationshipUpdateDto"/> to avoid stringly-typed hub events.</summary>
public enum RelationshipUpdateType
{
    /// <summary>A Blood Bond stage changed for a character in this session.</summary>
    BloodBond,

    /// <summary>A Predatory Aura contest was resolved involving a character in this session.</summary>
    PredatoryAura,

    /// <summary>A character's sire or childer linkage changed.</summary>
    Lineage,
}
```

**`RelationshipUpdateDto`:**
```csharp
public record RelationshipUpdateDto(
    RelationshipUpdateType UpdateType,
    int? AffectedCharacterId,       // character whose state changed
    string Summary                  // human-readable description e.g. "Blood Bond with Mira advanced to Stage 2"
);
```

**Broadcast targets:**
- Blood Bond stage change → broadcast to the thrall's player group (they need to know their Condition changed) and the Storyteller group.
- Predatory Aura contest → broadcast to both attacker and defender player groups and ST.
- Lineage change → broadcast to the affected character's owner.

The hub remains a thin relay; `IBloodBondService`, `IPredatoryAuraService`, and `IKindredLineageService` call `ISessionStateRepository` / the hub notifier directly (same pattern as existing services).

---

## 🗃️ Migration Plan

### Migration: `Phase12WebOfNight`

**Changes:**
1. Add `SireCharacterId`, `SireNpcId`, `SireDisplayName` columns to `Characters`.
2. Add FK indexes for sire columns.
3. Add `SourceTag nvarchar(100)?` column to `CharacterConditions`. No data migration needed; all existing rows default to `NULL`.
4. Create `BloodBonds` table with all columns (including `RegnantKey`) and the unique index on `(ChronicleId, ThrallCharacterId, RegnantKey)`.
5. Create `PredatoryAuraContests` table.
6. Create `Ghouls` table.

**Single migration.** All Phase 12 schema changes ship in one migration named `Phase12WebOfNight`. If a breaking change is needed mid-phase, a corrective forward migration is created (never a rollback).

**`CharacterConditionConfiguration` update:** Add a composite index on `(CharacterId, ConditionType, IsResolved, SourceTag)`. The bond service's resolution query filters on all four columns; the existing single-column `CharacterId` index would require a secondary filter pass. The query shape is known at design time — add the composite index in the Phase 12 migration, not as a follow-up optimization.

**`DbInitializer` changes:**
- No new seed data required for Phase 12 (no definition tables). The initializer needs no extension for this phase.

**`TestDbInitializer` changes:**
- Add sample Blood Bond, sample Ghoul, and sample sire linkage rows to enable Application integration tests.

---

## 🧪 Testing Requirements

### Domain Tests (`RequiemNexus.Domain.Tests`)

| Class | Test Coverage |
|-------|--------------|
| `BloodSympathyRules` | `ComputeRating`: BP 0–1 → 0, BP 2–3 → 1, BP 4–5 → 2, BP 6 → 3. `EffectiveRange` min logic. `BonusDiceForDegree` per degree. |
| `BloodBondRules` | `IsFading` true/false on boundary dates. `ConditionForStage` all 3 stages. `StageResolutionAwardsBeat` only Stage 3. |
| `PredatoryAuraRules` | `ResolveContest` — attacker wins, defender wins, tie broken by BP, true draw. |
| `GhoulAgingRules` | `IsAgingDue` boundary (exactly 30 days = not due; 30 days + 1 minute = due). `OverdueMonths` zero, one, three. |

All domain tests are purely in-memory. No EF Core, no I/O. Every test is deterministic.

### Application Tests (`RequiemNexus.Application.Tests`)

| Service | Key Scenarios |
|---------|--------------|
| `IKindredLineageService` | Set sire (PC → Character): FK set, prior sire cleared. Set NPC sire. ST-only enforcement. Blood Sympathy roll returns correct pool size. Self-sire rejected. Cycle rejected. Cross-chronicle sire rejected. Out-of-range Blood Sympathy returns `Result.Failure`. |
| `IBloodBondService` | First feeding creates Stage 1 + `Addicted`. Second feeding same regnant → Stage 2 + `Swooned` (removes `Addicted` by `SourceTag`). Third → Stage 3 + `Bound`. Re-feed at Stage 3 → only `LastFedAt` updated, no new Condition, no broadcast. `FadeBondAsync` on Stage 3 → Stage 2, resolves `Bound`, awards Beat. `FadeBondAsync` on Stage 1 → removes bond. `SourceTag` isolation: fading bond resolves only its own `Swooned`, not a Social Maneuvering `Swooned`. Display-name collision (`"Mira"` vs `"  mira  "`) → same `RegnantKey`, second insert rejected. Different regnant → independent bond. |
| `IPredatoryAuraService` | Attacker wins → defender gets `Shaken`. Defender wins → attacker gets `Shaken`. Tie broken by higher BP. **True draw** (equal successes + equal BP) → `WinnerId = null`, no Condition applied. Cross-chronicle characters rejected. Non-participant cannot initiate. |
| `IGhoulManagementService` | Create ghoul. Feed ghoul → `LastFedAt` updated. `GetAgingAlertsAsync` returns overdue ghouls. Release ghoul → excluded from active list. Non-ST cannot mutate. Regnant owner can read their own ghouls. Discipline cap enforced for linked PC regnant. |

### Infrastructure Tests (`RequiemNexus.Data.Tests`)

- Verify migration `Phase12WebOfNight` applies cleanly against empty DB.
- Verify self-referential FK on `Characters.SireCharacterId` (round-trip save and load).
- Verify `BloodBond` unique constraint is enforced.

---

## 📖 Rules Interpretation Log — Phase 12 Entries

Document all of these in `docs/rules-interpretations.md` when implementing:

| Decision | Interpretation |
|----------|---------------|
| **Blood Sympathy — BP ÷ 2 minimum** | Characters with Blood Potency 0 or 1 have no active Blood Sympathy (rating 0). VtR 2e implies BP 2+ is required; we enforce strictly. |
| **Blood Sympathy roll pool** | Pool = Wits + Empathy (Skill) + Blood Sympathy Rating, where the rating is a flat integer bonus injected by the Application layer *after* TraitResolver resolves Wits and Empathy. The resolver does not know about Blood Sympathy; the service adds it explicitly. |
| **Blood Bond fading interval** | Fixed 30-day interval per stage (not a calendar month — no DST or month-length variance). VtR 2e p. 154 states a year for full recovery; we interpret this as ~1 month per stage to keep the tracker actionable at chronicle scale. Table may use any pacing; this value is the default. |
| **Blood Bond Stage 2 Condition** | `Swooned` is reused for Blood Bond Stage 2. In V:tR 2e, both social maneuvering success and Bond Stage 2 are described using similar obsession language. The existing `Swooned` Condition correctly models this. |
| **Predatory Aura — Blood Potency pool bypass** | Predatory Aura contests use `Character.BloodPotency` directly as the dice count, not via `TraitResolver`. Blood Potency is a first-class Character scalar; routing through the resolver would require a special-case `TraitType.BloodPotency` that pollutes the generic contract for a single use case. |
| **Predatory Aura — default outcome Shaken** | The rulebook gives the ST a choice between `Beaten Down` (Tilt) and `Shaken` (Condition). Automated resolution defaults to `Shaken`. ST can override by manually applying `BeatenDown` Tilt. Rationale: Shaken is a Condition (storable, narrative), while BeatenDown is a combat Tilt more appropriate for explicit combat encounters. |
| **Ghoul aging damage** | Ghouls have no health track in this system — they are not `Character` entities. `GhoulAgingRules.OverdueMonths` returns how overdue a ghoul is; the ST records consequences in the `Notes` field or outside the app. Automated damage application is not deferred — it is out of scope for a non-character entity. |
| **Ghoul Discipline access** | Ghouls can access one dot of any single in-clan Discipline of their regnant, up to the regnant's Blood Potency. We store accessible Discipline IDs at rating 1; multi-dot ghoul Disciplines are out of scope per Phase 12 non-goals. The cap is only enforced when the regnant is a linked PC; NPC/display-name regnants are ST-trusted. |
| **Predatory Aura — passive first-meeting contest** | The passive aura lock (V:tR 2e p.89 — two vampires contest on first encounter each evening) is deferred. Phase 12 implements deliberate Lash Out only. The `IsLashOut` column on `PredatoryAuraContest` is reserved for the future passive path. |
| **Blood Bond `Swooned` disambiguation** | `Swooned` can be applied by both Social Maneuvering (Phase 10) and Blood Bond Stage 2. The bond service writes `SourceTag = "bloodbond:{bondId}"` to its Condition rows. Resolution targets only rows matching that `SourceTag`, leaving Social Maneuvering rows unaffected. |
| **Ghoul aging interval** | Fixed 30-day interval (not a calendar month), matching the bond fading interval. Rationale: avoids DST and month-length edge cases; simpler to reason about in tests and UI. |

---

## ✅ Phase 12 Implementation Checklist

Work should be completed in the order listed. Complete each unit before moving to the next to maintain a green build throughout.

### Foundation

- [x] **`ConditionType` — add `Addicted` and `Bound` after `Inspired`** (Domain) with XML doc comments.
- [x] **`TiltType` — add `BeatenDown` after `Custom`** (Domain) with XML doc comment.
- [x] **Migration `Phase12WebOfNight`** (Data) — sire columns on Characters; `SourceTag` on CharacterConditions; BloodBonds (with `RegnantKey` and unique index); PredatoryAuraContests; Ghouls.
- [x] **Entity configurations** (Data) — `BloodBondConfiguration` (unique index on `RegnantKey`, cascade/restrict delete rules), `PredatoryAuraContestConfiguration`, `GhoulConfiguration`, update `CharacterConfiguration` for sire FKs, update `CharacterConditionConfiguration` (composite index on `CharacterId`, `ConditionType`, `IsResolved`, `SourceTag`).
- [x] **`TestDbInitializer` seed extension** — sample bond, ghoul, and sire linkage for integration tests.
- [ ] **Observability** — every new service (`KindredLineageService`, `BloodBondService`, `PredatoryAuraService`, `GhoulManagementService`) emits structured Serilog log entries with correlation ID for all state-changing operations; emit OpenTelemetry metrics for bond stage changes and aura contest resolutions per project norms.

### Subsystem A — Blood Ties & Sympathy

- [ ] **`BloodSympathyRules`** (Domain) — `ComputeRating`, `EffectiveRange`, `BonusDiceForDegree`.
- [ ] **Domain unit tests** for `BloodSympathyRules`.
- [ ] **`IKindredLineageService` + `KindredLineageService`** (Application) — including self-sire, cycle, cross-chronicle, and Blood Sympathy range validation.
- [ ] **`LineageGraphDto`, `KinNodeDto`** (Application Contracts).
- [ ] **Application integration tests** for `IKindredLineageService` — cover self-sire rejection, cycle rejection, cross-chronicle rejection, out-of-range Blood Sympathy roll failure.
- [ ] **`LineageSection.razor`** (Web — character sheet, player read-only).
- [ ] **`EditLineageModal.razor`** (Web — ST mutation modal).

### Subsystem B — Blood Bond Tracker

- [ ] **`BloodBondRules`** (Domain) — `IsFading`, `ConditionForStage`, `StageResolutionAwardsBeat`.
- [ ] **Domain unit tests** for `BloodBondRules`.
- [ ] **`IBloodBondService` + `BloodBondService`** (Application) — including Condition lifecycle management.
- [ ] **`BloodBondDto`, `RecordFeedingRequest`** (Application Contracts).
- [ ] **Application integration tests** for `IBloodBondService` — all stage transitions, re-feed at same stage (idempotent), `SourceTag` isolation (bond `Swooned` not resolved by Social Maneuvering path), fading, `RegnantKey` duplicate prevention, **display-name collision** (`"Mira"` and `"  mira  "` resolve to the same `RegnantKey` → second insert rejected, not a new bond).
- [ ] **`RelationshipUpdateType` enum** (Data/RealTime) — new file, one type per file rule.
- [ ] **`ReceiveRelationshipUpdate`** added to `ISessionClient` and implemented in `SessionHub`.
- [ ] **`BloodBondsPanel.razor`** (Web — ST Glimpse panel).
- [ ] **`RecordFeedingModal.razor`** (Web — ST modal).
- [ ] **Blood Bonds section on character sheet** (Web — thrall read-only view).

### Subsystem C — Predatory Aura

- [ ] **`PredatoryAuraRules`** (Domain) — `ResolveContest`.
- [ ] **`PredatoryAuraOutcome` enum** (Domain).
- [ ] **Domain unit tests** for `PredatoryAuraRules`.
- [ ] **`IPredatoryAuraService` + `PredatoryAuraService`** (Application).
- [ ] **`PredatoryAuraContestResultDto`** (Application Contracts).
- [ ] **Application integration tests** for `IPredatoryAuraService` — attacker wins → defender gets `Shaken`; defender wins → attacker gets `Shaken`; tie broken by higher BP; **true draw** (equal successes + equal BP → `WinnerId = null`, no Condition applied to either party); non-participant cannot initiate; **cross-chronicle characters rejected**.
- [ ] **`PredatoryAuraChallengeModal.razor`** (Web — character sheet action).
- [ ] **`AuraContestResultDisplay.razor`** (Web — inline result display).
- [ ] **Contest history accordion** in ST Glimpse.

### Subsystem D — Ghoul Management

- [ ] **`GhoulAgingRules`** (Domain) — `IsAgingDue`, `OverdueMonths`.
- [ ] **Domain unit tests** for `GhoulAgingRules`.
- [ ] **`IGhoulManagementService` + `GhoulManagementService`** (Application).
- [ ] **`GhoulDto`, `GhoulAgingAlertDto`, `CreateGhoulRequest`, `UpdateGhoulRequest`** (Application Contracts).
- [ ] **Application integration tests** for `IGhoulManagementService`.
- [ ] **`GhoulsTab.razor`** (Web — ST Glimpse tab).
- [ ] **`CreateGhoulModal.razor`**, **`EditGhoulModal.razor`** (Web).
- [ ] **`GhoulAgingBadge.razor`** (Web — amber badge indicator).
- [ ] **Character sheet "Ghouls" section** (Web — regnant's own ghouls, read-only for player).

### Completion

- [ ] **`docs/rules-interpretations.md`** — add all Phase 12 entries (table above).
- [ ] **`dotnet format`** — all modified files pass format check.
- [ ] **`.\scripts\test-local.ps1`** — full suite green.
- [ ] **`docs/mission.md`** — mark Phase 12 as `✅ Complete`.

---

## 🚫 Non-Goals (Phase 12)

- Full playable ghoul characters with attribute sheets, XP, and skill advancement.
- In-app health tracking for ghouls — ghouls are not `Character` entities; aging consequences are recorded by the ST in the `Notes` field or outside the application entirely.
- Multi-dot ghoul Discipline access (only first dot of in-clan Disciplines).
- Blood Sympathy passive combat bonus auto-injection into dice pools (ST tracks narratively; only the active roll is automated).
- V:tR 2e Diablerie and soul-stealing mechanics (not in scope for this phase or any planned phase).

---

> *The blood remembers every name it has ever tasted.*
> *The code must remember too.*
