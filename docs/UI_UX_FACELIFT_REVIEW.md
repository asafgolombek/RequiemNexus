# UI/UX Facelift Plan — Peer Review

**Role:** Review (engineering + UX alignment)
**Subject:** [UI_UX_FACELIFT.md](./UI_UX_FACELIFT.md)
**Status:** ✅ Review resolved — all items addressed in [UI_UX_FACELIFT.md](./UI_UX_FACELIFT.md)

## Purpose

This file is a **peer review and traceability record** for the facelift plan. It does **not** replace [UI_UX_FACELIFT.md](./UI_UX_FACELIFT.md); use both together when prioritizing work. The facelift spec is the **current next execution plan** for Phase 13 presentation work, as declared in [mission.md](./mission.md). Open rows in the question tables are **product decisions** still pending the team, not spec gaps.

---

## Overall assessment

The facelift plan is strong and usable as an implementation roadmap.

- **Audit table:** Findings are concrete and severity-weighted. Spot-checks against the codebase align: [SharedHeader.razor](../src/RequiemNexus.Web/Components/Layout/SharedHeader.razor) uses inline `style` on `<header>` and Bootstrap utilities while scoped CSS may diverge; [CharacterDetails.razor](../src/RequiemNexus.Web/Components/Pages/CharacterDetails.razor) and [CampaignCharacterView.razor](../src/RequiemNexus.Web/Components/Pages/Campaigns/CampaignCharacterView.razor) include an empty `char-avatar` placeholder.
- **Structure:** Tracks are ordered by dependency and impact; P0/P1 items correctly front-load token cleanup and header fixes.
- **Non-goals:** Explicit boundaries reduce scope creep (aesthetic identity, no framework swap, no data-model churn).
- **Phase 13 fit:** Track 6 (accessibility and polish) matches the mission's E2E and accessibility emphasis; see [mission.md](./mission.md).

---

## Questions

### From the original plan — resolution status

| # | Question | Status |
|---|----------|--------|
| 1 | **Icon system:** Custom `<Icon>` vs Lucide? | ⏳ Open — default is Option A (zero dependencies) unless team approves NuGet. AGENTS.md approval requirement documented in Track 3.1. |
| 2 | **Gold accent:** In brand scope? | ⏳ Open — see Track 1.1: `--color-gold` / `--color-gold-dim` are `EXPERIMENTAL` until WCAG AA contrast is verified; this question decides whether gold ever leaves experimental use in the product. |
| 3 | **Avatar system:** Initials only vs roadmap for character art? | ⏳ Open — initials avatar is the implementation target; the empty `char-avatar` div leaves a future upload slot. |
| 4 | **Tab grouping:** Acceptable to collapse to CORE/KINDRED/CHRONICLE? | ⏳ Open — preferred mobile pattern documented as horizontal scroll + right-edge fade (Track 4.2). Tab grouping flagged as the recommended path pending team sign-off. |
| 5 | **Animation intensity / performance mode?** | ⏳ Open — Track 6.4 adds `prefers-reduced-motion` as the baseline; a manual toggle remains a future option. |

**Q2 vs Q10:** Question **2** is the *brand / product* call (whether gold accent belongs in the product beyond experiments). Question **10** in the table below is the *engineering verification* track (WCAG contrast + keeping tokens `EXPERIMENTAL` until checks pass). They work together: Q10 can be satisfied while Q2 remains open, and resolving Q2 “out of brand” may retire gold usage regardless of contrast passes.

### Additional engineering / product questions — resolution status

| # | Question | Status |
|---|----------|--------|
| 6 | **Dependencies / NuGet approval** | ✅ Resolved — Track 3.1 now notes the AGENTS.md approval requirement and states Option A (inline SVG) is the default if zero-dependency constraint applies. |
| 7 | **Cmd+K platform label + aria-label** | ✅ Resolved — Track 2.3 updated: shortcut label is platform-aware (⌘K / Ctrl+K via JS interop), button requires explicit `aria-label="Open command palette"`. |
| 8 | **Session sidebar connection quality signal** | ✅ Resolved — Track 4.6 now gates the indicator on a real backend metric from `ISessionService` / `SessionClientService`. Fallback is a simple "Live" / "Reconnecting" label from the existing `IsConnected` flag. |
| 9 | **Mobile tab UX pattern** | ✅ Resolved — Track 4.2 documents preferred pattern: horizontal scroll with right-edge fade mask. Tab grouping (question 4) is the recommended upgrade path if approved. |
| 10 | **Gold token contrast** | ✅ Resolved — `--color-gold` and `--color-gold-dim` marked `EXPERIMENTAL` in Track 1.1 tokens block until WCAG AA contrast is verified against all target surfaces. |

---

## Suggestions — resolution status

