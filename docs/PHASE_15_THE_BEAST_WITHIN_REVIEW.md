# Phase 15 — The Beast Within: plan review

Review of [PHASE_15_THE_BEAST_WITHIN.md](./PHASE_15_THE_BEAST_WITHIN.md) against [PLAYABILITY_GAP_PLAN.md](./PLAYABILITY_GAP_PLAN.md), [mission.md](./mission.md), and the current codebase. **Status:** all items below resolved in the plan — no open questions remain.

## Strengths

- **Frenzy as a pool-based save** (Resolve + Blood Potency, optional Willpower die removal) matches VtR 2e and preserves table agency better than a binary toggle.
- **Explicit triggers** (no ambient polling) align with the project's "no silent narrative automation" stance.
- **Centralizing Vitae mutation in `VitaeService`** is the right move for a reliable Vitae-zero hook; the doc correctly calls out fragility if every caller mutates `CurrentVitae` directly.
- **Torpor as `TorporSince` on `Character`** is a minimal, query-friendly model; separating `Hunger` (Vitae 0) from `Starvation` (torpor interval) in `FrenzyTrigger` avoids conflating code paths.
- **Duplicate tilt handling** (pre-insert check + filtered unique index on active tilts) matches the concurrency discussion in [PLAYABILITY_GAP_PLAN_REVIEW.md](./PLAYABILITY_GAP_PLAN_REVIEW.md).
- **Exit criteria and test matrix** are concrete enough to verify completion; dependency links to Phase 14 and Phase 17 are coherent.

---

## Questions — all resolved

### Q1 — Storyteller "Hunger" in the Glimpse dropdown ✅ Resolved
**Original:** Elsewhere the plan stated `Hunger` is always tied to `VitaeDepletedEvent`. If the ST can manually pick "Hunger," is that intentional?

**Resolution (AD 2 + T5.2):** `Hunger` is explicitly available as a manual ST trigger for narrative edge cases (off-screen Vitae depletion, etc.). The plan clarifies that this is a direct `FrenzyService` call and does **not** re-fire `VitaeDepletedEvent`. `Starvation` is the only trigger type excluded from the manual dropdown — it is torpor-interval only. Documented in the Rules Interpretation Log (Hunger trigger — manual ST).

---

### Q2 — Mutual exclusion: `TiltType.Frenzy` vs `TiltType.Rotschreck` ✅ Resolved
**Original:** The unique index on `(CharacterId, TiltType) WHERE IsActive = 1` prevents duplicate same-tilt but does not prevent one active Frenzy and one active Rotschreck simultaneously. Should the implementation enforce "only one Beast tilt at a time"?

**Resolution (AD 3 + T3.3):** Yes — only one Beast tilt at a time. `FrenzyService` now performs an application-level check for **any** active Beast tilt (`Frenzy` or `Rotschreck`) before rolling. If found, the save is suppressed and `SuppressedDueToBeastAlreadyActive = true` is returned. The DB unique index (same-type prevention) and the application Beast-tilt guard (cross-type prevention) are complementary and both required. `FrenzySaveResult` includes the new field. Test coverage added for both cross-tilt suppression cases.

---

### Q3 — Vitae-zero save when `Rotschreck` is already active ✅ Resolved
**Original:** If the character is in `Rotschreck` and Vitae hits 0, does the Hunger path still run, merge, or suppress?

**Resolution (AD 3 + Track 6 rules log):** Suppressed entirely. If the character is in any active Beast state, the automatic `Hunger` save from `VitaeDepletedEvent` is suppressed. Documented in the Rules Interpretation Log: "Vitae-zero when Rotschreck active — save suppressed; character is already in a Beast state."

---

### Q4 — Dice feed without an active session ✅ Resolved
**Original:** `PublishDiceRollAsync` requires a `chronicleId`. What happens when Vitae hits 0 on a character not in a live session?

**Resolution (AD 6 + T3.3 implementation rules + Track 6):** `FrenzyService` wraps the `PublishDiceRollAsync` call in a try/catch. On failure (no active session, no `CampaignId`, Redis unavailable), the error is logged and the method returns normally — the `FrenzySaveResult` is still correct and the tilt is still applied. The Storyteller sees the tilt on next Glimpse load. This fallback is documented in the Rules Interpretation Log.

---

### Q5 — Starvation notifications on a fixed interval (deduplication) ✅ Resolved
**Original:** `CheckStarvationIntervalAsync` performs no DB write. If the threshold is exceeded, every future tick may re-fire the same notification. Is one-shot-per-milestone required?

