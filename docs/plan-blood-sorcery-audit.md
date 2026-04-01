# Blood Sorcery Audit — Crúac, Theban Sorcery & Kindred Necromancy

**Date:** 2026-04-01 (updated 2026-04-01 — implementation status)
**Source of truth:** *Vampire: The Requiem 2e* PDF (pages 150–165) + `docs/magic_types_and_rules.txt`
**Status:** **Delivered in codebase for P0, P1-2, P2, P5 (canonical ritual JSON in Data + unified importer + Ranking / Elder gates + Blandishment split), P6 (Ordo Dracul ritual type removed), historical P3-1–2 rating work, P3-4/5 reference JSON, and P4 Theban learn/eligible gate; deferred P1-1, P1-3, P1-4, remaining P4 verification.**  
**Companion review:** [plan-blood-sorcery-audit-review.md](./plan-blood-sorcery-audit-review.md) (open questions, backlog gaps, doc fixes)

---

## Implementation status (codebase)

| Audit block | Status | Notes |
|-------------|--------|--------|
| **P0-1** BOM + encoding | Done | `SeedDataLoader.TryLoadJson` uses UTF-8 with BOM detection; Crúac catalog (`cruac_rituales.json`) uses proper `Crúac` escapes. |
| **P0-2** Theban sacrament | Done | `DbInitializer` builds `PhysicalSacrament` in `RequirementsJson`; tests cover cast without acknowledgment. |
| **P1-2** `TargetSuccesses` | Done | Column + seed JSON + UI (sheet + learn modal); catalog sync in `DbInitializer`. |
| **P1-1** Extended actions | **Not done** | Requires session persistence + cost-timing decisions (see § P1-1). |
| **P1-3** Outcome Conditions | **Not done** | Depends on P1-1 roll pipeline. |
| **P1-4** Potency | **Not done** | Depends on P1-1; scope remains informational until decided. |
| **P2-1** Extra Vitae (Crúac) | Done | `BeginRiteActivationRequest.ExtraVitae`, `SorceryActivationService`, `RiteActivationPrepModal`. |
| **P2-2** Blood Sympathy | Done | `TargetCharacterId`, `KindredLineageDegree` + `BloodSympathyRules.RitualSympathyBonusThebanOrNecromancy`, chronicle roster picker. |
| **P2-3** Crúac Humanity cap | Done | `HumanityService` / `CharacterDisciplineService` clamp + degeneration hooks (Phase 19). |
| **P2-4** Necromancy torpor | Done | `TorporDurationTable` + `TorporService` effective BP. |
| **P2-5** Necromancy clan gate | Done | Seed cleanup + `IsTraditionAllowedForCharacter` + `ClearNecromancyRequiredClanGateAsync`. |
| **P3-1 / P3-2** Ratings | Done (historical) | Pre–P5, `rites.json` / `rituals.json` were corrected vs PDF; **P5** now owns authoritative Crúac/Theban rows in `cruac_rituales.json` / `Theban_Sorcery_rituals.json`. |
| **P3-3** Necromancy catalog | **Superseded by P5** | Shipped set = `SeedSource/kindred_necromancy_rituals.json` only; no parallel `necromancyRites.json`. |
| **P5** Canonical catalogs in Data | Done | Three unified-schema files under `src/RequiemNexus.Data/SeedSource/`; `SorceryRiteSeedData` + `RequiresElder`; old SeedSource trio removed. |
| **P6** Ordo Dracul rituals | Done | `OrdoDraculRitual` removed from `SorceryType`; ritual rows and UI paths stripped; Coils / `CoilOrdoEligibility` unchanged. |
| **P3-4 / P3-5** Docs JSON tables | Done | Reference tables updated when files lived under `docs/`; **authoritative** `Target Successes` / `Ranking` now live in the three SeedSource catalogs (optional human mirrors under `docs/` only). |
| **P4** Backlog | Partial | Necromancy event UI path + `ResolveRiteActivationPoolAsync` still open. Theban Humanity floor enforced at learn + eligible + approve. |

---

## Summary

