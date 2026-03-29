# 🩸 Master Plan: Completing Requiem Nexus (Phases 17 & 18)

> Covers all remaining work before Phase 20 (The Global Embrace).
> Phases 1–16b and 19 are ✅ complete. This document is the **execution-level** single source of truth for Phases 17–18 and supersedes the phase sections in `mission.md` wherever the two conflict. `mission.md` retains the canonical phase status table and dependency graph.

---

## 📊 Remaining Work at a Glance

| Phase | Name | Status | Blockers |
|-------|------|--------|----------|
| 17 | The Fog of Eternity — Humanity & Conditions | ✅ Delivered | None — exit criteria verified in repo |
| 18 | The Wider Web — Edge Systems & Content | 🔄 Tracks A–C delivered; Track D (seed catalogs) pending | None — fully independent |

**Recommended execution order:** Phase 17 first (mechanical core), then Phase 18 (edge systems + content fills). Both can be parallelized after Phase 17's `ModifierService` changes land, since Phase 18 has no dependency on Phase 17.

---

## 📅 Phase 17: The Fog of Eternity — Humanity & Condition Wiring

**Objective:** Automate degeneration rolls and wire all Condition penalties into the dice pool resolver.

**Key architectural principle:** Condition penalties are a _modifier source_, not special-cased code. Degeneration is a _triggered roll_, not an automatic loss. Remorse is an _explicit roll_ (character owner or Storyteller), not passive resolution.

---

### Step 1 — Domain: `IConditionRules.GetPenalties` Extension

**Layer:** `RequiemNexus.Domain`

**Architectural note:** `ConditionType` is a Domain **enum** (see `RequiemNexus.Domain/Enums/ConditionType.cs`). There is no database table or entity for condition types. Condition penalties are canonical game rules — the correct pattern is to extend `IConditionRules` / `ConditionRules`, exactly as `GetConditionDescription` and `AwardsBeatOnResolve` already do. No migration is required.

Also note: `Stunned` and `Blind` are **Tilt** types (`TiltType`), not Conditions. Tilt mechanical effects are already surfaced by `ConditionRules.GetTiltEffects()`. Do not add tilt penalties to this step.

**What to do:**

1. Add a `ConditionPenaltyModifier` record to `RequiemNexus.Domain/Models/`:
   ```csharp
   // One modifier entry — a condition may produce multiple (e.g. multiple pool targets).
   public record ConditionPenaltyModifier(string PoolTarget, int Delta, bool IsNoActionFlag = false);
   ```

2. Add to `IConditionRules`:
   ```csharp
   /// <summary>Returns the dice-pool penalties imposed by an active Condition. Empty list = no mechanical penalty.</summary>
   IReadOnlyList<ConditionPenaltyModifier> GetPenalties(ConditionType type);
   ```

3. Implement in `ConditionRules` as a `switch` expression:

| Condition | Penalty entries |
|-----------|-----------------|
| `Shaken` | `("AllPools", -2)` |
| `Exhausted` | `("PhysicalPools", -2)` |
| `Frightened` | `("AllExceptFleeing", -2)` |
| `Guilty` | `("ResolveComposure", -1)` |
| `Despondent` | `("MentalPools", -2)` |
| `Provoked` | `("Composure", -1)` |
| All others (including `Custom`, `Wassail`, `Bleeding`, etc.) | `[]` — no pool penalty; ST applies any custom effects |

`PoolTarget` strings are interpreted by `ModifierService` in Step 2. Define a `ConditionPoolTarget` static class with named constants (e.g. `ConditionPoolTarget.AllPools = "AllPools"`) so the mapping in Step 2 avoids magic strings.

**No migration required — this is a pure Domain code change.**

**Tests:**
- Unit: `ConditionRules.GetPenalties(ConditionType.Shaken)` returns one entry `("AllPools", -2)`.
- Unit: `ConditionRules.GetPenalties(ConditionType.Custom)` returns empty list.
- Unit: `ConditionRules.GetPenalties(ConditionType.Guilty)` returns `("ResolveComposure", -1)`.