**Resolution (AD 8 + T2.1 + T3.5):** Yes. A `LastStarvationNotifiedAt` (`DateTime?` UTC) column is added to `Character` (batched into the `Phase15TorporState` migration). `CheckStarvationIntervalAsync` only fires a notification when `LastStarvationNotifiedAt` is null or the full interval has elapsed again since the last notification. When a notification fires, `LastStarvationNotifiedAt = UtcNow` is persisted. `AwakenFromTorporAsync` clears both `TorporSince` and `LastStarvationNotifiedAt`. Two new tests cover the deduplication behavior. Exit criterion #9 verifies it explicitly.

---

## Improvements — all resolved

### I1 — Background service precedent ✅ Resolved
**Original:** `SessionTerminationService` is a Redis keyspace subscription, not a periodic timer. The torpor loop should cite `AccountDeletionCleanupService` instead.

**Resolution (AD 9 + T4.1):** The plan now explicitly cites `AccountDeletionCleanupService` as the reference pattern (`IServiceScopeFactory` + `while (!stoppingToken.IsCancellationRequested)` + `Task.Delay`). The `SessionTerminationService` reference has been removed. T4.1 includes a startup delay matching the account cleanup service.

---

### I2 — Audit all Vitae decrements before claiming Vitae-zero coverage ✅ Resolved
**Original:** `SorceryService.cs` uses `ExecuteUpdateAsync` to decrement `CurrentVitae` directly. This path must also route through `IVitaeService` or Hunger frenzy on Vitae 0 will be missed.

**Resolution (T3.0):** A new prerequisite task T3.0 explicitly inventories every `CurrentVitae` and `CurrentWillpower` mutation call site on `Character` entities. Two spend sites are identified for migration (`CharacterHealthService` + `SorceryService`), and two initialization sites (`CharacterManagementService`, `EncounterService`) are explicitly exempted with rationale. The `SorceryService` bulk update refactor notes include a warning about preserving the atomicity of the conditional decrement (WHERE clause equivalent). This task is marked as a prerequisite to T3.1 and T3.2.

---

### I3 — Complete `TorporDurationTable` in the plan ✅ Resolved
**Original:** The sample stopped at BP 6 with `// ... etc`. Exit criteria and Domain tests require BP 1–10 verbatim.

**Resolution (T1.4):** The table is now complete for BP 1–10 with explicit day counts (1, 7, 30, 365, 3650, 36500, 182500, 365000, 3650000, `int.MaxValue`). Month = 30 days, year = 365 days, with rounding rationale. BP 10 uses `int.MaxValue` with a code comment. Domain test updated to assert BP 10 = `int.MaxValue` and all values strictly ascending.

---

### I4 — One type per file ✅ Resolved
**Original:** The T3.3 snippet showed `FrenzySaveResult` defined next to `IFrenzyService`, violating the one-type-per-file rule from AGENTS.md.

**Resolution (T3.3 + New Files Summary):** The plan now explicitly separates them: `IFrenzyService.cs` contains only the interface, `FrenzySaveResult.cs` is a separate file in `Application/Models/`. A parenthetical note "(separate file — one type per file rule)" is included in T3.3 and the New Files Summary table.

---

### I5 — Filtered unique index and EF provider compatibility ✅ Resolved
**Original:** Add a note to T2.2 to implement via Fluent API for both PostgreSQL and SQLite, and add a Data test.

**Resolution (T2.2):** T2.2 now specifies Fluent API implementation in `CharacterTiltConfiguration.cs` with both the SQLite and PostgreSQL filter syntax shown. A note is included about using a migration-override partial class or provider-conditional SQL. A `RequiemNexus.Data.Tests` integration test asserting the constraint on the active provider is called out explicitly.

---

### I6 — `IDomainEventDispatcher` registration and scope expectations ✅ Resolved
**Original:** Confirm handler resolution ordering and same-DbContext scope / single transaction expectation.

**Resolution (T1.3 + T4.2):** AD 5 now states: "All handlers are resolved from the same DI scope as the dispatching service and share the same `DbContext` instance. There is no nested transaction boundary." T4.2 registration block includes a comment: "Handler registered first = invoked first; ordering is intentional."

---

### I7 — Willpower centralization scope ✅ Resolved
**Original:** T3.2 said "all Willpower mutations" but didn't list known call sites to refactor.

**Resolution (AD 10 + T3.0):** Known call sites are now listed in AD 10 (narrative) and the detailed table in T3.0. `SorceryService` bulk update is the only spend to migrate. `AdvancementService` max-recalculation and `CharacterManagementService` initialization are explicitly exempted with rationale.

---

## Minor consistency nits — all resolved

- **Mission cross-link:** Phase 16a note retained as-is per original plan; no conflict. ✅
- **PLAYABILITY_GAP_PLAN alignment:** All Phase 15 bullets in the gap plan are now represented in the updated plan doc. ✅