The core learning/approval pipeline and basic activation costs are implemented correctly for all three Ritual Disciplines. **Remaining gap for “complete” V:tR ritual casting:** extended-action session (P1-1), outcome Conditions (P1-3), and informational Potency (P1-4). Reference doc JSON tables (P3-4/5) are updated; other P4 verification items (Necromancy degeneration UI, preview pool API) remain backlog.

---

## P0 — Bugs (Break Existing Functionality)

### P0-1 · Crúac catalog JSON has a UTF-8 BOM that breaks JSON parsing

**File:** `src/RequiemNexus.Data/SeedSource/cruac_rituales.json` (historical note: formerly `rites.json`)
**Symptom:** Python's `json.load()` raises `JSONDecodeError: Expecting value` when reading the file directly without BOM handling. Seeding uses `File.ReadAllText` + `JsonDocument.Parse` in `SeedDataLoader.TryLoadJson`; verify whether a leading UTF-8 BOM still causes parse failure and fallback to the minimal in-memory Crúac set (meaning **all full-catalog Crúac rites are missing from the database**). Same invariant applies to the current canonical filename.

**Fix:** Fix once in `SeedDataLoader.TryLoadJson` so all seed JSON files share the same BOM-tolerant read path. Either resave the catalog without the BOM or pass `new UTF8Encoding(detectEncodingFromByteOrderMarks: true)` to the reader.

**Side effect:** Several `Effect` strings in the Crúac catalog contained mojibake (`Cr├║ac` instead of `Crúac`). Fix encoding at the same time as the BOM normalization pass (see also P3-1).

**Acceptance tests:**
- After seeding, the Crúac row count in `SorceryRiteDefinitions` must match the number of Crúac entries loaded from `cruac_rituales.json` (not a fixed magic number: P3-1 can add or rename rites, e.g. *Donning the Beast’s Flesh*). Prefer asserting against a **seed-derived expected count** (parse the catalog or `SorceryRiteSeedData.LoadCatalogEntries` in the test) or a documented minimum baseline that tracks seed changes.
- Add a seed assertion integration test: `SorceryRiteSeedData_Cruac_LoadsAllEntries` — e.g. `Assert.Equal(expectedFromSeed, dbCount)` or `dbCount >= BaselineDocumentedInTest` so the test does not rot when the catalog grows.
- **Failure mode:** 0 or “minimal fallback” Crúac rows indicates BOM/parse failure or empty catalog load.

---

### P0-2 · Theban Sorcery sacrament requirement not enforced per miracle

**File:** `src/RequiemNexus.Application/Services/SorceryActivationService.cs`
**Symptom:** The activation service checks `RiteRequirementValidator.ValidateAcknowledgments()` against the `RequirementsJson` column. However, the Theban miracles in `Theban_Sorcery_rituals.json` store sacrament text in the human-readable `Prerequisites` field — not as structured `RiteRequirement(PhysicalSacrament)` entries in `RequirementsJson`. Therefore **no sacrament acknowledgment is demanded at runtime**, violating the core cost of every Theban miracle.

**Fix:** Add a `RequirementsJson` population pass to the seed importer for Theban miracles. Each catalog entry must emit at least one `{ "Type": "PhysicalSacrament", "Value": 1, "DisplayHint": "<sacrament text>" }` requirement. The sacrament hint text comes from the existing `Prerequisites` field (already in the format "Sacrament: …"). If a row lacks `Sacrament:` in `Prerequisites`, use overlay or code — **do not** rewrite the canonical JSON for that.

**Open question — UX:** Should the acknowledgment UI show a single "I have the sacrament" checkbox (current generic pattern), or display the per-miracle sacrament text as the label? Confirm also whether the sacrament is consumed at the **first roll** or only when the ritual reaches its crescendo (per PDF: "the sacrament crumbles to dust when the ritual reaches a crescendo" — implies on success or terminal failure, not on first roll). Acknowledge this in the UI copy.

**Acceptance test:** Extend `SorceryServiceTests` — a call to `BeginRiteActivationAsync` for a Theban miracle without acknowledging the sacrament must return a validation failure.

---

## P1 — Critical Missing Rules

### P1-1 · No extended action system for ritual casting

