# 🩸 Playability Gap Plan — Closing the V:tR 2e Mechanics Debt

> _The blood is the life… but only if the engine knows what blood does._

This document maps every identified gap between the current Requiem Nexus implementation and a fully playable **Vampire: The Requiem 2e** chronicle. Gaps are organized into six proposed phases (15–20), ordered by table-session impact. Each phase is self-contained and independently shippable.

---

## 📊 Gap Summary

| # | Gap | Severity | Proposed Phase |
|---|-----|----------|----------------|
| 1 | Combat attack / damage / armor pipeline | 🔴 Critical | 15 |
| 2 | Wound penalties auto-applied to pools | 🔴 Critical | 15 |
| 3 | Healing mechanics (Vitae spend to heal) | 🔴 Critical | 15 |
| 4 | Damage-type conversion (B/L/A)  | 🔴 Critical | 15 |
| 5 | Frenzy save rolls not automated | 🔴 Critical | 16 |
| 6 | Rötschreck automation | 🔴 Critical | 16 |
| 7 | Torpor state (entry, awakening, starvation) | 🟠 High | 16 |
| 8 | Hunting / feeding roll automation | 🟠 High | 17a |
| 9 | Vitae gain mechanics and resonance | 🟠 High | 17a |
| 10 | Discipline power activation (cost + pool) | 🟠 High | 17b _(blocked on Phase 20 schema)_ |
| 11 | Humanity degeneration rolls | 🟡 Medium | 18 |
| 12 | Condition penalties fully wired to pools | 🟡 Medium | 18 |
| 13 | Remorse / Humanity anchor checks | 🟡 Medium | 18 |
| 14 | Passive Predatory Aura (first-meeting contest) | 🟢 Low | 19 |
| 15 | Blood Sympathy roll trigger | 🟢 Low | 19 |
| 16 | Social Maneuvering interception | 🟢 Low | 19 |
| 17 | Expanded ritual / rite catalog (content pass) | 🟢 Low | 19 |
| 18 | `Disciplines.json` not wired to seeder (`DisciplineSeedData.cs` is orphaned override) | 🔴 Critical | 20 |
| 19 | `Discipline` model missing acquisition metadata (teacher, blood, covenant, bloodline flags) | 🔴 Critical | 20 |
| 20 | `CharacterDisciplineService` enforces only XP — none of the rules from `DisciplinesRules.txt` | 🔴 Critical | 20 |
| 21 | Character creation 3-dot rule not enforced (2 must be in-clan) | 🔴 Critical | 20 |
| 22 | Crúac Humanity cap (`10 − Crúac dots`) not applied | 🔴 Critical | 20 |
| 23 | Theban Sorcery Humanity-floor prerequisite not enforced | 🔴 Critical | 20 |
| 24 | Celerity / Resilience / Vigor powers have generic placeholder names, not rulebook names | 🟡 Medium | 20 |
| 25 | `DisciplinePower` has no `PoolDefinitionJson` — blocks Phase 17b activation pipeline | 🟠 High | 20 |

---

## 📅 Phase 15: The Danse Macabre — Combat & Wounds

**The Objective:** Build the attack-to-damage pipeline so that initiative resolution leads to real mechanical outcomes, not just health box bookkeeping by hand.

### Architectural Decisions

- **Attack is just a dice roll with a structured result.** An `AttackResult` value object (Domain) captures successes, weapon damage dice, damage type, and a `DamageSource` tag. `DamageSource` is included from day one (Bashing, Lethal, Aggravated, Fire, Sunlight) to avoid retrofitting when Phase 16 adds fire/sunlight interactions. No new entity is needed — the existing `CombatEncounter` and `InitiativeEntry` anchor it.
- **Phase 15 MVP boundary: melee + generic weapon profile only.** Ranged/firearms, improvised weapons, and touch attacks are valid follow-on slices. The first shippable cut is: one attack roll → bashing or lethal damage → armor mitigation → health-box update → wound penalty in pool. This boundary is recorded in `rules-interpretations.md` when the slice ships.
- **Damage application flows through `HealthService`** (Application) which already tracks health boxes. The new path adds damage-type logic (bashing overflow → lethal, lethal overflow → aggravated) without touching unrelated character data.
- **Wound penalty uses the existing `ModifierTarget.WoundPenalty` path.** `ModifierTarget.WoundPenalty` already exists in the Domain enum. `WoundPenaltyResolver` (Domain, pure function) reads `Character.Health` and returns 0 / -1 / -2 / Incapacitated. Its output is injected into `ModifierService.GetModifiersForCharacterAsync` as a `PassiveModifier` with `Target = ModifierTarget.WoundPenalty`, so `TraitResolver` receives it via the same aggregation loop as Coils and equipment — no special-case caller code.
- **Defense is sourced from the existing derived stat on `Character`.** `AttackService` reads `character.Defense` directly. Defense vs. firearms and unaware-target rules are documented in `rules-interpretations.md`; dodge actions are out of scope for Phase 15.
- **Healing is a Vitae spend.** Existing `VitaeService.SpendVitaeAsync` is extended with a `HealingReason` enum variant. The healed boxes are recorded in `HealthService`. Fast healing (heal one bashing per round for 1 Vitae, lethal requires rest + more Vitae) is modeled as a structured cost, not a timer.

