# Playability gap plan — review

This document records how earlier review questions landed in [PLAYABILITY_GAP_PLAN.md](./PLAYABILITY_GAP_PLAN.md) (disposition) and what still needs attention during implementation. It does not replace the plan.

Style note: [AGENTS.md](../AGENTS.md) discourages decorative emoji in some contexts; this review uses plain headings. The source plan uses emoji for severity markers.

---

## Open questions — answers

**Cross-phase / sequencing**

- **Phase 16a/16b vs Phase 19:** _Resolved._ Feeding/hunting is **Phase 16a** (independent). Discipline activation is **Phase 16b**, blocked on Phase 19's `PoolDefinitionJson` migration + importer. Phase 19's data-model slice must merge before 16b can start; Phase 19's content pass (populating pool JSON) can follow in parallel with 16b work.

- **Phase 17 vs Phase 19 (degeneration event):** _Resolved._ A shared `DegenerationCheckRequired(CharacterId, DegenerationReason)` domain event is defined once in the Domain layer (see the shared-event block before Phase 17 in the plan). Phase 19 raises it with `Reason = CrúacPurchase` before Phase 17's roll UI exists — Glimpse notification only, purchase not blocked. The full degeneration roll UI ships with Phase 17. No duplicate event types; no circular dependency.

- **Vitae-zero frenzy (Phase 15):** _Resolved._ `VitaeDepletedEvent` is in-process only (no cross-service guarantee on restart — acceptable for the same app process). The plan requires tilt application inside a single `SaveChangesAsync` transaction with a guard plus **EF optimistic concurrency** (row version or a unique index on `CharacterId + TiltType + IsActive`) so concurrent `VitaeDepletedEvent` handlers cannot insert duplicate active tilts.

**Phase 14 — combat**

- **Scope of first slice:** _Resolved._ MVP boundary is explicit in the plan: melee + generic weapon profile only. Ranged/firearms, improvised weapons, and touch attacks are documented as follow-on slices in `rules-interpretations.md` when Phase 14 ships.

- **Defense:** _Resolved._ `AttackService` reads `character.Defense` from the existing derived stat (already calculated including armor modifiers from Phase 11). Defense vs. firearms and unaware-target rules are recorded in `rules-interpretations.md`. Dodge as a declared action is out of scope for Phase 14.

- **Aggravated sources:** _Resolved._ `DamageSource` enum is added to `AttackResult` from day one: Bashing, Lethal, Aggravated, Fire, Sunlight, Weapon. Phase 15's `FrenzyService` reuses `Fire` and `Sunlight` tags for Rötschreck triggers. No retrofitting needed.

**Phase 15 — frenzy / torpor**

- **Trigger catalog:** _Resolved._ `FrenzyTrigger` values: `Hunger` (Vitae 0 in active play via `VitaeDepletedEvent`), `Rage`, `Rotschreck`, and `Starvation` (torpor interval / Advance Time only). Architectural decision and enum task line in the plan distinguish `Hunger` from `Starvation` explicitly.

- **Rötschreck vs fire/sunlight UI:** _Resolved._ Rötschreck uses the same `Resolve + Blood Potency` pool as other frenzy types (no separate pool — the book does not specify one; documented in rules log). Player-visible "I am exposed" button triggers `Rotschreck` save on the player's own character. ST Glimpse trigger covers NPCs and any PC the ST needs to force-trigger. The separation means only the player can self-declare exposure; the ST can override for dramatic moments.

- **Torpor “CronJob”:** _Resolved._ Architectural decision plus tasks specify `TorporIntervalService : BackgroundService` in `Web/BackgroundServices/`, following `SessionTerminationService`; configurable timer (default 24 h); each tick notifies the Storyteller only (no direct hunger-state DB write in the worker). ST **Advance Time** calls the same `TorporService` interval logic on demand. A legacy duplicate task that still mentioned `IBackgroundJobService` was removed from the plan so only this pattern remains.

**Phase 16a — hunting**

- **Territory:** _Resolved._ `territoryId` is optional. When provided, territory quality (1–5) adds a flat die bonus to the pool. Ownership is not validated — the ST controls narrative territory access. When null, the hunt proceeds without the bonus. This is the minimal rule-correct interpretation; elaboration to required territory per predator type is a future content pass.

- **Typo in plan:** _Fixed._ `RessonanceOutcome` corrected to `ResonanceOutcome` throughout the plan.

**Phase 17 — humanity / conditions**

- **Condition coverage:** _Resolved._ `PenaltyModifierJson` is added to the canonical seeded condition types only. Custom / homebrew condition types have `PenaltyModifierJson = null` and contribute no automatic modifier — the ST applies any custom penalty by hand. This is the correct fallback and requires no additional schema.

- **Incapacitated:** _Resolved._ Incapacitated suppression is UI-only on the player-facing character sheet. The Application layer still accepts roll requests from the Storyteller Glimpse view without any server-side enforcement bypass — the ST can always roll on an incapacitated character's behalf (e.g., coup de grâce, test of death condition). This is explicitly documented in the Phase 17 task.

**Phase 18 — edge systems**

