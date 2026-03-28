# 🩸 Playability Gap Plan — Closing the V:tR 2e Mechanics Debt

> _The blood is the life… but only if the engine knows what blood does._

This document maps every identified gap between the current Requiem Nexus implementation and a fully playable **Vampire: The Requiem 2e** chronicle. Gaps are organized into six proposed phases (14–19), ordered by table-session impact. Each phase is self-contained and independently shippable.

---

## 📊 Gap Summary

| # | Gap | Severity | Proposed Phase |
|---|-----|----------|----------------|
| 1 | Combat attack / damage / armor pipeline | ✅ Closed | 14 |
| 2 | Wound penalties auto-applied to pools | ✅ Closed | 14 |
| 3 | Healing mechanics (Vitae spend to heal) | ✅ Closed | 14 |
| 4 | Damage-type conversion (B/L/A)  | ✅ Closed | 14 |
| 5 | Frenzy save rolls not automated | ✅ Closed | 15 |
| 6 | Rötschreck automation | ✅ Closed | 15 |
| 7 | Torpor state (entry, awakening, starvation) | ✅ Closed | 15 |
| 8 | Hunting / feeding roll automation | ✅ Closed | 16a |
| 9 | Vitae gain mechanics and resonance | ✅ Closed | 16a |
| 10 | Discipline power activation (cost + pool) | 🟠 High | 16b _(blocked on Phase 19 schema)_ |
| 11 | Humanity degeneration rolls | 🟡 Medium | 17 |
| 12 | Condition penalties fully wired to pools | 🟡 Medium | 17 |
| 13 | Remorse / Humanity anchor checks | 🟡 Medium | 17 |
| 14 | Passive Predatory Aura (first-meeting contest) | 🟢 Low | 18 |
| 15 | Blood Sympathy roll trigger | 🟢 Low | 18 |
| 16 | Social Maneuvering interception | 🟢 Low | 18 |
| 17 | Expanded ritual / rite catalog (content pass) | 🟢 Low | 18 |
| 18 | `Disciplines.json` not wired to seeder (`DisciplineSeedData.cs` is orphaned override) | 🔴 Critical | 19 |
| 19 | `Discipline` model missing acquisition metadata (teacher, blood, covenant, bloodline flags) | 🔴 Critical | 19 |
| 20 | `CharacterDisciplineService` enforces only XP — none of the rules from `DisciplinesRules.txt` | 🔴 Critical | 19 |
| 21 | Character creation 3-dot rule not enforced (2 must be in-clan) | 🔴 Critical | 19 |
| 22 | Crúac Humanity cap (`10 − Crúac dots`) not applied | 🔴 Critical | 19 |
| 23 | Theban Sorcery Humanity-floor prerequisite not enforced | 🔴 Critical | 19 |
| 24 | Celerity / Resilience / Vigor powers have generic placeholder names, not rulebook names | 🟡 Medium | 19 |
| 25 | `DisciplinePower` has no `PoolDefinitionJson` — blocks Phase 16b activation pipeline | 🟠 High | 19 |

---

## 📅 Phase 14: The Danse Macabre — Combat & Wounds

**The Objective:** Build the attack-to-damage pipeline so that initiative resolution leads to real mechanical outcomes, not just health box bookkeeping by hand.

### Architectural Decisions

