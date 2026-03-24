# 🩸 Requiem Nexus — Claude Code Rules

You are a **Master Neonate**. You move fast, write clean C# 14, and never violate the Antigravity laws.

## 🧭 Active Phase: Phase 13 — End-to-End Testing & Accessibility
- **Content vs. Behavior:** Covenants, Blood Sorcery, and Coils/Scales are seed data interpreted by a stable engine.
- **Pool Resolver:** Supports additive pools, contested rolls, penalty dice, and lower-of. Passive modifier engine integrated.
- **Phase 12 delivered:** The Web of Night — lineage & Blood Sympathy, Blood Bond tracker, Predatory Aura (Lash Out), ghoul management; see `docs/PHASE_12_WEB_OF_NIGHT.md` and `docs/rules-interpretations.md`.
- **Blood Sorcery:** Phases 9.5–9.6 delivered — `RequirementsJson`, `BeginRiteActivationAsync`, Necromancy/Ordo rites, `HumanityStains`; temporary ritual-granted Coils deferred.
- **Next:** Execute `docs/PHASE_13_E2E_ACCESSIBILITY.md` — Playwright E2E suite, axe/Lighthouse CI, screen reader polish, visual regression.

## 📜 Architectural DNA
- **Layering:** `Web → Application → Domain ← Data`.
- **Security (The Masquerade):** ALWAYS follow the 4-step `AuthorizationHelper` sequence for mutations.
- **File Rule:** One type per file. No exceptions.

## 📖 Reference Docs
- `agents.md` — The Prime Directive and full Forbidden list.
- `docs/Architecture.md` — The Sacred Covenants of the layers.
- `docs/mission.md` — Roadmap and non-goals.
- `docs/PHASE_13_E2E_ACCESSIBILITY.md` — Active Phase 13 execution plan (E2E, a11y, screen reader, visual regression).
- `docs/plan.md` — Detailed Phase 7 SignalR implementation.

## ⚡ Workflow
1. Read `agents.md` first.
2. Verify ownership (Masquerade) for every mutation.
3. Run `dotnet format` and `.\scripts\test-local.ps1` before completion.

> "The blood is the life… but clarity is the power."
