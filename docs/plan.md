# 📋 Phase 7 Implementation Plan — Realtime Play (The Blood Communion)

> This document is the authoritative implementation plan for Phase 7.
> It is the detailed complement to the feature list in `docs/mission.md`.
> All architectural decisions recorded here were made explicitly — nothing is implicit.

---

## 🧭 Decisions Locked Before Planning

| Decision | Resolution | Rationale |
|---|---|---|
| Session persistence | Transient — Redis only, no DB entity | Sessions are ephemeral play contexts. Persisting them adds schema complexity with no player value. |
| Session lifecycle | ST starts/ends explicitly; auto-terminates after 15 min ST disconnect | Gives the ST full control. The timeout prevents ghost sessions consuming Redis memory. |
| ST disconnect timer | Redis key with TTL + `IHostedService` watcher | Redis TTL handles expiry naturally. The background service broadcasts "session terminated" to players. |
| Roll history | Ephemeral — Redis list for session duration only | Rolls are in-the-moment. No player has ever asked "what did I roll last Tuesday?" |
| Blazor hosting model | Stay on Blazor Server — no WASM migration | Phase 8 (PWA/Offline) is deferred indefinitely. The WiFi-at-the-table assumption holds. Hub is still required for cross-client broadcasting regardless of hosting model. |
| Phase 8 status | Deferred indefinitely | Target players are in WiFi-stable environments (home, university, school). The offline engineering cost does not justify the use case. |
| 200ms budget | Server dispatch time only — from hub method invocation to message dispatched to group | Network and client performance are beyond our control. We own the server side. |
| 200ms enforcement | NBomber integration test in CI | Consistent with how all other performance budgets are enforced in this project. |
| Dice rolling | Server-side only — the `DiceService` rolls, the hub broadcasts the result | Prevents client-reported rolls. Integrity is non-negotiable. |
| Hub typing | Strongly-typed `ISessionClient` interface | Compile-time method name safety. A teachable Grimoire moment for C# generics in SignalR. |
| Online concurrency | Optimistic concurrency with `RowVersion` on mutated entities | Two concurrent ST mutations to the same character are resolved by the DB, not the hub. |

---

## 🗺️ Layer Ownership Map

This follows the Sacred Covenant: `Web → Application → Domain → Data`.

| Concern | Layer | Location |
|---|---|---|
| `SessionHub` (thin relay, no logic) | Web | `src/RequiemNexus.Web/Hubs/SessionHub.cs` |
| `ISessionClient` typed hub interface | Application | `src/RequiemNexus.Application/RealTime/ISessionClient.cs` |
| `SessionService` (orchestrates session operations) | Application | `src/RequiemNexus.Application/RealTime/SessionService.cs` |
| `ISessionService` | Application | `src/RequiemNexus.Application/RealTime/ISessionService.cs` |
| `SessionStateRepository` (Redis reads/writes) | Data | `src/RequiemNexus.Data/RealTime/SessionStateRepository.cs` |
| `ISessionStateRepository` | Application | `src/RequiemNexus.Application/RealTime/ISessionStateRepository.cs` |
| `SessionTerminationService` (background watcher) | Web | `src/RequiemNexus.Web/BackgroundServices/SessionTerminationService.cs` |
| Hub authorization (Masquerade extension) | Application | `src/RequiemNexus.Application/RealTime/SessionAuthorizationService.cs` |
| Public roll record (async/play-by-post) | Data + Domain | New `PublicRoll` entity with slug |
| Redis backplane configuration | Data / Infrastructure | `src/RequiemNexus.Data/` startup extensions |

**Rule:** `SessionHub` calls `SessionService`. It holds no logic, makes no authorization decisions, and touches no Redis directly. The hub is a pure output channel — exactly as defined in `Architecture.md`.

---

## ✅ Task Breakdown

### Area 1 — Foundation & Infrastructure

- [ ] **Approve and add NuGet package:** `Microsoft.AspNetCore.SignalR.StackExchangeRedis`
  — Required for the Redis backplane. This is a first-party Microsoft package with no transitive surprises. Flag for explicit approval per `agents.md`.
