# Requiem Nexus — Agent Guide

This document defines how AI coding agents (Claude Code, Copilot, etc.) should operate within this codebase. It is the **first file an agent should read** before making any changes.

## Agent Startup Checklist

Before touching any file, complete these steps in order:

1. Read this file (`agents.md`) in full.
2. Read [`docs/mission.md`](./docs/mission.md) — understand the current phase and what is already done.
3. Read the existing tests for the area you are changing.
4. Run `dotnet build` to confirm the baseline is healthy before you introduce any changes.
5. Run `dotnet format --verify-no-changes` to confirm formatting is clean.

If you are blocked or the requirements are ambiguous, stop and surface the question explicitly. Do not guess.

---

## Project Identity

**Requiem Nexus** is a cloud-native character and chronicle management platform for *Vampire: The Requiem (Chronicles of Darkness) Second Edition*, built on **.NET 10 / C# 14 / Blazor**.

It is also a **learning artifact** — the Grimoire. Every architectural decision is intentional, documented, and teachable. Agents must preserve that intent.

---

## Stack at a Glance

| Concern | Technology |
|---------|------------|
| Framework | .NET 10, ASP.NET Core, Blazor |
| Language | C# 14 (Primary Constructors, Collection Expressions) |
| Architecture | Modular Monolith — `Application`, `Data`, `Domain`, `Web` |
| ORM | EF Core (SQLite local, PostgreSQL production) |
| Caching | Redis |
| Real-Time | SignalR |
| Orchestration | .NET Aspire |
| Deployment | Docker → AWS ECS Fargate |
| CI/CD | GitHub Actions |
| Observability | Serilog + OpenTelemetry |
| Code Style | StyleCop.Analyzers + `.editorconfig` |

---

## Repository Layout

```
RequiemNexus/
├── .github/            # Workflows, PR templates, issue templates
├── docs/               # Architecture.md, mission.md
├── scripts/            # PowerShell automation
├── src/
│   ├── RequiemNexus.Application/  # Application services, use-case orchestration, contracts
│   ├── RequiemNexus.Data/         # Infrastructure — EF Core, migrations, repositories
│   ├── RequiemNexus.Domain/       # Domain — game rules, models, invariants
│   └── RequiemNexus.Web/          # Presentation — Blazor components, SignalR hubs
└── tests/
    ├── RequiemNexus.Domain.Tests/      # Unit tests (deterministic, in-memory)
    ├── RequiemNexus.Data.Tests/        # Integration tests (EF Core, Dockerized DB)
    └── RequiemNexus.PerformanceTests/  # Load and latency tests
```

---

## How to Run Locally

```powershell
# Start the full dev stack (hot reload via .NET Aspire)
.\scripts\build-debug.ps1

# Run the full test suite before opening a PR
.\scripts\test-local.ps1
```

A new developer (or agent) must be able to run the project in **under 10 minutes**.

---

## Git & Branch Conventions

