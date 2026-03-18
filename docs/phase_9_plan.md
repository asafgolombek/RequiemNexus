# 🩸 Phase 9: The Accord of Power (Covenants & Blood Sorcery)

## 🌌 Overview
Phase 9 codifies the mystical and political structures of the Danse Macabre. It introduces **Covenants**, **Blood Sorcery** (Crúac and Theban Sorcery), and the **Mysteries of the Dragon** (Coils and Scales).

## 🧱 Architectural Pillar: Content vs. Behavior (Reused)
The separation established in Phase 8 is maintained.
- **Covenants**, **Rites**, and **Miracles** are defined as seed data.
- The **Unified Pool Resolver** and a new **Passive Modifier Engine** handle the mechanical behavior.

### Implementation Order
Sections must be implemented in dependency order:
1. Extended Unified Pool Resolver
2. Passive Modifier Engine ← **prerequisite for Covenants, Blood Sorcery, and Coils**
3. Covenants
4. Blood Sorcery
5. Mysteries of the Dragon

---

## 🎲 1. Extended Unified Pool Resolver

The Unified Pool Resolver must be extended to handle more complex V:tR 2e roll formats.

### Design
- **Contested Rolls:** Support the `Trait1 + Trait2 vs Trait3 + Trait4` format via `ContestedAgainst: PoolDefinition?` on `PoolDefinition`.
- **Penalty Dice:** Support subtracting traits or fixed values (e.g., `Pool - Stamina`) via `PenaltyTraits: IReadOnlyList<TraitReference>?`. These are resolved and subtracted after the additive pool is summed.
- **Lower Discipline:** Support using the lower of two Disciplines in a pool via a dedicated `LowerOf: (TraitReference, TraitReference)?` field. Do **not** piggyback on the existing `MinimumLevel` field — these are semantically distinct.

> **Rules Interpretation:** Crúac roll pools (Intelligence + Occult + Crúac) are contested against the target's Resolve + Composure. Theban Sorcery contests against Composure only. These interpretations must be logged in `docs/rules-interpretations.md`.

### Tasks
- [ ] Update `PoolDefinition` (Domain) to add `ContestedAgainst: PoolDefinition?`.
- [ ] Update `PoolDefinition` (Domain) to add `PenaltyTraits: IReadOnlyList<TraitReference>?`.
- [ ] Update `PoolDefinition` (Domain) to add `LowerOf: (TraitReference Left, TraitReference Right)?`.
- [ ] Implement contested resolution logic in `ITraitResolver` and `DiceService`.
- [ ] Implement penalty dice subtraction in `DiceService` (applied after pool hydration).
- [ ] Implement "lower of" hydration logic in `TraitResolver`.

---

## 📜 2. Passive Modifier Engine

Many Devotions, Coils, and Covenant benefits provide passive bonuses (e.g., +1 to Defense, -1 to wound penalties). This engine is a prerequisite for all subsequent sections.

