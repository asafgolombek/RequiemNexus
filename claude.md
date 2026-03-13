@agents.md

## Session Orientation

This repository is both a working product and a learning artifact — the **Grimoire**.

When answering questions, default to Grimoire mode:
- Explain *why* architectural decisions were made, not just *what* they are.
- Connect C# 14 features (primary constructors, collection expressions) to the specific problems they solve.
- Reference `docs/Architecture.md` and `docs/mission.md` for architectural context.
- Reference the Antigravity Philosophy rules (in `agents.md`) when they apply.

**Currently active phase: Phase 6 — CI/CD Hardening & Supply Chain.**
See `docs/mission.md` for full phase status and scope boundaries.

## Quick Navigation

| Want to understand...  | Start here                                                          |
|------------------------|---------------------------------------------------------------------|
| Layer architecture     | `docs/Architecture.md`                                              |
| Current goals          | `docs/mission.md`                                                   |
| Domain rules           | `src/RequiemNexus.Domain/`                                          |
| Security pattern       | `src/RequiemNexus.Application/Services/AuthorizationHelper.cs`      |
| EF Core schema         | `src/RequiemNexus.Data/ApplicationDbContext.cs`                     |
| Test patterns          | `tests/RequiemNexus.Domain.Tests/`                                  |
| CI/CD                  | `.github/workflows/`                                                |
| Infrastructure (CDK)   | `infra/src/RequiemNexus.Infra/Stacks/`                              |
| Phase 6 workflows      | `.github/workflows/codeql.yml`, `container-scan.yml`, `dependabot-auto-merge.yml`, `performance-nightly.yml` |
| Release pipeline       | `.github/workflows/release.yml` (SBOM, Cosign signing, SLSA provenance) |
| Dependabot config      | `.github/dependabot.yml`                                            |
| Code owners            | `.github/CODEOWNERS`                                                |
