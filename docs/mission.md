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
| 9 | The Accord of Power (Covenants & Blood Sorcery) | ✅ Complete |
| 9.5 | Sacrifice Mechanics (Blood Sorcery) | ✅ Complete |
| 9.6 | Additional Blood Sorcery (Necromancy & Ordo Dracul) | ✅ Complete |
| 10 | The Social Graces (Social Maneuvering) | ✅ Complete |
| 11 | Assets & Armory (Equipment & Services) | ✅ Complete |
| 12 | The Web of Night (Relationship Webs) | ✅ Complete |
| 13 | End-to-End Testing & Accessibility | ✅ Complete |
| 14 | The Danse Macabre — Combat & Wounds | ✅ Complete |
| 15 | The Beast Within — Frenzy & Torpor | ✅ Complete |
| 16a | The Hunting Ground — Feeding | ✅ Complete |
| 16b | The Discipline Engine — Power Activation | ⬜ Planned |
| 17 | The Fog of Eternity — Humanity & Condition Wiring | ⬜ Planned |
| 18 | The Wider Web — Edge Systems & Content | ⬜ Planned |
| 19 | The Blood Lineage — Discipline Acquisition Rules | ⬜ Planned |
| 20 | The Global Embrace | ⬜ Planned |

> **Phase 16a — The Hunting Ground (Feeding) is complete** (`IHuntingService`, `HuntPanel`, hunt ledger). **Phase 16b** (Discipline power activation) remains **blocked on Phase 19** (`DisciplinePower.PoolDefinitionJson`). Phases 14–19 are the **V:tR 2e Playability Gap** — full scope in this document and [`docs/rules-interpretations.md`](./rules-interpretations.md). **Phase 20 — The Global Embrace** (i18n, public API, Discord presence, production polish) is the **last planned phase** after playability work. Phases 14–16a are **complete** — see phase sections below. Phase 13 (E2E Playwright suite, axe/Lighthouse CI, screen-reader announcer, visual-regression workflow) is **complete** — run local browser tests with `scripts/test-e2e-local.ps1`.

---

## 🗺️ Playability Dependency Graph (Phases 14–19)

```
Phase 14 (Combat) ✅
    ├──► Phase 15 (Frenzy/Torpor) ✅      ← VitaeDepletedEvent
    │         └──► Phase 17 (Humanity)    ← DegenerationCheckRequired UI
    └──► Phase 17 (Humanity)              ← WoundPenaltyResolver in ModifierService

Phase 16a (Hunting) ✅  ← independent
Phase 19  (Disciplines — model + seed)   ← independent; start now
    └──► Phase 16b (Discipline Activation)  ← needs PoolDefinitionJson from Phase 19

Phase 18 (Edge Systems) ← fully independent; content passes any time
```

**Recommended parallel tracks:**
- Track A: ~~14 → 15~~ ✅ → **Phase 17** next
- Track B: **Phase 19** → **Phase 16b** (discipline chain)
- Track C: **Phase 18** (independent, any time)

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
- Automate **chases or mass combat** — these VtR 2e mechanical frames are out of scope; Storytellers manage them manually.
- Automate **merged pools / coordinated actions** across multiple characters — handled manually via the dice modal.
- Cover supplements beyond the **VtR 2e core book** in Phase 18 content passes — supplement catalogs are future work.

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
- **Identity & Security (The Masquerade)** — Deep dive into ASP.NET Core Identity, cookie-based sessions for the first-party Blazor app, and enterprise-grade data privacy. Bearer tokens (e.g. JWT) are in scope when a public API ships (Phase 20 — The Global Embrace), not for the primary UI today.
- **Data-Driven Domain Modeling** — Phases 8–11 extend the pattern of separating content (seed data) from behavior (engine logic): Bloodlines/Devotions and Covenants/Sorcery (8–9), social maneuvers and clues (10), asset catalog and inventory (11).

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

- **Zero-Trust Identity** — Implemented via **ASP.NET Core Identity** with **secure cookie authentication** for the Blazor web app. There is no JWT bearer scheme for browser sessions today.
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

Phase 8 supported **additive pools only**; contested rolls and penalty dice were deferred and implemented in Phase 9.

### Tasks

