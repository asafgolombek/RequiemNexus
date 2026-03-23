# Requiem Nexus — UI/UX Facelift Plan

**Role:** UI/UX Designer
**Status:** Active — current team execution focus (see `docs/mission.md` Phase 13)
**Target Phase:** Phase 13 — alongside E2E, accessibility CI, screen reader polish, and visual regression (`docs/mission.md`)

---

## Executive Summary

The app has a strong design DNA — dark gothic aesthetic, good token architecture, and cohesive crimson branding. The facelift focuses on **elevating quality without changing identity**. The goal is to fix structural inconsistencies, sharpen visual hierarchy, replace placeholder UX patterns (emoji icons, hardcoded colors), and make the character sheet — the app's core view — genuinely excellent to use.

Changes are grouped into **6 tracks** ordered by impact-to-effort ratio.

---

## Audit Findings

Before the plan, here's what the audit surfaced:

| # | Finding | Severity |
|---|---------|----------|
| 1 | **CSS dead code:** `SharedHeader.razor.css` defines `.main-header`, `.header-content`, etc. but the markup uses inline `style=""` — the classes are never applied | High |
| 2 | **Hardcoded colors outside tokens:** `Home.razor.css` uses `#ff4d4d` and `#ffffff` in the welcome banner gradient instead of design token variables | Medium |
| 3 | **No spacing scale:** Padding/margin values are ad-hoc throughout (e.g., `px-3 py-3`, `padding: 2.5rem`, `gap: 1rem` with no system) | Medium |
| 4 | **Emoji as icons:** Home dashboard widget icons are OS-rendered emoji (🦇📖🩸) — inconsistent size, color, and rendering across platforms | Medium |
| 5 | **No mobile navigation:** On mobile, the nav items in the header are hidden (`d-none d-md-flex`) with no hamburger/drawer replacement | High |
| 6 | **No elevation scale:** Cards, modals, dropdowns, and tooltips all use ad-hoc box-shadows; no defined layering system | Low |
| 7 | **No font size scale:** Sizes scattered: `0.75rem`, `0.8rem`, `0.85rem`, `0.875rem`, `0.9rem` — minor differences with no semantic names | Low |
| 8 | **Character sheet header has a dead avatar:** `<div class="char-avatar"></div>` is an empty div with no content or placeholder | Medium |
| 9 | **Inline styles in SharedHeader markup:** The `<header>` element uses `style="background:var(--deep-black)..."` alongside a `.razor.css` that defines `.main-header` separately — duplicated, conflicting intent | High |
| 10 | **No icon system:** Mix of emoji, Unicode symbols (✎), and SVG icons — no cohesive visual language for actions | Medium |

---

## Track 1: Foundation — Tokens, Spacing, Type Scale

**Effort:** Medium | **Impact:** Every component, forever

This track establishes the bedrock that all other tracks build on. No visual changes are visible to users, but everything downstream becomes more consistent.

### 1.1 — Expand `design-tokens.css`

Add the following token groups:

**Spacing scale** (based on 4px base unit):
```css
--space-1: 4px;
--space-2: 8px;
--space-3: 12px;
--space-4: 16px;
--space-5: 20px;
--space-6: 24px;
--space-8: 32px;
--space-10: 40px;
--space-12: 48px;
--space-16: 64px;
```

**Font size scale:**
```css
--text-xs:   0.75rem;   /* 12px — labels, hints, badges */
--text-sm:   0.875rem;  /* 14px — body secondary, meta */
--text-base: 1rem;      /* 16px — body primary */
--text-lg:   1.125rem;  /* 18px — large body, sub-headings */
--text-xl:   1.25rem;   /* 20px — section headings */
--text-2xl:  1.5rem;    /* 24px — page headings */
--text-3xl:  1.875rem;  /* 30px — hero headings */
--text-4xl:  2.25rem;   /* 36px — display headings */
```

