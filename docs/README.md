# 🩸 Requiem Nexus

[![Build](https://github.com/asafgolombek/RequiemNexus/actions/workflows/ci.yml/badge.svg)](https://github.com/asafgolombek/RequiemNexus/actions)
![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)
![C# 14](https://img.shields.io/badge/C%23-14-239120?logo=csharp)
![License](https://img.shields.io/badge/license-MIT-blue)

**Requiem Nexus** is a learning-driven, cloud-native platform that eliminates the friction of character and chronicle management for **Vampire: The Requiem (Chronicles of Darkness) Second Edition**.

Built with the **Antigravity Philosophy** on **.NET 10**, it provides a fast, secure, and immersive digital ecosystem where TTRPG tools disappear during play — leaving only the story. The UI is forged in a **Modern Gothic Aesthetic** (bone-white and crimson), ensuring the digital environment matches the nocturnal atmosphere of the world.

> _"The blood is the life… but clarity is the power."_

---

## 🌌 Who Is This For?

- **🧛 Players** — Manage characters, roll dice, track Conditions and Aspirations.
- **🎭 Storytellers** — Run chronicles, manage NPCs, distribute XP, and track the Danse Macabre.
- **🧙 Developers (The Apprentice)** — Learn modern .NET architecture through a real-world project.

---

## 🎯 Key Features

- **The Living Sheet** — Auto-calculated Health, Willpower, Defense, and Speed. Tap-to-roll directly from the sheet.
- **Beat & Experience Ledger** — Immutable transaction history of Beats earned and XP spent. No more lost bookkeeping.
- **Condition & Tilt Tracker** — First-class tracking with one-tap resolution to automatically award Beats.
- **Campaign Management** — Coterie Hub, shared lore, NPC databases, and group XP allocation.
- **Coterie & Domain Mapping** — Track feeding territories, city power structures, Touchstones, and NPC relationships.
- **Storyteller Toolkit** — Initiative tracker, encounter manager, private Glimpse dashboard, and NPC stat blocks.
- **The Dice Nexus** — High-throughput dice rolling with 10-again, 9-again, 8-again, rote actions, and deterministic seeded replay.

---

## 🚀 Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/get-started)

### Quick Start

**Ensure Docker Desktop is running** before proceeding.

```powershell
# Clone the repository
git clone https://github.com/asafgolombek/RequiemNexus.git
cd RequiemNexus

# Start local infrastructure (PostgreSQL & Redis via Docker)
docker compose up -d

# Initialize/Update local database schema
dotnet ef database update --project src/RequiemNexus.Data --startup-project src/RequiemNexus.Web

# Boot The Haven (local dev with hot reload via .NET Aspire)
.\scripts\build-debug.ps1

# Run the Inquisition (full test suite)
.\scripts\test-local.ps1
```

A new developer should be able to run the project locally in **under 10 minutes**. No manual database setup or configuration is required; `docker compose` will provision everything automatically.

---

## 📚 Technical Stack

| Component | Technology |
|-----------|------------|
| **Framework** | .NET 10 (ASP.NET Core, Blazor, EF Core) |
| **Language** | C# 14 (Primary Constructors, Collection Expressions) |
| **Architecture** | Modular Monolith — `Web`, `Application`, `Domain`, `Data` |
| **Real-Time** | SignalR |
| **Database** | PostgreSQL (production), SQLite (local dev) |
| **Caching** | Redis |
| **Orchestration** | .NET Aspire |
| **Deployment** | Docker → AWS ECS Fargate (IaC via AWS CDK in Phase 5) |
| **CI/CD** | GitHub Actions |
| **Observability** | Serilog + OpenTelemetry |

---

## 📐 Architecture

Requiem Nexus follows the **Antigravity Philosophy**: systems must reduce cognitive weight, not add to it.

The project is a **Modular Monolith** with strict layer boundaries:

```
Presentation (Web) → Application Layer → Domain Layer → Infrastructure (Data)
```

Dependencies always point inward. Infrastructure is a plugin to the domain, never the reverse.

→ Read the full [Architecture Guide](./Architecture.md)

---

## 📁 Project Structure

```
RequiemNexus/
├── .github/                  # CI workflows, PR & issue templates
├── docs/                     # Architecture and Mission documents
├── scripts/                  # PowerShell automation (build, test, deploy)
├── src/
│   ├── RequiemNexus.Application/ # Application — orchestrates domain logic
│   ├── RequiemNexus.Data/        # Infrastructure — EF Core, migrations, repositories
│   ├── RequiemNexus.Domain/      # Domain — game rules, models, invariants
│   └── RequiemNexus.Web/         # Presentation — Blazor components, SignalR hubs, gothic design system
└── tests/
    ├── RequiemNexus.Domain.Tests/       # Unit tests
    ├── RequiemNexus.Data.Tests/         # Integration tests
    └── RequiemNexus.PerformanceTests/   # Load & latency tests
```

---

## 🗺️ Roadmap

| Phase | Name | Status |
|-------|------|--------|
| 1 | **The Neonate** — Character system & dice rolling | ✅ Complete |
| 2 | **The Ascendant** — Validation, CI/CD, testing | ✅ Complete |
| 3 | **The Masquerade Veil** — Account management & security | ✅ Complete |
| 4 | **The Storyteller & The Danse Macabre** — Chronicle & ST tools | ✅ Complete |
| 5 | **Automated Deployments & Observability** | ✅ Complete |
| 6 | **CI/CD Hardening & Supply Chain** | ✅ Complete |
| 7 | **The Blood Communion** — Realtime play | ✅ Complete |
| 8 | **The Hidden Blood** — Bloodlines & Devotions | ✅ Complete |
| 9 | **The Accord of Power** — Covenants & Blood Sorcery | ✅ Complete |
| 9.5 | **Sacrifice Mechanics** — Blood Sorcery | ✅ Complete |
| 9.6 | **Necromancy & Ordo Dracul** — Additional traditions | ✅ Complete |
| 10 | **The Social Graces** — Social Maneuvering | ✅ Complete |
| 11 | **Assets & Armory** — Equipment & Services | ✅ Complete |
| 12 | **The Web of Night** — Relationship Webs | ⬜ Planned |
| 13 | **End-to-End Testing & Accessibility** | ⬜ Planned |
| 14 | **The Global Embrace** — i18n, public API | ⬜ Planned |

→ See the full roadmap with details in the [Mission Document](./mission.md)

---

## 🤝 Contributing

We welcome contributions! Please read the [Contributing Guide](../Contributing.md) before submitting a PR.

All contributions must uphold the **Antigravity Pledge**:
- Reduce cognitive weight, not add to it.
- Increase clarity and eliminate "magic".
- Preserve an unbroken chain of traceability.

---

## 🛡️ Security

Found a vulnerability? Please report it responsibly. See our [Security Policy](./SECURITY.md).

**Do not** open a public issue for security vulnerabilities.

---

## 🤝 Code of Conduct

Help us keep Requiem Nexus a welcoming environment. Please read our [Code of Conduct](./CODE_OF_CONDUCT.md) before interacting with the community.

---

## 📄 License

This project is licensed under the [MIT License](../LICENSE).

---

> _"The blood remembers. The code must too."_