### Design
- **`PassiveModifier`** value object:
    - `Target: ModifierTarget` — strongly-typed enum, **not** a freeform string (Antigravity Rule #1: implicit is a bug waiting to happen).
    - `Value: int` — positive or negative delta.
    - `ModifierType: ModifierType` — `Static` (permanent), `Conditional` (scene/circumstance), or `RuleBreaking` (alters engine behavior via an explicit flag, not a numeric delta).
    - `Condition: string?` — human-readable description of when a Conditional modifier applies (e.g., "when resisting frenzy").
    - `Source: ModifierSource` — tracks what generated this modifier (SourceType enum + SourceId). Required for debuggability (Antigravity Rule #8).
- **`IModifierService`** (Application): aggregates all active `PassiveModifier` records for a character at query time. Modifiers are **never** applied permanently to a base stat — derived values are always computed on demand.
- **Integration:** `TraitResolver` and derived stat calculators (Health, Speed, Defense) query `IModifierService` before returning a value.

### Tasks
- [ ] Define `ModifierTarget` enum (Defense, Speed, Health, Brawl, WoundPenalty, etc.) in `Domain`.
- [ ] Define `ModifierType` enum (`Static`, `Conditional`, `RuleBreaking`) in `Domain`.
- [ ] Define `ModifierSource` value object (`SourceType` enum + `SourceId: int`) in `Domain`.
- [ ] Define `PassiveModifier` record in `Domain` using the above types.
- [ ] Implement `IModifierService` (Application) to aggregate all active modifiers for a character.
- [ ] Update `Character` derived stat calculations (Health, Speed, Defense) to query `IModifierService`.
- [ ] Update `TraitResolver` to inject modifiers into hydrated pools.

---

## 🧛 3. Covenants (The Political Accord)

Covenants represent the ideological factions of Kindred society.

### 🛡️ Membership & Status
Joining a Covenant is a narrative choice requiring Storyteller approval (same pending pattern as Bloodlines). Covenant Status is a Merit bought with XP (1–5 dots) — it is **not** a separate field on `Character`. Being **Unaligned** is an explicit, valid state (`CovenantId: null`).

### Data Models
- **`CovenantDefinition`**:
    - `Name`, `Description`.
    - `CovenantSpecificMerits`: Links to Merit definitions gated by this Covenant. These are surfaced as purchasable only when the character's `CovenantId` matches.
- **`Character`**:
    - `CovenantId: int?` — `null` = Unaligned. Nullable FK to `CovenantDefinition`.
    - `CovenantJoinStatus: PendingApprovalStatus?` — mirrors Bloodline pending flow. `null` when Unaligned or already approved.
    - Covenant Status level is tracked as a standard Merit purchase, not a dedicated field.

> **Rules Interpretation:** Covenant Status as a Merit (not a standalone attribute) is an intentional interpretation to reuse the existing Merit system and avoid redundant state. Log in `docs/rules-interpretations.md`.

### Tasks
- [ ] Create `CovenantDefinition` entity and seed core Covenants (Carthian, Circle of the Crone, Invictus, Lancea et Sanctum, Ordo Dracul).
- [ ] Update `Character` model to include `CovenantId: int?` (nullable FK) and `CovenantJoinStatus`.
- [ ] Implement Covenant join pending flow (Storyteller approval, same pattern as Bloodlines).
- [ ] Enforce that Covenant-gated Merits (including Status) are only purchasable by members.
- [ ] Enforce that Covenant-gated Disciplines (Theban Sorcery → Lancea only; Crúac → Circle only) are blocked at the application layer.
- [ ] Add Covenant definitions and Covenant-specific Merits to `DbInitializer`.
- [ ] Wire `CovenantDefinition` to Redis cache (24h TTL, invalidate on seed update — per `Architecture.md` caching strategy).

---

## 🩸 4. Blood Sorcery (The Mystical Rites)

Crúac (Circle of the Crone) and Theban Sorcery (Lancea et Sanctum).

### 🛡️ Learning vs. Activation
Blood Sorcery has two distinct lifecycle phases that must not be conflated:
- **Learning:** XP expenditure + Storyteller approval gate (same pending pattern as Bloodlines). Covenant prerequisite enforced here.
- **Activation:** Vitae/Willpower cost deducted at roll time. Pool resolved via Unified Pool Resolver.

### Data Models
- **`SorceryRiteDefinition`**:
    - `Name`, `Description`, `Level` (1–5).
    - `SorceryType: SorceryType` — strongly-typed enum (`Cruac`, `Theban`), **not** a string.
    - `XpCost: int`.
    - `PoolDefinition` — hydrated by Unified Pool Resolver (supports contested format; see Section 1).
    - `ActivationCost` — Vitae and/or Willpower cost at activation time.
    - `RequiredCovenantId: int` — FK to `CovenantDefinition`. Enforces Covenant gating.

> **Rules Interpretation:** Crúac activation cost and contested pool format to be logged in `docs/rules-interpretations.md`.

### Tasks
- [ ] Define `SorceryType` enum (`Cruac`, `Theban`) in `Domain`.
- [ ] Create `SorceryRiteDefinition` entity with `RequiredCovenantId` FK.
- [ ] Implement `SorceryService` — split into `LearnRite` (XP + approval gate + Covenant check) and `ActivateRite` (cost deduction + pool resolution).
- [ ] Enforce Covenant prerequisite in `LearnRite`: reject if `character.CovenantId != rite.RequiredCovenantId`.
- [ ] Implement Storyteller approval flow for Rite learning.
- [ ] Create UI for "Blood Sorcery" section on the character sheet.
- [ ] Seed core Rites (Crúac levels 1–3) and Miracles (Theban levels 1–3) in `DbInitializer`.
- [ ] Wire `SorceryRiteDefinition` to Redis cache (24h TTL — per `Architecture.md` caching strategy).

---

## 🐉 5. The Mysteries of the Dragon (Coils & Scales)

The Ordo Dracul's unique system of permanent physiological changes. Requires Ordo Dracul membership (enforced at application layer) and the Passive Modifier Engine (Section 2).

### 🛡️ Prerequisite Chain
Coils are organized into Scales (thematic groupings). A character must hold Scale N before purchasing Coil N+1 within that Scale. This prerequisite chain must be modeled explicitly — do not rely on implicit ordering.

### 🛡️ Rule-Breaking Modifiers
Coils often alter core engine behavior rather than providing numeric deltas (e.g., "Ignore the first 2 points of sunlight damage"). These use the `RuleBreaking` modifier type defined in Section 2, handled via explicit engine flags, not integer math.

### Data Models
- **`ScaleDefinition`**: `Name`, `Description`, `MaxLevel` (the number of Coils in this Scale).
- **`CoilDefinition`**:
    - `Name`, `Description`, `Level` (position within its Scale).
    - `ScaleId: int` — FK to `ScaleDefinition`.
    - `PrerequisiteCoilId: int?` — explicit prerequisite chain (Coil N requires Coil N-1).
    - `XpCost: int`.
    - `Modifiers: IReadOnlyList<PassiveModifier>` — uses the engine from Section 2.

### Tasks
- [ ] Create `ScaleDefinition` entity.
- [ ] Create `CoilDefinition` entity with `ScaleId` FK and `PrerequisiteCoilId` chain.
- [ ] Implement `CoilService` — enforce Ordo Dracul membership and prerequisite chain on purchase.
- [ ] Implement `RuleBreaking` modifier handling in the Passive Modifier Engine for Coil-specific effects.
- [ ] Create UI for "Mysteries of the Dragon" section (visible to Ordo Dracul characters only).
- [ ] Seed core Scales (Coils of the Beast, Banes, Mortality) and their Coils in `DbInitializer`.
- [ ] Wire `CoilDefinition` / `ScaleDefinition` to Redis cache (24h TTL — per `Architecture.md` caching strategy).

---

## 🧪 6. Testing Strategy

- **Domain Tests** (`RequiemNexus.Domain.Tests`):
    - `PoolResolverTests`: Verify contested roll format, penalty dice subtraction, and "lower of" hydration.
    - `ModifierEngineTests`: Verify Static, Conditional, and RuleBreaking modifier stacking and source tracking.
- **Application Tests** (`RequiemNexus.Application.Tests`):
    - `SorceryServiceTests`: Verify `LearnRite` approval gate, Covenant prerequisite rejection, and `ActivateRite` cost deduction.
    - `CoilServiceTests`: Verify Ordo Dracul gating and prerequisite chain enforcement.
    - `CovenantServiceTests`: Verify join pending flow and Covenant-gated Merit/Discipline blocking.
- **Data Tests** (`RequiemNexus.Data.Tests`):
    - `DbInitializerTests`: Ensure Covenants, Rites, Coils, and Scales are all seeded correctly against a Dockerized PostgreSQL instance.
- **E2E Tests** (Playwright):
    - Verify Blood Sorcery UI section is visible and functional.
    - Verify Mysteries of the Dragon section is hidden for non-Ordo characters.
- **Performance Tests** (`RequiemNexus.PerformanceTests`):
    - Benchmark `IModifierService` aggregation for characters with many active Coils/Devotions — must remain within the 300ms p95 API budget.

---

## 📜 Rules Interpretation Log
All deliberate interpretations of V:tR 2e rules for this phase must be documented in `docs/rules-interpretations.md`. Each section above flags specific interpretations requiring logging:
- Contested pool format for Crúac vs. Theban Sorcery (Section 1).
- Covenant Status modeled as a Merit, not a standalone field (Section 3).
- Crúac activation cost and pool composition (Section 4).

> *"The blood remembers. The code must too."*
