# Requiem Nexus — Agent Guide

This is the **first file an agent must read** before making any changes to this codebase.

---

## Agent Startup Checklist

Complete these steps **in order** before touching any file:

1. Read this file (`agents.md`) in full.
2. Read [`docs/mission.md`](./docs/mission.md) — understand the current phase and what is already done.
3. Read the existing tests for the area you are changing.
4. Run `dotnet build` — confirm the baseline is healthy.
5. Run `dotnet format --verify-no-changes` — confirm formatting is clean.

If you are blocked or requirements are ambiguous, **stop and surface the question explicitly. Do not guess. Do not proceed with a workaround.**

---

## Project Identity

**Requiem Nexus** is a cloud-native character and chronicle management platform for *Vampire: The Requiem (Chronicles of Darkness) Second Edition*, built on **.NET 10 / C# 14 / Blazor**.

It is also a **learning artifact — the Grimoire**. Every architectural decision is intentional, documented, and teachable. Agents must preserve that intent.

---

## Stack

| Concern | Technology |
|---------|------------|
| Framework | .NET 10, ASP.NET Core, Blazor |
| Language | C# 14 (Primary Constructors, Collection Expressions) |
| Architecture | Modular Monolith — `Application`, `Data`, `Domain`, `Web` |
| ORM | EF Core (SQLite local / PostgreSQL production) |
| Caching | Redis |
| Real-Time | SignalR |
| Orchestration | .NET Aspire |
| Deployment | Docker → AWS ECS Fargate |
| CI/CD | GitHub Actions |
| Observability | Serilog + OpenTelemetry |
| Code Style | StyleCop.Analyzers + `.editorconfig` |

---

## How to Run Locally

```powershell
.\scripts\build-debug.ps1     # Start full dev stack (hot reload via .NET Aspire)
.\scripts\test-local.ps1      # Unit + integration tests + format check (see script for scope)
.\scripts\run-performance.ps1  # NBomber load tests — requires a running app
```

A new developer or agent must be able to run the project in **under 10 minutes**.

---

## Architectural Laws (Sacred Covenants)

These are **non-negotiable**. If a task would require violating any of these laws, **STOP. Do not proceed with a workaround. Surface the conflict explicitly.**

### Layer Dependency Direction

```
Web (Presentation) → Application → Domain → Data (Infrastructure)
```

- `Domain` has **no dependency** on `Data` or `Web`.
- `Data` serves the Domain — it never drives it.
- `Web` holds **no business rules** and makes **no authorization decisions**.

### Forbidden Patterns

- **No shared `Common` or `Utils` projects.** Shared logic belongs in the Domain.
- **No cross-domain object references.** Cross-domain access is only via explicit contracts.
- **No `SELECT *` equivalents.** Always project only needed columns.
- **No N+1 queries.** Use `.Include()` or explicit projections.
- **No `#if DEBUG`.** Environment-specific behavior is driven by configuration, not compilation symbols.

### Error Handling

- Domain methods return `Result<T>` — **never throw** for expected business failures.
- Application layer translates `Result.Failure` into HTTP status codes or UI messages.
- Infrastructure catches external failures and wraps them in domain-friendly types.
- **No silent failures.** Every `catch` must log or rethrow.

### Security — The Masquerade

Every data-mutating operation must follow this exact sequence:

1. Extract the authenticated user's ID from the security context.
2. Load the target entity.
3. **Verify ownership** — reject with `403 Forbidden` if `entity.OwnerId != currentUserId`.
4. Proceed only after ownership is confirmed.

Skipping step 3 is a **security defect**, not a shortcut.

---

## C# Style Rules

- Use **C# 14 features**: primary constructors, collection expressions, `var` where type is obvious.
- **StyleCop is enforced in CI** — code must pass `dotnet format --verify-no-changes`.
- **XML doc comments** on all `public` members in `Domain` and `Application` services.

---

## File & Type Rules (Rule of One)

- **One type per file** — every `class`, `interface`, `record`, and `enum` lives in its own `.cs` file.
- **File name must exactly match the type name.**
- When creating a new type, **always create a new file**. Never add a type to an existing file.
- There are no exceptions. Violations fail the CI pipeline (`/warnaserror`).

---

## Agent Scope Boundaries

Agents must stay within the explicit scope of the task. Do not deviate without surfacing it first.

- **Do not refactor working code** unless explicitly asked.
- **Do not rename files or types** without explicit instruction — this affects the entire dependency graph.
- **Do not add NuGet packages** without explicit approval — every dependency is a liability.
- **Do not delete files** without explicit instruction.
- **Do not modify migrations** that have already been applied — create a corrective migration instead.
- **Do not open a PR** without running the full Inquisition (`.\scripts\test-local.ps1`) locally first.

---

## Database & Migration Rules