---

### Step 2 — Application: `ConditionModifierSource` in `ModifierService`

**Layer:** `RequiemNexus.Application`

**What to do:**
- In `ModifierService.GetModifiersForCharacterAsync`:
  1. Load active `CharacterCondition` rows for the character.
  2. For each active condition, call `IConditionRules.GetPenalties(condition.ConditionType)`.
  3. Map each `ConditionPenaltyModifier` to a `PassiveModifier` using `ConditionPoolTarget` constants to select the affected dice pool (skip if empty list — homebrew or non-penalizing condition).
  4. Inject the resulting `PassiveModifier` entries into the aggregation loop alongside existing equipment and Coil modifiers.
- The `TraitResolver` call is unchanged — it already accepts a modifier collection.
- `IConditionRules` is already registered in DI — inject it into `ModifierService`.

**Tests:**
- Unit: `ModifierService` returns a −2 modifier for a character with an active `Shaken` condition.
- Unit: `ModifierService` returns no penalty modifiers for a character with a `Custom` condition.
- Integration: a dice pool roll on a `Shaken` character produces a pool 2 dice lower than without the condition. Use a fixed attribute+skill pool from test seed to prevent flakiness.

---

### Step 3 — Application: Wire `EvaluateStainsAsync` Call Sites

**Layer:** `RequiemNexus.Application`

**Existing state:** `IHumanityService.EvaluateStainsAsync(int characterId, string userId)` and its implementation already exist in `src/RequiemNexus.Application/Services/HumanityService.cs`. The threshold is:
```csharp
if (character.HumanityStains >= character.Humanity)
    _domainEventDispatcher.Dispatch(new DegenerationCheckRequiredEvent(...));
```
This formula — `stains >= Humanity` — is **correct per VtR 2e** (p.185: "a number of Stains equal to or greater than her Humanity score"). Do not change it.

The domain event type is `DegenerationCheckRequiredEvent` (see `src/RequiemNexus.Domain/Events/DegenerationCheckRequiredEvent.cs`). A placeholder handler `DegenerationCheckRequiredEventHandler` already exists and logs only.

**What to do:**
- Identify every service that applies stains and add a call to `EvaluateStainsAsync` after the stain is persisted. Likely call sites: `SorceryService` (rite activation with `HumanityStain` requirement), any breaking-point handling path.
- Define idempotency policy: the event fires **every time** stains cross threshold — callers may call `EvaluateStainsAsync` multiple times but the handler must be idempotent. Document this decision in `rules-interpretations.md`.
- Add `ExecuteDegenerationRollAsync` and `RollRemorseAsync` to `IHumanityService` (see Steps 4 & 5).

**Tests (verify existing + add missing):**
- Unit: stains below threshold → no event dispatched.
- Unit: stains exactly at threshold → `DegenerationCheckRequiredEvent` dispatched once.
- Unit: calling `EvaluateStainsAsync` twice with stains above threshold → event dispatched twice (fire-every-time semantics confirmed in tests).

---

### Step 4 — Application: Degeneration Roll Logic

**Layer:** `RequiemNexus.Application`

**What to do:**
- Add `ExecuteDegenerationRollAsync(int characterId, string userId)` to `IHumanityService`.
- Pool: `Resolve + (7 − Humanity)` dice. At Humanity 0 this is a chance die.
- Outcomes:
  - **Success (≥1):** clear all stains; Humanity unchanged.
  - **Failure (0 successes):** remove 1 Humanity dot; clear all stains.
  - **Dramatic Failure:** remove 1 Humanity dot; clear all stains; apply `Guilty` condition.
- Dice submission goes through `DiceService` / `PublishDiceRollAsync` (same rite/discipline pattern) so the result lands in the session feed.
- After outcome is applied, call `EvaluateStainsAsync` again to check for cascading threshold breach (edge case at very low Humanity).

**Tests:**
- Unit: success → humanity unchanged, stains cleared.
- Unit: failure → humanity dot removed, stains cleared.
- Unit: dramatic failure → humanity dot removed, stains cleared, `Guilty` condition applied.
- Unit: Humanity 0 → chance die pool used.

