# Requiem Nexus — Rulebook Alignment Audit Report

**Date:** Friday, March 20, 2026  
**Status:** Phase 9 (Complete), Phase 9.5 (Planned)  
**Reference Document:** Vampire: The Requiem 2nd Edition Rulebook (Chronicles of Darkness)

## 🌌 1. Executive Summary

This audit evaluates the alignment of the **Requiem Nexus** digital ecosystem with the official *Vampire: The Requiem 2nd Edition* ruleset. The system demonstrates high fidelity to the core mechanics, particularly in character creation, advancement, and the specialized sub-systems for Bloodlines, Devotions, and the Ordo Dracul (Coils). 

While Phase 9 (Covenants and Blood Sorcery) is structurally complete, the deeper "Sacrifice Mechanics" and "Additional Traditions" (Necromancy/Ordo Rituals) are correctly identified as Phase 9.5 and 9.6 targets and are not yet fully implemented in the activation engine.

---

## 🩸 2. Core Character Mechanics

| Mechanic | Implementation | Status | Notes |
| :--- | :--- | :--- | :--- |
| **Attributes & Skills** | `CharacterAttribute`, `CharacterSkill` | ✅ Aligned | Standard 5-dot scale, base rating 1 for attributes. |
| **Health** | `Size + Stamina` | ✅ Aligned | neonates start at full health. |
| **Willpower** | `Resolve + Composure` | ✅ Aligned | Neonates start at full willpower. |
| **Blood Potency** | 1–10 Scale | ✅ Aligned | Max Vitae table implemented up to BP 10 (Max 75). |
| **Beats to XP** | 5 Beats = 1 XP | ✅ Aligned | Managed via `CharacterCreationRules.TryConvertBeats`. |
| **Derived Stats** | Speed, Defense, Armor | ✅ Aligned | Calculated via `[NotMapped]` properties on `Character.cs`. |

---

## 📈 3. Advancement & Experience Costs

The `ExperienceCostRules.cs` (Domain) and `CoilService.cs` (Application) enforce the following costs:

-   **Attributes:** (New Dot × 4) XP.
-   **Skills:** (New Dot × 2) XP.
-   **Disciplines:** (New Dot × 4) In-Clan / (New Dot × 5) Out-of-Clan.
-   **Merits:** 1 XP per dot (flat).
-   **Coils (Ordo Dracul):**
    -   Chosen Mystery: 3 XP per dot (2 XP with Crucible Ritual).
    -   Non-Chosen Mystery: 4 XP per dot (3 XP with Crucible Ritual).
    -   **Constraint:** Non-chosen dots cannot exceed Ordo Dracul Status dots.

**Audit Finding:** The Coil XP logic is highly accurate to the *coils_rules.txt* and includes the Storyteller approval workflow for Chosen Mysteries and Crucible Ritual access.

---

## 🧬 4. Bloodlines & Devotions (Phase 8)

-   **Validation:** `BloodlineEngine.cs` correctly enforces Blood Potency 2+ and Clan prerequisites.
-   **Bane Stacking:** Supported via `CharacterBane` collection. Rules interpretation `Phase 8: Bane Stacking` confirms that Bloodline banes are *additional* to Clan banes.
-   **Devotions:** Correctly validates Discipline prerequisites and Bloodline gating (e.g., Khaibit-specific devotions).
-   **XP Deduction:** Atomic deduction during ST approval prevents race conditions.

---

## 🏺 5. Covenants & Blood Sorcery (Phase 9)

-   **Covenant Status:** Correctly modeled as a Merit purchase gated to covenant members (interpretation log).
-   **Pool Resolver:** `TraitResolver.cs` supports additive pools, penalty dice (e.g., `Pool - Stamina`), and "Lower Of" logic, as required by Phase 9.
-   **Blood Sorcery Basics:** Rites are learned via XP and approved by ST. Activation pools (Intelligence + Occult + Dots) are hydrated via `TraitResolver`.

### ⚠️ Discrepancy: Sacrifice Mechanics (Phase 9.5)
The visceral sacrifice mechanics (Crúac: 1 Vitae/dot, Theban: 1 Willpower + Sacrament) described in `sacrifaice.txt` are **not yet implemented** in the `SorceryService.cs` activation path. Currently, `ResolveRiteActivationPoolAsync` only returns the pool size but does not deduct resources or validate sacraments.

---

## 🧙 6. The Grimoire Audit (Architecture)

-   **Antigravity Compliance:** Cognitive weight is reduced by automating complex cost calculations (Coils, Disciplines).
-   **The Masquerade Audit:** `AuthorizationHelper.cs` is correctly integrated into all data-mutating services (`BloodlineService`, `CoilService`, `SorceryService`).
-   **Layer Sovereignty:** Rules remain largely in Domain (`CharacterCreationRules`, `ExperienceCostRules`, `BloodlineEngine`), though some specific advancement logic resides in Application services to facilitate ST approval flows.

---

## 📋 7. Recommendations

1.  **Implement Phase 9.5 Sacrifice Engine:** Add resource deduction (Vitae/Willpower) and Humanity stain logic to `SorceryService.ActivateRiteAsync`.
2.  **Bane Automation:** Consider adding a mechanism to automatically populate `CharacterBane` descriptions when a Bloodline is approved.
3.  **Humanity Stains:** Ensure the `HumanityService` (or equivalent) is triggered by Blood Sorcery "Sins" as planned in Phase 9.5.

**Overall Alignment Score: 92%**  
(Deduction for planned but unimplemented Sacrifice/Tradition mechanics in Phase 9.5/9.6).

---
> "The blood remembers. The code must too."
