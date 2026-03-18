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
| 7 | Realtime Play (The Blood Communion) | ✅ Complete |
| 8 | The Hidden Blood (Bloodlines & Devotions) | ✅ Complete |
| 9 | The Accord of Power (Covenants & Blood Sorcery) | 🔄 Active |
| 9.5 | Sacrifice Mechanics (Blood Sorcery) | ⬜ Planned |
| 9.6 | Additional Blood Sorcery (Necromancy & Ordo Dracul) | ⬜ Planned |
| 10 | The Social Graces (Social Maneuvering) | ⬜ Planned |
| 11 | Assets & Armory (Equipment & Services) | ⬜ Planned |
| 12 | The Web of Night (Relationship Webs) | ⬜ Planned |
| 13 | End-to-End Testing & Accessibility | ⬜ Planned |
| 14 | The Global Embrace | ⬜ Planned |

> **Currently Active → [Phase 9](#-phase-9-the-accord-of-power-covenants--blood-sorcery)**

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
- Automate Storyteller narrative judgment — social and approval gates are intentionally lightweight.

---

## 👥 Primary Users

- **🧛 Player** — Manages characters, rolls dice, tracks conditions and aspirations.
- **🎭 Storyteller** — Runs chronicles, manages NPCs, tracks player vitals, and organizes lore.
- **🧙 Developer (The Apprentice)** — Learns modern architecture through the project's Grimoire.

---

## 📚 1. Educational Core (The Grimoire)

Every architectural choice is a learning milestone. We prioritize **Explicit Understanding over Implicit "Magic"**.

### Core Learning Goals

- **Reactive Patterns** — Master real-time state changes without page refreshes using explicit C# state management.
- **ORM Mastery** — Use **EF Core** to understand relational mapping, migrations, and performance tuning from SQLite to PostgreSQL.
- **Modern Syntax (C# 14)** — Wield Primary Constructors and enhanced collection expressions as deliberate learning milestones, reducing boilerplate to sharpen intent.
- **Identity & Security (The Masquerade)** — Deep dive into ASP.NET Core Identity, JWTs, and enterprise-grade data privacy.
- **Data-Driven Domain Modeling** — Phases 8–9 introduce the pattern of separating content (seed data) from behavior (engine logic), a critical architectural distinction.

### Learning Artifacts (Mandatory)

Every major subsystem must include:
- A `README.md` explaining **why** it exists.
- One intentionally **simple** example.
- One intentionally **wrong** example, with explanation.

### Rules Interpretation Log (Phase 8+)

Some V:tR 2e mechanics contain edge cases the rulebook does not fully resolve. For any deliberate rules interpretation made during implementation, a decision must be recorded in `docs/rules-interpretations.md` alongside the affected subsystem. This is an architectural requirement, not optional documentation.

---

## ☁️ 2. Cloud-Native & Deployment (The Global Nexus)

The application is **cloud-agnostic** by design and deployable to any sanctuary: Azure, AWS, Railway, or equivalent.

### Architectural Principles

- **The Haven (Containerization)** — All services are isolated within Docker to ensure environment parity and uncorrupted local execution.
- **Modular Monolith (The Sacred Covenants)** — Logic is partitioned into four domain-specific projects (`Application`, `Data`, `Domain`, `Web`).
- **Service Orchestration** — **.NET Aspire** manages local resources, service discovery, and configuration.
- **Stateless Scaling** — The rules engine is stateless. Sessions and character state are persisted via distributed caching (Redis).

---

## 🎨 3. UI/UX: Intuitive Immersion (The Masquerade)

- **Modern Gothic Aesthetic** — Bone-white and crimson UI optimized for dark mode.
- **The 3-Click Rule** — No core action should require more than three interactions.
- **Mobile-First Responsiveness** — Full functionality on phones and tablets.
- **Offline Capabilities (PWA)** — Deferred indefinitely. The architectural assumption is stable connectivity at the table. Real-time synchronization takes priority.
- **Accessibility (a11y) — WCAG 2.1 AA Compliance** — Contrast ratios and ARIA labels are strictly enforced.

---

## 🛡️ 4. Security & Data Integrity

- **Zero-Trust Identity** — Currently implemented via **ASP.NET Core Identity** with secure cookie authentication.
- **BOLA / IDOR Prevention** — Strict ownership checks for characters and chronicles.
- **Input Sanitization** — Strong typing and parameterized queries — no raw SQL.

---

## 🧭 5. Observability & Diagnostics

- **Structured Logging** — Correlation-ID aware, machine-queryable logs via Serilog.
- **Metrics First** — Dice rolls, XP spends, and state changes emit OpenTelemetry metrics.
- **Reproducibility** — Any defect must be explicitly reproducible via logged inputs.

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
- [x] Add Size, Speed, Defense, and Armor to the character
- [x] Add Mask and Dirge to the character
- [x] In the roll, add ability to choose the relevant ability
- [x] Humanity tracking — dot-scale Humanity stat with Stain accumulation and degradation
- [x] Vitae (Blood Pool) tracking — current/max Vitae with spend and replenish actions
- [x] Blood Potency — core vampire stat affecting feeding and power level
- [x] My Characters dashboard — character roster view with create, select, and manage actions
- [x] **Predator Type** — (Implemented in Phase 4: grants bonuses, feeding restrictions, and starting Merits/Specialties)

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

---

## 📅 Phase 4: The Storyteller & The Danse Macabre

- [x] **Initiative Tracker** — real-time initiative order using Initiative Mod mechanics
- [x] **Encounter Manager** — create, manage, and resolve combat encounters
- [x] **Storyteller Glimpse** — private dashboard showing player vitals
- [x] **Campaign Notes & Shared Lore** — collaborative lore database
- [x] **NPC Quick Stat Blocks** — pre-built and custom NPC stat blocks
- [x] **XP Distribution** — group or individual XP/Beat allocation
- [x] **Beat Tracking** — immutable, transactional log of earnings
- [x] **XP Spend Audit Trail** — full history of how XP was spent
- [x] **Condition Management** — add, view, and resolve V:tR 2e Conditions
- [x] **Tilt Management** — track combat Tilts with mechanical effects
- [x] **One-Tap Resolution** — resolving a Condition automatically awards a Beat
- [x] **Coterie Hub** — shared coterie identity, resources, and group aspirations
- [x] **Feeding Territories** — track hunting grounds and their ratings
- [x] **City Power Structure** — map the political landscape
- [x] **Touchstone Management** — track mortal Touchstones tied to Humanity anchors
- [x] **NPC Relationship Web** — relationship tracker between PCs, NPCs, and factions
- [x] **Saved Dice Macros** — save named dice pools for one-tap reuse
- [x] **Content Sharing** — export/import homebrew content packs as JSON

---

## 📅 Phase 5: Automated Deployments & Observability

- [x] Automated Versioning & Git Tagging
- [x] GitHub Environments (Staging auto-deploy, Production approvals)
- [x] GitHub Actions → AWS via OIDC
- [x] Infrastructure as Code (IaC) — **AWS CDK**
- [x] Secrets & environment variable management (AWS Secrets Manager)
- [x] Expose `/health` and `/ready` endpoints
- [x] Containerize application and push to Container Registry
- [x] Define migration deployment strategy
- [x] Post-deploy smoke test
- [x] **Performance Budget Enforcement** — CI checks fail if thresholds are exceeded

---

## 📅 Phase 6: CI/CD Hardening & Supply Chain

- [x] CodeQL scanning (C#) enforced on PRs
- [x] Dependabot updates with safe auto-merge policy
- [x] Secret scanning + push protection enabled
- [x] Container image vulnerability scanning in CI
- [x] SBOM generation for release artifacts
- [x] Image signing + provenance (keyless via GitHub OIDC)
- [x] Nightly performance regression workflow

---

## 📅 Phase 7: Realtime Play (The Blood Communion)

- [x] **Live Dice Rolls** — dice results broadcast to the coterie via SignalR
- [x] **SignalR Backplane** — configure Redis as the backplane for horizontal scaling
- [x] **Shared Initiative Tracker** — live initiative order visible to all participants
- [x] **Real-Time Character Updates** — Health, Willpower, and Condition sync
- [x] **Session Presence** — indicators showing which players are online
- [x] **Synchronized Chronicle State** — Storyteller actions push live to players
- [x] **Dice Roll History Feed** — shared feed of all rolls made during a session
- [x] **Reconnection Resilience** — clients rejoin and receive full current session state
- [x] **Rate Limiting on SignalR Hubs** — throttle message frequency per connection
- [x] **Async / Play-by-Post Dice Sharing** — shareable permanent link to a roll result

---

## 📅 Phase 8: The Hidden Blood (Bloodlines & Devotions)

**The Objective:** Implement the advanced evolution and hybridization of the Kindred form.

### Architectural Decisions

- **Content is data, behavior is code.** Bloodlines and Devotions are defined as seed data (`BloodlineDefinition`, `DevotionDefinition`) interpreted by a stable engine. A new Bloodline is a migration, not a deployment.
- **Storyteller approval is a lightweight pending state**, not a workflow engine. Mechanical prerequisites (Blood Potency, Clan) are validated automatically. Narrative approval is a single Storyteller action surfaced in the existing Storyteller Glimpse dashboard.
- **The Unified Pool Resolver** is the key architectural problem of this phase. Devotions compose dice pools from Attributes, Skills, *and* Discipline ratings — three entity types the Dice Nexus must unify before Devotion activation can be modeled cleanly. Design this first.
- **Exotic Bloodline escape hatch.** `BloodlineDefinition` includes a nullable `CustomRuleOverride` flag for mechanics that resist clean data modeling. Document every use in `docs/rules-interpretations.md`.

### Pool Resolver Scope (Phase 8)

Phase 8 supports **additive pools only** (e.g., `Strength + Brawl + Vigor`). Contested rolls (`vs Resolve + Tolerance`) and penalty dice (`Pool - Stamina`) are deferred to **Phase 9**. Document deferred formats in `docs/rules-interpretations.md`.

### Tasks

- [x] **Unified Pool Resolver** — `TraitResolver` (Application) hydrates `PoolDefinition` from Character; produces resolved integer for `DiceService`. Additive pools only; contested/penalty deferred to Phase 9.
- [x] **`BloodlineDefinition` seed data** — data model covering prerequisite Blood Potency, parent Clan, Discipline substitutions (replace or supplement), Bane descriptor, and `CustomRuleOverride`
- [x] **Bloodline Engine** — stateless domain service that reads a `BloodlineDefinition` and applies it to a character; never knows a Bloodline by name
- [x] **Bloodline Validation** — enforce Blood Potency (2+) and Clan prerequisites before the pending state is created; surface as `Result<T>` failures
- [x] **`BloodlineStatus` pending flow** — `PendingApproval` state visible to the Storyteller in the Glimpse dashboard; one-tap approve/reject with optional note
- [x] **`DevotionDefinition` seed data** — catalog from the rulebook: name, description, prerequisite Disciplines (with `OrGroupId` for OR logic), XP cost, dice pool composition, passive vs. active flag, optional `RequiredBloodlineId` for bloodline-gated devotions
- [x] **Devotion prerequisite automation** — validate required Discipline levels and XP before purchase; enforced in the Application Layer, not the UI
- [x] **Devotion activation** — active Devotions feed into the Unified Pool Resolver; passive Devotions are display-only in Phase 8 — full modifier integration deferred to Phase 9
- [x] **Character sheet and Edit Character** — Bloodlines and Devotions are first-class in the character sheet UI and editable via the Edit Character flow (add/remove devotions, apply for bloodline). Dedicated Bloodline section showing lineage and Bane; Devotions list with "Roll" buttons; cache invalidation on any lineage mutation
- [x] **Rules Interpretation Log** — document all edge-case V:tR 2e decisions in `docs/rules-interpretations.md`

---

## 📅 Phase 9: The Accord of Power (Covenants & Blood Sorcery)

**The Objective:** Codify the mystical and political structures of the Danse Macabre.

> The content/behavior separation established in Phase 8 is reused directly here. `CovenantDefinition` mirrors `BloodlineDefinition` in shape.

- [ ] **Covenant Integration** — First-class support for the five core Covenants (Carthian, Circle, Invictus, Lancea, Ordo)
- [ ] **Covenant Merits & Benefits** — Tracking "Carthian Law," "Theban Miracles," and "Invictus Oaths" with their unique mechanical triggers
- [ ] **Extend Unified Pool Resolver** — Support contested rolls ("vs" format) and penalty dice (e.g., `Pool - Stamina`). Deferred from Phase 8; document formats in `rules-interpretations.md`.
- [ ] **Passive Devotion Modifier Engine** — Define `PassiveModifier` value object (TargetStat, Delta, OptionalCondition) and integration with derived-stat cache. Deferred from Phase 8; effects that resist data modeling use `CustomRuleOverride`.
- [ ] **Blood Sorcery Module** — Dedicated UI for Crúac and Theban Sorcery; tracking Rites/Miracles with specific resource costs (Vitae vs. Willpower)
- [ ] **The Mysteries of the Dragon** — Specialized tracker for Coils and Scales, including the permanent "rule-breaking" modifiers they apply to the core character sheet logic

---

## 📅 Phase 9.5: Sacrifice Mechanics (Blood Sorcery)

**The Objective:** Implement ritual sacrifice and "Sins" mechanics associated with Crúac and Theban Sorcery rolls. Deferred from Phase 9 to keep scope manageable; builds on the Blood Sorcery foundation.

### Prerequisites

- Phase 9 Blood Sorcery Module must be complete (SorceryRiteDefinition, SorceryService, CharacterRite, activation flow).

### Scope

- [ ] **Sacrifice Types** — Define data model for sacrifice requirements (e.g., Vitae, Willpower, Humanity stain, material sacrifice) that some rites require for activation or enhanced effect.
- [ ] **Rite-Sacrifice Linking** — Link `SorceryRiteDefinition` to optional sacrifice requirements (which rites demand what, and under what conditions).
- [ ] **Sin/Stain Integration** — Logic for rites that require or inflict "Sins" (Humanity-related mechanics); integrate with existing Humanity/Stain tracking.
- [ ] **Activation Cost Extension** — Extend `ActivationCost` on rites to support sacrifice costs alongside Vitae/Willpower.
- [ ] **UI for Sacrifice** — Surface sacrifice requirements in the Blood Sorcery UI; prompt for/confirm sacrifice when activating a rite that requires it.
- [ ] **Rules Interpretation Log** — Document V:tR 2e sacrifice and sin mechanics in `docs/rules-interpretations.md`.

### Non-Goals (Phase 9.5)

- Full narrative automation of sacrifice outcomes — Storyteller judgment remains primary.
- Sacrifice mechanics for non-sorcery powers (e.g., Devotions) — defer to future phases if needed.

---

## 📅 Phase 9.6: Additional Blood Sorcery Traditions (Necromancy & Ordo Dracul)

**The Objective:** Extend the Blood Sorcery module to support Necromancy and Ordo Dracul rituals. Deferred from Phase 9 to keep scope focused on Crúac and Theban Sorcery.

### Prerequisites

- Phase 9 Blood Sorcery Module complete (Crúac, Theban Sorcery, Discipline model, rite purchase flow).

### Scope

- [ ] **Necromancy** — Add Necromancy as a covenant-gated Discipline (Mekhet-associated or open). Seed rituals from source material (corpse manipulation, ghost binding, Avernian Gates, etc.).
- [ ] **Ordo Dracul Rituals** — Add Ordo Dracul–specific blood sorcery rituals (e.g., Dragon's Own Fire, Taste of the Dragon, Pasha's Vision, Kale-Kob, Intikam, Burn the Dragon's Blood).
- [ ] **Data model extension** — Extend `SorceryRiteDefinition` / seeding pipeline to support additional `SorceryType` values and covenant gating.
- [ ] **UI** — Surface Necromancy and Ordo rituals in the Blood Sorcery UI for eligible characters.
- [ ] **Rules Interpretation Log** — Document Necromancy and Ordo ritual mechanics in `docs/rules-interpretations.md`.

### Non-Goals (Phase 9.6)

- Phase 9.5 Sacrifice Mechanics — handled separately.
- Exotic or homebrew blood sorcery traditions — defer to future phases.