### Tasks

- [ ] **`DamageSource` enum** — Domain: Bashing, Lethal, Aggravated, Fire, Sunlight, Weapon. Added to `AttackResult` from day one; Phase 16 `FrenzyService` reuses `Fire` and `Sunlight` tags for Rötschreck triggers.
- [ ] **`AttackResult` value object** — Domain: successes, weapon damage dice, `DamageSource`, defense applied, net damage. No EF dependency.
- [ ] **`AttackService`** — Application: resolves attacker pool via `TraitResolver`, reads `character.Defense`, produces `AttackResult`. First slice: melee only. Masquerade: verifies encounter ownership.
- [ ] **Damage application in `HealthService`** — B/L/A overflow rules per VtR 2e p.172: bashing above max overflows to lethal; lethal above max overflows to aggravated. Applies in correct health-box order.
- [ ] **`WoundPenaltyResolver`** — Domain: reads current health track, returns `PassiveModifier(Target = WoundPenalty, Delta = 0/-1/-2)` or an `Incapacitated` flag. Pure function, no EF dependency.
- [ ] **`ModifierService.GetModifiersForCharacterAsync` extension** — Application: call `WoundPenaltyResolver` in the existing aggregation loop alongside Coils and equipment modifiers. No change to `TraitResolver` or callers.
- [ ] **Healing via `VitaeService`** — extend existing Vitae spend path; add `HealingReason` (FastHealBashing, HealLethal, HealAggravated). Costs from rulebook (p.173) enforced as structured cost constants in Domain.
- [ ] **Combat UI — Attack Panel** — Storyteller Glimpse: "Roll Attack" per initiative entry → choose pool → open `DiceRollerModal` → on confirm, call `AttackService` → damage applied automatically.
- [ ] **Combat UI — Heal Panel** — Character sheet and Glimpse: "Spend Vitae to Heal" button with cost preview.
- [ ] **Rules Interpretation Log** — MVP boundary (melee-first), Defense vs. firearms rule, `DamageSource` tag mapping to damage type, and B/L/A overflow edge cases in `docs/rules-interpretations.md`.

---

## 📅 Phase 16: The Beast Within — Frenzy & Torpor

**The Objective:** Give the Beast teeth. Frenzy and Torpor are core VtR 2e horror mechanics; without them every character is just a powerful human.

### Architectural Decisions

- **Frenzy is a contested save, not a toggle.** `FrenzyService` (Application) receives a `FrenzyTrigger` enum (`Hunger`, `Rage`, `Rotschreck`, `Starvation`) and executes a `Resolve + Blood Potency` pool via `DiceService`. Failure applies the `Frenzy` Tilt or `Rotschreck` Tilt. Willpower spend is optional (subtract 1 from pool cost, same pattern as existing Willpower spends).
- **Triggers are explicit, not automatic.** The app does not poll ambient conditions. Triggers are fired by: (a) the Storyteller via Glimpse, (b) the character's Vitae dropping to 0, or (c) a player confirming they are exposed to fire / sunlight. This matches the "no narrative automation" principle.
- **Torpor is a character state, not a separate entity.** A nullable `TorporSince` timestamp on `Character` is sufficient. A `TorporService` (Application) encapsulates entry, awakening, and starvation-hunger-escalation logic.
- **Hunger / starvation escalation during Torpor** is modeled as a new `TorporIntervalService : BackgroundService` in `RequiemNexus.Web/BackgroundServices/`, following the exact pattern of the existing `SessionTerminationService`. It runs on a configurable cadence (default: nightly `IHostedService` timer) and raises a Storyteller notification when a character's torpor interval has elapsed. An "Advance Time" action on the ST Glimpse panel calls the same `TorporService` interval logic on demand, covering in-session time jumps without changing the scheduler.

### Tasks

