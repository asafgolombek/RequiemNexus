# Phase 18 — The Wider Web: Edge Systems & Content

**Status: 🔄 In progress.** Tracks A–C are implemented in code. Track D (content), D8 (activation cost UI), tests, and final ✅ on `mission.md` / `claude.md` remain. **Task checkboxes:** [`mission.md`](./mission.md) Phase 18 section. **Detail, file paths, and test names:** this document.

**Objective:** Close the remaining mechanical gaps from the V:tR 2e playability audit and fill the core-book content catalog. Four independent tracks that can proceed in any order.

---

## Dependency Context

Phase 18 is fully independent — it has no blocking dependencies on any other phase. It reuses existing infrastructure without adding new architectural concepts.

> Note: **D8** (cost-choice UI) touches the discipline activation modal from Phase 16b. There is no new phase dependency, but reviewers should be aware of that coupling.

```
Phase 18 reuses:
  ├── PredatoryAuraService (Phase 12)       ← Track A passive hook
  ├── BloodSympathyRules + ITraitResolver   ← Track B pool calculation
  ├── SocialManeuveringEngine (Phase 10)    ← Track C interception logic
  └── SeedSource/ + DbInitializer           ← Track D content passes
  (D8 also touches DisciplineActivationService — Phase 16b)
```

---

## Track A — Passive Predatory Aura

### What Was Planned

When two Kindred enter the same scene, Predatory Aura contests automatically — the loser gains Shaken. "Same scene" was scoped to `CombatEncounter` to avoid a new scene/location entity.

### What Was Delivered ✅

| Component | File | Notes |
|-----------|------|-------|
| `IPredatoryAuraService.ResolvePassiveContestAsync` | `Application/Contracts/IPredatoryAuraService.cs` | `storytellerUserId`, `int? encounterId` parameters; skips duplicate pair per encounter |
| `PredatoryAuraService.ResolvePassiveContestAsync` | `Application/Services/PredatoryAuraService.cs` | Full impl: Vampire type guard, per-encounter dedup via `EncounterAuraContest`, `IsLashOut = false` flag, Shaken applied, dice feed published with `"Passive Predatory Aura"` prefix |
| `EncounterAuraContest` entity | `Data/Models/EncounterAuraContest.cs` | Unique index on `(EncounterId, VampireLowerId, VampireHigherId)` prevents double-triggering |
| Migration | `Phase18EncounterAuraAndManeuverInterceptor` | Creates `EncounterAuraContests` table |
| Auto-hook in `EncounterParticipantService` | `Application/Services/EncounterParticipantService.cs` | `TriggerPassivePredatoryAuraForNewParticipantIfNeededAsync`: called from all three add-to-encounter paths (`BulkAddOnlinePlayersAsync`, `AddCharacterToEncounterAsync`, `AddNpcToEncounterFromChronicleNpcAsync`); checks `CreatureType.Vampire`; skips non-Kindred |
| ST manual trigger | `Web/Components/Pages/Campaigns/StorytellerGlimpse.razor(.cs)` | `TriggerPassivePredatoryAuraAsync` invokes `ResolvePassiveContestAsync` without an `encounterId` (no dedup for manual triggers — by design) |
| Rules log | `docs/rules-interpretations.md` **Phase 18** section | "Same scene" = same `CombatEncounter`; dedup key; dice feed label convention |

**Dice feed / player visibility:** Passive aura outcomes are surfaced exclusively via the **session dice feed** (two roll entries per contest, prefixed `"Passive Predatory Aura"`). No separate toast or banner is emitted — this is intentional and matches the existing Lash Out pattern.

### Architectural Decisions Recorded

