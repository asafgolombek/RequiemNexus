# Performance hot paths — static review

This document records a **static** review (no live trace) aligned with Phase 3 of the roadmap. Re-run profiling after major UI or schema changes.

## Character sheet (`CharacterDetails`)

- **Presentation**: [CharacterDetails.razor](../src/RequiemNexus.Web/Components/Pages/CharacterDetails.razor) and [CharacterDetails.razor.cs](../src/RequiemNexus.Web/Components/Pages/CharacterDetails.razor.cs) orchestrate many sections; large `StateHasChanged` fan-out increases render cost. Splitting into section components reduces re-render scope (see Phase 4 decomposition).
- **Data**: Verify the character load path uses a **single** Application/EF round-trip or explicit includes — watch for per-section service calls from the page `OnInitializedAsync` that could be batched.
- **Caching**: Per [Architecture.md](./Architecture.md), character-derived stats use a short Redis TTL; ensure mutations invalidate or bypass stale reads where correctness requires fresh data.

## Encounter tools (`EncounterManager`, `InitiativeTracker`)

- **Real-time**: Hub traffic and Redis session keys should stay bounded; avoid polling where SignalR push exists.
- **UI**: Long Razor files mix markup and event handlers; extracting subcomponents improves maintainability and allows targeted `ShouldRender` if profiling shows benefit.

## Campaign hub (`CampaignDetails`)

- Session heartbeat and hub connection logic should stay isolated from lore/roster loads so failures or timers do not trigger full page reloads unnecessarily.

## EF Core (general)

- Prefer `AsNoTracking()` on read-only queries (see comments in `AuthorizationHelper` for join-safe patterns).
- Avoid N+1: use `.Include()` / projections per Architecture.md query rules.

## Recommended profiling commands (local)

- `dotnet-counters monitor --counters System.Runtime` while exercising the character sheet.
- Visual Studio **CPU Usage** or `dotnet-trace collect` during encounter open and initiative updates.

## Baseline automation

- `.\scripts\run-performance.ps1` — requires a running Web app; enforces SignalR and `/health` p95 on non-localhost targets (see `tests/RequiemNexus.PerformanceTests/Program.cs`).
