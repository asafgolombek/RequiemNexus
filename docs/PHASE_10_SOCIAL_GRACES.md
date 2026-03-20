# Phase 10 Plan: The Social Graces (Social Maneuvering)

**Status:** Complete for book-aligned core + Nexus extensions **10.5–10.6** (clues + conditions). **Interception** remains deferred per §8 / `mission.md`.  
**Goal:** Automate the formal **Social maneuvering** system from *Vampire: The Requiem 2nd Edition* (Doors, impressions, forcing Doors, hard leverage), with optional **Nexus extensions** for investigation clues and real-time play. See also [mission.md](./mission.md) Phase 10.

---

## 0. Rules citation and scope

### Canonical rules (VtR 2e)

- **Source:** *Vampire: The Requiem* 2nd Edition, **Chapter Four: Rules of the Night** — **Social Maneuvering** (PDF pp. 173–176 in the repo rulebook).
- **Design rule:** Domain tests and engine math should match this section unless explicitly labeled a **house rule** or **Nexus extension**.

### Nexus policy: initiator and target (book-aligned)

VtR 2e positions **Social maneuvering** as **player characters** influencing **Storyteller-controlled** characters. **Requiem Nexus adopts that strictly:**

- **Allowed:** A **player’s character** (initiator) maneuvers against an **NPC / ST-controlled character** (target).
- **Not allowed:** **Player vs player** — another PC cannot be the maneuver target. PvP social outcomes stay **freeform roleplay** only; the Doors subsystem does not apply.

**API, validation, and UI** must reject a non-NPC target (no chronicle flag to override). ST-created maneuvers still use a **PC initiator** when the story is “this PC is maneuvering,” never PC-vs-PC through this feature.

### Scope

- **In scope:** Doors, impressions and roll intervals, opening Doors (including exceptional success and cumulative failure), failure states, forcing Doors, hard leverage, ST narrative overrides (doors/impression), persistence per campaign, SignalR notifications, integration with server-side rolls (`IDiceService` / pool resolution), **enforcement of NPC-only targets**.
- **Non-goals (Phase 10):** **Player-vs-player** Social maneuvering (Doors on PCs); automating freeform dialogue; ST fiat on reasonableness of goals; full **Interception** of maneuvers (see §8 — align with [mission.md](./mission.md) in a later slice).

### Nexus extensions (not in the Social maneuvering section of VtR 2e)

- **Clue pipeline:** Tracking investigation outcomes as spendable **clues** tied to a maneuver is a **product extension**. If implemented, document default thresholds (e.g. successes per clue) as **chronicle or ST configuration**, not as core book text.
- **Conditions:** *Inspired* / *Shaken* / *Swooned* from maneuver outcomes — tie to book + [mission.md](./mission.md); some triggers are ST judgment calls.

### Cross-links

- Layering and hub discipline: [Architecture.md](./Architecture.md), [agents.md](../agents.md) (Masquerade sequence for mutations).
- Roadmap checklist: [mission.md](./mission.md) Phase 10 (Doors, impression, leverage UI, investigation support, interception, social Conditions).

---

## 1. Core objectives

Phase 10 turns long-form social pressure into trackable state: **Doors** (resistance), **impression** (how often you may roll to open a Door), **forcing Doors** (high risk), and optional **clues** for ST-approved shortcuts.

### Key pillars

1. **Doors tracker:** Live view of remaining Doors and modifiers the ST has applied.
2. **Impression management:** Enforce minimum time between “open a Door” attempts per the book’s impression table (server time: `DateTimeOffset`, prefer UTC).
3. **Leverage (book):** Impression shifts via roleplay, rolls, Vice/Dirge, accepted gifts/Merits; **hard leverage** only when forcing Doors (with breaking-point logic).
4. **Forced entry:** Force Doors with a penalty equal to closed Doors; failure burns the relationship for future maneuvering against that victim.
5. **Optional clues extension:** ST-visible feed of discovered clues and spends (if Phase 10.5 ships).

---

## 2. Data architecture (The Grimoire)

### Authorization (Masquerade)

Every **mutating** use case must follow the four-step sequence in `AuthorizationHelper` (extract user → load entity → **verify ownership** → mutate). For social maneuvers, define explicitly (and reflect in services):

- Who may **create** a maneuver (e.g. initiating player, or ST on behalf of a PC).
- Who may **edit** doors/impression, **record rolls**, **force Doors**, **spend clues** (typically ST + initiator within campaign boundaries).
- **Visibility:** Which users may **read** maneuver state (initiator, ST, whole table — product decision).

`Web` remains a thin client; **no** authorization decisions in Blazor beyond routing; **Application** owns checks.

### New domain models (`RequiemNexus.Domain`)

**`ImpressionLevel` (enum)** — align with **VtR 2e** impression table (five values). **Minimum interval between rolls** to open a Door:

