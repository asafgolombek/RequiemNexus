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
- **`DamageSource` mapping:** `Weapon`, `Lethal` → lethal on the track; `Bashing` → bashing; `Aggravated`, `Fire`, `Sunlight` → aggravated (`Fire` / `Sunlight` tie to Rötschreck and frenzy UI in Phase 15 — see Phase 15 section below).
- **Attack aggregation (MVP):** Total damage instances = **net attack successes** (successes minus Defense, floor 0) **plus** **weapon damage pool successes**. This is a deliberate automation shortcut; tables may use different damage steps — ST can apply manual adjustments outside this path.
- **Defense:** `AttackService` consumes a **defense total** supplied by the caller (typically a PC’s sheet `Defense` or an NPC value). Firearms vs. Defense and unaware-target rules are not automated in Phase 14.
- **Wound penalty tiers:** Penalty dice match vitals tooltips: damage in health box **max−3** → −1, **max−2** → −2, **max−1** → −3. **Incapacitated** when every box in the normalized track holds damage.
- **Vitae healing:** Only **fast bashing** heal is automated (structured cost via `VitaeHealingCosts`). Lethal/aggravated Vitae costs return a **player-safe failure** until a later slice implements full resting rules.
- **B/L/A overflow automation:** When the track is already full, the mutator repeatedly upgrades rightmost bashing → lethal → aggravated until either a space opens or the track is **all aggravated** (no further upgrades). On very small tracks an extra hit can therefore end as `***` with nowhere left to mark a new bash — ST adjudication for “damage past destruction” remains manual.

## Phase 15 — The Beast Within (frenzy & torpor)

Mechanical choices for automated frenzy saves, Vitae-zero handling, torpor intervals, and tilt exclusivity. Full tasking lives in [`PHASE_15_THE_BEAST_WITHIN.md`](./PHASE_15_THE_BEAST_WITHIN.md).

- **Frenzy pool:** `Resolve + Blood Potency` for all frenzy save types including Rötschreck (VtR 2e p. 99). No separate pool is specified in the text.
- **Rötschreck vs. Frenzy tilt:** `Rotschreck` trigger maps to `TiltType.Rotschreck`; all other frenzy triggers map to `TiltType.Frenzy`. Both are **Beast** tilts; **only one Beast tilt** may be active at a time (application guard plus unique index on active same-type tilts).
- **Hunger trigger — manual ST:** The Storyteller may manually trigger a `Hunger` save for narrative edge cases (e.g. off-screen Vitae loss). This is a direct `FrenzyService` call and does **not** re-fire `VitaeDepletedEvent`. `Starvation` is never a manual UI trigger (torpor interval / Advance Time only).
- **Vitae-zero when Rotschreck active:** If the character already has any active Beast tilt (including `Rotschreck`) when Vitae reaches 0, the automatic `Hunger` save from `VitaeDepletedEvent` is **suppressed** — the character is already in a Beast state.
- **Willpower die subtraction:** Spending Willpower to resist frenzy removes **1 die** from the pool (VtR 2e p. 92 general Willpower rule). If the pool reaches 0 after subtraction, use a **chance die** (1 die). This is not a bonus to successes.
- **Torpor awakening cost:** “One Vitae, or an anchor moment” (p. 165). Automation: `narrativeAwakening = false` deducts 1 Vitae; `narrativeAwakening = true` is ST-confirmed narrative awakening with no Vitae cost.
- **Torpor duration table:** VtR 2e p. 165 table encoded as days; month = 30 days, year = 365 days. Blood Potency 10 (“indefinitely”) is represented as `int.MaxValue` in code — **no automatic starvation notification** for that tier.
- **Hunger escalation in torpor:** Book: Hunger increases by 1 at each torpor interval milestone. The app fires Storyteller notifications on interval; the **Hunger track update** at those milestones remains a **manual ST action** (Phase 16a delivers **feeding rolls** and Vitae gain via `HuntingService`, not automatic Hunger tier advancement from torpor intervals).
- **Starvation notification deduplication:** One notification per elapsed interval per character, tracked with `LastStarvationNotifiedAt` so the background ticker does not spam repeat notifications.
- **Dice feed without active session:** If `PublishDiceRollAsync` fails (no live session, no `CampaignId`, Redis error), the roll outcome and tilt application still succeed; failure is logged and the ST sees the tilt on next Glimpse load.

## Phase 16a — The Hunting Ground (feeding)

Mechanical choices for predator-type pools, territory bonus dice, resonance display, and hunt ledger. Full tasking lives in [`PHASE_16A_THE_HUNTING_GROUND.md`](./PHASE_16A_THE_HUNTING_GROUND.md).

- **Hunting pool per Predator Type:** V:tR 2e lists primary pools per type (p. 104–107). Where the book offers a choice of two pools (e.g. Brawl or Weaponry for Alleycat), we select the first listed option for automation. Tables can use the alternate by seeding a custom `HuntingPoolDefinition` row or via ST override.
- **Resonance thresholds:** The corebook describes resonance as a narrative quality tied to Blood Potency and circumstances, not a pure success-count table. For Phase 16a display automation we map success count to the four intensity labels (Fleeting / Weak / Functional / Saturated), with thresholds 1–2 / 3–4 / 5–6 / 7+ respectively. These thresholds are interpretive; the ST may override the displayed resonance outside the app. Mechanical resonance effects (Diablerie, Sorcery, Disciplines) are out of scope until a later phase.
- **Territory bonus formula:** `FeedingTerritory.Rating` (1–5) is added as flat bonus dice to the resolved pool. V:tR 2e does not specify a territory-bonus formula; this is a table convenience to reward holding high-quality hunting grounds. No cap applied — pool floor is 1 die regardless. Territory must belong to the same campaign as the character; cross-campaign territory IDs return `Result.Failure`.
- **Pool floor enforcement:** After `ResolvePoolAsync` + territory bonus, if the total pool is less than 1, it is clamped to 1 before rolling. A resolver returning 0 (e.g. missing traits) is a data setup issue — the character still rolls one die rather than receiving a hard failure. This preserves audit log completeness.
- **Zero-success hunts:** 0 successes = 0 Vitae gained. The hunt is not a botch unless a dramatic failure rule applies (the app does not automate dramatic failures in Phase 16a). A `HuntingRecord` is still written to preserve the ledger.
- **PredatorType null guard:** Characters without a PredatorType cannot initiate a hunt through the UI. The service also returns `Result.Failure` on null PredatorType. The ST must assign one (character creation or sheet edit — future task).
- **Vitae cap at MaxVitae:** `IVitaeService.GainVitaeAsync` already caps at `character.MaxVitae`. Overflow is silently discarded (same as existing behavior). The hunt result UI shows actual Vitae gained (delta on the character).
