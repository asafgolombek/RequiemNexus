# Blood Sorcery Audit — Review Notes (Open Questions & Improvements)

**Date:** 2026-04-01
**Applied:** 2026-04-01 (initial merge); 2026-04-01 (second review — §7)
**Related:** [plan-blood-sorcery-audit.md](./plan-blood-sorcery-audit.md)

This companion document records consistency fixes, open questions, gaps versus `magic_types_and_rules.txt`, and execution-plan improvements identified during review of the blood sorcery audit. It does not replace the audit; it extends it.

All items are marked **✅ Applied** (merged into the audit) or **⏳ Open** (still requires a decision or cannot be resolved in the plan doc alone).

---

## 1. Document consistency

- ✅ **Seed path mismatch:** Paths now uniformly use `src/RequiemNexus.Data/SeedSource/` everywhere in the audit. Verified against actual `SorceryRiteSeedData` → `SeedDataLoader.TryLoadJson` load path.

- ✅ **P0-1 vs actual loader:** P0-1 now correctly names `File.ReadAllText` + `JsonDocument.Parse` in `SeedDataLoader.cs` and notes that BOM behavior needs runtime verification. Fix targets `SeedDataLoader` centrally.

- ✅ **Encoding quality / mojibake:** Added to P0-1 as a companion fix alongside BOM normalization. Crúac catalog `Effect` strings containing `Cr├║ac` were corrected in the same pass.

---

## 2. Open questions

- ✅ **Necromancy catalog (P3-3):** **Superseded by P5** in the audit — the shipped set is `SeedSource/kindred_necromancy_rituals.json` only; `necromancyRites.json` is retired. A future `Source` field remains optional only if product explicitly needs dual-track content (not the default).

- ✅ **Extended actions (P1-1):** **Decided 2026-04-02** — cost timing: once on `BeginRiteActivationAsync`. Session: Blazor/component state for progress; each roll to chronicle Redis roll history like `DiceRollerModal`; no DB ritual session entity. Disconnect/refresh → ST adjudication.

- ✅ **"Failure → Stumbled" (P1-3):** Clarified in P1-3: Stumbled fires on continue-after-failure only, not on terminal abandonment. Necromancy has no tradition-specific dramatic-failure Condition; documented as a Storyteller ruling rather than a missing implementation.

- ✅ **Potency (P1-4):** Scope bounded in P1-4: Potency is informational output in this phase. Full mechanical effect consumption is a separate future feature; boundary made explicit.

- ✅ **Blood sympathy target (P2-2):** Scope note added to P2-2: bonus applies only when `TargetCharacterId` is set; environmental/territory rites excluded.

- ✅ **Theban sacrament UX (P0-2):** Delivered in cast prep modal — per-miracle `DisplayHint` on sacrament checkbox; Theban crescendo timing note; ack flags on prep result (see audit P0-2).

---

## 3. Gaps vs `magic_types_and_rules.txt`

All items moved into the audit as a **P4 Backlog** table with current status and disposition.

| Topic | Audit location | Status |
|-------|---------------|--------|
| Theban Humanity floor for casting | P4 Backlog | ✅ In audit — verify learn-request gate |
| Necromancy breaking point on use | P4 Backlog | ✅ In audit — verify event reaches UI end-to-end |
| Crúac spilled Vitae | P4 Backlog | ✅ In audit — narrative-only, no code change yet |
| Necromancy alternate dice pools | P4 Backlog | ✅ In audit — future data-driven pool override |
| Defense while casting | P4 Backlog | ✅ In audit — Storyteller ruling until combat overlap in scope |
| `ResolveRiteActivationPoolAsync` dead API | P4 Backlog | ✅ In audit — options: remove, mirror gates, or document as informational |

**Coils/Scales:** Out of scope. No change needed.

---

## 4. Suggestions to strengthen the execution plan

- ✅ **Centralize robust JSON reads:** P0-1 targets `SeedDataLoader` centrally for all seed JSON files.

- ✅ **Tests:** Explicit test callouts added to P0-1 (seed assertion), P0-2 (sacrament validation), and P1-1 (roll cap, Stumbled, sacrifice-on-abandon). `SorceryServiceTests` extension noted.

- ✅ **P1-1 and UI:** UI shape (accumulated successes, successes needed, rolls remaining, time per roll, Continue/Abandon prompt) specified as a delivery requirement alongside the API in P1-1.

- ✅ **P2-5 detail:** `IsTraditionAllowedForCharacter` and all related service-layer gates added to the P2-5 review scope — not just the seed data.

- ✅ **Docs filename typo:** Grep-before-rename instruction added to P3-3.

---

## 5. Data flow (reference)

