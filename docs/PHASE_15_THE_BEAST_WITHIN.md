# 🩸 Phase 15: The Beast Within — Frenzy & Torpor

> _"The Beast does not negotiate. It does not wait. It simply is — until the moment it isn't."_

## Overview

**Status:** 🔄 In progress
**Depends on:** Phase 14 (Combat & Wounds) — `DamageSource.Fire` / `DamageSource.Sunlight`, `CharacterHealthService`, `ModifierService` with `WoundPenaltyResolver`
**Unlocks:** Phase 17 (Humanity & Condition Wiring) — `DegenerationCheckRequired` event UI + wound-penalty path already in `ModifierService`

Phase 15 gives the Beast mechanical teeth. Frenzy and Torpor are the two pillars of Vampire: The Requiem 2e horror fiction; without them every character is just a powerful human with fangs. This phase introduces:

- **Frenzy saves** — contested rolls driven by explicit triggers, not ambient polling
- **Willpower spend** as the optional mitigation path
- **Vitae-zero event** wiring (the first in-process Domain event in the project)
- **Torpor state** — entry, awakening, and hunger escalation over time
- **Background interval service** for real-time torpor tracking

---

## 📐 Architectural Decisions

### 1. Frenzy is a contested save, not a toggle

`FrenzyService` (Application) receives a `FrenzyTrigger` enum and executes a `Resolve + Blood Potency` pool via `TraitResolver`. On failure it applies the relevant Tilt (`TiltType.Frenzy` or `TiltType.Rotschreck`) to the character — both enum values already exist in `TiltType.cs`. The tilt application is handled via the existing tilt pipeline from Phase 4, extended with a concurrency guard to prevent duplicate active tilts.

**Why not a toggle?** The book models frenzy as a dice-pool save, not a binary state flip. Automating it as a toggle would short-circuit the player's agency (Willpower spend) and the Storyteller's narrative gate.

### 2. Triggers are explicit, never ambient

The app does not poll character state for frenzy triggers. Triggers are fired by exactly three paths:

| Path | Trigger Type | Who fires it |
|------|--------------|-------------|
| `VitaeService.SpendVitaeAsync` reaches 0 | `Hunger` | System (in-process event) |
| Storyteller clicks "Trigger Frenzy Save" in Glimpse | `Hunger` / `Rage` / `Rotschreck` | ST UI |
| Player clicks "I am exposed to fire / sunlight" | `Rotschreck` | Player UI |

The ST Glimpse trigger allows `Hunger` as a manual option — this covers edge cases where Vitae was spent offscreen or a narrative hunger description needs mechanical resolution. It is distinct from the automated `VitaeDepletedEvent` path and does not re-fire that event. `Starvation` is never available as a manual trigger; it is torpor-interval only.

### 3. Only one Beast tilt at a time

`FrenzyService` enforces a single active Beast tilt per character. Before applying a `Frenzy` or `Rotschreck` tilt, the service checks for **any** active Beast tilt (`TiltType.Frenzy` or `TiltType.Rotschreck`). If one is found, the new save is suppressed and the existing tilt is returned in the result. This prevents:

- A character entering `Frenzy` when `Rotschreck` is already active (and vice versa)
- Duplicate `Frenzy` rows from concurrent `VitaeDepletedEvent` handlers

The unique index on `CharacterTilts` (see T2.2) is a DB-level backstop for the same-tilt case. The application-level Beast-tilt check in `FrenzyService` covers the cross-tilt case. Both layers are required.

**When Vitae hits 0 and the character is already in `Rotschreck`:** The `Hunger` frenzy save is suppressed entirely. The character is already in a Beast state. This decision is recorded in the Rules Interpretation Log.

### 4. VitaeService is introduced in Phase 15

Phase 14 placed Vitae spend logic directly in `CharacterHealthService.TryFastHealBashingWithVitaeAsync`. Phase 15 extracts all Vitae mutation into a dedicated `VitaeService` (Application) that owns every Vitae gain and spend on `Character` entities. Initialization writes in `CharacterManagementService` (character creation) and `EncounterService` (NPC initialization on `InitiativeEntry`) are not Vitae spends and are left as-is.

**Why now?** The Vitae-zero event requires a single authoritative spending path. Without `VitaeService`, frenzy triggers would need to be inserted into every caller that decrements `CurrentVitae` — fragile and error-prone.

**Known call sites to migrate (see T3.0):**
- `CharacterHealthService.cs` — inline `CurrentVitae -= vitaeSpent` in `TryFastHealBashingWithVitaeAsync`
- `SorceryService.cs` — `ExecuteUpdateAsync` with `.SetProperty(c => c.CurrentVitae, c => c.CurrentVitae - vitaeCost)`