---

### Step 5 — Application: `TouchstoneService.RollRemorseAsync`

**Layer:** `RequiemNexus.Application`

**What to do:**
- Add `RollRemorseAsync(int characterId, string userId)` to `ITouchstoneService` / `TouchstoneService`.
- Pool: `Humanity` dice; +1 die if character has at least one active `Touchstone`.
- At Humanity 0: chance die.
- Outcomes mirror degeneration outcomes (success/fail/dramatic fail → same `HumanityService` calls).
- Only callable when stains are present but _below_ the degeneration threshold (enforce this guard; return `Result.Failure` otherwise).
- Publish dice roll to session feed.

**Tests:**
- Unit: no stains → `Result.Failure("No stains to roll remorse for")`.
- Unit: stains at threshold → `Result.Failure("Use degeneration roll, not remorse")`.
- Unit: active Touchstone → pool is `Humanity + 1`.
- Unit: no active Touchstone → pool is `Humanity`.

---

### Step 6 — UI: Degeneration Roll Panel (Glimpse)

**Layer:** `RequiemNexus.Web`

**What to do:**
- Add a **Glimpse banner** that appears when `DegenerationCheckRequiredEvent` is raised for any character in the Storyteller's active chronicle.
  - Use the existing domain event / SignalR pattern to push the banner state to the Glimpse page.
  - Banner text: _"[Character Name] must roll degeneration (Resolve + [7−Humanity])."_
- Clicking the banner opens a confirmation modal showing the pool and the three possible outcomes.
- On confirm: call `ExecuteDegenerationRollAsync`; the result publishes to the dice feed and the banner dismisses.
- **Authorization:** ST can always trigger the roll. A player may call `ExecuteDegenerationRollAsync` for their own character (enforce `RequireCharacterOwnerAsync` in the service). Apply the 4-step AuthorizationHelper sequence — not a UI-only guard.
- The ST cannot dismiss the banner without rolling.
- If the character is the player's own, add a **"Roll Degeneration"** button to the character sheet Humanity section (only visible when the banner condition is active).
- Phase 17 should **extend** `DegenerationCheckRequiredEventHandler` (via SignalR push) rather than introduce a parallel notification path — the handler is the single wiring point.

---

### Step 7 — UI: Remorse Roll Button

**Layer:** `RequiemNexus.Web`

**What to do:**
- Add a **"Roll Remorse"** button to:
  - The character sheet **Humanity section** (player-facing).
  - The **Storyteller Glimpse** character panel.
- Visibility rule: button appears when `HumanityStains > 0` AND `HumanityStains < degenerationThreshold`.
- On click: call `RollRemorseAsync`; publish result to dice feed; refresh humanity/stain display.
- Show active Touchstone count in the button tooltip: _"Pool: [Humanity + Touchstones] dice"_.

---

### Step 8 — UI: Incapacitated Flag

**Layer:** `RequiemNexus.Web`

**What to do:**
- When a character's health track is fully aggravated (the existing `CharacterHealthService` already tracks this), the **player-facing character sheet** should render an **Incapacitated overlay** suppressing all action buttons (attack, discipline activation, hunting, social maneuvers).
- The **Storyteller Glimpse** bypasses the overlay (ST can still apply coup de grâce / death conditions from the Glimpse panel).
- This is a UI-only concern — no new domain logic required; read the existing health state.
- Add an `IsIncapacitated` computed property to the character sheet view model for clean template binding.

---

### Step 9 — Rules Interpretation Log

**File:** `docs/rules-interpretations.md`