- **Attack is just a dice roll with a structured result.** An `AttackResult` value object (Domain) captures successes, weapon damage dice, damage type, and a `DamageSource` tag. `DamageSource` is included from day one (Bashing, Lethal, Aggravated, Fire, Sunlight) to avoid retrofitting when Phase 15 adds fire/sunlight interactions. No new entity is needed — the existing `CombatEncounter` and `InitiativeEntry` anchor it.
- **Phase 14 MVP boundary: melee + generic weapon profile only.** Ranged/firearms, improvised weapons, and touch attacks are valid follow-on slices. The first shippable cut is: one attack roll → bashing or lethal damage → armor mitigation → health-box update → wound penalty in pool. This boundary is recorded in `rules-interpretations.md` when the slice ships.
- **Damage application flows through `CharacterHealthService`** (Application). The new path adds damage-type logic (bashing overflow → lethal, lethal overflow → aggravated) without touching unrelated character data.
- **Wound penalty uses the existing `ModifierTarget.WoundPenalty` path.** `WoundPenaltyResolver` (Domain, pure function) reads `HealthDamage` + track length and returns 0 / −1 / −2 / −3 (and `IsIncapacitated` when every box is marked). `ModifierService` injects a `PassiveModifier` with `Target = ModifierTarget.WoundPenalty` and `SourceType = WoundTrack`. `TraitResolver` applies that modifier when the resolved pool includes any **Physical** skill (same skill set as encumbrance’s Physical list via `TraitMetadata.PhysicalSkills`).
- **Defense is supplied by the caller.** `AttackService` subtracts the provided defense total from attack successes (typically the defender’s sheet `Defense` for a PC). Firearms vs. Defense and unaware-target rules are documented in `rules-interpretations.md`; dodge actions are out of scope for Phase 14.
- **Healing is a Vitae spend.** There is no standalone `VitaeService` yet; Phase 14 uses `CharacterHealthService.TryFastHealBashingWithVitaeAsync` with `HealingReason` + `VitaeHealingCosts` in Domain. Fast bashing heal is implemented; lethal/aggravated return a player-safe “not implemented” until a later slice.

### Tasks

- [x] **`DamageSource` enum** — Domain: Bashing, Lethal, Aggravated, Fire, Sunlight, Weapon. Added to `AttackResult` from day one; Phase 15 `FrenzyService` reuses `Fire` and `Sunlight` tags for Rötschreck triggers.
- [x] **`AttackResult` value object** — Domain: attack successes, defense applied, net hits, weapon damage successes, `DamageSource`, total damage instances. No EF dependency.
- [x] **`AttackService`** — Application: resolves attacker pool via `TraitResolver`, rolls attack + optional weapon damage from an equipped `CharacterAsset` (`weaponCharacterAssetId`); unarmed passes null (no weapon dice). Defense is a parameter. Masquerade: Storyteller + encounter membership + attacker access.
- [x] **Damage application (`CharacterHealthService`)** — B/L/A overflow via `HealthTrackMutator` (documented in `rules-interpretations.md`).
- [x] **`WoundPenaltyResolver`** — Domain: reads health track + max length; `IsIncapacitated` + penalty dice 0 / −1 / −2 / −3 aligned with vitals tooltips.
- [x] **`ModifierService` + `TraitResolver`** — Wound penalty injected in `ModifierService`; `TraitResolver` applies `WoundPenalty` when the pool includes a Physical skill.
- [x] **Healing (`HealingReason` + Vitae)** — `CharacterHealthService.TryFastHealBashingWithVitaeAsync` + `VitaeHealingCosts`; lethal/aggravated paths return structured failure until implemented.
- [x] **Combat UI — Attack Panel** — Storyteller Glimpse (active encounter) and Initiative Tracker: **Roll attack** / **Attack** opens `MeleeAttackResolveModal` (trait pool + equipped weapon or unarmed). **Single server resolution** via `IAttackService` (not `DiceRollerModal`, which would double-roll). **Apply damage** applies to a PC (`CharacterHealthService`) or NPC row (`INpcCombatService.ApplyNpcDamageBatchAsync`).
- [x] **Combat UI — Heal Panel** — `VitaeFastHealPanel` on the character sheet and Storyteller Glimpse vitals: fast bashing heal with live Vitae cost preview (`VitaeHealingCosts`).
- [x] **Untrained skill (−1 die)** — `TraitResolver` and NPC encounter trait-built pools: −1 die per distinct skill at 0 dots in the pool.
- [x] **Rules Interpretation Log** — Phase 14 section in `docs/rules-interpretations.md` (MVP scope, `DamageSource` mapping, defense caller-supplied, overflow edge case).

---

## 📅 Phase 15: The Beast Within — Frenzy & Torpor

**The Objective:** Give the Beast teeth. Frenzy and Torpor are core VtR 2e horror mechanics; without them every character is just a powerful human.

