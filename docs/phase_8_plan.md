# 🩸 Phase 8: The Hidden Blood (Bloodlines & Devotions)

## 🌌 Overview
Phase 8 introduces the advanced evolution of the Kindred: **Bloodlines** and **Devotions**. This phase codifies the structural hybridization of the vampire form, moving beyond the five core Clans and basic Disciplines.

## 🧱 Architectural Pillar: Content vs. Behavior
Following the **Antigravity Philosophy**, we distinguish between **Content** (data) and **Behavior** (code).
- **Content:** Bloodlines and Devotions are defined in seed data (`BloodlineDefinition`, `DevotionDefinition`).
- **Behavior:** A stable domain engine interprets these definitions. Adding a new Bloodline from a sourcebook should require a database migration/seed update, not a code change.

---

## 🎲 1. Unified Pool Resolver

The most critical architectural task of Phase 8 is the **Unified Pool Resolver**. Devotions and Bloodline powers often combine Attributes, Skills, and Discipline ratings (e.g., `Strength + Brawl + Vigor`).

### Design
- **Pool Definition:** A declarative list of trait references.
- **Phase 8 Scope:** Additive pools only (e.g., `Strength + Brawl + Vigor`). Contested rolls ("vs") and penalty dice are deferred to Phase 9 — see mission.md.
- **Resolution Path:**
    1. `Application` layer receives a request to roll a pool (e.g., for a Devotion).
    2. `TraitResolver` (Application Service) hydrates the pool by fetching the relevant ratings from the `Character` entity.
    3. The resulting integer is passed to the `DiceService` (Domain).

### Tasks
- [ ] Define `TraitReference` record in `Domain` (Type: Attribute/Skill/Discipline, Name/Id).
- [ ] Define `PoolDefinition` in `Domain` (Collection of `TraitReference`).
- [ ] Implement `ITraitResolver` in `Application` to hydrate `PoolDefinition` from a `Character`.
- [ ] Update `DiceService` (or create a wrapper) to handle `PoolDefinition` hydration result.

---

## 🧛 2. Bloodlines (The Hidden Lineage)

Bloodlines are specialized offshoots of the five core Clans, requiring Blood Potency 2+ to join.

### 📜 Data Source
Seeded data is sourced from [`docs/bloodlines.json`](./bloodlines.json).

### 🛡️ Clan Constraint Rule
A character **MUST** belong to one of the allowed parent clans for a bloodline. 
- Most bloodlines have a single parent clan.
- Shared bloodlines (e.g., *Vilseduire*, *Icelus*) must support multiple valid parent clans.
- This constraint is enforced in the `BloodlineEngine` (Domain) and the "Apply for Bloodline" dialog (UI).

### 🩸 Mechanical Benefits
Joining a bloodline grants:
1.  **A Fourth In-Clan Discipline:** This Discipline is purchased at the in-clan XP rate.
2.  **Access to Bloodline Devotions:** Some bloodlines (e.g., *Khaibit*) unlock specialized Devotions.
3.  **Bloodline Bane:** The character gains an *additional* Bane. The bloodline bane does **not** replace the parent clan's bane — both apply simultaneously. The bloodline bane typically works in tandem with, or stacks on top of, the original clan curse.

### Data Models
- **`BloodlineDefinition`**:
    - `Name`, `Description`
    - `AllowedParentClanIds`: A collection of valid Clan IDs (supporting shared bloodlines).
    - `FourthDisciplineId`: The ID of the Discipline that becomes in-clan.
    - `PrerequisiteBloodPotency` (Default: 2)
    - `BaneOverride`: A description of the bloodline's unique Bane.
    - `CustomRuleOverride`: Boolean flag for mechanics that resist data modeling.

### Tasks
- [ ] Create `BloodlineDefinition` entity in `Data.Models`.
- [ ] Create `CharacterBloodline` join table/status entity.
- [ ] Implement `BloodlineEngine` in `Domain` to validate prerequisites.
- [ ] Add `BloodlineStatus` (Pending, Active, Rejected) for Storyteller approval flow.
- [ ] Seed core Bloodlines (e.g., *Bruja*, *En*) in `DbInitializer`.

