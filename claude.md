# 🩸 Requiem Nexus — Claude Code Rules

You are a **Master Neonate**. You move fast, write clean C# 14, and never violate the Antigravity laws.

## 🧭 Active Phase: Phase 20 — The Global Embrace — ✅ roadmap and technical polish complete (2026-04-04). Discord session webhooks + SignalR hub limits; further polish is discretionary with explicit scope (record: `docs/mission.md`, Phase 20). Not near-term: i18n, public API.
- **Content vs. Behavior:** Covenants, Blood Sorcery, and Coils/Scales are seed data interpreted by a stable engine.
- **Pool Resolver:** Supports additive pools, contested rolls, penalty dice, and lower-of. Passive modifier engine integrated.
- **Phase 14 delivered:** The Danse Macabre — combat pipeline (`AttackService`, `AttackResult`, `CharacterHealthService`, `WoundPenaltyResolver`, NPC combat, `NpcCombatService`); B/L/A overflow, armor mitigation, Vitae fast-heal, wound penalty in pools, full combat UI.
- **Phase 15 delivered:** The Beast Within — `FrenzyService` (Resolve + Blood Potency save, Willpower optional), `TorporService` (entry/awakening/starvation), `VitaeService`, `WillpowerService`, `VitaeDepletedEvent` → Hunger frenzy auto-trigger, `TorporIntervalService` background service, `HealthDamageTrackBoxes` UI, torpor/frenzy UI panels.
- **Phase 16a delivered:** The Hunting Ground — `IHuntingService` / `HuntingService`, `HuntingPoolDefinition` + `HuntingRecord`, `HuntPanel`, territory bonus + campaign alignment, resonance display (static thresholds).
- **Phase 19 delivered:** The Blood Lineage — `Discipline` acquisition metadata, `DisciplineJsonImporter`, two-pass seed pipeline, 7 acquisition gates in `CharacterDisciplineService`, `IHumanityService`, `DegenerationCheckRequiredEvent`.
- **Phase 16b delivered:** The Discipline Engine — `DisciplineActivationService`, `ActivationCost`, activation UI; pool size returned to `DiceRollerModal` for feed publication (rite pattern). See `docs/phase16b-the-discipline-engine.md`.
- **Phase 17 delivered:** The Fog of Eternity — `IConditionRules.GetPenalties()`, condition penalties in `ModifierService`, `EvaluateStainsAsync` call sites, degeneration + remorse + incapacitated UI. See `docs/mission.md` (Phase 17 section) and `docs/rules-interpretations.md`.
- **Phase 18 delivered ✅:** The Wider Web — passive predatory aura, blood sympathy roll, social maneuver interceptors, SeedSource catalog passes (D1–D8), discipline pool JSON + Vitae/Willpower choice UI. Record: **`docs/mission.md`** (Phase 18 section).
- **Phase 19.5 — The Rite Perfected** is **complete** (2026-04-02): Crúac/Theban/Necromancy rules accuracy — extended rites, informational Potency, ritual Conditions, canonical SeedSource catalogs, BOM fix, Theban sacrament UX, Necromancy clan gate, P4 backlog verified. Record: **`docs/mission.md`** (Phase 19.5) and **`docs/rules-interpretations.md`** (Phase 19.5).
- **Phase 20 — The Global Embrace** — **roadmap checklist complete** (Discord incoming webhooks for session presence, SignalR receive-size tuning); last planned V:tR roadmap phase — see `docs/mission.md`.
- **Phase 20 technical polish — delivered** (2026-04-03–05): record in **`docs/mission.md`** Phase 20. Shipped: `ISeeder` pipeline, `IReferenceDataCache`, `ICharacterQueryService`, `IModifierProvider` / `IRiteActivationStrategy`, `CharacterDetails` partials, loading/toast patterns, `CharacterAdvancement` sections, `DanseMacabreTabs/`, **`EncounterManager`** + **`EncounterParts/`** + NpcPicker/SmartLaunch, **`InitiativeTracker`** + **`InitiativeParts/`**, **`DiceRollerModal`** decomposition.

## 📜 Architectural DNA
- **Layering:** `Web → Application → Domain ← Data`.
- **Security (The Masquerade):** ALWAYS follow the 4-step `AuthorizationHelper` sequence for mutations.
- **File Rule:** One type per file. No exceptions.

## 📖 Reference Docs
- `agents.md` — The Prime Directive and full Forbidden list.
- `docs/Architecture.md` — The Sacred Covenants of the layers.
- `docs/mission.md` — Roadmap, dependency graph, and per-phase delivery (including **Phase 18: The Wider Web** — ✅ complete).
- `docs/phase16b-the-discipline-engine.md` — Phase 16b delivery record (discipline activation).
- `docs/plan.md` — Detailed Phase 7 SignalR implementation.

## ⚡ Workflow
1. Read `agents.md` first.
2. Verify ownership (Masquerade) for every mutation.
3. Run `dotnet format` and `.\scripts\test-local.ps1` before completion.

> "The blood is the life… but clarity is the power."