**Entries to add:**
- **Degeneration threshold formula** — `stains ≥ Humanity` (VtR 2e p.185: "a number of Stains equal to or greater than her Humanity score"). Already implemented in `HumanityService.EvaluateStainsAsync`. Record this page cite so the formula is never accidentally changed.
- **Touchstone bonus justification** — +1 die rationale; any ambiguity in the rulebook.
- **Stain-clearing behavior on both degeneration outcomes** — both success and failure clear stains (only the dot removal differs).
- **Chance die at Humanity 0** — how `Resolve + 7` resolves when Humanity is 0.
- **Condition penalty scope** — only `ConditionType` enum members receive pool penalties here. `Stunned` and `Blind` are `TiltType` values handled by `ConditionRules.GetTiltEffects()` — they are not re-implemented in this step.

---

### Phase 17 — Acceptance Criteria

- [x] `IConditionRules.GetPenalties(ConditionType.Shaken)` returns `[("AllPools", -2)]` (via `ConditionPoolTarget.AllPools`).
- [x] Rolling dice for a `Shaken` character produces a pool 2 dice smaller than normal (`ModifierService` + `ModifierServiceTests`; trait rolls use aggregated modifiers).
- [x] Accumulating stains to the threshold triggers the Glimpse degeneration banner (`DegenerationCheckRequiredEventHandler` → `ChronicleUpdateDto.DegenerationCheckRequired` + Glimpse hub).
- [x] Rolling degeneration with 0 successes removes a Humanity dot and clears stains (`HumanityServiceDegenerationTests`).
- [x] Rolling degeneration with a dramatic failure additionally applies `Guilty` (`HumanityServiceDegenerationTests`).
- [x] `RollRemorseAsync` adds +1 die when a Touchstone is present (`TouchstoneServiceRemorseTests`).
- [x] Remorse roll fails fast when no stains are present (`TouchstoneServiceRemorseTests`).
- [x] Incapacitated player sheet suppresses all action buttons (`CharacterDetails` overlay + blocked interactive region).
- [x] ST Glimpse shows action buttons for incapacitated characters (Glimpse is a separate page; no overlay applied there).
- [x] Incapacitated overlay exposes `role="alert"` and a concise `aria-label` (Phase 13 a11y).
- [x] Phase 17 rules entries added to `rules-interpretations.md` (degeneration, remorse, condition vs tilt scope).
- [x] `dotnet format` passes; `.\scripts\test-local.ps1` passes (including new tests).

---

## 📅 Phase 18: The Wider Web — Edge Systems & Content

**Objective:** Close remaining mechanical gaps and fill the core-book content catalog.

Phase 18 has four sub-tracks that can be worked independently:
- **Track A:** Passive Predatory Aura
- **Track B:** Blood Sympathy Roll
- **Track C:** Social Maneuvering Interception
- **Track D:** Content passes (data-only, no code changes)

---

### Track A — Passive Predatory Aura

#### Step A1 — Application: `PassiveAuraService`

**Layer:** `RequiemNexus.Application`

**What to do:**
- Create `IPassiveAuraService` / `PassiveAuraService`.
- Method: `TriggerPassiveContestAsync(int vampireAId, int vampireBId, string triggeredByUserId)`.
- Guard: both characters must share the same `ChronicleId`; return `Result.Failure` if not.
- Guard: if these two characters have already contested this scene (track by `CombatEncounterId` or a new lightweight "scene contest log"), skip duplicate invocation.
- Delegate to existing `PredatoryAuraService` with `IsLashOut = false`.
- Outcome conditions applied via existing logic.
- Publish result to dice feed.

**Tests:**
- Unit: characters in different chronicles → `Result.Failure`.
- Unit: already contested this scene → no-op.
- Unit: valid call → delegates to `PredatoryAuraService` with correct flag.

#### Step A2 — Domain: Scene Context Hook

**What to do:**
- When two vampires are added to the same `CombatEncounter`, raise a domain event or call `PassiveAuraService.TriggerPassiveContestAsync` for every new vampire-pair not yet contested in that encounter.
- Use the existing `DomainEventDispatcher` pattern: raise `VampireAddedToEncounterEvent(EncounterId, CharacterId)`, handled by `PassiveAuraContest​Handler` that checks all existing encounter members.
- Record the "already contested" state in a new `EncounterAuraContest` join table (columns: `EncounterId`, `VampireAId`, `VampireBId`; unique constraint on sorted pair) — or store it in-memory via the encounter aggregate if the encounter is loaded in full. A persistent table is safer. Migration: `Phase18EncounterAuraContest`.

