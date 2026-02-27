# 🩸 Requiem Nexus

**Requiem Nexus** is a learning-driven, cloud-ready platform designed to eliminate the friction of character and campaign management for **Vampire: The Requiem (Chronicles of Darkness) second edition**.

## 🌌 Overview

Built with the **Antigravity philosophy** on **.NET 10**, Requiem Nexus provides a fast, secure, and intuitive digital ecosystem for Storytellers and Players alike.

### Key Features

- **The Living Sheet**: Auto-calculations for Health, Willpower, Defense, and Speed. Contextual rolling directly from the sheet.
- **Campaign Management**: Centralized Coterie Hub, shared lore management, and group XP allocation.
- **Storyteller Toolkit**: Real-time initiative tracker, private glimpse views, and encounter managers.
- **The Dice Nexus**: High-throughput rolling service supporting Chronicles of Darkness specific mechanics (10-again, 9-again, rote actions, etc.).

## 🚀 Getting Started

To get started with local development:

1. Use the provided build scripts in the `scripts/` directory.
2. Run `.\scripts\build-debug.ps1` to boot the application locally.
3. Ensure you have the appropriate .NET 10 SDK and Docker environment set up depending on your target configuration.

## 📚 Technical Stack

- **Framework**: .NET 10 (ASP.NET Core / EF Core)
- **Architecture**: Modular Monolith organized into domain-specific projects (`Data`, `Web`, `Roll`).
- **Deployment**: Containerized via Docker, cloud-agnostic, and orchestrated with .NET Aspire.

---

> _"The blood is the life... but the data is the legacy."_
