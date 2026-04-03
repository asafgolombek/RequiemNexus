# Review: Technical Improvement Plan (`plan-improvement.md`)

> **Review date:** 2026-04-02  
> **Re-review:** 2026-04-02 — compared current `plan-improvement.md` (504 lines) to this document; no open questions remain.  
> **Third pass:** 2026-04-02 — synced wording with Wave terminology, Q5 vs HTTP callout, and fixed plan cross-reference **1.2 → 3.4** (was incorrectly 3.2).  
> **Subject:** `docs/plan-improvement.md`  
> **Purpose:** Original review questions/suggestions; **now primarily a resolution log** tied to the revised plan (header cross-link in the plan points here).  
> **Status:** All first-pass questions answered; all first-pass suggestions applied in the plan. Item **Q5 section citation** below corrected (was 3.2; campaign work lives in **3.4**).

---

## Overall

The plan is unusually actionable: clear problem statements, concrete fixes, code sketches, and a prioritized backlog. It aligns well with `AGENTS.md` (layering, Masquerade, one-type-per-file, testing expectations) and with the Grimoire goal of teachable architecture. The **original** review targeted **dependency ordering**, **operational risks** (caching, `Task.Run`), **tooling constraints** (no new packages unless approved), and **deduplication** of repeated items — all now reflected in the current `plan-improvement.md`.

All first-pass gaps have been addressed in the revised plan. The plan now includes Section 5 (sequencing), Section 6 (risk register), exit criteria, hybrid errors, cache policy, zero-package session bus, and Appendix anti-patterns.

---

## Questions — Answered

### Q1. `ISessionEventBus` / `IObservable` — new NuGet dependency?

**Question:** The sketch uses reactive-style observables. Does the project want to add **System.Reactive** or another Rx package, or should the plan explicitly require a **zero–new-package** implementation? `AGENTS.md` forbids new NuGet dependencies without explicit approval.

**Answer:** Zero new packages. The revised plan (Section 2.4) uses a `ConcurrentDictionary<Guid, Action<T>>` subscription token pattern — no `System.Reactive` or other Rx library. Components store the returned `IDisposable` and call `Dispose()` once in `DisposeAsync`. The `IObservable<T>` sketch in the original plan has been replaced with `IDisposable SubscribeX(Action<T> handler)` overloads. If `System.Reactive` is wanted in a future phase it must go through the standard NuGet approval process in `AGENTS.md`.

---

### Q2. `IReferenceDataCache` invalidation

**Question:** The plan states reference data "never change at runtime." If admin editing of definitions is added later, how should cache refresh work (version stamp, explicit flush API, app restart only)?

**Answer:** **Application-lifetime only (restart to refresh)** for Phase 20. No flush API is needed now. The revised plan (Section 3.3) documents this explicitly and reserves a `FlushAsync()` contract on the interface for the future. When an admin definition-edit feature is implemented, that feature's implementation plan must call `FlushAsync()` after any definition mutation. The cache interface is designed so adding `FlushAsync()` is a non-breaking extension.

---

### Q3. `TraitResolver` → `Dictionary<PoolTraitType, Func<Character, int>>`

**Question:** Are all trait resolutions purely synchronous `Character → int`? If any path needs services, campaign context, or conditions, a flat dictionary of funcs may be insufficient.

**Answer:** The dictionary refactor applies **only to synchronous `Character → int` lookups** (direct attribute/skill/stat reads). The revised plan (Section 2.2) adds an explicit constraint: trait paths that require services, campaign context, or conditions remain as explicit branches or a separate `IContextualTraitResolver`. Confirm the full set of `PoolTraitType` values before implementing — do not assume all are simple property reads.

---

### Q4. Toast-only errors vs. inline form validation

**Question:** Replacing all `string? _error` fields with toasts can hurt inline validation (field-adjacent messages, accessibility, screen readers). Should the plan allow a hybrid?

**Answer:** **Yes — hybrid policy adopted.** The revised plan (Section 4.2) defines:
- `ToastService` for global/unexpected errors (service failures, auth errors, unhandled exceptions)
- A single `string? _validationError` per modal/form for field-adjacent inline validation

The rule "components do not own error state" in the original plan was too absolute. Components may own **one** inline validation error per form scope. They must not own 7 separate error fields for 7 different states.

---

### Q5. `CampaignService.GetCampaignByIdAsync` — Masquerade safety with merged query

**Question:** The single-query projection must still enforce no data leakage. Is the intended shape "minimal DTO + 404/403 when unauthorized," or full campaign with membership flag?

