# Phase 13 E2E & Accessibility — Review Status

This document tracks review findings against [PHASE_13_E2E_ACCESSIBILITY.md](./PHASE_13_E2E_ACCESSIBILITY.md).
All issues from both review passes are now resolved.

---

## Pass 1 — Resolved

| Topic | Resolution |
|-------|-----------|
| `ASPNETCORE_ENVIRONMENT` / Redis | Plan uses `Testing`; `AppFixture.MigrateAsync()` documented; cites `Program.cs` guards. |
| xUnit | All samples use `[Fact]` / `Assert.True` / `Assert.Equal`. |
| Layout / toasts | `MainLayout.razor` announcers; `ToastContainer` noted as already accessible; Track 3 gaps limited to 5 SignalR-bypass components. |
| Snapshot update command | `$env:PLAYWRIGHT_UPDATE_SNAPSHOTS = '1'` pattern documented. |
| Visual regression gating | Dedicated `RequiemNexus.VisualRegression.Tests` project + `continue-on-error` CI job. |
| Broken facelift doc links (plan) | [PHASE_13_E2E_ACCESSIBILITY.md](./PHASE_13_E2E_ACCESSIBILITY.md) contains no links to deleted `UI_UX_FACELIFT*.md`. |
| Lighthouse static URLs | Dynamic `/characters/{id}` removed; covered by axe in E2E instead. |
| `permissions:` blocks | Present in both sample `e2e.yml` and `lighthouse.yml`. |
| Architecture questions | Host model, `TestEmailSink`, palette smoke guard, checklist-only coverage — all spelled out in the plan. |
| Editorial | Out of scope, dependency diagram, flake policy, DoD → `mission.md`. |

---

## Pass 2 — Resolved

### 1. Broken links to deleted UI facelift docs (repo-wide) ✅

All references to `docs/UI_UX_FACELIFT.md` and `docs/UI_UX_FACELIFT_REVIEW.md` updated to
point to `docs/PHASE_13_E2E_ACCESSIBILITY.md` across:

| File | Change |
|------|--------|
| `docs/mission.md` | "Currently Active" callout |
| `docs/README.md` | "Next up" paragraph |
| `docs/Architecture.md` | E2E testing row in Testing Architecture table |
| `agents.md` | "Next execution plan" |
| `GEMINI.md` | Strategy Phase instruction |
| `CLAUDE.md` | "Next" bullet + Reference Docs list |
| `Contributing.md` | Step 4 in contributing workflow |

### 2. `ISignalRBackplane` — plan vs codebase ✅

The non-existent `ISignalRBackplane` stub was removed. Plan now states: `Program.cs` already
skips `AddStackExchangeRedis` when `IsEnvironment("Testing")` is true, so SignalR falls back to
the in-process backplane automatically — no extra abstraction needed.

### 3. Track 2 "separate CI step" inconsistency ✅

Removed the claim that the `[Trait("Category", "Accessibility")]` group maps to a separate CI
step. Plan now states clearly: a11y tests run in the **same E2E CI job**; the trait exists for
local filtering only (`--filter "Category=Accessibility"`).

### 4. Lighthouse trigger vs `deploy.yml` ✅

`lighthouse.yml` now triggers on `push` to `main` and `workflow_dispatch` (works today), with
`workflow_run` on `Deploy` included but gated by a comment noting it activates when
`deploy.yml` re-enables automatic triggers. The `if:` condition handles both trigger types.

### 5. Minor corrections ✅

- Typo `StortellerSnapshots.cs` → `StorytellerSnapshots.cs` fixed in §4.1 folder tree.
- JS module import pattern clarified: `announcer.js` uses `export function`; components import
  via `IJSRuntime.InvokeAsync<IJSObjectReference>("import", "./js/announcer.js")` — consistent
  with the existing `ReconnectModal.razor.js` pattern; no global `window` pollution.

---

## Remaining open items

None. All issues from both review passes are resolved. The plan is ready for implementation.

Follow the **Deliverables Checklist** in [PHASE_13_E2E_ACCESSIBILITY.md](./PHASE_13_E2E_ACCESSIBILITY.md),
starting with shared infrastructure (`AppFixture`, `AuthFixture`, `TestEmailSink`).