#### Step A3 — UI: Passive Aura Notification & ST Toggle

**Layer:** `RequiemNexus.Web`

**What to do:**
- When `PassiveAuraService` fires, the contest outcome appears in the **dice feed** with the label _"Passive Predatory Aura: [VampireA] vs [VampireB]"_.
- Add a **"Trigger Passive Aura Contest"** button on the Glimpse NPC panel, visible for any pair of vampires in the current chronicle. This is the ST manual trigger path.

---

### Track B — Blood Sympathy Roll

#### Step B1 — Application: `BloodSympathyService.RollBloodSympathyAsync`

**Layer:** `RequiemNexus.Application`

**What to do:**
- Add `RollBloodSympathyAsync(int characterId, int targetCharacterId, string userId)` to `IBloodSympathyService` / `BloodSympathyService`.
- Pool: `Wits + Empathy + BloodSympathyRating` (the rating is already stored on the existing blood tie entity from Phase 12).
- Guard: the target must be a known kindred (existing `BloodTie` / lineage record); return `Result.Failure` if no tie exists.
- Submit pool to `DiceService`; publish to dice feed.
- Returns the rolled result (successes) to the UI for display.

**Tests:**
- Unit: no blood tie to target → `Result.Failure`.
- Unit: valid tie → pool is `Wits + Empathy + BloodSympathyRating`.

#### Step B2 — UI: "Sense Blood Kin" Button

**Layer:** `RequiemNexus.Web`

**What to do:**
- Add a **"Sense Blood Kin"** button on the character sheet **Lineage section** (alongside existing Sire/Childer display).
- On click: open a modal listing known kindred (from blood tie records); player selects target; call `RollBloodSympathyAsync`; result appears in dice feed.

---

### Track C — Social Maneuvering Interception

#### Step C1 — Data: `ManeuverInterceptor` Entity

**Layer:** `RequiemNexus.Data`

**What to do:**
- Create `ManeuverInterceptor` entity:
  ```csharp
  class ManeuverInterceptor
  {
      int Id { get; set; }
      int SocialManeuverId { get; set; }
      int InterceptorCharacterId { get; set; }
      bool IsActive { get; set; }
      int Successes { get; set; }
  }
  ```
- Add FK navigation to `SocialManeuver` and `Character`.
- Migration: `Phase18ManeuverInterceptor`.

#### Step C2 — Application: Interception Logic in `SocialManeuveringEngine`

**Layer:** `RequiemNexus.Application`

**What to do:**
- In `SocialManeuveringEngine`, before applying door reductions from the initiator's roll:
  1. Load all active `ManeuverInterceptor` rows for the maneuver.
  2. For each interceptor with `Successes > 0`, subtract interceptor successes from the initiator's net successes (floor at 0).
  3. After interception, resume normal door-reduction logic.
- Add `AddInterceptorAsync(int socialManeuverId, int interceptorCharacterId, string userId)` to `ISocialManeuveringService`.
- Add `RecordInterceptorRollAsync(int interceptorId, int successes, string userId)` to record the dice result.

**Tests:**
- Unit: interceptor with 2 successes reduces initiator's 3 successes to 1.
- Unit: interceptor successes ≥ initiator successes → no door reduction.
- Unit: no interceptors → existing behavior unchanged.

#### Step C3 — UI: ST "Add Interceptor" Flow

**Layer:** `RequiemNexus.Web`

**What to do:**
- On the Glimpse active maneuvers list, add an **"Add Interceptor"** button per maneuver.
- ST selects a character from the chronicle roster to become the interceptor.
- The interceptor rolls via the existing dice modal; the result is recorded via `RecordInterceptorRollAsync`.
- The dice feed announces: _"[Interceptor] contests [Initiator]'s maneuver on [Target]."_

#### Step C4 — Rules Interpretation Log