**Elevation scale** (z-index + shadow pairs):
```css
--shadow-1: 0 1px 3px rgba(0,0,0,0.4);           /* Flat surfaces */
--shadow-2: 0 4px 12px rgba(0,0,0,0.5);           /* Cards */
--shadow-3: 0 8px 24px rgba(0,0,0,0.6);           /* Modals, dropdowns */
--shadow-4: 0 16px 48px rgba(0,0,0,0.7);          /* Overlays */
--shadow-crimson: 0 0 16px rgba(139,0,0,0.35);    /* Accent glow */

--z-base:   0;
--z-above:  10;
--z-sticky: 100;
--z-modal:  400;
--z-toast:  500;
--z-tooltip: 600;
```

Per-level shadow opacity above is intentional (stronger lift for higher layers). A shared alpha variable is optional if the team prefers a single knob later.

> **⚠ z-index reconciliation required during Track 2:** `SharedHeader.razor` currently sets `z-index: 1030` on the sticky header (Bootstrap convention). The token above proposes `--z-sticky: 100`. Before adopting the token scale, verify that `--z-modal` (400), `--z-toast` (500), and `--z-tooltip` (600) all render **above** the header at their respective values — they will, since 400 > 100. Then remove the hardcoded `1030` from the header and replace with `var(--z-sticky)`. Do not leave both values alive simultaneously or layering will be non-deterministic.

**Border radius scale:**
```css
--radius-sm: 4px;
--radius-md: 8px;
--radius-lg: 12px;
--radius-xl: 16px;
--radius-full: 9999px;
```

**Additional semantic color aliases:**
```css
--color-surface-1: var(--deep-black);      /* Page background */
--color-surface-2: var(--charcoal);        /* Primary panels */
--color-surface-3: var(--ash-gray);        /* Nested panels, inputs */
--color-surface-4: var(--muted-gray);      /* Borders, dividers */
--color-gold: #C9A84C;                     /* EXPERIMENTAL — in brand for achievements/special states (Q2 resolved); drop EXPERIMENTAL after WCAG AA on surfaces (Q10) */
--color-gold-dim: #8A6F32;                 /* EXPERIMENTAL — verify WCAG AA on target surfaces before promoting */
--color-info: #2563EB;                     /* Info state */
--color-info-dim: rgba(37, 99, 235, 0.15);
--color-warning-dim: rgba(184, 134, 11, 0.15);
--color-success-dim: rgba(46, 90, 58, 0.15);
--color-danger-dim: rgba(139, 0, 0, 0.15);
```

### 1.2 — Migrate hardcoded values in existing CSS

- **`Home.razor.css` line 23–25:** Replace `#ff4d4d` with `var(--crimson-glow)` and `#ffffff` with `var(--bone-white)`
- **`SharedHeader.razor`:** Remove inline `style="background:..."` from `<header>` — apply `.main-header` class instead so the `.razor.css` rules activate
- Audit all `.razor.css` files for any remaining `#RRGGBB` values outside the token files

**Token migration hygiene checklist** — run these searches before closing the P0 task. **Run from the repository root** (`C:\gitrepo\RequiemNexus`) so the `src/RequiemNexus.Web` paths resolve correctly; running from inside `src/` will yield partial or empty results.

```powershell
# PowerShell (Windows — works in CI and local dev)
Get-ChildItem -Recurse -Include *.css,*.razor.css src/RequiemNexus.Web |
  Select-String -Pattern '#[0-9A-Fa-f]{3,6}'

Get-ChildItem -Recurse -Include *.razor src/RequiemNexus.Web |
  Select-String -Pattern 'style="[^"]*#[0-9A-Fa-f]'
```

```bash
# ripgrep (cross-platform, preferred in CI)
rg "#[0-9A-Fa-f]{3,6}" src/RequiemNexus.Web/wwwroot/css/ src/RequiemNexus.Web/Components/ -g "*.css"
rg 'style="[^"]*#[0-9A-Fa-f]' src/RequiemNexus.Web/Components/ -g "*.razor"
```

Any new semantic aliases added must also be contrast-checked against surfaces — minimum: `--color-gold` on `--color-surface-2` and `--color-surface-3`.

---

## Track 2: Navigation & Global Chrome

**Effort:** Medium | **Impact:** Every page

### 2.1 — Fix SharedHeader CSS/Markup Mismatch