**Affected:** All three traditions
**Rule (PDF p. 152):**
> Action: Extended. Ritualists may roll as many times as the unmodified dice pool. The base time per roll is half an hour, reduced to 15 minutes if the character has more dots in the Ritual Discipline than the dot rating of the ritual being cast. A ritual must be completed in one attempt. Ritualists do not receive any bonus for attempting a ritual having already failed with a near miss; rituals automatically fail if interrupted; and ritualists may not use Defense while casting.

**Current state:** The codebase has no `ExtendedAction` concept. `BeginRiteActivationAsync` resolves a pool and returns it for a single dice roll — the ST/player presumably handles success tracking manually.

**Required changes:**
1. Add `TargetSuccesses` (int) to `SorceryRiteDefinition` entity and seed data for every rite (prerequisite: P1-2).
2. Add `RiteActivationSession` (or extend `DiceRollerModal` context) to track:
   - `AccumulatedSuccesses` so far
   - `RollsRemaining` (= unmodified pool − rolls already made)
   - `TimePerRoll` (30 min or 15 min based on Discipline dots vs. rite level)
   - `IsAbandoned` / `IsInterrupted` flags
3. The activation service must refuse a new roll if `RollsRemaining == 0` or session is interrupted.
4. The activation service must detect when `AccumulatedSuccesses >= TargetSuccesses` and trigger the ritual effect.
5. Per-roll failure (0 successes accumulated in a roll) must apply the `Stumbled` Condition and let the player decide to continue or abandon. *(See P1-3 for clarification on Stumbled interactions.)*
6. Ritualists may not use Defense while casting — if rituals can overlap with combat turns, enforce this at the action-resolution layer (or document as a Storyteller call if combat integration is out of scope for this phase).

**Cost timing (extended actions):** Today `BeginRiteActivationAsync` deducts Vitae/Willpower/stains in one call before the dice pool is returned. For true extended rituals, decide explicitly: costs **up front once** (current shape, matches “sacrifice then roll until done or fail”), **per roll**, or **on completion** — and split the API if needed (e.g. `OpenRiteActivationSessionAsync` vs `CommitRiteRollAsync`) so behavior matches table intent and P1-1 abandonment rules (“lose sacrifice already paid”).

**Open question — session persistence:** Where does `RiteActivationSession` state live? Options: (a) ephemeral in-memory / Blazor circuit state (lost on refresh/disconnect); (b) persisted `CharacterRiteSession` entity; (c) SignalR session (Phase 7 dependency). Choose before implementation. If ephemeral, document that interrupted rituals must be manually adjudicated.

**UI shape (deliver alongside API):** The casting dialog must show: current accumulated successes, successes needed, rolls remaining, time per roll, and a "Continue / Abandon" prompt after each failure.

**Acceptance tests:**
- A Rating-3 rite with an unmodified pool of 6 may receive at most 6 rolls.
- A player who abandons mid-ritual loses the sacrifice (Vitae/Willpower already paid).
- A ritual with 0 successes on a roll adds the `Stumbled` Condition to the character.
- Unit tests: `TargetSuccesses` respected; roll cap enforced; `Stumbled` on failure-continue.

---

### P1-2 · No `TargetSuccesses` stored per rite

**Affected:** All three traditions
**Rule (PDF p. 152–154):** Each rite/miracle has a specific target success count (e.g., *Pangs of Proserpina* = 6, *Rigor Mortis* = 5, *Blood Scourge* = 6, *Vitae Reliquary* = 5, *Malediction of Despair* = 13).

**Required changes:**
1. Ensure every catalog row carries target successes (unified key **`Target Successes`** in the three canonical SeedSource JSON files, or importer defaults per tradition).
2. Add `TargetSuccesses` column to `SorceryRiteDefinition` (EF migration required).
3. Display target successes in the ritual casting UI so players know how many successes they need.

---

### P1-3 · Roll outcome Conditions not applied

**Affected:** Crúac and Theban Sorcery
**Rules (PDF p. 152):**

| Outcome | Crúac | Theban Sorcery |
|---------|-------|----------------|
| Dramatic Failure | Tempted Condition | Humbled Condition |
| Failure (any roll, player continues) | Stumbled Condition | Stumbled Condition |
| Exceptional Success | Ecstatic Condition | Raptured Condition |

