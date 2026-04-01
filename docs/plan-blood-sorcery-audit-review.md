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

- ✅ **Encoding quality / mojibake:** Added to P0-1 as a companion fix alongside BOM normalization. `rites.json` Effect strings containing `Cr├║ac` will be corrected in the same pass.

---

## 2. Open questions

- ⏳ **Necromancy catalog (P3-3):** Option A / B / C decision is documented in the audit. Storyteller must choose canon vs homebrew vs dual-track. Whether a `Source` field is added to `SorceryRiteDefinition` for filtering/UX is part of that decision.

- ✅ **Extended actions (P1-1):** Session persistence question added to P1-1 as an explicit pre-implementation decision point (ephemeral Blazor state vs persisted entity vs SignalR). Disconnect/refresh behavior documented as a Storyteller call if ephemeral option is chosen.

- ✅ **"Failure → Stumbled" (P1-3):** Clarified in P1-3: Stumbled fires on continue-after-failure only, not on terminal abandonment. Necromancy has no tradition-specific dramatic-failure Condition; documented as a Storyteller ruling rather than a missing implementation.

- ✅ **Potency (P1-4):** Scope bounded in P1-4: Potency is informational output in this phase. Full mechanical effect consumption is a separate future feature; boundary made explicit.

- ✅ **Blood sympathy target (P2-2):** Scope note added to P2-2: bonus applies only when `TargetCharacterId` is set; environmental/territory rites excluded.

- ✅ **Theban sacrament UX (P0-2):** Open question added to P0-2: per-miracle sacrament text in acknowledgment label; sacrament consumed at crescendo not first roll (per PDF).

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

- ✅ **Centralize robust JSON reads:** P0-1 now targets `SeedDataLoader` centrally rather than only `rites.json`.

- ✅ **Tests:** Explicit test callouts added to P0-1 (seed assertion), P0-2 (sacrament validation), and P1-1 (roll cap, Stumbled, sacrifice-on-abandon). `SorceryServiceTests` extension noted.

- ✅ **P1-1 and UI:** UI shape (accumulated successes, successes needed, rolls remaining, time per roll, Continue/Abandon prompt) specified as a delivery requirement alongside the API in P1-1.

- ✅ **P2-5 detail:** `IsTraditionAllowedForCharacter` and all related service-layer gates added to the P2-5 review scope — not just the seed data.

- ✅ **Docs filename typo:** Grep-before-rename instruction added to P3-3.

---

## 5. Data flow (reference)

```mermaid
flowchart LR
  seed[SeedSource_JSON]
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

- **Theban Humanity floor:** Implemented at **cast** time in `SorceryActivationService.BeginRiteActivationAsync` when `character.Humanity` is below the miracle’s dot rating (`def.Level`). **Not** enforced in `SorceryService.RequestLearnRiteAsync` — the P4 row (“verify learn-request gate”) is accurate; eligible list can still show miracles the character cannot cast until Humanity rises.
- **Necromancy breaking point on use:** `BeginRiteActivationAsync` dispatches `DegenerationCheckRequiredEvent` with `DegenerationReason.NecromancyActivation` when Humanity ≥ 7. P4’s “verify end-to-end to UI” remains the right follow-up (event wiring + test).
- **`ResolveRiteActivationPoolAsync`:** Exposed on `ISorceryActivationService` but **not referenced** from `RequiemNexus.Web` at present. If a future “preview pool” UI calls it without going through `BeginRiteActivationAsync`, it would **skip** Theban Humanity, sacrament ack, resource validation, and Necromancy degeneration — either remove the dead API, or mirror the same gates in preview, or document “preview is informational only.”

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

Sections **2–4** largely **mirror** the audit after the merge. To avoid dual edits going forward, treat [plan-blood-sorcery-audit.md](./plan-blood-sorcery-audit.md) as the **source of truth** for execution; keep this file for **§7-style deltas**, code verification, and the single **⏳** item until P3-3 is decided.

### 7.4 Remaining open decisions

All ⏳ items currently in the audit that require a product/chronicle/architecture decision before implementation can proceed:

| Item | Audit location | Decision needed |
|------|---------------|-----------------|
| ⏳ **Necromancy catalog** (Option A / B / C) | P3-3 | Canon vs homebrew vs dual-track; whether `Source` field added to entity |
| ⏳ **Ritual session persistence** | P1-1 | Ephemeral Blazor state vs persisted `CharacterRiteSession` entity vs SignalR (Phase 7) |
| ⏳ **Cost timing for extended rituals** | P1-1 | Vitae/WP deducted up-front once, per roll, or on completion; whether API needs `OpenRiteActivationSessionAsync` / `CommitRiteRollAsync` split |
| ⏳ **Theban sacrament UX** | P0-2 | Per-miracle sacrament text as checkbox label vs generic "I have the sacrament"; UI copy timing (consumed at crescendo, not first roll) |
| ⏳ **Necromancy outcome table** | P1-3 | No tradition-specific Conditions in PDF — confirm supplement adds none, or treat as Storyteller-only ruling |
| ⏳ **Potency scope boundary** | P1-4 | Informational-only display vs mechanical effect consumption; what triggers the second phase |
| ⏳ **`ResolveRiteActivationPoolAsync` fate** | P4 Backlog | Remove dead API, mirror all gates in preview, or document as strictly informational |