- All schema changes **require an EF Core migration**. Never hand-write SQL.
- Migrations are **forward-only** — no rollback via down-migration; create a corrective migration instead.
- **Breaking changes** (rename, type change) require a multi-step migration: add new → migrate data → remove old.
- Seed data lives in `DbInitializer` and must evolve alongside migrations.

---

## Testing Requirements

| Layer | Project | Approach |
|-------|---------|----------|
| Domain | `RequiemNexus.Domain.Tests` | Unit tests — deterministic, purely in-memory, no I/O |
| Application | `RequiemNexus.Application.Tests` | Integration tests — use cases, authorization flows, mocked infrastructure |
| Infrastructure | `RequiemNexus.Data.Tests` | Integration tests — Dockerized PostgreSQL |
| Performance | `RequiemNexus.PerformanceTests` | Load tests enforcing latency budgets |

> Performance budgets are defined and enforced per [Architecture.md](./docs/Architecture.md#-performance-architecture).

---

## Observability

Every new domain event or significant operation must:

- Emit a **structured Serilog log entry** (include a Correlation ID).
- Emit a **metric** via OpenTelemetry where applicable.
- Surface **Player-Safe Errors** to users (friendly message) while logging full diagnostics for developers.

---

## GitHub Actions Workflow Rules (Phase 6 — retained)

When authoring or modifying `.github/workflows/` files:

- Every workflow must declare an explicit minimal `permissions:` block — never omit it or use `write-all`.
- Trivy container scans must use `exit-code: '1'`, `severity: 'HIGH,CRITICAL'`, and `ignore-unfixed: true`.
- Dependabot auto-merge must never approve `major` version bumps — `patch` and `minor` only.
- SBOM generation uses the `dotnet CycloneDX` global tool targeting the `.slnx` solution file.
- Image signing uses `cosign sign --yes` (keyless OIDC) — requires `id-token: write` permission.
- Performance artifacts (`./NBomberData`) are uploaded with `retention-days: 90` for trend tracking.
- All artifact upload steps use `if: always()` so results are preserved even on failure.
- Changes to `.github/workflows/`, `infra/`, `src/RequiemNexus.Data/Migrations/`, and `src/RequiemNexus.Application/Services/AuthorizationHelper.cs` require owner review per `.github/CODEOWNERS`.

---

## SignalR Hub Rules (Phase 7)

These rules govern all work in `src/RequiemNexus.Web/Hubs/` and `src/RequiemNexus.Application/RealTime/`.

### The Hub is a Thin Relay

`SessionHub` must not contain business logic, authorization decisions, or Redis access. Its only job is to delegate to `ISessionService` and call `ISessionClient` methods. Any logic found in the hub is a Sacred Covenant violation.

### The Masquerade Applies to the Hub

Every hub method that reads or mutates state must follow the four Masquerade steps:
1. Extract the authenticated user's ID from `Context.UserIdentifier`.
2. Load the target entity (or verify session membership from Redis).
3. **Verify ownership / membership** — reject unauthorized callers with a `HubException`.
4. Proceed only after authorization is confirmed.

Unauthenticated connections must be rejected in `OnConnectedAsync`. There are no anonymous hub connections.

### Server-Side Dice Rolling Only

Dice are always rolled by `DiceService` on the server. The hub never accepts a client-supplied roll result. Client-reported rolls are a security defect.

### Session State Lives in Redis — Nowhere Else

Session data (active players, roll history, initiative, ST heartbeat) is stored only in Redis. It is never written to the PostgreSQL database. Session state is ephemeral by design.

### Redis Key Naming Convention

All session keys follow the pattern `session:{chronicleId}:{type}`:
- `session:{chronicleId}:info` — session metadata, TTL 15 min
- `session:{chronicleId}:players` — Redis SET of connected players
- `session:{chronicleId}:rolls` — Redis LIST of roll results (capped at 100)
- `session:{chronicleId}:initiative` — Redis ZSET scored by initiative value

No other key shapes are permitted without updating this document.

### Hub Dispatch Performance Budget

The p95 server dispatch latency (hub method invocation → message delivered to group) must remain ≤ 200ms. This is enforced by a NBomber test in the nightly performance workflow. A regression in this metric fails CI.

---

## Git & Branch Conventions

- **Branch naming:** `feature/{author}/{short-description}`
- **Commits:** [Conventional Commits](https://www.conventionalcommits.org/) — `feat:`, `fix:`, `refactor:`, `test:`, `docs:`, `chore:`
- **One logical change per commit.** Do not batch unrelated changes.
- **Never force-push to `main`.** All changes go through a PR.
- Run `dotnet format` and `dotnet build` before every commit.

---

## Secrets & Configuration

- **Never store secrets in `appsettings.json`** or any committed file.
- Use `dotnet user-secrets` for local overrides.
- Use environment variables for CI and production secrets.
- Placeholder values in `appsettings.json` (e.g. `"YOUR_MAILTRAP_PASSWORD"`) are documentation, not credentials.

---

## What Agents Must Always Do

- Verify ownership before any data-mutating operation (The Masquerade — all 4 steps).
- Emit a structured Serilog log entry for new domain events or significant operations.
- Add a unit test in `RequiemNexus.Domain.Tests` for any new domain logic.
- Run `dotnet format` before committing.
- Add XML doc comments to all `public` members in `Domain` and `Application`.
- Confirm `dotnet build` is green before declaring a task done.
- Create a new `.cs` file for every new type — never co-locate types.
- Stop and surface any ambiguity or architectural conflict before proceeding.

---

## What Agents Must Never Do

- Create a shared `Common` or `Utils` project.
- Add business logic to the `Web` layer.
- Access the database directly from the `Domain` layer.
- Skip ownership verification on any data-mutating endpoint.
- Throw exceptions for expected business failures in the Domain.
- Store secrets in any committed file.
- Use `#if DEBUG` or compilation-based environment branching.
- Write a down-migration and rely on it for rollback.
- Bypass StyleCop or `.editorconfig` rules.
- Swallow exceptions silently.
- Refactor, rename, or delete anything outside the explicit scope of the task.
- Add a NuGet dependency without explicit approval.
- Declare a task done without a green `dotnet build`.

---

## Stop Conditions

Stop immediately and surface the issue if:

- The task requires violating any Architectural Law.
- Requirements are ambiguous and proceeding would require guessing.
- The task scope is unclear (e.g., it is not obvious which layer owns the change).
- A test is failing and the fix is non-obvious.
- Adding the feature would require a new NuGet package.

**Do not work around blockers silently. Surface them.**

---

## Antigravity Philosophy — The Prime Directive

> *Systems must reduce cognitive weight, not add to it.*

When making any change, ask: *does this make the system easier or harder to understand?*

1. If it's implicit, it's a bug waiting to happen.
2. State must be visible or eliminable.
3. Magic is debt.
4. Traceability beats cleverness.
5. One reason to change per module (SRP).
6. No silent failure — ever.
7. Every subsystem must be teachable (the Grimoire).
8. If debugging is hard, the design is wrong.
9. Performance is a feature, not an optimization.
10. Every shortcut must be temporary — and documented.
11. If it isn't scripted or automated, it doesn't effectively exist.

---

## Glossary

| Term | Meaning |
|------|---------|
| **Requiem** | *Vampire: The Requiem* — the tabletop RPG this platform supports |
| **Chronicle** | A campaign or ongoing story; maps to a `Campaign` entity |
| **Covenant** | A vampire political faction; a character attribute |
| **Clan** | A vampire bloodline with distinct powers; maps to a `Clan` entity |
| **Discipline** | A supernatural power; maps to `Discipline` / `DisciplinePower` entities |
| **Merit** | A character advantage; maps to the `Merit` entity |
| **Touchstone** | A mortal anchor to a vampire's humanity; a character field |
| **Beats** | Experience point increments; maps to `Beats` on `Character` |
| **The Masquerade** | (1) The vampire secret from mortals; (2) the security/auth phase of this project |
| **Grimoire** | The project's role as a living learning artifact — every decision is teachable |
| **The Haven** | The local development environment |
| **The Inquisition** | Local unit + integration + format (`.\scripts\test-local.ps1`); browser E2E is `.\scripts\test-e2e-local.ps1` |

---

## Current Phase

**Phases 14–16a are complete** (combat & wounds, frenzy & torpor, feeding / hunting). **Phase 19 — The Blood Lineage is active 🔄** — read [`docs/phase19-the-blood-lineage.md`](./docs/phase19-the-blood-lineage.md) before touching anything in the Discipline, `CharacterDisciplineService`, `DbInitializer`, or `IAuthorizationHelper` areas. **Phase 16b** remains blocked on Phase 19 (`DisciplinePower.PoolDefinitionJson`). **Phase 17** (Humanity & Conditions) is independent and may run in parallel. **Phase 20 — The Global Embrace** is the last planned phase (i18n, public API, Discord, production polish). Phases 13, 12, and 8–11 are complete.

**Local E2E:** `scripts/test-e2e-local.ps1` (PostgreSQL + Playwright). **Inquisition (unit/integration):** `scripts/test-local.ps1`.

See [`docs/mission.md`](./docs/mission.md) for the feature list, phase table, and exit criteria.
See [`docs/phase19-the-blood-lineage.md`](./docs/phase19-the-blood-lineage.md) for the active Phase 19 implementation plan.
See [`docs/phase_8_plan.md`](./docs/phase_8_plan.md) for the Phase 8 implementation plan. Phase 9 plan retired after completion.

---

> *"The blood remembers. The code must too."*