- **Passive aura "scene context":** _Resolved._ "Same scene" = (a) added to the same `CombatEncounter` (automatic contest triggered), or (b) ST manually fires the contest from the Glimpse NPC panel. No ambient session or location entity is introduced — that scope would require a new data model. The ST-toggle path covers all non-combat first-meetings. Decision recorded in `rules-interpretations.md`.

- **Social maneuvering:** _Acknowledged._ The plan has the interceptor contest the initiator using Manipulation + Persuasion; tie-breaking and other edge cases are left to the Phase 18 rules-log task (`rules-interpretations.md` at ship), not spelled out in the architectural section.

**Phase 19 — disciplines**

- **Soft gates and audit:** _Resolved._ For every purchase where `AcquisitionAcknowledgedByST = true`, append the canonical suffix to `XpLedgerEntry.Notes`: space + pipe + space + `gate-override stUserId={userId} {timestamp:O}` (see Phase 19 architectural decisions for a full example). Same string appears in the Phase 19 task list; record the format in `rules-interpretations.md` when implementing.

- **Covenant exception:** _Resolved._ Covenant Status is a **hard gate overridable by the ST** for covenant Disciplines only. When `AcquisitionAcknowledgedByST = true`, the Status check is bypassed and the override is audited in the ledger. Bloodline restrictions and Theban Humanity floor remain always-hard (no ST override). The distinction is documented in `rules-interpretations.md` as the "stolen secrets" rule interpretation.

- **Necromancy "cultural connection":** _Resolved._ Option (b) is a soft gate. `Discipline.IsNecromancy` (bool) flags the special path. If the character is neither Mekhet-clan nor a Necromancy bloodline member, `AcquisitionAcknowledgedByST = true` is required. The ST confirmation modal quotes all three eligible conditions verbatim from `DisciplinesRules.txt`. No additional character flag is needed — the condition is either met by clan/bloodline (hard-checkable) or acknowledged by the ST (soft).

**Non-goals**

- **Chases, mass combat, merged pools:** _Resolved._ All three are now explicit non-goals in the plan: chases and mass combat are out of scope; merged-pool coordinated actions remain manual ST work via the existing dice modal.

---

## Suggestions — disposition

1. **Fix dependency narrative:** _Done._ Feeding vs discipline activation split into Phase 16a / 16b; dependency graph updated with three parallel tracks; Phase 16b explicitly depends on Phase 19 schema.

2. **Align with existing modifier infrastructure:** _Done._ Phase 14 now specifies `WoundPenaltyResolver` returns a `PassiveModifier(Target = ModifierTarget.WoundPenalty)` injected into `ModifierService.GetModifiersForCharacterAsync` — the same aggregation loop as Coils and equipment. No separate resolver call; no `TraitResolver` change.

3. **Verify seed path:** _Acknowledged in plan._ Phase 19 task list includes: importer idempotency (upsert by name), integration test coverage before `DisciplineSeedData.cs` is deleted, and rollback story (if `Disciplines.json` fails to parse, `DbInitializer` throws and startup fails fast — same behavior as all other JSON importers).

4. **Single event contract for degeneration:** _Done._ `DegenerationCheckRequired(CharacterId, DegenerationReason)` is defined in a dedicated shared-event block between Phases 16b and 17. Both Phase 17 and Phase 19 reference it. `DegenerationReason` covers `StainsThreshold` and `CrúacPurchase`; future reasons (e.g., diablerie) extend the enum.

5. **Combat MVP boundary:** _Done._ Explicit "Phase 14 MVP boundary" note added to the Phase 14 architectural decisions: first shippable slice = melee attack + health overflow + wound penalty + armor mitigation. Specialty damage tags (`DamageSource`) added from day one to avoid retrofitting.

6. **Accessibility / Glimpse patterns:** _Done._ Added to non-goals: all new UI in Phases 14–17 must reuse existing Glimpse patterns, `DiceRollerModal`, and Phase 13 ARIA announcer. No new front-end dependencies.

7. **Crúac cap formula:** _Done._ Corrected from `Math.Min(10, 10 − CrúacRating)` to `10 − CrúacRating` (the `Math.Min` is redundant since the result is always ≤ 10). Note added: if future mechanics introduce additional Humanity ceilings they are `Math.Min`-composed at that point.

8. **Rename typo when implementing:** _Done._ `RessonanceOutcome` → `ResonanceOutcome` fixed in plan text.

---

## Residual notes — status

Earlier review residuals are reflected in the current plan:

1. **Background scheduling** — Phase 15 uses `TorporIntervalService : BackgroundService` (not `IBackgroundJobService`). A redundant Phase 15 task line that still referenced `IBackgroundJobService` was removed from the plan so tasks match the architecture.

2. **Phase 16b wording** — Gap row 25, Phase 19 objective, Current State table, and `DisciplineActivationService` bullet use **16b** where appropriate.

3. **Ledger audit format** — Single canonical suffix: ` | gate-override stUserId={userId} {timestamp:O}` (space-pipe-space before `gate-override`), with example in Phase 19 architectural decisions.

4. **Frenzy concurrency** — `FrenzyService` task specifies transactional `SaveChangesAsync` plus unique index or row version (not app-level locking only).

**Still on implementers (not a plan defect):** copy the ledger format and any tilt uniqueness rule into [rules-interpretations.md](./rules-interpretations.md) when those features ship, per the plan’s Rules Interpretation Log tasks.
