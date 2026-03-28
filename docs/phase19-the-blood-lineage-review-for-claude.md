# Phase 19 Plan Review — Questions, Suggestions, Improvements

Companion to [`phase19-the-blood-lineage.md`](./phase19-the-blood-lineage.md). Use this when implementing or refining the phase so gaps are resolved before coding.

---

## Critical corrections (fix in plan or implementation)

### 1. `DbInitializer` method name and seed path ✅ FIXED

The plan referred to `EnsureDisciplinesAsync`. The codebase uses **`SeedClansAndDisciplinesAsync`** with guard `!hasClansAndDisciplines` (checks both clans and disciplines).

**Verified seeding order (lines 22–34 of `DbInitializer.cs`):**
1. `SeedRolesAsync`
2. **`SeedClansAndDisciplinesAsync`** ← disciplines here
3. `SeedHuntingPoolDefinitionsAsync`
4. `SeedMeritsAsync`
5. `SeedEquipmentCatalogAsync`
6. **`SeedCovenantsAsync`** ← covenants here
7. `SeedCovenantDefinitionMeritsAsync`
8. **`SeedBloodlinesAsync`** ← bloodlines here
9. …

**Critical consequence found:** Covenants and bloodlines seed AFTER disciplines. The original plan's "load FKs at seed time" was impossible. **Resolution:** Two-pass seeding — Pass 1 (`SeedClansAndDisciplinesAsync` → `LoadFromDocs()`) sets booleans + `PoolDefinitionJson`; Pass 2 (`UpdateDisciplineAcquisitionMetadataAsync`, called after step 8) resolves `CovenantId` and `BloodlineId` by name. Plan updated throughout Group B accordingly.

---

### 2. Necromancy "bloodline" gate — pseudocode corrected ✅ FIXED

`BloodlineDefinition` has **no** `IsBloodlineDiscipline` property. It has `FourthDisciplineId` (FK to `Discipline`) that identifies the fourth in-clan discipline the bloodline grants.

**Correct gate check (now in D6):**
```csharp
bool hasNecromancyBloodline = character.Bloodlines
    .Any(cb => cb.BloodlineDefinition?.FourthDisciplineId == discipline.Id)
```

Also added to rules log (G4 entry 7) so the data model path is documented for future maintainers.

---

### 3. Server-side trust for `AcquisitionAcknowledgedByST` ✅ FIXED

Added as **D9** in the plan. The service verifies the requesting `userId` is the campaign Storyteller via **`IAuthorizationHelper.IsStorytellerAsync(campaignId, userId)`** (non-throwing predicate added in Phase 19 — see plan D9 and Files to Modify). Player requests silently treat the flag as `false` (no error exposed). Specific tests added in G1: `AddDiscipline_CovenantGate_PlayerBypassAttempt_Fails`.

> **Do not confuse** with `ISessionAuthorizationService.IsStorytellerAsync` (SignalR / chronicle id, different layer) — discipline acquisition uses **`IAuthorizationHelper`** and `Character.CampaignId`.

---

### 4. Existing databases after migration ✅ FIXED

The new `UpdateDisciplineAcquisitionMetadataAsync` (B3) runs unconditionally after step 8 — it is not guarded by an "already seeded" check. It **full-syncs** official disciplines from JSON (bools, FKs, power `PoolDefinitionJson`). This correctly handles both fresh installs and existing staging/prod databases without requiring a DB reset. *(Prefer saving only when values actually change — see Third review.)*

---

### 5. Razor snippet in F3 — invalid C# ✅ FIXED

`@("•".Repeat(crucRating))` replaced with:
```csharp
string dots = new string('•', crucRating);
```
Used in the updated F3 Razor snippet in the plan.

---

## Questions resolved

### Q1. Phase 17 vs Phase 19 — `IHumanityService` and `DegenerationCheckRequiredEvent` ownership ✅ ANSWERED

Both definitions are in Phase 19 by default. The coordination rule is explicit in the plan's dependency graph and Group C header: **whoever lands first owns the definitions; whoever lands second only extends them**. If Phase 17 lands first, it introduces the event + enum + service; Phase 19 adds `CrúacPurchase` to the enum and calls `GetEffectiveMaxHumanity`. Grep guard documented in Group C.

---

### Q2. Coils of the Dragon vs `CharacterDiscipline` ✅ ANSWERED

Scoped in the plan (Group E "Scope Note"). The 2-of-3 creation rule applies to `CharacterDiscipline` rows only. Coils are `CharacterCoil` entities managed by the existing coil flow — not validated by `CharacterCreationService`. The covenant-status gate for Coils is already handled by `CoilService`. Also recorded in G4 rules log entry 9.

---

### Q3. Covenant gate naming ✅ VERIFIED