- [ ] **Configure Redis as SignalR backplane** in `Program.cs` startup
  — `.AddSignalR().AddStackExchangeRedis(...)`. Without this, messages sent on one ECS Fargate task never reach clients connected to another.
- [ ] **Verify CDK stack** — confirm the existing ElastiCache Redis instance is accessible from the SignalR backplane config. No new AWS resources expected; this is a connection string/configuration task.
- [ ] **Define `ISessionClient`** — the typed interface all hub client methods implement. Every method the server can call on a client is declared here. This is the contract.
- [ ] **Create `SessionHub`** — inherits `Hub<ISessionClient>`. Thin relay only. Maps incoming calls to `ISessionService`. No `if` statements, no Redis, no game logic.
- [ ] **Create `ISessionService` and `SessionService`** — orchestrates all session operations. This is where authorization checks live, where `ISessionStateRepository` is called, and where `IHubContext<SessionHub, ISessionClient>` is injected for server-initiated pushes.
- [ ] **Create `ISessionStateRepository` and `SessionStateRepository`** — all Redis reads and writes for session state. Keys, TTLs, and data shapes are defined here and nowhere else.

---

### Area 2 — Session Lifecycle

Session state shape in Redis:

```
session:{chronicleId}:info        → { stConnectionId, startedAt }    TTL: 15min (renewed on ST heartbeat)
session:{chronicleId}:players     → Redis SET of { userId, connectionId, characterId }
session:{chronicleId}:rolls       → Redis LIST of roll result DTOs    (LPUSH, LTRIM to last 100)
session:{chronicleId}:initiative  → Redis ZSET scored by initiative value
```

- [ ] **Implement "Start Session"** — ST-only hub method. Creates `session:{chronicleId}:info` in Redis. Broadcasts `SessionStarted` to the chronicle group. Validates the ST owns the chronicle (Masquerade step 3).
- [ ] **Implement "End Session"** — ST-only hub method. Deletes all `session:{chronicleId}:*` keys. Broadcasts `SessionEnded` to the chronicle group so all clients know to clear real-time state.
- [ ] **Implement ST heartbeat** — the ST client sends a lightweight `Heartbeat` message every 2 minutes. `SessionService` renews the Redis key TTL on receipt. No heartbeat for 15 minutes = session expires.
- [ ] **Implement `SessionTerminationService`** (`IHostedService`)
  — Uses Redis keyspace notifications (or polling) to detect `session:{chronicleId}:info` key expiry. On expiry, broadcasts `SessionEnded` via `IHubContext` and cleans up sibling keys. This is the fallback when the ST disconnects without explicitly ending the session.
- [ ] **Implement player join** — players call `JoinSession` hub method on connect. `SessionService` validates they are a member of the chronicle, then adds them to the Redis players SET and the SignalR `chronicleId` group. Broadcasts `PlayerJoined` to the group.
- [ ] **Implement player leave** — `OnDisconnectedAsync` removes the player from the Redis SET and broadcasts `PlayerLeft`. Connection IDs are transient; `userId` is the stable identity.

---

### Area 3 — Hub Authorization (The Masquerade Extended)

The four Masquerade steps apply to every hub method that mutates state.

- [ ] **Authenticate connections** — configure SignalR to read the ASP.NET Core Identity cookie. Unauthenticated connections are rejected at the hub `OnConnectedAsync`. No special token flow needed — the existing cookie auth is sufficient for Blazor Server.
- [ ] **Create `SessionAuthorizationService`** — encapsulates all authorization checks for hub methods:
  - `RequireChroniclePlayer(userId, chronicleId)` — caller must be a member
  - `RequireStoryteller(userId, chronicleId)` — caller must be the ST of this chronicle
  - `RequireCharacterOwner(userId, characterId)` — caller must own the character
- [ ] **Apply per-method authorization** inside `SessionHub` — every method delegates immediately to `SessionAuthorizationService` before touching anything. Unauthorized calls are rejected with a `HubException` (which the client receives as a structured error, not a crash).
- [ ] **Input validation** — all hub method parameters are validated (null checks, range checks, max string lengths) before being passed to `SessionService`. Invalid input returns a `HubException`. Same standard as REST endpoints.