- **`PassiveAuraService` "same scene" definition** — defined as two vampires in the same `CombatEncounter`; no ambient scene detection beyond that boundary.
- **Interception pool** — `Manipulation + Persuasion` vs. initiator (cite rulebook).
- **Tie-breaking** — if net successes after interception are tied at 0, no doors reduce (clarify).

---

### Track D — Content Passes (Data-Only)

All items below are JSON seed additions to `SeedSource/` and `DbInitializer` extension calls. Zero business logic changes. Each can be done independently. Reference the existing `CovenantJsonImporter` / `DisciplineJsonImporter` pattern.

#### D1 — Theban Sorcery Full Catalog

- File: `src/RequiemNexus.Data/SeedSource/bloodSorceryRites.json`
- Add all Miracles from VtR 2e core book (Theban Sorcery chapter).
- Each entry: `name`, `description`, `sorceryType: "ThebanSorcery"`, `dotLevel`, `activationCostDescription`, `RequirementsJson` (if any).
- Call from `DbInitializer.EnsureBloodSorceryPhaseExtensionsAsync`.

#### D2 — Crúac Full Catalog

- Same file or separate `crucacRites.json`.
- Add all Rites from VtR 2e core book (Crúac chapter).
- Each entry: `sorceryType: "Crucac"` + standard fields.

#### D3 — Ordo Dracul Coil Catalog

- File: `src/RequiemNexus.Data/SeedSource/coils.json`
- Add all 5 Mysteries × 5 Coils from VtR 2e core book.
- Each Coil entry: `name`, `mystery` (enum or string), `dotLevel`, `description`, `PassiveModifierJson` where applicable.
- Call from `DbInitializer.EnsureCoilsAsync` (create if not exists; follow `EnsureBloodSorceryPhaseExtensionsAsync` pattern).

#### D4 — Necromancy Catalog Expansion

- Extend `bloodSorceryRites.json` with additional Necromancy rites beyond the Phase 9.6 sample (`Corrupting the Corpse`).
- `sorceryType: "Necromancy"`, `RequiredClanId` gating per Phase 9.6 rules.

#### D5 — Devotion Catalog Expansion

- File: `src/RequiemNexus.Data/SeedSource/devotions.json`
- Add remaining clan/covenant-specific Devotions from VtR 2e core book.
- Follow existing `DevotionDefinition` seed schema (prerequisite Disciplines, `OrGroupId`, XP cost, `PoolDefinitionJson`).
- Call from `DbInitializer.EnsureDevotionsAsync`.

#### D6 — Loresheet Merits

- Add `Merit` seed entries for all Loresheet entries from VtR 2e core book.
- In the existing `merits.json` (or create `loresheetMerits.json`).
- Mark with a `category: "Loresheet"` field to allow filtered display in the UI.

---

### Phase 18 — Acceptance Criteria

**Track A (Passive Aura):**
- [x] Adding two vampires to the same encounter auto-triggers a passive aura contest for each new pair.
- [x] Contest outcome appears in the dice feed.
- [x] ST can manually trigger a passive aura contest from the Glimpse overview (Kindred pair selector).
- [x] Duplicate contests in the same encounter are silently skipped.

**Track B (Blood Sympathy):**
- [x] "Sense Blood Kin" button visible on the Lineage section when blood ties exist.
- [x] Pool is `Wits + Empathy + BloodSympathyRating`.
- [x] Roll result appears in the dice feed.

**Track C (Interception):**
- [x] ST can add an interceptor to any active maneuver.
- [x] Interceptor's successes reduce the initiator's net door reductions.
- [x] Dice feed announces the interception.
- [x] Existing maneuver behavior is unchanged when no interceptors are present.