- [ ] **`FrenzyTrigger` enum** — Domain: `Hunger` (Vitae 0 **in active play** — synchronous `VitaeDepletedEvent`; not torpor-interval hunger), `Rage` (provocation in combat), `Rotschreck` (fire or sunlight exposure), `Starvation` (torpor hunger escalation — background interval / Advance Time only; never the same code path as `VitaeDepletedEvent`). Pure value, no EF.
- [ ] **`FrenzyService`** — Application: `RollFrenzySaveAsync(characterId, trigger, spendWillpower)` — resolves `Resolve + Blood Potency` via `TraitResolver`, applies `Frenzy` or `Rotschreck` Tilt on failure, records to dice history feed. Tilt application must be wrapped in a single `SaveChangesAsync` transaction: the guard check (is tilt already active?) and the insert must be atomic to prevent duplicate rows under concurrent `VitaeDepletedEvent` handlers. Use EF's optimistic concurrency (row version or unique index on `CharacterId + TiltType + IsActive`) rather than application-level locking.
- [ ] **Willpower spend path in `FrenzyService`** — subtract 1 die from the pool; spend 1 Willpower box (re-use `WillpowerService` if it exists, else add).
- [ ] **Vitae-zero trigger** — `VitaeService.SpendVitaeAsync` checks for Vitae reaching 0 and raises a `VitaeDepletedEvent` (in-process Domain event, not SignalR). `FrenzyService` subscribes. The event is idempotent by design: concurrent spend calls reaching 0 both raise `VitaeDepletedEvent`, but `FrenzyService` checks whether a `Frenzy` Tilt is already active before applying a second save — duplicate saves are suppressed, not queued.
- [ ] **`TorporSince` on `Character`** — Data: nullable datetime column, migration. Domain: `Character.IsInTorpor` computed property.
- [ ] **`TorporService`** — Application: `EnterTorporAsync`, `AwakenFromTorporAsync` (Storyteller action, costs one Vitae or anchor moment per book p.165), starvation-interval check (raises ST notification at correct torpor-length thresholds).
- [ ] **`TorporIntervalService`** — `Web/BackgroundServices/TorporIntervalService.cs` extending `BackgroundService`, following the `SessionTerminationService` pattern. Configurable timer (default 24 h). On each tick: queries characters where `IsInTorpor = true` and interval since `TorporSince` exceeds the hunger threshold for their Blood Potency; raises Storyteller notification via the existing notification channel. No direct DB write — notification only.
- [ ] **Torpor UI** — Character sheet badge and Storyteller Glimpse panel: enter/awaken buttons, "Torpor Since" display, starvation notification banner. "Advance Time" button on ST panel triggers the same `TorporService` interval check on demand.
- [ ] **Frenzy save UI — player** — Character sheet: "I am exposed to fire / sunlight" button triggers `Rotschreck` save; result posted to real-time dice feed. Rötschreck uses the same `Resolve + Blood Potency` pool as other frenzy types; the book does not specify a separate pool (documented in rules log).
- [ ] **Frenzy save UI — ST** — Storyteller Glimpse: "Trigger Frenzy Save" per character with trigger-type picker (Hunger / Rage / Rotschreck). NPC frenzy saves are ST-only; PC saves visible to the player.
- [ ] **Rules Interpretation Log** — Torpor duration table (BP 1–10 → weeks to centuries), hunger escalation rate, Rötschreck pool choice, and the "one Vitae to awaken" interpretation.

---

## 📅 Phase 17a: The Hunting Ground — Feeding

**The Objective:** Make feeding a first-class mechanical action with per-predator-type pools and a resonance outcome. Independent of Phase 20.

### Architectural Decisions

- **Hunting is a pool roll wired to Predator Type.** `HuntingService` (Application) reads the character's `PredatorType`, selects the canonical hunting pool from a seed table (`HuntingPoolDefinition`), resolves it via `TraitResolver`, and returns successes mapped to Vitae gained (1 success = 1 Vitae baseline; bonus successes can exceed).
- **Territory is optional, not required.** `ExecuteHuntAsync(characterId, territoryId?)` — when `territoryId` is provided, territory quality (1–5 rating, already tracked on `FeedingTerritory`) is added as a flat bonus die to the pool. Territory ownership is not validated — the Storyteller owns that narrative gate. When `territoryId` is null, the roll proceeds without the bonus.
- **Feeding resonance is seed data, not business logic.** A `ResonanceTable` seed (JSON → `DbInitializer`) maps success thresholds to `ResonanceOutcome` quality (Fleeting / Weak / Functional / Saturated). `HuntingService` attaches the result; the character sheet displays it. No mechanical effects beyond display are automated in this phase.

### Tasks

- [ ] **`HuntingPoolDefinition` seed table** — Data: one row per `PredatorType` containing `PoolDefinitionJson`, `BaseVitaeGain`, `PerSuccessVitaeGain`, short narrative description. Seeded in `DbInitializer`.
- [ ] **`HuntingService`** — Application: `ExecuteHuntAsync(characterId, territoryId?)` — resolves pool (+ territory quality bonus when provided), maps successes to Vitae, applies Vitae gain via `VitaeService`, records resonance result. Masquerade: character ownership check.
- [ ] **`ResonanceOutcome` enum** — Domain: Fleeting, Weak, Functional, Saturated. `ResonanceTable` JSON seeded in `SeedSource/` mapping success-count to outcome.
- [ ] **Hunt history ledger** — `HuntingRecord` entity (characterId, territoryId, pool successes, Vitae gained, resonance, timestamp). Lightweight audit, mirrors Beat / XP ledger pattern.
- [ ] **Hunting UI** — Character sheet "Hunt" button → optional territory picker → rolls → shows Vitae gained + resonance + narrative outcome. Existing Phase 13 announcer pattern used for screen-reader announcement of result.
- [ ] **Rules Interpretation Log** — Hunting pool choices per predator type, resonance table interpretation, Vitae-per-success scaling, territory bonus formula.

