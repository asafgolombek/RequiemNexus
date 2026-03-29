# Review: Phase 16b — The Discipline Engine (Power Activation)

**Document reviewed:** [`phase16b-the-discipline-engine.md`](./phase16b-the-discipline-engine.md)
**Review passes:** 2026-03-29 (initial through third — plan vs. sorcery reference); **2026-03-29 (fourth — post-implementation closure)**
**Purpose:** Historical assessment of the implementation plan; **current** status is **shipped** — this file now records closure after coding.

---

## Executive summary (post-implementation)

The plan was verified against `SorceryActivationService` / `CharacterDetails.razor.cs` before implementation; dice-feed responsibility and `_rollerBaseDice` vs. `_rollerFixedDicePool` were corrected in the plan. **Implementation is complete:** `ActivationCost` / `ActivationCostType` (Domain), `IDisciplineActivationService` / `DisciplineActivationService` (Application), `DisciplinePowerActivateModal` and character sheet wiring (Web), `DisciplineActivationServiceTests` and `ActivationCostTests`, DI registration, and **Phase 16b** entries in [`docs/rules-interpretations.md`](./rules-interpretations.md) (D3).

**Verdict:** Phase 16b is **delivered**. For as-built behavior, read `DisciplineActivationService`, `ActivationCost.Parse`, and the associated tests.

---

## Strengths (plan quality — retained for Grimoire)

1. **Traceable pattern** — Sorcery activation pipeline (transaction, dice feed, `DiceRollerModal`) as the reference reduces drift.
2. **Layering** — `ActivationCost` in Domain; Application orchestrates; Web modal stays presentational.
3. **Security** — `RequireCharacterAccessAsync` matches other in-play character flows; intentional difference from rites (`RequireCharacterOwnerAsync`) is documented.
4. **UX honesty** — Sync preview vs. async full modifiers, with rules-log coverage.
5. **Test matrix** — Costs, eligibility, campaign/dice feed, broadcast gating, plus Domain parse cases.
6. **Phase 17** — Correctly notes no Phase 16b code change when condition modifiers enrich `ResolvePoolAsync`.

---

## All findings — cumulative status

| # | Finding | Pass | Status |
|---|---------|------|--------|
| 1 | Exit criteria said “20 tests” vs. 12 + 9 in tables | 1st | **Resolved** — Objective now states 21 tests (12 application + 9 domain). |
| 2 | “Matching rite activation” vs. `RequireCharacterOwnerAsync` on rites | 1st | **Resolved** — Architectural Decisions spell out ST-inclusive discipline activation vs. narrower rite auth. |
| 3 | Preview path Warning vs. sorcery Error on bad JSON | 1st | **Resolved** — Group B2: Error + structured fields, explicitly matching sorcery. |
| 4 | Domain XML `cref` to `Data.Models.DisciplinePower` | 1st | **Resolved** — Summary refers to seeded `Cost` string only. |
| 5 | D3 rules log section absent | 1st | **Resolved** — `docs/rules-interpretations.md` contains the six Phase 16b bullets. |
| 6 | B2.9 calls `PublishDiceRollAsync` from service (wrong) | 3rd | **Resolved** — Step removed; dice-feed publication is `DiceRollerModal`'s responsibility. |
| 7 | C2 sets `_rollerFixedDicePool = dice` (should be `_rollerBaseDice`) | 3rd | **Resolved** — Implementation uses `_rollerBaseDice = dice; _rollerFixedDicePool = null`, matching rite flow. |

---

## Third-pass findings — bugs corrected in plan (historical)

### F1. `PublishDiceRollAsync` must not be called from the service

The plan's B2 step 9 originally read that the service would call `PublishDiceRollAsync`. **Resolution:** Step removed; matches `SorceryActivationService.BeginRiteActivationAsync`.

### F2. Roller state field: `_rollerFixedDicePool` → `_rollerBaseDice`

**Resolution:** Plan and implementation set `_rollerBaseDice = dice; _rollerFixedDicePool = null`.

---

## Minor residual notes (optional polish, non-blocking)

1. **Verification step 5** (“Insufficient resources → … no resource deducted”) is correct because the spend inside the EF transaction will not commit if `Result.IsSuccess` is false and the code throws before `tx.CommitAsync()`.

2. **Objective** mentions `dotnet format` clean; local/CI rigor is `dotnet format --verify-no-changes` via `test-local.ps1` — equivalent intent.

3. **Parse rules:** unknown token → `None` with “service logs Warning” refers to **`ActivatePowerAsync`** when the string is non-empty but parses to `None`; preview path may return 0 without that specific check.

---

## Architecture alignment (AGENTS.md / CLAUDE.md)

| Topic | Assessment |
|-------|------------|
| Layer direction | Application orchestration; Domain `ActivationCost`; Web thin. **OK.** |
| Masquerade | `RequireCharacterAccessAsync` + downstream services that enforce access on spend. **OK.** |
| Exceptions in Application | `InvalidOperationException` for activation guardrails; UI toasts. **OK** for Application layer. |
| One type per file | **OK.** |

---

## Implementation readiness checklist (closure)

- [x] All files in “Files to Create / Modify” exist and are registered in DI.
- [x] Tests green under `.\scripts\test-local.ps1` (including `DisciplineActivationServiceTests`, `ActivationCostTests`).
- [x] `docs/rules-interpretations.md` contains the six Phase 16b entries (D3).
- [ ] Manual smoke steps in the plan’s Verification section — repeat when validating releases.

---

## Suggested follow-ups (out of plan scope)

- **Compound costs** — Single `(Type, Amount)` may need extension if seed strings ever combine multiple resource types in one activation.
- **Rite auth parity** — ST activating rites for NPCs would be a separate `SorceryActivationService` change if product wants parity with disciplines.

---

## Conclusion

All seven review findings are **resolved**. Phase 16b shipped per [`phase16b-the-discipline-engine.md`](./phase16b-the-discipline-engine.md); remaining roadmap work is **Phase 17** (Humanity & Conditions) and later phases, not further Phase 16b design.