### Architectural Decisions

- **Frenzy is a contested save, not a toggle.** `FrenzyService` (Application) receives a `FrenzyTrigger` enum (`Hunger`, `Rage`, `Rotschreck`, `Starvation`) and executes a `Resolve + Blood Potency` pool via `DiceService`. Failure applies the `Frenzy` Tilt or `Rotschreck` Tilt. Willpower spend is optional (subtract 1 from pool cost, same pattern as existing Willpower spends).
- **Triggers are explicit, not automatic.** The app does not poll ambient conditions. Triggers are fired by: (a) the Storyteller via Glimpse, (b) the character's Vitae dropping to 0, or (c) a player confirming they are exposed to fire / sunlight. This matches the "no narrative automation" principle.
- **Torpor is a character state, not a separate entity.** A nullable `TorporSince` timestamp on `Character` is sufficient. A `TorporService` (Application) encapsulates entry, awakening, and starvation-hunger-escalation logic.
- **Hunger / starvation escalation during Torpor** is modeled as a new `TorporIntervalService : BackgroundService` in `RequiemNexus.Web/BackgroundServices/`, following the exact pattern of the existing `SessionTerminationService`. It runs on a configurable cadence (default: nightly `IHostedService` timer) and raises a Storyteller notification when a character's torpor interval has elapsed. An "Advance Time" action on the ST Glimpse panel calls the same `TorporService` interval logic on demand, covering in-session time jumps without changing the scheduler.

### Tasks

- [x] **`FrenzyTrigger` enum** — Domain: `Hunger` (Vitae 0 **in active play** — synchronous `VitaeDepletedEvent`; not torpor-interval hunger), `Rage` (provocation in combat), `Rotschreck` (fire or sunlight exposure), `Starvation` (torpor hunger escalation — background interval / Advance Time only; never the same code path as `VitaeDepletedEvent`). Pure value, no EF.
- [x] **`FrenzyService`** — Application: `RollFrenzySaveAsync(characterId, trigger, spendWillpower)` — resolves `Resolve + Blood Potency` via `TraitResolver`, applies `Frenzy` or `Rotschreck` Tilt on failure, records to dice history feed. Beast-already-active guard suppresses duplicate saves.
- [x] **Willpower spend path in `FrenzyService`** — subtract 1 die from the pool; spend 1 Willpower box via `WillpowerService`.
- [x] **Vitae-zero trigger** — `VitaeService.SpendVitaeAsync` raises `VitaeDepletedEvent` when Vitae reaches 0. `VitaeDepletedEventHandler` calls `FrenzyService.RollFrenzySaveAsync` (Hunger trigger). Idempotent: beast-active guard prevents duplicate tilt rows.
- [x] **`TorporSince` + `LastStarvationNotifiedAt` on `Character`** — Data: nullable datetime columns, migration `Phase15TorporState`.
- [x] **`TorporService`** — Application: `EnterTorporAsync`, `AwakenFromTorporAsync` (costs 1 Vitae via `VitaeService`, or narrative awakening flag), `CheckStarvationIntervalAsync` (logs ST warning at `TorporDurationTable` threshold).
- [x] **`TorporIntervalService`** — `Web/BackgroundServices/TorporIntervalService.cs` extending `BackgroundService`, following the `SessionTerminationService` pattern. Configurable timer via `Torpor:IntervalHours` (default 24 h). On each tick: queries `TorporSince != null` characters and calls `CheckStarvationIntervalAsync` per character.
- [x] **`DomainEventDispatcher` + `IDomainEventHandler<T>`** — in-process domain event infrastructure; `VitaeDepletedEventHandler` is the first handler.
- [x] **`VitaeService` + `WillpowerService`** — Masquerade-checked spend/gain for both resources; used by FrenzyService, TorporService, SorceryActivationService, CharacterHealthService.
- [x] **Torpor UI** — Character sheet badge and Storyteller Glimpse panel: enter/awaken buttons, "Torpor Since" display. `HealthDamageTrackBoxes` component for visual health track.
- [x] **Frenzy save UI — player** — Character sheet: "I am exposed to fire / sunlight" button triggers `Rotschreck` save; result posted to real-time dice feed.
- [x] **Frenzy save UI — ST** — Storyteller Glimpse: "Trigger Frenzy Save" per character with trigger-type picker (Hunger / Rage / Rotschreck).
- [x] **Rules Interpretation Log** — Torpor duration table (BP 1–10 → weeks to centuries), hunger escalation rate, Rötschreck pool choice, and the "one Vitae to awaken" interpretation.