---

## 📅 Phase 17b: The Discipline Engine — Power Activation

**The Objective:** Give each seeded `DisciplinePower` an activation button with pool resolution and cost enforcement.

**Dependency:** Requires Phase 20 to have shipped `DisciplinePower.PoolDefinitionJson` (migration + importer). Phase 17b cannot start until Phase 20's data-model slice is merged.

### Architectural Decisions

- **Discipline activation is a wrapper around the existing `TraitResolver`.** `DisciplineActivationService` (Application) receives a `disciplinePowerId` and `characterId`, reads the seeded `DisciplinePower.PoolDefinitionJson` and `Cost`, calls `TraitResolver`, deducts cost, and posts the result to the dice feed. Everything else is already built in Phase 20.
- **Cost deduction is atomic.** Vitae and Willpower spends for Discipline activation go through the same `VitaeService` / `WillpowerService` path as all other spends — no separate code path.
- **Powers with null `PoolDefinitionJson` remain display-only** — the "Activate" button is suppressed for those rows until a content pass populates their pool.

### Tasks

- [ ] **`DisciplineActivationService`** — Application: `ActivatePowerAsync(characterId, disciplinePowerId, optional contested target)` — reads `DisciplinePower.PoolDefinitionJson`, resolves pool via `TraitResolver`, deducts `ActivationCost`, posts result to dice feed. Masquerade: ownership check.
- [ ] **`ActivationCost` value object** — Domain: parse `DisciplinePower.Cost` string (`"1 Vitae"`, `"1 Willpower"`, `"—"`) into a typed cost (same pattern as `SorceryRiteDefinition.RequirementsJson`). `DisciplineActivationService` enforces before rolling.
- [ ] **Discipline activation UI** — Character sheet Disciplines section: each power row with a populated `PoolDefinitionJson` gets an "Activate" button. Opens cost-preview modal → confirm → resolves pool → result in dice feed. Powers with null pool remain display-only. Passive powers remain display-only regardless.
- [ ] **Rules Interpretation Log** — cost enforcement choices, any pool edge cases not covered by existing `TraitResolver` contract.

---

## 🔔 Shared Domain Event: `DegenerationCheckRequired`

Both Phase 18 and Phase 20 raise this event. It is defined once in the Domain layer and handled by a single Application-layer handler — do not create two separate event types.

```csharp
// Domain/Events/DegenerationCheckRequired.cs
public record DegenerationCheckRequired(
    int CharacterId,
    DegenerationReason Reason);   // enum: StainsThreshold, CrúacPurchase

public enum DegenerationReason { StainsThreshold, CrúacPurchase }
```

**Phase 20** raises it (with `Reason = CrúacPurchase`) before the Phase 18 roll UI exists — at that point the handler logs a Glimpse notification but cannot yet open the roll dialog. Once Phase 18 ships its UI, the same handler gains the "Roll Degeneration" button path. **Crúac purchase does not block** until Phase 18 exists; it creates an ST notification that can be dismissed.

---

## 📅 Phase 18: The Fog of Eternity — Humanity & Condition Wiring

**The Objective:** Close the gap between tracking moral decline and mechanically enforcing it. Make every Condition penalty show up in the dice pool without the player having to remember.

### Architectural Decisions

- **Degeneration is a triggered roll, not an automatic loss.** When `HumanityStains` crosses the threshold for the current Humanity dot, `HumanityService` raises a `DegenerationCheckRequired(Reason = StainsThreshold)` event (defined in the shared event block above). The Storyteller sees a Glimpse banner; clicking it fires a `Resolve + (7 − Humanity)` degeneration roll via `DiceService`. The system applies the result (lose a dot or remove stains) automatically.
- **Condition penalties are a modifier source, not special-cased code.** Each canonical `ConditionType` gains a nullable `PenaltyModifierJson` column (Application-layer model). `ModifierService` reads active conditions and injects their penalties into `TraitResolver` alongside equipment and Coil modifiers. No caller change is needed — the aggregated penalty just gets larger.
- **Remorse / anchor checks are explicit ST actions.** `TouchstoneService` exposes `RollRemorseAsync` (Storyteller triggers it) which rolls `Humanity` dice (chance die if Humanity = 0). Result either restores stains or confirms Humanity loss. Touchstone anchors reduce the cost (documented rule interpretation).

### Tasks