The current header markup uses Bootstrap utility classes and inline styles while the CSS file defines entirely separate classes (`.main-header`, `.header-content`, etc.) that are never applied. Choose one approach and make the markup match.

**Recommendation:** Switch entirely to scoped CSS classes (no inline styles, no Bootstrap utilities for layout) **for the header chrome**. This enables the media query rules in the `.razor.css` file to actually work and removes the current conflict with inline styles.

**Scope boundary:** This change applies only to header chrome (the `<header>` element and its children in `SharedHeader.razor`). Page-level layout — grids, content columns, responsive gutters — continues to use Bootstrap 5 grid utilities, consistent with the Non-goals section. Don't migrate Bootstrap grid classes as part of this task.

### 2.2 — Add Mobile Navigation Drawer

Currently on mobile all navigation links vanish. Add a slide-in drawer:

- Hamburger icon (3 lines → X transition) in the top-right on `<768px`
- Drawer slides in from the right, full-height overlay
- Contains: username, My Characters, My Campaigns, Settings, Log Out
- Backdrop click closes the drawer
- No Bootstrap dependency — pure CSS transform + Blazor state
- **Accessibility requirements (must ship together, not after):**
  - Hamburger button: `aria-expanded` bound to drawer open state, `aria-controls` pointing to the drawer element
  - Focus trap while drawer is open — keyboard focus must not escape to the page behind the overlay
  - `Escape` key closes the drawer
  - On close, return focus to the hamburger button (mirror modal guidance in Track 6.3)

```
Mobile header:  [Logo + Brand] ............. [☰]
Drawer open:    [overlay]  | My Characters   |
                           | My Campaigns    |
                           | Settings        |
                           | Log Out         |
```

### 2.3 — Header Visual Refinement

- Replace the search input with a **Command Palette trigger** button styled as a pill — the full `CommandPalette` already exists and is keyboard-triggered. The text input in the header currently does nothing. Label the shortcut hint platform-aware: `⌘K` on macOS, `Ctrl+K` on Windows/Linux. **Do not use `navigator.platform`** — it is legacy, deprecated, and unreliable across browsers. Preferred approach: detect once at app startup via a small Blazor JS interop call using `navigator.userAgentData.platform` (with a `navigator.userAgent` string fallback), store the result in a scoped service or `IJSRuntime` call cached in `MainLayout`, and pass it down as a parameter. The button must have an explicit `aria-label="Open command palette"` so screen readers are not left with a key-chord label that has no spoken meaning. **Fallback when detection is unavailable:** `navigator.userAgentData` is not present in all browsers (notably Firefox). When the API is absent and `userAgent` sniffing is ambiguous, default to `Ctrl+K` — it is the more common chord across contributors and Windows users. Alternatively, display both chords (`⌘K / Ctrl+K`) or use a neutral visible label (`Search…`) and omit the chord hint entirely.
- Add a **subtle bottom gradient fade** on the header to separate it from page content
- Add a thin `1px` crimson line at the very top of the header (like a "bleeding" effect)
- **Sub-nav active indicator:** The existing `::after` underline on `.sub-nav-link.active` is good — add a matching `color: var(--crimson-glow)` (not just bone-white) for the active tab text to make the selection more prominent

### 2.4 — Brand Mark

The current SVG logo is a minimal crosshair circle. Consider adding a **subtle pulse animation** to the center dot (`r="2"` circle with `fill:#C41E3A`) on hover — a living heartbeat. Prefer animating **`transform: scale()`** on the inner dot (or a wrapper `<g>`) rather than the SVG `r` attribute; CSS keyframes do not reliably animate presentation attributes on `<circle>`. Respect `prefers-reduced-motion: reduce` (see Track 6).

```css
@keyframes brand-pulse {
  0%, 100% { transform: scale(1); opacity: 1; }
  50% { transform: scale(1.2); opacity: 0.75; }
}
```

---

## Track 3: Home Dashboard & Navigation Pages

**Effort:** Low-Medium | **Impact:** First impression, daily driver

### 3.1 — Replace Emoji Icons with SVG Icons

The widget cards (🦇, 📖, 🩸) use OS-rendered emoji which vary wildly across platforms. Replace with inline SVGs or a consistent icon system.