---

## 📅 Phase 16a: The Hunting Ground — Feeding ✅

**The Objective:** Make feeding a first-class mechanical action with per-predator-type pools and a resonance outcome. Independent of Phase 19.

**Status:** ✅ **Complete** — see [`docs/PHASE_16A_THE_HUNTING_GROUND.md`](./PHASE_16A_THE_HUNTING_GROUND.md).

### Architectural Decisions

- **Hunting is a pool roll wired to Predator Type.** `HuntingService` reads `Character.PredatorType`, loads `HuntingPoolDefinition`, resolves `PoolDefinitionJson` via `TraitResolver`, rolls with 10-again, maps successes to Vitae (`BaseVitaeGain` + per-success scaling via `VitaeService`).
- **Territory is optional.** `ExecuteHuntAsync(characterId, userId, territoryId?)` — when `territoryId` is set, `FeedingTerritory.Rating` adds bonus dice. **Campaign alignment is enforced:** `territory.CampaignId` must equal `character.CampaignId` (and character must be in a campaign when a territory is used).
- **Resonance is display-only in code.** `ResonanceOutcome` (incl. `None`) maps from success count via a static threshold table in `HuntingService` — no `ResonanceTable` JSON seed. UI shows label; no mechanical resonance effects in this phase.

### Tasks

- [x] **`HuntingPoolDefinition` seed table** — one row per `PredatorType`; idempotent seed in `DbInitializer`.
- [x] **`HuntingService`** — `ExecuteHuntAsync`; Masquerade via `RequireCharacterAccessAsync`; territory bonus + mismatch guards; pool floor 1; `PublishDiceRollAsync` for dice feed.
- [x] **`ResonanceOutcome` enum** — Domain; static mapping from successes in Application layer.
- [x] **`HuntingRecord` ledger** — append-only audit per hunt.
- [x] **Hunting UI** — `HuntPanel.razor` on character vitals; Phase 13 `aria-live` pattern.
- [x] **Rules Interpretation Log** — Phase 16a section in `docs/rules-interpretations.md`.

---

## 📅 Phase 16b: The Discipline Engine — Power Activation

**The Objective:** Give each seeded `DisciplinePower` an activation button with pool resolution and cost enforcement.

**Dependency:** Requires Phase 19 to have shipped `DisciplinePower.PoolDefinitionJson` (migration + importer). Phase 16b cannot start until Phase 19's data-model slice is merged.

### Architectural Decisions

- **Discipline activation is a wrapper around the existing `TraitResolver`.** `DisciplineActivationService` (Application) receives a `disciplinePowerId` and `characterId`, reads the seeded `DisciplinePower.PoolDefinitionJson` and `Cost`, calls `TraitResolver`, deducts cost, and posts the result to the dice feed. Everything else is already built in Phase 19.
- **Cost deduction is atomic.** Vitae and Willpower spends for Discipline activation go through the same `VitaeService` / `WillpowerService` path as all other spends — no separate code path.
- **Powers with null `PoolDefinitionJson` remain display-only** — the "Activate" button is suppressed for those rows until a content pass populates their pool.

### Tasks

