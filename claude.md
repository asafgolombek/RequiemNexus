# 🩸 Requiem Nexus — Claude Code Rules

You are a **Master Neonate**. You move fast, write clean C# 14, and never violate the Antigravity laws.

## 🧭 Active Phase: Phase 17 — The Fog of Eternity (Humanity & Conditions)
- **Content vs. Behavior:** Covenants, Blood Sorcery, and Coils/Scales are seed data interpreted by a stable engine.
- **Pool Resolver:** Supports additive pools, contested rolls, penalty dice, and lower-of. Passive modifier engine integrated.
- **Phase 14 delivered:** The Danse Macabre — combat pipeline (`AttackService`, `AttackResult`, `CharacterHealthService`, `WoundPenaltyResolver`, NPC combat, `NpcCombatService`); B/L/A overflow, armor mitigation, Vitae fast-heal, wound penalty in pools, full combat UI.
- **Phase 15 delivered:** The Beast Within — `FrenzyService` (Resolve + Blood Potency save, Willpower optional), `TorporService` (entry/awakening/starvation), `VitaeService`, `WillpowerService`, `VitaeDepletedEvent` → Hunger frenzy auto-trigger, `TorporIntervalService` background service, `HealthDamageTrackBoxes` UI, torpor/frenzy UI panels.
- **Phase 16a delivered:** The Hunting Ground — `IHuntingService` / `HuntingService`, `HuntingPoolDefinition` + `HuntingRecord`, `HuntPanel`, territory bonus + campaign alignment, resonance display (static thresholds).
- **Phase 19 delivered:** The Blood Lineage — `Discipline` acquisition metadata, `DisciplineJsonImporter`, two-pass seed pipeline, 7 acquisition gates in `CharacterDisciplineService`, `IHumanityService`, `DegenerationCheckRequiredEvent`.
- **Phase 16b delivered:** The Discipline Engine — `DisciplineActivationService`, `ActivationCost`, activation UI; pool size returned to `DiceRollerModal` for feed publication (rite pattern). See `docs/phase16b-the-discipline-engine.md`.
- **Phase 17 active 🔄:** Humanity & Conditions — extend `IConditionRules.GetPenalties()` (no migration), wire into `ModifierService`; wire `EvaluateStainsAsync` call sites; degeneration + remorse + incapacitated UI. See `docs/final_steps.md` (execution detail supersedes `docs/mission.md` for this phase).
- **Phase 18 planned ⬜:** The Wider Web — passive Predatory Aura, Blood Sympathy roll, social maneuver interception, content passes. See `docs/final_steps.md`.
- **Phase 20 — The Global Embrace** is the **last planned phase** after the V:tR 2e **playability** arc on the roadmap: **14–16b** and **17–19** (see `docs/mission.md`).

## 📜 Architectural DNA
- **Layering:** `Web → Application → Domain ← Data`.
- **Security (The Masquerade):** ALWAYS follow the 4-step `AuthorizationHelper` sequence for mutations.
- **File Rule:** One type per file. No exceptions.

## 📖 Reference Docs
- `agents.md` — The Prime Directive and full Forbidden list.
- `docs/Architecture.md` — The Sacred Covenants of the layers.
- `docs/mission.md` — Roadmap, dependency graph, and full task breakdown for all phases including 14–16b, 17–19 (V:tR 2e playability gaps).
- `docs/phase16b-the-discipline-engine.md` — Phase 16b delivery record (discipline activation).
- `docs/final_steps.md` — Execution-level plan for Phases 17–18 (supersedes phase sections in `mission.md` for implementation detail).
- `docs/plan.md` — Detailed Phase 7 SignalR implementation.

## ⚡ Workflow
1. Read `agents.md` first.
2. Verify ownership (Masquerade) for every mutation.
3. Run `dotnet format` and `.\scripts\test-local.ps1` before completion.

> "The blood is the life… but clarity is the power."