**Answer:** **Minimal DTO; unauthorized callers never receive campaign payload.** The revised plan (**Section 3.4** — `CampaignService.GetCampaignByIdAsync`; not Section 3.2, which is character reload) states explicitly:
1. The DTO shape is minimal — no sensitive campaign fields are included before the authorization check.
2. The Masquerade 4-step `AuthorizationHelper` sequence is still invoked in-memory on the returned DTO.
3. If the caller is not a member or storyteller, the method returns `null` / throws `NotFoundException` — it does not return the campaign even partially populated.

Section 3.4 also notes that **HTTP 404 vs 403** when surfacing that failure is an **implementation-time** choice (document in Application layer). The review’s earlier “403 when unauthorized” phrasing was shorthand for “deny access”; it is not prescriptive of the HTTP status.

Merging the queries is purely a round-trip optimization. Authorization semantics are unchanged.

---

### Q6. Scheduling vs. `docs/mission.md`

**Question:** The plan is "backlog — not yet scheduled." Should items be mapped to Phase 20 or a post-Phase 20 technical-debt track?

**Answer:** **P1–P2 items target Phase 20 polish; P3–P4 items are post-Phase 20.** The revised plan header includes this mapping and links to `docs/mission.md`. **Section 5** labels delivery order as **Wave 1–4** (with a naming note distinguishing waves from mission.md phase numbers) so items can be pulled into Phase 20 sprints one group at a time without coordination risk.

---

## Suggestions — Applied

### S1. Add a dependency / sequencing section
**Applied:** Section 5 defines **Wave 1–4** with explicit ordering rationale and a header note that “Wave” means implementation batch, not a mission.md product phase. Key dependency: the cache (**Wave 2**) must land before service-layer splits (**Wave 3**); service-layer API must stabilize before `CharacterDetails` decomposition (**Wave 4**).

---

### S2. Define measurable exit criteria per P1 theme
**Applied:** Each P1 item in the revised plan includes an **Exit criteria** block, e.g.:
- `CharacterDetails`: no partial class exceeds 300 lines, ≤ 5 injections each
- Beat-add: ≤ 2 DB queries after reload refactor
- Reference-data cache: 0 DB round-trips on warm instance

---

### S3. Testing strategy callout
**Applied:** P1 items include a **Tests** note pointing to integration test patterns (existing `RequiemNexus.Application.Tests` or EF query counting). Razor partial splits note that existing component tests must pass unchanged — new tests are only required when logic moves to a new service.

---

### S4. Line counts will drift
**Applied:** The plan header now reads "line counts correct as of 2026-04-02." All inline line counts use the `~` prefix (e.g., "~1,430 lines"). The canonical backlog (Section 7) refers to files by type/name only, not line numbers.

---

