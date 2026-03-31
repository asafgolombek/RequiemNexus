# Phase 18 — The Wider Web: Edge Systems & Content

**Status: ✅ Complete.** Tracks A–C, Track D (D1–D8), tests, and doc sync are delivered. **Roadmap:** [`mission.md`](./mission.md) Phase 18 section. **Detail, file paths, and test names:** this document.

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

- [x] **Tests** — `EncounterParticipantServiceTests` with mock `IPredatoryAuraService`: bulk vampire pair, single add, mortal skip, repeated bulk does not re-invoke aura.

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

- [x] **Regression test** — `BloodSympathyRollServiceTests.RollBloodSympathy_TargetBeyondRange_ReturnsFailure` (Application.Tests, in-memory lineage chain).

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
| `SocialManeuveringEngine` interception | `Domain/Services/SocialManeuveringEngine.cs` | `ApplyInterceptorReductionToSuccesses(int initiatorSuccesses, int totalInterceptorSuccesses)` — subtracts interceptor total, floors at 0 |
| ST UI | `Web/Components/Pages/Campaigns/GlimpseSocialManeuvers.razor` | Interceptors section per maneuver: list of active interceptors with recorded successes; character picker; "Add interceptor" button; "Record opposition" button opens dice modal |
| Rules log | `docs/rules-interpretations.md` **Phase 18** section | Subtraction order, cap formula, tie/zero-adjusted-success behaviour |

### Architectural Decisions Recorded

- **Interception reduces effective successes, not Doors directly.** `ApplyInterceptorReductionToSuccesses` is called by the roll coordinator before Door math. This preserves the single place where Doors are calculated (`SocialManeuveringEngine`) and avoids conditional branching in the door-reduction path.
- **Recorded opposition is ST-entered, not server-rolled.** The interceptor's player would roll their dice IRL or via the Dice Nexus modal; the ST enters the result. This mirrors the existing social maneuver roll flow and avoids a new contested-roll endpoint.
- **Cap is `Manipulation + Persuasion` on the interceptor's sheet.** This is the V:tR 2e social contest pool — using the interceptor's own pool as a ceiling prevents inflated ST-entered values while keeping the cap meaningful.
- **Zero adjusted successes = standard failure.** No Doors open. This is the same branch as a roll with no successes in the base engine.

### Remaining Work

- [x] **Application tests** — `SocialManeuveringServiceTests`: add interceptor success, non-ST, wrong campaign, duplicate, closed maneuver; record roll at pool max, above max throws (service rejects over `Manipulation + Persuasion`), non-ST record throws.
- [x] **Domain** — `ApplyInterceptorReductionToSuccesses` covered by existing theory tests; `GetDoorsOpenedByOpenDoorRoll_AfterInterceptorReductionToZero_OpensNoDoors` links zero adjusted successes to no Doors.

---

## Track D — Content Passes (Data-Only)

Content passes are JSON-seed additions to `SeedSource/` with corresponding `DbInitializer` extension calls. No business logic changes are required. Each file can be a separate PR.

### Seed File Inventory

| File | Entries | Catalog scope |
|------|---------|---------------|
| `rites.json` | 45 | Crúac rites — V:tR 2e core book |
| `rituals.json` | 29 | Theban Sorcery Miracles — V:tR 2e core book (+ aggravated Blandishment row) |
| `coils_info.json` | 42 | Ordo Dracul — see D3 note on entry count |
| `necromancyRites.json` | 8 | Necromancy rites — matches V:tR 2e core count (eight rites) |
| `devotions.json` | 63 | Clan/Covenant Devotions |
| `loresheetMerits.json` | 12 | Loresheet Merits |

### Non-Goals for Phase 18 Content Passes

Per `docs/mission.md` Non-Goals:
- Supplements beyond the **V:tR 2e core book** are out of scope.
- Temporary ritual-granted Coils/Scales (timed `PassiveModifier`) — deferred.
- Exotic or homebrew blood sorcery traditions.

### Remaining Work

**D1 — Theban Sorcery full catalog** ✅
- [x] Cross-check vs. V:tR 2e–tagged miracles (secondary ref.: Codex of Darkness Theban table): **all** such entries are present in `rituals.json`. **Blandishment of Sin** at **1** dot (bashing → lethal); **`Blandishment of Sin (Aggravated)`** at **4** dots (lethal → aggravated; distinct name for name-keyed upsert). The file remains a **superset** (e.g. miracles tagged to supplements in third-party tables) — removing supplement-only rows was out of scope.
- File: `src/RequiemNexus.Data/SeedSource/rituals.json`

**D2 — Crúac full catalog** ✅
- [x] Cross-check vs. V:tR 2e–tagged Crúac rites (Codex): **Pangs of Proserpina**, **Rigor Mortis**, **Cheval**, **Hydra's Vitae**, **Deflection of Wooden Doom**, **Touch of the Morrigan**, **Blood Blight**, **Blood Price**, **Feeding the Crone**, **Willful Vitae** are all present in `rites.json` (naming matches allow minor article/casing variants). Seed remains a **superset** with additional non–core-only rites for chronicle use.
- File: `src/RequiemNexus.Data/SeedSource/rites.json`

**D3 — Ordo Dracul Coil catalog** ✅ *(audit + data fix)*
- [x] **Five core Mysteries × 5 Coils = 25** are present: Ascendant, Wyrm, Voivode, Zirnitra, Quintessence. **Data fix:** `Coil of the Quintessence` had `mystery` wrongly set to `Mystery of the Voivode`; now **`Mystery of the Quintessence`**. **Into the Fold** placeholder text replaced with a V:tR 2e–accurate summary (blood sympathy via the Vinculum).
- **Note:** The seed also includes **Mystery of Ziva** and **Mystery of the Vigilant** (5 Coils each) — treat as supplement-friendly extras beyond the core 25; not removed in this pass.
- File: `src/RequiemNexus.Data/SeedSource/coils_info.json`