- [ ] **`PenaltyModifierJson` on canonical Conditions** — Data: nullable JSON column on condition seed data; migration. Add penalty entries for all canonical Conditions: Shaken (-2 pools), Exhausted (-2 physical pools), Frightened (-2 except fleeing), Guilty (-1 Resolve + Composure), Despondent (-2 Mental), Provoked (-1 Composure), Blind (-3 all attack / -2 all other), Stunned (no action this turn flag). Homebrew / custom condition types have `PenaltyModifierJson = null` and contribute no automatic penalty — Storyteller applies any custom penalty by hand. This is the intended fallback; no further schema is needed.
- [ ] **`ModifierService` — Condition source integration** — Application: add `ConditionModifierSource` to the aggregation loop. Reads `CharacterCondition` rows (active only), maps `ConditionType` → `PenaltyModifierJson`, injects into `TraitResolver` call. No change required outside `ModifierService`.
- [ ] **`HumanityService.EvaluateStainsAsync`** — Application: called after any stain increment. If stains ≥ (10 − current Humanity), raises `DegenerationCheckRequired` event → Storyteller Glimpse banner.
- [ ] **Degeneration roll UI** — Storyteller Glimpse: banner with "Roll Degeneration" button. Resolves `Resolve + (7 − Humanity)` dice. On success: clear stains. On failure: remove one Humanity dot, clear stains. On dramatic failure: remove dot + apply `Guilty` Condition.
- [ ] **`TouchstoneService.RollRemorseAsync`** — Application: rolls Humanity dice (chance die at Humanity 0); parses result; calls `HumanityService` to apply outcome. Touchstone anchor: if at least one Touchstone is active, add +1 die.
- [ ] **Remorse UI** — Character sheet and Glimpse: "Roll Remorse" button (active when stains are present but below the degeneration threshold — e.g., voluntary reflection). This is distinct from the forced degeneration roll.
- [ ] **Incapacitated flag enforcement** — When `WoundPenaltyResolver` returns `Incapacitated`, suppress "Roll" buttons on the **player-facing** character sheet UI only. Server-side enforcement is UI-only suppression — the Application layer still accepts roll requests originating from the Storyteller Glimpse view, which bypasses the suppression. This means the ST can always roll on a character's behalf (e.g., to resolve a coup de grâce) without a separate permission bypass.
- [ ] **Rules Interpretation Log** — Degeneration threshold formula, Touchstone bonus justification, stain-clearing behavior on both degeneration outcomes.

---

## 📅 Phase 19: The Wider Web — Edge Systems & Content

**The Objective:** Close the remaining low-priority mechanical gaps and fill the content catalog.

### Architectural Decisions

- **Passive Predatory Aura reuses existing `PredatoryAuraContest` infrastructure.** The `IsLashOut` column reserved in Phase 12 now drives the distinction. "Same scene" is defined as: two vampires added to the same `CombatEncounter` (automatic) OR a Storyteller manually triggering the contest from the Glimpse NPC panel (explicit). There is no automatic ambient scene detection beyond `CombatEncounter` — a session entity or location entity would require new scope. The ST toggle path covers all non-combat first-meetings. This decision is documented in `rules-interpretations.md`.
- **Blood Sympathy rolls are a thin wrapper.** `BloodSympathyService` already calculates the pool. A `RollBloodSympathyAsync` method on that service submits it to `DiceService` and posts to the dice feed. No new entity needed.
- **Social Maneuvering interception adds a third party to an existing `SocialManeuver`.** A `ManeuverInterceptor` join entity links a second character to an active maneuver. `SocialManeuveringEngine` checks for interceptors before applying door reductions; an interceptor can contest the roll.
- **Content passes are data migrations, not code changes.** All rite / Coil / Devotion catalog expansions are JSON seed additions to `SeedSource/` and a single `DbInitializer` extension call — no business logic changes required.

### Tasks

**Passive Predatory Aura**
- [ ] **`PassiveAuraService`** — Application: `TriggerPassiveContestAsync(vampireAId, vampireBId, sceneContext)` — calls existing `PredatoryAuraService` logic with `IsLashOut = false`. Masquerade: both characters must be in a shared Chronicle.
- [ ] **Scene context hook** — When two vampires are added to the same `CombatEncounter`, `PassiveAuraService` is invoked automatically for any pair not yet contested that scene.
- [ ] **UI** — "Passive aura contest happened" notification in dice feed; outcome Conditions applied as per existing logic.

