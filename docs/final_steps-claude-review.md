# Review: `final_steps.md` — Questions, Suggestions & Improvements for Claude

This note accompanies [`final_steps.md`](./final_steps.md). Use it when implementing Phases 17–18 so the plan stays aligned with the repo and rules.

**Status:** All critical issues and suggestions have been applied to `final_steps.md`. Open design questions are answered below.

---

## Critical: Reconcile with the codebase before coding

### 1. `ConditionType` is a Domain enum, not a database table

**Status: ✅ FIXED in `final_steps.md`**

**Resolution:** Step 1 has been completely rewritten. The chosen approach is **extend `IConditionRules`** — the existing Domain service interface used for `GetConditionDescription` and `AwardsBeatOnResolve`. A new `GetPenalties(ConditionType type)` method returns `IReadOnlyList<ConditionPenaltyModifier>`. No migration required.

Key finding: `Stunned` and `Blind` are `TiltType` values, not `ConditionType`. Their effects are already handled by `ConditionRules.GetTiltEffects()`. They have been removed from the penalty table.

---

### 2. Degeneration stain threshold: doc vs. `HumanityService`

**Status: ✅ FIXED in `final_steps.md`**

**Resolution:** The doc formula `stains ≥ (11 − Humanity)` was **wrong**. The code uses `stains >= character.Humanity`, which is correct per VtR 2e p.185: _"a number of Stains equal to or greater than her Humanity score."_ Step 3 and Step 9 have been updated to use the correct formula and cite the rulebook page. Do not change the code.

---

### 3. Event type name

**Status: ✅ FIXED in `final_steps.md`**

**Resolution:** All doc references now use `DegenerationCheckRequiredEvent`. The type is confirmed at `src/RequiemNexus.Domain/Events/DegenerationCheckRequiredEvent.cs`. The existing handler is at `src/RequiemNexus.Application/Events/Handlers/DegenerationCheckRequiredEventHandler.cs`.

---

### 4. Step 3 scope: `EvaluateStainsAsync` is partially done

**Status: ✅ FIXED in `final_steps.md`**

**Resolution:** Step 3 has been reframed as "Wire `EvaluateStainsAsync` call sites." The method exists and is correct. Remaining work: identify stain-adding services, add `EvaluateStainsAsync` calls after stain persistence, define idempotency policy.

---

## Questions (open design) — ANSWERED

**1. Idempotency (Step 3 test)**

