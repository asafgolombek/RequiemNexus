# 🎭 Phase 10 Plan: The Social Graces (Social Maneuvering)

**Status:** Draft / Planned  
**Goal:** Automate the formal systems of social dominance, investigation, and manipulation as defined in *Vampire: The Requiem 2nd Edition*.

---

## 📜 1. Core Objectives

Phase 10 transforms the "Social" section of the character sheet from a collection of dots into a dynamic, reactive subsystem. It focuses on the **Doors** mechanic for long-term persuasion and the **Investigation** mechanic for uncovering secrets.

### Key Pillars
1. **The Doors Tracker:** Real-time visualization of a target's social resistance.
2. **Impression Management:** Automated roll intervals (from *Suspicious* to *Perfect*).
3. **Leverage & Clues:** Linking investigation outcomes to social shortcuts.
4. **The "Forced Entry":** High-risk mechanics for immediate resolution.

---

## 🏗️ 2. Data Architecture (The Grimoire)

### New Domain Models (`RequiemNexus.Domain`)
- **`ImpressionLevel` (Enum):**
  - `Hostile` (-2, No rolls)
  - `Suspicious` (-1, One roll/week)
  - `Average` (0, One roll/day)
  - `Good` (+1, One roll/hour)
  - `Excellent` (+2, One roll/minute)
  - `Perfect` (+3, One roll/turn)
- **`ManeuverStatus` (Enum):** `Active`, `Succeeded`, `Failed`, `Burnt` (bridge destroyed).
- **`LeverageType` (Enum):** `Soft` (Improves Impression), `Hard` (Removes Doors).

### New Persistence Models (`RequiemNexus.Data`)
- **`SocialManeuver`:**
    - `Id` (Guid)
    - `ChronicleId` (Guid)
    - `InitiatorId` (Guid/CharacterId)
    - `TargetId` (Guid/NpcId or CharacterId)
    - `GoalDescription` (string)
    - `InitialDoors` (int)
    - `RemainingDoors` (int)
    - `CurrentImpression` (ImpressionLevel)
    - `Status` (ManeuverStatus)
    - `LastRollAt` (DateTimeOffset?)
    - `ManeuverType` (Investigation vs. Persuasion)
- **`ManeuverClue`:**
    - `Id` (Guid)
    - `ManeuverId` (Guid)
    - `SourceDescription` (string)
    - `IsSpent` (bool)
    - `Benefit` (string) — e.g., "Provides Soft Leverage: Target's Vice discovered."

---

## ⚙️ 3. Logical Engine (Application Layer)

### `SocialManeuveringEngine`
- **Initial Door Calculation:** `Max(Composure, Resolve)` + Modifiers (+1 for Aspiration conflict, +2 for Breaking Point risk, -1 for Aspiration alignment).
- **Interval Enforcement:** Logic to prevent "roll-spamming." The `DiceNexus` will check the `CurrentImpression` against `LastRollAt`.
- **Door Opening:** 
  - `Success`: `RemainingDoors--`
  - `Exceptional Success`: `RemainingDoors -= 2`
  - `Failure`: Apply cumulative -1 penalty to the maneuver; set "Next Roll" timestamp based on interval.

### `InvestigationService`
- **Clue Discovery:** Tracks successes on Investigation rolls. Every $N$ successes generates a `ManeuverClue`.
- **Clue Application:** Logic to convert a Clue into a "Leverage" action (e.g., "Spend Clue to drop a Door").

---

## 📡 4. Real-Time Integration (SignalR)

- **`BroadcastManeuverUpdateAsync`:** Pushes state changes (Doors remaining, Impression shifts) to all participants in the session.
- **`ImpressionShift` Notification:** Alerts the player when the Storyteller adjusts the Impression level due to narrative roleplay.

---

## 🎨 5. UI/UX Design

### Storyteller View (Glimpse Dashboard)
- **"New Maneuver" Modal:** Select Target (NPC/PC), select Initiator, set Goal.
- **Doors Control:** Manual +/- buttons for Doors (to reflect narrative shifts).
- **Impression Selector:** Dropdown to change the pacing.
- **Leverage Feed:** Log of Clues discovered and how they were spent.

### Player View (Character Sheet)
- **"Active Maneuvers" Widget:**
    - Visual representation of **Doors** (Iconography: Padlocks that unlock/open).
    - **Countdown Timer:** "Next roll available in: 4h 22m" (based on Impression).
    - **The "Force Doors" Button:** Only active when doors remain; triggers the high-risk penalty roll.
- **Leverage/Clues Tray:** A sidebar showing "Secrets known about [Target]" with buttons to apply them as Leverage.

---

## 🧪 6. Testing & Validation

1. **Unit Tests:**
    - Verify interval logic for all `ImpressionLevel` values.
    - Verify Door calculation for various character stat combinations.
    - Verify "Forcing the Door" penalty math.
2. **Integration Tests:**
    - Ensure `SocialManeuver` records persist across session restarts.
    - Verify SignalR broadcasts trigger UI updates for both ST and Player.
3. **E2E Playwright:**
    - Path: Create Maneuver -> Roll Success -> Open Door -> Force Remaining -> Success -> Close Maneuver.

---

## 📅 7. Phased Implementation (Sub-tasks)

- **Phase 10.1 (Data):** DB Migrations for `SocialManeuver`, `ManeuverClue`, and `ManeuverDoor`.
- **Phase 10.2 (Engine):** Core `SocialManeuveringService` with interval and door logic.
- **Phase 10.3 (UI - ST):** Maneuver management in Storyteller Glimpse.
- **Phase 10.4 (UI - Player):** Real-time Doors tracker on character sheet.
- **Phase 10.5 (Investigation):** Clue tracking and Leverage spending logic.
- **Phase 10.6 (Refinement):** Automatic Condition application (Inspired/Shaken).

---

> _"Every secret has a weight. Every lock has a key. Every soul has a price."_
