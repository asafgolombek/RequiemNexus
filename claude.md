# 🩸 Requiem Nexus — Claude Code Rules

You are a **Master Neonate**. You move fast, write clean C# 14, and never violate the Antigravity laws.

## 🧭 Active Phase: Phase 19 — The Blood Lineage (Discipline Acquisition)
- **Content vs. Behavior:** Covenants, Blood Sorcery, and Coils/Scales are seed data interpreted by a stable engine.
- **Pool Resolver:** Supports additive pools, contested rolls, penalty dice, and lower-of. Passive modifier engine integrated.
- **Phase 14 delivered:** The Danse Macabre — combat pipeline (`AttackService`, `AttackResult`, `CharacterHealthService`, `WoundPenaltyResolver`, NPC combat, `NpcCombatService`); B/L/A overflow, armor mitigation, Vitae fast-heal, wound penalty in pools, full combat UI.
- **Phase 15 delivered:** The Beast Within — `FrenzyService` (Resolve + Blood Potency save, Willpower optional), `TorporService` (entry/awakening/starvation), `VitaeService`, `WillpowerService`, `VitaeDepletedEvent` → Hunger frenzy auto-trigger, `TorporIntervalService` background service, `HealthDamageTrackBoxes` UI, torpor/frenzy UI panels.
- **Phase 16a delivered:** The Hunting Ground — `IHuntingService` / `HuntingService`, `HuntingPoolDefinition` + `HuntingRecord`, `HuntPanel`, territory bonus + campaign alignment, resonance display (static thresholds).
- **Phase 19 active 🔄:** The Blood Lineage — `Discipline` acquisition metadata, `DisciplinePower.PoolDefinitionJson` (unblocks 16b), `DisciplineJsonImporter`, 7 acquisition gates in `CharacterDisciplineService`, `IHumanityService`, `DegenerationCheckRequiredEvent`. See `docs/phase19-the-blood-lineage.md`.
- **Phase 17 ready (parallel):** Humanity & Conditions — independent of Phase 19; can proceed on a separate branch.
- **Phase 16b blocked on 19:** Discipline activation needs `PoolDefinitionJson` from Phase 19 first.
- **Phase 20 — The Global Embrace** is the **last planned phase** after 14–19.

## 📜 Architectural DNA
- **Layering:** `Web → Application → Domain ← Data`.
- **Security (The Masquerade):** ALWAYS follow the 4-step `AuthorizationHelper` sequence for mutations.
- **File Rule:** One type per file. No exceptions.

## 📖 Reference Docs
- `agents.md` — The Prime Directive and full Forbidden list.
- `docs/Architecture.md` — The Sacred Covenants of the layers.
- `docs/mission.md` — Roadmap, dependency graph, and full task breakdown for all phases including 14–19 (V:tR 2e playability gaps).
- `docs/phase19-the-blood-lineage.md` — **Active phase plan** — execution order, file paths, method signatures, gate logic, and tests for Phase 19.
- `docs/plan.md` — Detailed Phase 7 SignalR implementation.

## ⚡ Workflow
1. Read `agents.md` first.
2. Verify ownership (Masquerade) for every mutation.
3. Run `dotnet format` and `.\scripts\test-local.ps1` before completion.

> "The blood is the life… but clarity is the power."