**Option A (recommended):** Inline SVG component `<Icon Name="bats" />` with a library of ~20 gothic-appropriate icons — zero new dependencies, full control over paths and theming.
**Option B:** Use [Lucide Icons](https://lucide.dev/) (MIT) — lightweight, stroke-based, easy to crimson-tint with `currentColor`. **Note:** adding a NuGet package requires explicit approval per `AGENTS.md`. If Lucide is chosen, document the package name, version policy, and approval in the implementation PR. If zero new dependencies is the constraint, Option A is the default.

Widget icons should:
- Be `32px` or `40px` square
- Use `currentColor` so they inherit crimson/bone-white from parent
- Scale on hover with `transform: scale(1.1)` + crimson glow filter

### 3.2 — Welcome Banner Redesign

Current: Simple card with gradient text heading.

**Proposed:**
```
┌─────────────────────────────────────────────────────────┐
│  [Thin crimson left border]                             │
│                                                         │
│  REQUIEM NEXUS          [optional: last-login text]     │
│  ─────────────────                                      │
│  "The blood is the life. Your chronicle awaits."        │
│                                                         │
│  [3 stat chips: N Characters · N Campaigns · N Sessions]│
└─────────────────────────────────────────────────────────┘
```

- Add **real data**: show count of the user's characters and campaigns (inject services)
- The "Digital Grimoire" tagline can move here as a subtitle
- Stat chips use `--color-surface-3` background with crimson border

### 3.3 — Widget Cards Enhancement

- Add **count badges**: "3 characters", "2 campaigns" inside the card before navigation
- Add a **keyboard shortcut hint**: subtle label e.g. `C` for Characters (→ Cmd+K shortcut mapping)
- On hover, show a **right-arrow icon** nudge that slides in from the right
- Grid: Stay auto-fill but set `minmax(180px, 1fr)` instead of `140px` for better card proportions

---

## Track 4: Character Sheet

**Effort:** High | **Impact:** Core app experience — this is what users spend 90% of their time on

The character sheet is the heart of the app. It's currently functional but visually dense and lacks visual hierarchy between its sections.

### 4.1 — Character Sheet Header

**Current issue:** `<div class="char-avatar"></div>` is an empty div — it renders nothing.

**Fix:**
- **Near term:** **initials avatar** (or clear placeholder when no image): first letter of the character's name in a styled circle (dark crimson with bone-white text, gothic font)
- **Roadmap:** **user-uploaded portrait** per character (storage, validation, Masquerade ownership, and CDN/cache strategy to be designed in application/data layers — not blocked on facelift markup alone)
- For Clan affiliation, add a **clan glyph placeholder** (SVG slot or first-letter icon in a different style)
- Add a **status indicator dot** next to the name: green pulsing if in an active session, gray if not

**Header layout improvement:**
```
┌─────────────────────────────────────────────────────────┐
│  [Avatar]  CHARACTER NAME          [Status] [⚙ Actions] │
│            Clan · Covenant                              │
│            Concept · Mask · Dirge                       │
│            Chronicle: Campaign Name                     │
└─────────────────────────────────────────────────────────┘
```

### 4.2 — Tab Navigation

The character sheet likely uses a tab system for sections (Attributes, Skills, Disciplines, etc.). Improvements:

- Active tab: Use **bold + crimson underline + slightly brighter background** (currently too subtle)
- Tab list: Allow **horizontal scroll** on mobile with a fade-out on the right edge to hint at scrollability
- Consider **tab grouping**: "CORE" (Attributes, Skills) | "KINDRED" (Disciplines, Blood Sorcery, Lineage) | "CHRONICLE" (Conditions, Notes, Beats, Ghouls)
- **Tab grouping decision:** ⏳ **Open** — ship visual/tab polish first; decide whether to group into CORE/KINDRED/CHRONICLE after the character sheet facelift is far enough along to judge in context.
- **Preferred mobile pattern:** Horizontal scroll with a right-edge fade mask to signal overflow — no additional nesting, no toggle menu. This keeps the tab count visible and avoids a two-level navigation pattern on small screens. If tab grouping is later approved, it would shorten the scroll range; until then, keep the flat tab strip.

### 4.3 — Section Cards

Within the character sheet, each section (Attributes, Skills, etc.) is a card. Improvements:

- **Section headers:** Use `font-heading` Cinzel, ALL CAPS, `text-xs` tracking-widest, with a thin crimson left border
- **Collapsible sections:** Add collapse toggle to lower-priority sections (Notes, Beat Ledger, Ghouls) to reduce scroll on mobile
- Add a **subtle section divider** between grouped content within a section (e.g., Physical / Social / Mental split in Attributes)

### 4.4 — DotScale Component

The `DotScale` component (for Attribute/Skill ratings) is one of the most-used UI elements. Refinements:

- Add **group labels** above each dot cluster (Physical, Social, Mental) rendered in `text-xs text-secondary`
- Dots: Current `16px` desktop size is fine; ensure the `22px` mobile size is consistently applied
- Add **fractional fill states**: A "half dot" for temporary buffs (e.g., equipment bonuses) using a half-filled SVG variant
- The `bleeding` animation on increment is a great touch — keep it

### 4.5 — Vitals Panel (Blood Pool, Willpower, Humanity)

The CharacterVitals section is a key panel. Suggestions:

- Blood Pool: Use a **vertical column** of drop-shaped icons rather than horizontal dots — more thematic, fits better in a sidebar
- Willpower: Use **rectangular pips** (like the VTM sheet) to differentiate visually from blood/attributes
- Humanity: Consider a **gradient fill** from emerald-green (high) → amber (mid) → crimson (low) to convey moral state at a glance
- Add **animated fill/drain**: When blood pool changes in a session, animate the fill level with a liquid-pour feel

### 4.6 — Session Sidebar

When a character is in an active session (`SessionClient.IsConnected`), a sidebar appears. Refinements:

- Add a **glowing top border** to the sidebar to indicate live status
- Show the **SignalR connection quality** (green/yellow/red dot) — **gate on backend metric:** confirm that `ISessionService` or `SessionClientService` exposes a measurable signal (round-trip latency, hub connection state, or reconnect count) before shipping this indicator. A UI-only colour toggle without a real metric would be misleading. If no signal is available, show a simple "Live" / "Reconnecting" label derived from the existing `IsConnected` flag instead.
- The `SessionPresenceBar` (who's online) should be pinned to the top of the sidebar for immediate visibility

---

## Track 5: Components & Interactions

**Effort:** Low-Medium | **Impact:** Polish and cohesion throughout

### 5.1 — Button System Audit

Three button variants are currently used inconsistently: `.btn-primary`, `.btn-primary-rn`, and inline styles. Consolidate:

| Class | Usage | Style |
|-------|-------|-------|
| `.btn-rn-primary` | Primary action | Crimson gradient fill |
| `.btn-rn-secondary` | Secondary/cancel | Dark border, transparent fill |
| `.btn-rn-ghost` | Tertiary/icon | No border, hover highlight only |
| `.btn-rn-danger` | Destructive | Red border + hold-to-confirm |
| `.btn-rn-gold` | Special/achievement | Gold accent (new) |

All should be sized consistently: `sm` (28px h), `md` (36px h, default), `lg` (44px h).

**Migration approach — use a deprecation map, not a big-bang rename.** Add the new classes alongside the old ones, ship a mapping table in a comment block in `app-chrome.css`, then migrate pages in small batches (one component file per PR). This avoids a single mega-PR and keeps regressions reviewable. Remove old classes only after all call-sites are migrated.

```css
/* DEPRECATED — migrate to .btn-rn-* equivalents
   .btn-primary      → .btn-rn-primary
   .btn-primary-rn   → .btn-rn-primary
   .btn-secondary    → .btn-rn-secondary
   .btn-secondary-rn → .btn-rn-secondary
   .btn-ghost        → .btn-rn-ghost
   .btn-danger-hold  → .btn-rn-danger
*/
```

### 5.2 — Toast Notifications

- Add **icon per type**: ✗ danger, ⚠ warning, ✓ success, ℹ info — rendered as SVG, not emoji
- Add **dismiss button** visible on hover
- **Stack cap:** Max 4 toasts visible, older ones auto-dismiss as new ones arrive
- Position: Bottom-right on desktop, bottom-center on mobile

### 5.3 — Modals

Current modals vary in styling. Standardize:
- **Backdrop:** `backdrop-filter: blur(4px)` + `rgba(0,0,0,0.7)` overlay
- **Modal card:** `--color-surface-2` background, `var(--shadow-4)` shadow, `var(--radius-lg)` corners
- **Header:** Cinzel heading + thin crimson bottom border
- **Footer:** Button row, always right-aligned, primary action on right

### 5.4 — Empty States

`EmptyState.razor` is used throughout. Elevate it:

- Add a **slot for a thematic icon** (e.g., coffin SVG for empty characters, scroll for empty campaigns)
- Use Cinzel for the main message text
- Secondary text in `text-secondary`
- CTA button directly inside the component (optional slot)

Example for empty characters:
```
    ⚰️ (SVG coffin icon)
    No Characters Yet
    Your chronicle is unwritten.
    [Begin the Embrace →]
```

### 5.5 — Form Fields

- **Floating labels:** Already using Bootstrap 5 floating label pattern — ensure all forms use it consistently (some may still use static labels)
- **Validation states:**
  - Error: Red border + red helper text below the field
  - Valid: Subtle green checkmark icon trailing edge of input
  - Required indicator: small crimson `*` inline with label (not just HTML `required` attribute)
- **Textarea:** Add a character counter for fields with a max length
- **Select dropdowns:** Style the `<select>` with `.input-grimoire` to match text inputs; current browser default selects break the dark theme

### 5.6 — Loading States

- `SkeletonLoader` is good — ensure the `sheet` variant covers all sections of `CharacterDetails`
- Add a **fullscreen loader** for initial auth/route changes: pulsing logo SVG in the center of the screen
- `LoadingSpinner` should match the crimson aesthetic (it may already, confirm)

### 5.7 — Code Actions & Inline Editing

The character sheet has inline name editing (`_isEditingName`). The pattern of click-to-edit can be extended:

- Add a **pencil icon** (`✎`) that fades in on row hover to signal editability
- The edit icon should use the token-based approach, not a raw Unicode character
- Save/cancel with `Enter`/`Escape` keyboard shortcuts (may already exist — verify)

---

## Track 6: Accessibility & Polish

**Effort:** Low | **Impact:** Critical for Phase 13 goals

This track aligns directly with Phase 13 (E2E Testing & Accessibility).

### 6.1 — ARIA & Semantic HTML

- All icon-only buttons need `aria-label`
- Tab panels need `role="tabpanel"` + `aria-labelledby`
- DotScale: `role="slider"` + `aria-valuenow`/`aria-valuemin`/`aria-valuemax` **only when the dots are keyboard-interactive** (e.g., during character creation or advancement). On the read-only character sheet view, use `role="img"` with an `aria-label` such as `"Strength 3 out of 5"` instead — a slider that cannot be moved is misleading to assistive technology users. Confirm which views are interactive vs read-only before assigning roles.
- Modals need `role="dialog"` + `aria-modal="true"` + focus trap
- Toasts need `role="status"` or `role="alert"` depending on urgency
- `<nav>` elements need `aria-label` to distinguish primary/secondary nav

### 6.2 — Color Contrast

Run WCAG 2.1 AA contrast checks on:
- `var(--text-secondary)` (`#B8B3A8`) on `var(--charcoal)` (`#1A1A1A`) — target 4.5:1
- `var(--text-hint)` (`#8A8580`) on any surface — may fail at small sizes
- Crimson buttons: white text on `#8B0000` background — check at small sizes

### 6.3 — Focus Management

- Ensure all interactive elements are reachable and have a visible focus ring
- Modal open: focus the first interactive element inside
- Modal close: return focus to the trigger element
- Command palette: focus the search input on open

### 6.4 — Reduced Motion & Performance Mode

**System preference:** Add `@media (prefers-reduced-motion: reduce)` overrides for:
- Particle background animation (disable)
- `bloodSpread` / `dotFillPulse` (instant, no animation)
- Page fade-in transitions (instant)
- Card hover lift (disable transform)

**User setting (approved):** Add a **performance mode** (or equivalent name) in settings that **reduces motion globally** for users who do not have OS reduced-motion enabled — same class of overrides as above, plus any other heavy visuals (e.g. particle density) the team ties to this flag. Persist preference server- or client-side per product standards; respect both `prefers-reduced-motion` **and** performance mode (either one on = reduced visuals).

---

## Implementation Order & Priorities

Hour estimates below are **relative sizing hints**, not fixed commitments.

> **A11y-first rule:** Track 6 items (ARIA roles, focus management, contrast fixes) should land **alongside or just ahead of** the visual changes in their respective tracks — not as a trailing clean-up. Large markup refactors without accessibility baselines tend to require duplicate rework once Playwright and screen-reader tests run.

> **Test selector hygiene:** Replacing emoji, header structure, and button class names will break any brittle Playwright selectors. Plan explicit E2E test updates in the **same PR** as the markup change, or in an immediately following PR. Tie high-churn UI changes to the Phase 13 visual regression suite.

| Priority | Track | Task | Effort |
|----------|-------|------|--------|
| P0 | 1 | Migrate hardcoded colors to tokens | 1h |
| P0 | 2 | Fix SharedHeader CSS/markup mismatch | 1h |
| P1 | 1 | Expand design-tokens.css with spacing/type/elevation scales | 2h |
| P1 | 2 | Add mobile navigation drawer (incl. a11y requirements) | 3h |
| P1 | 6 | ARIA + focus management audit — baseline before large visual PRs | 3h |
| P1 | 4 | Character sheet header (initials/placeholder + status dot; portrait upload per roadmap) | 2h |
| P1 | 5 | Consolidate button system (deprecation map, batch migration) | 2h |
| P2 | 3 | Home dashboard — replace emoji icons with SVG | 2h |
| P2 | 3 | Welcome banner with real data | 2h |
| P2 | 4 | Vitals panel redesign (blood/willpower/humanity) | 3h |
| P2 | 5 | Modal standardization | 2h |
| P3 | 2 | Header — replace search with Cmd+K pill (platform-aware label) | 1h |
| P3 | 4 | Section collapsibility on mobile | 2h |
| P3 | 5 | Toast improvements | 1h |
| P3 | 5 | Empty state elevations | 1h |
| P3 | 6 | Reduced motion (`prefers-reduced-motion`) + user performance mode setting | 2h |
| P4 | 4 | DotScale fractional fill states | 2h |
| P4 | 4 | Blood pool liquid animation | 3h |
| P4 | 2 | Brand mark pulse animation | 1h |

---

## Team decisions (formerly “Questions for the Team”)

| # | Topic | Decision |
|---|--------|----------|
| 1 | **Icon system** | First-party `<Icon>` + inline Lucide-derived paths, no NuGet (see implementation in `RequiemNexus.Web` / [UI_UX_FACELIFT_REVIEW.md](./UI_UX_FACELIFT_REVIEW.md)). |
| 2 | **Gold accent** | **In brand scope** for achievements and special states (e.g. Discipline mastery, Elder). **`--color-gold` / `--color-gold-dim` stay `EXPERIMENTAL` in CSS until WCAG AA is verified** on target surfaces (Track 6.2 / review Q10). |
| 3 | **Avatar system** | **Roadmap: user-uploaded artwork** per character. **Near-term UI:** initials (or placeholder) until upload/storage pipeline exists. |
| 4 | **Tab grouping (CORE / KINDRED / CHRONICLE)** | **Open** — defer until the character sheet facelift is far enough along to evaluate look-and-feel; no commitment to grouping yet. |
| 5 | **Animation / performance** | **Yes — add a user-facing performance mode** that reduces motion and related heavy visuals globally, in addition to `prefers-reduced-motion` (Track 6.4). |

---

## Non-Goals

The following are explicitly out of scope for this facelift:

- Changing the overall dark gothic aesthetic or color palette identity
- Switching UI framework (staying on Bootstrap 5 grid + custom CSS)
- Adding a new character creation or campaign workflow
- Redesigning the data model or application logic
- Light mode support

---

*"The blood is the life… but clarity is the power."*