| Value     | Time per roll (book) |
|-----------|----------------------|
| `Perfect` | 1 turn               |
| `Excellent` | 1 hour             |
| `Good`    | 1 day                |
| `Average` | 1 week               |
| `Hostile` | Cannot roll          |

**`ManeuverStatus` (enum):** e.g. `Active`, `Succeeded`, `Failed`, `Burnt` (victim will not be maneuvered again by this initiator per forcing failure, or maneuver ended by book failure conditions).

**`LeverageType` (enum)** — use for **Nexus/clue UX** if needed; book **hard leverage** is mechanical (breaking point + remove Doors before force roll), not necessarily this enum alone.

### New persistence models (`RequiemNexus.Data`)

**`SocialManeuver`**

- `Id` (Guid)
- `CampaignId` (int, FK — chronicle in product language; matches existing schema naming)
- `InitiatorCharacterId` (Guid or int per existing `Character` key)
- `TargetChronicleNpcId` — FK to **`ChronicleNpc`** (campaign NPC roster). **No `Character` row as victim** in v1 — avoids conflating PCs with NPCs; initiator remains `Character` in the same `CampaignId`.
- `GoalDescription` (string)
- `InitialDoors` / `RemainingDoors` (int)
- `CurrentImpression` (`ImpressionLevel`)
- `Status` (`ManeuverStatus`)
- `LastRollAt` (`DateTimeOffset?`) — last attempt to open a Door (or last interval boundary as designed)
- `CumulativePenalty` (int, optional) — book: cumulative −1 on further rolls with same victim on failure
- `HostileSince` (`DateTimeOffset?`, optional) — if impression is Hostile, track one-week failure condition
- Vinculum hint (optional): stage or boolean for “impression treated as higher” vs thrall — see §3

**`ManeuverClue`** (optional / Phase 10.5)

- `Id`, `SocialManeuverId`, `SourceDescription`, `IsSpent`, `Benefit` (ST-facing text)

**`ManeuverDoor` (optional)**

- **Not required for v1** if `RemainingDoors` + event log suffices. Add only if you need **per-door audit history** (which Door opened when, by what roll). If omitted, **Phase 10.1** migrates `SocialManeuver` + `ManeuverClue` only.

**Persistence vs SignalR**

- **PostgreSQL / EF** is the source of truth for maneuvers and clues.
- **SignalR** broadcasts changes to subscribed clients; session ephemera stays in Redis per existing hub rules.

---

## 3. Logical engine (Application + Domain)

Use a single public orchestration name in docs and code, e.g. **`SocialManeuveringService`** (Application), with **pure domain helpers** for door math and interval checks where appropriate.

### Initial door count (book)

- **Base:** `min(Resolve, Composure)` of the **victim** (dot values as used elsewhere in Nexus).
- **+2 Doors** if the goal would be a **breaking point** for the victim.
- **+1 Door** if the goal would **prevent** the victim from resolving an **Aspiration**.
- **+1 Door** if acting against the victim’s **Virtue** (or **Mask** for Kindred).

ST may adjust totals for evolving goals (book: reassess if goal changes; opened Doors stay open).

### During play (book)

- **Open one Door:** successful roll opens **one** Door (not one per success).
- **Exceptional success:** opens **two** Doors.
- **Failure:** cumulative **−1** to all further rolls against this victim in this maneuver; ST **may** worsen impression by one (player takes a **Beat**). If impression becomes **Hostile** and remains so for **one week**, maneuver **fails**.
- **Dramatic failure** on opening a Door: can end trust per ST/book; player takes a Beat.
- **Aspiration known:** if persuader knows victim’s Aspiration, presents a clear path, and follows through — **opens one Door** without a roll when that condition is met; if opportunity appears and persuader does not help, **two Doors close**.

### Roll interval enforcement

After a roll to open a Door, compute **next eligible** time from `CurrentImpression` using the table in §2. Integrate with server-side rolling: **`IDiceService`** (not “DiceNexus”) and existing pool resolution when recording an “open Door” attempt.

### Forcing doors (book)

- Declare goal and approach; roll as for opening a Door with a penalty equal to **current number of closed Doors**.
- **Success:** all remaining Doors open.
- **Failure:** victim will not trust the manipulator again for Social maneuvering — model as **`Burnt`** or equivalent for that initiator–target pair.

### Hard leverage (book)

- Only when **forcing Doors**.
- Inflicts a **breaking point** on the **persuader**; ST sets severity. Compare breaking-point level to persuader’s **Humanity**: if **difference ≤ 2**, remove **one** Door before the roll; if **≥ 3**, remove **two**. Then apply the force-Door roll with remaining Doors as penalty.

### Blood bond (book cross-reference)

