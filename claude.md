# 🩸 Requiem Nexus — Claude Code Rules

You are a **Master Neonate**. You move fast, write clean C# 14, and never violate the Antigravity laws.

## 🧭 Active Phase: Phase 16a — The Hunting Ground (Feeding)
- **Content vs. Behavior:** Covenants, Blood Sorcery, and Coils/Scales are seed data interpreted by a stable engine.
- **Pool Resolver:** Supports additive pools, contested rolls, penalty dice, and lower-of. Passive modifier engine integrated.
- **Phase 14 delivered:** The Danse Macabre — combat pipeline (`AttackService`, `AttackResult`, `CharacterHealthService`, `WoundPenaltyResolver`, NPC combat, `NpcCombatService`); B/L/A overflow, armor mitigation, Vitae fast-heal, wound penalty in pools, full combat UI. See `docs/PLAYABILITY_GAP_PLAN.md`.
- **Phase 15 delivered:** The Beast Within — `FrenzyService` (Resolve + Blood Potency save, Willpower optional), `TorporService` (entry/awakening/starvation), `VitaeService`, `WillpowerService`, `VitaeDepletedEvent` → Hunger frenzy auto-trigger, `TorporIntervalService` background service, `HealthDamageTrackBoxes` UI, torpor/frenzy UI panels. See `docs/PLAYABILITY_GAP_PLAN.md`.
- **Phases 16–19:** V:tR 2e playability gap — feeding, Discipline activation, degeneration, Discipline acquisition rules. Full scope in `docs/PLAYABILITY_GAP_PLAN.md`. **Phase 20 — The Global Embrace** (i18n, public API, Discord, production SignalR) is the **last planned phase** after 14–19.
- **Next:** Follow `docs/mission.md` and `docs/PLAYABILITY_GAP_PLAN.md` for Phase 16a (current focus) through Phase 19; Phase 20 when playability phases are delivered.

## 📜 Architectural DNA
- **Layering:** `Web → Application → Domain ← Data`.
- **Security (The Masquerade):** ALWAYS follow the 4-step `AuthorizationHelper` sequence for mutations.
- **File Rule:** One type per file. No exceptions.

## 📖 Reference Docs
- `agents.md` — The Prime Directive and full Forbidden list.
- `docs/Architecture.md` — The Sacred Covenants of the layers.
- `docs/mission.md` — Roadmap, non-goals, phase table, and phase completion summaries.
- `docs/PLAYABILITY_GAP_PLAN.md` — Full scope, dependency graph, and task breakdown for Phases 14–19 (V:tR 2e playability gaps).
- `docs/plan.md` — Detailed Phase 7 SignalR implementation.

## ⚡ Workflow
1. Read `agents.md` first.
2. Verify ownership (Masquerade) for every mutation.
3. Run `dotnet format` and `.\scripts\test-local.ps1` before completion.

> "The blood is the life… but clarity is the power."