Confirmed from `SeedSource/Covenants.json`:
- Circle of the Crone → **`"The Circle of the Crone"`**
- Lancea et Sanctum → **`"The Lancea et Sanctum"`**

Both include the `"The "` prefix. Updated throughout B1 (JSON schema table and example) and the `ReadBool` + importer logic. The `UpdateDisciplineAcquisitionMetadataAsync` lookup uses `StringComparer.OrdinalIgnoreCase` to tolerate minor capitalization drift.

---

### Q4. Discipline identity for gates — ID vs name ✅ ANSWERED

Strategy documented in D2–D8 and in G4 rules log entry 8: **FK/ID comparison preferred for all gates**. The only string match (`discipline.Name == "Crúac"` in Gate 5) is used where no FK is available at the call site; its use is recorded in the rules log to catch future renames.

---

### Q5. `ReadBool` behavior ✅ CONFIRMED

Returns `true` only when the JSON value is `JsonValueKind.True`. Missing keys → `false`. All new bool fields must be spelled out explicitly in JSON. This is intentional and consistent with the other seed data loaders. No "default true" fields in Phase 19 scope.

---

### Q6. Rules log page reference placeholder ✅ ACTIONED

`VtR 2e p.XX` placeholder for Crúac breaking point noted in G4 entry 2 as "verify and fill in before closing the phase." This is a documentation task for the implementer at phase close, not a blocker.

---

## Suggestions resolved

### S1. Order of seeding ✅ ADDRESSED

Seeding order verified and documented in Group B "Seeding Order Context" with the full numbered list. Two-pass approach resolves the FK dependency issue.

---

### S2. Table column count in A3 ✅ CORRECTED

A3 now states "5 bool columns + 2 nullable int FK columns" (not generic "7 columns") to match the generated migration output accurately.

---

### S3. Gate 7 — Crúac Humanity cap enforcement ✅ ADDRESSED

Plan updated in D2–D8 (end of section): when Crúac is purchased and `GetEffectiveMaxHumanity(character) < character.Humanity`, clamp `character.Humanity` to the cap at purchase time. This prevents invalid DB state. Documented in G4 rules log entry 4.

---

### S4. D9 handler — unused `ISessionService` constructor param ✅ FIXED

`ISessionService` removed from the handler constructor stub. It will be added by Phase 17 when the ST banner notification is wired. Current stub only uses `ILogger` to avoid analyzer noise.

---

### S5. Character creation validation timing ✅ CLARIFIED

Group E documents: validate reactively on each discipline change AND on final submit. The character object during creation reflects only starting dots. Block final progression, not intermediate changes.

---

### S6. Hard gate bypass test ✅ ADDED

G1 test table now includes explicit tests that `AcquisitionAcknowledgedByST = true` does NOT bypass hard gates:
- `AddDiscipline_BloodlineRestriction_CannotBypassWithST`
- `AddDiscipline_ThebanFloor_CannotBypassWithST`

---

### S7. Mission.md cross-link ✅ DEFERRED

Optional doc hygiene — add a pointer from `docs/mission.md` to `phase19-the-blood-lineage.md` when the phase is finalized. Not a blocker; will be handled in the mission.md status update at phase completion.

---

## Minor / editorial

### B4 "Levinbolt" ⚠ OPEN

Verify Celerity 5 name against the physical VtR 2e core book before commit. Note source page in B5 task if using core vs. supplement name.

### D8 audit string — gate name included ✅ FIXED

Audit format updated to include gate name: `" | gate-override:{gate-name} stUserId={userId} {timestamp:O}"` where gate name is `covenant`, `teacher`, or `necromancy`. Parseable without context.

### Group C Phase 17 label ✅ ADDED

Group C in the execution order now carries a `⚠ Coordination point with Phase 17` label. Dependency graph also notes this as a merge-order consideration.

---

## Summary

All critical corrections are applied to the plan. All questions are answered. All suggestions are resolved or explicitly deferred. The one remaining open item is the Celerity 5 power name verification (B4/B5) — this is a lookup task for the implementer, not a design gap.

---

## Second review (after plan + review doc updates)

Re-read [`phase19-the-blood-lineage.md`](./phase19-the-blood-lineage.md) and this file together. The plan is in strong shape. Below are **remaining gaps** and **optional improvements** not fully closed by the current text.

### Open — align D9 with real `IAuthorizationHelper` API ✅ FIXED

`IAuthorizationHelper` only exposes `RequireStorytellerAsync` (throws on failure) — no bool predicate existed.

**Resolution:** Plan updated in D9 to add `IsStorytellerAsync(int campaignId, string userId)` to both `IAuthorizationHelper` and `AuthorizationHelper`, reusing the same `Campaigns.AnyAsync(c => c.Id == campaignId && c.StoryTellerId == userId)` query already inside `RequireStorytellerAsync`. Both files added to the Files to Modify table.