### 5. VitaeDepletedEvent is a synchronous in-process Domain event

`VitaeDepletedEvent` is a C# record defined in `RequiemNexus.Domain/Events/`. It is raised synchronously inside `VitaeService.SpendVitaeAsync` when `CurrentVitae` drops to 0. `FrenzyService` is registered as the handler via an `IDomainEventDispatcher` (a thin dispatcher interface in `RequiemNexus.Application`) — no MediatR, no message bus, no SignalR round-trip.

All handlers are resolved from the **same DI scope** as the dispatching service and share the same `DbContext` instance. There is no nested transaction boundary. The dispatcher invokes handlers synchronously and in registration order.

### 6. Dice feed fallback when no active session

`FrenzyService` publishes roll results via `ISessionService.PublishDiceRollAsync`, which requires a `chronicleId`. When Vitae hits 0 on a character not in a live session, `FrenzyService` attempts the publish using the character's `CampaignId`. If the character has no `CampaignId`, or `PublishDiceRollAsync` throws (no active Redis session), the exception is caught, the error is logged, and the method returns the `FrenzySaveResult` normally — the roll result is never lost, only the real-time broadcast is skipped. The Storyteller will see the tilt applied in the Glimpse panel on next load.

### 7. Torpor is a character state, not a separate entity

`TorporSince` (nullable `DateTime` UTC) is added to the `Character` entity. `Character.IsInTorpor` is a computed `[NotMapped]` property (`TorporSince.HasValue`). No new join table. No separate torpor lifecycle entity. The `TorporService` (Application) owns all state transitions.

**Why not a Condition?** Torpor persists across sessions, across chronicle boundaries, and has time-dependent mechanics (hunger escalation based on Blood Potency tier). The Condition system is scoped to scenes. A nullable timestamp on `Character` is the simplest accurate model.

### 8. Starvation notifications are deduplicated per interval

`TorporIntervalService` re-fires on every tick (default: every 24 hours). Without deduplication, a character past their threshold would receive a notification every tick indefinitely. A `LastStarvationNotifiedAt` (`DateTime?` UTC) column on `Character` records the last time a starvation notification was sent. `TorporService.CheckStarvationIntervalAsync` only fires a notification if `LastStarvationNotifiedAt` is null or the elapsed time since the last notification exceeds the character's torpor interval again. `LastStarvationNotifiedAt` is updated (DB write) when a notification is sent.

### 9. TorporIntervalService follows AccountDeletionCleanupService

`TorporIntervalService : BackgroundService` lives at `RequiemNexus.Web/BackgroundServices/TorporIntervalService.cs`. The implementation pattern matches `AccountDeletionCleanupService` exactly: `IServiceScopeFactory` + `while (!stoppingToken.IsCancellationRequested)` + `Task.Delay(TimeSpan.FromHours(N), stoppingToken)`. The `SessionTerminationService` is Redis keyspace subscription-based and is **not** the reference pattern here.

### 10. WillpowerService is extracted alongside VitaeService

Phase 14 left Willpower decrements in `SorceryService` (bulk EF update). Phase 15 introduces `WillpowerService` (Application) with `SpendWillpowerAsync` and `RecoverWillpowerAsync` to centralize all Willpower mutations on `Character` entities.

**Known call sites to migrate (see T3.0):**
- `SorceryService.cs` — `ExecuteUpdateAsync` with `.SetProperty(c => c.CurrentWillpower, c => c.CurrentWillpower - wpCost)`

`AdvancementService.cs` recalculates `CurrentWillpower` when max changes — this is a stat recalculation on XP spend, not a spend, and is left as-is.

---

## 📋 Task Breakdown

### Track 1 — Domain Layer (no EF dependencies)

#### T1.1 — `FrenzyTrigger` enum
**File:** `RequiemNexus.Domain/Enums/FrenzyTrigger.cs`

```csharp
public enum FrenzyTrigger
{
    /// <summary>Vitae reaches 0 during active play. Fired via VitaeDepletedEvent (automatic) or ST manual trigger (edge case).</summary>
    Hunger,

    /// <summary>Provocation during combat. Storyteller-triggered.</summary>
    Rage,

    /// <summary>Exposure to fire or sunlight. Player- or Storyteller-triggered.</summary>
    Rotschreck,

    /// <summary>Torpor hunger escalation. BackgroundService / Advance Time path only. Never a manual UI trigger.</summary>
    Starvation
}
```

> **Rule:** `Hunger` and `Starvation` are distinct code paths. `Hunger` can come from `VitaeDepletedEvent` or ST manual trigger. `Starvation` is always a torpor-interval response only.

#### T1.2 — `VitaeDepletedEvent` Domain event
**File:** `RequiemNexus.Domain/Events/VitaeDepletedEvent.cs`