- [ ] **`DisciplineActivationService`** — Application: `ActivatePowerAsync(characterId, disciplinePowerId, optional contested target)` — reads `DisciplinePower.PoolDefinitionJson`, resolves pool via `TraitResolver`, deducts `ActivationCost`, posts result to dice feed. Masquerade: ownership check.
- [ ] **`ActivationCost` value object** — Domain: parse `DisciplinePower.Cost` string (`"1 Vitae"`, `"1 Willpower"`, `"—"`) into a typed cost (same pattern as `SorceryRiteDefinition.RequirementsJson`). `DisciplineActivationService` enforces before rolling.
- [ ] **Discipline activation UI** — Character sheet Disciplines section: each power row with a populated `PoolDefinitionJson` gets an "Activate" button. Opens cost-preview modal → confirm → resolves pool → result in dice feed. Powers with null pool remain display-only. Passive powers remain display-only regardless.
- [ ] **Rules Interpretation Log** — cost enforcement choices, any pool edge cases not covered by existing `TraitResolver` contract.

---

## 🔔 Shared Domain Event: `DegenerationCheckRequired`

Both Phase 17 and Phase 19 raise this event. It is defined once in the Domain layer and handled by a single Application-layer handler — do not create two separate event types.

```csharp
// Domain/Events/DegenerationCheckRequired.cs
public record DegenerationCheckRequired(
    int CharacterId,
    DegenerationReason Reason);   // enum: StainsThreshold, CrúacPurchase

public enum DegenerationReason { StainsThreshold, CrúacPurchase }
```

**Phase 19** raises it (with `Reason = CrúacPurchase`) before the Phase 17 roll UI exists — at that point the handler logs a Glimpse notification but cannot yet open the roll dialog. Once Phase 17 ships its UI, the same handler gains the "Roll Degeneration" button path. **Crúac purchase does not block** until Phase 17 exists; it creates an ST notification that can be dismissed.

---

## 📅 Phase 17: The Fog of Eternity — Humanity & Condition Wiring

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

## 📅 Phase 18: The Wider Web — Edge Systems & Content

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
Phase 14 (Combat)
    ├──► Phase 15 (Frenzy/Torpor)       ← VitaeDepletedEvent from Phase 14
    │         └──► Phase 17 (Humanity)  ← wound-penalty path + DegenerationCheckRequired UI
    └──► Phase 17 (Humanity)            ← WoundPenaltyResolver in ModifierService

Phase 16a (Hunting)   ← fully independent; no upstream dependencies
Phase 19  (Disciplines — model + seed)  ← independent of 14-17; can start now
    └──► Phase 16b (Discipline Activation)  ← needs PoolDefinitionJson from Phase 19

