To reach the milestone of Phase 11: Assets & Armory, the Requiem Nexus must transition from tracking metaphysical traits to the material anchors of the Requiem. Following the Antigravity Philosophy, this system is not just a list of items but an active participant in the Dice Nexus, injecting situational modifiers directly into the resolved pools.

## 📅 Phase 11: Assets & Armory - Detailed Plan

**The Objective:** Standardize physical and service assets, automating their procurement rituals and mechanical impacts.

### 🏗️ 1. Architectural Blueprint

#### **Unified Asset Schema**
Implement a core **`Asset`** (or `AssetDefinition`) row that every carried thing shares, then **split subtype details into separate tables** (table-per-type / TPT). One fat table with dozens of nullable columns fails StyleCop-on-the-soul and invites invalid states (a row that is both rifle and flak jacket).

| Layer | Responsibility |
|--------|----------------|
| **`Asset`** | `Id`, `Slug`, display `Name`, `AssetKind` (Equipment / Weapon / Armor / Service), `Availability`, `isIllicit`, narrative `Description`, seed provenance. |
| **`EquipmentAsset`** | Maps to `generalItems.json`: `Skill` (or `SkillId`), `DiceBonus` rules, `Size`, `Durability`, `Category` (Mental/Physical/Social). Handle tiered book stats (`"1 to 3"`) as **min/max columns** or **separate seed rows** per tier—pick one and document it in `docs/rules-interpretations.md`. |
| **`WeaponAsset`** | Maps to `weapons.json`: `Damage`, `Initiative`, `StrengthRequirement`, `Size`, `Ranges`, `Clip`, `WeaponType` (Melee/Ranged), structured `Special` flags (see below). |
| **`ArmorAsset`** | Maps to `armors.json`: General/Ballistic ratings (parse `"2/4"`), `Defense`, `Speed`, `Coverage`, `Concealed`, `Era`. **While equipped**, apply armor Defense/Speed to the character as well as mitigation rules. |
| **`ServiceAsset`** | Maps to `services.json`: linked `Skill`, equipment **Bonus** (book: temporary bonus while the service applies), `Availability`. Add **`RecurringResourcesCost`** (nullable) or `BillingPeriod` for retainers; one-shot purchases leave recurring null. |

**JSON alignment:** Treat **`generalItems.json` as the canonical shape for shared equipment fields** (`Name`, `Category`, `Skill`, `DiceBonus`, `Availability`, `Size`, `Durability`, `Description`, `isIllicit`). Importers map weapons/armor/services onto the same `Asset` header + their extension table.

**Structured specials (weapons):** Prefer columns or a child table (e.g. `WeaponSpecial`: Autofire, NineAgain, ArmorPiercingRating, Stun) over unparsed strings; keep the string in seed JSON for Grimoire display until migrated.

#### **One item, multiple mechanics (e.g. Crowbar)**
Do **not** duplicate two `Asset` rows for the same narrative object. Use a single `Asset` with **multiple capabilities**, e.g. **`AssetCapability`** rows: `{ CapabilityKind: Tool, SkillId: Larceny, Bonus: +2 }` and `{ CapabilityKind: MeleeWeapon, UsesWeaponProfileId: … }` pointing at the same inventory line. The resolver picks the capability that matches the current roll (Larceny vs Weaponry attack). If the book lists different Availability for “as tool” vs “as weapon,” store the stricter value on `Asset` or model two procurement SKUs only if you intentionally want two purchases—default is **one owned item, two mechanical hooks**.

#### **Character Inventory (`CharacterAsset`)**
A join entity tracking:
- `CurrentStructure`: Tracking item health and "Broken" states.
- `IsEquipped`: Only equipped items inject modifiers into the Dice Nexus.
- `Quantity`: For consumable or stackable assets.

### ⚙️ 2. The Procurement Engine

**Logic Flow (p. 184):**
- **Automatic Acquisition**: If `Character.Resources` >= `Asset.Availability`, the item is added immediately.
- **Procurement Roll**: If `Character.Resources` < `Asset.Availability`, the system opens a `DiceNexus` modal pre-configured for a **Procurement Roll** (Manipulation + Persuasion/Streetwise). Success adds the item; failure logs a narrative setback.
- **Illicit Flags**: Items with the `IsIllicit` flag require the ST to manually approve the procurement request via the Glimpse dashboard.

### 🎲 3. Dice Nexus Integration

- **Modifier Injection**: The `Unified Pool Resolver` will scan `CharacterAsset` for equipped items matching the current `SkillId`.
- **Weapon Pools**: Adds a `Damage` bonus directly to the dice pool for `Brawl`, `Weaponry`, or `Firearms` rolls.
- **Armor Mitigation**: 
    - **General Armor**: Reduces all damage by its rating.
    - **Ballistic Armor**: Converts Lethal damage from firearms into Bashing damage before applying reduction.
- **Strength Requirements**: Automatically apply a -1 penalty to rolls if the character's Strength is lower than the asset's `StrengthRequirement`.

### 🛡️ 4. The Armory UI (The Pack)

- **Inventory Management**: A specialized tab in the Web app allowing for one-click "Equip/Unequip" actions.
- **Breaking Point Alerts**: Automated notifications when an item's Structure reaches 0, disabling its bonuses until repaired.
- **Quick-Access Slots**: Allow players to pin up to 3 "Ready" items (e.g., a Pistol, a Flashlight, and a Kevlar Vest) for immediate use on the main character sheet.

### 💎 5. Seed Data Catalog

#### **Equipment (`generalItems.json`)**
- **Mental**: Automotive Tools, Crime Scene Kit, Cracking Software, Personal Computer.
- **Physical**: Battering Ram, Camouflage Clothing, Climbing Gear, Lockpicking Kit.
- **Social**: Cash (Bribe), Disguise Kit, Fashionable Clothing.

#### **Weapons (`weapons.json`)**
- **Melee**: Brass Knuckles, Crowbar, Fire Ax, Chainsaw, Stake, Spear.
- **Ranged**: Revolvers, Pistols, SMGs, Rifles, Shotguns, Crossbows.
- **Special Properties**: Support for `Autofire`, `9-again`, `Armor Piercing`, and `Stun`.

#### **Armor (`armors.json`)**
- **Modern**: Reinforced Clothing, Kevlar Vest, Flak Jacket, Riot Gear.
- **Archaic**: Hard Leather, Chainmail, Plate.

#### **Services (`services.json`)**
Per **Vampire: The Requiem 2e** core, **professional services** are purchases that grant an **equipment bonus** to a specific **skill** when you have access to that service (not a permanent dot on the sheet). Phase 11 models that as **`ServiceAsset`** tied to a **`CharacterAsset`** (or a “retainer slot”) with **optional recurring Resources** for ongoing access. Haven/scene security bonuses can be a later extension (separate capability or merit linkage) if not present as a flat skill row in the seed table.

---
*"The blood is the life... but the steel ensures it stays within."*
