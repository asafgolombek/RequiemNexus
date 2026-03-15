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
| 4 | The Storyteller & The Danse Macabre | ✅ Complete |
| 5 | Automated Deployments & Observability | ✅ Complete |
| 6 | CI/CD Hardening & Supply Chain | ✅ Complete |
| 7 | Realtime Play (The Blood Communion) | 🔄 In Progress |
| 8 | End-to-End Testing & Accessibility | ⬜ Planned |
| 9 | The Global Embrace | ⬜ Planned |

> **Currently Active → [Phase 7](#-phase-7-realtime-play-the-blood-communion)**

> ⚠️ **Phases 8–9 are out of scope for current development. Do not scaffold, stub, or implement anything beyond Phase 7 unless explicitly instructed.**

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

- **Reactive Patterns** — Master real-time state changes without page refreshes using explicit C# state management.
- **ORM Mastery** — Use **EF Core** to understand relational mapping, migrations, and performance tuning from SQLite to PostgreSQL.
- **Modern Syntax (C# 14)** — Wield Primary Constructors and enhanced collection expressions as deliberate learning milestones, reducing boilerplate to sharpen intent.
- **Identity & Security (The Masquerade)** — Deep dive into ASP.NET Core Identity, JWTs, and enterprise-grade data privacy.

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

- **The Haven (Containerization)** — All services are isolated within Docker to ensure environment parity and uncorrupted local execution.
- **Modular Monolith (The Sacred Covenants)** — Logic is partitioned into four domain-specific projects (`Application`, `Data`, `Domain`, `Web`). They are isolated covenants — no shared dumping grounds are permitted.
- **Service Orchestration** — **.NET Aspire** manages local resources, service discovery, and configuration. It is the ritual binding the services together.
- **Stateless Scaling** — The rules engine is stateless. Sessions and character state are persisted via distributed caching (Redis).

### Performance Budgets

"Fast" is not a feeling — it is a constraint. Budgets are defined and enforced per [Architecture.md](./Architecture.md#-performance-architecture).

### Data Recovery SLA

Player data must be recoverable to within **24 hours** of any failure. This governs backup frequency and infrastructure decisions from Phase 5 onward.

> "Player Owns Their Data" is not just a UX principle — it is an infrastructure commitment.

### Domain Boundaries (Non-Negotiable)

Each domain exercises total sovereignty over its persistence models, validation rules, and invariants. Cross-domain access is forbidden except via explicit contracts. There is **no shared "Common" dumping ground**.

---

## 🎨 3. UI/UX: Intuitive Immersion (The Masquerade)

TTRPG tools should disappear during play.

- **Modern Gothic Aesthetic** — Bone-white and crimson UI optimized for dark mode.
- **The 3-Click Rule** — No core action should require more than three interactions.
- **Mobile-First Responsiveness** — Full functionality on phones and tablets.
- **Offline Capabilities (PWA)** — Local-first data resolution to support unstable connections at the table. Sync Strategy: Event-sourced state changes are queued locally and reconciled on reconnect using last-write-wins optimistic concurrency with conflict detection.
- **Tactile Feedback** — Subtle animations and reactive components reinforce character growth.
- **Accessibility (a11y) — WCAG 2.1 AA Compliance** — The Gothic aesthetic must not sacrifice usability. Crimson-on-black is beautiful but must pass contrast ratios. The system commits to:
  - High-contrast mode toggle for all UI surfaces.
  - Legible font scaling and responsive typography.
  - ARIA labels on all interactive elements (dice results, dot-scale controls, navigation).
  - Screen-reader-friendly announcements for dice rolls and state changes.
- **Data Sovereignty (The Player Owns Their Data)** — If the server ever goes down, no character is lost. Players can:
  - **Export** their full character sheet to a standardized JSON format.
  - **Export** a printable PDF character sheet.
  - **Import** a previously exported character into any Requiem Nexus instance.

The UI must _feel alive_, but never distracting.

---

## 🛡️ 4. Security & Data Integrity

Security is intentional, explicit, and verifiable. The Masquerade is maintained at every perimeter.

- **Zero-Trust Identity** — Currently implemented via **ASP.NET Core Identity** with secure cookie authentication. OpenID Connect (OIDC) with short-lived JWTs is the target for Phase 5+ service-to-service communication.
- **BOLA / IDOR Prevention** — Strict ownership checks for characters and chronicles.
- **Input Sanitization** — Strong typing and parameterized queries — no raw SQL.
- **Privacy First** — Minimum viable data collection. Sensitive data encrypted at rest.
- **Threat Modeling (Lite)** — Every exposed endpoint documents trust boundaries, expected attacker capabilities, and failure impact.

---

## 🧭 5. Observability & Diagnostics

Nothing important happens silently.

- **Structured Logging** — Correlation-ID aware, machine-queryable logs via Serilog.
- **Metrics First** — Dice rolls, XP spends, and state changes emit OpenTelemetry metrics.
- **Reproducibility** — Any defect must be explicitly reproducible via logged inputs.
- **Player-Safe Errors** — Friendly messages for users, rich diagnostics for developers.

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
- Eliminates the "Wait, did I add my Beats from last session?" problem forever.

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
- Private "Glimpse" view of player vitals.
- Pre-built NPC stat-blocks for instant encounters.

### 7. The Dice Nexus _(Phase 1 — ✅ Complete)_
- High-throughput dice rolling.
- Support for 10-again, 9-again, 8-again, and rote actions.
- **Deterministic Mode** via seeded rolls for debugging and session replay.

---

## 🧪 DevOps & Automation

- **The Haven (Local-First Development)** — One-command startup via `scripts/build-debug.ps1`. Run `scripts/test-local.ps1` to face the Inquisition before opening a PR.
- **Database Governance (The Blood of the System)** — All schema changes require EF Core migrations and `DbInitializer` updates.
- **CI/CD Discipline** — Every commit to `src/` must keep the build green. GitHub Actions enforce compilation, test coverage, and code styling.
- **Automation is Documentation** — If a deploy or test step isn't meticulously scripted in a PowerShell file or bound to a GitHub Action, it does not exist.

---

## 📅 Phase 1: The Neonate (Player Focus)

- [x] Initialize .NET 10 modular project structure
- [x] EF Core migrations and schema manifest
- [x] Robust `DbInitializer` for game data
- [x] Comprehensive Character Management
- [x] Reactive `DotScale` component
- [x] XP expenditure and advancement flows
- [x] Finalize Dice Nexus service
- [x] Add Aspirations to the character
- [x] Add Bane section to the character
- [x] Add Size, Speed, Defense, and Armor to the character sheet
- [x] Add Mask and Dirge to the character
- [x] In the roll, add ability to choose the relevant ability
- [x] Humanity tracking — dot-scale Humanity stat with Stain accumulation and degradation
- [x] Vitae (Blood Pool) tracking — current/max Vitae with spend and replenish actions
- [x] Blood Potency — core vampire stat affecting feeding and power level
- [x] My Characters dashboard — character roster view with create, select, and manage actions
- [x] **Predator Type** — (Implemented in Phase 4: grants bonuses, feeding restrictions, and starting Merits/Specialties)

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
- [x] Enforce minimum test coverage threshold in CI
- [x] Configure branch protection rules on `main`
- [x] Enforce `.editorconfig` in CI
- [x] Performance / Load Testing baseline (NBomber smoke test)
- [x] Test Data Seeding Strategy (repeatable, deterministic `TestDbInitializer`)
- [x] CI Caching & Build Time Optimization
- [x] Automated Changelog / Release Notes
- [x] Developer Documentation (`CONTRIBUTING.md`)

### Phase 2 Exit Criteria
- All pull requests are automatically validated before merge
- Unit, integration, and E2E test suites pass on CI with no manual steps
- Security and dependency scanning is active on every PR
- A new developer can run the full test suite locally in one command
- Merging to `main` is blocked unless all required status checks pass

---

## 📅 Phase 3: Account Management & Security (The Masquerade Veil)

- [x] Registration & Onboarding (email verification, welcome emails, resend links)
- [x] Authentication Rules (lockout policies, Remember Me, password complexity)
- [x] Password reset and change flows
- [x] Two-Factor Authentication (TOTP + Email)
- [x] FIDO2 / WebAuthn Physical Security Keys
- [x] Recovery backup codes for 2FA
- [x] Session Management (view active sessions, remote logout, inactivity timeout)
- [x] GDPR/CCPA compliant "Download My Data"
- [x] Account deletion with soft-delete grace period
- [x] Audit logs for security events
- [x] Data Sovereignty (Export / Import — JSON and PDF)
- [x] Profile Management (display name, avatar, email with re-verification)
- [x] Account Recovery (lost 2FA device + recovery codes path)
- [x] Rate Limiting on auth endpoints
- [x] OAuth Connect (Google, Discord, Apple)
- [x] Role Management (Player vs Storyteller authorization policies)
- [x] Notification Preferences
- [x] Terms of Service & Privacy Policy consent tracking

### Phase 3 Exit Criteria
- A user can register, verify email, enable 2FA, and delete their account without developer intervention
- All authentication endpoints have 100% test coverage
- A player can export → delete → re-register → import with zero data loss

---

## 📅 Phase 4: The Storyteller & The Danse Macabre

### Chronicle & Storyteller Tools
- [x] **Initiative Tracker** — real-time initiative order using Initiative Mod mechanics
- [x] **Encounter Manager** — create, manage, and resolve combat encounters
- [x] **Storyteller Glimpse** — private dashboard showing player vitals (Health, Willpower, Humanity, active Conditions)
- [x] **Campaign Notes & Shared Lore** — collaborative lore database accessible to the coterie
- [x] **NPC Quick Stat Blocks** — pre-built and custom NPC stat blocks for instant encounters
- [x] **XP Distribution** — group or individual XP/Beat allocation from the Storyteller dashboard

### The Beat & Experience Ledger
- [x] **Beat Tracking** — immutable, transactional log of how Beats are earned
- [x] **XP Spend Audit Trail** — full history of how XP was spent
- [x] **Storyteller Beat Awards** — award Beats to individuals or the coterie with a tagged reason

### Condition & Tilt Tracker
- [x] **Condition Management** — add, view, and resolve V:tR 2e Conditions
- [x] **Tilt Management** — track combat Tilts with mechanical effects
- [x] **One-Tap Resolution** — resolving a Condition automatically awards a Beat
- [x] **Active Effects Display** — mechanical effects surfaced directly on the character sheet

### Coterie & Domain Mapping (The Danse Macabre)
- [x] **Coterie Hub** — shared coterie identity, resources, and group aspirations
- [x] **Feeding Territories** — track hunting grounds and their ratings
- [x] **City Power Structure** — map the political landscape: Prince, Primogen, Covenants
- [x] **Touchstone Management** — track mortal Touchstones tied to Humanity anchors
- [x] **NPC Relationship Web** — relationship tracker between PCs, NPCs, and factions

### Player Character Management
- [x] **Predator Type** — chosen at character creation; grants specific bonuses, feeding restrictions, and a starting Merit or Skill specialty; surfaced in Feeding Territory and Coterie context
- [x] **Character Archiving / Retirement** — retire without deleting; archived characters remain viewable and exportable
- [x] **Saved Dice Macros** — save named dice pools for one-tap reuse during play
- [x] **Character Notes & Session Journal** — free-form notes tied to a character or session
- [x] **Storyteller Secret Notes** — private per-character notes visible only to the Storyteller
- [x] **Session Prep Workspace** — private Storyteller workspace for scene outlines and encounter planning

### Homebrew & Custom Content
- [x] **Custom Disciplines & Devotions** — create and share homebrew supernatural powers
- [x] **Custom Bloodlines** — define new Bloodlines with unique Disciplines and weaknesses
- [x] **Content Sharing** — export/import homebrew content packs as JSON

### Phase 4 Exit Criteria
- A Storyteller can run a full session from the Glimpse dashboard
- Conditions apply, track mechanically, and resolve for Beats without manual bookkeeping
- The city's political structure and feeding territories are viewable and editable
- A player can retire a character, save a dice macro, and write session notes without developer intervention
- CI coverage gate passes for all new Phase 4 code (no raw "100% coverage" claim — gate enforced by threshold in GitHub Actions)

---

## 📅 Phase 5: Automated Deployments & Observability

- [x] Automated Versioning & Git Tagging
- [x] GitHub Environments (Staging auto-deploy, Production approvals)
- [x] GitHub Actions → AWS via OIDC (no long-lived AWS keys)
- [x] Infrastructure as Code (IaC) — **AWS CDK** (C# — Stacks implemented)
- [x] PR Infrastructure Preview (CDK synth + diff posted to PR)
- [x] Secrets & environment variable management (AWS Secrets Manager)
- [x] Expose `/health` and `/ready` endpoints
- [x] Containerize application and push to Container Registry
- [x] Configure AWS Environments (Staging, Production — Workflows ready)
- [x] Application Configurations (.NET Environments & appsettings)
- [x] Define migration deployment strategy
- [x] Enforce deploy concurrency (no overlapping deployments per environment)
- [x] Automated deployments to staging and production
- [x] Post-deploy smoke test
- [x] **Performance Budget Enforcement** — CI checks fail if thresholds are exceeded

### Phase 5 Implementation Notes (AWS + CDK)

> [!IMPORTANT]
> **Bootstrapping**: Before the first deployment to any AWS account/region, the environment must be bootstrapped. Run `cdk bootstrap aws://<ACCOUNT_ID>/<REGION>` from a local terminal with appropriate AWS credentials. This creates the necessary staging resources (S3 bucket, IAM roles) used by the CDK deployment process.

These notes are Phase 5 scope guidance only — do not scaffold infrastructure before Phase 5 begins.

#### Database Choice

- **Staging / Production**: **RDS PostgreSQL** (managed, backed up, supports PITR; aligns with the 24-hour recovery SLA).
- **Local (The Haven)**: SQLite (fast, zero-ops) or local PostgreSQL orchestrated by .NET Aspire for parity testing.

#### “Rules Data” (Clans, Merits, Disciplines, etc.)

- Treat system-owned rules content as **reference data** that is versioned alongside the application.
- Update and evolve reference data through `DbInitializer` (idempotent, safe to run multiple times).
- If reference data must be corrected for existing rows, use an **EF Core migration** for deterministic forward-only updates (no manual SQL against production).

#### Migration Strategy (No Races)

- Apply EF Core migrations as a **one-off migration step** during deploy (not “on every app instance startup”).
- Canonical AWS implementation: run a dedicated “migrator” ECS task (or a pre-deploy one-off task) against the target RDS database, then deploy the new ECS task revision.

#### Local DB Without Committing It

- Local database files must never be checked into git.
- Store local DBs under a local-only directory (e.g. `.data/`) and ensure it is ignored (`.gitignore`).
- Local secrets: `dotnet user-secrets`. Staging/production secrets: AWS Secrets Manager.

#### Email Service

- Use **Amazon SES** for transactional email (verification, password reset, account notifications).
- Route bounces/complaints via SNS if/when needed.

#### Minimal AWS Services (Typical)

- **Networking**: VPC, subnets, security groups
- **Compute**: ECS Fargate, ECR, ALB
- **Persistence**: RDS PostgreSQL, ElastiCache (Redis)
- **Secrets & IAM**: Secrets Manager, IAM roles/policies
- **Observability**: CloudWatch Logs/Metrics/Alarms (and/or Grafana), OpenTelemetry Collector
- **Static/CDN**: S3 + CloudFront
- **DNS/TLS**: Route 53 + ACM
- **Cost**: AWS Budgets
- **Optional**: WAF

### Phase 5 Exit Criteria
- Every merge to `main` automatically deploys to staging
- Production deployments are one-command from a green staging build
- Observability dashboards are live with alerts configured
- Zero manual infrastructure configuration via the AWS Console
- All secrets managed via AWS Secrets Manager — no credentials in the repository
- Rollback to the previous version completes in under 5 minutes
- Database backups are verified against the 24-hour recovery SLA
- Performance budgets enforced in CI

---

## 📅 Phase 6: CI/CD Hardening & Supply Chain

- [x] CodeQL scanning (C#) enforced on PRs
- [x] Dependabot updates (NuGet + GitHub Actions), with safe auto-merge policy for patch releases
- [x] Secret scanning + push protection enabled _(GitHub repo settings — cannot be automated via workflow)_
- [x] Container image vulnerability scanning in CI (fail on high/critical)
- [x] SBOM generation for release artifacts (CycloneDX or equivalent)
- [x] Image signing + provenance (keyless via GitHub OIDC where possible)
- [x] Coverage reporting + minimum thresholds enforced for changed code
- [x] Nightly performance regression workflow (budget enforcement trends over time)
- [x] Branch protection rules + `CODEOWNERS` for security/infra sensitive paths _(CODEOWNERS created; branch protection rules require GitHub repo settings)_
- [x] Test results + logs uploaded as CI artifacts (TRX, coverage, etc.)

### Phase 6 Exit Criteria
- Required security scans are enabled and required for merges
- Release artifacts include an SBOM and are signed (or have published provenance)
- Nightly performance runs are automated and visible
- Branch protections prevent bypassing required checks

---

## 📅 Phase 7: Realtime Play (The Blood Communion)

- [ ] **Live Dice Rolls** — dice results broadcast to the coterie in real-time via SignalR
- [ ] **SignalR Backplane** — configure Redis as the SignalR backplane to support horizontal scaling
- [ ] **Shared Initiative Tracker** — live initiative order visible to all session participants
- [ ] **Real-Time Character Updates** — Health, Willpower, and Condition changes sync instantly across clients
- [ ] **Session Presence** — indicators showing which players are online and active
- [ ] **Synchronized Chronicle State** — Storyteller actions push live to all connected players
- [ ] **Dice Roll History Feed** — shared, scrollable feed of all rolls made during a session
- [ ] **Reconnection Resilience** — define and implement behavior for in-flight dice rolls and state if SignalR drops mid-broadcast; clients must rejoin and receive full current session state
- [ ] **Rate Limiting on SignalR Hubs** — throttle message frequency per connection to prevent hub abuse
- [ ] **Async / Play-by-Post Dice Sharing** — shareable permanent link to a roll result for async groups

### Phase 7 Exit Criteria
- A full coterie can connect, roll dice, and see each other's results in real-time
- Storyteller actions propagate to player sheets within 200ms
- Disconnected players rejoin and receive full current session state
- SignalR hub is protected against message flooding
- All realtime features have integration tests covering connection, broadcast, and reconnection

---

## 📅 Phase 8: End-to-End Testing & Accessibility

- [ ] E2E UI testing for critical player flows (character creation, dice rolling, XP spending)
- [ ] E2E Storyteller flows (initiative, encounter management, Beat awarding)
- [ ] **Automated Accessibility (a11y) Scanning** — WCAG 2.1 AA audit integrated into E2E tests
- [ ] **Contrast Ratio Validation** — automated checks for the Gothic color palette against accessibility standards
- [ ] **Screen Reader Testing** — verify ARIA labels and live regions for dice results and state changes
- [ ] **Keyboard Navigation** — full app navigable via keyboard alone
- [ ] **Font Scaling & Responsive Typography Tests** — verify legible rendering across device sizes and user font preferences
- [ ] **Visual Regression Testing** — Playwright screenshot comparisons to catch unintended UI drift against the Gothic aesthetic baseline
- [ ] **Cross-Browser Testing Matrix** — explicitly verify: Chrome, Firefox, Safari (iOS + macOS), Edge; document any known limitations
- [ ] Add all E2E and a11y tests to the CI pipeline

### Phase 8 Exit Criteria
- Every critical user flow is covered by an E2E test
- Zero WCAG 2.1 AA violations on any page
- The app is fully operable via keyboard navigation
- Visual regression baseline is established and monitored in CI
- Supported browser matrix is documented and verified

---

## 📅 Phase 9: The Global Embrace

- [ ] **Localization & i18n** — full language support for UI strings; priority languages: French, German, Spanish (largest TTRPG markets after English)
- [ ] - **Game Term Localization Strategy** — define canonical policy before any i18n work begins. The following terms are **sacred** and must not be silently translated, anglicized, or altered without an      explicit, documented decision per language: `Discipline`, `Covenant`, `Touchstone`, `Coterie`, `Clan`, `Beat`, `Predator Type`, `Humanity`, `Vitae`, `Blood Potency`. Each language's treatment of these terms must be recorded in a localization policy document before strings are extracted. No silent drift.
- [ ] **SEO & Rich Social Sharing** — Open Graph tags, structured data for chronicle/character sharing
- [ ] **Discord & Webhook Integrations** — post roll results, Beat awards, and session summaries to Discord channels
- [ ] **Public API** — versioned, documented REST API for community tool builders; includes API key registration, rate limiting, and quota enforcement
- [ ] **GDPR Per-Region Compliance Review** — multi-region deployments introduce new data residency obligations; audit and document per-region requirements before expanding infrastructure
- [ ] **Community Content Marketplace** — browse and share homebrew content packs

### Phase 9 Exit Criteria
- The app is fully localized in at least two languages
- Game term localization policy is documented and applied consistently
- Shared chronicle links render rich previews on Discord and social platforms
- The public API has versioned documentation, rate limiting, and a developer registration flow
- Per-region GDPR obligations are documented for each active deployment region

---

## 🔭 Future Plans — Deferred Initiatives

### PWA & Offline Capabilities (The Hidden Refuge)

**What was planned:** A full offline-capable PWA experience — service worker caching of the application shell, full read/write access to character sheets without a network connection, offline dice rolling via the Dice Nexus, and an event-sourced local state queue that reconciles with the server on reconnect using last-write-wins optimistic concurrency with conflict detection and a player-facing merge UI.

**Why it was deferred:** Offline-first architecture introduces significant implementation complexity — IndexedDB quota management, canonical ordering of offline dice rolls in the shared session history, and conflict resolution UX all require careful design to meet the Antigravity standard of zero magic and full traceability. Building this before real users exist would mean speculating about usage patterns that may never materialize.

**How it will be decided:** The offline strategy will be revisited after the web application ships and real user feedback is collected. If players consistently report playing in connectivity-challenged environments (game tables, conventions, travel), a targeted offline feature set will be scoped and prioritized accordingly. No architecture will be pre-built in anticipation of this — the decision will be driven by evidence, not assumption.

---

## 🧠 Antigravity Rules of Thumb

See the full list with explanations in [Architecture.md](./Architecture.md#-antigravity-rules-of-thumb).

---

> _The blood remembers._
> _The code must too._
