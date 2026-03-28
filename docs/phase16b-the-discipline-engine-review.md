# Review: Phase 16b — The Discipline Engine (Power Activation)

**Document reviewed:** [`phase16b-the-discipline-engine.md`](./phase16b-the-discipline-engine.md)
**Review passes:** 2026-03-29 (initial), 2026-03-29 (second — plan + review doc refreshed), 2026-03-29 (third — codebase verification of sorcery pattern)
**Purpose:** Structured assessment of the implementation plan for clarity, architecture alignment, completeness, and consistency with the current codebase.

---

## Executive summary (third pass)

Third pass verified the plan against the actual `SorceryActivationService` and `CharacterDetails.razor.cs` source. Two **implementation bugs** were found and corrected in the plan — both relate to the dice-feed publish step and the roller state field. The plan is now **fully aligned with the sorcery reference pattern**.

**Repository state at third review:** `ActivationCost`, `DisciplineActivationService`, `DisciplinePowerActivateModal`, and tests are still absent (correct for in-flight work). `docs/rules-interpretations.md` still has no Phase 16b section (D3 — implementer task).

**Verdict:** Plan is implementation-ready. No further design questions remain.

---

## Strengths (unchanged)

1. **Traceable pattern** — Sorcery activation pipeline (transaction, dice feed, `DiceRollerModal`) as the reference reduces drift.
2. **Layering** — `ActivationCost` in Domain; Application orchestrates; Web modal stays presentational.
3. **Security** — `RequireCharacterAccessAsync` matches other in-play character flows; the plan now **documents** the intentional difference from rites.
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
| 5 | D3 rules log section absent | 1st | **Outstanding** — implement during Group D (not a plan bug). |
| 6 | B2.9 calls `PublishDiceRollAsync` from service (wrong) | 3rd | **Resolved** — Step removed; note added that dice-feed publication is `DiceRollerModal`'s responsibility. |
| 7 | C2 sets `_rollerFixedDicePool = dice` (should be `_rollerBaseDice`) | 3rd | **Resolved** — Updated to `_rollerBaseDice = dice; _rollerFixedDicePool = null`, matching rite flow. |

---

## Third-pass findings — bugs corrected in plan

### F1. `PublishDiceRollAsync` must not be called from the service ✅ FIXED

The plan's B2 step 9 originally read `”If character.CampaignId has a value: await sessionService.PublishDiceRollAsync(...)”`.

**Why this is wrong:** `SorceryActivationService.BeginRiteActivationAsync` (the reference) does **not** call `PublishDiceRollAsync` or `RollDiceAsync`. It calls only `BroadcastCharacterUpdateAsync` (and only when cost is non-zero). The dice roll and dice-feed publication are performed by `DiceRollerModal` when the code-behind opens it with the returned pool size. `PublishDiceRollAsync` requires a `RollResult` argument that the service cannot provide — `IDiceService` is not in the constructor.

**Resolution:** Step 9 removed from `ActivatePowerAsync`. Added an explicit note: “Do not call `PublishDiceRollAsync` or `RollDiceAsync` from the service — matches `SorceryActivationService.BeginRiteActivationAsync` exactly.”

### F2. Roller state field: `_rollerFixedDicePool` → `_rollerBaseDice` ✅ FIXED

The plan's C2 `HandleDisciplineActivateConfirmedAsync` originally set `_rollerFixedDicePool = dice; _rollerBaseDice = 0`.

**Why this is wrong:** The sorcery code-behind (`OpenRiteRoller`) sets `_rollerBaseDice = dice` and leaves `_rollerFixedDicePool` as its default (`null`). Both sorcery and discipline activation return a fully-resolved pool from `TraitResolver.ResolvePoolAsync` — there is no reason for discipline to use a different field.

**Resolution:** Updated C2 code snippet to `_rollerBaseDice = dice; _rollerFixedDicePool = null`.

---

## Minor residual notes (optional polish, non-blocking)

1. **Verification step 5** (“Insufficient resources → … no resource deducted”) is correct because the spend inside the EF transaction will not commit if `Result.IsSuccess` is false and the code throws before `tx.CommitAsync()`.

2. **Objective** mentions `dotnet format` clean; local/CI rigor is `dotnet format --verify-no-changes` via `test-local.ps1` — equivalent intent, no plan change required.

3. **Parse rules** (line ~118): unknown token → `None` with “service logs Warning” refers to **`ActivatePowerAsync`** when the string is non-empty but parses to `None`; preview path returns 0 without that specific check — consistent with the numbered steps in B2.

---

## Architecture alignment (AGENTS.md / CLAUDE.md)

| Topic | Assessment |
|-------|------------|
| Layer direction | Application orchestration; Domain `ActivationCost`; Web thin. **OK.** |
| Masquerade | `RequireCharacterAccessAsync` + downstream services that enforce access on spend. **OK.** |
| Exceptions in Application | `InvalidOperationException` for activation guardrails; UI toasts. **OK** for Application layer. |
| One type per file | Plan complies. **OK.** |

---

## Implementation readiness checklist

- [ ] All files in “Files to Create / Modify” exist and are registered in DI.
- [ ] All **21** tests green under `.\scripts\test-local.ps1`.
- [ ] `docs/rules-interpretations.md` contains the six Phase 16b entries (D3).
- [ ] Manual smoke steps in Verification completed once.

---

## Suggested follow-ups (out of plan scope)

- **Compound costs** — Single `(Type, Amount)` may need extension if seed strings ever combine multiple resource types in one activation.
- **Rite auth parity** — ST activating rites for NPCs would be a separate `SorceryActivationService` change if product wants parity with disciplines.

---

## Conclusion

Three review passes have been completed. All seven findings are resolved except D3 (rules log — implementer task at Group D). The plan is **specification-complete**: it correctly mirrors the `SorceryActivationService` / `OpenRiteRoller` reference pattern in service scope, dice-feed responsibility, and roller state field. Remaining work is coding + D3.