| # | Suggestion | Status |
|---|------------|--------|
| 1 | **Phase 13 ordering:** land a11y fixes alongside/ahead of visual changes | ✅ Resolved — Implementation Order section now has an explicit "A11y-first rule" callout; Track 6 ARIA audit promoted to P1 (before large visual PRs). |
| 2 | **E2E and selectors:** plan test updates in same PRs as markup churn | ✅ Resolved — "Test selector hygiene" note added to Implementation Order section. |
| 3 | **Token migration hygiene checklist** | ✅ Resolved — Track 1.2 adds PowerShell `Select-String` paths, ripgrep (`rg`) for CI/cross-platform, plus contrast notes for new aliases. |
| 4 | **Mobile drawer a11y:** focus trap, Escape, aria-expanded, focus return | ✅ Resolved — Track 2.2 now lists these as explicit requirements that must ship together with the drawer, not after. |
| 5 | **Button consolidation:** deprecation map + batch migration | ✅ Resolved — Track 5.1 now describes the deprecation map approach with a CSS comment block showing old → new class mappings and a batch-PR migration strategy. |

---

## Improvements to the facelift document itself — resolution status

| Topic | Recommendation | Status |
|--------|----------------|--------|
| Track count | Executive summary said "5 tracks" but included Track 6 | ✅ Already fixed — current doc says "6 tracks". |
| Typo | "charater" → "character" in Non-goals | ✅ Already fixed. |
| Brand pulse snippet | CSS `@keyframes` cannot animate SVG `r` attribute; use `transform: scale()` | ✅ Already fixed — Track 2.4 uses `transform: scale()` on the dot with a correct keyframe. |
| Effort table | Add note that hours are relative hints, not fixed commitments | ✅ Already fixed — Implementation Order section opens with this statement. |
| Shadow tokens | Clarify whether per-level opacity is intentional or a shared alpha is preferred | ✅ Already fixed — a clarifying sentence was added after the elevation scale block. |

---

## Risks and trade-offs

*(Retained for reference — no action items, informational only)*

- **Character sheet scope:** Vitals redesign, tab grouping, collapsible sections, DotScale fractions, and liquid animations can each justify their own slice; bundling too much delays feedback and increases regression risk.
- **Icon system as a gate:** Home dashboard, empty states, toasts, and inline edit affordances all depend on a consistent icon approach; decide early so work does not stall on re-skinning.
- **Mobile navigation:** Header/drawer changes touch every route's chrome; expect layout and E2E churn until patterns stabilize.

---

## Follow-up remarks (second pass) — resolution status

| Topic | Note | Status |
|--------|------|--------|
| **Token grep (Track 1.2)** | Examples use Unix `grep -rn`. On Windows, use ripgrep (`rg`) or PowerShell `Select-String`. | ✅ Resolved — Track 1.2 now provides both a PowerShell block (default for Windows contributors) and a ripgrep block (CI), replacing the Unix-only `grep` commands. |
| **Platform shortcut (Track 2.3)** | `navigator.platform` is legacy and unreliable. Prefer User-Agent Client Hints or a Blazor helper. | ✅ Resolved — Track 2.3 explicitly bans `navigator.platform`, specifies `navigator.userAgentData.platform` with `userAgent` string fallback, and describes caching the result in `MainLayout`. |
| **Header vs Bootstrap (Track 2.1)** | Clarify that header chrome uses scoped CSS only while page layout keeps Bootstrap 5 grid. | ✅ Resolved — Track 2.1 now has an explicit "Scope boundary" paragraph: scoped CSS for `<header>` chrome only; Bootstrap 5 grid utilities for page layout are out of scope for this task. |
| **DotScale ARIA (Track 6.1)** | `role="slider"` only for keyboard-adjustable dots; read-only views should use `role="img"` + accessible name. | ✅ Resolved — Track 6.1 DotScale entry now distinguishes interactive vs read-only contexts, specifies `role="img"` with an example label (`"Strength 3 out of 5"`), and notes that roles must be confirmed per view. |
| **Question 2 vs tokens** | Gold scope (Q2) and Track 1.1 `EXPERIMENTAL` comments should stay linked so they cannot drift. | ✅ Resolved — `--color-gold` and `--color-gold-dim` token comments now reference Q2 by name; Q2 in the questions table references Track 1.1. |

---

## Third pass — notes for implementers — resolution status

| Topic | Guidance | Status |
|--------|----------|--------|
| **Track 1.2 — working directory** | Run the PowerShell and `rg` examples from the **repository root** so paths like `src/RequiemNexus.Web` resolve. Running from `src/` yields empty or partial results. | ✅ Resolved — Track 1.2 checklist now opens with an explicit “Run from the repository root” instruction. |
| **Track 2.3 — OS detection fallback** | `navigator.userAgentData` is unavailable in some browsers. When detection fails, default to Ctrl+K, show both chords, or use a neutral label. | ✅ Resolved — Track 2.3 now documents the fallback hierarchy: `userAgentData` → `userAgent` sniff → default to `Ctrl+K`; neutral label (`Search…`) listed as an alternative. |
| **Track 1 / 2 — sticky header `z-index`** | `SharedHeader.razor` uses Bootstrap stacking (`1030`); tokens propose `--z-sticky: 100`. Reconcile all layers before shipping. | ✅ Resolved — Track 1.1 elevation scale block now has a callout warning: explains the `1030` → `var(--z-sticky)` migration, confirms modal/toast/tooltip still render above at 400/500/600, and warns against leaving both values alive simultaneously. |

---

*"The blood is the life… but clarity is the power."*