```csharp
/// <summary>Raised synchronously when a character's Vitae reaches 0 during a spend operation.</summary>
public record VitaeDepletedEvent(int CharacterId);
```

#### T1.3 — `IDomainEventDispatcher` interface
**File:** `RequiemNexus.Application/Events/IDomainEventDispatcher.cs`

```csharp
public interface IDomainEventDispatcher
{
    void Dispatch<TEvent>(TEvent domainEvent) where TEvent : class;
}
```

Simple synchronous dispatch — no async, no queuing. Resolves all `IDomainEventHandler<TEvent>` implementations from the **same DI scope** as the caller. Handlers share the same `DbContext` instance. Register dispatcher as `Scoped`. Handlers are invoked in registration order; ordering is documented in `Program.cs`.

#### T1.4 — Torpor duration table (Domain constant)
**File:** `RequiemNexus.Domain/TorporDurationTable.cs`

Static readonly dictionary mapping `BloodPotency` (1–10) to minimum torpor duration in days. Values from VtR 2e core p. 165. BP 10 uses `int.MaxValue` as "indefinitely" with a code comment.

```csharp
public static class TorporDurationTable
{
    // Blood Potency → minimum torpor length in days (VtR 2e p. 165)
    // BP 10 ("indefinitely") represented as int.MaxValue; treated as "notification threshold never elapses automatically."
    public static readonly IReadOnlyDictionary<int, int> MinimumDaysById = new Dictionary<int, int>
    {
        { 1,  1      },   // one night
        { 2,  7      },   // one week
        { 3,  30     },   // one month
        { 4,  365    },   // one year
        { 5,  3_650  },   // one decade
        { 6,  36_500 },   // one century
        { 7,  182_500},   // five centuries
        { 8,  365_000},   // one millennium
        { 9,  3_650_000}, // ten millennia
        { 10, int.MaxValue }, // indefinitely
    };
}
```

> Month = 30 days, year = 365 days (fractional rounding). Any ambiguity in the source text is recorded in the Rules Interpretation Log.

---

### Track 2 — Data Layer (migrations)

#### T2.1 — Add `TorporSince` and `LastStarvationNotifiedAt` to `Character`
**Migration name:** `Phase15TorporState`

```csharp
// In Character.cs
/// <summary>UTC timestamp when the character entered torpor. Null when not in torpor.</summary>
public DateTime? TorporSince { get; set; }

/// <summary>UTC timestamp of the last starvation notification sent for this character. Null if never notified.</summary>
public DateTime? LastStarvationNotifiedAt { get; set; }

/// <summary>True when the character is currently in torpor.</summary>
[NotMapped]
public bool IsInTorpor => TorporSince.HasValue;
```

#### T2.2 — Unique index on CharacterTilt to prevent duplicate active tilts of the same type
**Migration name:** batched with T2.1 (`Phase15TorporState`)

Add a unique filtered index on `CharacterTilts(CharacterId, TiltType)` where `IsActive = 1`. Implement via **Fluent API** in `CharacterTiltConfiguration.cs` so it works on both PostgreSQL (CI/production) and SQLite (local):

```csharp
// In CharacterTiltConfiguration.cs
builder.HasIndex(t => new { t.CharacterId, t.TiltType })
       .HasFilter("[IsActive] = 1")          // SQLite
       // .HasFilter("\"IsActive\" = true")  // PostgreSQL — use conditional compile or migration override
       .IsUnique();
```

> **Note:** EF Core filtered index syntax differs between SQLite and PostgreSQL. Use a migration-override partial class to apply the correct SQL per provider, or use a single raw SQL migration that branches on provider name. Add a `RequiemNexus.Data.Tests` integration test asserting the constraint fires on the active provider.

---

### Track 3 — Application Layer (services)

#### T3.0 — Vitae and Willpower mutation inventory and migration _(prerequisite to T3.1/T3.2)_

Before `VitaeService` and `WillpowerService` can be the authoritative paths, all existing `CurrentVitae` and `CurrentWillpower` mutation call sites on `Character` entities must be identified and migrated. Complete this audit first; every call site below must be refactored to use the new services before Phase 15 ships.

**Vitae spend call sites (must migrate to `IVitaeService`):**

| File | Line pattern | Notes |
|------|-------------|-------|
| `CharacterHealthService.cs` | `character.CurrentVitae -= vitaeSpent` | Fast bashing heal |
| `SorceryService.cs` | `ExecuteUpdateAsync(…CurrentVitae - vitaeCost…)` | Bulk EF update — must refactor to tracked-entity update + `IVitaeService.SpendVitaeAsync` so the event fires |

**Vitae initialization call sites (leave as-is — not spends):**

