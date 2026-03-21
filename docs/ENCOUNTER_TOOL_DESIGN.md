# ⚔️ Encounter Tool — Full Feature Design

> Phase design document for the combat encounter system improvements.
> Covers all features across ST tooling, player UX, and real-time signaling.

---

## 📋 Feature Index

| # | Feature | Priority | Complexity |
|---|---------|----------|------------|
| 1 | [Encounter Builder (Pre-Session Prep)](#1-encounter-builder) | High | Medium |
| 2 | [Smart Encounter Launch (Online Player Selection + Auto-Roll)](#2-smart-encounter-launch) | High | Low |
| 3 | [Player-Facing Initiative Tracker UI](#3-player-facing-initiative-tracker-ui) | High | Low |
| 4 | [Condition Notifications (Toast Push)](#4-condition-notifications) | Medium | Low |
| 5 | [Hidden Perception Roll (ST-Only)](#5-hidden-perception-roll) | Medium | Low |
| 6 | [Round Counter](#6-round-counter) | High | Low |
| 7 | [NPC Health Tracking (ST-Only)](#7-npc-health-tracking) | Medium | Medium |
| 8 | [Hold / Delay Action](#8-hold--delay-action) | Medium | Medium |
| 9 | [NPC Name Reveal Toggle](#9-npc-name-reveal-toggle) | Low | Low |
| 10 | [Encounter Templates](#10-encounter-templates) | Low | Medium |

---

## 1. Encounter Builder

### Goal
Allow the ST to fully configure an encounter **before the session starts**. When play begins, launch with one click — no live data entry under pressure.

### UX Flow
1. ST navigates to `/campaigns/{id}/encounters` → clicks **"New Encounter"**.
2. A builder form opens (can be a new page or a modal):
   - **Name** — free text (e.g., "Elysium Ambush").
   - **NPC Roster** — add one or more NPCs by name. For each NPC:
     - Name
     - Initiative Modifier (manual — ST knows their stats)
     - Health track size (number of boxes, default 7)
     - Optional notes (hidden from players, for ST reference)
   - **Save as Draft** — saves without activating. Status = `Draft`.
3. The encounters list shows both `Draft` and `Active` encounters.
4. When session begins, ST clicks **"Launch Encounter"** on a draft → it becomes `Active` and triggers the [Smart Launch flow](#2-smart-encounter-launch).

### Data Model Changes

**`CombatEncounter`** — add:
```csharp
public bool IsDraft { get; set; } = false;
public ICollection<EncounterNpcTemplate> NpcTemplates { get; set; } = [];
```

**New model — `EncounterNpcTemplate`**:
```csharp
public class EncounterNpcTemplate
{
    public int Id { get; set; }
    public int EncounterId { get; set; }
    public CombatEncounter? Encounter { get; set; }
    public string Name { get; set; } = string.Empty;
    public int InitiativeMod { get; set; }
    public int HealthBoxes { get; set; } = 7;
    public string? Notes { get; set; }         // ST-only
    public bool IsRevealed { get; set; } = true; // ties into feature #9
}
```

### New Service Methods (`IEncounterService`)
```csharp
Task<CombatEncounter> CreateDraftEncounterAsync(int campaignId, string name, string storyTellerUserId);
Task AddNpcTemplateAsync(int encounterId, string name, int initiativeMod, int healthBoxes, string? notes, string storyTellerUserId);
Task RemoveNpcTemplateAsync(int templateId, string storyTellerUserId);
Task LaunchEncounterAsync(int encounterId, string storyTellerUserId); // Draft → Active, triggers smart launch
```

`LaunchEncounterAsync` sets `IsDraft = false`, `IsActive = true`, and auto-adds all `NpcTemplates` as `InitiativeEntry` rows (rolling 1d10 for each).

---

## 2. Smart Encounter Launch

### Goal
When an encounter is launched (or already active and empty), the ST picks which online players are in the scene. The system auto-rolls initiative for them.

### Initiative Formula
```
InitiativeMod = Wits + Composure   (from character attributes)
RollResult    = Random.Shared.Next(1, 11)   // 1d10, server-side
Total         = InitiativeMod + RollResult
```

### UX Flow
1. After clicking **"Launch Encounter"** (or an explicit **"Add Online Players"** button on an active encounter), a panel appears **above the tracker**.
2. The panel fetches `GetSessionStateAsync(campaignId)` and lists all `PlayerPresenceDto` where:
   - `IsOnline == true`
   - `CharacterId != null`
   - Character is **not already** in the encounter
3. Each row shows: character name, player name, checkbox (default: **checked**).
4. ST unchecks players who are not in the scene (observers, off-screen characters).
5. **"Roll Initiative & Join"** button → calls `BulkAddOnlinePlayersAsync` → SignalR broadcast.

### New Service Method (`IEncounterService`)
```csharp
Task BulkAddOnlinePlayersAsync(
    int encounterId,
    IEnumerable<int> characterIds,
    string storyTellerUserId);
```

**Implementation:**
```csharp
// Pseudocode
foreach (int charId in characterIds)
{
    Character character = await _characterRepo.GetByIdAsync(charId);
    int mod = character.GetAttributeRating(AttributeId.Wits)
            + character.GetAttributeRating(AttributeId.Composure);
    int roll = Random.Shared.Next(1, 11);
    await AddCharacterToEncounterAsync(encounterId, charId, mod, roll, storyTellerUserId);
}
```

### Edge Cases
- Player goes offline between panel load and confirmation → skip silently, log a warning.
- Player character already in encounter → exclude from list.
- Character has no Wits/Composure attributes set → default each missing attribute to `1`.

### UI Notes
- The panel is **inline** (no modal), shown directly above the tracker list.
- Dismissed by clicking "Cancel" or after a successful roll.
- ST can still add individual players via the existing manual "Add Participant" form.

---

## 3. Player-Facing Initiative Tracker UI

### Goal
Players see a clean, read-only real-time tracker in their browser. ST sees the full control panel; players see only the scene state.

### Player View — Layout
```
┌────────────────────────────────────────────┐
│  ⚔️  Elysium Ambush          Round 2       │
│──────────────────────────────────────────  │
│  ▶  1. Marcus Vane          Init: 14  [██░]│  ← current actor, mini health
│     2. Elena Dusk           Init: 11  [███]│
│     3. Unknown Assailant    Init:  9       │  ← NPC, name hidden if not revealed
│     4. Petra Voss           Init:  7  [██░]│
│──────────────────────────────────────────  │
│  ⏳ Marcus Vane's turn                      │
└────────────────────────────────────────────┘
```

### Rules for Player View
- **Own character**: shows full health track (bashing/lethal/aggravated colors).
- **Other PCs**: shows name and initiative total only. No health track.
- **NPCs**: show name (or "Unknown Assailant" if `IsRevealed == false`). No health track, no stats.
- **Current actor** highlighted with `▶` and a distinct background color.
- **Acted actors** shown with reduced opacity (greyed out).
- **Round counter** shown in header (see [Feature 6](#6-round-counter)).
- No ST controls (no Add, Remove, Advance, Resolve buttons).

### Component Split
The current `InitiativeTracker.razor` handles both views. Split into:
- `InitiativeTracker.razor` — existing, retains all ST controls.
- `InitiativeTrackerPlayerView.razor` — lean read-only component, embedded in `InitiativeTracker.razor` when `!_isSt`.

Or use a conditional render block within the existing file — prefer this to avoid duplication.

---

## 4. Condition Notifications

### Goal
When the ST applies a Condition or Tilt to a player character, that player sees an in-browser toast notification without needing to check their character sheet.

### Trigger Points
- `ConditionService.ApplyConditionAsync(...)` — push condition name.
- `ConditionService.ApplyTiltAsync(...)` — push tilt name.
- `ConditionService.RemoveConditionAsync(...)` — push "Condition cleared" (optional, lower priority).

### SignalR Event
Add a new hub event to `ISessionHubClient` / `SessionClient`:

```csharp
// New event method
event Action<ConditionNotificationDto>? ConditionApplied;

// DTO
public record ConditionNotificationDto(
    int CharacterId,
    string ConditionName,
    bool IsTilt,
    bool IsRemoval);
```

Broadcasting in `ConditionService`:
```csharp
await _sessionHub.Clients
    .User(targetUserId)
    .SendAsync("ConditionApplied", new ConditionNotificationDto(...));
```

### Toast UI
A `<ConditionToast>` Blazor component subscribes to `SessionClient.ConditionApplied`:
- Only renders toasts for the **current user's character**.
- Appears bottom-right, auto-dismisses after 5 seconds.
- Styling: crimson border, gothic font, condition name + icon (e.g., ⚠️ Blinded, ☠️ Poisoned).
- Placed in `MainLayout.razor` so it works across all pages.

---

## 5. Hidden Perception Roll

### Goal
ST can trigger a Perception roll (Wits + Composure, or Wits + Awareness) for a player character from the campaign page. The result is **never shown to the player** — it's purely an ST tool.

### UX
- On the character card in the Campaign page, ST sees a small **"Roll Perception"** button (players do not see it).
- Clicking it opens a minimal inline picker:
  - Pool: `Wits + Composure` (default) or `Wits + Awareness` (toggle).
  - Optional penalty dice (darkness, distraction).
  - **"Roll"** button.
- Result appears in an ST-only result panel on the same card. **No SignalR broadcast. No character sheet update.**

### Implementation

New service method:
```csharp
// Returns dice results — no persistence, no broadcast
Task<DiceRollResultDto> RollPerceptionAsync(
    int characterId,
    bool useAwareness,
    int penaltyDice,
    string storyTellerUserId);
```

Uses the existing `PoolResolver` / dice engine. Returns successes + individual die faces.

### Security
- `storyTellerUserId` is verified via `AuthorizationHelper.RequireStorytellerAsync()`.
- No event is emitted that could be intercepted by the player's SignalR connection.
- The result DTO is returned only to the HTTP response for the ST's session.

---

## 6. Round Counter

### Goal
Track which combat round the encounter is on. Surface it in the header for both ST and players.

### Data Model Change
**`CombatEncounter`** — add:
```csharp
public int CurrentRound { get; set; } = 1;
```

### Logic
In `EncounterService.AdvanceTurnAsync`:
- When all participants have `HasActed == true` and the round resets → increment `CurrentRound`.
- Broadcast updated round in `InitiativeEntryDto` or a separate `EncounterStateDto`.

### Display
- ST view: `Round @_encounter.CurrentRound` in the encounter header.
- Player view: same, shown prominently at top of the tracker.
- The "New round!" feedback message is already in the razor; replace it with the round number.

---

## 7. NPC Health Tracking (ST-Only)

### Goal
ST can track damage on NPCs during combat without leaving the initiative tracker.

### Data Model Change
**`InitiativeEntry`** — add:
```csharp
public int NpcHealthBoxes { get; set; } = 7;    // set from EncounterNpcTemplate or manual
public string NpcHealthDamage { get; set; } = string.Empty;  // same format as Character.HealthDamage: '/', 'X', '*'
```

### UX (ST Only, NPCs Only)
Each NPC row in the tracker shows:
```
[NPC Name]  [████░░░]   +Bashing  +Lethal  +Agg   ×Remove
```
- Clicking a damage button appends the appropriate character to `NpcHealthDamage`.
- The mini health track renders identically to the PC health track using the same `GetDamageClass()` helper.
- Players **do not** see NPC health tracks.

### New Service Method (`IEncounterService`)
```csharp
Task ApplyNpcDamageAsync(int entryId, char damageType, string storyTellerUserId);
Task HealNpcDamageAsync(int entryId, string storyTellerUserId); // removes last damage box
```

---

## 8. Hold / Delay Action

### Goal
A combatant can choose to hold their action and act later in the round (or at the start of the next round). Common tactical choice in VtR.

### Data Model Change
**`InitiativeEntry`** — add:
```csharp
public bool IsHeld { get; set; } = false;
```

### UX (ST Only)
On the current actor's row:
- **"Hold Action"** button appears alongside "Advance Turn".
- Clicking it sets `IsHeld = true` on the current entry and advances to the next actor.
- Held combatants appear in the list with a `[HELD]` badge, after all non-held non-acted entries.
- ST can **"Release"** a held combatant at any point → they become the current actor immediately (inserted at the top of remaining unacted entries).
- At round reset, held entries are treated as having acted (their hold expires).

### Sort Order Change
`RecalculateOrderAsync` sort priority:
1. `HasActed == false && IsHeld == false` — active queue, sorted by `Total` desc.
2. `HasActed == false && IsHeld == true` — held queue, sorted by `Total` desc (after active).
3. `HasActed == true` — acted, sorted by `Total` desc (at the bottom, greyed out).

### New Service Method (`IEncounterService`)
```csharp
Task HoldActionAsync(int encounterId, int entryId, string storyTellerUserId);
Task ReleaseHeldActionAsync(int encounterId, int entryId, string storyTellerUserId);
```

---

## 9. NPC Name Reveal Toggle

### Goal
ST controls whether players see an NPC's true name or "Unknown". Useful for ambushes, disguised enemies, and horror reveals.

### Data Model
`EncounterNpcTemplate.IsRevealed` (added in Feature 1).
Also add to `InitiativeEntry`:
```csharp
public bool IsRevealed { get; set; } = true;
```

Defaults to `true` for manually-added NPCs. Defaults from `EncounterNpcTemplate.IsRevealed` for template-spawned NPCs.

### UX (ST Only)
Each NPC row in ST view shows a **👁 / 🙈 toggle** button next to the name.
- `IsRevealed = true` → players see actual name.
- `IsRevealed = false` → players see `"Unknown"`.

The `InitiativeEntryDto` broadcast includes a `DisplayName` field:
```csharp
public record InitiativeEntryDto(
    int? CharacterId,
    string Name,          // actual name (ST only)
    string DisplayName,   // name or "Unknown" based on IsRevealed
    int InitiativeValue,
    bool IsActiveTurn,
    bool IsNpc,
    bool IsRevealed);
```

---

## 10. Encounter Templates

### Goal
Save a configured encounter (name + NPC roster) as a reusable template for future sessions. Eliminates repeated setup for recurring enemy types.

### Data Model — New Table `EncounterTemplate`
```csharp
public class EncounterTemplate
{
    public int Id { get; set; }
    public int CampaignId { get; set; }
    public Campaign? Campaign { get; set; }
    public string Name { get; set; } = string.Empty;   // e.g., "3 Invictus Guards"
    public ICollection<EncounterTemplateNpc> Npcs { get; set; } = [];
}

public class EncounterTemplateNpc
{
    public int Id { get; set; }
    public int TemplateId { get; set; }
    public EncounterTemplate? Template { get; set; }
    public string Name { get; set; } = string.Empty;
    public int InitiativeMod { get; set; }
    public int HealthBoxes { get; set; } = 7;
    public bool IsRevealedByDefault { get; set; } = true;
}
```

### UX Flow
- In the Encounter Builder (Feature 1), ST sees a **"Save as Template"** button after building a draft.
- On the Encounter list page, a **"From Template"** button opens a picker showing all saved templates.
- Selecting a template pre-populates a new draft encounter with all NPCs from the template.
- ST can edit the draft before launching.

### New Service (`IEncounterTemplateService`)
```csharp
Task<EncounterTemplate> CreateTemplateAsync(int campaignId, string name, string storyTellerUserId);
Task AddNpcToTemplateAsync(int templateId, string name, int initiativeMod, int healthBoxes, string storyTellerUserId);
Task<List<EncounterTemplate>> GetTemplatesAsync(int campaignId, string storyTellerUserId);
Task<CombatEncounter> CreateEncounterFromTemplateAsync(int templateId, string encounterName, string storyTellerUserId);
Task DeleteTemplateAsync(int templateId, string storyTellerUserId);
```

---

## 🗺️ Implementation Order

Build in this sequence to deliver value incrementally:

```
Phase A — Foundation (unblock every session immediately)
  6. Round Counter          — tiny, zero risk, immediate value
  2. Smart Launch           — core request, low complexity

Phase B — Player Experience
  3. Player View UI         — polish the tracker for players
  4. Condition Toasts       — reactive, uses existing SignalR

Phase C — ST Power Tools
  5. Hidden Perception Roll — self-contained, no state changes
  7. NPC Health Tracking    — requires data model migration
  8. Hold / Delay Action    — requires sort order changes

Phase D — Prep Tooling
  1. Encounter Builder      — new page, new model
  9. NPC Name Toggle        — extends builder
 10. Encounter Templates    — extends builder further
```

---

## 🔒 Security Notes

- All ST mutations go through `AuthorizationHelper.RequireStorytellerAsync()` — no exceptions.
- Hidden Perception Roll result: **never emitted via SignalR**, returned only to the HTTP response context of the requesting ST.
- NPC health data: **excluded from `InitiativeEntryDto`** so it is never sent to player SignalR connections.
- Player view renders from the broadcast DTO only — never from direct database access on the client.

---

## 📐 SignalR Events Summary

| Event | Direction | Consumers |
|-------|-----------|-----------|
| `InitiativeUpdated` | Server → All clients | Existing — tracker sync |
| `ConditionApplied` | Server → Target user | New — toast notification |
| `EncounterLaunched` | Server → All clients | New — auto-navigate players to tracker |

> `EncounterLaunched` is optional but enables the "players auto-join" experience from Feature 2 — clients listening to this event can be redirected or prompted to open the tracker.
