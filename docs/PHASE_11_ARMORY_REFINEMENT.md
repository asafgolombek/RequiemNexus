# 🩸 Phase 11 Refinement: The Forge & The Pack

This document outlines the detailed refinement plan for the Assets & Armory system, transitioning from basic item tracking to a high-fidelity mechanical participant in the Requiem Nexus.

## 🏗️ 1. Extended Architectural Schema

To support specialized gear and narrative modifications without catalog pollution, we introduce the **Modifier** system.

### **New Entities**

| Entity | Purpose | Key Fields |
|--------|---------|------------|
| **`AssetModifier`** | Global catalog of "upgrades" (e.g., Scope, Silver-plating). | `Id`, `Name`, `Description`, `Availability`, `ModifierEffectJson` (PassiveModifier shape). |
| **`CharacterAssetModifier`** | Join table linking a specific owned item to a modifier. | `CharacterAssetId`, `AssetModifierId`, `CustomName` (e.g., "Sire's Silver Dagger"). |

### **Schema Updates (Data Layer)**

- **`WeaponAsset`**: Add `DamageType` enum (`Bashing`, `Lethal`, `Aggravated`) to automate Health tracker subtraction.
- **`WeaponAsset`**: Add `ConcealmentRating` (derived from `Size` p. 178) for automated Masquerade checks.
- **`CharacterAsset`**: Add `IsCustom` flag. If true, the item name can be overridden by the player (for narrative items).
- **`CharacterAsset`**: Add `LastProcurementDate`. Used to enforce the "Once per Chapter" frequency limit for Resource-level purchases.

---

## 🎲 2. Dice Nexus Logic Refinements

### **Cumulative Strength Penalty (p. 179)**
The current implementation assumes a flat -1. The `TraitResolver` must be updated:
- **Rule**: `Penalty = Math.Min(0, Character.Strength - Asset.StrengthRequirement)`.
- **Injection**: This penalty applies to the specific pool using the asset (Brawl/Weaponry/Firearms).

### **Armor Piercing (AP) Resolution**
- **Logic**: AP reduces the **Armor rating** of the target for that specific roll. It does **not** affect Defense.
- **Resolver Logic**: `EffectiveArmor = Math.Max(0, TargetArmor - AttackerAP)`.

### **Encumbrance (p. 179)**
- **Threshold**: `Strength + Stamina + Size`.
- **Effect**: If total `Size` of equipped items exceeds threshold, inject the **Fatigued** Condition (-1 to all Physical pools) and -2 to **Speed**.

---

## ⚙️ 3. The Procurement Engine 2.0

### **The "Reach" & Frequency Limits**
- **Automatic**: `Availability < Resources` (Unlimited).
- **The Reach**: `Availability == Resources`. Limited to **once per Chapter** (tracked via `LastProcurementDate`).
- **Procurement Roll**: `Availability > Resources`.

### **Social Maneuvering Integration**
If a Procurement Roll fails (especially for **Illicit** items):
- The system offers a "Seek a Connection" button.
- This initiates a **Phase 10 Social Maneuver** with a Black Market NPC.
- Success grants a "Temporary Availability Bonus" for that specific item.

---

## 🛡️ 4. UI: The Pack & The Forge

### **The Forge (Modification UI)**
A sub-view within the Pack allowing players to apply `AssetModifiers` to their `CharacterAssets`.
- **Cost**: Applying a modifier triggers a `Resources` check or a `Crafts` roll.

### **Repair Flow**
When `CurrentStructure` reaches 0 (Broken):
- The item is highlighted in crimson.
- A "Repair" button appears, prompting a **Wits + Crafts** roll.
- Success restores `1 + Successes` Structure.

---

## 💎 5. Rules Interpretation Updates

The following must be added to `docs/rules-interpretations.md`:

1. **Materiality**: Silver-plated weapons deal Lethal to vampires but Aggravated to Werewolves (manual toggle for now).
2. **Concealment Mapping**:
    - Size 1: Pocket (Automatic concealment).
    - Size 2: Small (Requires Jacket/Bag).
    - Size 3+: Large (Visible/Illicit in public).
3. **Ballistic Conversion**: Until a full attack pipeline exists, players must manually toggle "Ballistic Applied" on the Health tracker to convert incoming Lethal to Bashing.

---
*"The steel is only as sharp as the mind that whets it."*
