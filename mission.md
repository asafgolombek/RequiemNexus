# 🩸 Project: Requiem Nexus

## 🌌 The Mission

To build the definitive, high-performance digital ecosystem for **Vampire: The Requiem (Chronicles of Darkness) second edition**.

**Requiem Nexus** is a learning-driven, cloud-ready platform designed to eliminate the friction of character and campaign management. By leveraging the reactive power of **.NET 10** and the **Antigravity philosophy**, we provide a "Beyond"-style experience that is fast, secure, and infinitely scalable.

---

## 📚 1. Educational Core (The Grimoire)

Every architectural choice is a learning milestone. We prioritize "Understanding over Magic."

- **Reactive Patterns:** Master how real-time state changes are handled without page refreshes using C# state management.
- **ORM Mastery:** Use **EF Core** to understand relational mapping, migrations, and high-performance querying transitioning from SQLite to PostgreSQL.
- **Identity & Security:** Deep dive into **ASP.NET Core Identity** to learn JWTs, Claims-based authorization, and data privacy at an enterprise level.

---

## ☁️ 2. Cloud-Native & Deployment (The Global Nexus)

The app is built to be "Cloud-Agnostic" for easy deployment to **Azure**, **AWS**, or **Railway**.

- **Containerization:** All services are containerized using **Docker** to ensure environment parity between local development and the cloud.
- **Modular Monolith:** Logic is partitioned into domain-specific projects (`Data`, `Web`, `Roll`) to allow for future Microservice extraction.
- **Service Orchestration:** Utilize **.NET Aspire** to manage local resources and service discovery, making cloud transitions seamless.
- **Stateless Scaling:** The rules engine is kept stateless; sessions and character state use distributed caching (Redis) for horizontal scaling.

---

## 🎨 3. UI/UX: Intuitive Immersion (The Masquerade)

TTRPG tools should vanish into the background during roleplay.

- **Modern Gothic Aesthetic:** A bone-white and crimson interface optimized for dark-mode environments to reduce eye strain.
- **The 3-Click Rule:** No core function (rolling dice, spending XP, checking a merit) should be more than three clicks away from the dashboard.
- **Mobile-First Responsiveness:** Optimized for tablets and phones, allowing Storytellers to track initiative while players manage sheets on mobile.
- **Tactile Feedback:** Use reactive components and subtle animations to make digital character growth feel as satisfying as marking a paper sheet.

---

## 🛡️ 4. Security & Data Integrity

- **Zero-Trust Identity:** Implement **OpenID Connect (OIDC)** with short-lived JWTs. Every internal service call requires identity verification.
- **BOLA/IDOR Prevention:** Strict ownership checks ensure a user can only access a character if they are the owner or a designated Storyteller.
- **Input Sanitization:** All inputs are validated via strict C# Type-checking and parameterized queries to prevent SQL Injection.
- **Privacy:** Minimum Viable Data collection; all sensitive player and chronicle data is encrypted at rest.

---

## 🎯 Key Objectives

### 1. The Living Sheet

- **Auto-Calculations:** Instant updates for Health, Willpower, Defense, and Speed using .NET 10 performance optimizations.
- **Contextual Rolling:** Tap any Attribute or Skill to trigger the **Dice Nexus** with appropriate modifiers.

### 2. Campaign Management (The Chronicle Nexus)

- **Coterie Hub:** A centralized space for players to link characters to a shared Chronicle.
- **Shared Lore:** Storytellers can manage "public knowledge" notes, NPCs, and location descriptions.
- **XP Allocation:** Grant XP to the entire group or specific individuals directly from the dashboard.

### 3. Storyteller Toolkit

- **Initiative Tracker:** A real-time tracker handling the unique "Initiative Mod" mechanics of Chronicles of Darkness.
- **ST Oversight:** Private "Glimpse" view to see player vitals at a glance without switching tabs.
- **Encounter Manager:** Pre-build NPC stat-blocks for instant deployment during sessions.

### 4. The Dice Nexus

- **High-Throughput:** A specialized rolling service supporting 10-again, 9-again, 8-again, and rote actions.

---

## 🧪 DevOps & Automation

- **Local-First Development:** Bootable via a single script (`scripts/build-debug.ps1`) to ensure environment parity.
- **Database Governance:** Schema changes must be accompanied by a Migration and a corresponding update to the `DbInitializer`.
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
