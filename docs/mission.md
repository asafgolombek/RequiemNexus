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
  One-command startup via `scripts/build-debug.ps1`. Run `scripts/test-local.ps1` to validate before opening a PR.

- **Database Governance**  
  All schema changes require EF Core migrations and `DbInitializer` updates. Migrations are validated against an empty database in CI.

- **CI/CD Discipline**  
  Every commit to `src/` must keep the build green. GitHub Actions enforce compilation, test coverage, and formatting on every PR.

- **Automation is Documentation**  
  If a build, test, or deploy step is not automated, it does not reliably exist.

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
- [x] Performance / Load Testing baseline (e.g., k6 or NBomber smoke test to catch regressions on critical endpoints)
- [x] Test Data Seeding Strategy (repeatable, deterministic `TestDbInitializer` or fixture for integration and E2E tests)
- [x] CI Caching & Build Time Optimization (cache NuGet packages and build artifacts in GitHub Actions)
- [x] Automated Changelog / Release Notes (auto-generate from PR titles or conventional commits)
- [x] Developer Documentation (`CONTRIBUTING.md` — how to run tests, PR process, coding conventions)

### Phase 2 Exit Criteria

- All pull requests are automatically validated (build, test, lint) before merge
- Unit, integration, and E2E test suites pass on CI with no manual steps
- Security and dependency scanning is active on every PR
- A new developer can run the full test suite locally in one command
- Merging to `main` is blocked unless all required status checks pass
- A `CONTRIBUTING.md` exists and is kept up to date with the project workflow

---

## 📅 Phase 3: Account Management & Security (The Masquerade Veil)

- [x] **Registration & Onboarding**
  - Email verification (with token expiration)
  - Welcome emails and resend verification links
- [ ] **Authentication Rules**
  - Login lockout policies (e.g., 5 failed attempts locks for 15 mins)
  - "Remember Me" functionality
  - Password complexity rules and validation
- [ ] **Account Security**
  - Password reset and change flows
  - Two-Factor Authentication (2FA) via Authenticator App (TOTP) or Email
  - Recovery backup codes for 2FA
- [ ] **Session Management**
  - View active sessions across devices
  - Remote logout of other active sessions
  - Automatic timeout for inactivity
- [ ] **Data Privacy & Compliance**
  - GDPR/CCPA compliant "Download My Data"
  - Account deletion with a soft-delete grace period
  - Audit logs for security events (login, password change, 2FA toggle)
- [ ] **Profile Management**
  - Update display name, avatar, and email address
  - Email change flow with re-verification of the new address before it takes effect
- [ ] **Password Reset via Email**
  - Request link → email with time-limited token → reset form → invalidate old sessions
- [ ] **Account Recovery**
  - Define recovery path for users who lose 2FA device and recovery codes (e.g., support ticket, identity verification)
- [ ] **Rate Limiting**
  - Throttle login attempts, password reset requests, and registration endpoints to prevent brute-force and abuse
- [ ] **OAuth Connect**
  - Link/unlink Google, Discord, or Apple accounts to an existing local account
- [ ] **Role Management**
  - Player vs Storyteller authorization policies
- [ ] **Notification Preferences**
  - Opt in/out of email notifications (security alerts, campaign updates)
- [ ] **Terms of Service & Privacy Policy**
  - Track user consent acceptance with timestamps (required for GDPR compliance)

### Phase 3 Testing & Security Criteria

- **Unit Tests (Domain/Services):** Isolate and verify password hashing, token generation, lockout logic, and 2FA code validation.
- **Integration Tests (API/DB):** Verify EF Core correctly saves user states (`EmailConfirmed`, `TwoFactorEnabled`) and endpoints return correct HTTP status codes (e.g., `401 Unauthorized` for bad passwords).
- **E2E UI Tests (Playwright/bUnit):**
  - Automate the registration form submission.
  - Attempt login with incorrect passwords to trigger lockout UI.
  - Navigate to the profile page, change the password, and verify the old password no longer works.
- **Security Tests:** Ensure all session cookies have `HttpOnly`, `Secure`, and `SameSite` flags set correctly.

### Phase 3 Exit Criteria

- A user can register, verify their email, enable 2FA, and delete their account without developer intervention.
- All authentication endpoints and services have 100% test coverage.
- Account lockout and session invalidation work automatically.

---

## 📅 Phase 4: The Storyteller

- Initiative Tracker
- Encounter Manager
- Campaign notes and shared lore
- ST Glimpse view
- **Homebrew / Custom Content Support** (Disciplines, Devotions, Bloodlines)

---

## 📅 Phase 5: Automated Deployments & Observability

- [ ] Automated Versioning & Git Tagging (generate CI build numbers, inject into assemblies/logs, push Git tags)
- [ ] Infrastructure as Code (IaC) — define all AWS resources (ECS, RDS, ALB, etc.) in CDK, Terraform, or CloudFormation
- [ ] Secrets & environment variable management (AWS Secrets Manager / Parameter Store; no hardcoded secrets)
- [ ] Expose `/health` and `/ready` endpoints (ASP.NET Core HealthChecks) consumed by the load balancer
- [ ] Containerize application (Dockerfile) and push to Container Registry
- [ ] Configure AWS Environments (Staging, Production)
- [ ] Establish Application Configurations (.NET Environments & appsettings)
- [ ] Define migration deployment strategy (pre-deploy step vs. container startup, with ordering guards)
- [ ] Automated deployments to staging and production environments
- [ ] Post-deploy smoke test — automated check after each staging deploy (health endpoint + critical flows)
- [ ] Define and test rollback strategy (ECS task revision rollback or blue/green)
- [ ] Load testing and observability alerts
- [ ] Error Tracking Integration (e.g., Sentry, Raygun) for real-time exception alerts

### Phase 5 Exit Criteria

- Every merge to `main` automatically deploys to staging
- Production deployments are one-command (or one-click) from a green staging build
- Observability dashboards are live with alerts configured for error rate and latency
- Zero manual infrastructure configuration via the AWS Console
- All secrets are managed via AWS Secrets Manager; no credentials exist in the repository
- A rollback to the previous version can be completed in under 5 minutes

---
## 📅 Phase 6: The Global Embrace

- **Localization and Internationalization (i18n)** (Full language support)
- Third-party API integrations
- **SEO & Rich Social Sharing** (Open Graph tags for public character sheets and chronicles)

---

## 📅 Phase 7: End-to-End (E2E) Testing 

- [ ] End-to-End (E2E) UI testing for critical player flows
- [ ] Automated Accessibility (a11y) scanning integrated into E2E tests
- [ ] Add E2E tests to the CI pipeline

---

## 🧠 Antigravity Rules of Thumb

See the full list with explanations in [Architecture.md](./Architecture.md#-antigravity-rules-of-thumb).

---

> _The blood remembers.  
> The code must too._
