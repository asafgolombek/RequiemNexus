# Rules interpretation log (Vampire: The Requiem 2e)

Deliberate mechanics choices where the table text is ambiguous or automation requires a single rule.

## Phase 11 — Assets & Armory (equipment modifiers)

- **Persistence (TPT):** The catalog is an `Asset` root row with **table-per-type** extensions (`WeaponAsset`, `ArmorAsset`, `EquipmentAsset`, `ServiceAsset`). `AssetCapability` rows attach extra mechanical hooks to one catalog item (e.g. a tool bonus plus a reference to a separate **weapon profile** asset for melee stats). Inventory is `CharacterAsset` (quantity, equipped, ready slots, structure).
- **Crowbar:** One listed general `Asset` is seeded from `generalItems.json`; a non-catalog weapon-profile asset (`vtm2e:wp:crowbar-profile`) holds melee stats. The general row gains capabilities linking Larceny assist dice and that profile. Duplicate standalone Crowbar weapon catalog entries are not seeded.
- **Tiered dice bonuses (`"1 to 3"`, etc.) in JSON seeds:** The resolver applies the **upper bound** of the range as the equipment bonus (same as taking the best-quality item the dots allow). `Availability` for procurement uses the **upper bound** of tiered availability strings.
- **Equipment bonus cap (corebook p. 185):** Multiple items may assist one roll, but the total bonus from **catalog equipment rows** (`ModifierSourceType.Equipment` with `SkillPool`) is **capped at +5** per resolved pool. Modifiers from Coils and other sources are not counted toward that cap.
- **Weapon damage in pools:** Weapon `Damage` is added to the dice pool when the pool includes **Brawl**, **Weaponry**, or **Firearms**, matching the combat skill for that weapon (ranged → Firearms; melee → Weaponry unless the special text references Brawl).
- **Broken gear:** If `CurrentStructure` is tracked and equals **0**, the item contributes **no** equipment modifiers (skill assists, weapon damage, or strength-based penalties from that row).
- **Strength requirement (automation):** If any **equipped** weapon the character is using for mechanics requires a higher **Strength** than the character’s Strength attribute, the app applies **−1 die per missing point of Strength** to **Brawl**, **Weaponry**, and **Firearms** pools (VtR 2e p. 179).
- **Encumbrance (p. 179):** If the total **Size** of all equipped items exceeds the character's **Strength + Stamina + Size**, the character suffers a **-1 penalty to all Physical pools** (Fatigued) and **-2 to Speed**.
- **Procurement (p. 184):** 
    - **Automatic**: Availability < Resources.
    - **The Reach**: Availability == Resources. Limited to **once per Chapter** (heuristically 12h).
    - **Roll**: Availability > Resources. Requires a **Manipulation + Persuasion** roll. Success adds the item immediately.
- **Armor on the sheet:** Only **equipped**, non-broken inventory rows contribute **general** and **ballistic** armor from `ArmorAsset`. Defense and Speed on the sheet follow the same equipped-armor data (see `Character` / derived stat pipeline).
- **Ballistic / damage-type conversion:** Automated **lethal ↔ bashing** resolution and damage application against armor is **deferred** until an attack/damage pipeline exists; equipped armor still affects displayed ratings and Defense/Speed modifiers from the book.
