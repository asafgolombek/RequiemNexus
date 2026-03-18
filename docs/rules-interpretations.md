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