| File | Notes |
|------|-------|
| `CharacterManagementService.cs` | Character creation — sets `CurrentVitae = initialValue` |
| `EncounterService.cs` (×2) | NPC `InitiativeEntry.NpcCurrentVitae` initialization — not a `Character` entity spend |

**Willpower spend call sites (must migrate to `IWillpowerService`):**

| File | Line pattern | Notes |
|------|-------------|-------|
| `SorceryService.cs` | `ExecuteUpdateAsync(…CurrentWillpower - wpCost…)` | Bulk EF update — same refactor as Vitae above |

**Willpower non-spend call sites (leave as-is):**

| File | Notes |
|------|-------|
| `AdvancementService.cs` | `CurrentWillpower = Math.Clamp(current + diff, 0, max)` — stat recalculation on advancement |
| `CharacterManagementService.cs` | Character creation initialization |

> **SorceryService refactor note:** The existing `ExecuteUpdateAsync` performs a conditional atomic decrement (only updates if `CurrentVitae >= cost`). Refactoring to a tracked-entity approach must preserve this atomicity. Use an optimistic concurrency check (`RowVersion`) or a `WHERE CurrentVitae >= cost` EF condition equivalent when rewriting.

#### T3.1 — `VitaeService`
**Files:**
- `RequiemNexus.Application/Contracts/IVitaeService.cs`
- `RequiemNexus.Application/Services/VitaeService.cs`

```csharp
public interface IVitaeService
{
    /// <summary>Spends <paramref name="amount"/> Vitae. Raises VitaeDepletedEvent if CurrentVitae reaches 0.</summary>
    Task<Result<int>> SpendVitaeAsync(int characterId, string userId, int amount, string reason,
        CancellationToken cancellationToken = default);

    /// <summary>Gains <paramref name="amount"/> Vitae up to MaxVitae.</summary>
    Task<Result<int>> GainVitaeAsync(int characterId, string userId, int amount, string reason,
        CancellationToken cancellationToken = default);
}
```

**Masquerade:** Character ownership OR Storyteller access (same pattern as `CharacterHealthService`).
**Event:** After `SaveChangesAsync`, if `CurrentVitae == 0`, call `IDomainEventDispatcher.Dispatch(new VitaeDepletedEvent(characterId))`.
**Failure:** Returns `Result.Failure` if requested spend exceeds `CurrentVitae`.

#### T3.2 — `WillpowerService`
**Files:**
- `RequiemNexus.Application/Contracts/IWillpowerService.cs`
- `RequiemNexus.Application/Services/WillpowerService.cs`

```csharp
public interface IWillpowerService
{
    Task<Result<int>> SpendWillpowerAsync(int characterId, string userId, int amount,
        CancellationToken cancellationToken = default);

    Task<Result<int>> RecoverWillpowerAsync(int characterId, string userId, int amount,
        CancellationToken cancellationToken = default);
}
```

**Masquerade:** Same pattern. `SpendWillpowerAsync` returns `Result.Failure` if `CurrentWillpower < amount`.

#### T3.3 — `FrenzyService`
**Files:**
- `RequiemNexus.Application/Contracts/IFrenzyService.cs`
- `RequiemNexus.Application/Services/FrenzyService.cs`
- `RequiemNexus.Application/Models/FrenzySaveResult.cs` _(separate file — one type per file rule)_

**Contract:**
```csharp
public interface IFrenzyService
{
    /// <summary>
    /// Executes a frenzy save roll for <paramref name="characterId"/>.
    /// Pool: Resolve + Blood Potency. Failure applies the relevant Tilt.
    /// </summary>
    Task<Result<FrenzySaveResult>> RollFrenzySaveAsync(
        int characterId,
        string userId,
        FrenzyTrigger trigger,
        bool spendWillpower,
        CancellationToken cancellationToken = default);
}
```

**Result model (`FrenzySaveResult.cs`):**
```csharp
public record FrenzySaveResult(
    int Successes,
    bool Saved,
    bool WillpowerSpent,
    FrenzyTrigger Trigger,
    TiltType? TiltApplied,
    bool SuppressedDueToBeastAlreadyActive);
```

**Implementation rules:**
1. Load character; check Masquerade (character ownership or Storyteller access).
2. **Beast tilt guard:** If any active tilt is `Frenzy` or `Rotschreck`, return a suppressed result (`SuppressedDueToBeastAlreadyActive = true`) without rolling or spending Willpower.
3. Resolve `Resolve + Blood Potency` pool via `TraitResolver`.
4. If `spendWillpower = true` and `CurrentWillpower > 0`: subtract 1 die from pool; spend 1 Willpower via `IWillpowerService`. If pool reaches 0 after subtraction, use a chance die (1 die).
5. Roll via `IDiceService`.
6. On failure: determine tilt — `Rotschreck` trigger → `TiltType.Rotschreck`; all other triggers → `TiltType.Frenzy`.
7. Insert `CharacterTilt` inside a `SaveChangesAsync` transaction. The unique index (T2.2) is the DB backstop for concurrent same-tilt inserts.
8. Attempt to publish result to dice feed via `ISessionService.PublishDiceRollAsync`. Catch and log on failure (no active session is non-fatal — see AD 6).
9. Return `FrenzySaveResult`.

