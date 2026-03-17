# Requiem Nexus — Rules Interpretation Log

This document records deliberate interpretations of *Vampire: The Requiem (Chronicles of Darkness) Second Edition* rules where the source material is ambiguous or silent. Every entry must cite the affected subsystem and the decision made.

---

## Phase 8: The Hidden Blood (Bloodlines & Devotions)

### Bane Stacking (Bloodlines)

**Decision:** When a character joins a bloodline, they gain an *additional* bane. The bloodline bane does **not** replace the parent clan's bane — both apply simultaneously. The bloodline bane typically works in tandem with, or stacks on top of, the original clan curse.

**Subsystem:** `BloodlineDefinition.BaneOverride`, character sheet Bane display

**Source:** V:tR 2e core rulebook; clarified per Requiem Nexus design

---

### Pool Resolver Scope — Contested and Penalty Dice (Deferred)

**Decision:** Phase 8 supports **additive pools only** (e.g., `Strength + Brawl + Vigor`). Contested rolls (`vs Resolve + Tolerance`) and penalty dice (`Pool - Stamina`) are deferred to Phase 9.

**Deferred formats documented here for future implementation:**
- Contested: `Presence + Empathy + Majesty vs Resolve + Tolerance`
- Penalty: `Dexterity + Athletics + Celerity - Stamina`
- Special: `Presence + Empathy + lower Discipline`

**Subsystem:** `TraitResolver`, `DevotionDefinition.PoolDefinitionJson`

---

### Bloodline-Gated Devotions

**Decision:** Some devotions are only learnable by characters in specific bloodlines (e.g., Khaibit Obtenebration Devotions). These are modeled via `DevotionDefinition.RequiredBloodlineId`. Prerequisite validation in the Application layer checks both Discipline levels and bloodline membership when `RequiredBloodlineId` is set.

**Subsystem:** `DevotionDefinition`, `DevotionPrerequisiteValidator`

---
