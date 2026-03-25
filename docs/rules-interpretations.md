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

## Phase 12 — The Web of Night (relationship webs)

- **Blood Sympathy — BP ÷ 2 minimum:** Characters with Blood Potency 0 or 1 have no active Blood Sympathy (rating 0). VtR 2e implies BP 2+ is required; we enforce strictly.
- **Blood Sympathy roll pool:** Pool = Wits + Empathy (Skill) + Blood Sympathy Rating, where the rating is a flat integer bonus injected by the Application layer *after* `TraitResolver` resolves Wits and Empathy. The resolver does not know about Blood Sympathy; the service adds it explicitly.
- **Blood Bond fading interval:** Fixed 30-day interval per stage (not a calendar month — no DST or month-length variance). VtR 2e p. 154 states a year for full recovery; we interpret this as ~1 month per stage to keep the tracker actionable at chronicle scale. Tables may use any pacing; this value is the default.
- **Blood Bond Stage 2 Condition:** `Swooned` is reused for Blood Bond Stage 2. In V:tR 2e, both social maneuvering success and Bond Stage 2 are described using similar obsession language. The existing `Swooned` Condition correctly models this.
- **Predatory Aura — Blood Potency pool bypass:** Predatory Aura contests use `Character.BloodPotency` directly as the dice count, not via `TraitResolver`. Blood Potency is a first-class Character scalar; routing through the resolver would require a special-case `TraitType.BloodPotency` that pollutes the generic contract for a single use case.
- **Predatory Aura — default outcome Shaken:** The rulebook gives the ST a choice between `Beaten Down` (Tilt) and `Shaken` (Condition). Automated resolution defaults to `Shaken`. ST can override by manually applying `BeatenDown` Tilt. Rationale: Shaken is a Condition (storable, narrative), while BeatenDown is a combat Tilt more appropriate for explicit combat encounters.
- **Ghoul aging damage:** Ghouls have no health track in this system — they are not `Character` entities. `GhoulAgingRules.OverdueMonths` returns how overdue a ghoul is; the ST records consequences in the `Notes` field or outside the app. Automated damage application is not deferred — it is out of scope for a non-character entity.
- **Ghoul Discipline access:** Ghouls can access one dot of any single in-clan Discipline of their regnant, up to the regnant's Blood Potency. We store accessible Discipline IDs at rating 1; multi-dot ghoul Disciplines are out of scope per Phase 12 non-goals. The cap is only enforced when the regnant is a linked PC; NPC/display-name regnants are ST-trusted.
- **Predatory Aura — passive first-meeting contest:** The passive aura lock (V:tR 2e p.89 — two vampires contest on first encounter each evening) is deferred. Phase 12 implements deliberate Lash Out only. The `IsLashOut` column on `PredatoryAuraContest` is reserved for the future passive path.
- **Blood Bond `Swooned` disambiguation:** `Swooned` can be applied by both Social Maneuvering (Phase 10) and Blood Bond Stage 2. The bond service writes `SourceTag = "bloodbond:{bondId}"` to its Condition rows. Resolution targets only rows matching that `SourceTag`, leaving Social Maneuvering rows unaffected.
- **Ghoul aging interval:** Fixed 30-day interval (not a calendar month), matching the bond fading interval. Rationale: avoids DST and month-length edge cases; simpler to reason about in tests and UI.

## Phase 14 — The Danse Macabre (combat & wounds)

- **MVP scope:** Automated pipeline covers **melee** attack dice resolution (Storyteller encounter), structured damage application to `Character.HealthDamage`, bashing overflow (bashing → lethal → aggravated when the track is full), **wound penalty** on **Physical** skill pools via `ModifierTarget.WoundPenalty`, and **fast bashing heal** (1 Vitae per bashing box). Ranged firearms, improvised weapons, touch attacks, dodge actions, and armor mitigation rolls remain ST-facing or deferred.
- **Damage symbols:** `HealthDamage` uses `/` (bashing), `X` (lethal), `*` (aggravated), space (empty), matching the character sheet vitals UI.
- **`DamageSource` mapping:** `Weapon`, `Lethal` → lethal on the track; `Bashing` → bashing; `Aggravated`, `Fire`, `Sunlight` → aggravated (fire/sunlight reserved for Phase 15 tilt/frenzy hooks).
- **Attack aggregation (MVP):** Total damage instances = **net attack successes** (successes minus Defense, floor 0) **plus** **weapon damage pool successes**. This is a deliberate automation shortcut; tables may use different damage steps — ST can apply manual adjustments outside this path.
- **Defense:** `AttackService` consumes a **defense total** supplied by the caller (typically a PC’s sheet `Defense` or an NPC value). Firearms vs. Defense and unaware-target rules are not automated in Phase 14.
- **Wound penalty tiers:** Penalty dice match vitals tooltips: damage in health box **max−3** → −1, **max−2** → −2, **max−1** → −3. **Incapacitated** when every box in the normalized track holds damage.
- **Vitae healing:** Only **fast bashing** heal is automated (structured cost via `VitaeHealingCosts`). Lethal/aggravated Vitae costs return a **player-safe failure** until a later slice implements full resting rules.
- **B/L/A overflow automation:** When the track is already full, the mutator repeatedly upgrades rightmost bashing → lethal → aggravated until either a space opens or the track is **all aggravated** (no further upgrades). On very small tracks an extra hit can therefore end as `***` with nowhere left to mark a new bash — ST adjudication for “damage past destruction” remains manual.
