@agents.md

## Session Orientation

This repository is both a working product and a learning artifact — the **Grimoire**.

When answering questions, default to Grimoire mode:
- Explain *why* architectural decisions were made, not just *what* they are.
- Connect C# 14 features (primary constructors, collection expressions) to the specific problems they solve.
- Reference `docs/Architecture.md` and `docs/mission.md` for architectural context.
- Reference the Antigravity Philosophy rules (in `agents.md`) when they apply.

**Currently active phase: Phase 7 — Realtime Play (The Blood Communion).**
See `docs/mission.md` for full phase status and scope boundaries.
See `docs/plan.md` for the detailed Phase 7 implementation plan.

## Quick Navigation

| Want to understand...  | Start here                                                          |
|------------------------|---------------------------------------------------------------------|
| Layer architecture     | `docs/Architecture.md`                                              |
| Current goals          | `docs/mission.md`                                                   |
| Phase 7 plan           | `docs/plan.md`                                                      |
| Domain rules           | `src/RequiemNexus.Domain/`                                          |
| Security pattern       | `src/RequiemNexus.Application/Services/AuthorizationHelper.cs`      |
| EF Core schema         | `src/RequiemNexus.Data/ApplicationDbContext.cs`                     |
| Test patterns          | `tests/RequiemNexus.Domain.Tests/`                                  |
| CI/CD                  | `.github/workflows/`                                                |
| Infrastructure (CDK)   | `infra/src/RequiemNexus.Infra/Stacks/`                              |
| SignalR hub            | `src/RequiemNexus.Web/Hubs/SessionHub.cs`                           |
| Session service        | `src/RequiemNexus.Application/RealTime/SessionService.cs`           |
| Session state (Redis)  | `src/RequiemNexus.Data/RealTime/SessionStateRepository.cs`          |
| Hub client contract    | `src/RequiemNexus.Application/RealTime/ISessionClient.cs`           |
| Release pipeline       | `.github/workflows/release.yml` (SBOM, Cosign signing, SLSA provenance) |
| Dependabot config      | `.github/dependabot.yml`                                            |
| Code owners            | `.github/CODEOWNERS`                                                |
