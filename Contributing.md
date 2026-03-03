# 🤝 Contributing to Requiem Nexus

Welcome to Requiem Nexus.

This is not just a codebase—it is a **teaching system**, an **architectural reference**, and a **philosophical stance**.

By contributing, you agree to uphold the **Antigravity Philosophy**.

> Before you contribute, read:
> - [🩸 Mission](./docs/mission.md) — _Why_ this project exists
> - [📐 Architecture](./docs/Architecture.md) — _How_ this project is structured

---

## 🪐 The Antigravity Pledge

All contributions must:

- Reduce cognitive load
- Increase clarity
- Preserve traceability

If your change makes the system harder to reason about, it will be rejected—even if it “works”.

---

## 🧠 Contribution Principles

1. **Explicit over clever**  
   Readability always wins.

2. **Understanding over speed**  
   Shipping fast is meaningless if the system becomes opaque.

3. **Traceability is mandatory**  
   Every change must be traceable from UI → logic → data.

4. **Teach through code**  
   Assume the next reader is learning.

---

## 🛠️ Development Setup

### Prerequisites

- .NET 10 SDK
- Docker
- PostgreSQL (or Dockerized equivalent)

### Local Startup

```bash
# For local development with hot reload and debugging tools
scripts/build-debug.ps1

# To verify production-ready optimizations locally
scripts/build-release.ps1
```

### 🧪 Testing Expectations

Before submitting a Pull Request, you must validate your changes locally:

```bash
# Runs all unit, integration, and E2E tests
scripts/test-local.ps1
```

If any tests fail, **fix them before opening the PR**. PRs with failing tests will not be reviewed.

### 🗄️ Database Configuration

When running locally, the application is configured to connect to a local development database. 
- Ensure your local connection strings in `appsettings.Development.json` or user secrets are configured correctly.
- Do not check production connection strings into source control.
- Ensure Docker is running if you rely on a containerized local database instance.

### 🚀 CI/CD & Pull Requests

We treat automation as a first-class citizen. 
All Pull Requests must pass automated GitHub Actions workflows which enforce:
- Successful compilation
- 100% passing test suites (Unit, Integration, E2E)
- Code formatting and style enforcement

Branches cannot be merged if any automated check fails.

---

## 🌿 Branching Strategy

All branches should be cut from `main` and follow the naming convention:

| Type | Pattern | Example |
|---|---|---|
| Feature | `feature/<short-description>` | `feature/xp-advancement-flow` |
| Bug Fix | `fix/<short-description>` | `fix/dice-roll-seeding` |
| Chore | `chore/<short-description>` | `chore/update-ef-core` |
| Documentation | `docs/<short-description>` | `docs/update-architecture` |

---

## 📝 Commit Message Conventions

All commit messages must follow [Conventional Commits](https://www.conventionalcommits.org/):

```
<type>(<scope>): <short summary>
```

Common types: `feat`, `fix`, `chore`, `docs`, `refactor`, `test`, `perf`

**Examples:**
```
feat(domain): add Touchstone to character advancement
fix(dice): correct 8-again explosion logic
docs(arch): update deployment topology section
test(integration): add EF Core migration validation test
```

This convention feeds into automated versioning and changelog generation in Phase 2+.

---

## 👁️ Code Review Standards

Every PR must receive at least one approving review before merging.

A valid approval confirms that:

- The change is **traceable** from UI → logic → data
- No new **implicit state** or hidden dependencies were introduced
- All new logic is **unit-tested** or has a documented reason for exemption
- Commit messages follow the conventions above

> If you are unsure whether an architectural exception is justified, document it explicitly in the PR description. Undocumented exceptions will be rejected.