**Masquerade:** Storyteller access OR character ownership.

#### T3.4 — `VitaeDepletedEvent` Handler
**File:** `RequiemNexus.Application/Events/Handlers/VitaeDepletedEventHandler.cs`

Implements `IDomainEventHandler<VitaeDepletedEvent>`. Called synchronously by `IDomainEventDispatcher`. Invokes `FrenzyService.RollFrenzySaveAsync` with `trigger = FrenzyTrigger.Hunger` and `spendWillpower = false`. Uses the character's `ApplicationUserId` as the `userId` parameter (the spend originated from the character's own action).

**Idempotency:** `FrenzyService` step 2 (Beast tilt guard) suppresses the save if the character is already in a Beast state — no special handling needed in the handler itself.

#### T3.5 — `TorporService`
**Files:**
- `RequiemNexus.Application/Contracts/ITorporService.cs`
- `RequiemNexus.Application/Services/TorporService.cs`

```csharp
public interface ITorporService
{
    /// <summary>Enters torpor. Sets TorporSince = UtcNow. Resolves any active Beast tilt.</summary>
    Task<Result> EnterTorporAsync(int characterId, string userId,
        CancellationToken cancellationToken = default);

    /// <summary>Awakens from torpor. Costs 1 Vitae unless narrativeAwakening = true.</summary>
    Task<Result> AwakenFromTorporAsync(int characterId, string userId, bool narrativeAwakening,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether the starvation threshold has elapsed since TorporSince.
    /// Fires a Storyteller notification and updates LastStarvationNotifiedAt if threshold exceeded.
    /// No-op if threshold not reached or already notified for this interval.
    /// </summary>
    Task CheckStarvationIntervalAsync(int characterId, CancellationToken cancellationToken = default);
}
```