---

### Area 4 — Real-Time Features

- [ ] **Live Dice Rolls**
  — Player calls `RollDice(chronicleId, poolSpec)`. `SessionService` invokes `DiceService` server-side (integrity: server rolls, not client-reported). Result is stored in the Redis roll history list and broadcast via `ISessionClient.ReceiveDiceRoll(result)` to the chronicle group.
- [ ] **Dice Roll History Feed**
  — On `JoinSession`, the server sends the current Redis rolls list to the joining client as an initial hydration payload (`ReceiveRollHistory`). New rolls are appended live. History is capped at 100 entries (Redis `LTRIM`). Dies when the session ends.
- [ ] **Real-Time Character Updates**
  — Health, Willpower, Vitae, and Condition changes broadcast `ReceiveCharacterUpdate(patch)` to the chronicle group after the REST mutation succeeds. The hub does not own the mutation — it only broadcasts the outcome. Online concurrency handled by EF Core `RowVersion`; the REST endpoint returns a conflict error if stale.
- [ ] **Shared Initiative Tracker**
  — ST calls `UpdateInitiative(chronicleId, entries[])`. Stored in the Redis initiative ZSET. Broadcast via `ReceiveInitiativeUpdate(entries[])` to the group. Read-only for players.
- [ ] **Session Presence**
  — Derived from the Redis players SET. `ReceivePresenceUpdate(players[])` broadcast on every join/leave. No dedicated presence store needed.
- [ ] **Synchronized Chronicle State**
  — ST actions (Beat awards, NPC updates, scene changes) broadcast `ReceiveChronicleUpdate(patch)` after the REST mutation succeeds. Same pattern as character updates — hub broadcasts outcomes, not commands.

---

### Area 5 — Reconnection

- [ ] **Define full-state-snapshot REST endpoint** — `GET /api/sessions/{chronicleId}/state` returns current session info, initiative, presence, and roll history from Redis. Used by clients on reconnect.
- [ ] **Implement client reconnection flow** — on `OnConnectedAsync`, if a session is already active for the chronicle, the server sends `SessionAlreadyActive` with the snapshot URL. Client re-hydrates via REST, then rejoins the SignalR group. Missed SignalR messages are not replayed — the snapshot is the source of truth (per `Architecture.md`).

---

### Area 6 — Rate Limiting

- [ ] **Throttle hub message frequency per connection** — add ASP.NET Core rate limiting middleware applied to the `/hubs/session` endpoint. Limit: 30 messages per connection per minute (configurable). Excess messages are silently dropped with a `HubException` returned to the caller. Prevents hub flooding without disconnecting the player.

---

### Area 7 — Async / Play-by-Post Dice Sharing

- [ ] **Define `PublicRoll` domain entity** — `Id`, `Slug` (short URL-safe identifier), `ChronicleId`, `RolledBy` (userId), `PoolSpec`, `Result`, `CreatedAt`. Lives in the DB (persistent, unlike session rolls).
- [ ] **Add EF Core migration** for `PublicRolls` table.
- [ ] **Implement `ShareRoll` application service** — player chooses to share a roll result. Creates a `PublicRoll` record with a generated slug. Returns the shareable URL.
- [ ] **Implement `GET /rolls/{slug}` endpoint** — public, no auth required. Returns the roll result in a shareable format (suitable for link previews). Rate-limited.

---

### Area 8 — Observability

Every significant real-time operation must be traceable.

- [ ] **Structured log entries** for: session start/end, ST heartbeat renewal, auto-termination, player join/leave, dice roll broadcast, authorization failure.
- [ ] **OpenTelemetry metrics**: `requiem.sessions.active` (gauge), `requiem.sessions.players_connected` (gauge), `requiem.rolls.broadcast_total` (counter), `requiem.hub.dispatch_duration_ms` (histogram — this is what the 200ms budget tracks).
- [ ] **Correlation IDs** on all hub messages — the SignalR connection ID is included in every log entry as a structured property.

