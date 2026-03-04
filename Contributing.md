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

If your change makes the system harder to reason about, it will be rejected—even if it “works”.

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
- PostgreSQL (The Blood of the System)

### Local Startup

```bash
# Invokes The Haven with hot reload and .NET Aspire orchestration
scripts/build-debug.ps1

# To verify production-ready optimizations locally
scripts/build-release.ps1
```

### 🧪 Testing Expectations (The Inquisition)

Before submitting a Pull Request, your code must survive the Inquisition locally:

```bash
# Runs full unit, integration, and E2E validations
scripts/test-local.ps1
```

If any tests fail, **cleanse them before opening the PR**. PRs with failing tests will be rejected without review.

### 🗄️ Database Configuration (The Blood of the System)

- Ensure your local connection strings in `appsettings.Development.json` or user secrets point to a pure local instance.
- **Never** commit production connection strings into source control.

### 🚀 CI/CD & The Automated Masquerade

We treat automation as a first-class citizen. 
All Pull Requests must pass automated GitHub Actions workflows enforcing:
- Successful compilation
- 100% passing test suites
- Code formatting and style enforcement

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

## 👁️ Code Review Standards (The PR Inquisition)

Every PR must receive at least one approving review before merging. Reviewers are Inquisitors ensuring architectural purity.

A valid approval confirms that:
- The change provides a **Traceability Report** linking UI → Domain Logic → Data Persistence.
- No new **implicit magic**, shared common libraries, or hidden dependencies infected the system.
- C# 14 technical milestones are properly utilized as clear learning artifacts.
- Automation scripts reflect any new manual steps. 

> If you seek an architectural exemption, it must be documented explicitly in the PR. Undocumented deviations are inherently rejected.