**EnterTorpor rules:**
- Sets `TorporSince = DateTime.UtcNow`.
- If a `Frenzy` or `Rotschreck` tilt is active, resolves it (Torpor ends the Beast's rage).
- Masquerade: Storyteller access.

**AwakenFromTorpor rules:**
- Clears `TorporSince` and `LastStarvationNotifiedAt`.
- If `narrativeAwakening = false`: deducts 1 Vitae via `IVitaeService`; returns `Result.Failure` if Vitae = 0.
- If `narrativeAwakening = true`: no Vitae cost; Storyteller confirmed an anchor moment per book p. 165.
- Masquerade: Storyteller access.

**CheckStarvationInterval rules:**
- Reads `TorporDurationTable` for the character's `BloodPotency`.
- If BP = 10 (`int.MaxValue` threshold): no notification ever fires automatically.
- If `DateTime.UtcNow - TorporSince >= threshold` AND (`LastStarvationNotifiedAt` is null OR `DateTime.UtcNow - LastStarvationNotifiedAt >= threshold`): fire ST notification + set `LastStarvationNotifiedAt = DateTime.UtcNow` + `SaveChangesAsync`.
- Otherwise: no-op.

#### T3.6 — `DomainEventDispatcher` implementation
**File:** `RequiemNexus.Application/Events/DomainEventDispatcher.cs`

Resolves all `IDomainEventHandler<TEvent>` from the injected `IServiceProvider` (same scope) and invokes them in registration order. Synchronous only.

---

### Track 4 — Web / Background Services

#### T4.1 — `TorporIntervalService`
**File:** `RequiemNexus.Web/BackgroundServices/TorporIntervalService.cs`

Extends `BackgroundService`. Pattern mirrors `AccountDeletionCleanupService` exactly: `IServiceScopeFactory`, `while (!stoppingToken.IsCancellationRequested)`, `Task.Delay` for configurable cadence.

```
ExecuteAsync:
  - await Task.Delay(30s, stoppingToken)  // startup delay, same as AccountDeletionCleanupService
  - while not cancelled:
      - try:
          - using scope = scopeFactory.CreateScope()
          - resolve ApplicationDbContext + ITorporService
          - ids = SELECT Id FROM Characters WHERE TorporSince IS NOT NULL
          - foreach id: await TorporService.CheckStarvationIntervalAsync(id)
      - catch (non-cancellation): log error, continue
      - await Task.Delay(TimeSpan.FromHours(config["Torpor:IntervalHours"] ?? 24), stoppingToken)
```

Registered in `Program.cs` as `AddHostedService<TorporIntervalService>()`.

#### T4.2 — Program.cs registrations
```csharp
builder.Services.AddScoped<IVitaeService, VitaeService>();
builder.Services.AddScoped<IWillpowerService, WillpowerService>();
builder.Services.AddScoped<IFrenzyService, FrenzyService>();
builder.Services.AddScoped<ITorporService, TorporService>();
builder.Services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
// Handler registered first = invoked first; ordering is intentional.
builder.Services.AddScoped<IDomainEventHandler<VitaeDepletedEvent>, VitaeDepletedEventHandler>();
builder.Services.AddHostedService<TorporIntervalService>();
```

---

### Track 5 — UI Components

#### T5.1 — Frenzy Save UI — Player
**Target component:** Character sheet — new collapsible **"The Beast"** panel

| Element | Behavior |
|---------|----------|
| "Rotschreck Exposure" button | Opens confirmation modal: "Exposed to fire or sunlight. Roll Rotschreck save?" with Willpower spend checkbox (disabled if `CurrentWillpower == 0`). Calls `FrenzyService.RollFrenzySaveAsync(trigger: Rotschreck)`. |
| Active frenzy badge | Shown when `Frenzy` or `Rotschreck` tilt is active. Red pulsing indicator. Links to Tilt detail. |

Result posted to the real-time dice feed. Screen-reader announcement via Phase 13 announcer pattern.

#### T5.2 — Frenzy Save UI — Storyteller Glimpse
**Target component:** Storyteller Glimpse vitals panel per character — **"Trigger Frenzy Save"** dropdown

| Element | Behavior |
|---------|----------|
| Trigger type picker | Dropdown: `Hunger` / `Rage` / `Rotschreck`. `Starvation` is **not** available here. |
| Willpower spend checkbox | Whether the character spends Willpower to resist |
| Roll button | Calls `FrenzyService.RollFrenzySaveAsync` |
| Result banner | Inline: successes, saved/failed, tilt applied, or "Beast already active — save suppressed" |

NPC frenzy saves are ST-only. PC save results are visible to the owning player via SignalR.

#### T5.3 — Torpor UI — Character Sheet
| Element | Behavior |
|---------|----------|
| "In Torpor" badge | Shown when `character.IsInTorpor`. Displays "Torpor Since: {date}" |
| Torpor entry button | Storyteller-only. Calls `TorporService.EnterTorporAsync`. |
| Awaken button | Storyteller-only. Toggle: "Costs 1 Vitae" (`narrativeAwakening = false`) or "Narrative awakening" (`narrativeAwakening = true`). |

#### T5.4 — Torpor UI — Storyteller Glimpse
| Element | Behavior |
|---------|----------|
| Torpor overview panel | Collapsible section listing all torpored characters in the chronicle |
| "Torpor Since" display | Formatted relative time + absolute UTC |
| Starvation warning | Amber icon when `LastStarvationNotifiedAt` is within the last tick window |
| "Advance Time" button | Calls `TorporService.CheckStarvationIntervalAsync` for all torpored characters on demand |
| Awaken button | Per-character inline awaken action |

#### T5.5 — Starvation notification banner
Uses the existing Storyteller notification channel. Notification body includes:
- Character name + Blood Potency tier
- Elapsed torpor time
- Expected hunger threshold (from `TorporDurationTable`)
- "Advance hunger" action button that triggers a `Starvation` frenzy save via `FrenzyService`

---

### Track 6 — Rules Interpretation Log

Add a **Phase 15** section to `docs/rules-interpretations.md` covering:

| Decision | Interpretation |
|----------|----------------|
| Frenzy pool | `Resolve + Blood Potency` for all frenzy save types including Rötschreck (VtR 2e p. 99). No separate pool is specified. |
| Rötschreck vs. Frenzy tilt | `Rotschreck` trigger → `TiltType.Rotschreck`; all other triggers → `TiltType.Frenzy`. Both are Beast tilts. Only one Beast tilt is active at a time. |
| Hunger trigger — manual ST | The ST may manually trigger a `Hunger` save for narrative edge cases (off-screen Vitae depletion). This does not re-fire `VitaeDepletedEvent`; it is a direct `FrenzyService` call. |
| Vitae-zero when Rotschreck active | If the character is already in `Rotschreck` when Vitae reaches 0, the automatic `Hunger` frenzy save is suppressed. The character is already in a Beast state. |
| Willpower die subtraction | Spending Willpower to resist frenzy subtracts 1 die from the pool (VtR 2e p. 92 general Willpower rule). If the pool reaches 0 after subtraction, a chance die (1 die) is used. Not a bonus to successes. |
| Torpor awakening cost | "One Vitae, or an anchor moment" (p. 165). `narrativeAwakening = false` deducts 1 Vitae; `narrativeAwakening = true` requires ST confirmation with no Vitae cost. |
| Torpor duration table | VtR 2e p. 165 table. Month = 30 days, year = 365 days. BP 10 ("indefinitely") uses `int.MaxValue` — no automatic starvation notification. |
| Hunger escalation | Book: Hunger increases by 1 at each torpor interval milestone. App fires a Storyteller notification; the Hunger track update is a manual ST action until Phase 16a wires full Hunger mechanics. |
| Starvation notification deduplication | One notification per elapsed interval per character. `LastStarvationNotifiedAt` prevents repeat-fire on subsequent ticks. |
| Dice feed without active session | If `PublishDiceRollAsync` fails (no active session or no `CampaignId`), the `FrenzySaveResult` is still returned normally and the tilt is still applied. Only the real-time broadcast is skipped. The Storyteller sees the tilt on next Glimpse load. |

---

## 🔗 Dependency Map

```
Phase 14 delivers:
  ✅ DamageSource.Fire / .Sunlight  → used by Rotschreck trigger type in UI
  ✅ CharacterHealthService          → Phase 15 refactors its Vitae path to call VitaeService
  ✅ ModifierService / WoundPenalty  → referenced in Phase 17 (not Phase 15)
  ✅ TiltType.Frenzy / .Rotschreck  → already in TiltType.cs; Phase 15 applies them

Phase 15 delivers for Phase 16a (Hunting):
  → VitaeService.GainVitaeAsync (feeding Vitae gain uses this)

Phase 15 delivers for Phase 17 (Humanity):
  → VitaeDepletedEvent + IDomainEventDispatcher pattern (template for DegenerationCheckRequired)
  → IDomainEventHandler<T> interface (reused for degeneration handler)
  → WillpowerService (reused for degeneration roll Willpower cost)
```

---

## ✅ Exit Criteria

Phase 15 is complete when **all** of the following are true:

1. **Vitae/Willpower migration** — All `CurrentVitae` spend call sites (CharacterHealthService, SorceryService) and `CurrentWillpower` spend call sites (SorceryService) route through `IVitaeService` / `IWillpowerService`. Verified by grepping for direct `CurrentVitae -=` and `CurrentWillpower -=` assignments on `Character` entities.
2. **Frenzy save rolls** — A Storyteller can trigger a frenzy save (`Hunger` / `Rage` / `Rotschreck`) for any PC or NPC from the Glimpse panel; result appears in dice feed and the correct tilt is applied on failure.
3. **Player Rotschreck** — A player can trigger a Rotschreck save from the character sheet; Willpower spend is optional; result appears in dice feed.
4. **Vitae-zero event** — When `VitaeService.SpendVitaeAsync` reduces `CurrentVitae` to 0, a `Hunger` frenzy save is rolled automatically and appears in dice feed (or is logged gracefully if no session is active).
5. **Beast tilt mutual exclusion** — A character with an active `Rotschreck` tilt cannot have a new `Frenzy` tilt applied (and vice versa). The second save returns `SuppressedDueToBeastAlreadyActive = true`.
6. **Torpor entry** — A Storyteller can enter any character into torpor; `TorporSince` is set and badge appears on sheet.
7. **Torpor awakening** — A Storyteller can awaken a torpored character; `TorporSince` is cleared. Vitae cost enforced when `narrativeAwakening = false`; bypassed when `true`.
8. **Background service** — `TorporIntervalService` starts with the application, runs on configurable cadence, calls `CheckStarvationIntervalAsync` per torpored character.
9. **Starvation deduplication** — Re-running the interval check on the same character does not produce duplicate notifications within the same interval window.
10. **Advance Time** — The ST Glimpse "Advance Time" button triggers the interval check on demand.
11. **Willpower spend in frenzy** — Spending Willpower subtracts 1 die from pool and decrements `CurrentWillpower`.
12. **Rules Interpretation Log** — Phase 15 section committed to `docs/rules-interpretations.md`.
13. **`dotnet format` clean** — No format violations.
14. **`.\scripts\test-local.ps1` green** — All unit and integration tests pass.

---

## 🧪 Test Coverage Requirements

### Unit Tests (`RequiemNexus.Domain.Tests`)
- `TorporDurationTable` contains entries for BP 1–10; values are strictly ascending; BP 10 = `int.MaxValue`
- `FrenzyTrigger` enum values are distinct

### Unit Tests (`RequiemNexus.Application.Tests`)
- `FrenzyService` — returns saved result when successes > 0
- `FrenzyService` — applies `TiltType.Frenzy` when successes = 0 and trigger = `Rage`
- `FrenzyService` — applies `TiltType.Rotschreck` when successes = 0 and trigger = `Rotschreck`
- `FrenzyService` — Willpower spend subtracts 1 die from pool and decrements `CurrentWillpower`
- `FrenzyService` — pool of 0 after Willpower spend uses chance die (1 die)
- `FrenzyService` — returns `SuppressedDueToBeastAlreadyActive = true` when any Beast tilt already active (Frenzy active → Rotschreck save suppressed; Rotschreck active → Hunger save suppressed)
- `VitaeService.SpendVitaeAsync` — dispatches `VitaeDepletedEvent` when `CurrentVitae` reaches 0
- `VitaeService.SpendVitaeAsync` — does not dispatch event when `CurrentVitae > 0` after spend
- `VitaeService.SpendVitaeAsync` — returns `Result.Failure` when spend exceeds available Vitae
- `TorporService.EnterTorporAsync` — sets `TorporSince`; resolves active `Frenzy` tilt
- `TorporService.AwakenFromTorporAsync` — clears `TorporSince` and `LastStarvationNotifiedAt`; deducts 1 Vitae when `narrativeAwakening = false`
- `TorporService.AwakenFromTorporAsync` — returns failure when Vitae = 0 and `narrativeAwakening = false`
- `TorporService.AwakenFromTorporAsync` — clears `TorporSince` with no Vitae cost when `narrativeAwakening = true`
- `TorporService.CheckStarvationIntervalAsync` — fires notification when threshold elapsed and `LastStarvationNotifiedAt` is null
- `TorporService.CheckStarvationIntervalAsync` — does NOT fire when already notified within current interval
- `TorporService.CheckStarvationIntervalAsync` — does NOT fire for BP 10 characters

### Integration Tests (`RequiemNexus.Data.Tests`)
- `Phase15TorporState` migration applies cleanly to an empty database
- Unique index on `CharacterTilts` prevents duplicate active tilt rows of the same type (constraint violation test on active provider)
- `VitaeService` end-to-end: spend to 0 → `VitaeDepletedEvent` → `FrenzyService` → tilt row inserted

---

## 📂 New Files Summary

| File | Layer | Purpose |
|------|-------|---------|
| `Domain/Enums/FrenzyTrigger.cs` | Domain | Trigger enum |
| `Domain/Events/VitaeDepletedEvent.cs` | Domain | In-process domain event |
| `Domain/TorporDurationTable.cs` | Domain | BP → days static table (BP 1–10) |
| `Application/Events/IDomainEventDispatcher.cs` | Application | Dispatcher interface |
| `Application/Events/IDomainEventHandler.cs` | Application | Handler interface |
| `Application/Events/DomainEventDispatcher.cs` | Application | Dispatcher implementation |
| `Application/Events/Handlers/VitaeDepletedEventHandler.cs` | Application | Wires event → FrenzyService |
| `Application/Contracts/IVitaeService.cs` | Application | Vitae spend/gain contract |
| `Application/Services/VitaeService.cs` | Application | Vitae spend/gain implementation |
| `Application/Contracts/IWillpowerService.cs` | Application | Willpower spend/recover contract |
| `Application/Services/WillpowerService.cs` | Application | Willpower implementation |
| `Application/Contracts/IFrenzyService.cs` | Application | Frenzy save contract |
| `Application/Services/FrenzyService.cs` | Application | Frenzy save implementation |
| `Application/Models/FrenzySaveResult.cs` | Application | Roll outcome record (separate file) |
| `Application/Contracts/ITorporService.cs` | Application | Torpor state contract |
| `Application/Services/TorporService.cs` | Application | Torpor state implementation |
| `Web/BackgroundServices/TorporIntervalService.cs` | Web | Nightly torpor ticker |
| `Data/Migrations/Phase15TorporState.cs` | Data | TorporSince + LastStarvationNotifiedAt + unique tilt index |

---

## 🔄 Modified Files Summary

| File | Change |
|------|--------|
| `Data/Models/Character.cs` | Add `TorporSince`, `LastStarvationNotifiedAt`, `IsInTorpor` |
| `Data/EntityConfigurations/CharacterTiltConfiguration.cs` | Add Fluent API filtered unique index |
| `Application/Services/CharacterHealthService.cs` | Delegate Vitae deduction to `IVitaeService` |
| `Application/Services/SorceryService.cs` | Replace bulk `ExecuteUpdateAsync` Vitae/Willpower decrements with `IVitaeService` / `IWillpowerService` calls |
| `Web/Program.cs` | Register all new services + hosted service |
| `docs/rules-interpretations.md` | Add Phase 15 section |
| `docs/mission.md` | Update Phase 15 status to ✅ Complete when done |

---

> _The blood runs cold when the Beast takes hold. Give it rules. Make it trackable. Make it terrifying._
