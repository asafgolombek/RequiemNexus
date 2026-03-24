# 🤝 Contributing to Requiem Nexus

Welcome to Requiem Nexus.

This is not just a codebase—it is a **teaching system**, an **architectural reference**, and a **philosophical stance**.

By contributing, you agree to uphold the **Antigravity Philosophy**.

> Before you invoke the code, read:
> - [🩸 Mission](./docs/mission.md) — _Why_ this project exists
> - [📐 Architecture](./docs/Architecture.md) — _How_ this project is structured

---

## 🪐 The Antigravity Pledge

All contributions must:
- Reduce cognitive weight, not add to it.
- Increase clarity and eliminate "magic".
- Preserve an unbroken chain of traceability.

If your change makes the system harder to reason about, it will be rejected—even if it "works".

---

## 🤝 Code of Conduct

We are committed to providing a welcoming and inspiring community for all. Please review our [Code of Conduct](./CODE_OF_CONDUCT.md) before contributing.

---

## 🧠 Contribution Principles

1. **Explicit over implicit**  
   Readability always wins. "Magic" or hidden logic is an architectural failure.
2. **Understanding over speed**  
   Shipping fast is meaningless if the system becomes a labyrinth. 
3. **Traceability is mandatory**  
   Every change must trace a clear lineage from Presentation → Domain → Persistence.
4. **Teach through code (The Grimoire)**  
   Every line of code is an intentional strike against technical debt. Write it as a lesson for the next reader.
5. **Automation is Documentation**  
   If a deployment, build, or test step isn't meticulously scripted in PowerShell or bound to a GitHub Action, it does not exist.

---

## 🛠️ The Haven (Development Setup)

Your local development environment is **"The Haven."** It must remain pure and predictable.

### Prerequisites
- .NET 10 SDK (Leveraging C# 14 primary constructors, collection expressions, etc.)
- Docker
- SQLite (local dev) or PostgreSQL (for parity testing / integration)

### Repository Structure

The project is organized into domain-specific modules. See the full layout in the [📁 Repository Structure](./docs/Architecture.md#-repository-structure) section of the Architecture guide.

```
src/
├── RequiemNexus.Data/      # Infrastructure Layer — EF Core, migrations, repositories
├── RequiemNexus.Domain/    # Domain Layer — game rules, models, invariants
└── RequiemNexus.Web/       # Presentation Layer — Blazor components, SignalR hubs
tests/
├── RequiemNexus.Domain.Tests/       # Unit tests
├── RequiemNexus.Data.Tests/         # Integration tests
└── RequiemNexus.PerformanceTests/   # Load and latency tests
```

### Local Startup

```powershell
# Invokes The Haven with hot reload and .NET Aspire orchestration
scripts/build-debug.ps1

# To verify production-ready optimizations locally
scripts/build-release.ps1
```

### 🧪 Testing Expectations (The Inquisition)

Before submitting a Pull Request, your code must survive the Inquisition locally:

```powershell
# Runs full unit, integration, and E2E validations
scripts/test-local.ps1
```

If any tests fail, **cleanse them before opening the PR**. PRs with failing tests will be rejected without review.

### 🎨 Code Formatting

Requiem Nexus enforces code style via `.editorconfig` and CI. Before committing:

```powershell
# Check for style violations (same check CI runs)
dotnet format --verify-no-changes

# Auto-fix style violations
dotnet format
```

PRs that fail `dotnet format --verify-no-changes` in CI will be blocked from merging.

### 📏 Code Style (The Rule of One)

Requiem Nexus enforces a strict **one type per file** policy (SA1402). 
- Every `class`, `interface`, `record`, and `enum` must live in its own `.cs` file.
- The file name must exactly match the type name.
- There are **no exceptions**. This reduces cognitive load and ensures that the filesystem accurately reflects the domain model.

PRs violating this rule will fail the CI pipeline as the build is configured with `/warnaserror`.

- Ensure your local connection strings in `appsettings.Development.json` or user secrets point to a pure local instance.
- **Never** commit production connection strings into source control.
- Local database files are local-only artifacts (e.g. `.data/`) and must be ignored via `.gitignore` (never committed).

### 🚀 CI/CD & The Automated Masquerade

We treat automation as a first-class citizen. 
All Pull Requests must pass automated GitHub Actions workflows enforcing:
- Successful compilation
- 100% passing test suites
- Code formatting and style enforcement (`dotnet format`)

Planned additions (Phase 6 roadmap) harden the supply chain and security gates:
- CodeQL scanning
- Dependabot updates
- Secret scanning / push protection
- Container image scanning
- SBOM generation and artifact signing/provenance for releases

Covenants cannot be merged if any automated check fails. 

---

## 🌿 Branching Strategy

Branches must be drawn from `main` with strict naming conventions, acting as a formalized audit trail.

| Type | Pattern | Example |
|---|---|---|
| Feature | `feature/<short-description>` | `feature/xp-advancement-flow` |
| Bug Fix | `fix/<short-description>` | `fix/dice-roll-seeding` |
| Chore | `chore/<short-description>` | `chore/update-ef-core` |
| Documentation | `docs/<short-description>` | `docs/update-architecture` |

---

## 📝 Commit Message Conventions

Commit messages must follow the [Conventional Commits](https://www.conventionalcommits.org/) format, tracking explicit intent:

```
<type>(<scope>): <short summary>
```

**Examples:**
```
feat(domain): add Touchstone to character advancement
fix(dice): correct 8-again explosion logic
docs(arch): update deployment topology section
test(integration): validate The Blood of the System mapping
```

---

## 📋 Pull Request Templates

We provide type-specific PR templates for focused reviews. When creating a PR, you can use:

| Type | Template | When to Use |
|------|----------|-------------|
| **Feature** | `feature.md` | New functionality |
| **Bug Fix** | `bugfix.md` | Fixing a defect |
| **Chore** | `chore.md` | Maintenance, dependencies, refactors |
| **Docs** | `docs.md` | Documentation / Grimoire updates |

Append `&template=feature.md` (or the appropriate template name) to the PR creation URL, or select the template from the GitHub UI.

If none of the specific templates fit, the default template will be used automatically.

---

## 🧭 First-Time Contributors

Not sure where to start? Here's your path:

1. **Read the [Mission](./docs/mission.md)** — understand the *why*.
2. **Read the [Architecture](./docs/Architecture.md)** — understand the *how*.
3. **Check the [Issues](../../issues)** — look for issues labeled `good first issue`.
4. **Check the mission roadmap** — the Phase checklist in `mission.md` shows what's in progress and what's coming next. Phase 13 work is guided by [PHASE_13_E2E_ACCESSIBILITY.md](./docs/PHASE_13_E2E_ACCESSIBILITY.md) (E2E, accessibility, screen reader, visual regression).
5. **Set up The Haven** — follow the Development Setup section above.
6. **Pick something small** — a documentation fix, a missing test, or a `good first issue` is perfect for your first PR.

---

## 👁️ Code Review Standards (The PR Inquisition)

Every PR must receive at least one approving review before merging. Reviewers are Inquisitors ensuring architectural purity.

A valid approval confirms that:
- The change provides a **Traceability Report** linking UI → Domain Logic → Data Persistence.
- No new **implicit magic**, shared common libraries, or hidden dependencies infected the system.
- C# 14 technical milestones are properly utilized as clear learning artifacts.
- Automation scripts reflect any new manual steps.
- `dotnet format` passes with no violations.

> If you seek an architectural exemption, it must be documented explicitly in the PR. Undocumented deviations are inherently rejected.

---

> _"Every contribution is a strike against entropy. Make it count."_