**Track D (Content):**
- [x] Full Theban Sorcery catalog seeded and selectable in advancement (JSON + idempotent `EnsureMissingSorceryRiteCatalogEntriesAsync`).
- [x] Full Crúac catalog seeded and selectable in advancement (same).
- [x] Coils: `coils_info.json` incremental `SeedCoilsAsync` (35 coil powers across mysteries; meets catalog visibility goal).
- [x] Necromancy catalog expanded (`necromancyRites.json` + ensure; Phase 9.6 sample retained).
- [x] Devotion catalog: `DevotionSeedData.EnsureMissingDefinitionsAsync` adds rows from `devotions.json` not yet in DB.
- [x] Loresheet Merits: `loresheetMerits.json` with `category: "Loresheet"`, `MeritCategory` column, advancement dropdown shows `[Loresheet]`.

**All:**
- [x] `dotnet format` passes; solution tests green (run full `.\scripts\test-local.ps1` before PR).
- [x] Rules interpretations for Phase 18 (Tracks A–C) recorded in `docs/rules-interpretations.md`.

---

## 🗓️ Suggested Execution Order

```
Week 1 — Phase 17 (domain + services)
  ├── Step 1: `IConditionRules.GetPenalties` + `ConditionPenaltyModifier` (no migration)
  ├── Step 2: `ConditionModifierSource` in `ModifierService`
  ├── Step 3: Wire `EvaluateStainsAsync` at all stain call sites + idempotency policy
  └── Step 4: `ExecuteDegenerationRollAsync`

Week 2 — Phase 17 (services + UI)
  ├── Step 5: TouchstoneService.RollRemorseAsync
  ├── Step 6: Degeneration Roll UI (Glimpse banner)
  ├── Step 7: Remorse Roll UI
  └── Step 8: Incapacitated flag UI

Week 3 — Phase 18 Tracks A & B (mechanical)
  ├── Track A: PassiveAuraService + EncounterAuraContest migration
  ├── Track A: Scene context hook (VampireAddedToEncounterEvent handler)
  └── Track B: BloodSympathyService.RollBloodSympathyAsync

Week 4 — Phase 18 Track C (interception)
  ├── Step C1: ManeuverInterceptor entity + migration
  ├── Step C2: SocialManeuveringEngine interception logic
  └── Step C3: ST UI "Add Interceptor"

Week 5 — Phase 18 Track D (content passes)
  ├── D1: Theban Sorcery full catalog
  ├── D2: Crúac full catalog
  ├── D3: Ordo Dracul Coils
  ├── D4: Necromancy expansion
  ├── D5: Devotion expansion
  └── D6: Loresheet Merits

Week 6 — Polish & verification
  ├── End-to-end tests for all new flows
  ├── Rules interpretation log final entries
  └── Final dotnet format + test-local.ps1 pass
```

---

## 🔗 Key Files to Read Before Starting Each Phase

| Work | Files to read first |
|------|---------------------|
| Phase 17 — Condition penalties | `src/RequiemNexus.Domain/Enums/ConditionType.cs`, `src/RequiemNexus.Domain/Contracts/IConditionRules.cs`, `src/RequiemNexus.Domain/Services/ConditionRules.cs`, `src/RequiemNexus.Application/Services/ModifierService.cs` |
| Phase 17 — Degeneration rolls | `src/RequiemNexus.Application/Services/HumanityService.cs`, `src/RequiemNexus.Domain/Events/DegenerationCheckRequiredEvent.cs`, `src/RequiemNexus.Application/Events/Handlers/DegenerationCheckRequiredEventHandler.cs` |
| Phase 17 — Remorse | `src/RequiemNexus.Application/Services/TouchstoneService.cs`, character sheet Humanity section Razor component |
| Phase 18 — Passive Aura | `src/RequiemNexus.Application/Services/PredatoryAuraService.cs`, `src/RequiemNexus.Data/Models/CombatEncounter.cs`, Phase 12 blood tie entities |
| Phase 18 — Interception | `src/RequiemNexus.Domain/Services/SocialManeuveringEngine.cs`, `src/RequiemNexus.Data/Models/SocialManeuver.cs` |
| Phase 18 — Content | `src/RequiemNexus.Data/SeedSource/`, `src/RequiemNexus.Data/DbInitializer.cs`, any existing `*JsonImporter.cs` |

---

> _The blood remembers._
> _The code must too._
> _These are the last rites before The Global Embrace._