- **`PassiveAuraService` was not needed as a separate class.** `PredatoryAuraService.ResolvePassiveContestAsync` carries the `IsLashOut = false` distinction — adding a wrapper service would have been a YAGNI violation. `EncounterParticipantService` calls `IPredatoryAuraService` directly.
- **"Same scene" is conservatively defined as `CombatEncounter`.** A session/location model would require new scope not justified by the use case. Chronicle-wide ST triggers use `encounterId = null` and bypass dedup.
- **Dedup is write-on-first-contest, read-on-subsequent.** The `EncounterAuraContest` row is written after the first resolved contest; all subsequent calls for the same pair in the same encounter return `Success(null)` immediately.
- **Manual ST triggers have no dedup.** When the ST triggers a passive contest from the Glimpse without an `encounterId`, there is no `EncounterAuraContest` record written and the same pair can be contested multiple times. This is intentional — the ST is making a deliberate narrative decision, not wiring scene detection. If this proves abusive in play, a per-session dedup key can be added without schema changes by using the Redis session key.

### Remaining Work

- [ ] **Tests (new file: `EncounterParticipantServiceTests`)** — no test class exists for this service. Create one with a mock `IPredatoryAuraService` and cover all three add-to-encounter paths:
  - `BulkAddOnlinePlayers_VampirePair_TriggersPassiveAura` — adding two vampires calls `ResolvePassiveContestAsync` once for the pair.
  - `AddCharacterToEncounter_VampireJoinsVampire_TriggersPassiveAura` — single add path.
  - `AddCharacterToEncounter_MortalJoinsVampire_SkipsPassiveAura` — non-Kindred does not trigger.
  - `BulkAddOnlinePlayers_SamePairTwice_CallsOncePerNewArrivant` — duplicate dedup behavior (second call returns null, which the service ignores).

---

## Track B — Blood Sympathy

### What Was Planned

A "Sense Blood Kin" button on the character sheet Lineage section that rolls `Wits + Empathy + BloodSympathyRating` and posts the result to the session dice feed.

### What Was Delivered ✅

| Component | File | Notes |
|-----------|------|-------|
| `IBloodSympathyRollService` | `Application/Contracts/IBloodSympathyRollService.cs` | `RollBloodSympathyAsync(characterId, targetCharacterId, userId)` |
| `BloodSympathyRollService` | `Application/Services/BloodSympathyRollService.cs` | Pool: `Wits + Empathy` via `ITraitResolver`; dice count `+= BloodSympathyRating`; lineage degree checked via BFS over `Character.SireCharacterId` within chronicle; range validated against `BloodSympathyRules.EffectiveRange`; dice feed published with label `"Blood Sympathy — Wits + Empathy + rating (N dice) vs {name}"` |
| UI | `Web/Components/Pages/CharacterSheet/LineageSection.razor` | Character picker (chronicle roster); "Roll Blood Sympathy" button; `aria-live` toast on result; button disabled when `BloodPotency < 2` |
| DI registration | `Web/Extensions/ApplicationServiceExtensions.cs` | Scoped |
| Rules log | `docs/rules-interpretations.md` **Phase 18** section | Dice feed label convention |

### Architectural Decisions Recorded

- **Pool is `Wits + Empathy + rating`, not a flat rating roll.** Book text references both; the composite pool is more consistent with how other sensing rolls work in V:tR 2e.
- **Lineage BFS stays inside the chronicle roster.** `SireCharacterId` links that point outside the chronicle (imported characters) are treated as dead edges — the app only reasons about PCs it manages.
- **`BloodSympathyRating` comes from `BloodSympathyRules.ComputeRating(BloodPotency)`.** No separate DB column; the rating is derived at roll time.

### Remaining Work

- [ ] **Regression test** — add to `Domain.Tests` (or `Application.Tests` if it requires DB): `RollBloodSympathy_TargetBeyondRange_ReturnsFailure` verifies that a character out of lineage range gets `Result.Failure` rather than a 0-die roll. This guards the range-guard branch from accidental deletion during refactors.

---

## Track C — Social Maneuvering Interception

### What Was Planned

A third party (`ManeuverInterceptor`) can oppose an active social maneuver. Their opposition successes (`Manipulation + Persuasion` contest) reduce the initiator's effective door reductions before they are applied.

### What Was Delivered ✅