**Current state:** The condition system (`IConditionRules`) exists (delivered in Phase 17), but no Condition is applied on ritual roll outcomes.

**Required changes:**
1. Extend `BeginRiteActivationAsync` / the per-roll result handler to dispatch:
   - `TemptedConditionEvent` on Crúac dramatic failure.
   - `HumbledConditionEvent` on Theban dramatic failure.
   - `StumbledConditionEvent` on any failure where the player continues.
   - `EcstaticConditionEvent` (Crúac) or `RapturedConditionEvent` (Theban) on exceptional success.
2. Verify that `Tempted`, `Humbled`, `Ecstatic`, `Raptured`, and `Stumbled` Conditions are already defined in the Condition catalog; add them if not.

**Clarification — Stumbled:** Stumbled fires when a per-roll result is 0 successes *and the player chooses to continue*. It does not fire on terminal failure (abandon). Kindred Necromancy has no tradition-specific dramatic-failure Condition defined in the rules; treat it as no-condition on dramatic failure (document this as a Storyteller ruling rather than a bug).

**Open question — Necromancy outcome table:** The PDF only defines Tempted/Humbled/Ecstatic/Raptured for Crúac and Theban. Confirm whether Necromancy uses no special conditions, or if a supplement adds them.

---

### P1-4 · No Potency mechanic

**Affected:** All three traditions
**Rule (PDF p. 152, sidebar):**
> The potency of a ritual is one after meeting the target number of successes, plus one for every additional success rolled above the target. If you get an exceptional success during your ritual, you can choose to add your Discipline dots to Potency.

**Current state:** Rite effects describe "Potency in damage" but there is no computed Potency value flowing out of the activation service.

**Required changes:**
1. Compute `int Potency` = 1 + (total successes − `TargetSuccesses`). On an **exceptional success**, the rules allow the player to **choose** to add their Ritual Discipline dots to Potency — this must **not** be automatic; expose an explicit UI opt-in (checkbox or confirm) when exceptional success occurs, then add dots only if selected.
2. Return or surface Potency from the activation / per-roll completion path (exact signature follows P1-1 session design).
3. Pass Potency to the dice roller result and display it in the UI.
4. Narrative effects that reference Potency (e.g., *Stigmata*, *Blood Blight*, *Corpse Preservation*) should show Potency in the result summary.

**Open question — scope:** For this phase, Potency is informational output (displayed to the ST/player). Full mechanical consumption by an effect interpreter (auto-applying damage, healing, or duration) is a separate larger feature. Define the boundary explicitly before building.

---

## P2 — Important Missing Rules

### P2-1 · Crúac extra Vitae bonus not implemented

**Rule (PDF p. 153, Modifiers table):**
> Crúac Only: The ritualist sacrifices more Vitae than required — bonus equal to excess Vitae spent.

**Current state:** The cost model enforces exactly the required Vitae; there is no UI affordance for the player to spend additional Vitae for a dice bonus.

**Required changes:**
1. Add an optional `ExtraVitae` (int 0–5) input to `BeginRiteActivationRequest`.
2. In `SorceryActivationService`, deduct the extra Vitae and add them as a flat bonus to the pool before returning `PoolSize`.
3. UI: show a spinner or numeric input "Spend extra Vitae for +dice?" on the Crúac casting dialog.

---

### P2-2 · Blood Sympathy roll modifier not wired for ritual pools

**Rule (PDF p. 153, Modifiers table):**
> +1 to +3 Power is turned on or applies to a vampire with whom the ritualist already has blood sympathy. Crúac doubles this modifier to +2 to +6.

**Current state:** Blood Sympathy rolls were delivered in Phase 18, but the existing modifier is not wired into ritual pool resolution.

**Required changes:**
1. When a target is specified for a rite activation, compute blood sympathy rank between ritualist and target.
2. Apply +1/+2/+3 bonus to Theban/Necromancy pools; apply +2/+4/+6 for Crúac.
3. `BeginRiteActivationRequest` needs an optional `TargetCharacterId` field.

**Scope note:** Blood Sympathy applies only when the ritual's power is "turned on" a specific vampire (per PDF p. 153: "Power is turned on or applies to a vampire with whom the ritualist already has blood sympathy"). Environmental or territory rites with no named vampire target do not receive the bonus. `BeginRiteActivationAsync` should only compute sympathy when `TargetCharacterId` is set.