`character.CampaignId` is nullable (`int?`) — confirmed in the data model. When null, `stAcknowledged` is `false` (soft gate bypass unavailable for unassigned characters). Documented in D9 snippet.

---

### Open — existing databases: acquisition bools and `PoolDefinitionJson` ✅ FIXED

**Resolution:** `UpdateDisciplineAcquisitionMetadataAsync` (B3) is now a **full sync** of all Phase 19 fields: booleans (`CanLearnIndependently`, etc.), `CovenantId`, `BloodlineId`, and `DisciplinePower.PoolDefinitionJson`. It runs unconditionally on every startup, updating rows by name match. Animalism, Celerity, etc. will get their correct `CanLearnIndependently = true` on upgraded DBs. Phase 16b pool JSON is also covered.

---

### Open — rules log page reference ⚠ CLOSEOUT ITEM

G4 entry 2: “verify and fill in before closing the phase.” Remains an implementer task at phase close — not a design blocker.

---

### Open — B5 / Levinbolt ⚠ CONTENT TASK

Naming verification for Celerity 5 (and Resilience / Vigor) against the core book is a content task for the implementer. Note source page in the B5 commit message.

---

### Suggestions resolved

**S8 — E2 vs. `DisciplinesRules.txt` (“stolen secrets” at creation) ✅ ADDRESSED**

Plan updated in E2 with a dedicated note: if the creation flow allows Crúac / Theban as a third creation dot, the same Covenant Status gate (D3) and ST acknowledgment modal (F1) must apply. Implementer must verify whether the creation catalogue includes covenant disciplines and document the chosen behaviour in `rules-interpretations.md`.

**S9 — G1 Necromancy bloodline test fixture ✅ ADDRESSED**

G1 test table now includes a fixture note: either confirm an existing seed bloodline has `FourthDisciplineId` pointing to Necromancy, or seed a minimal `BloodlineDefinition` fixture directly in the test.

**S10 — Mission.md cross-link ⚠ DEFERRED**

Still deferred; add when Phase 19 status flips in [`mission.md`](./mission.md).

---

## What is fully closed

Two-pass seeding order, covenant name prefix (`The …`), Necromancy `FourthDisciplineId` gate, D9 ST-only bool predicate + null-CampaignId handling, full-sync B3 covering bools + powers, F3 Razor, Crúac Humanity clamp, D10 logger-only handler, Phase 17 merge / Coils scope, hard-gate tests, audit gate names, E2 stolen-secrets note, G1 test fixture note.

## Remaining open items (implementer tasks, not design gaps)

- ⚠ Crúac breaking-point page reference in `rules-interpretations.md` (G4 entry 2)
- ⚠ Celerity 5 / Resilience / Vigor power name verification against core book (B5)
- ⚠ `mission.md` cross-link when phase closes

---

## Third review (latest plan + review sync)

Cross-checked [`phase19-the-blood-lineage.md`](./phase19-the-blood-lineage.md) again. The plan now includes **full B3 sync** (bools + FKs + `PoolDefinitionJson`), **D9** with **`IsStorytellerAsync`** on `IAuthorizationHelper`, nullable **`CampaignId`**, **E2** stolen-secrets note, and **G1** Necromancy fixture note. **Stray markdown** after the B3 code block (extra ` ``` `) and the **”creation creation”** typo in Group E were corrected in the plan file.

### B3 `anyChanged` logic ✅ FIXED

`anyChanged = true` (unconditional per-row flag) replaced with `context.ChangeTracker.HasChanges()` after the sync loop. EF tracks only properties whose value genuinely changed — `SaveChangesAsync` is a no-op on a fully-synced DB. No unnecessary write on every host start.

### G4 entry for creation covenant catalogue ✅ ADDED

G4 entry 10 added: record whether the creation discipline selector exposes Crúac / Theban, and document the gate behaviour (D3 + F1 modal at creation, or intentional catalogue restriction deferring the gate to Advancement).

### Tests for `AuthorizationHelper.IsStorytellerAsync` ✅ ADDED

New **G2b** section added with three tests covering `true`, `false`, and invalid-campaign-id cases. Mirrors the query in `RequireStorytellerAsync` to guard against future divergence.

---

## Remaining open items (implementer tasks, not design gaps)

- ⚠ Crúac breaking-point page reference in `rules-interpretations.md` (G4 entry 2)
- ⚠ Celerity 5 / Resilience / Vigor power name verification against core book (B5)
- ⚠ `mission.md` cross-link when phase closes
- ⚠ G4 entry 10: verify creation discipline catalogue scope and record in `rules-interpretations.md`

## What is fully closed

All design questions resolved. No unresolved architecture, security, or data-model gaps remain. The plan is implementation-ready.