| Component | File | Notes |
|-----------|------|-------|
| `ManeuverInterceptor` entity | `Data/Models/ManeuverInterceptor.cs` | `SocialManeuverId`, `InterceptorCharacterId`, `IsActive`, `Successes`; unique per maneuver + character |
| `ManeuverInterceptorConfiguration` | `Data/EntityConfigurations/ManeuverInterceptorConfiguration.cs` | FK to `SocialManeuvers` (Cascade) and `Characters` (Restrict) |
| Migration | `Phase18EncounterAuraAndManeuverInterceptor` | Creates `ManeuverInterceptors` table with unique index |
| `ISocialManeuveringService.AddInterceptorAsync` | `Application/Contracts/ISocialManeuveringService.cs` | ST-only; unique per maneuver + character; maneuver must be Active |
| `ISocialManeuveringService.RecordInterceptorRollAsync` | `Application/Contracts/ISocialManeuveringService.cs` | ST-only; successes capped at `Manipulation + Persuasion` on interceptor's sheet |
| Both impls | `Application/Services/SocialManeuveringService.cs` | Full Masquerade sequence; structured logging |
| `SocialManeuveringEngine` interception | `Domain/Services/SocialManeuveringEngine.cs` | `ApplyInterceptorReductionToSuccesses(int rollSuccesses, IEnumerable<ManeuverInterceptor> interceptors)` — subtracts total active interceptor successes, floors at 0 |
| ST UI | `Web/Components/Pages/Campaigns/GlimpseSocialManeuvers.razor` | Interceptors section per maneuver: list of active interceptors with recorded successes; character picker; "Add interceptor" button; "Record opposition" button opens dice modal |
| Rules log | `docs/rules-interpretations.md` **Phase 18** section | Subtraction order, cap formula, tie/zero-adjusted-success behaviour |

### Architectural Decisions Recorded

- **Interception reduces effective successes, not Doors directly.** `ApplyInterceptorReductionToSuccesses` is called by the roll coordinator before Door math. This preserves the single place where Doors are calculated (`SocialManeuveringEngine`) and avoids conditional branching in the door-reduction path.
- **Recorded opposition is ST-entered, not server-rolled.** The interceptor's player would roll their dice IRL or via the Dice Nexus modal; the ST enters the result. This mirrors the existing social maneuver roll flow and avoids a new contested-roll endpoint.
- **Cap is `Manipulation + Persuasion` on the interceptor's sheet.** This is the V:tR 2e social contest pool — using the interceptor's own pool as a ceiling prevents inflated ST-entered values while keeping the cap meaningful.
- **Zero adjusted successes = standard failure.** No Doors open. This is the same branch as a roll with no successes in the base engine.

### Remaining Work

- [ ] **Tests** — `SocialManeuveringServiceTests` currently has 13 methods; none cover `AddInterceptorAsync` or `RecordInterceptorRollAsync`. Add:
  - `AddInterceptor_Success_ReturnsInterceptorRow` — ST adds an interceptor to an Active maneuver.
  - `AddInterceptor_NotStoryteller_Throws` — non-ST caller rejected (Masquerade guard).
  - `AddInterceptor_WrongChronicle_Throws` — interceptor character not in the maneuver's campaign.
  - `AddInterceptor_DuplicateCharacter_Throws` — second add of same character fails.
  - `AddInterceptor_ClosedManeuver_Throws` — interceptor cannot be added to a non-Active maneuver.
  - `RecordInterceptorRoll_CapsAtPoolMax` — recorded successes clamped to `Manipulation + Persuasion`.
  - `RecordInterceptorRoll_NotStoryteller_Throws` — auth guard.
- [ ] **Domain unit test** — add to `SocialManeuveringEngineTests`:
  - `Engine_InterceptorReducesEffectiveSuccesses` — verifies `ApplyInterceptorReductionToSuccesses` subtracts correctly and floors at 0.
  - `Engine_InterceptorAtZero_NoDoorsFromRoll` — confirms zero-adjusted-successes takes the failure branch.

---

## Track D — Content Passes (Data-Only)

Content passes are JSON-seed additions to `SeedSource/` with corresponding `DbInitializer` extension calls. No business logic changes are required. Each file can be a separate PR.

### Seed File Inventory

