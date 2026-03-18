# Requiem Nexus — Rules Interpretation Log

This document records deliberate interpretations of *Vampire: The Requiem (Chronicles of Darkness) Second Edition* rules where the source material is ambiguous or silent. Every entry must cite the affected subsystem and the decision made.

---

## Phase 8: The Hidden Blood (Bloodlines & Devotions)

### Bane Stacking (Bloodlines)

**Decision:** When a character joins a bloodline, they gain an *additional* bane. The bloodline bane does **not** replace the parent clan's bane — both apply simultaneously. The bloodline bane typically works in tandem with, or stacks on top of, the original clan curse.

**Subsystem:** `BloodlineDefinition.BaneOverride`, character sheet Bane display

**Source:** V:tR 2e core rulebook; clarified per Requiem Nexus design

---

### Pool Resolver Scope — Contested and Penalty Dice (Phase 9)

**Decision:** Phase 9 extends the pool resolver to support:
- **Contested:** `PoolDefinition.ContestedAgainst` — when set, the UI displays what the target should roll (e.g., "Target rolls: Resolve + Composure"). The target rolls manually; no in-app target selection.
- **Penalty dice:** `PoolDefinition.PenaltyTraits` — traits subtracted after the additive pool is summed (e.g., `Dexterity + Athletics + Celerity - Stamina`).
- **Lower of:** `PoolDefinition.LowerOf` — contributes min(left, right) to the pool (e.g., lower of Majesty and Dominate).

Crúac roll pools contest against Resolve + Composure. Theban Sorcery contests against Composure only.

**Subsystem:** `TraitResolver`, `PoolDefinition`, `DevotionDefinition.PoolDefinitionJson`

---

### Pool Resolver Scope — Historical (Phase 8 Deferred)

Phase 8 supported additive pools only. The above formats were deferred and are now implemented in Phase 9.

---

### Bloodline-Gated Devotions

**Decision:** Some devotions are only learnable by characters in specific bloodlines (e.g., Khaibit Obtenebration Devotions). These are modeled via `DevotionDefinition.RequiredBloodlineId`. Prerequisite validation in the Application layer checks both Discipline levels and bloodline membership when `RequiredBloodlineId` is set.

**Subsystem:** `DevotionDefinition`, `DevotionPrerequisiteValidator`

---

---

## Phase 9: The Accord of Power (Covenants & Blood Sorcery)

### Covenant Status as a Merit

**Decision:** Covenant Status (1–5 dots) is modeled as a standard Merit purchase gated to covenant members, not as a dedicated `CovenantStatus` field on `Character`. This reuses the existing Merit system and avoids redundant state.

**Subsystem:** `CovenantDefinitionMerit`, `CharacterMeritService.GetAvailableMeritsAsync`, `CharacterMeritService.AddMeritAsync`

**Source:** V:tR 2e core rulebook; Requiem Nexus design decision to minimize model bloat.

---

### Crúac Activation Pool and Contested Format

**Decision:** Crúac activation pool is Intelligence + Occult + Crúac dots. Crúac rolls contest against the target's Resolve + Composure. Theban Sorcery uses the same pool formula (Intelligence + Occult + Theban Sorcery) but contests against Composure only.

The `ContestedAgainst` field on `PoolDefinition` is display-only: the UI shows "Target rolls: Resolve + Composure" (Crúac) or "Target rolls: Composure" (Theban). The target rolls manually; no in-app target selection.

**Subsystem:** `SorceryRiteDefinition.PoolDefinitionJson`, `TraitResolver`, `PoolDefinition.ContestedAgainst`

**Source:** V:tR 2e, Blood Sorcery chapter. Requiem Nexus interpretation: contested portion is informational only, not mechanically resolved in-app.

---

### Coil XP Costs, Chosen Mystery Cap, and Crucible Ritual

**Decision:**
- Chosen Mystery Coil: **3 XP per tier** (2 XP with Crucible Ritual).
- Non-chosen Mystery Coil: **4 XP per tier** (3 XP with Crucible Ritual).
- Dots in any non-chosen Coil cannot exceed the character's Ordo Dracul Status Merit dots.
- Crucible Ritual access is a **character-level flag** (`Character.HasCrucibleRitualAccess`) granted/revoked by Storyteller approval. Once granted, it persists and applies to all future Coil purchases.

**Subsystem:** `CoilService.CalculateXpCost`, `CoilService.ApproveCoilLearnAsync`, `CoilService.GetOrdoStatusDots`

**Source:** V:tR 2e, Ordo Dracul chapter / coils_rules.txt.

---

### Chosen Mystery Selection Flow

**Decision:** A player requests a Chosen Mystery (stored as `Character.PendingChosenMysteryScaleId`), which then awaits Storyteller approval. On approval, `ChosenMysteryScaleId` is set and the pending field is cleared. A character may join Ordo Dracul before selecting a Mystery.

**Subsystem:** `CoilService.RequestChosenMysteryAsync`, `CoilService.ApproveChosenMysteryAsync`

**Source:** V:tR 2e rules for the Ordo Dracul; Requiem Nexus mirrors the same approval pattern used for Covenant join and Bloodline join.