Against a **thrall**, regnant’s impression is treated as **one, two, or perfect** steps higher by **Vinculum** stage, with dice bonuses on Social/Discipline — engine should not double-apply if those bonuses are already in pool resolution; document single source of truth.

### Investigation / clues (`Investigation` extension)

- If implemented: track extended or pooled Investigation outcomes; **N** successes → `ManeuverClue` ( **N** is ST/chronicle config ).
- **Clue spend:** only as ST-approved mechanical benefit (house rule); do not claim book parity unless tied to a cited optional rule.

---

## 4. Real-time integration (SignalR)

- **`ReceiveSocialManeuverUpdate`:** `SocialManeuverUpdateDto` broadcast to the chronicle SignalR group after create, open/force rolls, impression change, narrative doors, and hostile-week auto-failure. Client: `SessionClientService.SocialManeuverUpdated`.
- **`ImpressionShift` notification:** covered by the same payload when ST sets impression (no separate hub method).
- Hub stays a **thin relay**; business logic and auth in Application services.

---

## 5. UI/UX design

### Storyteller (Glimpse)

- **New maneuver:** Initiator (PC), target (**NPC / ST-only — never another player’s PC**), goal text.
- **Doors:** Manual ± for narrative changes; show **computed** initial Doors from stats + modifiers where data exists.
- **Impression:** Dropdown matching the **five** book levels.
- **Clue / leverage feed** (if Phase 10.5): log discoveries and spends.

### Player (character sheet)

- **Active maneuvers:** Doors visualization; **countdown** to next allowed roll from `LastRollAt` + impression interval.
- **Force doors:** Exposes the high-risk action with clear copy (burn on failure).
- **Clues tray** (optional): only if clue extension ships.

---

## 6. Testing and validation

1. **Domain / unit tests (book-aligned):**
   - Initial Doors: `min(Resolve, Composure)` + modifiers.
   - Intervals for each `ImpressionLevel`.
   - Open Door: success / exceptional / cumulative failure; Hostile + one week → fail.
   - Force Doors: penalty = closed Doors; hard leverage door removal vs Humanity gap.
2. **Integration tests:** Maneuvers persist across restarts; Masquerade on create/update; **create fails** when target is a player-owned PC.
3. **SignalR:** Broadcast after state change (mock or harness).
4. **E2E (optional in Phase 10):** Full Playwright path may wait for [mission.md](./mission.md) Phase 13; if run earlier, scope to ST creates maneuver → roll → door opens.

---

## 7. Phased implementation (sub-tasks)

- **Phase 10.1 (Data):** EF migration for `SocialManeuver` (+ `ManeuverClue` if clues ship with core). Omit `ManeuverDoor` unless audit requirement is confirmed. **Implemented:** `SocialManeuver` targets `ChronicleNpc` via `TargetChronicleNpcId` (initiator: `Character`); migration `Phase10SocialManeuvering`.
- **Phase 10.2 (Engine):** **`SocialManeuveringEngine`** (Domain) + **`SocialManeuveringService`** (Application) — initial Doors, impression intervals, open-Door rolls (cumulative failure dice, exceptional opens two), force + hard leverage, hostile-week auto-failure on mutation load. Aspiration “free Door” / dramatic-trust end remain ST/UI judgment (manual door edits).
- **Phase 10.3 (UI — ST):** Glimpse maneuver management. **Delivered:** `StorytellerGlimpse` Social Maneuvering card (create, list, impression, narrative doors, open/force rolls, `rnConfirm` for force).
- **Phase 10.4 (UI — player):** Character sheet widget + countdown. **Delivered:** `SocialManeuversSection` on character sheet; next open-Door timing via `SocialManeuveringEngine` + 1s UI refresh; rolls + hard leverage + confirm.
- **Phase 10.5 (Investigation extension):** **Delivered** — `Campaign.SocialManeuverInvestigationSuccessesPerClue` (default 3), `BankInvestigationSuccessesAsync`, ST `AddManeuverClueAsync`, `SpendManeuverClueAsync`; Glimpse + character sheet UI.
- **Phase 10.6 (Conditions):** **Delivered** — auto-apply via `SocialManeuveringService` + `IConditionService` (see `docs/rules-interpretations.md`).

---

## 8. Mission.md alignment (explicit)

| mission.md item | Plan |
|-----------------|------|
| Doors tracker | §1–2, §5 |
| Impression management | §2–3 |
| Leverage UI | §5; hard leverage §3 |
| Investigation / clues | §3, §7 Phase 10.5 (extension) |
| **Interception** of maneuvers | **Not specified in VtR 2e Social maneuvering excerpt** — defer to follow-up slice; add requirements when defined |
| Social Conditions (Inspired / Shaken / Swooned) | §7 Phase 10.6 |

---

> *"Every secret has a weight. Every lock has a key. Every soul has a price."*
