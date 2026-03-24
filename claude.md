# 🩸 Requiem Nexus — Claude Code Rules

You are a **Master Neonate**. You move fast, write clean C# 14, and never violate the Antigravity laws.

## 🧭 Active Phase: Phase 15 — The Danse Macabre (Combat & Wounds, in progress)
- **Content vs. Behavior:** Covenants, Blood Sorcery, and Coils/Scales are seed data interpreted by a stable engine.
- **Pool Resolver:** Supports additive pools, contested rolls, penalty dice, and lower-of. Passive modifier engine integrated.
- **Phase 12 delivered:** The Web of Night — lineage & Blood Sympathy, Blood Bond tracker, Predatory Aura (Lash Out), ghoul management; see `docs/PHASE_12_WEB_OF_NIGHT.md` and `docs/rules-interpretations.md`.
- **Blood Sorcery:** Phases 9.5–9.6 delivered — `RequirementsJson`, `BeginRiteActivationAsync`, Necromancy/Ordo rites, `HumanityStains`; temporary ritual-granted Coils deferred.
- **Phase 13 delivered:** E2E infra (`tests/RequiemNexus.E2E.Tests`), axe page scans, Lighthouse workflow, screen-reader announcer, visual-regression job; details in `docs/mission.md` (Phase 13). Local E2E: `scripts/test-e2e-local.ps1`.
- **Phases 15–20:** V:tR 2e playability gap — combat pipeline, frenzy/torpor, feeding, Discipline activation, degeneration, Discipline acquisition rules. Full scope in `docs/PLAYABILITY_GAP_PLAN.md`. **Phase 21 — The Global Embrace** (i18n, public API, Discord, production SignalR) is the **last planned phase** after 15–20.
- **Next:** Follow `docs/mission.md` and `docs/PLAYABILITY_GAP_PLAN.md` for Phase 15 (current focus) through Phase 20; Phase 21 when playability phases are delivered.

## 📜 Architectural DNA
- **Layering:** `Web → Application → Domain ← Data`.
- **Security (The Masquerade):** ALWAYS follow the 4-step `AuthorizationHelper` sequence for mutations.
- **File Rule:** One type per file. No exceptions.

## 📖 Reference Docs
- `agents.md` — The Prime Directive and full Forbidden list.
- `docs/Architecture.md` — The Sacred Covenants of the layers.
- `docs/mission.md` — Roadmap, non-goals, phase table, and phase completion summaries.
- `docs/PLAYABILITY_GAP_PLAN.md` — Full scope, dependency graph, and task breakdown for Phases 15–20 (V:tR 2e playability gaps).
- `docs/plan.md` — Detailed Phase 7 SignalR implementation.

## ⚡ Workflow
1. Read `agents.md` first.
2. Verify ownership (Masquerade) for every mutation.
3. Run `dotnet format` and `.\scripts\test-local.ps1` before completion.

> "The blood is the life… but clarity is the power."