```mermaid
flowchart LR
  seed[SeedSource_3_ritual_JSON]
  loader[SeedDataLoader]
  db[SorceryRiteDefinitions]
  begin[BeginRiteActivationAsync]
  roller[DiceRoller_UI]
  seed --> loader --> db
  db --> begin --> roller
```

---

## 6. Where this doc lives

The companion file keeps [plan-blood-sorcery-audit.md](./plan-blood-sorcery-audit.md) stable as the execution checklist; substantive review commentary and follow-ups live here. If you prefer a single document later, merge sections into the audit as an addendum.

---

## 7. Second review (audit + companion updated)

The audit now contains most earlier review content (P0/P1/P2 clarifications, P4 backlog, execution table with tests). This pass checks alignment with code and notes what is still open or easy to drift.

### 7.1 P4 vs code (spot check)

- **Theban Humanity floor:** Enforced at **cast** time in `SorceryActivationService.BeginRiteActivationAsync` and at **learn** time in `SorceryService` (`GetEligibleRitesAsync`, `RequestLearnRiteAsync`, `ApproveRiteLearnAsync`) using the same `Humanity >= miracle.Level` rule.
- **Necromancy breaking point on use:** `BeginRiteActivationAsync` dispatches `DegenerationCheckRequiredEvent` when Humanity ≥ 7; **`DegenerationCheckRequiredChronicleBroadcastTests`** asserts `DegenerationCheckRequiredEventHandler` calls `BroadcastChronicleUpdateAsync` for in-chronicle characters; player sees an info toast and `BeginRiteActivationResult.NecromancyDegenerationCheckRaised`; Glimpse still consumes hub `ChronicleUpdateDto`.
- **`ResolveRiteActivationPoolAsync`:** **Removed** (was unused from Web; bypassed gates). Any future preview must go through `BeginRiteActivationAsync` or a deliberately gated successor.

### 7.2 Suggestions merged into audit (2026-04-01)

These were promoted from review into [plan-blood-sorcery-audit.md](./plan-blood-sorcery-audit.md):

| Topic | Audit location |
|-------|----------------|
| Crúac row-count acceptance (seed-derived / non-rotting test) | P0-1 **Acceptance tests** |
| Extended action + Vitae/Willpower cost timing vs `BeginRiteActivationAsync` | P1-1 **Cost timing (extended actions)** |
| Potency + exceptional success — UI **opt-in** for adding Discipline dots | P1-4 **Required changes** |
| `ResolveRiteActivationPoolAsync` preview / gate bypass risk | P4 backlog table |
| Execution table tweaks (P0-1 test wording, P1-1 cost timing, P1-4 opt-in) | **Execution Order** |

### 7.3 Companion doc maintenance

Sections **2–4** largely **mirror** the audit after the merge. To avoid dual edits going forward, treat [plan-blood-sorcery-audit.md](./plan-blood-sorcery-audit.md) as the **source of truth** for execution; keep this file for **§7-style deltas**, code verification, and remaining **⏳** items (e.g. Necromancy outcome table as Storyteller-only unless a supplement is adopted).

### 7.4 Remaining open decisions

All ⏳ items currently in the audit that require a product/chronicle/architecture decision before implementation can proceed:

| Item | Audit location | Decision needed |
|------|---------------|-----------------|
| ✅ **Necromancy catalog** | P5 / P3-3 | **Canonical** `SeedSource/kindred_necromancy_rituals.json` only; P3-3 Option A **retired** |
| ✅ **Ritual session persistence** | P1-1 | **Decided 2026-04-02:** Blazor/circuit UI state for extended progress; each roll → chronicle roll feed (Redis) like other dice; **no** `CharacterRiteSession` DB entity |
| ✅ **Cost timing for extended rituals** | P1-1 | **Decided 2026-04-02:** **Once** on `BeginRiteActivationAsync` only; no per-roll or on-completion billing; no separate open-session API for costs |
| ✅ **Theban sacrament UX** | P0-2 | **Delivered:** `RiteActivationPrepModal` — `DisplayHint` in sacrament label; crescendo note for Theban + `PhysicalSacrament`; prep result carries ack flags (no generic `confirm()`) |
| ⏳ **Necromancy outcome table** | P1-3 | No tradition-specific Conditions in PDF — confirm supplement adds none, or treat as Storyteller-only ruling |
| ✅ **Potency scope boundary** | P1-4 | **Delivered:** informational display at completion + optional exceptional Discipline dots in UI; mechanical effect consumption remains future work |
| ✅ **`ResolveRiteActivationPoolAsync` fate** | P4 Backlog | **Removed** from `ISorceryActivationService` / `SorceryActivationService` (2026-04-02) |