---

## 📜 3. Devotions (The Communion of Power)

Devotions are unique powers that combine multiple Disciplines.

### 📜 Data Source
Seeded data is sourced from [`docs/devotions.json`](./devotions.json).

### 🛡️ Prerequisite Rule
A character **MUST** meet all discipline level requirements and have sufficient XP before a devotion can be learned.
- Prerequisites are enforced in the `Application` layer.
- The UI should filter devotions by their prerequisites, but the authoritative check is in the backend.

### Prerequisite Design (Long-Term Choice)
Use **fully structured** prerequisites (Option 1): parse source data into `DevotionPrerequisite` rows with `OrGroupId` for OR logic. This is best for long-term maintainability — explicit, queryable, testable, no runtime parsing of freeform strings. Aligns with Antigravity: "If it's implicit, it's a bug waiting to happen."

### Prerequisite Design (Long-Term)
Use **fully structured** prerequisites (Option 1): parse seed data into `List<(DisciplineId, MinLevel)>` with `OrGroupId` for OR logic. Bloodline-tagged prerequisites map to `RequiredBloodlineId` or `CustomRuleOverride`. This is preferred over hybrid/freeform strings because: explicit and queryable, no runtime parsing, testable, and aligns with Antigravity ("if it's implicit, it's a bug waiting to happen").

### Data Models
- **`DevotionDefinition`**:
    - `Name`, `Description`, `XpCost`.
    - `Prerequisites`: Structured list with OR support via `OrGroupId` — satisfy any group. Bloodline-tagged prerequisites (e.g., "Ascendancy") map to `RequiredBloodlineId` or `CustomRuleOverride`.
    - `RequiredBloodlineId?`: Optional — gates devotions that only characters in specific bloodlines can learn.
    - `PoolDefinition`: The dice pool used to activate the power (handled by the **Unified Pool Resolver**).
    - `IsPassive`: Boolean (passive modifiers vs. active rolls). Passive modifier engine deferred to Phase 9 — see mission.md.

### Tasks
- [ ] Create `DevotionDefinition` entity in `Data.Models`.
- [ ] Create `CharacterDevotion` join table.
- [ ] Implement Devotion prerequisite validation in `Application`.
- [ ] Integrate Devotion pools into the **Unified Pool Resolver**.
- [ ] Seed sample Devotions in `DbInitializer`.

---

## 🎭 4. Storyteller & UI Integration

- **Storyteller Glimpse**: A new "Pending Requests" tab for Bloodline applications.
- **Character Sheet** (and **Edit Character** flow):
    - Bloodlines and Devotions are first-class sections on the character sheet.
    - Players can view and **edit** Bloodline affiliation and Devotions as part of the Edit Character flow (subject to ST approval for Bloodlines and prerequisite validation for Devotions).
    - Dedicated Bloodline section showing the lineage and Bane (both clan and bloodline banes displayed).
    - Devotions list with "Roll" buttons for active Devotions (using the Unified Pool Resolver).
    - "Apply for Bloodline" dialog (filtering by Clan and Blood Potency).

---

## 🧪 5. Testing Strategy

- **Domain Tests**:
    - `BloodlineEngineTests`: Verify prerequisite logic (BP 2+, Clan match).
    - `PoolResolverTests`: Verify correct summation of Attributes, Skills, and Disciplines.
- **Application Tests**:
    - `BloodlineServiceTests`: Verify the ST approval workflow and `Result<T>` handling.
- **Integration Tests**:
    - `DbInitializerTests`: Ensure all definitions are seeded correctly.

---

## 📜 Rules Interpretation Log
All deliberate interpretations of V:tR 2e rules for this phase must be documented in `docs/rules-interpretations.md`.

> *"The blood remembers. The code must too."*
