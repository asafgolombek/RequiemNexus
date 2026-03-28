# Review: Phase 16a — The Hunting Ground

Companion to [`PHASE_16A_THE_HUNTING_GROUND.md`](./PHASE_16A_THE_HUNTING_GROUND.md). **This file is review notes only** — not a second source of truth for implementation.

---

## Open questions — RESOLVED

1. **Territory ID vs. campaign** ✅
   **Decision:** Cross-campaign mismatch is a hard failure. `HuntingService` loads the territory and returns `Result.Failure("Territory does not belong to this campaign.")` if `territory.CampaignId != character.CampaignId`. This is data integrity, not narrative gating — the plan is updated at AD §2 and step 5 of the logic walkthrough.

2. **Character with no campaign** ✅
   **Decision:** If `Character.CampaignId` is null and `territoryId` is non-null, `Result.Failure` is returned — same path as the campaign mismatch check. Territory picker is empty when the character has no campaign, so the UI path is unreachable; the service guard protects the API path. Verified by verification step 10.

3. **Minimum pool size** ✅
   **Decision:** Pool floor of 1 die is enforced after `ResolvePoolAsync` + territory bonus. A resolver returning 0 (e.g. missing traits) is treated as a data setup issue — clamp to 1 and roll rather than returning `Result.Failure`. This preserves audit completeness and avoids surfacing internal data gaps as user-visible errors. Added to Rules Interpretation Log and logic walkthrough step 6.

4. **Chronicle ID for dice feed** ✅
   **Decision:** `Character.CampaignId` is used directly as the chronicle scope, consistent with `FrenzyService`. Dice feed is skipped (no call) when `CampaignId` is null. Confirmed in logic walkthrough step 12.

5. **Predator Type assignment (same phase or follow-up)** ✅
   **Decision:** Deferred. Phase 16a scopes only the hunt execution. Setting `PredatorType` requires a future ST/character-creation UI task. For playtesting, ST assigns via DB or a future admin endpoint. The null guard (button hidden, service fails) is the complete Phase 16a behavior.

6. **NPC / ST-initiated hunts** ✅
   **Decision:** Player characters only in Phase 16a. The `userId` ownership check (`RequireCharacterAccessAsync`) already prevents ST from running hunts for NPCs without a bypass pattern. NPC hunting is deferred to a later phase when an ST-bypass pattern is established consistently across services.

7. **Structured logging and metrics** ✅
   **Decision:** Yes. `ExecuteHuntAsync` emits one structured log event: `{ CharacterId, CampaignId, PredatorType, PoolSize, Successes, VitaeGained, Resonance, TerritoryId }` using Serilog. Added to logic walkthrough step 13. Counters/histograms are deferred — logging the structured event is sufficient for Phase 16a observability.

8. **Integration vs. unit tests** ✅
   **Decision:** `Application.Tests` for `HuntingService` (mocked `IDiceService`, `ITraitResolver`, `IVitaeService`). Eight named cases added to the checklist: successful hunt, null PredatorType, missing pool definition, territory bonus, campaign mismatch, zero-success, Masquerade rejection, pool floor clamp. Domain-layer pure helpers (resonance thresholds) can be tested there if extracted later.

---

## Suggestions — disposition

1. **Unique constraint on `HuntingPoolDefinition.PredatorType`** → **Adopted.** `HasIndex(h => h.PredatorType).IsUnique()` added to the plan (Data section) and checklist.

2. **Resolve `FeedingTerritory` load with campaign check** → **Adopted.** Implemented as a hard `Result.Failure` (see Q1 above), not just a WHERE clause — avoids silent misses.

3. **Document Windows migration command** → **Adopted.** PowerShell one-liner added alongside the Unix example in the Migration section.

4. **Optional: store dice breakdown on `HuntingRecord`** → **Deferred.** Dice feed is considered sufficient for replay/debug in Phase 16a. A future task can add breakdown JSON if audit needs grow. Added as a one-line note in the Ledger Volume section.

5. **Align `mission.md` / `PLAYABILITY_GAP_PLAN.md`** → **Deferred to a cleanup pass.** The plan already notes the resonance JSON seed difference at AD §4. A doc-only alignment task can be scheduled after Phase 16a is delivered.

6. **Accessibility** → **Adopted.** HuntPanel spec now explicitly requires one `aria-live` sentence covering successes, Vitae gained, and resonance (Phase 13 roll-announcement pattern).

7. **`HuntResult` and API surface** → **Adopted.** Plan now documents that `HuntPanel` is Blazor server-side only (no HTTP API in Phase 16a). If an HTTP endpoint is added later, a response DTO should be mapped from `HuntResult`.

8. **Long-term ledger volume** → **Adopted.** A one-line note added to the Character Sheet Integration section: pagination / pruning policy is a later backlog item.

9. **Resonance helper location** → **Kept as private static in Phase 16a.** A future phase can extract to `Domain` if thresholds become data-driven. The note is acknowledged but not acted on now to avoid speculative abstraction.

10. **Verification step for territory mismatch** → **Adopted.** Explicit test cases added to the Verification section (steps 9 and 10) covering campaign mismatch and null-campaign + non-null territory.

---

## Residual (intentional or outside Phase 16a)

| Item | Status |
|------|--------|
| **`mission.md` / `PLAYABILITY_GAP_PLAN.md` bullets** (e.g. resonance JSON seed) | Still deferred doc cleanup — not blocking implementation if builders follow `PHASE_16A_THE_HUNTING_GROUND.md`. |
| **OpenTelemetry metrics** (counters/histograms beyond Serilog) | Explicitly deferred; structured log in walkthrough step 13 is the Phase 16a bar. |
| **NPC / ST hunt bypass** | Deferred to a later phase (consistent ST pattern across services). |
| **`PredatorType` editor UI** | Deferred; playtest via DB or future admin path. |

No unresolved **design** questions remain relative to the updated plan.

---

## Overall

All open questions are resolved. The plan is updated at:
- AD §2 (campaign alignment)
- Logic walkthrough steps 5–6 (territory check + pool floor) and step 13 (structured logging)
- Rules Interpretation Log (territory formula + pool floor)
- Checklist (unique index task, test task)
- Verification section (steps 9–11)
- Character Sheet Integration (Blazor server-side note, ledger volume note)
- Migration section (PowerShell command)