| File | Entries | Catalog scope |
|------|---------|---------------|
| `rites.json` | 45 | Crúac rites — V:tR 2e core book |
| `rituals.json` | 28 | Theban Sorcery Miracles — V:tR 2e core book |
| `coils_info.json` | 42 | Ordo Dracul — see D3 note on entry count |
| `necromancyRites.json` | 8 | Necromancy rites — Phase 9.6 sample; expansion needed |
| `devotions.json` | 63 | Clan/Covenant Devotions |
| `loresheetMerits.json` | 12 | Loresheet Merits |

### Non-Goals for Phase 18 Content Passes

Per `docs/mission.md` Non-Goals:
- Supplements beyond the **V:tR 2e core book** are out of scope.
- Temporary ritual-granted Coils/Scales (timed `PassiveModifier`) — deferred.
- Exotic or homebrew blood sorcery traditions.

### Remaining Work

**D1 — Theban Sorcery full catalog**
- [ ] Audit `rituals.json` against V:tR 2e core book Theban Sorcery chapter. Add any missing Miracles.
- File: `src/RequiemNexus.Data/SeedSource/rituals.json`
- No migration required (content update to existing `SorceryRites` rows via upsert-by-name in `DbInitializer`).

**D2 — Crúac full catalog**
- [ ] Audit `rites.json` against V:tR 2e core book Crúac chapter. Add any missing Rites.
- File: `src/RequiemNexus.Data/SeedSource/rites.json`

**D3 — Ordo Dracul Coil catalog**
- [ ] Audit `coils_info.json` against V:tR 2e core book Ordo Dracul chapter.
- **Acceptance criterion:** All 5 Mysteries are present, each with 5 Coils (25 Coils total). The current 42-entry count likely includes Scale entries and/or metadata rows alongside the Coil entries — verify the schema and confirm the 25-Coil completeness specifically, not just row count.
- File: `src/RequiemNexus.Data/SeedSource/coils_info.json`

**D4 — Necromancy catalog expansion**
- [ ] Add remaining Necromancy rites from the **V:tR 2e core book only** to `necromancyRites.json` (current: 8 entries). Scope is strictly V:tR 2e core — do not pull from V:tM or first-edition sources.
- File: `src/RequiemNexus.Data/SeedSource/necromancyRites.json`

**D5 — Devotion catalog expansion**
- [ ] Audit `devotions.json` (63 entries) against remaining clan/covenant Devotions in the V:tR 2e core book. Add any missing entries.
- Devotion schema: `name`, `description`, `prerequisiteDisciplines` (array with optional `orGroupId`), `xpCost`, `poolDefinitionJson` (nullable), `requiredBloodlineId` (nullable), `isPassive`.
- File: `src/RequiemNexus.Data/SeedSource/devotions.json`

**D6 — Loresheet Merits**
- [ ] Audit `loresheetMerits.json` (12 entries) against V:tR 2e core Loresheet chapter. Add any missing Loresheet Merit entries.
- Merit schema: `name`, `description`, `meritType`, `maxRating`, `meritCategory = "Loresheet"`.
- File: `src/RequiemNexus.Data/SeedSource/loresheetMerits.json`

**D7 — `PoolDefinitionJson` population for Discipline powers**
- [ ] For any `DisciplinePower` whose `PoolDefinitionJson` is currently `null` and which has a rollable pool described in the rulebook, populate the JSON using the same `PoolDefinition` format as Devotions.
  - Format reference: `{"traits":[{"traitType":"Attribute","attributeId":"Wits"},{"traitType":"Skill","skillId":"Empathy"}]}`
  - Powers that are always display-only (e.g., passive aura, always-on buffs) should remain `null`.
- File: `src/RequiemNexus.Data/SeedSource/Disciplines.json` — update `poolDefinitionJson` fields. Batch by discipline to keep reviews manageable (one PR per discipline line, not one giant JSON diff).
- No new migration required; `DisciplineJsonImporter` upserts by power name.

