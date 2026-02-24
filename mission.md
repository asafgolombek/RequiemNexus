# 🩸 Project: Requiem Nexus

## 🌌 The Mission

To build the definitive, high-performance digital ecosystem for **Vampire: The Requiem (Chronicles of Darkness)**.

**Requiem Nexus** is designed to eliminate the friction of character management. By leveraging the reactive power of **.NET 10** and the **Antigravity philosophy**, we provide a "Beyond"-style experience that is as fast as a digital app but as deep as a leather-bound sourcebook.

---

## 🏗️ Architectural Excellence (.NET 10)

- **Modular Monolith:** Logic is partitioned into domain-specific projects (`Data`, `Web`, `Roll`) to allow for future Microservice extraction.
- **Service Orchestration:** Utilize **.NET Aspire** to manage local resources (PostgreSQL, Redis) and service discovery.
- **Focus on Scaling Up:**
  - **State Management:** Use distributed caching (Redis) to ensure SignalR sessions can persist across multiple server instances.
  - **Stateless Logic:** Keep the Rules Engine stateless; calculate derived stats on-demand rather than storing redundant data.
- **Observability:** Integrated Health Checks and OpenTelemetry to monitor "Vital Signs," ensuring low latency and high availability.

---

## 🛡️ The Masquerade (Security)

Data integrity and player privacy are our highest priorities.

- **Zero-Trust Identity:** Implement **OpenID Connect (OIDC)** with short-lived JWTs. Every internal service call requires identity verification.
- **BOLA/IDOR Prevention:** Strict ownership checks ensure a user can only access a character if they are the owner or a designated Storyteller.
- **Input Sanitization:** "Trust no one." All inputs are validated via strict C# Type-checking and parameterized queries to prevent SQL Injection.
- **Privacy:** Minimum Viable Data collection; all sensitive player and chronicle data is encrypted at rest.

---

## 🎯 Key Objectives

### 1. The Living Sheet

- **Auto-Calculations:** Instant updates for Health, Willpower, Defense, and Speed using .NET 10 performance optimizations.
- **Tactile UI:** A bone-white and crimson interface optimized for mobile and desktop, adhering to "Modern Gothic" aesthetics.

### 2. Campaign Management (The Chronicle Nexus)

- **Coterie Hub:** A centralized space for players to link characters to a shared Chronicle.
- **Shared Lore:** Storytellers can upload and share "public knowledge" notes, NPCs, and location descriptions.
- **XP Allocation:** Storytellers can grant XP to the entire group or specific individuals directly from the dashboard.

### 3. Storyteller Toolkit

- **Initiative Tracker:** A real-time tracker handling the unique "Initiative Mod" mechanics of Chronicles of Darkness.
- **ST Oversight:** Private "Glimpse" view allowing Storytellers to see player Health, Willpower, and Conditions at a glance without switching tabs.
- **Encounter Manager:** Pre-build NPC stat-blocks (Ghouls, Strix, rival Kindred) for instant deployment during sessions.

### 4. The XP Ledger

- **Immutable Logs:** Audit every dot increased or merit purchased.
- **Rule Enforcement:** Backend validation prevents "illegal" character builds according to _Chronicles of Darkness_ rules.

### 5. The Dice Nexus

- **High-Throughput:** A specialized rolling service supporting 10-again, 9-again, 8-again, and rote actions.

---

## 🧪 DevOps & Automation

- **Local-First Development:** The project must be bootable via a single script (`build-debug.ps1`) to ensure environment parity.
- **Database Governance:** All schema changes must be accompanied by a Migration and a corresponding update to the `DbInitializer` for local seeding.
- **CI/CD:** Automated builds verify every commit to the `src/` directory to ensure the build remains "Green".

---

## 📅 Phase 1: The Neonate

- [x] Initialize **.NET 10** modular project structure.
- [x] Manifest local database schema via **EF Core Migrations**.
- [ ] Implement `DbInitializer` for automatic Clan and Admin seeding.
- [ ] Build the reactive `DotScale` component for Attributes and Skills.
- [ ] Finalize the high-performance **Dice Nexus** service.

---

> _"The blood is the life... but the data is the legacy."_
