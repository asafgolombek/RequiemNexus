# 📜 Phase 9.5 & 9.6: The Sacrifice and the Forbidden Arts — Implementation Plan

## 🌌 Overview

This document codifies the architectural and implementation strategy for extending the Blood Sorcery module: **Ritual Sacrifice** (Phase 9.5) and **Necromancy** / **Ordo Dracul Rituals** (Phase 9.6).

**Shipped implementation (Grimoire alignment):**

- Domain: `SacrificeType`, `RiteRequirement`, `RiteActivationAcknowledgment`, `RiteActivationResourceSnapshot`; `RiteRequirementValidator` (parse, validate resources/acks, aggregate costs).
- Data: `SorceryRiteDefinition.RequirementsJson`; nullable `RequiredCovenantId`; `RequiredClanId`; `Character.HumanityStains`; `CovenantDefinition.SupportsOrdoRituals`; migration `Phase95Phase96BloodSorceryExtensions`.
- Application: **`SorceryService.BeginRiteActivationAsync`** (Masquerade, conditional `ExecuteUpdateAsync` for Vitae/Willpower/stains, then `TraitResolver` pool); `BeginRiteActivationRequest` DTO; generalized rite eligibility/learn for all `SorceryType` values.
- Web: Character sheet Blood Sorcery uses **`BeginRiteActivationAsync`**; rite learning requests live on **Advancement** (`ApplyLearnRiteModal`); narrative `confirm` when external sacrifice acks are required; section visibility for Crúac/Theban, Ordo rituals, or Necromancy dots; learn-rite modal tradition labels.

## 🧬 Architectural Principles (The Grimoire)

1. **Sacrifice is Temporal, The Blood is Eternal:** Sacrifices are validated and consumed at the *outset* of a ritual. Success or failure does not return the sacrifice.
2. **Content is Data, Behavior is Code:** Ritual requirements live in seed data (`RequirementsJson` on `SorceryRiteDefinition`). `SorceryService` interprets them.
3. **Explicit Sovereignty:** Narrative costs use acknowledgment flags on `BeginRiteActivationRequest`; no silent inventory deduction.
4. **The Masquerade Audit:** Owner verification before activation (`AuthorizationHelper`).

---

## 🛠️ Phase 9.5: Sacrifice Mechanics (Blood Sorcery)

### 1. Data Model (`RequiemNexus.Domain` + Data)

- **`SacrificeType`:** InternalVitae, SpilledVitae, Willpower, PhysicalSacrament, Heart, MaterialOffering, HumanityStain, MaterialFocus.
- **`RiteRequirement`:** Type, Value, IsConsumed, optional DisplayHint — serialized as JSON array in `RequirementsJson`.
- **`SorceryRiteDefinition`:** `RequirementsJson` column (not a separate table).

### 2. Application Logic

- **`RiteRequirementValidator`:** Parse JSON, validate acknowledgments, validate resources, aggregate internal costs.
- **`BeginRiteActivationAsync`:** Single entry point for paid activation + dice pool return (replaces the older plan name `ActivateRiteAsync`). Humanity stains update **`Character.HumanityStains`** (no separate `HumanityService`). Structured **Serilog** on cost application.

### 3. UI/UX

- Activation cost string beside each rite; browser **confirm** when external sacrifice types require acknowledgment; errors surfaced via toast.

---

## 🛠️ Phase 9.6: Additional Traditions (Necromancy & Ordo Dracul)

- **`SorceryType`:** `Necromancy`, `OrdoDraculRitual` in addition to Crúac/Theban.
- **Gating:** Necromancy — Mekhet + `RequiredClanId`; Ordo — `SupportsOrdoRituals` + `RequiredCovenantId` to Ordo.
- **Seed catalog:** Sample rites in `DbInitializer.EnsureBloodSorceryPhaseExtensionsAsync` (`Corrupting the Corpse`, `Dragon's Own Fire`). Expand via same JSON/initializer pattern—not the typo’d filename `sacrifaice.txt`.
- **Temporary Coil/Scale from rituals:** **Deferred** — documented under `rules-interpretations.md` (“Temporary Coils/Scales granted by rituals”).

---

## 📅 Implementation Roadmap (retrospective)

| Step | Status |
|------|--------|
| Domain: `SacrificeType`, `RiteRequirement`, validator + tests | Done |
| Migration + `RequirementsJson` + seeds | Done |
| `BeginRiteActivationAsync` + Application tests (SQLite in-memory for `ExecuteUpdateAsync`) | Done |
| UI: sheet + modal + paid activation path | Done |
| `mission.md` / `rules-interpretations.md` | Done |

---

> "The blood is the price. The ritual is the path. The Grimoire is the record."