**Blood Sympathy Rolls**
- [ ] **`BloodSympathyService.RollBloodSympathyAsync`** — Application: resolves `Wits + Empathy + BloodSympathyRating` via `TraitResolver`, posts to dice feed. Optional contested resistance pool (target's Composure + Blood Potency) deferred to future if desired.
- [ ] **UI** — Character sheet Lineage section: "Sense Blood Kin" button → selects target from known kindred → rolls → result in dice feed.

**Social Maneuvering Interception**
- [ ] **`ManeuverInterceptor` entity** — Data: `SocialManeuverId`, `InterceptorCharacterId`, `IsActive`, `Successes`. Migration.
- [ ] **`SocialManeuveringEngine` interception logic** — Before applying a door-reduction roll, check for active interceptors. Interceptor may contest (roll Manipulation + Persuasion vs. initiator); net successes subtract from effective door reductions.
- [ ] **ST UI** — Glimpse: "Add Interceptor" to any active maneuver; interceptor roll handled via existing dice modal.
- [ ] **Rules Interpretation Log** — Interception pool choice and contested-roll tie-breaking.

**Content Passes (Data-Only)**
- [ ] **Theban Sorcery full catalog** — All Miracles from VtR 2e core book seeded in `bloodSorceryRites.json`.
- [ ] **Crúac full catalog** — All Rites from VtR 2e core book seeded.
- [ ] **Ordo Dracul Coil catalog** — All 5 Mysteries, all 5 Coils per Mystery seeded in `coils.json`.
- [ ] **Necromancy catalog expansion** — Additional rites beyond the Phase 9.6 sample seeded.
- [ ] **Devotion catalog expansion** — Fill remaining clan/covenant-specific Devotions from core book into `devotions.json`.
- [ ] **Loresheet Merits** — `Merit` seed additions for Loresheet entries from core book; no engine change needed.

---

## 🗺️ Dependency Graph

```
Phase 15 (Combat)
    ├──► Phase 16 (Frenzy/Torpor)       ← VitaeDepletedEvent from Phase 15
    │         └──► Phase 18 (Humanity)  ← wound-penalty path + DegenerationCheckRequired UI
    └──► Phase 18 (Humanity)            ← WoundPenaltyResolver in ModifierService

Phase 17a (Hunting)   ← fully independent; no upstream dependencies
Phase 20  (Disciplines — model + seed)  ← independent of 15-18; can start now
    └──► Phase 17b (Discipline Activation)  ← needs PoolDefinitionJson from Phase 20

Phase 19 (Edge Systems) ← fully independent; content passes can start immediately
```

**Recommended parallel tracks:**
- Track A: Phases 15 → 16 → 18 (combat chain)
- Track B: Phase 20 → 17b (discipline chain, can start immediately)
- Track C: Phase 17a and Phase 19 (independent, any time)

---

## 📅 Phase 20: The Blood Lineage — Discipline Acquisition Rules & Seed Pipeline

**The Objective:** Make the Discipline system enforce the exact acquisition rules written in `docs/DisciplinesRules.txt`, promote `Disciplines.json` to the authoritative seed source, and add the `PoolDefinitionJson` field that Phase 17b needs for activation.

### Current State (what's broken)

| Component | Problem |
|-----------|---------|
| `Disciplines.json` | Exists in `SeedSource/` but is **not read by `DbInitializer`**. `DisciplineSeedData.cs` is the actual seed, making the JSON dead weight. |
| `Discipline` entity | No fields for `CanLearnIndependently`, `RequiresMentorBloodToLearn`, `IsCovenantDiscipline`, `CovenantId`, `IsBloodlineDiscipline`, `BloodlineId`. |
| `DisciplinePower` entity | No `PoolDefinitionJson` column — Phase 17b activation cannot resolve pools per-power. |
| `CharacterDisciplineService` | Validates XP and in-clan status only. Zero enforcement of teacher, blood-drinking, Covenant Status, Theban Humanity floor, Crúac Humanity cap, or bloodline restrictions. |
| Character creation | The "3 dots: ≥2 must be in-clan, 1 free" rule is not validated anywhere. |
| Power names | Celerity / Resilience / Vigor use placeholder names (`"Celerity 1"`, `"Resilience 2"`, etc.) rather than rulebook power names. |

### Acquisition Rules Reference (from `DisciplinesRules.txt`)

| Rule | Affects | Enforcement point |
|------|---------|-------------------|
| ≥2 of 3 creation dots must be in-clan | Character creation | `CharacterCreationService` or creation wizard |
| Animalism, Celerity, Obfuscate, Resilience, Vigor — learn independently | XP purchase | `CharacterDisciplineService` (allow without teacher flag) |
| Auspex, Dominate, Majesty, Nightmare, Protean out-of-clan — require teacher **and** Vitae drink | XP purchase | `CharacterDisciplineService` (soft gate: ST-acknowledged flag) |
| Crúac, Theban, Coils — require Covenant Status + teacher (no Vitae) | XP purchase | `CharacterDisciplineService` + `CovenantMembershipService` |
| Theban Sorcery dot N requires Humanity ≥ N | XP purchase | `CharacterDisciplineService` |
| Crúac dot 1 is a breaking point at Humanity 4+ | XP purchase | raise `DegenerationCheckRequired` event |
| Crúac permanently caps Humanity at `10 − Crúac dots` | Stat derived | `HumanityService.GetEffectiveMaxHumanity` |
| Bloodline Disciplines — bloodline members only; cannot be learned via diablerie | XP purchase | `CharacterDisciplineService` (check `CharacterBloodline`) |
| Necromancy — requires teacher OR cultural connection OR bloodline membership | XP purchase | `CharacterDisciplineService` (soft gate: ST-acknowledged flag) |

### Architectural Decisions

- **`Disciplines.json` becomes authoritative; `DisciplineSeedData.cs` is retired.** The JSON already follows the same pattern as `bloodlines.json` and `Covenants.json`. A `DisciplineJsonImporter` in `DbInitializer` reads it using the same `JsonSerializerOptions` pattern already in the codebase. `DisciplineSeedData.cs` is deleted once the importer covers all 12 core disciplines.
- **`Disciplines.json` schema is extended, not replaced.** New fields (`canLearnIndependently`, `requiresMentorBlood`, `isCovenantDiscipline`, `covenantName`, `isBloodlineDiscipline`, `bloodlineName`) are added to each entry. This is a content file change, not a schema-breaking one — missing fields default to false/null.
- **Acquisition gates are soft or hard depending on verifiability.** Teacher presence and Vitae-drinking cannot be verified by the app — they are *acknowledged gates*: `CharacterDisciplineService` requires a Storyteller-confirmed `AcquisitionAcknowledged` flag on the purchase request DTO. Mechanical prerequisites (Covenant Status, Humanity, bloodline membership) are hard gates enforced in code.
- **`DisciplinePower.PoolDefinitionJson` mirrors `DevotionDefinition.PoolDefinitionJson`** — same `PoolDefinition` serialization format, same `TraitResolver` contract. Phase 17b's `DisciplineActivationService` reads this column directly.
- **Crúac Humanity cap is a derived modifier, not a stored stat.** `HumanityService.GetEffectiveMaxHumanity(character)` returns `10 − character.CrúacRating` (the book formula directly; `Math.Min(10, …)` is redundant since the result is always ≤ 10). This value is used by the degeneration logic in Phase 18 and displayed on the character sheet. No migration needed — it is computed on the fly. If future mechanics introduce additional Humanity ceilings, they are `Math.Min`-composed at that point.
- **Covenant Status override turns a hard gate into a soft gate for covenant Disciplines only.** When `AcquisitionAcknowledgedByST = true` is supplied for a covenant-gated Discipline (Crúac, Theban, Coils), the service bypasses the Status check. This models the "stolen secrets" path in `DisciplinesRules.txt`. The override is **not** available for bloodline restrictions (always hard) or Theban Humanity floor (always hard). This distinction is documented in `rules-interpretations.md`.
- **Soft gate acknowledgments are audited.** Any purchase where `AcquisitionAcknowledgedByST = true` appends a structured suffix to the `XpLedgerEntry.Notes` string. Canonical format (recorded in `rules-interpretations.md`): existing notes text + `" | gate-override stUserId={userId} {timestamp:O}"`. Example: `"Purchased Discipline (Id=3, rating=1, in-clan=False) | gate-override stUserId=abc123 2026-03-24T21:00:00Z"`. The space-pipe-space separator keeps the note human-readable alongside existing text. No new entity or migration needed — the ledger already exists.
- **Necromancy "cultural connection" (option b) is a soft gate.** The app cannot verify mortal-life background. `Discipline.IsNecromancy` (bool flag) gates a dedicated soft-gate path: if the character is not Mekhet-clan and has no Necromancy-bloodline, `AcquisitionAcknowledgedByST = true` is required. The ST confirmation modal quotes all three eligible conditions from the rule text.

### Tasks

**Data model & migration**
- [ ] **Add acquisition metadata to `Discipline` entity** — `CanLearnIndependently` (bool), `RequiresMentorBloodToLearn` (bool), `IsCovenantDiscipline` (bool), `CovenantId` (int?, FK), `IsBloodlineDiscipline` (bool), `BloodlineId` (int?, FK). Migration: `Phase20DisciplineAcquisitionMetadata`.
- [ ] **Add `PoolDefinitionJson` to `DisciplinePower`** — nullable string, same serialization contract as `DevotionDefinition.PoolDefinitionJson`. Migration included in the same batch.
- [ ] **Extend `Disciplines.json` schema** — add acquisition fields to each discipline entry. Populate all 12 core disciplines and the bloodline disciplines (Dead Signal, Perfidy, etc.) already present.
- [ ] **Populate `PoolDefinitionJson` for all `DisciplinePower` rows** — fill roll data from `DisciplinesRules.txt` and core book; entries that are "Not detailed in source text" in the JSON remain null (display-only until content pass).

**Seed pipeline**
- [ ] **`DisciplineJsonImporter`** — new class in `RequiemNexus.Data` following the same `JsonSerializerOptions` / file-read pattern as `CovenantJsonImporter` (or equivalent). Called from `DbInitializer.EnsureDisciplinesAsync`. Maps JSON fields (including new acquisition fields) to `Discipline` and `DisciplinePower` entities. Idempotent (upsert by name).
- [ ] **Retire `DisciplineSeedData.cs`** — delete after importer is verified in integration tests. Record the switch in `rules-interpretations.md`.
- [ ] **Fix power names for Celerity, Resilience, Vigor** — replace generic `"Celerity 1"` names with official rulebook names (e.g., "Quickness", "Stutter-Step", etc.) in `Disciplines.json`. These become the authoritative names via the importer.

**Acquisition rule enforcement**
- [ ] **`DisciplineAcquisitionRequest` DTO** — `DisciplineId`, `TargetRating`, `AcquisitionAcknowledgedByST` (bool). This replaces bare `(disciplineId, rating)` parameters on the service.
- [ ] **Hard gate: Bloodline restriction** — `CharacterDisciplineService`: if `Discipline.IsBloodlineDiscipline`, character must have an active `CharacterBloodline` with matching `BloodlineId`. Return `Result.Failure` if not.
- [ ] **Hard gate (overridable): Covenant Status for covenant Disciplines** — if `Discipline.IsCovenantDiscipline`, character must have an active (non-pending) `CovenantMembership` with matching `CovenantId`. Return `Result.Failure` if not and `AcquisitionAcknowledgedByST = false`. When `AcquisitionAcknowledgedByST = true`, bypass the check and record the ST override in the ledger note (see architectural decisions above).
- [ ] **Hard gate: Theban Sorcery Humanity floor** — if discipline is Theban Sorcery and `TargetRating > character.Humanity`, return `Result.Failure` with message quoting the rule.
- [ ] **Soft gate: teacher + Vitae (`RequiresMentorBloodToLearn`)** — if `true` and discipline is out-of-clan, require `AcquisitionAcknowledgedByST = true`; surface a confirmation modal in the UI. Not a hard code block — Storyteller confirms the narrative condition was met.
- [ ] **Crúac breaking point** — on first purchase of Crúac by a character with Humanity ≥ 4, raise `DegenerationCheckRequired(Reason = CrúacPurchase)` (the shared event defined above). Before Phase 18 ships its roll UI, this creates a Glimpse notification only; the full roll is wired when Phase 18 is complete.
- [ ] **Necromancy gate** — add `IsNecromancy` bool to `Discipline` entity (migration included in `Phase20DisciplineAcquisitionMetadata`). In `CharacterDisciplineService`: if `IsNecromancy` and character is not Mekhet-clan and has no Necromancy bloodline, require `AcquisitionAcknowledgedByST = true`. Confirmation modal quotes all three eligible conditions from `DisciplinesRules.txt`. Audit logged to ledger.
- [ ] **Soft gate audit** — for any purchase where `AcquisitionAcknowledgedByST = true`, append `" | gate-override stUserId={userId} {timestamp:O}"` to the `XpLedgerEntry.Notes` string (canonical format per architectural decisions above). Record the format in `rules-interpretations.md`. No migration — reuses existing ledger.
- [ ] **Crúac Humanity cap** — `HumanityService.GetEffectiveMaxHumanity(character)` returns `10 − CrúacRating` when character has Crúac. Character sheet displays effective max. Phase 18 degeneration clamps loss to this ceiling.

**Character creation**
- [ ] **In-clan minimum during character creation** — `CharacterCreationService` (or creation wizard validation step): count in-clan dots allocated; if fewer than 2 of the 3 starting dots are in-clan, return a `Result.Failure` before saving. Display as an inline validation error in the creation UI.
- [ ] **Third-dot covenant gate** — if the third creation dot targets Crúac / Theban / Coils and the character does not have Covenant Status Merit (or ST acknowledges the "stolen secrets" path), surface an ST confirmation prompt before saving.

**UI**
- [ ] **Acquisition gate feedback** — Advancement page: when a Discipline is blocked by a hard gate, show a descriptive tooltip ("Requires active Lancea et Sanctum membership", "Humanity must be ≥ dot rating for Theban Sorcery"). Soft gates show a Storyteller-confirmation modal with the rule quoted verbatim.
- [ ] **Crúac Humanity cap badge** — Character sheet Humanity section: when `CrúacRating > 0`, show "Max Humanity: X (capped by Crúac •••)" beside the dots.
- [ ] **Power pool display** — Character sheet Disciplines section: when `DisciplinePower.PoolDefinitionJson` is populated, show the resolved pool formula next to each power (same pattern as Devotion display).

**Rules Interpretation Log**
- [ ] Record all interpretation decisions in `docs/rules-interpretations.md`: soft vs. hard gate choices, Crúac breaking-point trigger threshold (Humanity 4+), Theban floor formula, the `DisciplineSeedData.cs` → JSON migration rationale.

---

## 🚫 Explicit Non-Goals (this plan)

- **Battle maps / grid combat** — out of scope per core non-goals.
- **Chases and mass combat** — the chase and mass-combat subsystems in VtR 2e are separate mechanical frames. They are out of scope for this plan; tables that use them manage them manually.
- **Merged pools across multiple characters** — coordinated actions, teamwork rules, and merged-pool mechanics are not automated; the Storyteller calls the roll manually via the dice modal.
- **Automated Storyteller narrative judgment** — degeneration and frenzy *triggers* are automated; *narrative outcomes* remain ST discretion.
- **Full supplement catalog** — Phase 19 targets the VtR 2e core book only. Supplements (Bloodlines: The Hidden, etc.) are future content passes.
- **Temporary timed Coils from rites** — deferred from Phase 9.6; requires a time-based modifier system not yet designed.
- **New UI packages / component libraries** — Phases 15–18 add ST banners and activation modals. All new UI must reuse existing Glimpse patterns, the `DiceRollerModal`, and the Phase 13 ARIA announcer. No new front-end dependencies are introduced by this plan.

---

> _The Danse Macabre has rules. So does the engine._
> _Close the gaps, and the chronicle runs itself._
