# 🩸 Project: Requiem Nexus

## 🌌 The Mission

To forge the definitive, high-performance digital ecosystem for **Vampire: The Requiem (Chronicles of Darkness) Second Edition**.

**Requiem Nexus** is a learning-driven, cloud-native covenant designed to banish the friction of character and chronicle management. By wielding the reactive, high-performance supremacy of **.NET 10** and the authoritative **Antigravity Philosophy**, we cast aside the archaic in favor of tactile immersion. We deliver an experience that mirrors the aesthetic of the nocturnal world—bone-white and crimson, fast, secure, observable, and infinitely scalable. The architecture itself serves as a **Grimoire of Modern Engineering**, ensuring every line of code is an intentional strike against technical debt.

> _The blood is the life… but clarity is the power._

---

## 📊 Phase Status

| Phase | Name | Status |
|-------|------|--------|
| 1 | The Neonate (Player Focus) | ✅ Complete |
| 2 | Validation & Automation (The Ascendant) | ✅ Complete |
| 3 | Account Management & Security (The Masquerade Veil) | ✅ Complete |
| 4 | The Storyteller & The Danse Macabre | ⬜ Planned |
| 5 | Automated Deployments & Observability | ⬜ Planned |
| 6 | Realtime Play (The Blood Communion) | ⬜ Planned |
| 7 | PWA & Offline Capabilities (The Hidden Refuge) | ⬜ Planned |
| 8 | End-to-End Testing & Accessibility | ⬜ Planned |
| 9 | The Global Embrace | ⬜ Planned |