### S5. Deduplicate `OpenReference` / `Console.WriteLine`
**Applied:** The original plan had the `OpenReference` stub in Section 1.1, Section 4.5, Section 4.6, and two backlog rows (16 and 26). In the revised plan, these are merged into **one backlog row (#16)**. Section **1.1** points to backlog #16; Section **4.5** is a single consolidated audit (stub + `SessionClientService` + `DbInitializer`/seeders + `grep` follow-up). The former duplicate Section 4.6 was absorbed into 4.5.

---

### S6. `Console.WriteLine` inventory — test infra exclusion
**Applied:** Section 4.5 in the revised plan explicitly excludes test infrastructure (`*.Tests.*` projects) from the audit. It recommends a targeted `grep` command and notes that `Console.WriteLine` in test infra is acceptable.

---

### S7. `Task.Run` Blazor Server concurrency warning
**Applied:** Section 3.5 now includes a **Concurrency warning** block: bare `Task.Run` is acceptable for Phase 20 scale, but the long-term upgrade path (bounded `Channel<T>` export queue) is documented so it is not forgotten.

---

### S8. `ISeeder` placement
**Applied:** Section 1.2 (`DbInitializer`) now explicitly states: `ISeeder` and all implementations live in the **Data project** (`RequiemNexus.Data/Seeding/`). Seeders must not reference the Web project. Logging is via `ILogger` passed through the interface, consistent with the layering rules in `docs/Architecture.md`.

---

### S9. Database indexes need EF Core migration
**Applied:** Section 3.6 now opens with: "All index additions require an **EF Core migration** — do not add raw SQL. Add `HasIndex` calls to the entity configuration, then run `dotnet ef migrations add <name>`." The same reminder appears in backlog rows 26–27.

---

### S10. Appendix A — add counterexample
**Applied:** Appendix A has two tables: **Exemplary Patterns** (includes decomposition exemplars such as `CharacterCreation/` steps, `CharacterSheet/*AdvancementSection*`, `DanseMacabreTabs/*`, and **`EncounterParts/*`** for `EncounterManager`) and **Anti-Patterns (what to avoid)** (`CharacterDetails.razor.cs` pre-refactor, `DbInitializer.cs` pre-refactor). The **`EncounterManager.razor.cs`** row is framed as **historical (pre–2026-04-03)** — error-field sprawl mitigated by toast + fewer inline validation strings + **`EncounterParts/`** markup extraction; optional further consolidation remains (backlog **#20**).

---

## Editorial Items — Applied

### E1. Cross-link to `docs/mission.md`
**Applied:** Plan header and Section 5 both reference `docs/mission.md` Phase 20. Section 5’s naming note explicitly contrasts **Waves** with mission phase numbering.

### E2. Glossary row for P1–P4
**Applied:** Priority Legend table retained in revised plan with clearer "when to address" language per level.

### E3. Owner / RFC field
**Not applied — deferred.** The project does not currently use RFC or ownership tracking in plan docs. If a formal review process is introduced for large refactors (e.g., cache scope, session bus API), this can be added at that time. Not worth adding structure that has no process behind it yet.

### E4. Risk register
**Applied:** New Section 6 (Risk Register) covers: stale cache on future admin edits, `Task.Run` thread pool starvation, Masquerade safety on merged campaign query, seeder ordering bugs, and `CharacterDetails` decomposition breaking existing tests.

---

## Alignment check (unchanged — positive)

- **Masquerade** preserved: merged-query refactors explicitly maintain the `AuthorizationHelper` sequence.
- **Thin hub / thin Web** direction matches `SessionHub.cs` exemplar.
- **No migrations in "large file" split** respects the exclusion rule.
- **SRP decomposition** of `DbInitializer` and exports matches one-type-per-file and teachability.
- **No new NuGet packages** introduced by any item in the revised plan.

---

## Second pass — residual notes

These were not blockers; they were raised after the first-pass revision.

1. **Section 5 “Phase 1–4” vs roadmap Phase 20** ✅ Applied  
   Renamed to **Wave 1–4** in `plan-improvement.md` Section 5, with an explicit naming note explaining the distinction from `docs/mission.md` product phase numbers.

2. **403 vs `NotFoundException` wording** ✅ Applied  
   Section 3.4 of `plan-improvement.md` now includes a callout block: “The HTTP status mapping (404 vs 403) is an implementation-time decision — document in the Application layer. The plan’s invariant is authorization-first, not status-code-prescriptive.”

3. **`grep` example in plan Section 4.5** ✅ Applied  
   Section 4.5 now lists both the Unix `grep` form and the Windows `rg` (ripgrep) equivalent, plus IDE fallback.

4. **`ISessionEventBus` implementation detail** ✅ Applied  
   Section 2.4 now includes an explicit callout block: hub callbacks must marshal to Blazor’s sync context via `InvokeAsync` before invoking UI subscribers, to avoid `InvalidOperationException` on `StateHasChanged`. Noted as an implementer responsibility.

5. **E3 Owner / RFC**  
   Still appropriately deferred. No change.

---

## Changelog (this file)

| Date | Change |
|---|---|
| 2026-04-02 | Initial review → questions and suggestions. |
| 2026-04-02 | Plan updated; this file rewritten as resolution log. |
| 2026-04-02 | Re-review: fixed Q5 section reference (3.4); added second-pass residual notes and changelog. |
| 2026-04-02 | Second-pass items 1–4 applied to `plan-improvement.md`; all notes now resolved. |
| 2026-04-02 | Third pass: review header → 504 lines; S1/Q5/Q6/E1 wording synced with Waves + HTTP callout; `plan-improvement.md` §1.2 CampaignService cross-ref **3.2 → 3.4**. |
| 2026-04-02 | Wave 1 execution started: `DbInitializer` §3.1 fixes merged; Web `Console.WriteLine` → `ILogger`; fluent `HasIndex` for Ghoul/BloodBond regnant columns (snapshot already matched — no migration). Docs: `mission.md`, `CLAUDE.md`, `AGENTS.md`, plan header + §5 checkmarks. |
| 2026-04-02 | Wave 2 — `ISeeder` decomposition: `DbInitializer` orchestrates `IEnumerable<ISeeder>`; 13 seeders in `RequiemNexus.Data/Seeding/` + `SorceryRiteSeedingHelper`; `AddRequiemDataSeeders()` in Web infrastructure; tests/E2E resolve seeders from DI. `IReferenceDataCache` not started (remaining Wave 2 item). |