- [x] **Unified Pool Resolver** — `TraitResolver` (Application) hydrates `PoolDefinition` from Character; produces resolved integer for `DiceService`. Additive pools in Phase 8; contested/penalty implemented in Phase 9.
- [x] **`BloodlineDefinition` seed data** — data model covering prerequisite Blood Potency, parent Clan, Discipline substitutions (replace or supplement), Bane descriptor, and `CustomRuleOverride`
- [x] **Bloodline Engine** — stateless domain service that reads a `BloodlineDefinition` and applies it to a character; never knows a Bloodline by name
- [x] **Bloodline Validation** — enforce Blood Potency (2+) and Clan prerequisites before the pending state is created; surface as `Result<T>` failures
- [x] **`BloodlineStatus` pending flow** — `PendingApproval` state visible to the Storyteller in the Glimpse dashboard; one-tap approve/reject with optional note
- [x] **`DevotionDefinition` seed data** — catalog from the rulebook: name, description, prerequisite Disciplines (with `OrGroupId` for OR logic), XP cost, dice pool composition, passive vs. active flag, optional `RequiredBloodlineId` for bloodline-gated devotions
- [x] **Devotion prerequisite automation** — validate required Discipline levels and XP before purchase; enforced in the Application Layer, not the UI
- [x] **Devotion activation** — active Devotions feed into the Unified Pool Resolver; passive Devotions display-only in Phase 8 — full modifier integration implemented in Phase 9.
- [x] **Character sheet and Edit Character** — Bloodlines and Devotions are first-class in the character sheet UI and editable via the Edit Character flow (add/remove devotions, apply for bloodline). Dedicated Bloodline section showing lineage and Bane; Devotions list with "Roll" buttons; cache invalidation on any lineage mutation
- [x] **Rules Interpretation Log** — document all edge-case V:tR 2e decisions in `docs/rules-interpretations.md`

---

## 📅 Phase 9: The Accord of Power (Covenants & Blood Sorcery) ✅

**The Objective:** Codify the mystical and political structures of the Danse Macabre.

> The content/behavior separation established in Phase 8 is reused directly here. `CovenantDefinition` mirrors `BloodlineDefinition` in shape.

- [x] **Covenant Integration** — First-class support for the five core Covenants (Carthian, Circle, Invictus, Lancea, Ordo)
- [x] **Covenant Merits & Benefits** — Tracking "Carthian Law," "Theban Miracles," and "Invictus Oaths" with their unique mechanical triggers
- [x] **Extend Unified Pool Resolver** — Support contested rolls ("vs" format) and penalty dice (e.g., `Pool - Stamina`). Documented in `rules-interpretations.md`.
- [x] **Passive Devotion Modifier Engine** — `PassiveModifier` value object (TargetStat, Delta, OptionalCondition) integrated with derived-stat cache. Effects that resist data modeling use `CustomRuleOverride`.
- [x] **Blood Sorcery Module** — Dedicated UI for Crúac and Theban Sorcery; tracking Rites/Miracles with specific resource costs (Vitae vs. Willpower)
- [x] **The Mysteries of the Dragon** — Specialized tracker for Coils and Scales, including the permanent "rule-breaking" modifiers they apply to the core character sheet logic

---

## 📅 Phase 9.5: Sacrifice Mechanics (Blood Sorcery)

**The Objective:** Implement ritual sacrifice and "Sins" mechanics associated with Crúac and Theban Sorcery rolls. Deferred from Phase 9 to keep scope manageable; builds on the Blood Sorcery foundation.

### Prerequisites

- Phase 9 Blood Sorcery Module must be complete (SorceryRiteDefinition, SorceryService, CharacterRite, activation flow).

### Scope

- [x] **Sacrifice Types** — `SacrificeType` enum and `RiteRequirement` (JSON in `SorceryRiteDefinition.RequirementsJson`).
- [x] **Rite-Sacrifice Linking** — Per-rite `RequirementsJson` interpreted by `RiteRequirementValidator` and `SorceryService.BeginRiteActivationAsync`.
- [x] **Sin/Stain Integration** — `HumanityStain` requirements update `Character.HumanityStains`; degeneration rolls remain ST/table (documented in `rules-interpretations.md`).
- [x] **Activation Cost Extension** — Structured costs alongside display `ActivationCostDescription`; paid activation before pool resolution.
- [x] **UI for Sacrifice** — Activation cost text on the sheet; browser `confirm` when narrative acknowledgments are required; `BeginRiteActivationAsync` applies costs then opens the roller.
- [x] **Rules Interpretation Log** — Phase 9.5/9.6 entries in `docs/rules-interpretations.md`.

### Non-Goals (Phase 9.5)

- Full narrative automation of sacrifice outcomes — Storyteller judgment remains primary.
- Sacrifice mechanics for non-sorcery powers (e.g., Devotions) — defer to future phases if needed.

---

## 📅 Phase 9.6: Additional Blood Sorcery Traditions (Necromancy & Ordo Dracul)

**The Objective:** Extend the Blood Sorcery module to support Necromancy and Ordo Dracul rituals. Deferred from Phase 9 to keep scope focused on Crúac and Theban Sorcery.