---

### P2-3 · Crúac Humanity hard cap not enforced

**Rule (PDF p. 151):**
> Simply knowing the Discipline caps Humanity at 10 − Crúac dots.

**Current state:** The Humanity cap may not be enforced when a character gains Crúac dots.

**Required changes:**
1. In `CharacterDisciplineService` (or wherever Crúac dots are written), after incrementing Crúac, call `IHumanityService` to clamp Humanity to `10 − newCruacDots`.
2. Add a check in `IHumanityService.SetHumanityAsync` that enforces the cap whenever Crúac dots are present on a character.
3. Verify that the breaking point on *learning* a new Crúac dot (Humanity 4+ triggers degeneration) is dispatched from `CharacterDisciplineService`. Add `DegenerationCheckRequiredEvent(DegenerationReason.CruacLearned)` if missing.

**Acceptance test:** A character with Crúac 3 cannot have Humanity above 7.

---

### P2-4 · Kindred Necromancy torpor duration penalty not implemented

**Rule (`docs/magic_types_and_rules.txt`):**
> Dots in Necromancy are added to the vampire's Blood Potency when calculating the duration they must spend in torpor (up to a maximum effective Blood Potency of 10).

**Current state:** `TorporService` uses Blood Potency for torpor duration but does not include Necromancy dots.

**Required changes:**
1. In `TorporService.CalculateTorporDuration()` (or equivalent), query the character's Necromancy discipline dots.
2. Add `min(BloodPotency + NecromancyDots, 10)` as the effective Blood Potency for the torpor table lookup.
3. Display the effective BP (and its source) in the torpor UI panel.

---

### P2-5 · Kindred Necromancy acquisition gates not correctly modeled

