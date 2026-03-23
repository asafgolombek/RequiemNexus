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

**Authorization rules:**
- Sire mutations: Storyteller only (chronicle owner verified via `AuthorizationHelper`).
- Lineage read: any player in the chronicle OR the character's owner.
- Blood Sympathy roll: character owner or Storyteller.

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
├── Stage                 int NOT NULL (1, 2, or 3)
├── LastFedAt             datetime? (UTC — when the thrall last drank from this regnant)
├── CreatedAt             datetime NOT NULL (UTC)
├── Notes                 nvarchar(1000)?
```

**Unique constraint:** `(ChronicleId, ThrallCharacterId, RegnantCharacterId)` and `(ChronicleId, ThrallCharacterId, RegnantNpcId)` — one bond per regnant–thrall pair per chronicle. Two separate bonds from different regnants on the same thrall are allowed (and are independent).

**EF Entity Configuration:** `BloodBondConfiguration` — cascade delete on `ThrallCharacterId` (remove character → remove bonds); restrict on `RegnantCharacterId` (regnant leaving chronicle does not auto-delete bonds; ST must handle manually).

---

### B2. New ConditionType Values

Two new entries appended to `ConditionType` enum (appended after existing values to preserve ordinal stability):

```csharp
/// <summary>
/// Vampire Blood Bond Stage 1. The thrall craves the regnant's blood above all else.
/// Persistent: fades when the Blood Bond drops below Stage 1 (monthly if untreated).
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
    /// Interpretation: one calendar month (see rules-interpretations.md §Phase 12).
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

**Authorization:**
- `RecordFeedingAsync`, `FadeBondAsync`: Storyteller (chronicle owner) only.
- `GetBondsForThrallAsync`: character owner OR Storyteller of the chronicle.
- `GetBondsInChronicleAsync`, `GetFadingAlertsAsync`: Storyteller only.

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

