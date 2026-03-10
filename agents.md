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

If you are blocked or requirements are ambiguous, **stop and surface the question explicitly. Do not guess.**

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
.\scripts\build-debug.ps1   # Start full dev stack (hot reload via .NET Aspire)
.\scripts\test-local.ps1    # Run full test suite before opening a PR
```

A new developer or agent must be able to run the project in **under 10 minutes**.

---

## Architectural Laws (Sacred Covenants)

These are **non-negotiable**. Do not violate them.

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

Every data-mutating operation must:

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

- Verify ownership before any data-mutating operation.
- Emit a structured Serilog log entry for new domain events or significant operations.
- Add a unit test in `RequiemNexus.Domain.Tests` for any new domain logic.
- Run `dotnet format` before committing.
- Add XML doc comments to all `public` members in `Domain` and `Application`.
- Confirm `dotnet build` is green before declaring a task done.

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

---

## Current Phase

See [`docs/mission.md`](./docs/mission.md) — it is the single source of truth for active goals and completion status.

---

> *"The blood remembers. The code must too."*