**Rule (`docs/magic_types_and_rules.txt`):**
> Any vampire can learn it if they meet one of three conditions: 1) Finding a teacher (limits knowledge to mentor's rituals); 2) Having a mortal cultural connection to death magic; 3) Holding membership in a specific bloodline (Sangiovanni, Apollinaire, Burakumin, or Rexroth).

**Current state:** Necromancy rites may have `RequiredClanId` set to Mekhet in seed data, effectively making it Mekhet-only. This contradicts the rules — Necromancy is **not** clan-restricted.

**Required changes:**
1. Verify Necromancy catalog entries: remove any `RequiredClanId`/`RequiredCovenantId` from seed data (they should be null).
2. The Storyteller approval step (`ApproveRiteLearnAsync`) is the correct gate for validating teacher/cultural/bloodline qualifications narratively. No hard code gate at the clan level should exist for Necromancy.
3. Review `SorceryService.GetEligibleRitesAsync()` *and* `IsTraditionAllowedForCharacter` (and any related helpers) — confirm neither applies a clan filter for Necromancy. Fixing only the seed data is insufficient if the service layer also contains a hard gate.

---

## P3 — Seed Data Rating Corrections

> **Context (historical):** A comparison of `docs/*.json`, `src/RequiemNexus.Data/SeedSource/*.json`, and the PDF revealed systematic rating inflation in the seed data and divergent Necromancy sets. **P5 supersedes file layout:** the only runtime ritual catalogs are the three unified-schema files under `SeedSource/` (see **P5**). The subsections below remain as a **PDF cross-check log** for ratings; filenames `rites.json`, `rituals.json`, and `necromancyRites.json` are **retired**.

### P3-1 · `src/RequiemNexus.Data/SeedSource/rites.json` — Crúac ratings inflated by +1 on many rites

Verified against PDF (pages 152–154). The docs file (`docs/cruac_rituales.json`) is correct for these; the seed data is wrong:

| Rite | PDF / docs (correct) | seed (wrong) |
|------|----------------------|--------------|
| Pangs of Proserpina | 1 | 2 |
| Rigor Mortis | 1 | 2 |
| The Mantle of Amorous Fire | 1 | 2 |
| The Pool of Forbidden Truths | 1 | 2 |
| The Hydra's Vitae | 2 | 3 |
| Mantle of the Beast's Breath | 2 | 3 |
| Doom of Osiris | 2 | 4 |
| Ahmet's Pursuit | 3 | 2 *(under-rated)* |
| Shed the Virulent Bowels | 3 | 4 |
| Curse of Aphrodite's Favor | 3 | 4 |
| Curse of the Beloved Toy | 3 | 4 |
| Sanguine Auger | 1 | 3 |
| Wisdom of the Blood | 3 | 3 ✅ |
| Touch of the Morrigan | 3 | 4 |
| Mantle of the Glorious Dervish | 4 | 3 *(under-rated)* |
| Mantle of the Predator Goddess | 5 | 4 |
| The Hand of Seth | 4 | 3 |
| Clotho's Skein | 4 | 4 ✅ |
| Bounty of the Storm | 4 | 5 |
| Denying Hades | 4 | 5 |
| Manananggal's Working | 4 | 5 |
| Feast of the Ra | 3 | 5 |

Also: `docs/cruac_rituales.json` has 2 rites not in seed under slightly different names — reconcile:
- `"The Thrashing of Apep's Coils"` (docs) = `"The Thrashing of Apeps Coils"` (seed, missing apostrophe) — fix name in seed.
- `"Donning the Beast's Flesh"` is in docs but absent from seed — add it at Rating 2.

The seed has 11 rites not in docs at all (custom/supplemental rites like *Lair of the Beast*, *Red Tick*, territory rites). These are fine — keep them, but verify their ratings against any available supplement sources.

---

### P3-2 · `src/RequiemNexus.Data/SeedSource/rituals.json` — Theban miracle ratings inflated on several entries

The docs file has all rankings as `"None"` so cannot help here; use the PDF directly (pages 154–155):

| Miracle | PDF (correct) | seed (wrong) |
|---------|---------------|--------------|
| Vitae Reliquary | 1 | 2 |
| Curse of Babel | 2 | 3 |
| Liar's Plague | 2 | 3 |
| Malediction of Despair | 3 | 4 |
| Stigmata | 4 | 5 |

Blood Scourge (1), Blandishment of Sin (1), Marian Apparition (2), Gift of Lazarus (4), Transubstantiation (5) are already correct.

---

### P3-3 · Necromancy catalog (superseded by P5)

**Supersedes prior “Option A vs B” decision.** The shipped Necromancy rite set is **exactly** the entries in `src/RequiemNexus.Data/SeedSource/kindred_necromancy_rituals.json` (unified schema: `name`, `Effect`, `Roll`, `Prerequisites`, `Target Successes`, `Ranking`). The old `necromancyRites.json` custom eight-rite set is **removed** — do not maintain a parallel catalog.

**Mechanics:** Prefer no overlay for `Target Successes` and numeric `Ranking` when present in JSON. Use a **small overlay or code-only split** only for variants not expressible as one row (e.g. dual-rating Theban entries such as Blandishment aggravated — **one DB row per playable variant** without editing the canonical JSON line).

---

### P3-4 · `docs/cruac_rituales.json` — add `TargetSuccesses` field

The docs Crúac file has correct ratings but no target success counts. Add these from the PDF for the 10 core rites (pages 152–154). This makes the docs file useful as a quick reference table:

| Rite | Rating | Target Successes |
|------|--------|-----------------|
| Pangs of Proserpina | 1 | 6 |
| Rigor Mortis | 1 | 5 |
| Cheval | 2 | 5 |
| The Hydra's Vitae | 2 | 5 |
| Deflection of Wooden Doom | 3 | 6 |
| Touch of the Morrigan | 3 | 6 |
| Blood Price | 4 | 8 |
| Willful Vitae | 4 | 7 |
| Blood Blight | 5 | 8 |
| Feeding the Crone | 5 | 10 |

---

### P3-5 · `docs/Theban_Sorcery_rituals.json` — add ratings and target successes

All rankings are `"None"`. Add correct integer ratings from the PDF and target successes. See P3-2 table above for corrections; target successes from PDF:

| Miracle | Rating | Target Successes |
|---------|--------|-----------------|
| Blood Scourge | 1 | 6 |
| Vitae Reliquary | 1 | 5 |
| Blandishment of Sin | 1 | 5 |
| Curse of Babel | 2 | 6 |
| Liar's Plague | 2 | 5 |
| Malediction of Despair | 3 | 13 |
| Gift of Lazarus | 4 | 8 |
| Stigmata | 4 | 5 |
| Transubstantiation | 5 | 8 |

---

## P5 — Canonical ritual lists in RequiemNexus.Data

| Canonical file (under `src/RequiemNexus.Data/SeedSource/`) | Replaces (removed) | Role |
|--------------------------------------------------------------|--------------------|------|
| `cruac_rituales.json` | `rites.json` | **Authoritative** list of Crúac rites that exist |
| `Theban_Sorcery_rituals.json` | `rituals.json` | **Authoritative** list of Theban miracles |
| `kindred_necromancy_rituals.json` | `necromancyRites.json` | **Authoritative** list of Kindred Necromancy rites |

**Rules**

1. **Single source of truth:** These three JSON files are the **only** authoritative source for which rites exist and for authored fields (`Effect`, `Roll`, `Prerequisites`, `Target Successes`, `Ranking`). Do not maintain parallel SeedSource ritual catalogs. Editorial changes happen **only** in these files.
2. **Relocation:** Files were **git mv**’d from `docs/` into `SeedSource/`; `docs/` must not remain the runtime source (optional markdown pointers for humans only).
3. **Importer:** Parse the unified keys (case-insensitive / alias fallbacks only if needed for older tooling). Parse `Target Successes` to `int`. Parse `Ranking` per **Ranking semantics** below. Missing fields fall back to code defaults **for that field only**.
4. **Ranking semantics (all three traditions):**
   - **Numeric:** A character may **learn / request / cast** the rite only if their dots in the matching ritual discipline are **≥** the parsed minimum (first integer in the string, e.g. `"2 (or 4 for the aggravated variant)"` → 2 for the base row). **Crúac** / **Theban Sorcery** / **Necromancy** respectively. Learn and activation use the **same** check (aligned with Theban Humanity floor pattern for Theban).
   - **`Elder` token** (case-insensitive): **Elder-only** learn + cast. Defined in code as `SorceryElderRules.MinimumBloodPotency` (see `docs/rules-interpretations.md`). Stored on `SorceryRiteDefinition.RequiresElder`.
   - **Compound rows:** Emit **one DB row per playable variant** via code-side splitting / distinct names **without** editing the canonical JSON (e.g. Blandishment aggravated).
5. **Theban sacraments:** `DbInitializer.BuildThebanRequirementsJson` uses `Prerequisites` text. If `Sacrament:` lines are missing, handle via overlay or code — **do not** rewrite canonical Theban JSON for that.
6. **Deletion:** `rites.json`, `rituals.json`, `necromancyRites.json` are removed from `SeedSource` after cutover. Catalog tests load the new filenames.
7. **Cross-doc:** `plan-blood-sorcery-audit-review.md` and `docs/mission.md` Track D state that **Data/SeedSource** holds the three canonical lists.

---

## P6 — Ordo Dracul rituals removed (Coils only)

Per `docs/magic_types_and_rules.txt` (Mysteries of the Dragon), **Coils and Scales** are the mechanical dragon “sorcery” track; **Ordo ritual spells** are not modeled as a fourth `SorceryType` in this codebase.

**Delivered**

- Remove `SorceryType.OrdoDraculRitual` and all Ordo ritual seed rows (e.g. Dragon’s Own Fire).
- Strip Ordo ritual branches from `SorceryService`, `DbInitializer`, and Web UI; keep `CoilOrdoEligibility` and coil seed paths.
- EF migration: delete `SorceryRiteDefinitions` with legacy stored enum value **3** (and dependent `CharacterRites`); add `RequiresElder` column as applicable.

`CovenantDefinition.SupportsOrdoRituals` may remain as a dormant column for now (optional future migration to drop).

---

## P4 — Backlog (Rules from `magic_types_and_rules.txt` not yet prioritized)

These appear in the narrative source-of-truth doc and are correctly implemented or out-of-scope for the current phase, but warrant verification or a future ticket.

| Topic | Status | Note |
|-------|--------|------|
| **Theban Humanity floor for casting** | `SorceryActivationService` + `SorceryService` | Enforced at cast, eligible list, `RequestLearnRiteAsync`, and `ApproveRiteLearnAsync`. |
| **Necromancy breaking point on ritual use** | Dispatches `DegenerationCheckRequiredEvent` (Humanity ≥ 7) per explore | Verify event reaches the degeneration/remorse UI panel end-to-end; add integration test if not covered. |
| **Crúac spilled Vitae becomes inert** | Narrative-only for now | Blood spilled during a rite is unsuitable for feeding. Stays narrative unless Vitae economy tracking is added. No code change needed yet. |
| **Necromancy alternate dice pools** | Not implemented | Some bloodlines use Presence + Persuasion or Composure + Occult instead of Resolve + Occult + Necromancy. Future: data-driven pool override per rite × bloodline combination. |
| **Defense while casting** | Not enforced | Ritualists may not use Defense during casting. Only relevant if rituals run concurrently with the combat pipeline (Phase 14). Document as Storyteller ruling until combat/ritual overlap is in scope. |
| **`ResolveRiteActivationPoolAsync`** | Implemented; unused from `RequiemNexus.Web` | A future “preview pool” call that bypasses `BeginRiteActivationAsync` would skip Theban Humanity, sacrament acknowledgment, resource checks, and Necromancy degeneration dispatch. **Options:** remove the dead API, mirror the same gates in preview, or document preview as strictly informational (no enforcement). |

---

## Execution Order

| Priority | Item | Effort | Blocks | Key Tests |
|----------|------|--------|--------|-----------|
| P5 | Canonical trio in `SeedSource/` + importer + remove old trio + catalog tests | M | — | `SorceryRiteSeedData` tests; DB row counts match catalogs |
| P6 | Remove Ordo ritual enum + rows + UI; migration cleanup | S | — | No `SorceryType` 3 rows; build green |
| P0-1 | Fix catalog JSON BOM + mojibake; centralize in `SeedDataLoader` | XS | P1-2, P3-1 | Seed assertion: Crúac row count matches seed-derived expected count |
| P0-2 | Wire Theban sacrament to `RequirementsJson` | S | — | `BeginRiteActivationAsync` fails without sacrament ack |
| P2-5 | Fix Necromancy clan gate (seed + `IsTraditionAllowedForCharacter`) | XS | — | Non-Mekhet character sees Necromancy rites |
| P1-2 | Add `TargetSuccesses` to seed data + EF migration | S | P1-1 | All rite rows have `TargetSuccesses > 0` |
| P3-1/2 | Correct inflated ratings (historical; pre–P5 filenames) | S | — | Verified against PDF table above |
| P2-3 | Crúac Humanity cap + learning breaking point | S | — | Crúac 3 → Humanity capped at 7 |
| P2-4 | Necromancy torpor duration penalty | S | — | Torpor duration uses `min(BP + NecroDoTS, 10)` |
| P1-1 | Implement extended action system (decide session persistence + cost timing first) | L | P1-3, P1-4 | Roll cap; Stumbled on continue; sacrifice consumed on abandon; Vitae/WP timing per P1-1 |
| P1-3 | Apply ritual Conditions on roll outcomes | S | P1-1 | Tempted/Humbled on dramatic failure; Stumbled on continue |
| P1-4 | Implement Potency return value (informational) | S | P1-1 | Potency = 1 + excess successes; exceptional success → optional Discipline dots via UI opt-in |
| P2-1 | Extra Vitae bonus for Crúac | S | — | Pool +2 when 2 extra Vitae spent |
| P2-2 | Blood Sympathy modifier for ritual pools | M | — | Targeted rite only; bonus absent for environmental rites |
| P3-3 | ~~Decide Necromancy catalog~~ — **done via P5** (`kindred_necromancy_rituals.json` only) | — | — | — |
| P3-4/5 | Add `TargetSuccesses` + correct ratings to docs JSON files | XS | — | — |
| P4 | Backlog verification (Humanity floor at learn-time, Necromancy BP event end-to-end) | XS | — | — |

**XS** = ≤ 1 hour · **S** = 2–4 hours · **M** = 4–8 hours · **L** = 1–2 days