---

### Area 9 — Testing

| Test | Project | What it validates |
|---|---|---|
| Hub connection with valid cookie | `Application.Tests` | Authenticated users can connect |
| Hub connection rejection without cookie | `Application.Tests` | Unauthenticated connections are rejected |
| ST can start/end session | `Application.Tests` | Lifecycle happy path |
| Non-ST cannot start session | `Application.Tests` | Per-method authorization |
| Non-member cannot join | `Application.Tests` | Group join authorization |
| Dice roll broadcasts to all group members | `Application.Tests` | Group broadcast |
| Character update broadcast after REST mutation | `Application.Tests` | Outcome broadcast pattern |
| Player join hydrates with existing roll history | `Application.Tests` | Reconnect hydration |
| ST disconnect for 15 min terminates session | `Application.Tests` | `SessionTerminationService` |
| Rate limit rejects excess messages | `Application.Tests` | Hub throttling |
| Hub dispatch latency ≤ 200ms under 50 concurrent clients | `PerformanceTests` | NBomber — 200ms SLA |

All SignalR integration tests use `WebApplicationFactory` + `HubConnectionBuilder` pointed at the in-memory test server. No mocking of the hub transport.

---

### Area 10 — Documentation (Grimoire Learning Artifacts)

- [ ] **Create `src/RequiemNexus.Web/Hubs/README.md`** — the mandatory Learning Artifact for the SignalR subsystem:
  - *Why it exists* — why SignalR over polling, why Redis backplane, why typed hub
  - *Simple example* — a player joining and receiving a dice roll
  - *Intentionally wrong example* — what happens if you put game logic in the hub (the wrong pattern), and why it violates the Sacred Covenants
- [ ] **Update `docs/Architecture.md`** — refine the Real-Time Architecture section to reflect the final implementation decisions (typed hub, session state shape, dispatch metric definition).
- [ ] **Update `docs/Architecture.md`** — add a note under the PWA section documenting Phase 8 deferral and the WiFi assumption, per Antigravity Rule 10 ("every shortcut must be temporary and documented").
- [ ] **Update `docs/mission.md`** — mark Phase 7 as active (➡️), Phase 8 as ⏸️ Deferred with a one-line rationale.

---

### Area 11 — CI/CD

- [ ] **Add NBomber SignalR hub dispatch test** to the nightly performance workflow (`performance-nightly.yml`). Test: 50 concurrent clients, each triggering a dice roll broadcast, measuring dispatch latency. Fails if p95 > 200ms.
- [ ] **Update `CODEOWNERS`** — add `src/RequiemNexus.Web/Hubs/` to the owner-review paths.

---

## 📦 NuGet Packages Required

These must be explicitly approved before being added per `agents.md`:

| Package | Purpose | Notes |
|---|---|---|
| `Microsoft.AspNetCore.SignalR.StackExchangeRedis` | Redis backplane for SignalR | First-party Microsoft package. Required for multi-node ECS horizontal scaling. |
| `Microsoft.AspNetCore.SignalR.Client` | SignalR test client | Test-project-only. Required for `WebApplicationFactory` integration tests against the hub. |

No other new packages are anticipated.

---

## 🎓 Grimoire Moments (Teachable Patterns)

These are the intentional learning milestones in Phase 7:

| Pattern | Why it's here |
|---|---|
| Typed `Hub<ISessionClient>` | C# generics in SignalR — compile-time method name safety vs. magic strings |
| `IHubContext<THub, TClient>` injection | How the Application layer pushes to clients without knowing about HTTP — dependency inversion at the transport layer |
| Redis keyspace notifications | Event-driven expiry without polling — the right tool for the session timeout problem |
| Hub as thin relay | The Sacred Covenant applied to real-time: the hub is a transport boundary, not a business logic host |
| `RowVersion` optimistic concurrency | How EF Core handles concurrent writes without application-level locking |
| `IHostedService` for background work | The .NET pattern for long-running background tasks in ASP.NET Core — no Thread.Sleep, no fire-and-forget |