**Decision:** The event fires **every time** `EvaluateStainsAsync` runs with stains at or above threshold — fire-every-time semantics. The handler must be idempotent (logging is already idempotent; Phase 17's SignalR push will overwrite the banner state, which is also idempotent). This means a banner can be "re-shown" if stains remain after a failed roll — which is correct game behavior. Documented in Step 3 and `rules-interpretations.md` entry.

---

**2. Blind dual JSON (Step 1 table)**

**Non-issue — resolved with architecture change.** `Blind` is `TiltType.Blinded`, not a `ConditionType`. The Tilt is already handled by `ConditionRules.GetTiltEffects()` which returns `"−3 to attack and Perception rolls (Blinded)"`. No dual-entry problem exists in the new approach because Tilts and Conditions use separate mechanisms.

---

**3. Stunned `{"flag":"NoAction"}` — Domain vs. Web enforcement**

**Non-issue — resolved with architecture change.** `Stunned` is `TiltType.Stunned`, not a `ConditionType`. Its "cannot act this turn" effect is already described by `GetTiltEffects()`. Enforcement: the existing Tilt infrastructure and UI already surface this. No new "no action flag" is needed in the Domain.

---

**4. Passive aura outside combat (Phase 18 Track A)**

**Confirmed:** Auto-trigger is tied to `CombatEncounter`. Social scenes without an encounter rely entirely on the ST manual button in the Glimpse NPC panel. This is documented in the Track A acceptance criteria and is an intentional scope boundary (ambient scene detection requires a session/location entity that is out of scope per `mission.md` Non-Goals).

---

**5. Track C interceptor roll pool**

**Decision:** Interceptors use `Manipulation + Persuasion` vs. the initiator's pool — this is the social maneuvering roll formula (VtR 2e p.82). The interceptor's successes are subtracted from the initiator's net door reductions. `Successes` on `ManeuverInterceptor` records the result of a single roll (not cumulative). Document in Track C4 rules log.

---

**6. Content licensing (Track D)**

**Project policy:** Use only mechanical names and paraphrased descriptions. Do not reproduce verbatim rulebook text in seed JSON. Power names (Rite of X, Coil of Y) are used as identifiers; descriptions are original paraphrases sufficient for play reference. This matches the existing pattern in `Disciplines.json`, `bloodSorceryRites.json`, and `devotions.json`.

---

## Suggestions — APPLIED

| # | Suggestion | Status |
|---|---|---|
| 1 | Sub-track count: "three" → "four" | ✅ Fixed |
| 2 | Typo: `ChroniclId` → `ChronicleId` | ✅ Fixed |
| 3 | Unicode: `DegenerationReason.CrúacPurchase` | ✅ Noted — keep consistent with existing enum member in `DegenerationCheckRequiredEvent.cs` which already uses `CrúacPurchase`. Do not add an ASCII alias — match the existing file. |
| 4 | `final_steps.md` vs. `mission.md` relationship | ✅ Fixed — supersedence note added at top of `final_steps.md` |
| 5 | Full paths | ✅ Fixed — Key Files table updated to full `src/…` paths |
| 6 | Week 1 Step 9 (rules stub early) | ✅ Accepted — add a minimal rules stub (threshold formula + `ConditionPoolTarget` constants) to Week 1 work |
| 7 | Step 6 authorization clarity | ✅ Fixed — ST always; player only for their own character via `RequireCharacterOwnerAsync`. Masquerade sequence enforced in service, not UI. |

---

## Improvements — STATUS

| # | Improvement | Status |
|---|---|---|
| 1 | Migration naming | ✅ No new migrations from Phase 17 Step 1 (architecture change). Remaining migrations use descriptive names. |
| 2 | Integration test stable pool | ✅ Fixed in Step 2 — test now specifies "fixed attribute+skill pool from test seed" |
| 3 | `EncounterAuraContest` cleanup on encounter delete | ✅ Add cascade delete on FK in `EncounterAuraContestConfiguration` — document in Track A migration |
| 4 | `ManeuverInterceptor.Successes` semantics | ✅ Answered above: single-roll result, not cumulative |
| 5 | Extend `DegenerationCheckRequiredEventHandler` | ✅ Fixed in Step 6 — explicitly calls for extending the handler rather than a parallel path |
| 6 | Incapacitated accessibility | ✅ Add to acceptance criteria: overlay must have `role="alert"` and `aria-label="Incapacitated — all actions suspended"` per Phase 13 a11y standards. Focus trap is not required (it's an overlay, not a modal). |

---

## Alignment checklist for Claude before merge

- [x] Degeneration threshold matches `HumanityService` (`stains >= Humanity`) + tests + `rules-interpretations.md`.
- [x] Condition penalties use `IConditionRules.GetPenalties()` — no fictional `ConditionType` table.
- [x] Docs and code use `DegenerationCheckRequiredEvent`.
- [ ] `mission.md` phase checkboxes updated when `final_steps` acceptance items complete.
- [ ] `dotnet format` and `.\scripts\test-local.ps1` per `AGENTS.md`.

---

*Updated after codebase verification. All critical issues resolved.*

**Post-review doc hygiene:** The execution-order block in `final_steps.md` briefly still referenced “PenaltyModifierJson migration”; it is aligned with Step 1 (`IConditionRules`, no migration). Phase 17 acceptance criteria now include the incapacitated overlay a11y line (was only in this review file).

**Still manual / process:** Keep `mission.md` phase checkboxes in sync when Phase 17/18 acceptance items complete; run `dotnet format` and `.\scripts\test-local.ps1` before merge (`AGENTS.md`).