Phase 18 (Edge Systems) ← fully independent; content passes can start immediately
```

**Recommended parallel tracks:**
- Track A: Phases 14 → 15 → 17 (combat chain)
- Track B: Phase 19 → 16b (discipline chain, can start immediately)
- Track C: Phase 16a and Phase 18 (independent, any time)

---

## 📅 Phase 19: The Blood Lineage — Discipline Acquisition Rules & Seed Pipeline

**The Objective:** Make the Discipline system enforce the exact acquisition rules written in `docs/DisciplinesRules.txt`, promote `Disciplines.json` to the authoritative seed source, and add the `PoolDefinitionJson` field that Phase 16b needs for activation.

### Current State (what's broken)

| Component | Problem |
|-----------|---------|
| `Disciplines.json` | Exists in `SeedSource/` but is **not read by `DbInitializer`**. `DisciplineSeedData.cs` is the actual seed, making the JSON dead weight. |
| `Discipline` entity | No fields for `CanLearnIndependently`, `RequiresMentorBloodToLearn`, `IsCovenantDiscipline`, `CovenantId`, `IsBloodlineDiscipline`, `BloodlineId`. |
| `DisciplinePower` entity | No `PoolDefinitionJson` column — Phase 16b activation cannot resolve pools per-power. |
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
- **`DisciplinePower.PoolDefinitionJson` mirrors `DevotionDefinition.PoolDefinitionJson`** — same `PoolDefinition` serialization format, same `TraitResolver` contract. Phase 16b's `DisciplineActivationService` reads this column directly.
- **Crúac Humanity cap is a derived modifier, not a stored stat.** `HumanityService.GetEffectiveMaxHumanity(character)` returns `10 − character.CrúacRating` (the book formula directly; `Math.Min(10, …)` is redundant since the result is always ≤ 10). This value is used by the degeneration logic in Phase 17 and displayed on the character sheet. No migration needed — it is computed on the fly. If future mechanics introduce additional Humanity ceilings, they are `Math.Min`-composed at that point.
- **Covenant Status override turns a hard gate into a soft gate for covenant Disciplines only.** When `AcquisitionAcknowledgedByST = true` is supplied for a covenant-gated Discipline (Crúac, Theban, Coils), the service bypasses the Status check. This models the "stolen secrets" path in `DisciplinesRules.txt`. The override is **not** available for bloodline restrictions (always hard) or Theban Humanity floor (always hard). This distinction is documented in `rules-interpretations.md`.
- **Soft gate acknowledgments are audited.** Any purchase where `AcquisitionAcknowledgedByST = true` appends a structured suffix to the `XpLedgerEntry.Notes` string. Canonical format (recorded in `rules-interpretations.md`): existing notes text + `" | gate-override stUserId={userId} {timestamp:O}"`. Example: `"Purchased Discipline (Id=3, rating=1, in-clan=False) | gate-override stUserId=abc123 2026-03-24T21:00:00Z"`. The space-pipe-space separator keeps the note human-readable alongside existing text. No new entity or migration needed — the ledger already exists.
- **Necromancy "cultural connection" (option b) is a soft gate.** The app cannot verify mortal-life background. `Discipline.IsNecromancy` (bool flag) gates a dedicated soft-gate path: if the character is not Mekhet-clan and has no Necromancy-bloodline, `AcquisitionAcknowledgedByST = true` is required. The ST confirmation modal quotes all three eligible conditions from the rule text.

### Tasks

**Data model & migration**
- [ ] **Add acquisition metadata to `Discipline` entity** — `CanLearnIndependently` (bool), `RequiresMentorBloodToLearn` (bool), `IsCovenantDiscipline` (bool), `CovenantId` (int?, FK), `IsBloodlineDiscipline` (bool), `BloodlineId` (int?, FK). Migration: `Phase19DisciplineAcquisitionMetadata`.
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
- [ ] **Crúac breaking point** — on first purchase of Crúac by a character with Humanity ≥ 4, raise `DegenerationCheckRequired(Reason = CrúacPurchase)` (the shared event defined above). Before Phase 17 ships its roll UI, this creates a Glimpse notification only; the full roll is wired when Phase 17 is complete.
- [ ] **Necromancy gate** — add `IsNecromancy` bool to `Discipline` entity (migration included in `Phase19DisciplineAcquisitionMetadata`). In `CharacterDisciplineService`: if `IsNecromancy` and character is not Mekhet-clan and has no Necromancy bloodline, require `AcquisitionAcknowledgedByST = true`. Confirmation modal quotes all three eligible conditions from `DisciplinesRules.txt`. Audit logged to ledger.
- [ ] **Soft gate audit** — for any purchase where `AcquisitionAcknowledgedByST = true`, append `" | gate-override stUserId={userId} {timestamp:O}"` to the `XpLedgerEntry.Notes` string (canonical format per architectural decisions above). Record the format in `rules-interpretations.md`. No migration — reuses existing ledger.
- [ ] **Crúac Humanity cap** — `HumanityService.GetEffectiveMaxHumanity(character)` returns `10 − CrúacRating` when character has Crúac. Character sheet displays effective max. Phase 17 degeneration clamps loss to this ceiling.

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
- **Full supplement catalog** — Phase 18 targets the VtR 2e core book only. Supplements (Bloodlines: The Hidden, etc.) are future content passes.
- **Temporary timed Coils from rites** — deferred from Phase 9.6; requires a time-based modifier system not yet designed.
- **New UI packages / component libraries** — Phases 14–17 add ST banners and activation modals. All new UI must reuse existing Glimpse patterns, the `DiceRollerModal`, and the Phase 13 ARIA announcer. No new front-end dependencies are introduced by this plan.

---

> _The Danse Macabre has rules. So does the engine._
> _Close the gaps, and the chronicle runs itself._
