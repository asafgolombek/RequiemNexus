# 🍷 Requiem Nexus: UI/UX Improvement Plan

This document outlines the strategic roadmap for elevating the user experience of Requiem Nexus. In alignment with the **Antigravity Philosophy**, every improvement must reduce cognitive weight and enhance the "Modern Gothic" immersion.

---

## 📋 Priority Matrix

| Priority | Phase | Focus | Objective |
| :--- | :--- | :--- | :--- |
| **P0** | [Phase A: Architectural Foundation](#phase-a-architectural-foundation) | Stability & Cleanliness | Decouple styles and enforce design tokens. |
| **P1** | [Phase B: Tactical Character Sheet](#phase-b-tactical-character-sheet) | Player Immersion | Make the character sheet feel like a physical artifact. |
| **P1** | [Phase C: Real-time Awareness](#phase-c-real-time-awareness) | Session Flow | Ensure players notice updates in dynamic play. |
| **P2** | [Phase D: Storyteller Mastery](#phase-d-storyteller-mastery) | ST Efficiency | Reduce the "Danse Macabre" administrative friction. |
| **P3** | [Phase E: Navigation & Utility](#phase-e-navigation--utility) | Accessibility | Expand the Command Palette and ARIA support. |

---

## 🏗️ Phase A: Architectural Foundation (P0)
*Objective: Eliminate technical debt in the presentation layer and ensure visual consistency.*

1.  **CSS Decoupling (Antigravity Enforcement)**
    *   **Action:** Audit `CharacterDetails.razor` and `StorytellerGlimpse.razor` for inline styles.
    *   **Implementation:** Move all inline CSS into scoped `.razor.css` files.
    *   **Why:** Align with the "Explicit Engineering" pillar. Logic and presentation should not be conflated.
2.  **Design Token Audit**
    *   **Action:** Replace hardcoded hex codes with CSS variables from `design-tokens.css` (e.g., `#8B0000` → `var(--crimson)`).
    *   **Why:** Ensures that future theme adjustments (e.g., a "high-contrast" mode) can be applied globally.
3.  **Standardized Component Transitions**
    *   **Action:** Apply `var(--transition-fast)` to all hover and active states across buttons and inputs.
    *   **Why:** Create a cohesive, "snappy" feel across the entire SPA.

---

## 🧛 Phase B: Tactical Character Sheet (P1)
*Objective: Transform the character sheet from a data form into a tactile interface.*

1.  **Interactive Vitae Drop**
    *   **Action:** Refactor `CharacterVitals.razor` to allow direct interaction with the `vitae-drop` SVG.
    *   **Implementation:** Clicking at a specific height in the drop should set `CurrentVitae` to that percentage/level, rather than cycling values one-by-one.
    *   **Why:** Fulfills the "3-Click Rule"—one click to set your blood pool.
2.  **Thematic Health & Wound Tracking**
    *   **Action:** Update health box symbols from `✕` and `☆` to custom gothic icons.
    *   **Implementation:** 
        *   Bashing: `/` (Blunt strike)
        *   Lethal: `X` (Standard cross)
        *   Aggravated: `☩` (Crusader cross or "Double-X")
    *   **Interaction:** Add tooltips to the health track that dynamically describe current mechanical penalties (e.g., "−2 to all actions").
3.  **Enhanced DotScale Feedback**
    *   **Action:** Improve the "bleeding" animation in `DotScale.razor`.
    *   **Implementation:** Add a subtle crimson glow transition when a dot is filled, simulating the "investment of blood" into a trait.

---

## 📡 Phase C: Real-time Awareness (P1)
*Objective: Ensure synchronized state changes are visceral and noticeable.*

1.  **The Crimson Pulse**
    *   **Action:** Create a global CSS animation `@keyframes crimson-pulse`.
    *   **Implementation:** When a SignalR update is received (via `SessionClientService`), trigger this animation on the affected element (Health boxes, Willpower, Beats).
    *   **Why:** Players shouldn't have to scan their screen to see if the ST awarded them a Beat or took away health.
2.  **Roll History Immersion**
    *   **Action:** Refactor `RollHistoryFeed.razor` to emphasize "Critical Successes" and "Dramatic Failures."
    *   **Implementation:** Use `var(--crimson-glow)` for dramatic failures and a subtle "bone-white" flash for criticals.

---

## 🎭 Phase D: Storyteller Mastery (P2)
*Objective: Empower the Storyteller to manage complex scenes with minimal cognitive load.*

1.  **Pinned NPC Cards**
    *   **Action:** Add a "Pin" toggle to NPC stat blocks in `StorytellerGlimpse`.
    *   **Implementation:** Pinned NPCs remain at the top of the Glimpse dashboard, allowing the ST to track combat vitals for the "Boss" while scrolling through minions.
2.  **Drag-and-Drop Beat Awards**
    *   **Action:** Implement a draggable "Beat" icon in the Glimpse header.
    *   **Implementation:** ST can drag this icon onto any character card in the list to trigger `AwardBeat`.
    *   **Why:** Reduces the multi-step process of selecting a character and clicking a button to a single fluid motion.

---

## 🧭 Phase E: Navigation & Utility (P3)
*Objective: Broaden the reach and speed of the ecosystem.*

1.  **Command Palette Expansion**
    *   **Action:** Add "Quick Actions" to `CommandPalette.razor`.
    *   **Implementation:** 
        *   `/roll [pool]` (e.g., `/roll 7`)
        *   `/goto [section]` (e.g., `/goto Devotions`)
    *   **Why:** High-power users can navigate faster than via mouse alone.
2.  **ARIA Live Regions (The Global Embrace)**
    *   **Action:** Ensure SignalR broadcasts of dice results are wrapped in `aria-live="polite"` regions.
    *   **Why:** Fulfills the "Accessibility (a11y)" mandate for Phase 13 early.

---

> *"The blood remembers. The code must too."*