Appended to `TiltType` enum:

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
├── WinnerId              int? FK → Characters.Id (null = tie, resolved by higher BP)
├── OutcomeApplied        nvarchar(50) NOT NULL  (e.g. "BeatenDown", "Shaken", "Draw")
├── ResolvedAt            datetime NOT NULL (UTC)
├── IsLashOut             bool NOT NULL  (true = deliberate Lash Out; false = passive first-meeting contest)
```

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

**Authorization:**
- Any player in the chronicle can initiate a Lash Out involving their own character as the attacker.
- Storyteller can initiate any contest.
- A player cannot initiate a contest where they are the defender (the aura is a challenge, not a punishment they impose on themselves).

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
    /// Interpretation: one calendar month (see rules-interpretations.md §Phase 12).
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

**Aging damage application** is intentionally left to the Storyteller. The domain returns *how overdue* the ghoul is; the ST uses existing Character health tools to apply damage. This is documented in `rules-interpretations.md`.

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
    /// Validates each ID is one of the regnant character's in-clan Disciplines.
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
- All mutations: Storyteller (chronicle owner) only.
- Reads: Storyteller OR regnant character's owner (can view their own ghouls).

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

**`RelationshipUpdateDto`:**
```csharp
public record RelationshipUpdateDto(
    string UpdateType,              // "BloodBond" | "PredatoryAura" | "Lineage"
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
3. Create `BloodBonds` table with all columns and indexes.
4. Create `PredatoryAuraContests` table.
5. Create `Ghouls` table.

**Single migration.** All Phase 12 schema changes ship in one migration named `Phase12WebOfNight`. If a breaking change is needed mid-phase, a corrective forward migration is created (never a rollback).

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
| `IKindredLineageService` | Set sire (PC → Character): verify FK set, clears prior sire. Set NPC sire. ST-only enforcement. Blood Sympathy roll returns correct pool size. |
| `IBloodBondService` | First feeding creates Stage 1 + `Addicted` Condition. Second feeding same regnant → Stage 2 + `Swooned` (removes `Addicted`). Third → Stage 3 + `Bound`. `FadeBondAsync` on Stage 3 → Stage 2, resolves `Bound`, awards Beat. `FadeBondAsync` on Stage 1 → removes bond. Duplicate regnant–thrall pair → escalates, does not create duplicate. Different regnant → creates independent bond. |
| `IPredatoryAuraService` | Contest where attacker wins → defender gets `Shaken`. Contest where defender wins → attacker gets `Shaken`. Tie broken by higher BP. Non-participant cannot initiate contest. |
| `IGhoulManagementService` | Create ghoul. Feed ghoul → `LastFedAt` updated. `GetAgingAlertsAsync` returns overdue ghouls. Release ghoul → excluded from active list. Non-ST cannot mutate. Regnant owner can read their own ghouls. |

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
| **Blood Bond fading interval** | One calendar month (30 days) per stage. VtR 2e p. 154 states a year for full recovery; we interpret this as 4 stages × ~3 months, or ~1 month per stage, to keep the tracker actionable at chronicle scale. Document at table if using a different pacing. |
| **Blood Bond Stage 2 Condition** | `Swooned` is reused for Blood Bond Stage 2. In V:tR 2e, both social maneuvering success and Bond Stage 2 are described using similar obsession language. The existing `Swooned` Condition correctly models this. |
| **Predatory Aura — Blood Potency pool bypass** | Predatory Aura contests use `Character.BloodPotency` directly as the dice count, not via `TraitResolver`. Blood Potency is a first-class Character scalar; routing through the resolver would require a special-case `TraitType.BloodPotency` that pollutes the generic contract for a single use case. |
| **Predatory Aura — default outcome Shaken** | The rulebook gives the ST a choice between `Beaten Down` (Tilt) and `Shaken` (Condition). Automated resolution defaults to `Shaken`. ST can override by manually applying `BeatenDown` Tilt. Rationale: Shaken is a Condition (storable, narrative), while BeatenDown is a combat Tilt more appropriate for explicit combat encounters. |
| **Ghoul aging damage** | `GhoulAgingRules.OverdueMonths` returns how overdue a ghoul is; the ST applies the damage to the character's health track manually. Automated damage application to a mortal health track is deferred until a full mortal character pipeline exists. |
| **Ghoul Discipline access** | Ghouls can access one dot of any single in-clan Discipline of their regnant, up to the regnant's Blood Potency. We store accessible Discipline IDs at rating 1; multi-dot ghoul Disciplines are out of scope per Phase 12 non-goals. |

---

## ✅ Phase 12 Implementation Checklist

Work should be completed in the order listed. Complete each unit before moving to the next to maintain a green build throughout.

### Foundation

- [ ] **`ConditionType` — add `Addicted` and `Bound`** (Domain) with XML doc comments.
- [ ] **`TiltType` — add `BeatenDown`** (Domain) with XML doc comment.
- [ ] **Migration `Phase12WebOfNight`** (Data) — adds sire columns to Characters, and creates BloodBonds, PredatoryAuraContests, Ghouls tables.
- [ ] **Entity configurations** (Data) — `BloodBondConfiguration`, `PredatoryAuraContestConfiguration`, `GhoulConfiguration`, update `CharacterConfiguration` for sire FK.
- [ ] **`TestDbInitializer` seed extension** — sample bond, ghoul, and sire linkage for integration tests.

### Subsystem A — Blood Ties & Sympathy

- [ ] **`BloodSympathyRules`** (Domain) — `ComputeRating`, `EffectiveRange`, `BonusDiceForDegree`.
- [ ] **Domain unit tests** for `BloodSympathyRules`.
- [ ] **`IKindredLineageService` + `KindredLineageService`** (Application).
- [ ] **`LineageGraphDto`, `KinNodeDto`** (Application Contracts).
- [ ] **Application integration tests** for `IKindredLineageService`.
- [ ] **`LineageSection.razor`** (Web — character sheet, player read-only).
- [ ] **`EditLineageModal.razor`** (Web — ST mutation modal).

### Subsystem B — Blood Bond Tracker

- [ ] **`BloodBondRules`** (Domain) — `IsFading`, `ConditionForStage`, `StageResolutionAwardsBeat`.
- [ ] **Domain unit tests** for `BloodBondRules`.
- [ ] **`IBloodBondService` + `BloodBondService`** (Application) — including Condition lifecycle management.
- [ ] **`BloodBondDto`, `RecordFeedingRequest`** (Application Contracts).
- [ ] **Application integration tests** for `IBloodBondService` (all stage transitions, fading, duplicates).
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
- [ ] **Application integration tests** for `IPredatoryAuraService`.
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
- Automated aging damage application (ST applies manually via health track).
- Multi-dot ghoul Discipline access (only first dot of in-clan Disciplines).
- Blood Sympathy passive combat bonus auto-injection into dice pools (ST tracks narratively; only the active roll is automated).
- V:tR 2e Diablerie and soul-stealing mechanics (not in scope for this phase or any planned phase).

---

> *The blood remembers every name it has ever tasted.*
> *The code must remember too.*
