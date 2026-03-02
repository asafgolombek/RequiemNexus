# 🤝 Contributing to Requiem Nexus

Welcome to Requiem Nexus.

This is not just a codebase—it is a **teaching system**, an **architectural reference**, and a **philosophical stance**.

By contributing, you agree to uphold the **Antigravity Philosophy**.

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