**D8 — `"1 Vitae or 1 Willpower"` cost — player-choice UI**
- Background: `ActivationCost.Parse` defaults to `Vitae` when the cost string is `"1 Vitae or 1 Willpower"`. Player-choice UI was deferred to Phase 18 (see `docs/rules-interpretations.md` line 89).
- [ ] Extend `ActivationCost` (Domain) with an explicit `IsPlayerChoice` flag (or `PlayerChoiceVitaeOrWillpower` enum value) set during parsing. **Do not detect `"or"` in the description string in the UI** — that is fragile and will misfire on unrelated text. The flag must come from the domain type.
- [ ] In the discipline activation confirmation modal, render a radio-button or segmented control when `ActivationCost.IsPlayerChoice == true`.
- [ ] Pass the choice as a new `CostChoice` parameter to `DisciplineActivationService.ActivatePowerAsync`.
- [ ] `DisciplineActivationService` respects the choice when both options are valid. When **neither** Vitae nor Willpower is available, return `Result.Failure("Insufficient resources: neither Vitae nor Willpower available to activate this power.")` — do not roll.
- This is a small UI polish task with one domain model change and no schema changes.

---

## Test Coverage Targets

| Area | Test project | Test class | Gap to fill |
|------|-------------|------------|-------------|
| Passive Predatory Aura auto-trigger | Application.Tests | `EncounterParticipantServiceTests` (new) | Three add-to-encounter paths; mortal-skip path; duplicate dedup |
| Blood Sympathy range guard | Domain.Tests or Application.Tests | `BloodSympathyRollServiceTests` or `BloodSympathyRulesTests` | Out-of-range target returns `Result.Failure` |
| Interceptor — add | Application.Tests | `SocialManeuveringServiceTests` | Success, duplicate, non-ST, wrong-chronicle, non-Active cases |
| Interceptor — record roll | Application.Tests | `SocialManeuveringServiceTests` | Cap enforcement, non-ST case |
| Interceptor — engine reduction | Domain.Tests | `SocialManeuveringEngineTests` | `ApplyInterceptorReductionToSuccesses`; zero-adjusted-successes branch |

---

## Exit Criteria

Phase 18 is **complete** when all of the following are true:

1. All four track task lists in `mission.md` are checked (`[x]`) and status updated to ✅.
2. `dotnet build` is green with zero warnings.
3. `dotnet format --verify-no-changes` passes.
4. `.\scripts\test-local.ps1` passes — all new test cases green.
5. `docs/rules-interpretations.md` records Phase 18 mechanics (verify after any rules change):
   - [x] Passive aura — full definition under Phase 12; summary + feed-only visibility under Phase 18.
   - [x] Manual ST trigger no-dedup — Phase 12 passive contest bullet.
   - [x] Interception pool cap and zero-adjusted-success behavior — Phase 18 section.
   - [x] Content-pass scope (V:tR 2e core only) — Phase 18 section.
   - [x] D8 player-choice flag design (not UI string detection) — Phase 16b + Phase 18 cross-reference.
6. Seed files audited and all V:tR 2e core book entries present for Crúac, Theban Sorcery, Coils (25 total), Necromancy, Devotions, and Loresheet Merits.
7. `claude.md` active-phase bullet set to Phase 18 **complete** (✅) when the phase closes (currently **in progress** 🔄).

---

## What Phase 18 Does NOT Do

- No new architectural layers or service abstractions.
- No new NuGet dependencies.
- No V:tM or supplement content — V:tR 2e core book only.
- No automation of Storyteller narrative judgment in interception (ST enters opposition successes manually).
- No scene/location entity for ambient passive aura — `CombatEncounter` scope only.
- No chase or mass-combat automation.

---

## Sequence Recommendation

Since all tracks are independent, prioritize by value:

1. **D7 + D8** (pool data + cost choice) — unblocks existing discipline activation UX polish. D8 requires a small domain-model change; do that first.
2. **Tests (A + C)** — close the test gap before Phase 20.
3. **D1–D6** (content audit) — pure data work; one PR per seed file.
4. **Doc sync** — when Phase 18 closes, set `mission.md` status to ✅ and `claude.md` active-phase bullet to complete.

> *"The blood remembers. The catalog must too."*