---

## 🏁 Exit Criteria

These must all be true before Phase 7 is declared complete:

- [ ] A full coterie can connect, roll dice, and see each other's results in real-time
- [ ] Storyteller actions propagate to all connected players within 200ms (server dispatch time, NBomber verified)
- [ ] Disconnected players rejoin and receive the current session state via REST snapshot
- [ ] A session auto-terminates after 15 minutes of ST disconnect, and all players are notified
- [ ] Unauthenticated connections are rejected; non-members cannot join a chronicle group; non-ST cannot invoke ST-only methods
- [ ] SignalR hub is protected against message flooding (rate limiting enforced)
- [ ] All integration tests covering connection, broadcast, authorization, and reconnection are passing
- [ ] NBomber hub dispatch performance test passes at ≤ 200ms p95 under 50 concurrent clients
- [ ] `src/RequiemNexus.Web/Hubs/README.md` Learning Artifact exists and is complete
- [ ] `dotnet build` is green with zero warnings
- [ ] `dotnet format --verify-no-changes` passes

---

### Area 12 — Realtime UI Components

These UI components are directly required by Phase 7 realtime features. They have no meaningful function without an active SignalR session. They are implemented in the Web layer and follow the same layer rules as all other Blazor components.

> **Dependency note:** Area 12c (Realtime Roll Toast) depends on the `ToastService` from `docs/ui-improvements-plan.md` Wave 1a. That toast infrastructure must be in place before Area 12c is wired to SignalR events.

- [ ] **12a — Session Presence Indicators** _(supports Area 4 — Session Presence)_
  - New: `src/RequiemNexus.Web/Components/UI/SessionPresenceBar.razor` + `.razor.css`
  - Displays avatar chips (player initials + online dot) for each entry in the Redis players SET
  - Updated in real-time via `ISessionClient.ReceivePresenceUpdate(players[])`
  - Embedded in the `CampaignDetails` page header when a session is active (conditional render)

- [ ] **12b — Dice Roll History Feed** _(supports Area 4 — Dice Roll History Feed)_
  - New: `src/RequiemNexus.Web/Components/UI/RollHistoryFeed.razor` + `.razor.css`
  - Scrollable, bottom-anchored feed (newest entry at bottom, auto-scroll)
  - Each entry: player name, pool description, success count, exceptional/dramatic badge, timestamp
  - New entries slide in from below (300ms ease); capped at 100 entries (matches Redis `LTRIM`)
  - Hydrated on join via `ISessionClient.ReceiveRollHistory`; updated live via `ISessionClient.ReceiveDiceRoll`

- [ ] **12c — Realtime Roll Toast** _(supports Area 4 — Live Dice Rolls)_
  - When a player rolls, all other clients in the session receive a toast: "🩸 [Name] rolled [N] success(es)"
  - Triggered from the `ISessionClient.ReceiveDiceRoll` handler — the roller does not see their own toast
  - Uses `ToastService` (see `docs/ui-improvements-plan.md`, Wave 1a) — that service must exist first

- [ ] **12d — Live Character Vitals** _(supports Area 4 — Real-Time Character Updates)_
  - `CharacterVitals.razor` subscribes to `ISessionClient.ReceiveCharacterUpdate(patch)` while a session is active
  - Health, Willpower, and Vitae values update and animate in-place without a page reload
  - Animation uses CSS transitions already defined in Wave 4 of `docs/ui-improvements-plan.md`
  - No new component required — extend existing `CharacterVitals.razor`

- [ ] **12e — Live Initiative Tracker** _(supports Area 4 — Shared Initiative Tracker)_
  - `InitiativeTracker.razor` subscribes to `ISessionClient.ReceiveInitiativeUpdate(entries[])`
  - On receipt, replaces the local C# initiative list and triggers Blazor re-render — no page reload
  - The current-actor pulsing spotlight (see `docs/ui-improvements-plan.md`, Wave 6) fires automatically when the ST advances the turn and the update is broadcast to all clients

---

> *The coterie connects. The blood flows in real time.*