**D4 — Necromancy catalog expansion** ✅
- [x] Core **V:tR 2e** Necromancy chapter lists **eight** rites (two at •, two at ••, two at •••, one at ••••, one at •••••). `necromancyRites.json` already matches that count and dot spread; no further core-only additions identified.
- File: `src/RequiemNexus.Data/SeedSource/necromancyRites.json`

**D5 — Devotion catalog expansion** ✅
- [x] `devotions.json` includes **29** entries with `source` citing **VTR 2e** core page numbers; remaining entries cite other licensed books (`GTTN`, `TY`, etc.) as **traceable catalog extensions**. No missing core-tagged rows identified in spot-check vs. Codex Devotions (2nd Edition) VTR column. Schema: `name`, `description`, `prerequisiteDisciplines`, `xpCost`, `poolDefinitionJson`, `requiredBloodlineId`, `isPassive`.
- File: `src/RequiemNexus.Data/SeedSource/devotions.json`

**D6 — Loresheet Merits** ✅
- [x] Twelve **Loresheet:** chronicle merits in `loresheetMerits.json` cover core-style domain/chronicle hooks; `MeritSeedData` / `EnsureMissingMeritDefinitionsFromSeedFilesAsync` consume `rating` + `category` + `source book` fields. Further supplements remain future work per `mission.md` non-goals.
- File: `src/RequiemNexus.Data/SeedSource/loresheetMerits.json`

**D7 — `PoolDefinitionJson` population for Discipline powers** ✅ *(seed file scope)*
- [x] All powers in `Disciplines.json` that carry a non-empty `roll` now have `poolDefinitionJson` (same shape as existing Feral Whispers: `traits` with `type` / `attributeId` / `skillId` / `disciplineId: 0`; `DbInitializer.NormalizePoolDefinitionJson` rewrites `0` to the parent discipline id).
- Contested powers (e.g. vs Resolve + Blood Potency) encode the **actor** pool only; defender-side dice are not modeled in `TraitReference` today — the `roll` string remains the rulebook reference.
- Powers with empty `roll` (Celerity, Resilience, Vigor, most Protean, etc.) correctly remain without `poolDefinitionJson`.
- `Crúac` / `Theban Sorcery` stanzas in this file still have empty `powers` arrays — their rollable pools live on sorcery rite seeds, not here.
- No new migration required; discipline JSON refresh path upserts by power name.

**D8 — `"1 Vitae or 1 Willpower"` cost — player-choice UI** ✅
- **Delivered:** `ActivationCost.Parse` recognises `N Vitae or M Willpower` (regex in Domain), sets `IsPlayerChoiceVitaeOrWillpower` + `PlayerChoiceWillpowerAmount`. `DisciplineActivationResourceChoice` enum; `DisciplinePowerActivateModal` shows Vitae/Willpower radios driven by parsed cost (no substring heuristics in UI). `ActivatePowerAsync(..., DisciplineActivationResourceChoice? resourceChoice)` requires a choice when the cost is a player choice; throws `InvalidOperationException` if choice is missing or if **neither** resource can pay the respective amounts (no roll). See `docs/rules-interpretations.md` (Phase 16b).

---

## Test Coverage Targets

| Area | Test project | Test class | Status |
|------|-------------|------------|--------|
| Passive Predatory Aura auto-trigger | Application.Tests | `EncounterParticipantServiceTests` | ✅ |
| Blood Sympathy range guard | Application.Tests | `BloodSympathyRollServiceTests` | ✅ |
| Interceptor — add / record | Application.Tests | `SocialManeuveringServiceTests` | ✅ |
| Interceptor — engine + Doors | Domain.Tests | `SocialManeuveringEngineTests` | ✅ (incl. zero-adjusted → no Doors) |

---

## Exit Criteria

Phase 18 is **complete** when all of the following are true:

1. All track task lists in `mission.md` Phase 18 are checked (`[x]`) and status updated to ✅.
2. `dotnet build` is green with zero warnings.
3. `dotnet format --verify-no-changes` passes.
4. `.\scripts\test-local.ps1` passes — all new test cases green.
5. `docs/rules-interpretations.md` records Phase 18 mechanics (verify after any rules change):
   - [x] Passive aura — full definition under Phase 12; summary + feed-only visibility under Phase 18.
   - [x] Manual ST trigger no-dedup — Phase 12 passive contest bullet.
   - [x] Interception pool cap and zero-adjusted-success behavior — Phase 18 section.
   - [x] Content-pass scope (V:tR 2e core only) — Phase 18 section.
   - [x] D8 player-choice flag design (not UI string detection) — Phase 16b + Phase 18 cross-reference.
6. Seed files audited: V:tR 2e–indexed Crúac + Theban rows verified present; **25** core Coils across five Mysteries; **eight** Necromancy rites; devotions with **VTR 2e** sources present; loresheet seed set populated (see Track D sign-offs above). Catalog files may include additional supplement-tagged rows by design.
7. `claude.md` active-phase bullet set to Phase 18 **complete** (✅); next roadmap focus **Phase 20 — The Global Embrace** unless a maintenance phase is opened.

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

1. **D7** ✅ — `poolDefinitionJson` on all rollable clan/Necromancy (Death Sight) powers in `Disciplines.json`. **D8** (cost choice) is ✅.
2. **D1–D6** — ✅ closed (see Track D).
3. **Doc sync** — ✅ `mission.md` / `claude.md` / `AGENTS.md` updated for Phase 18 completion.

> *"The blood remembers. The catalog must too."*