> **Currently Active → [Phase 4](#-phase-4-the-storyteller--the-danse-macabre)**

---

## 🧱 The Five Pillars

Requiem Nexus is built on five guiding principles.

1. **Explicit Engineering** — If behavior cannot be understood by reading the code, the system is wrong.
2. **Learning Through Architecture** — The project is also an educational artifact demonstrating modern .NET architecture.
3. **Domain Sovereignty** — Each subsystem owns its models, rules, and persistence.
4. **Observability First** — Systems must explain themselves through logs, metrics, and traces.
5. **Player-Invisible UX** — The tool must disappear during play.

---

## 🚫 Non-Goals

Requiem Nexus intentionally does **not** attempt to:

- Replace a full **Virtual Tabletop (VTT)** or implement battle maps / tactical grid combat.
- Become a generic multi-system TTRPG platform.
- Support every **Chronicles of Darkness** splat line.

> **The focus is clear: Character and Chronicle management for Vampire: The Requiem 2e.**

---

## 👥 Primary Users

- **🧛 Player** — Manages characters, rolls dice, tracks conditions and aspirations.
- **🎭 Storyteller** — Runs chronicles, manages NPCs, tracks player vitals, and organizes lore. _(Storyteller-specific features are Phase 4 scope — not yet implemented.)_
- **🧙 Developer (The Apprentice)** — Learns modern architecture through the project's Grimoire.

---

## 📚 1. Educational Core (The Grimoire)

Every architectural choice is a learning milestone. We prioritize **Explicit Understanding over Implicit "Magic"**.

### Core Learning Goals

- **Reactive Patterns**  
  Master real-time state changes without page refreshes using explicit C# state management.
- **ORM Mastery**  
  Use **EF Core** to understand relational mapping, migrations, and performance tuning from SQLite to PostgreSQL.
- **Modern Syntax (C# 14)**  
  Wield Primary Constructors and enhanced collection expressions as deliberate learning milestones, reducing boilerplate to sharpen intent.
- **Identity & Security (The Masquerade)**  
  Deep dive into ASP.NET Core Identity, JWTs, and enterprise-grade data privacy.

### Learning Artifacts (Mandatory)

Every major subsystem must include:
- A `README.md` explaining **why** it exists.
- One intentionally **simple** example.
- One intentionally **wrong** example, with explanation.

If it cannot be taught, it is not finished.

> **Enforcement:** Learning artifacts are verified during PR review via the PR checklist — not automated CI. A PR that adds a major subsystem without a `README.md` must be rejected at review.

---

## ☁️ 2. Cloud-Native & Deployment (The Global Nexus)

The application is **cloud-agnostic** by design and deployable to any sanctuary: Azure, AWS, Railway, or equivalent.

### Architectural Principles

- **The Haven (Containerization)**  
  All services are isolated within Docker to ensure environment parity and uncorrupted local execution.
- **Modular Monolith (The Sacred Covenants)**
  Logic is partitioned into four domain-specific projects (`Application`, `Data`, `Domain`, `Web`). They are isolated covenants—no shared dumping grounds are permitted.
- **Service Orchestration**  
  **.NET Aspire** manages local resources, service discovery, and configuration. It is the ritual binding the services together.
- **Stateless Scaling**  
  The rules engine is stateless. Sessions and character state are persisted via distributed caching (Redis).

### Performance Budgets

"Fast" is not a feeling—it is a constraint:

- **Dice rolls** resolve and broadcast in **< 200ms**.
- **Time to Interactive (TTI)** for character sheets is **< 1.5 seconds**.
- **API response time** for all CRUD operations is **< 300ms** (p95).

### Domain Boundaries (Non-Negotiable)

Each domain exercises total sovereignty over:
- Its persistence models
- Its validation rules
- Its invariants

Cross-domain access is forbidden except via explicit contracts. There is **no shared “Common” dumping ground**. 

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
  **Sync Strategy:** Event-sourced state changes are queued locally and reconciled on reconnect using last-write-wins optimistic concurrency with conflict detection.
- **Tactile Feedback**  
  Subtle animations and reactive components reinforce character growth.
- **Accessibility (a11y) — WCAG 2.1 AA Compliance**  
  The Gothic aesthetic must not sacrifice usability. Crimson-on-black is beautiful but must pass contrast ratios. The system commits to:  
  - High-contrast mode toggle for all UI surfaces.
  - Legible font scaling and responsive typography.
  - ARIA labels on all interactive elements (dice results, dot-scale controls, navigation).
  - Screen-reader-friendly announcements for dice rolls and state changes.
- **Data Sovereignty (The Player Owns Their Data)**  
  If the server ever goes down, no character is lost. Players can:  
  - **Export** their full character sheet to a standardized JSON format.
  - **Export** a printable PDF character sheet.
  - **Import** a previously exported character into any Requiem Nexus instance.

The UI must _feel alive_, but never distracting.

---

## 🛡️ 4. Security & Data Integrity

Security is intentional, explicit, and verifiable. The Masquerade is maintained at every perimeter.

- **Zero-Trust Identity**
  Currently implemented via **ASP.NET Core Identity** with secure cookie authentication. OpenID Connect (OIDC) with short-lived JWTs is the target for Phase 5+ service-to-service communication.
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
  Any defect must be explicitly reproducible via logged inputs.
- **Player-Safe Errors**  
  Friendly messages for users, rich diagnostics for developers.

If a bug cannot be observed, it cannot be fixed.

---

## 🎯 Key Objectives

### 1. The Living Sheet _(Phase 1 — ✅ Complete)_
- Automatic calculation of Health, Willpower, Defense, and Speed.
- Zero manual recalculation at any time.
- Tap-to-roll integration with the Dice Nexus.

### 2. The Beat & Experience Ledger _(Phase 4)_
- An immutable, transactional history of how **Beats** were earned (Dramatic Failure, resolving a Condition, fulfilling an Aspiration).
- Full audit trail of how **XP** was spent (Attribute dots, Discipline levels, Merits).
- Eliminates the “Wait, did I add my Beats from last session?” problem forever.

### 3. Condition & Tilt Tracker _(Phase 4)_
- First-class tracking for V:tR 2e **Conditions** (Guilty, Swooned, Tempted) and **Tilts** (Knocked Down, Stunned, Blinded).
- One-tap resolution of Conditions to automatically award a Beat.
- Mechanical effects of active Conditions/Tilts surfaced directly on the character sheet.

### 4. Campaign Management (The Chronicle) _(Phase 4)_
- Coterie Hub for shared chronicles.
- Public lore, NPCs, and locations.
- Group or individual XP allocation.

### 5. Coterie & Domain Mapping (The Danse Macabre) _(Phase 4)_
- Track feeding territories and hunting grounds.
- Map the city's power structure: Prince, Primogen, Covenants, and their influence.
- Manage NPC relationships and **Touchstones** — the mortal anchors that keep the Beast at bay.

### 6. Storyteller Toolkit _(Phase 4)_
- Real-time Initiative Tracker (Initiative Mod mechanics).
- Private “Glimpse” view of player vitals.
- Pre-built NPC stat-blocks for instant encounters.

### 7. The Dice Nexus _(Phase 1 — ✅ Complete)_
- High-throughput dice rolling.
- Support for 10-again, 9-again, 8-again, and rote actions.
- **Deterministic Mode** via seeded rolls for debugging and session replay.

---

## 🧪 DevOps & Automation

- **The Haven (Local-First Development)**  
  One-command startup via `scripts/build-debug.ps1`. Run `scripts/test-local.ps1` to face the Inquisition before opening a PR.
- **Database Governance (The Blood of the System)**  
  All schema changes require EF Core migrations and `DbInitializer` updates.
- **CI/CD Discipline**  
  Every commit to `src/` must keep the build green. GitHub Actions enforce compilation, test coverage, and code styling.
- **Automation is Documentation**  
  If a deploy or test step isn't meticulously scripted in a PowerShell file or bound to a GitHub Action, it does not exist.

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
- [x] Humanity tracking — dot-scale Humanity stat with Stain accumulation and degradation
- [x] Vitae (Blood Pool) tracking — current/max Vitae with spend and replenish actions
- [x] Blood Potency — core vampire stat affecting feeding and power level
- [x] My Characters dashboard — character roster view with create, select, and manage actions
- [ ] Predator Type — chosen at character creation; grants specific bonuses, feeding restrictions, and a starting Merit or Skill specialty

### Phase 1 Exit Criteria
- A Neonate character can be created, advanced, and rolled entirely on mobile
- No manual stat recalculation exists
- A new developer can run the project locally in under 10 minutes

---

## 📅 Phase 2: Validation & Automation (The Ascendant)

- [x] Comprehensive unit testing for Domain models and Rules engine
- [x] Integration testing for EF Core and ASP.NET API endpoints
- [x] Automated Pull Request checks (Linting, Formatting, Test Coverage)
- [x] CI/CD Pipelines (GitHub Actions)
- [x] Setup Dependabot/Renovate for automated dependency updates
- [x] Database Migration Validation (test migrations against empty DB in CI)
- [x] Security & Vulnerability Scanning (NuGet packages and Static Analysis)
- [x] Code Quality & Advanced Static Analysis (e.g., Qodana)
- [x] Enforce minimum test coverage threshold in CI (fail the build if coverage drops below target)
- [x] Configure branch protection rules on `main` (require CI + PR Checks to pass before merge)
- [x] Enforce `.editorconfig` in CI (fail build if `dotnet format --verify-no-changes` fails)
- [x] Performance / Load Testing baseline (NBomber smoke test to catch regressions on critical endpoints)
- [x] Test Data Seeding Strategy (repeatable, deterministic `TestDbInitializer` or fixture for integration and E2E tests)
- [x] CI Caching & Build Time Optimization (cache NuGet packages and build artifacts in GitHub Actions)
- [x] Automated Changelog / Release Notes (auto-generate from PR titles or conventional commits)
- [x] Developer Documentation (`CONTRIBUTING.md` — how to run tests, PR process, coding conventions)

### Phase 2 Exit Criteria
- All pull requests are automatically validated (build, test, lint) before merge.
- Unit, integration, and E2E test suites pass on CI with no manual steps.
- Security and dependency scanning is active on every PR.
- A new developer can run the full test suite locally in one command.
- Merging to `main` is blocked unless all required status checks pass.
- A `CONTRIBUTING.md` exists and is kept up to date with the project workflow.
- Static analysis rules explicitly validate that C# 14 features (e.g., enhanced collection expressions, primary constructors) are utilized correctly throughout the "Grimoire".

---

## 📅 Phase 3: Account Management & Security (The Masquerade Veil)

- [x] **Registration & Onboarding**
  - Email verification (with token expiration)
  - Welcome emails and resend verification links
- [x] **Authentication Rules**
  - Login lockout policies (e.g., 5 failed attempts locks for 15 mins)
  - "Remember Me" functionality
  - Password complexity rules and validation
- [x] **Account Security**
  - Password reset and change flows
  - Two-Factor Authentication (2FA) via Authenticator App (TOTP) or Email
  - FIDO2 / WebAuthn Physical Security Keys
  - Recovery backup codes for 2FA
- [x] **Session Management**
  - View active sessions across devices
  - Remote logout of other active sessions
  - Automatic timeout for inactivity
- [x] **Data Privacy & Compliance**
  - GDPR/CCPA compliant "Download My Data"
  - Account deletion with a soft-delete grace period
  - Audit logs for security events (login, password change, 2FA toggle)
- [x] **Data Sovereignty (Export / Import)**
  - Export full character sheet to standardized JSON format
  - Export printable PDF character sheet
  - Import previously exported character into any Requiem Nexus instance
- [x] **Profile Management**
  - Update display name, avatar, and email address
  - Email change flow with re-verification of the new address before it takes effect
- [x] **Password Reset via Email**
  - Request link → email with time-limited token → reset form → invalidate old sessions
- [x] **Account Recovery**
  - Define recovery path for users who lose 2FA device and recovery codes
- [x] **Rate Limiting**
  - Throttle login attempts, password reset requests, and registration endpoints to prevent brute-force
- [x] **OAuth Connect**
  - Link/unlink Google, Discord, or Apple accounts to an existing local account
- [x] **Role Management**
  - Player vs Storyteller authorization policies
- [x] **Notification Preferences**
  - Opt in/out of email notifications (security alerts, campaign updates)
- [x] **Terms of Service & Privacy Policy**
  - Track user consent acceptance with timestamps (required for GDPR compliance)

### Phase 3 Testing & Security Criteria
- **Unit Tests (Domain/Services):** Isolate and verify password hashing, token generation, lockout logic, and 2FA code validation.
- **Integration Tests (API/DB):** Verify EF Core correctly saves user states (`EmailConfirmed`, `TwoFactorEnabled`) and endpoints return correct HTTP status codes.
- **Security Tests:** Ensure all session cookies have `HttpOnly`, `Secure`, and `SameSite` flags set correctly.
- **Export Tests:** Validate JSON export schema roundtrips (export → import → export produces identical output).

### Phase 3 Exit Criteria
- A user can register, verify their email, enable 2FA, and delete their account without developer intervention.
- All authentication endpoints and services have 100% test coverage.
- Account lockout and session invalidation work automatically.
- A player can export their character as JSON, delete their account, re-register, and import the character with zero data loss.

---

## 📅 Phase 4: The Storyteller & The Danse Macabre

### Chronicle & Storyteller Tools
- [ ] **Initiative Tracker** — real-time initiative order using Initiative Mod mechanics
- [ ] **Encounter Manager** — create, manage, and resolve combat encounters
- [ ] **Storyteller Glimpse** — private dashboard showing player vitals (Health, Willpower, Humanity, active Conditions)
- [ ] **Campaign Notes & Shared Lore** — collaborative lore database accessible to the coterie
- [ ] **NPC Quick Stat Blocks** — pre-built and custom NPC stat blocks for instant encounters
- [ ] **XP Distribution** — group or individual XP/Beat allocation from the Storyteller dashboard

### The Beat & Experience Ledger
- [ ] **Beat Tracking** — immutable, transactional log of how Beats are earned (Dramatic Failure, resolving a Condition, fulfilling an Aspiration)
- [ ] **XP Spend Audit Trail** — full history of how XP was spent (Attribute dots, Discipline levels, Merits)
- [ ] **Storyteller Beat Awards** — Storyteller can award Beats to individuals or the coterie with a tagged reason

### Condition & Tilt Tracker
- [ ] **Condition Management** — add, view, and resolve V:tR 2e Conditions (Guilty, Swooned, Tempted, etc.)
- [ ] **Tilt Management** — track combat Tilts (Knocked Down, Stunned, Blinded) with mechanical effects
- [ ] **One-Tap Resolution** — resolving a Condition automatically awards a Beat to the character
- [ ] **Active Effects Display** — mechanical effects of active Conditions/Tilts surfaced directly on the character sheet

### Coterie & Domain Mapping (The Danse Macabre)
- [ ] **Coterie Hub** — shared coterie identity, resources, and group aspirations
- [ ] **Feeding Territories** — track hunting grounds and their ratings
- [ ] **City Power Structure** — map the political landscape: Prince, Primogen, Covenants, and their domains of influence
- [ ] **Touchstone Management** — track mortal Touchstones tied to Humanity anchors, with relationship status
- [ ] **NPC Relationship Web** — visual or list-based relationship tracker between PCs, NPCs, and factions

### Player Character Management
- [ ] **Character Archiving / Retirement** — retire a character from active play without deleting them; archived characters remain viewable and exportable
- [ ] **Saved Dice Macros** — save named dice pools (e.g., "Dexterity + Stealth") for one-tap reuse during play
- [ ] **Character Notes & Session Journal** — free-form notes tied to a character or chronicle session
- [ ] **Storyteller Secret Notes** — private per-character notes visible only to the Storyteller (hidden Banes, compromised Touchstones, etc.)
- [ ] **Session Prep Workspace** — private Storyteller workspace for pre-session scene outlines and encounter planning, separate from shared chronicle lore

### Homebrew & Custom Content
- [ ] **Custom Disciplines & Devotions** — create and share homebrew supernatural powers
- [ ] **Custom Bloodlines** — define new Bloodlines with unique Disciplines and weaknesses
- [ ] **Content Sharing** — Storytellers can export/import homebrew content packs as JSON

### Phase 4 Exit Criteria
- A Storyteller can run a full session: track initiative, manage encounters, award Beats, and view player vitals — entirely from the Glimpse dashboard.
- Conditions can be applied, mechanically tracked, and resolved for Beats without manual bookkeeping.
- The city's political structure and feeding territories are viewable and editable by the Storyteller.
- A player can retire a character, save a dice macro, and write session notes without developer intervention.
- All features have full unit and integration test coverage.

---

## 📅 Phase 5: Automated Deployments & Observability

- [ ] Automated Versioning & Git Tagging
- [ ] Infrastructure as Code (IaC) — define all AWS resources in CDK, Terraform, or CloudFormation
- [ ] Secrets & environment variable management (AWS Secrets Manager)
- [ ] Expose `/health` and `/ready` endpoints
- [ ] Containerize application (Dockerfile) and push to Container Registry
- [ ] Configure AWS Environments (Staging, Production)
- [ ] Establish Application Configurations (.NET Environments & appsettings)
- [ ] Define migration deployment strategy
- [ ] Automated deployments to staging and production environments
- [ ] Post-deploy smoke test
- [ ] Define and test rollback strategy (ECS task revision rollback or blue/green)
- [ ] Load testing and observability alerts
- [ ] Error Tracking Integration (e.g., Sentry, Raygun) for real-time exception alerts
- [ ] **Performance Budget Enforcement** — automated checks that dice rolls < 200ms, TTI < 1.5s, API p95 < 300ms

### Phase 5 Exit Criteria
- Every merge to `main` automatically deploys to staging.
- Production deployments are one-command from a green staging build.
- Observability dashboards are live with alerts configured.
- Zero manual infrastructure configuration via the AWS Console.
- All secrets are managed via AWS Secrets Manager; no credentials exist in the repository.
- A rollback to the previous version can be completed in under 5 minutes.
- Performance budgets are enforced in CI — builds fail if thresholds are exceeded.

---

## 📅 Phase 6: Realtime Play (The Blood Communion)

- [ ] **Live Dice Rolls** — dice results broadcast to the coterie in real-time via SignalR / WebSockets
- [ ] **Shared Initiative Tracker** — live initiative order visible to all session participants
- [ ] **Real-Time Character Updates** — changes to Health, Willpower, and Conditions sync instantly across clients
- [ ] **Session Presence** — indicators showing which players are online and active in the chronicle
- [ ] **Synchronized Chronicle State** — Storyteller actions (awarding Beats, applying Conditions, advancing scenes) push live to all connected players
- [ ] **Dice Roll History Feed** — a shared, scrollable feed of all rolls made during a session
- [ ] **Async / Play-by-Post Dice Sharing** — generate a shareable, permanent link to a roll result for groups that play asynchronously (no live session required)

### Phase 6 Exit Criteria
- A full coterie can connect to a live session, roll dice, and see each other's results in real-time.
- Storyteller actions (applying Conditions, awarding Beats) propagate to player sheets within 200ms.
- Disconnected players can rejoin and receive the full current session state.
- All realtime features have integration tests covering connection, broadcast, and reconnection scenarios.

---

## 📅 Phase 7: PWA & Offline Capabilities (The Hidden Refuge)

- [ ] **Service Worker Registration** — cache core application shell for offline access
- [ ] **Offline Character Sheet** — full read/write access to character data without a network connection
- [ ] **Offline Dice Rolling** — the Dice Nexus functions entirely offline
- [ ] **Offline State Queue** — event-sourced state changes queued locally during offline play
- [ ] **Reconnection Sync** — last-write-wins optimistic concurrency with conflict detection on reconnect
- [ ] **Conflict Resolution UI** — when sync conflicts occur, present the player with a clear merge/override choice
- [ ] **Install Prompt** — PWA install banner for mobile and desktop

### Phase 7 Exit Criteria
- A player can create a character, edit attributes, and roll dice with zero network connectivity.
- When connectivity returns, all offline changes sync correctly without data loss.
- The app is installable as a PWA on iOS, Android, and desktop browsers.
- Conflict resolution is tested with deterministic scenarios (simultaneous edits to the same field).

---

## 📅 Phase 8: End-to-End (E2E) Testing & Accessibility

- [ ] End-to-End (E2E) UI testing for critical player flows (character creation, dice rolling, XP spending)
- [ ] End-to-End Storyteller flows (initiative, encounter management, Beat awarding)
- [ ] **Automated Accessibility (a11y) Scanning** — WCAG 2.1 AA audit integrated into E2E tests
- [ ] **Contrast Ratio Validation** — automated checks for the Gothic color palette against accessibility standards
- [ ] **Screen Reader Testing** — verify ARIA labels and live regions for dice results and state changes
- [ ] **Keyboard Navigation** — full app navigable via keyboard alone
- [ ] Add all E2E and a11y tests to the CI pipeline

### Phase 8 Exit Criteria
- Every critical user flow (Player and Storyteller) is covered by an E2E test.
- Zero WCAG 2.1 AA violations on any page.
- The app is fully operable via keyboard navigation.
- All E2E and accessibility tests run in CI and block merge on failure.

---

## 📅 Phase 9: The Global Embrace

- [ ] **Localization and Internationalization (i18n)** — full language support for UI strings and game terms
- [ ] **SEO & Rich Social Sharing** — Open Graph tags, structured data for chronicle/character sharing
- [ ] **Third-Party API Integrations** — Discord bots, webhook notifications, calendar sync
- [ ] **Public API** — documented REST API for community tool builders and third-party integrations
- [ ] **Community Content Marketplace** — browse and share homebrew content packs

### Phase 9 Exit Criteria
- The app is fully localized in at least two languages.
- Shared chronicle links render rich previews on Discord, Twitter, and other platforms.
- The public API has versioned documentation and rate limiting in place.

---

## 🧠 Antigravity Rules of Thumb

See the full list with explanations in [Architecture.md](./Architecture.md#-antigravity-rules-of-thumb).

---

> _The blood remembers._  
> _The code must too._