- **Branch naming:** `feature/{author}/{short-description}` — e.g., `feature/jdoe/add-totp-enrollment`
- **Commit messages:** [Conventional Commits](https://www.conventionalcommits.org/) — `feat:`, `fix:`, `refactor:`, `test:`, `docs:`, `chore:`
- **One logical change per commit.** Do not batch unrelated changes.
- **Never force-push to `main`.** All changes go through a PR.
- Run `dotnet format` and `dotnet build` before committing.

---

## Secrets & Local Configuration

- **Never store secrets in `appsettings.json`** or any file committed to the repository.
- Use `dotnet user-secrets` for local development overrides (e.g., SMTP credentials, connection strings).
- Use environment variables for CI and production secrets.
- `appsettings.json` may contain placeholder values (e.g., `"YOUR_MAILTRAP_PASSWORD"`) — these are documentation, not live credentials.

---

## Architectural Laws (Sacred Covenants)

These are **non-negotiable**. Agents must not violate them.

### Layer Dependency Direction

```
Presentation (Web) → Application Layer → Domain Layer → Infrastructure (Data)
```

- The `Domain` layer has **no dependency** on `Data` or `Web`.
- The `Data` layer **serves** the Domain; it never drives it.
- The `Web` layer holds **no business rules** and makes **no authorization decisions**.

### No Shared "Common" Projects

There is no `RequiemNexus.Common` or `RequiemNexus.Utils` project. Shared-dumping-ground projects are **forbidden**. If logic is shared, it belongs in the Domain.

### Domain Sovereignty

Each domain owns its models, invariants, and persistence mappings. Cross-domain access is only via **explicit contracts**. Never reach across domain boundaries via direct object reference.

### Error Handling

- Domain methods return `Result<T>` — they **never throw** for expected business failures.
- Application layer translates `Result.Failure` into HTTP status codes or UI messages.
- Infrastructure catches external failures and wraps them in domain-friendly types.
- **No silent failures.** Every `catch` must log or rethrow.

### Security (The Masquerade)

Every data-mutating operation must:
1. Extract the authenticated user's ID from the security context.
2. Load the target entity.
3. **Verify ownership** — reject with `403 Forbidden` if `entity.OwnerId != currentUserId`.
4. Proceed only after ownership is confirmed.

Skipping step 3 is a security defect, not a shortcut.

---

## C# Style Rules

- **Use C# 14 features**: primary constructors, collection expressions, `var` where type is obvious.
- **StyleCop is enforced in CI** — code must pass `dotnet format --verify-no-changes`.
- **No `#if DEBUG`** — environment-specific behavior is driven by configuration, not compilation symbols.
- **No `SELECT *` equivalents** — always project only needed columns.
- **No N+1 queries** — use `.Include()` or explicit projections.
- **XML doc comments** on all `public` members in `Domain` and application services.

---

## Database & Migration Rules

- All schema changes **require an EF Core migration**. Never hand-write SQL against the database.
- Migrations are **forward-only** — no rollback via down-migration. Create a corrective migration instead.
- **Breaking changes** (rename, type change) require a multi-step migration: add new → migrate data → remove old.
- Seed data lives in `DbInitializer` and must evolve alongside migrations.
- CI runs every migration against an empty database to verify clean application.

---

## Testing Requirements

| Layer | Project | Approach |
|-------|---------|----------|
| Domain | `RequiemNexus.Domain.Tests` | Unit tests — deterministic, purely in-memory, no I/O |
| Infrastructure | `RequiemNexus.Data.Tests` | Integration tests — Dockerized PostgreSQL |
| Performance | `RequiemNexus.PerformanceTests` | Load tests enforcing latency budgets |

**Performance budgets (enforced in CI via [NBomber](https://nbomber.com/)):**
- Dice rolls: **< 200ms**
- Character sheet TTI: **< 1.5s**
- API response (p95): **< 300ms**

Performance tests live in `tests/RequiemNexus.PerformanceTests/`. The target URL is configured via the `TARGET_URL` environment variable (defaults to `http://localhost:5000`).

---

## Observability Requirements

Every new domain event or significant operation must:
- Emit a **structured Serilog log entry** (include a Correlation ID).
- Emit a **metric** via OpenTelemetry where applicable.
- Surface **Player-Safe Errors** to users (friendly message) while logging full diagnostics for developers.

---

## CI Checks (Must Pass Before Merge)

All of these run automatically on every PR:
- Build (`dotnet build`)
- Format verification (`dotnet format --verify-no-changes`)
- Full test suite including unit and integration tests
- Code coverage threshold enforcement
- Security and NuGet vulnerability scanning
- Migration validation (run against an empty DB)

**Never use `--no-verify` to skip hooks.**

---

## Antigravity Philosophy (The Prime Directive)

> Systems must reduce cognitive weight, not add to it.

When making any change, ask: *does this make the system easier or harder to understand?*

Key rules of thumb:
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

## Current Phase

See [`docs/mission.md`](./docs/mission.md) for the active phase, its goals, and the completion checklist. That file is the single source of truth — this section intentionally contains no duplicated detail to avoid drift.

---

## What Agents Should Always Do

- Verify ownership before any data-mutating operation.
- Emit a structured Serilog log entry for new domain events or significant operations.
- Add a unit test in `RequiemNexus.Domain.Tests` for any new domain logic.
- Run `dotnet format` before committing — StyleCop failures block CI.
- Add XML doc comments to all `public` members in `Domain` and `Application` services.
- Confirm the build is green before declaring a task done (`dotnet build`).

---

## What Agents Should Never Do

- Create a shared "Common" or "Utils" project.
- Add business logic to the `Web` (Presentation) layer.
- Access the database directly from the `Domain` layer.
- Skip ownership verification on any data-mutating endpoint.
- Throw exceptions for expected business failures in the Domain.
- Store secrets in `appsettings.json` or any file committed to the repository.
- Use `#if DEBUG` or other compilation-based environment branching.
- Write a migration down-migration and rely on it for rollback.
- Bypass StyleCop or editorconfig rules.
- Swallow exceptions silently.

---

## Glossary

Game-world terminology used as code identifiers — do not confuse with framework concepts.

| Term | Meaning in Context |
|------|--------------------|
| **Requiem** | *Vampire: The Requiem* — the tabletop RPG this platform supports |
| **Chronicle** | A campaign or ongoing story; maps to a `Campaign` entity |
| **Covenant** | A vampire political faction (e.g., Invictus, Circle of the Crone); a character attribute |
| **Clan** | A vampire bloodline with distinct powers; maps to a `Clan` entity |
| **Discipline** | A supernatural power; maps to `Discipline` / `DisciplinePower` entities |
| **Merit** | A character advantage; maps to the `Merit` entity |
| **Touchstone** | A mortal anchor to a vampire's humanity; a character field |
| **Beats** | Experience point increments in Chronicles of Darkness; maps to `Beats` on `Character` |
| **The Masquerade** | (1) The vampire secret kept from mortals; (2) the security/auth phase of this project |
| **Grimoire** | The project's role as a living learning artifact — every decision is teachable |

---

> _"The blood remembers. The code must too."_
