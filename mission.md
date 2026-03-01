# 🩸 Project: Requiem Nexus

## 🌌 The Mission

To build the definitive, high-performance digital ecosystem for **Vampire: The Requiem (Chronicles of Darkness) Second Edition**.

**Requiem Nexus** is a learning-driven, cloud-ready platform designed to eliminate the friction of character and campaign management. By leveraging the reactive power of **.NET 10** and the **Antigravity Philosophy**, we deliver a Beyond-style experience that is fast, secure, observable, and infinitely scalable.

> _The blood is the life… but clarity is the power._

---

## 📚 1. Educational Core (The Grimoire)

Every architectural choice is a learning milestone.  
We prioritize **Understanding over Magic**.

### Core Learning Goals

- **Reactive Patterns**  
  Master real-time state changes without page refreshes using explicit C# state management.

- **ORM Mastery**  
  Use **EF Core** to understand relational mapping, migrations, performance tuning, and the transition from SQLite to PostgreSQL.

- **Identity & Security**  
  Deep dive into **ASP.NET Core Identity**, JWTs, Claims-based authorization, and enterprise-grade data privacy.

### Learning Artifacts (Mandatory)

Every major subsystem must include:

- A `README.md` explaining **why** it exists
- One intentionally **simple** example
- One intentionally **wrong** example, with explanation

If it cannot be taught, it is not finished.

---

## ☁️ 2. Cloud-Native & Deployment (The Global Nexus)

The application is **cloud-agnostic** by design and deployable to Azure, AWS, Railway, or equivalent platforms.

### Architectural Principles

- **Containerization**  
  All services are containerized using Docker to ensure environment parity.

- **Modular Monolith**  
  Logic is partitioned into domain-specific projects (`Data`, `Web`, `Roll`) to allow future microservice extraction.

- **Service Orchestration**  
  **.NET Aspire** manages local resources, service discovery, and configuration.

- **Stateless Scaling**  
  The rules engine is stateless. Sessions and character state are stored via distributed caching (Redis).

### Domain Boundaries (Non-Negotiable)

Each domain owns:

- Its persistence models
- Its validation rules
- Its invariants

Cross-domain access is forbidden except via explicit contracts.

There is **no shared “Common” dumping ground**.

---

## 🎨 3. UI/UX: Intuitive Immersion (The Masquerade)

TTRPG tools should disappear during play.

- **Modern Gothic Aesthetic**  
  Bone-white and crimson UI optimized for dark mode.

- **The 3-Click Rule**  
  No core action should require more than three interactions.

- **Mobile-First Responsiveness**  
  Full functionality on phones and tablets.

- **Offline Capabilities (PWA)**  
  Local-first data resolution to support unstable connections at the table.

- **Tactile Feedback**  
  Subtle animations and reactive components reinforce character growth.

The UI must _feel alive_, but never distracting.

---

## 🛡️ 4. Security & Data Integrity

Security is intentional, explicit, and verifiable.

- **Zero-Trust Identity**  
  OpenID Connect (OIDC) with short-lived JWTs. All service calls are authenticated.

- **BOLA / IDOR Prevention**  
  Strict ownership checks for characters and chronicles.

- **Input Sanitization**  
  Strong typing and parameterized queries—no raw SQL.

- **Privacy First**  
  Minimum viable data collection. Sensitive data encrypted at rest.

- **Threat Modeling (Lite)**  
  Every exposed endpoint documents:
  - Trust boundaries
  - Expected attacker capabilities
  - Failure impact

---

## 🧭 5. Observability & Diagnostics

Nothing important happens silently.

- **Structured Logging**  
  Correlation-ID aware, machine-queryable logs.

- **Metrics First**  
  Dice rolls, XP spends, and state changes emit metrics.

- **Reproducibility**  
  Any bug must be reproducible via logged inputs.

- **Player-Safe Errors**  
  Friendly messages for users, rich diagnostics for developers.

If a bug cannot be observed, it cannot be fixed.

---

## 🎯 Key Objectives

### 1. The Living Sheet

- Automatic calculation of Health, Willpower, Defense, and Speed
- Zero manual recalculation at any time
- Tap-to-roll integration with the Dice Nexus

---

### 2. Campaign Management (The Chronicle Nexus)

- Coterie Hub for shared chronicles
- Public lore, NPCs, and locations
- Group or individual XP allocation

---

### 3. Storyteller Toolkit

- Real-time Initiative Tracker (Initiative Mod mechanics)
- Private “Glimpse” view of player vitals
- Pre-built NPC stat-blocks for instant encounters

---

### 4. The Dice Nexus

- High-throughput dice rolling
- Support for 10-again, 9-again, 8-again, and rote actions
- **Deterministic Mode** via seeded rolls for:
  - Debugging
  - Probability teaching
  - Session replay

---

## 🧪 DevOps & Automation

- **Local-First Development**  
  One-command startup via `scripts/build-debug.ps1`.

- **Database Governance**  
  All schema changes require migrations and `DbInitializer` updates.

- **CI/CD Discipline**  
  Every commit to `src/` must keep the build green.

---

## 📅 Phase 1: The Neonate (Player Focus)

- [x] Initialize .NET 10 modular project structure
- [x] EF Core migrations and schema manifest
- [x] Robust `DbInitializer` for game data
- [x] Comprehensive Character Management
- [x] Reactive `DotScale` component
- [x] XP expenditure and advancement flows
- [x] Finalize Dice Nexus service
- [x] Add Aspirations to the character: allow choosing Aspirations in character creation and adding/removing them in the character sheet
- [x] Add Bane section to the character and ability to add/remove them
- [x] Add Size, Speed, Defense, and Armor to the character sheet
- [x] Add Mask and Dirge to the character and character sheet: include in character creation and ability to change them in the character sheet
- [x] In the roll, add ability to choose the relevant ability to the roll

### Phase 1 Exit Criteria

- A Neonate character can be created, advanced, and rolled entirely on mobile
- No manual stat recalculation exists
- A new developer can run the project locally in under 10 minutes

---

## 📅 Phase 2: Validation & Automation (The Ascendant)

- Comprehensive unit testing for Domain models and Rules engine
- Integration testing for EF Core and ASP.NET API endpoints
- End-to-End (E2E) UI testing for critical player flows
- Automated Pull Request checks (Linting, Formatting, Test Coverage)
- CI/CD Pipelines (GitHub Actions / Azure DevOps)
- Automated deployments to staging and production environments
- Load testing and observability alerts

---

## 📅 Phase 3: Account Management

- Password reset and change
- Account deletion and data wipe
- Two-Factor Authentication (2FA)
- Email validation
- Profile management
- Session management
- OAuth / Social logins
- Audit logs
- Role management (Player vs Storyteller)

---

## 📅 Phase 4: The Storyteller

- Initiative Tracker
- Encounter Manager
- Campaign notes and shared lore
- ST Glimpse view
- **Homebrew / Custom Content Support** (Disciplines, Devotions, Bloodlines)

---
## 📅 Phase 5: The Global Embrace

- **Localization and Internationalization (i18n)** (Full language support)
- Third-party API integrations

---

## 🧠 Antigravity Rules of Thumb

1. **If it’s implicit, it’s a bug waiting to happen**
2. **State must be visible or eliminable**
3. **Magic is debt**
4. **Traceability beats cleverness**
5. **One reason to change per module**
6. **No silent failure—ever**
7. **Teach the system by reading the code**
8. **If debugging is hard, the design is wrong**
9. **Performance is a feature, not an optimization**
10. **Every shortcut must be temporary—and documented**

---

> _The blood remembers.  
> The code must too._