### Prerequisites

- Phase 9 Blood Sorcery Module complete (Crúac, Theban Sorcery, Discipline model, rite purchase flow).

### Scope

- [x] **Necromancy** — `Necromancy` discipline; `SorceryType.Necromancy`; Mekhet clan gate via `RequiredClanId`; sample rite `Corrupting the Corpse` in `DbInitializer.EnsureBloodSorceryPhaseExtensionsAsync` (catalog can grow from seed/JSON).
- [x] **Ordo Dracul Rituals** — `SorceryType.OrdoDraculRitual`, `CovenantDefinition.SupportsOrdoRituals`, `Ordo Sorcery` discipline track for pools; sample rite `Dragon's Own Fire` (further rites: same pipeline).
- [ ] **Expanded ritual catalog** — Full rulebook/supplement list (e.g. Taste of the Dragon, Pasha's Vision, …) deferred to content passes; structure is in place.
- [x] **Data model extension** — `SorceryType` values, nullable `RequiredCovenantId`, `RequiredClanId`, `RequirementsJson`, migration `Phase95Phase96BloodSorceryExtensions`.
- [x] **UI** — Blood Sorcery section for Crúac/Theban, Ordo (`SupportsOrdoRituals`), or any character with Necromancy dots; rite requests from **Advancement** (`ApplyLearnRiteModal`); sheet uses activation only; modal labels all traditions.
- [x] **Rules Interpretation Log** — Necromancy/Ordo pool and gating decisions in `docs/rules-interpretations.md`.
- [ ] **Temporary ritual-granted Coils/Scales** — Deferred; no timed `PassiveModifier` from rites yet—Storyteller applies table-side or via existing tools.

### Non-Goals (Phase 9.6)

- Phase 9.5 Sacrifice Mechanics — handled separately.
- Exotic or homebrew blood sorcery traditions — defer to future phases.
## 📅 Phase 10: The Social Graces (Social Maneuvering)

**The Objective:** Automate the formal systems of social dominance, investigation, and manipulation.

- [x] **Core persistence & engine** — `SocialManeuver` / `ManeuverClue` (EF), `SocialManeuveringEngine` + `ISocialManeuveringService` (NPC-only targets, Masquerade, server-side open/force rolls, hard leverage, hostile-week failure). See `docs/PHASE_10_SOCIAL_GRACES.md`.
- [x] **Doors Tracker** — Glimpse + character sheet show remaining/total Doors; SignalR `ReceiveSocialManeuverUpdate` refreshes connected clients (`docs/PHASE_10_SOCIAL_GRACES.md`).
- [x] **Impression Management** — Impression shown on sheet and ST dropdown on Glimpse; open-Door timing enforced server-side and reflected in sheet copy/countdown.
- [x] **Leverage UI** — Hard leverage (breaking-point severity + door removal before force) on Glimpse and character sheet; soft/narrative leverage remains ST/table roleplay per book.
- [x] **Investigation Support** — Clue banking (configurable successes-per-clue per chronicle), ST manual clues, spend workflow; **Interception** deferred (see `docs/PHASE_10_SOCIAL_GRACES.md` §8)
- [x] **Social Condition Integration** — *Inspired* (exceptional open-Door), *Shaken* (dramatic failure, failed force/Burnt, hostile-week failure), *Swooned* (maneuver success open or forced); skips duplicate active Condition of the same type

---

## 📅 Phase 11: Assets & Armory (Equipment & Services)

**The Objective:** Standardize physical assets and their mechanical impact on play.

- [x] **Unified Asset Schema** — Core `Asset` with **TPT** (`EquipmentAsset` / `WeaponAsset` / `ArmorAsset` / `ServiceAsset`) and **`AssetCapability`** for multi-role items (e.g. Crowbar tool + weapon profile); see `docs/rules-interpretations.md`.
- [x] **Character Inventory (`CharacterAsset`)** — Ownership, **Structure**, **Equipped**, and **ready slots**; skill/service bonuses from gear apply only while equipped and non-broken.
- [x] **The Procurement Engine** — Resources vs. Availability; suggested procurement pool when below threshold; **`isIllicit`** → pending row and Storyteller approve/reject on Glimpse.
- [x] **Services (book)** — Services modeled with skill assist and **recurring Resources** cost in seed data; bonuses follow the same equipped rules as other gear where applicable.
- [x] **Dice Nexus Modifier Injection** — `DiceRollerModal` / sheet paths use `ITraitResolver` with equipment modifiers (skill cap, weapon damage, strength under-requirement penalty).
- [x] **Armor Mitigation Logic** — General and ballistic ratings plus Defense/Speed from equipped armor on the sheet; **automated damage-type conversion** deferred until an attack pipeline exists (documented in rules log).
- [x] **The Armory UI (The Pack)** — Pack tab with equip/structure/ready slots and procurement entry points.
- [x] **Seed Catalog Integration** — JSON seeds under [`src/RequiemNexus.Data/SeedSource/`](../src/RequiemNexus.Data/SeedSource/) for general items, weapons, armor, and services (copied with the Data assembly at build/publish). Any copies under `docs/` are optional mirrors for readability only; **SeedSource is authoritative** for runtime seeding.

---

## 📅 Phase 12: The Web of Night (Relationship Webs)

**The Objective:** Visualize and automate the spiritual and social ties that govern Kindred life.

- [x] **Blood Ties & Sympathy** — Real-time tracking of family trees (Sires, Childer) and the "Blood Sympathy" sense across distances
- [x] **Blood Bond Tracker** — Automated tracking of the three stages of the Blood Bond, including the specific Conditions they impose on the thrall
- [x] **Predatory Aura Interaction** — A dedicated UI for "Lashing Out" with the Predatory Aura, automating contested Blood Potency rolls and the resulting *Beaten* or *Shaken* states
- [x] **Ghoul Management** — Support for mortal retainers, tracking their Vitae dependency, monthly aging checks, and minor Discipline access

---

## 📅 Phase 13: End-to-End Testing & Accessibility ✅

**The Objective:** Harden the ecosystem for all users and ensure the Gothic aesthetic remains usable.

**Delivered:** Playwright E2E tests in `tests/RequiemNexus.E2E.Tests` (smoke, accessibility page scans, and critical-path coverage as implemented in that project). CI runs **axe**-based scans and **Lighthouse** audits via `.github/workflows/e2e.yml` and `.github/workflows/lighthouse.yml`. Real-time dice and Blood Sorcery feedback is announced through **ARIA live regions** (screen-reader announcer). **Visual regression** baselines live in `tests/RequiemNexus.VisualRegression.Tests` and run in the E2E workflow; refresh snapshots as documented in `Contributing.md`. Shared layout chrome and design tokens were aligned with this work so a11y and visual checks stay meaningful.

- [x] **Full E2E Playwright Suite** — Critical paths including character advancement, social maneuvers, and pack / procurement / equipment flows (as covered by the E2E project)
- [x] **Automated Accessibility Scanning** — WCAG-oriented audits integrated into CI (axe page scans and Lighthouse)
- [x] **Screen Reader Optimization** — Real-time dice rolls and Blood Sorcery results announced via ARIA live regions
- [x] **Visual Regression Baseline** — Playwright snapshot workflow for UI drift in the bone-white and crimson palette (see `Contributing.md` for optional baseline updates)

---

## 📅 Phase 14: The Danse Macabre — Combat & Wounds ✅

**The Objective:** Build the attack-to-damage pipeline so initiative resolution produces real mechanical outcomes.

**Status:** ✅ **Complete**

- [x] `AttackResult` value object — successes, weapon dice, `DamageSource` (Bashing / Lethal / Aggravated / Fire / Sunlight / Weapon)
- [x] `AttackService` — melee-first MVP; reads existing `character.Defense` derived stat
- [x] `CharacterHealthService` — B/L/A overflow rules (p.172); damage applied to health boxes
- [x] `WoundPenaltyResolver` — injects `PassiveModifier(Target = WoundPenalty)` into existing `ModifierService.GetModifiersForCharacterAsync`
- [x] Healing via `CharacterHealthService.TryFastHealBashingWithVitaeAsync` — `HealingReason` enum; fast-heal costs enforced as Domain constants (`VitaeHealingCosts`)
- [x] Combat UI — Attack Panel (`MeleeAttackResolveModal`, Glimpse + Tracker) and Heal Panel (sheet + Glimpse); NPC health track (`NpcCombatService`, `HealthDamageTrackBoxes`)
- [x] Rules Interpretation Log — MVP boundary, Defense vs. firearms, B/L/A edge cases

---

## 📅 Phase 15: The Beast Within — Frenzy & Torpor ✅

**The Objective:** Give the Beast teeth — automated frenzy saves and torpor state tracking.

**Status:** ✅ **Complete**

- [x] `FrenzyTrigger` enum — Hunger, Rage, Rotschreck, Starvation
- [x] `FrenzyService` — `Resolve + Blood Potency` save; tilt application guarded by beast-active check; Willpower optional spend path
- [x] `VitaeService` + `WillpowerService` — Masquerade-checked spend/gain; `VitaeDepletedEvent` → Hunger frenzy auto-trigger via `VitaeDepletedEventHandler`
- [x] `TorporSince` + `LastStarvationNotifiedAt` on `Character` (migration `Phase15TorporState`); `TorporService` — enter, awaken, starvation-interval check with `TorporDurationTable`
- [x] `TorporIntervalService : BackgroundService` — follows `SessionTerminationService` pattern; configurable cadence (default 24 h via `Torpor:IntervalHours`)
- [x] `DomainEventDispatcher` + `IDomainEventHandler<T>` — in-process domain event infrastructure
- [x] Frenzy/torpor UI — `HealthDamageTrackBoxes` component; torpor badge + enter/awaken panels on character sheet and ST Glimpse
- [x] Rules Interpretation Log — torpor duration table, Rötschreck pool, hunger escalation, one-Vitae awakening cost

---

## 📅 Phase 16a: The Hunting Ground — Feeding ✅

**The Objective:** First-class hunting rolls wired to Predator Type, with resonance outcomes.

**Status:** ✅ **Complete**

- [x] `PredatorType` on `Character`, `HuntingPoolDefinition` seed (9 rows) + unique index, `HuntingRecord` ledger
- [x] `IHuntingService` / `HuntingService` — `ExecuteHuntAsync(characterId, userId, territoryId?)`; territory campaign alignment; pool floor; `ResonanceOutcome` via static thresholds (no JSON table)
- [x] Vitae gain via `IVitaeService.GainVitaeAsync`; dice feed via `PublishDiceRollAsync`; structured logging
- [x] `HuntPanel.razor` on character vitals — optional territory picker, `aria-live` announcer, resonance display
- [x] `HuntingServiceTests` (Application.Tests); rules log — **Phase 16a** in [`docs/rules-interpretations.md`](./rules-interpretations.md)

---

## 📅 Phase 16b: The Discipline Engine — Power Activation

**The Objective:** Activate Discipline powers with cost enforcement and pool resolution.

**Dependency:** Phase 19 must ship `DisciplinePower.PoolDefinitionJson` first.

### Architectural Decisions

- **Discipline activation is a wrapper around the existing `TraitResolver`.** `DisciplineActivationService` (Application) receives a `disciplinePowerId` and `characterId`, reads `DisciplinePower.PoolDefinitionJson` and `Cost`, calls `TraitResolver`, deducts cost, and posts the result to the dice feed.
- **Cost deduction is atomic.** Vitae and Willpower spends go through `VitaeService` / `WillpowerService` — no separate code path.
- **Powers with null `PoolDefinitionJson` remain display-only** — the "Activate" button is suppressed until the content pass populates their pool.

- [ ] `DisciplineActivationService` — Application: `ActivatePowerAsync(characterId, disciplinePowerId)`; reads `PoolDefinitionJson`, resolves via `TraitResolver`, deducts `ActivationCost`, posts to dice feed; Masquerade ownership check
- [ ] `ActivationCost` value object — Domain: parses `DisciplinePower.Cost` string (`"1 Vitae"`, `"1 Willpower"`, `"—"`) into typed cost; enforced before rolling
- [ ] Discipline activation UI — character sheet Disciplines section: "Activate" button per power with populated pool; cost-preview modal → confirm → result in dice feed; null-pool powers remain display-only
- [ ] Rules Interpretation Log — cost enforcement choices, pool edge cases not covered by existing `TraitResolver` contract

---

## 📅 Phase 17: The Fog of Eternity — Humanity & Condition Wiring

**The Objective:** Automate degeneration rolls and wire all Condition penalties into the dice pool.

### Architectural Decisions

- **Degeneration is a triggered roll, not an automatic loss.** When `HumanityStains` crosses the threshold for the current Humanity dot, `HumanityService` raises `DegenerationCheckRequired(Reason = StainsThreshold)`. The Storyteller sees a Glimpse banner; clicking it fires a `Resolve + (7 − Humanity)` roll and auto-applies the result.
- **Condition penalties are a modifier source, not special-cased code.** Each canonical `ConditionType` gains a nullable `PenaltyModifierJson` column. `ModifierService` reads active conditions and injects their penalties into `TraitResolver` alongside equipment and Coil modifiers. Homebrew / custom condition types have `PenaltyModifierJson = null`; the ST applies custom penalties by hand.
- **Remorse / anchor checks are explicit ST actions.** `TouchstoneService.RollRemorseAsync` rolls `Humanity` dice (chance die at Humanity 0). An active Touchstone adds +1 die.

**Shared event (defined once, used by Phase 17 and Phase 19):**

```csharp
record DegenerationCheckRequired(int CharacterId, DegenerationReason Reason);
enum DegenerationReason { StainsThreshold, CrúacPurchase }
```

- [ ] **`PenaltyModifierJson` on canonical Conditions** — Data: nullable JSON column; migration. Canonical penalties: Shaken (−2 pools), Exhausted (−2 physical), Frightened (−2 except fleeing), Guilty (−1 Resolve + Composure), Despondent (−2 Mental), Provoked (−1 Composure), Blind (−3 attack / −2 other), Stunned (no action flag). Homebrew types: `null`.
- [ ] **`ModifierService` — Condition source integration** — `ConditionModifierSource` added to aggregation loop; reads active `CharacterCondition` rows, maps `ConditionType` → `PenaltyModifierJson`, injects into `TraitResolver` call
- [ ] **`HumanityService.EvaluateStainsAsync`** — raises `DegenerationCheckRequired` at stain threshold
- [ ] **Degeneration roll UI** — Glimpse banner → `Resolve + (7 − Humanity)` → auto-apply result (success: clear stains; failure: remove dot + clear stains; dramatic failure: remove dot + apply `Guilty`)
- [ ] **`TouchstoneService.RollRemorseAsync`** — voluntary remorse roll; Touchstone adds +1 die; applies outcome via `HumanityService`
- [ ] **Remorse UI** — "Roll Remorse" button on character sheet and Glimpse (active when stains are present but below degeneration threshold)
- [ ] **Incapacitated flag** — UI suppression on player sheet only; ST Glimpse bypasses for coup de grâce / death-condition tests
- [ ] **Rules Interpretation Log** — degeneration threshold formula, Touchstone bonus justification, stain-clearing behavior on both degeneration outcomes

---

## 📅 Phase 18: The Wider Web — Edge Systems & Content

**The Objective:** Close low-priority mechanical gaps and fill the core-book content catalog.

### Architectural Decisions

- **Passive Predatory Aura reuses existing `PredatoryAuraContest` infrastructure.** The `IsLashOut` column reserved in Phase 12 drives the distinction. "Same scene" = two vampires in the same `CombatEncounter` (automatic) or ST manual trigger from the Glimpse NPC panel. No ambient scene detection beyond `CombatEncounter` — a session/location entity would require new scope. Decision recorded in `rules-interpretations.md`.
- **Blood Sympathy rolls are a thin wrapper.** `BloodSympathyService` already calculates the pool; `RollBloodSympathyAsync` submits it to `DiceService` and posts to the dice feed. No new entity needed.
- **Social Maneuvering interception adds a third party to an existing `SocialManeuver`.** A `ManeuverInterceptor` join entity links a second character to an active maneuver. `SocialManeuveringEngine` checks for interceptors before applying door reductions; an interceptor may contest the roll (Manipulation + Persuasion vs. initiator).
- **Content passes are data migrations, not code changes.** All rite / Coil / Devotion catalog expansions are JSON seed additions to `SeedSource/` and a `DbInitializer` extension call — no business logic changes required.

**Passive Predatory Aura**
- [ ] `PassiveAuraService` — Application: `TriggerPassiveContestAsync(vampireAId, vampireBId)`; calls existing `PredatoryAuraService` with `IsLashOut = false`; both characters must be in a shared Chronicle
- [ ] Scene context hook — when two vampires are added to the same `CombatEncounter`, `PassiveAuraService` auto-invokes for any pair not yet contested that scene
- [ ] UI — "Passive aura contest" notification in dice feed; outcome Conditions applied via existing logic; ST manual toggle from Glimpse NPC panel

**Blood Sympathy**
- [ ] `BloodSympathyService.RollBloodSympathyAsync` — `Wits + Empathy + BloodSympathyRating`; posts to dice feed
- [ ] UI — "Sense Blood Kin" button on character sheet Lineage section; select target from known kindred; result in dice feed

**Social Maneuvering Interception**
- [ ] `ManeuverInterceptor` entity — Data: `SocialManeuverId`, `InterceptorCharacterId`, `IsActive`, `Successes`; migration
- [ ] `SocialManeuveringEngine` interception logic — check for active interceptors before door-reduction rolls; net successes subtract from effective door reductions
- [ ] ST UI — "Add Interceptor" to any active maneuver on Glimpse; interceptor roll via existing dice modal
- [ ] Rules Interpretation Log — `PassiveAuraService` "same scene" definition; interception pool and tie-breaking

**Content Passes (data-only)**
- [ ] Theban Sorcery full catalog — all Miracles from VtR 2e core book in `bloodSorceryRites.json`
- [ ] Crúac full catalog — all Rites from VtR 2e core book
- [ ] Ordo Dracul Coil catalog — all 5 Mysteries × 5 Coils in `coils.json`
- [ ] Necromancy catalog expansion — additional rites beyond Phase 9.6 sample
- [ ] Devotion catalog expansion — remaining clan/covenant-specific Devotions in `devotions.json`
- [ ] Loresheet Merits — `Merit` seed additions for Loresheet entries from core book

---

## 📅 Phase 19: The Blood Lineage — Discipline Acquisition Rules & Seed Pipeline

**The Objective:** Enforce the acquisition rules from `DisciplinesRules.txt`, promote `Disciplines.json` to authoritative seed source, and add `PoolDefinitionJson` to unblock Phase 16b.

### Current State (what's broken)

| Component | Problem |
|-----------|---------|
| `Disciplines.json` | Exists in `SeedSource/` but is **not read by `DbInitializer`**. `DisciplineSeedData.cs` is the actual seed — the JSON is dead weight. |
| `Discipline` entity | Missing: `CanLearnIndependently`, `RequiresMentorBloodToLearn`, `IsCovenantDiscipline`, `CovenantId`, `IsBloodlineDiscipline`, `BloodlineId`. |
| `DisciplinePower` entity | No `PoolDefinitionJson` — Phase 16b activation cannot resolve per-power pools. |
| `CharacterDisciplineService` | Validates XP and in-clan status only. Zero enforcement of teacher, Covenant Status, Theban Humanity floor, Crúac cap, or bloodline restrictions. |
| Character creation | "3 dots: ≥2 must be in-clan, 1 free" not validated anywhere. |
| Power names | Celerity / Resilience / Vigor use placeholder names (`"Celerity 1"`, etc.) not rulebook names. |

### Acquisition Rules Reference

| Rule | Gate type | Enforcement |
|------|-----------|-------------|
| ≥2 of 3 creation dots must be in-clan | Hard | `CharacterCreationService` |
| Animalism, Celerity, Obfuscate, Resilience, Vigor — learn independently | Hard allow | no teacher flag required |
| Auspex, Dominate, Majesty, Nightmare, Protean out-of-clan — require teacher + Vitae drink | Soft (ST-acknowledged) | `CharacterDisciplineService` |
| Crúac, Theban, Coils — require Covenant Status + teacher | Hard (overridable by ST for Covenant gate only — "stolen secrets") | `CharacterDisciplineService` + `CovenantMembershipService` |
| Theban Sorcery dot N requires Humanity ≥ N | Hard | `CharacterDisciplineService` |
| Crúac dot 1 is a breaking point at Humanity 4+ | Event | raise `DegenerationCheckRequired(CrúacPurchase)` |
| Crúac permanently caps Humanity at `10 − CrúacRating` | Derived stat | `HumanityService.GetEffectiveMaxHumanity` |
| Bloodline Disciplines — bloodline members only | Hard | `CharacterDisciplineService` (check `CharacterBloodline`) |
| Necromancy — Mekhet-clan OR Necromancy bloodline OR ST-acknowledged cultural connection | Soft (ST-acknowledged) | `CharacterDisciplineService` |

### Architectural Decisions

- **`Disciplines.json` becomes authoritative; `DisciplineSeedData.cs` is retired.** A `DisciplineJsonImporter` in `DbInitializer` reads the JSON using the same `JsonSerializerOptions` pattern as other importers. `DisciplineSeedData.cs` is deleted once the importer is verified in integration tests.
- **Acquisition gates are soft or hard depending on verifiability.** Teacher presence and Vitae-drinking cannot be verified by the app — these use an `AcquisitionAcknowledgedByST` bool on the purchase DTO. Mechanical prerequisites (Covenant Status, Humanity, bloodline) are hard gates enforced in code.
- **`DisciplinePower.PoolDefinitionJson` mirrors `DevotionDefinition.PoolDefinitionJson`** — same `PoolDefinition` serialization format and `TraitResolver` contract. Phase 16b reads this column directly.
- **Crúac Humanity cap is a derived modifier, not stored.** `HumanityService.GetEffectiveMaxHumanity(character)` returns `10 − CrúacRating`. If future mechanics add additional ceilings, they are `Math.Min`-composed at that point.
- **Covenant Status is a hard gate overridable by the ST for covenant Disciplines only.** When `AcquisitionAcknowledgedByST = true`, the Status check is bypassed and audited in the ledger as `" | gate-override stUserId={userId} {timestamp:O}"`. Bloodline restrictions and Theban Humanity floor remain always-hard.
- **Necromancy "cultural connection" is a soft gate.** `Discipline.IsNecromancy` gates a dedicated soft-gate path: if the character is not Mekhet-clan and has no Necromancy bloodline, `AcquisitionAcknowledgedByST = true` is required. The ST confirmation modal quotes all three eligible conditions verbatim from `DisciplinesRules.txt`.

**Data model & migration**
- [ ] Add acquisition metadata to `Discipline` entity — `CanLearnIndependently`, `RequiresMentorBloodToLearn`, `IsCovenantDiscipline`, `CovenantId` (int?, FK), `IsBloodlineDiscipline`, `BloodlineId` (int?, FK), `IsNecromancy`; migration `Phase19DisciplineAcquisitionMetadata`
- [ ] Add `PoolDefinitionJson` to `DisciplinePower` — nullable string, same contract as `DevotionDefinition.PoolDefinitionJson`; same migration batch
- [ ] Extend `Disciplines.json` schema — add acquisition fields to all 12 core disciplines + bloodline disciplines; populate `PoolDefinitionJson` from `DisciplinesRules.txt` (null where not detailed)

**Seed pipeline**
- [ ] `DisciplineJsonImporter` — `RequiemNexus.Data`; follows `CovenantJsonImporter` pattern; upsert by name; called from `DbInitializer.EnsureDisciplinesAsync`
- [ ] Retire `DisciplineSeedData.cs` — delete after importer verified by integration tests; record switch in `rules-interpretations.md`
- [ ] Fix Celerity / Resilience / Vigor power names to rulebook names in `Disciplines.json`

**Acquisition rule enforcement**
- [ ] `DisciplineAcquisitionRequest` DTO — `DisciplineId`, `TargetRating`, `AcquisitionAcknowledgedByST` (bool); replaces bare parameters
- [ ] Hard gate: bloodline restriction — `CharacterDisciplineService`: if `IsBloodlineDiscipline`, character must have matching `CharacterBloodline`; `Result.Failure` if not
- [ ] Hard gate (overridable): Covenant Status — if `IsCovenantDiscipline`, require active matching `CovenantMembership`; when `AcquisitionAcknowledgedByST = true`, bypass and audit ledger note
- [ ] Hard gate: Theban Humanity floor — if Theban Sorcery and `TargetRating > character.Humanity`, `Result.Failure`
- [ ] Soft gate: teacher + Vitae — if `RequiresMentorBloodToLearn` and out-of-clan, require `AcquisitionAcknowledgedByST = true`; ST confirmation modal
- [ ] Crúac breaking point — on first Crúac purchase at Humanity ≥ 4, raise `DegenerationCheckRequired(CrúacPurchase)`
- [ ] Necromancy gate — if `IsNecromancy` and not Mekhet-clan and no Necromancy bloodline, require `AcquisitionAcknowledgedByST = true`; modal quotes all three eligible conditions
- [ ] Soft gate audit — append `" | gate-override stUserId={userId} {timestamp:O}"` to `XpLedgerEntry.Notes` for all ST-acknowledged purchases; format recorded in `rules-interpretations.md`
- [ ] Crúac Humanity cap — `HumanityService.GetEffectiveMaxHumanity` returns `10 − CrúacRating`; displayed on character sheet

**Character creation**
- [ ] 2-of-3 in-clan minimum — `CharacterCreationService`: count in-clan dots; `Result.Failure` if fewer than 2; inline validation error in creation UI
- [ ] Third-dot Covenant gate — if third creation dot targets Crúac / Theban / Coils without Covenant Status, surface ST confirmation prompt

**UI**
- [ ] Acquisition gate feedback — Advancement page: hard gates show descriptive tooltip; soft gates show ST confirmation modal with rule quoted verbatim
- [ ] Crúac Humanity cap badge — "Max Humanity: X (capped by Crúac •••)" on character sheet when `CrúacRating > 0`
- [ ] Power pool display — when `DisciplinePower.PoolDefinitionJson` is populated, show resolved pool formula on character sheet (same pattern as Devotion display)

**Rules Interpretation Log**
- [ ] Record in `docs/rules-interpretations.md`: soft vs. hard gate choices, Crúac breaking-point threshold (Humanity 4+), Theban floor formula, `DisciplineSeedData.cs` → JSON migration rationale

---

## 📅 Phase 20: The Global Embrace

**The Objective:** Final polish and expansion into the international community. **This is the last planned roadmap phase** — it follows the V:tR 2e playability work in Phases 14–19.

- [ ] **Localization (i18n)** — Full support for French, German, and Spanish, adhering to the "Sacred Term Policy" (e.g., *Discipline* remains *Discipline*)
- [ ] **Public REST API** — Documented endpoints for community developers to build third-party companion tools; **external client auth** (typically JWT or OAuth2 access tokens) is introduced here. The first-party Blazor app remains cookie-based Identity.
- [ ] **Discord Rich Presence** — Enhanced webhooks for detailed session summaries and "Coterie Status" updates
- [ ] **Production Rollout** — Final optimization of SignalR hubs for high-concurrency public traffic

---

> _The blood remembers._
> _The code must too._