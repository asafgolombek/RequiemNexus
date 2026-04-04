# RequiemNexus — Technical Improvement Plan

## Start here (agents)

<a id="start-here-agents"></a>

1. **Waves 1–4 and the §7 consolidated backlog are complete.** There is **no** committed Wave 5 in this document.
2. **Post–Wave 4 work** lives only in **[Optional backlog](#optional-backlog)** (O-1 … O-n). Implement an O-row **only** when `docs/mission.md` or an explicit task scopes it — do not mine Sections 1–6 for implied scope.
3. **Grimoire:** Sections 1–6 are **reference** (patterns, rationale, history). §7 is the **closed** delivery record. Historical review Q&A: [`docs/plan-improvement-review.md`](./plan-improvement-review.md).

> **Scope:** Performance, large-file decomposition (migrations excluded), UI/UX consistency, and SOLID principle enforcement.
> **Date:** 2026-04-02 (line counts correct as of this date — they will drift). **Web decomposition inventory** (§1.1 / §1.3 campaign & combat UI) extended 2026-04-03.
> **Status:** **Waves 1–4 (Phase 20 technical polish) are complete** — see [Section 8](#section-8--plan-closure-phase-20-vs-backlog). **Optional follow-ups** are listed in [Optional backlog](#optional-backlog), not in §7. See `docs/mission.md` Phase 20 section. **Product note:** i18n and a public REST API are **not** on the near-term roadmap there.
> **Wave 1 (2026-04-02):** §3.1 `DbInitializer` N+1 / per-item saves addressed; §4.5 production `Console.WriteLine` removed in Web (`SessionClientService`, `CharacterDetails.OpenReference` → `ILogger`). §3.6: `HasIndex` for `Ghoul.RegnantCharacterId` / `RegnantNpcId` and `BloodBond.RegnantCharacterId` / `RegnantNpcId` added to fluent configuration — these indexes already appear in `ApplicationDbContextModelSnapshot` (no new migration).
> **Wave 2 (2026-04-03):** `ISeeder` pipeline in `RequiemNexus.Data/Seeding/` — `DbInitializer` orchestrates migrations + Identity roles + ordered seeders; `AddRequiemDataSeeders()` registers implementations. **`IReferenceDataCache`** (§3.3): `ReferenceDataCache` + `ReferenceDataCacheWarmupHostedService` in Web; contract in `Application.Contracts`; catalog consumers (`ClanService`, `DisciplineService`, `MeritService`, `CoilService`, `SorceryService`, `CovenantService`, `BloodlineService`, `DevotionService`, `CharacterMeritService`, `CharacterDisciplineService`, `CharacterManagementService`, `HumanityService`, `GhoulManagementService`) use the cache for official rows. **Full-pipeline seed test:** `DbInitializerTests.InitializeAsync_FullPipeline_PopulatesCoreCatalogTables` asserts non-zero counts for core seeded tables after `InitializeAsync`.
> **Wave 3 (2026-04-03):** **`ICharacterQueryService`** / **`CharacterQueryService`** (§1.2 / backlog #8); **§3.2 (partial)** — Beats/XP reload skip + `CharacterUpdateDto` progression fields + hub echo suppression. **`IModifierProvider`** (§2.2 / backlog #9) — `ConditionModifierProvider`, `CoilModifierProvider`, `WoundTrackModifierProvider`, `EquipmentModifierProvider`; `ModifierService` aggregates by `Order`; shared `PassiveModifierJsonSerializerOptions`; tests: `ModifierProviderTests` + updated `ModifierServiceTests`. **`IRiteActivationStrategy`** (§2.2 / backlog #10) — `CruacActivationStrategy`, `ThebanActivationStrategy`, `NecromancyActivationStrategy`; `SorceryActivationService` resolves by tradition; initial character load uses `Include(Rites).ThenInclude(SorceryRiteDefinition)` (removes fallback `CharacterRites` query).
> **Wave 4 (2026-04-03):** §4.1 — `SkeletonLoader` variants **`tracker`** / **`encounter-list`** / **`panel`** (compact embeds) in `SkeletonLoader.razor.css`; campaign hub pages + **character sheet sections** (Blood Bonds, Lineage, Ghouls, Predatory Aura, Social Maneuvers), **Glimpse** panels (`BloodBondsPanel`, `GhoulsTab`, `PredatoryAuraHistoryPanel`), **NPC/Faction detail**, **CampaignCharacterView** / **Advancement**, **join** page, modals (`EditLineage`, `GhoulDisciplineAccessEditor`, `PlayerWeaponDamageRollModal`), **Account/Manage** pages, and **`LoadingContainer`** use `<SkeletonLoader>` instead of italic loading paragraphs. §4.2 **(partial):** `CampaignDetails` / `CampaignJoin` / invite modal — service failures → **`ToastService.Show(..., ToastType.Error)`**; removed danger-zone `feedback-error` blocks and join `alert-rn` for caught exceptions; **`PlayerInviteModal`** no longer takes `InviteError`. **`EncounterManager`** — exception paths → toast; dropped **`_renameEncounterError`** / **`_smartLaunchConfirmError`**; **`_createError`** / **`_chronicleAddError`** / **`_improvError`** retain **inline validation only**. Remaining **`alert-rn`** on hub pages are **inline validation / empty-state** (e.g. missing campaign id, load failure message binding); auth pages keep field-adjacent alerts per §4.2. §4.3 **(2026-04-03):** `CharacterDetails` — unified **`_pendingModal`** + **`IsOpeningModal`**, `InvokeAsync(StateHasChanged)` before awaits, **`.btn-inline-spinner`** in **`CharacterDetails.razor.css`**; buttons: Apply Bloodline/Covenant, Learn Rite, Chosen Mystery, Purchase Coil. **`CharacterAdvancement`** — same for **Request rite** + **`CharacterAdvancement.razor.css`**. §1.1 **`CharacterDetails.razor.cs` decomposition (2026-04-03):** feature partials under `Components/Pages/` — **`CharacterDetails.State`**, **`.Session`**, **`.Export`**, **`.DiceRoller`**, **`.Rites`**, **`.Assets`**, **`.DisciplinePools`**, **`.Progression`**, **`.Modals.Faction`**, **`.Modals.Sorcery`**; injects consolidated in **`CharacterDetails.razor`** (removed unused **`IAdvancementService`**). **Post–Wave 4 (P3):** §4.2 on **`CharacterAdvancement`** — **done (2026-04-03)** (toast + inline validation; see §1.3 `CharacterAdvancement`). **`EncounterManager` markup (2026-04-03):** child components under **`Campaigns/EncounterParts/`** — see §1.1 / §1.3. **2026-04-04–05 sweep:** **`InitiativeTracker`** (#19), **`DiceRollerModal`** (#17), **`PageTitle`** / **`alert-rn`** (#13/#14), vitals, **`CampaignDetails`**, **`GlimpseSocialManeuvers`**, **`StorytellerGlimpse`** — **delivered**; any further extractions are **O-rows** in [Optional backlog](#optional-backlog), not §7.
> **Review:** See `docs/plan-improvement-review.md` for historical questions, answers, and rationale.
> **Closure (2026-04-04):** **Waves 1–4** are the committed Phase 20 technical-polish scope for this document — **complete.** §7 is **closed**. **Backlog implementation sweep (2026-04-04):** `CharacterProgressionSnapshotDto` + sheet patch after Beats/XP mutations (**#5**); `EncounterManager.NpcPicker` / `EncounterManager.SmartLaunch` partials (**#20**); `MeleeAttackResolveModal` + `Account/Manage` skeletons (**#13**). Post–§7 work: [Optional backlog](#optional-backlog).

---

## Priority Legend

| Level | Meaning |
|---|---|
| **P1 — Critical** | Correctness/performance risk or severe maintainability debt — fix before adding new features |
| **P2 — High** | Architectural violation or notable UX inconsistency — *historically* the Phase 20 technical-polish sprint; waves are complete — further P2-style work is **O-rows** in [Optional backlog](#optional-backlog) unless `mission.md` reopens scope |
| **P3 — Medium** | Code hygiene, minor perf wins, UX polish — post-Phase 20 |
| **P4 — Low** | Nice-to-have, very low risk |

---

## Status Summary

### ✅ Completed (Waves 1–4 + post-Wave sweep, as of 2026-04-04)

| # | Item |
|---|---|
| 1 | `CharacterDetails.razor.cs` decomposed into feature partials |
| 2–3 | `DbInitializer` N+1 fixes (`CoilSeeder`, `EquipmentSeeder`, `DisciplineAcquisitionMetadataSeeder`) |
| 4 | `IReferenceDataCache` singleton + startup warmup |
| 5 | Beats/XP reload skip — `CharacterProgressionSnapshotDto` |
| 6 | `ISeeder` pipeline in `Data/Seeding/` |
| 7 | `CharacterExportService` split into `ICharacterJsonExportService` + `ICharacterPdfExportService` |
| 8 | `CharacterQueryService` extracted from `CharacterManagementService` |
| 9 | `IModifierProvider` — 4 providers + `ModifierService` aggregator |
| 10 | `IRiteActivationStrategy` — 3 tradition strategies + `SorceryActivationService` |
| 11 | Coil eligibility filter superseded by `IReferenceDataCache` |
| 12 | `CampaignService.GetCampaignByIdAsync` round-trips reduced |
| 13 | Loading states: `<SkeletonLoader>` / `<LoadingContainer>` across all major pages |
| 14 | Error surfaces: `ToastService` + inline validation across hub/campaign/advancement flows |
| 15 | Modal loading feedback (`_pendingModal` + spinners) in `CharacterDetails` + `CharacterAdvancement` |
| 16 | `Console.WriteLine` → `ILogger` across all `src/` production code |
| 17 | `RiteExtendedRollPanel` + **`DiceRollerStandardPanel`** extracted from `DiceRollerModal` (2026-04-04) |
| 18 | `CharacterAdvancement` section components extracted (`CharacterSheet/`) |
| 19 | `InitiativeTracker` — **`SignalR`** (hub subscriptions) + `EncounterLoad` + feature `.razor.cs` partials (`State`, `AddParticipant`, `Announcements`, `Tilts`, `NpcCombat`, `EncounterFlow`, `Modals`, `Display`) + `IInitiativeTrackerDragState`, **`InitiativeParts/`** (2026-04-04); cosmetic rename `Session` → **`SignalR`** partial filename (2026-04-04) |
| 20 | `EncounterManager` — `EncounterParts/` UI + `NpcPicker` + `SmartLaunch` code-behind partials |
| 21 | `ICharacterReader` / `ICharacterWriter` split on `ICharacterService` |
| 22 | `TraitResolver` — switch replaced with `FrozenDictionary` dispatch |
| 23 | `ISessionEventBus` subscription token pattern on `SessionClientService` |
| 24 | `Task.Run` for PDF + JSON export |
| 25 | `<HealthTracker>`, `<WillpowerTracker>`, `<VitaeTracker>` extracted from `CharacterVitals.razor` |
| 26–27 | `Ghoul` + `BloodBond` `HasIndex` in fluent config |
| 29 | `PredatoryAuraService` double character lookup merged to single query |
| 30 | `CampaignDetails` decomposed into `CampaignDetailsParts/` + feature partials |
| 31 | `DanseMacabre` tab panels (`DanseMacabreTabs/`) + `DanseMacabre.razor.cs` |
| 32 | `GlimpseSocialManeuvers` decomposed into `GlimpseSocialManeuverParts/` |
| 33 | `StorytellerGlimpse` overview wrapper + CSS split (`StorytellerGlimpseOverview.razor.css`) |

### ➡️ Next step (post–§7)

<a id="agent-next-step"></a>

**Committed §7 / Wave work:** **exhausted** — do not add new §7 rows for items already covered by #17 / #19 / #20.

**Further technical polish:** use **[Optional backlog](#optional-backlog)** (O-1 … O-12). Each O-row needs explicit mission or task scope.

**UX conventions (already delivered, still apply):** **`PageTitle`:** `{Screen} — Requiem Nexus`** (em dash); **`alert` + `alert-rn`** for page-level alerts outside hub empty-states (hub empty-state **`alert-rn`** remains intentional per §4.2).

---

## Section 1 — Large Files

> **Execution scope:** Optional follow-up work is enumerated in [Optional backlog](#optional-backlog). Sections 1–6 are narrative reference (Grimoire); do not treat them as an implied task list.

Files over ~300 lines are a maintenance red flag. The list below excludes the `Migrations/` folder. Baseline line counts as of 2026-04-02; additional Web paths in §1.3 use counts as of 2026-04-03.

### 1.1 God Components in `Web/`

#### `CharacterDetails.razor.cs` — ~1,430 lines (P1) — **decomposed (2026-04-03)**

**Problem:** A single partial class that owns SignalR hub lifecycle, full character state reload, 20 modal open/close flags, dice-roller state, PDF/JSON export, covenant apply/leave, beat/XP mutation, asset procurement, tab navigation, and cookie SSR/interactive bridging. It injects 20 services.

**Fix:** Split along feature domains into focused partial classes and/or child components:

| Responsibility | Extract To |
|---|---|
| Shared field state | `CharacterDetails.State.razor.cs` |
| Modal state + triggers (bloodline/covenant; sorcery/coil) | `CharacterDetails.Modals.Faction.razor.cs`, `CharacterDetails.Modals.Sorcery.razor.cs` |
| Dice roller state + feed publication | `CharacterDetails.DiceRoller.razor.cs` |
| Export (PDF + JSON) | `CharacterDetails.Export.razor.cs` |
| Asset procurement + inventory | `CharacterDetails.Assets.razor.cs` |
| SignalR hub + reload orchestration | `CharacterDetails.Session.razor.cs` |
| Discipline power pool resolution | `CharacterDetails.DisciplinePools.razor.cs` |
| Beats / XP | `CharacterDetails.Progression.razor.cs` |
| Rite activation + extended roller context | `CharacterDetails.Rites.razor.cs` |
| Inline "Pack" tab markup | New `CharacterPackTab.razor` component *(still optional — `CharacterDetailsPackTab` exists)* |

**Delivered (2026-04-03):** Partials co-located with `CharacterDetails.razor`; **all `@inject` on the `.razor` file** (avoids duplicating the “≤5 injects per partial” constraint across many `*.razor.cs` files). **Exit criteria met:** each `CharacterDetails*.razor.cs` partial is **≤ ~300 lines**. Trait **OpenReference** uses an **info toast** (book pointer); an in-sheet rules browser remains optional — see backlog **#16**.

**Tests:** Existing component-level tests must pass unchanged. New partial boundaries require no new tests unless logic is extracted to services.

#### `InitiativeTracker.razor.cs` + `InitiativeTracker.razor` + `InitiativeTracker.razor.css` — ~793 + ~440 + scoped CSS (P2 concerns **addressed**; **P3** optional markup splits)

**Problem (historical):** One class owned drag-and-drop, tilt/combat modals, and hub subscriptions; load concurrency was previously guarded with a semaphore.

**Delivered (#19):** `InitiativeTracker.SignalR.razor.cs` — hub subscription registration + `InitiativeUpdated` / `CharacterUpdated` handlers (file renamed from `*.Session.razor.cs` for discoverability, 2026-04-04). `InitiativeTracker.EncounterLoad.razor.cs` — `_loadEncounterCts` for cancelable encounter loads. `IInitiativeTrackerDragState` — scoped drag item for reorder. **Loading:** `<SkeletonLoader Variant="tracker" />` per §4.1.

**Delivered (P3, 2026-04-04):** Child components under `Web/Components/Pages/Campaigns/InitiativeParts/` — add-participant card, turn toolbar, order list + per-row UI (tilts + ST combat actions + NPC roll panel host), blood-log sidebar; initiative row/list CSS in `InitiativeOrderList.razor.css`.

**See also:** §1.3 (`InitiativeTracker`) for a Razor-focused summary; backlog **#19**.

#### `EncounterManager.razor.cs` — ~706 lines + `EncounterManager.razor` ~215 lines (P2) — **partial markup split (2026-04-03)**

**Problem (historical):** Encounter create/edit form, chronicle NPC / improv NPC flows, participant management, and smart launch lived in one large `.razor` with a very large code-behind. Earlier sprawl included many separate error string fields (since reduced; see §4.2).

**Fix:** Extract focused child components; hybrid errors per Section 4.2. Optional later: `EncounterParticipantPanel`-style extraction and/or `EncounterManager.*` partial classes if the code-behind remains above a ~300-line maintenance target.

**Delivered (2026-04-03):** UI under `Web/Components/Pages/Campaigns/EncounterParts/`: `EncounterCreateFormPanel`, `EncounterFromTemplatePanel`, `EncounterSmartLaunchPanel`, `EncounterDraftPrepCard`, `EncounterActiveEncounterCard`, `EncounterPausedEncounterCard`, `EncounterPastEncounterCard`, `EncounterNpcPickerModal`, `EncounterNpcTemplateCard`. Parent keeps orchestration, services, and hub wiring.

---

### 1.2 Large Service Files in `Application/`

#### `DbInitializer.cs` — **decomposed (2026-04-02)** — orchestrator ~50 lines (P1 — see also Section 3.1)

**Problem (historical):** A static class with 20+ private seed methods and N+1 patterns in several seed paths.

**Fix (delivered):** `ISeeder` in the **Data** project (not Application or Web — seeders must not reference Web):
```csharp
// RequiemNexus.Data/Seeding/ISeeder.cs
public interface ISeeder
{
    int Order { get; }      // controls execution sequence
    Task SeedAsync(ApplicationDbContext context, ILogger logger);
}
```
Create one class per concern under `RequiemNexus.Data/Seeding/`:
- `ClanSeeder`, `DisciplineSeeder`, `HuntingPoolSeeder`, `MeritSeeder`
- `EquipmentSeeder`, `AssetCapabilitySeeder`
- `CovenantSeeder`, `BloodlineSeeder`
- `SorceryRiteSeeder`, `CoilSeeder`, `DevotionSeeder`
- `PrebuiltNpcSeeder`
- `DisciplineAcquisitionMetadataSeeder`

`DbInitializer` is a short orchestrator that discovers `ISeeder` implementations and calls them in `Order` sequence. Each seeder receives `ILogger` and logs via structured logging — no `Console.WriteLine` in production seed paths.

**Exit criteria (met for orchestrator):** `DbInitializer.cs` is a thin coordinator (on the order of tens of lines). Individual seeders own one concern each; very large seed JSON may still produce a seeder file above ~200 lines — split further only when maintainability demands it.

**Tests:** `DbInitializerTests.InitializeAsync_FullPipeline_PopulatesCoreCatalogTables` (and related) assert the pipeline populates core catalog tables.

#### `CharacterExportService.cs` — ~379 lines (P2) — **delivered**

**Problem (historical):** One class owned JSON and PDF export.

**Delivered:** `ICharacterJsonExportService` / `CharacterJsonExportService`, `ICharacterPdfExportService` / `CharacterPdfExportService`, with a thin facade `CharacterExportService` — see §7 **#7**.

#### `CharacterManagementService.cs` — ~407 lines, 8 constructor parameters (P2) — **partial**

**Problem:** Handles character CRUD, Beat/XP adjustment, embargo enforcement, reload-after-mutation, campaign kindred listing for rites, and deep-include graph loading.

**Delivered:** `ICharacterQueryService` / `CharacterQueryService` for reads — see §7 **#8**. Beats/XP reload reduction — §3.2 / §7 **#5**.

**Optional later:** Extract `ICharacterProgressionService` (Beat/XP/embargo only) if the facade still feels too broad; not a committed Wave item.

#### `CampaignService.cs` — ~496 lines (P2) — **round-trips improved**

**Problem (historical):** Multiple concerns in one service; extra `AnyAsync` round-trips on load.

**Delivered:** `GetCampaignByIdAsync` membership folded into main query — §7 **#12**. Lore + session prep split remains **optional** product refactor (no §7 row).

---

### 1.3 Large Razor Views

#### `DiceRollerModal.razor` — ~702 lines (P3) — **partial (#17)**

**Problem (historical):** Extended-rite logic lived entirely in the modal.

**Delivered:** `RiteExtendedRollPanel.razor` + `DiceRollerModal.razor.cs` — §7 **#17**. **Second child (2026-04-04):** `DiceRollerStandardPanel.razor` + `.razor.css` (base pool, associated trait, modifiers, total/chance hint, again/rote); modal body keeps rite failure choice, roll actions, results; `RiteExtendedRollPanel.razor.css` holds shared hint/checkbox styles used by the extended summary.

#### `CharacterAdvancement.razor` — ~680 lines (P3) — **delivered (#18)**

**Delivered (2026-04-03):** Section components under `Web/Components/Pages/CharacterSheet/` (`AttributesAdvancementSection`, `SkillsAdvancementSection`, `DisciplinesAdvancementListSection`, `MeritsAdvancementSection`, `NewDisciplineAdvancementSection`, `DevotionsAdvancementSection`, `BloodSorceryAdvancementSection`). Page keeps orchestration and `@code`. §4.2: **`ToastService`** + inline `.advancement-validation-message` — §7 **#18**, **#14** (partial).

#### `CampaignDetails.razor` + `CampaignDetails.razor.cs` — ~377 + ~592 lines (P3) — **delivered (#30)**

**Problem (historical):** Monolithic campaign hub page.

**Delivered (2026-04-04):** `CampaignDetailsParts/` + partials `CampaignDetails.Session` / `.Roster` / `.LorePrep` — §7 **#30**. Further optional splits only if line counts grow again.

#### `DanseMacabre.razor` — ~308 lines (P3) — **delivered (#31)**

**Delivered:** `DanseMacabreTabs/*` + `DanseMacabre.razor.cs` — §7 **#31**. Folder name **`DanseMacabreTabs`** avoids a `DanseMacabre/` sibling that would collide with the page type name.

#### `EncounterManager.razor` + `EncounterManager.razor.cs` — ~215 + ~706 lines (P2 / P3) — **delivered (#20)**

**Delivered:** `EncounterParts/` UI + `EncounterManager.NpcPicker.razor.cs` + `EncounterManager.SmartLaunch.razor.cs` — §7 **#20**. **Optional P3:** participant-heavy regions in the parent if still large after line-count review. “Loading encounters…” uses `<SkeletonLoader>` per §4.1.

#### `GlimpseSocialManeuvers.razor` — ~449 lines (P3) — **delivered (#32)**

**Delivered:** `GlimpseSocialManeuverParts/*` — §7 **#32**. **Constraint for future edits:** preserve parameter surface for `StorytellerGlimpse` (`Vitals`, `Npcs`, `Maneuvers`, campaign id, etc.).

#### `InitiativeTracker` — see §1.1 (P2 + P3)

**Summary:** Combined ~1,233 lines (`.razor` + `.razor.cs`) plus `InitiativeTracker.razor.css`. Architectural work (SignalR partial, `DragDropService`, `CancellationTokenSource` vs. semaphore) lives in **§1.1**. Markup/CSS/loading refinements are **§1.3 / Wave 4**; single backlog item **#19** (no duplicate row).

#### `StorytellerGlimpse.razor` + `.razor.cs` + `.razor.css` — ~420 + ~538 + ~473 lines (P3) — **partial / delivered (#33)**

**Delivered (2026-04-04):** `StorytellerGlimpseOverview` wrapper + scoped overview CSS + social/pending panel CSS splits — §7 **#33**. Overview tab may still be large; further child extractions (**optional P3**) follow the same pattern as Appendix A.

---

## Section 2 — SOLID Principles

### 2.1 Single Responsibility Principle

*Rows below describe **historical** violations; delivery status is in §1.2 and §7.*

| Violation (historical) | File | Resolution |
|---|---|---|
| God component (20 services, …) | `CharacterDetails.razor.cs` | **Done** — §1.1 partials |
| God static seeder | `DbInitializer.cs` | **Done** — `ISeeder` pipeline §1.2 |
| JSON + PDF export in one class | `CharacterExportService.cs` | **Done** — split services §1.2 / §7 **#7** |
| Character CRUD + Beat + reload + … | `CharacterManagementService.cs` | **Partial** — `CharacterQueryService` + snapshot path §1.2 / §7 **#5**, **#8** |

### 2.2 Open/Closed Principle

#### `ModifierService.cs` — ~375 lines (P2) — **delivered 2026-04-03**

**Problem:** Aggregates modifiers from Conditions, Coils, Devotions, Covenant benefits, and equipment using `if` chains on `ModifierSourceType`. Adding a new source requires editing this class.

**Fix (as implemented):** `IModifierProvider` with `Order`, `SourceType`, and `GetModifiersAsync(int characterId, CancellationToken)` returning `PassiveModifier` (current product shape; not `Character` entity). Providers: `ConditionModifierProvider`, `CoilModifierProvider`, `WoundTrackModifierProvider`, `EquipmentModifierProvider`. `ModifierService` aggregates ordered providers. *(Devotion / Covenant / Bloodline / Merit sources remain future providers if rules add DB-backed passive modifiers for them.)*

**Tests:** `ModifierProviderTests` (per provider) + existing `ModifierServiceTests` through the composed `ModifierService`.

#### `TraitResolver.cs` — ~326 lines (P3) — **delivered**

**Problem (historical):** Large `switch` on pool traits.

**Delivered:** `FrozenDictionary` dispatch — §7 **#22**. **Fix (pattern):** Dictionary / frozen map of resolvers built once in the constructor:
```csharp
private readonly Dictionary<PoolTraitType, Func<Character, int>> _resolvers = new()
{
    [PoolTraitType.Strength] = c => c.Attributes.Strength,
    // ...
};
```
**Constraint:** This applies only to traits that are synchronous `Character → int` lookups. If any trait resolution requires services, campaign context, or conditions — confirm before locking the design. Those cases remain as explicit branches or a separate `IContextualTraitResolver`.

#### `SorceryActivationService.cs` — ~349 lines (P2) — **delivered 2026-04-03**

**Problem:** Three sorcery traditions (Crúac, Theban, Necromancy) are handled via per-tradition `if` branches inside shared methods. The `BeginRiteActivationAsync` method does a fallback second query when the rite is not in the navigation collection.

**Fix (as implemented):** `IRiteActivationStrategy` with `CruacActivationStrategy`, `ThebanActivationStrategy`, `NecromancyActivationStrategy`; `SorceryActivationService` builds a dictionary by `Tradition` from injected strategies. Initial load includes `ThenInclude` on rite definitions so the extra `CharacterRites` query is removed.

### 2.3 Interface Segregation Principle

#### `ICharacterService` covers reads and writes (P3) — **delivered**

**Delivered:** `ICharacterReader` / `ICharacterWriter` — §7 **#21**. **Historical split plan:**
Components that only need to read character state still depend on the full `ICharacterService` interface. Split:
- `ICharacterReader` — `GetCharacterByIdAsync`, `GetAllCharactersAsync`
- `ICharacterWriter` — `CreateCharacterAsync`, `UpdateCharacterAsync`, `DeleteCharacterAsync`

Razor components and read-only services depend only on `ICharacterReader`.

### 2.4 Dependency Inversion Principle

#### `SessionClientService` exposes 12 raw `event Action<T>` fields (P3) — **delivered**

**Problem (historical):** Many raw events; easy to leak subscriptions.

**Delivered:** `ISessionEventBus` + `Subscribe*` returning `IDisposable` — §7 **#23**. **Fix (pattern) — zero new NuGet packages:** Lightweight subscription token using only BCL types. No `System.Reactive` or third-party Rx library:
```csharp
public interface ISessionEventBus
{
    IDisposable SubscribeDiceRoll(Action<DiceRollBroadcastEvent> handler);
    IDisposable SubscribeInitiativeChanged(Action<InitiativeChangedEvent> handler);
    // ...
}
```
Internally backed by `ConcurrentDictionary<Guid, Action<T>>`. Components store the returned `IDisposable` in a field and call `Dispose()` once in `DisposeAsync` — a single cleanup call instead of 12 `-=` lines. No new packages required.

> **Blazor Server sync context:** When implementing, ensure that hub callbacks marshal to Blazor's sync context before invoking UI subscribers (i.e., call `InvokeAsync(() => handler(evt))` rather than `handler(evt)` directly). Invoking a subscriber from a background SignalR thread without marshaling causes `InvalidOperationException` on `StateHasChanged`. This is an implementer responsibility — the interface contract does not enforce it.

---

## Section 3 — Performance

### 3.1 Eliminate N+1 Queries in `DbInitializer` — **delivered in seeders (2026-04-02)**

P1 fixes below are implemented in `RequiemNexus.Data/Seeding/` (e.g. `DisciplineAcquisitionMetadataSeeder`, `CoilSeeder`, `EquipmentSeeder.SeedDeferredAssetCapabilitiesAsync`), not in a monolithic `DbInitializer` body.

#### `UpdateDisciplineAcquisitionMetadataAsync` / acquisition metadata pass (P1) — **done**

**Problem:** For each entry in the JSON array, a separate `await context.Disciplines.FirstOrDefaultAsync(...)` is issued. With N disciplines this is N round-trips.

**Fix:**
```csharp
// Before: N queries
foreach (var entry in metadata)
{
    var discipline = await context.Disciplines.FirstOrDefaultAsync(d => d.Slug == entry.Slug);
    ...
}

// After: 1 query
var disciplineMap = await context.Disciplines
    .ToDictionaryAsync(d => d.Slug);
foreach (var entry in metadata)
{
    if (disciplineMap.TryGetValue(entry.Slug, out var discipline))
    { ... }
}
```

**Exit criteria:** Full seed run on a cold database completes with a fixed number of DB round-trips, verifiable via EF Core logging.

#### `SeedCoilsAsync` — `SaveChangesAsync` inside a loop (P1) — **done** (`CoilSeeder`)

**Problem:** One DB round-trip per coil during initial seed.

**Fix:** Replace per-item saves with:
```csharp
await context.CoilDefinitions.AddRangeAsync(coilsToAdd);
await context.SaveChangesAsync();
```

#### `SeedDeferredAssetCapabilitiesAsync` — `AnyAsync` inside a loop (P1) — **done** (`EquipmentSeeder`)

**Problem:** One existence-check query per capability.

**Fix:** Pre-load existing capability IDs into a `HashSet<int>`, then filter in memory before a single `AddRangeAsync`.

### 3.2 Reduce Character Reload Cost (P1) — **partial (delivered path)**

**Problem (historical):** Full character graph reload after many mutations.

**Delivered (Wave 3 / 2026-04-04):** `CharacterQueryService` boundary; Beats/XP mutations return **`CharacterProgressionSnapshotDto`** where applicable; sheet patches state + hub echo handling — §7 **#5**, **#8**. Full `ICharacterPatchEvent` for every mutation type and strict “≤2 queries on Beat-add” exit criteria were **not** all implemented; treat further reload reduction as **optional** unless mission reopens it.

### 3.3 Add Reference-Data Caching (P1) — **delivered**

**Problem (historical):** Reference definitions re-queried on many page loads.

**Delivered:** `IReferenceDataCache` + startup warmup — §7 **#4**. The interface shape in the snippet below is illustrative; use the in-repo contract as source of truth.

**Fix (as implemented):** Register a `IReferenceDataCache` singleton:
```csharp
public interface IReferenceDataCache
{
    IReadOnlyList<ClanDefinition> Clans { get; }
    IReadOnlyList<DisciplineDefinition> Disciplines { get; }
    IReadOnlyList<MeritDefinition> Merits { get; }
    IReadOnlyList<CovenantDefinition> Covenants { get; }
    IReadOnlyList<SorceryRiteDefinition> Rites { get; }
    IReadOnlyList<CoilDefinition> Coils { get; }
}
```
Populated once by a startup `IHostedService` before the first request. Services that currently query these tables use the cache instead.

**Cache invalidation policy:** The cache is application-lifetime only (restart to refresh). No runtime flush API is needed in Phase 20. If admin editing of definitions is added in a future phase, a `FlushAsync()` method must be added to the interface at that time and called after any definition mutation. This assumption must be documented in the admin feature's implementation plan.

**Exit criteria:** Reference-data reads: 0 DB round-trips on a warm application instance, verified by logging query counts on a known page load.

### 3.4 Push Eligibility Filters Into the Database (P2)

#### `CoilService.GetEligibleCoilsAsync` — **superseded (in product)**

**Problem (historical):** Per-request coil table load + in-memory filter.

**As shipped:** Eligibility uses **`IReferenceDataCache.CoilDefinitions`** plus Ordo/prerequisite rules in memory — §7 **#11**. The EF `Where` snippet below is **reference only**, not the current implementation target.

**Fix (reference pattern):**
```csharp
var existingSlugs = character.Coils.Select(c => c.CoilDefinitionSlug).ToHashSet();
return await _dbContext.CoilDefinitions
    .Where(c => c.MinBloodPotency <= character.BloodPotency
             && !existingSlugs.Contains(c.Slug))
    .Include(c => c.Scale)
    .AsNoTracking()
    .ToListAsync();
```

#### `CampaignService.GetCampaignByIdAsync` — **delivered (round-trip reduction)**

**Problem (historical):** Separate `AnyAsync` authorization checks plus main load.

**Delivered:** Membership folded into the main load path — §7 **#12**. **Fix (concept):** Fetch a minimal campaign DTO in a single query that includes the caller's membership status. Perform the authorization check in memory on the returned DTO. If unauthorized, return `null` / throw `NotFoundException` — **do not return sensitive campaign fields to non-members**. This must follow the standard Masquerade 4-step `AuthorizationHelper` sequence even though the check is now in-memory; the authorization helper must still be invoked to maintain audit compliance. This refactor is purely a round-trip optimization — authorization semantics are unchanged.

> **HTTP status mapping:** Whether the unauthorized path surfaces as HTTP 404 (to avoid leaking campaign existence) or HTTP 403 (explicit denial) is an implementation-time decision. Document the chosen mapping in the Application layer when implementing — the plan's invariant is authorization-first, not status-code-prescriptive.

### 3.5 Move CPU-Bound Work Off the Render Thread (P3) — **delivered (#24)**

**Problem (historical):** PDF/JSON export on the render thread.

**Delivered:** `Task.Run` in `CharacterPdfExportService` / `CharacterJsonExportService` — §7 **#24**.

**Pattern (reference):**
```csharp
public async Task<byte[]> ExportCharacterAsPdfAsync(Character character)
{
    return await Task.Run(() => BuildPdfDocument(character));
}
```
Similarly, `JsonSerializer.Serialize` for large character graphs should be called inside `Task.Run`.

**Concurrency warning:** Unbounded `Task.Run` under load can starve the Blazor Server thread pool. For Phase 20 (low concurrent user count), bare `Task.Run` is acceptable. If concurrent export load increases in the future, replace with a **bounded `Channel<T>`-based export queue** or a dedicated `IHostedService` worker. Do not use `Task.Run` as a permanent scaling solution.

### 3.6 Fix Missing Database Indexes (P3)

**Delivered / N/A:** `Ghoul` + `BloodBond` indexes — §7 **#26**, **#27**. **#28** — `Asset` uses **TPT**, not TPH; discriminator index row **N/A**.

For **new** indexes in future work: use **EF Core migration** — add `HasIndex` in fluent configuration, then `dotnet ef migrations add <name>` (no raw SQL).

| Table / Configuration | Index | Notes |
|---|---|---|
| `Ghoul` | `RegnantCharacterId`, `RegnantNpcId` | **Done** — `GhoulConfiguration` |
| `Asset` | TPH discriminator | **N/A** — TPT mapping (§7 **#28**) |
| `BloodBond` | Thrall + regnant keys | **Done** — `BloodBondConfiguration` |

---

## Section 4 — UI/UX Consistency

### 4.1 Standardize Loading States (P2)

**Problem:** At least five different loading patterns exist across pages:
- `<SkeletonLoader Variant="sheet" />` — Character details ✓
- `<SkeletonLoader Variant="card" Count="3" />` — Campaigns index ✓
- `<SkeletonLoader Variant="encounter-list" />` — Encounter manager ✓ (2026-04-03)
- `<SkeletonLoader Variant="tracker" />` — Initiative tracker ✓ (2026-04-03)
- Custom loading markup — Blood Bonds Panel, Ghouls Tab, character sheet embeds, NPC detail ✓ (`NpcDetail.razor` — `SkeletonLoader Variant="card"`), **Account/Manage** ✓ (`SkeletonLoader Variant="card"`, 2026-04-04), **`MeleeAttackResolveModal`** ✓ (`Variant="panel"`, 2026-04-04); **`PageTitle`** during skeletons improved for **view-only / faction / NPC** (2026-04-04)

**Fix:** Mandate `<SkeletonLoader>` or `<LoadingContainer>` for all loading states. Delete ad-hoc loading markup. Add skeleton variants for remaining page types (**tracker** ✓, **encounter-list** ✓, **NPC detail** ✓).

**Delivered (2026-04-03):** `CampaignDetails` (`card`), `StorytellerGlimpse` (`sheet`), `DanseMacabre` (`rows`); new CSS variants in `SkeletonLoader.razor.css`.

### 4.2 Standardize Error Display — Hybrid Policy (P2)

**Problem (historical):** Mixed patterns — raw page-level alerts vs toasts vs inline validation.

**Current shape (verify in code when editing):**
- **`CharacterDetails`** — `ToastService` for unexpected failures; field-adjacent / modal validation as designed.
- **`CampaignDetails`** — **no** `_errorMessage` field; **`ToastService`** in partials (e.g. invite, roster actions). A single **`alert-rn alert-danger`** remains for **empty-state only** when the campaign is missing or the user is not a member (static copy — not a caught-exception banner). Same class of **intentional hub `alert-rn`** (not an error-surface defect).
- **`EncounterManager`** — inline **`_createError`**, **`_chronicleAddError`**, **`_improvError`** (+ **`_prepFeedback`**); unexpected paths → **`ToastService`**.

**Policy — hybrid approach:**
- **`ToastService`** for global/unexpected errors (service failures, auth errors, unhandled exceptions)
- **Inline text** (small `<span class="text-danger">`) for form validation errors that must sit field-adjacent for accessibility and screen-reader compliance

**Fix:** Remove **unexpected-error** Bootstrap alert blocks (caught exceptions, generic `_errorMessage` pages). Use `ToastService.Show(..., ToastType.Error)` for those. **Retain** intentional **empty-state / validation** markup (`alert-rn` or inline `<span class="text-danger">`) where the UX must stay on-page. Retain (or introduce) a single `string? _validationError` per modal/form for inline validation only — do not proliferate to many separate error fields.

**Delivered (2026-04-03, partial):** `CampaignDetails` (leave/delete/remove/invite failures → toast); `CampaignJoin` (join API exceptions → toast); `PlayerInviteModal` (invite errors via toast from parent); `EncounterManager` (rename/smart-launch/service catches → toast; create/chronicle/improv keep inline messages only for validation). **`CharacterAdvancement` (2026-04-03):** `ToastService` for exceptions and post–ST-ack discipline failure; **inline** `.advancement-validation-message` for XP/spec/eligibility/gate-style text (no top `alert-danger`).

### 4.3 Add Intermediate Loading on Modal Triggers (P2)

**Problem:** Several modal trigger buttons in `CharacterDetails.razor.cs` (`OpenLearnRiteModal`, `OpenApplyBloodlineModal`, `OpenChosenMysteryModal`, `OpenLearnCoilModal`) start async service calls but show no visual feedback between click and modal open.

**Fix:** A single `string? _pendingModal` field tracks which modal is loading. The triggering button renders as disabled with a spinner while `_pendingModal == nameof(OpenLearnRiteModal)`. Cleared after the modal data is ready.

**Delivered (2026-04-03):** `CharacterDetails` — `OpenApplyBloodlineModal`, `OpenApplyCovenantModal`, `OpenLearnRiteModal`, `OpenChosenMysteryModal`, `OpenLearnCoilModal` set/clear `_pendingModal` in `try`/`finally`; markup uses `disabled`, `aria-busy`, and inline spinner + “Opening…” label. **`CharacterAdvancement`** — same pattern for **Request rite** (`CharacterAdvancement.razor.css` spinner).

### 4.4 Extract `CharacterVitals.razor` Tracker Sub-Components (P3) — **delivered (#25)**

**Delivered:** `<HealthTracker>`, `<WillpowerTracker>`, `<VitaeTracker>` composed from `CharacterVitals.razor` — §7 **#25**.

### 4.5 Audit and Clean Up `Console.WriteLine` (P2)

A full audit of production code (excluding test infrastructure, where `Console.WriteLine` is acceptable) reveals at minimum:

| Location | Fix |
|---|---|
| `CharacterDetails` — `OpenReference` | **Interim (2026-04-03+):** info `ToastService` with book pointer — optional future in-sheet rules browser remains product backlog, not a plan exit criterion |
| `SessionClientService.cs` — hub invocation error catch | `_logger.LogError(ex, "Hub invocation error: {Method}", methodName)` |
| `DbInitializer.cs` — seed diagnostics | Pass `ILogger` through each `ISeeder`; log via `_logger.LogInformation(...)` |

After addressing the known locations, run a solution-wide search to confirm no additional instances remain — excluding test infrastructure where `Console.WriteLine` is acceptable:
- **Unix / Git Bash:** `grep -r "Console.Write" src/ --include="*.cs"`
- **Windows (ripgrep):** `rg "Console\.Write" src/ -g "*.cs"` or use IDE "Find in Files" with the same pattern

Exclude any path matching `*.Tests.*` from both searches.

---

## Section 5 — Implementation Sequencing

The backlog items are not independent. The order below reduces rework and risk.

> **Naming note:** The waves below are *implementation delivery batches*, not product roadmap phases. Use "Wave 1–4" when referencing this sequencing to avoid confusion with `docs/mission.md` Phase 19/20/etc. numbering.

### Wave 1 — Low-risk, high-reward (do first)
1. **DbInitializer N+1 fixes** (3.1) — pure data layer, no API changes, immediately measurable — **done (2026-04-02)**
2. **Console.WriteLine → ILogger** (4.5) — pure logging swap, zero risk — **done (2026-04-02)** for production Web paths; test-only / `TestDbInitializer` unchanged per §4.5
3. **Missing DB indexes** (3.6) — **fluent `HasIndex` aligned with existing model snapshot** (2026-04-02); no migration generated because snapshot already matched the intended schema

### Wave 2 — Infrastructure (enables later items)
4. **`ISeeder` decomposition** (1.2 / DbInitializer) — prerequisite for clean seed-pipeline tests — **done (2026-04-02)** (`ISeeder`, `AddRequiemDataSeeders`, per-concern seeders under `Data/Seeding/`)
5. **`IReferenceDataCache`** (3.3) — **done (2026-04-03)** — singleton cache, startup warmup, Application services migrated; integration guard: `DbInitializerTests.InitializeAsync_FullPipeline_PopulatesCoreCatalogTables`

### Wave 3 — Service layer (depends on Wave 2 cache)
6. **`CharacterQueryService` extraction** (1.2 / CharacterManagementService) — unblocks patch event work — **done (2026-04-03)** (`ICharacterQueryService`, `CharacterQueryService`, `CharacterQueryableExtensions`, facade delegation on `CharacterManagementService`)
7. **Reduce `GetCharacterByIdAsync` reload** (3.2) — depends on step 6 — **partial (2026-04-03):** Beats/XP on sheet + hub echo suppression; DTO progression fields for real-time payloads
8. **`IModifierProvider`** (2.2) — depends on stable service boundaries from step 6 — **done (2026-04-03)** — four providers + thin `ModifierService`; DI registers each `IModifierProvider` + aggregator
9. **`IRiteActivationStrategy`** (2.2) — **done (2026-04-03)** — three tradition strategies + `SorceryActivationService` composition; `ThenInclude` on character rites for single-query load

### Wave 4 — UI/UX and large component splits (depends on Wave 3)
10. **Standardize loading + error surfaces** (4.1, 4.2) — foundation for component extraction — **§4.1 (2026-04-03)** as above; **§4.2 partial:** `CampaignDetails`, `CampaignJoin`, `PlayerInviteModal`, `EncounterManager` hybrid policy; hub **`alert-rn`** retained where inline/validation (see Wave 4 note in header)
11. **`CharacterDetails.razor.cs` decomposition** (1.1) — **done (2026-04-03)** — partials listed in header Wave 4 note; each `*.razor.cs` partial **≤ ~300 lines**; DI on **`CharacterDetails.razor`**
12. **Modal loading feedback** (4.3) — **done (2026-04-03)** for `CharacterDetails` listed openers + `CharacterAdvancement` “Request rite”
13. **Optional P3 extractions** (no committed wave): **`InitiativeTracker`** / **`DiceRollerModal`** — **delivered (2026-04-04)** — `InitiativeParts/` + `InitiativeTracker.*` partials; `DiceRollerStandardPanel`. **Done:** §4.4 vitals (**#25**), **`CampaignDetails`** (**#30**), **`GlimpseSocialManeuvers`** (**#32**), **`StorytellerGlimpse`** overview/CSS (**#33**), **`CharacterAdvancement`** sections + §4.2 (**#18**), **`DanseMacabre`** tabs + `.razor.cs` (**#31**), **`EncounterManager`** + NpcPicker/SmartLaunch partials (**#20**).

---

## Section 6 — Risk Register

| Risk | Mitigation |
|---|---|
| `IReferenceDataCache` serves stale data after a future admin definition edit | Cache invalidation policy documented in 3.3; `FlushAsync()` contract reserved for future admin feature |
| `Task.Run` for PDF/JSON export starves thread pool under load | Bounded export queue (`Channel<T>`) as a future upgrade path; acceptable for Phase 20 scale |
| Single-query campaign load leaks data to non-members | Masquerade 4-step `AuthorizationHelper` sequence still invoked in-memory; DTO shape is minimal (no sensitive fields before auth check) |
| `ISeeder` ordering bugs cause missing FK references at seed time | Each seeder's `Order` value is integration-tested; seed test asserts non-zero counts for all dependent tables |
| `CharacterDetails` decomposition breaks existing component tests | Decompose one partial at a time; run tests after each split before proceeding |

---

## Section 7 — Consolidated Priority Backlog

| # | Priority | Area | Item |
|---|---|---|---|
| 1 | P1 | Large File / SOLID | Decompose `CharacterDetails.razor.cs` into feature-scoped partials — **done (2026-04-03)** — see §1.1 `CharacterDetails` and Wave 4 header |
| 2 | P1 | Performance | `DbInitializer` / seeders — eliminate N+1 saves in coils + deferred capabilities — **done (2026-04-02)** — `CoilSeeder`, `EquipmentSeeder` |
| 3 | P1 | Performance | Discipline acquisition metadata pass — pre-load maps, no per-row `FirstOrDefault` — **done (2026-04-02)** — `DisciplineAcquisitionMetadataSeeder` |
| 4 | P1 | Performance | Add `IReferenceDataCache` singleton for static seed data — **done (Wave 2 / 2026-04-03)** |
| 5 | P1 | Performance | Reduce full reload on Beats/XP sheet mutations — **done (2026-04-04):** `CharacterProgressionSnapshotDto` from `AddBeatAsync` / `RemoveBeatAsync` / `AddXPAsync` / `RemoveXPAsync`; `CharacterDetails.Progression` applies snapshot + hub echo skip (existing) |
| 6 | P2 | SOLID / Large File | Decompose `DbInitializer.cs` into `ISeeder` implementations (Data project) — **done (2026-04-02)** — `AddRequiemDataSeeders`, `Data/Seeding/*` |
| 7 | P2 | SOLID | Split `CharacterExportService` into JSON and PDF export services — **done** — `ICharacterJsonExportService` / `ICharacterPdfExportService` + facade `CharacterExportService` |
| 8 | P2 | SOLID | Extract `CharacterQueryService` from `CharacterManagementService` — **done (Wave 3)** |
| 9 | P2 | SOLID | Introduce `IModifierProvider` per source type in `ModifierService` — **done (2026-04-03)** — Condition / Coil / WoundTrack / Equipment providers + aggregator |
| 10 | P2 | SOLID | Introduce `IRiteActivationStrategy` per tradition in `SorceryActivationService` — **done (2026-04-03)** |
| 11 | P2 | Performance | Push coil eligibility filter into EF query in `CoilService` — **superseded:** eligibility uses `IReferenceDataCache.CoilDefinitions` + Ordo/prerequisite rules in memory (no per-request coil table scan) |
| 12 | P2 | Performance | Reduce 3 round-trips in `CampaignService.GetCampaignByIdAsync` — **done:** membership folded into main query + user hydration query (see `CampaignService.GetCampaignByIdAsync` implementation) |
| 13 | P2 | UI/UX | Standardize loading states to `<SkeletonLoader>` / `<LoadingContainer>` — **done** — see §4.1; **`PageTitle`:** skeleton-time titles + **em-dash** normalization on static titles (2026-04-04–05) |
| 14 | P2 | UI/UX | Standardize error surfaces — **done:** `ToastService` + inline validation; **`.alert` + `.alert-rn`** in **`app-chrome*.css`**; **full `alert-rn`** on auth, Account/Manage, **`CharacterDetails`**, **`Characters`** (2026-04-05) |
| 15 | P2 | UI/UX | Add intermediate loading feedback on modal trigger buttons — **done (2026-04-03):** `CharacterDetails` + `CharacterAdvancement` (rite) `_pendingModal` + spinners |
| 16 | P2 | Logging | Audit and replace all `Console.WriteLine` in production code with `ILogger` — **done** for `src/` (remaining: `TestDbInitializer` only, acceptable per §4.5); `OpenReference` — **interim UX** via info toast (full rules panel optional backlog) |
| 17 | P3 | Large File | `DiceRollerModal` decomposition — **done** — `RiteExtendedRollPanel.razor` + **`DiceRollerStandardPanel.razor`** + `DiceRollerModal.razor.cs`; scoped CSS split across `DiceRollerStandardPanel.razor.css`, `RiteExtendedRollPanel.razor.css`, `DiceRollerModal.razor.css` |
| 18 | P3 | Large File | Extract section components from `CharacterAdvancement.razor` — **done (2026-04-03)** — see §1.3 `CharacterAdvancement` |
| 19 | P3 | Large File | Decompose `InitiativeTracker` — **done (2026-04-04):** **`SignalR`** (hub), `EncounterLoad`, **`State`**, **`AddParticipant`**, **`Announcements`**, **`Tilts`**, **`NpcCombat`**, **`EncounterFlow`**, **`Modals`**, **`Display`** `.razor.cs` partials + `InitiativeParts/` + `IInitiativeTrackerDragState` + skeleton |
| 20 | P3 | Large File | `EncounterManager` — **done (2026-04-03–04):** `EncounterParts/` UI + **`EncounterManager.NpcPicker.razor.cs`** + **`EncounterManager.SmartLaunch.razor.cs`**; main `.razor.cs` holds fields, load, create/template/rename flows |
| 21 | P3 | SOLID | Add `ICharacterReader` / `ICharacterWriter` split — **done** — `ICharacterService : ICharacterReader, ICharacterWriter` |
| 22 | P3 | SOLID | Replace trait resolution switch with dictionary dispatch — **done** — `TraitResolver` uses `FrozenDictionary<TraitType, Func<Character, TraitReference, int>>` for pool traits |
| 23 | P3 | SOLID | Introduce `ISessionEventBus` — **done** — `SessionClientService : ISessionEventBus` with `Subscribe*` + `IDisposable` tokens |
| 24 | P3 | Performance | Wrap QuestPDF + `JsonSerializer` in `Task.Run` — **done** — `CharacterPdfExportService` / `CharacterJsonExportService` |
| 25 | P3 | UI/UX | Extract `<HealthTracker>`, `<WillpowerTracker>`, `<VitaeTracker>` from `CharacterVitals.razor` — **done** — `CharacterSheet/CharacterVitals.razor` composes the three tracker components |
| 26 | P3 | Database | `HasIndex` for `Ghoul.RegnantCharacterId`, `Ghoul.RegnantNpcId` — **done** in `GhoulConfiguration` (verify snapshot / migration in repo matches environment) |
| 27 | P3 | Database | `BloodBond` thrall/regnant indexes — **done** in `BloodBondConfiguration` |
| 28 | P4 | Database | Discriminator index on `Asset` TPH — **N/A:** `Asset` root uses **TPT** (`AssetConfiguration`); no shared TPH discriminator column to index |
| 29 | P4 | Performance | Merge double character lookup in `PredatoryAuraService.ResolveLashOutAsync` — **done** — single query for attacker + defender pair |
| 30 | P3 | Large File | Decompose `CampaignDetails` (`.razor` + `.razor.cs`, ~970 lines combined) into child components and/or feature partials — session/roster, lore/prep, invite, ST perception (§1.3) — **done (2026-04-04):** `CampaignDetailsParts/` (`CampaignSessionHeader`, `CampaignRosterTabBar`, `CampaignPlayersSection`, `CampaignDangerZoneSection`, `CampaignLoreSection`, `CampaignSessionPrepSection`, `AddCharacterToCampaignModel`) + partials `CampaignDetails.Session` / `.Roster` / `.LorePrep` |
| 31 | P3 | Large File | `DanseMacabre` — **done** — `DanseMacabreTabs/*` + `DanseMacabre.razor.cs` orchestration |
| 32 | P3 | Large File | `GlimpseSocialManeuvers` — **done** — `GlimpseSocialManeuverParts/*` (`ThresholdPanel`, `NewForm`, `Card`, etc.) |
| 33 | P3 | Large File | Decompose `StorytellerGlimpse` overview + CSS — **done (2026-04-04):** `StorytellerGlimpseOverview` wrapper + `StorytellerGlimpseOverview.razor.css` (`::deep`); `GlimpseSocialManeuvers.razor.css` + `glimpse-social-root`; `GlimpsePendingRequests.razor.css`; page `.razor.css` keeps tab chrome only |

**Section 7 deduplication:** Do not add new rows for `DiceRollerModal` (**#17**), `EncounterManager` (**#20**), or a second `InitiativeTracker` row (**#19** covers `.razor` + CSS extensions).

---

## Optional backlog

<a id="optional-backlog"></a>

Work that was **never** part of the committed §7 table. Implement **one O-row at a time** when `docs/mission.md` or an explicit task authorizes it.

**Already delivered (do not reopen as O-work):** `InitiativeTracker` (#19), `DiceRollerModal` (#17), `PageTitle` / `alert-rn` sweep (#13/#14 residuals, 2026-04-05).

### Summary table

| ID | Priority | Area | Item |
|---|---|---|---|
| O-1 | P3 | Web / UX | In-sheet rules browser for trait **OpenReference** (replaces interim info toast) |
| O-2 | P3 | Application | **`ICharacterProgressionService`** — Beat/XP slice of **`CharacterManagementService`** — **delivered** (`CharacterProgressionService`, façade forwards on **`CharacterManagementService`**) |
| O-3 | P3 | Application | **`CampaignService`** — lore + session prep collaborators — **delivered** (`ICampaignLoreService` / **`CampaignLoreService`**, **`ICampaignSessionPrepService` / `CampaignSessionPrepService`**, **`CampaignService`** façade) |
| O-4 | P2–P3 | Application / SignalR | Broader character reload reduction — patch DTOs or **`ICharacterPatchEvent`** beyond Beats/XP snapshot |
| O-5 | P3 | Web | **`StorytellerGlimpse`** — further child components if overview tab grows |
| O-6 | P3 | Web | **`EncounterManager`** — participant-heavy extraction, optional **`EncounterManager.*`** partials, optional validation error consolidation |
| O-7 | P4 | Web | **`CampaignDetails`** — further splits if line counts exceed maintenance comfort again |
| O-8 | P4 | Domain / Application | New **`IModifierProvider`** implementations when rules add DB-backed passive modifiers (Devotion / Covenant / Bloodline / Merit) |
| O-9 | P4 | Infrastructure | Bounded PDF/JSON export queue (**`Channel<T>`** or worker) under high concurrent load |
| O-10 | P4 | Application | **`IReferenceDataCache.FlushAsync`** + callers when admin definition editing ships |
| O-11 | P3 | Domain | **`IContextualTraitResolver`** (or explicit branches) if a **`PoolTraitType`** needs async / service / campaign context |
| O-12 | P4 | Web | Align **`CharacterPackTab`** naming with shipped **`CharacterDetailsPackTab`** only if rename churn is justified |

### O-1 — In-sheet rules browser (`OpenReference`)

- **Goal:** Replace or supplement the interim **info toast** (book pointer) with searchable in-sheet rules content for the active trait.
- **Primary files:** `CharacterDetails` (OpenReference flow); new modal/panel components under `Components/` as designed; rules content source (seed/static) per product.
- **Dependencies:** Product decision on content scope and sourcing.
- **Exit criteria:** Reader can open, read, and dismiss rules without leaving the sheet; focus management and **`aria-modal`** consistent with other modals.
- **Tests:** Extend **Application** / **Web** tests if logic moves to services; **Domain** tests for any new parsing rules.

### O-2 — `ICharacterProgressionService` — **delivered**

- **Goal:** Narrow **`CharacterManagementService`** by extracting Beat/XP/embargo mutations and reload/snapshot coordination.
- **Primary files:** `CharacterManagementService.cs`, new contract + implementation in **Application**, **Web** DI and call sites.
- **Dependencies:** Stable **`CharacterProgressionSnapshotDto`** and hub echo behavior from §3.2 / #5.
- **Exit criteria:** Smaller façade; no regression on progression flows; Masquerade unchanged on mutations.
- **Tests:** **`RequiemNexus.Application.Tests`** (authorization, mutation contracts).

### O-3 — `CampaignService` lore / session prep split — **delivered**

- **Goal:** Separate campaign/session concerns from lore and session-prep note flows for readability and testing.
- **Primary files:** `CampaignService.cs`; optional new services (names illustrative: lore vs prep).
- **Dependencies:** None blocking.
- **Exit criteria:** Smaller types; same external behavior or documented API migration; **AuthorizationHelper** on every mutation.
- **Tests:** **`RequiemNexus.Data.Tests`** / **Application** tests for campaign APIs.

### O-4 — Broader reload reduction / patch events

- **Goal:** Extend the Beats/XP snapshot pattern to other high-churn mutations if profiling demands it; optional strict query budget (e.g. Beat-add).
- **Primary files:** `CharacterManagementService`, **`CharacterDetails`** SignalR handlers, new DTOs/events as needed.
- **Dependencies:** Mission buy-in; **`ICharacterQueryService`** load paths.
- **Exit criteria:** Measured fewer round-trips or explicit cancellation of the row in this doc.
- **Tests:** **Application** tests; performance notes if applicable.

### O-5 — `StorytellerGlimpse` further extractions

- **Goal:** Child components for remaining large overview regions (same pattern as **`StorytellerGlimpseOverview`**).
- **Primary files:** `StorytellerGlimpse.razor` (+ `.cs` / `.css`), new children under **`Components/Pages/`**.
- **Dependencies:** None.
- **Exit criteria:** Measurable reduction in overview markup or code-behind; scoped CSS with **`::deep`** where needed.
- **Tests:** Regression / manual ST flows unless logic extracted to services.

### O-6 — `EncounterManager` depth split

- **Goal:** Extract participant-heavy regions (**`EncounterParticipantPanel`**-style); add **`EncounterManager.*`** partials if **`.razor.cs`** exceeds the ~300-line maintenance target; optionally consolidate inline validation strings.
- **Primary files:** `EncounterManager.razor`, `EncounterManager.razor.cs`, **`EncounterParts/`**.
- **Dependencies:** §4.2 hybrid errors (toast vs inline).
- **Exit criteria:** Clearer ownership between participant management and create/template/smart-launch flows.
- **Tests:** Existing encounter coverage; manual ST scenarios.

### O-7 — `CampaignDetails` further splits

- **Goal:** Only if combined **`.razor` + partials** grow again — mirror **`CampaignDetailsParts/`** pattern.
- **Primary files:** `CampaignDetails.*`, **`CampaignDetailsParts/`**.
- **Dependencies:** None.
- **Exit criteria:** PR documents line-count or SRP rationale.

### O-8 — Additional `IModifierProvider`s

- **Goal:** When product rules require passive modifiers from Devotion/Covenant/Bloodline/Merit state in the database, add **providers** instead of extending **`ModifierService`** with new **`if`** chains.
- **Primary files:** `ModifierService.cs`, new provider types, DI registration.
- **Dependencies:** Data model for those modifier sources.
- **Exit criteria:** **`ModifierProviderTests`** + documented **`Order`** relative to existing providers.

### O-9 — Bounded export queue

- **Goal:** Replace unconstrained **`Task.Run`** for PDF/JSON export if concurrent load threatens the thread pool (§3.5 warning).
- **Primary files:** `CharacterPdfExportService`, `CharacterJsonExportService`; optional **`IHostedService`** worker.
- **Dependencies:** Ops requirements (depth, timeouts, user feedback).
- **Exit criteria:** Load-test notes or staged rollout documentation.

### O-10 — `IReferenceDataCache.FlushAsync`

- **Goal:** Runtime refresh after admin edits to reference definitions (§3.3 / risk register).
- **Primary files:** **`IReferenceDataCache`**, **`ReferenceDataCache`**, future admin mutation pipelines.
- **Dependencies:** Admin definition-edit feature.
- **Exit criteria:** Contract implemented; invalidation semantics documented.

### O-11 — Contextual trait resolution

- **Goal:** If any **`PoolTraitType`** cannot be a synchronous **`Character` → `int`** read, isolate in **`IContextualTraitResolver`** or explicit non-dictionary branches in **`TraitResolver`**.
- **Primary files:** `TraitResolver.cs`, new types if needed.
- **Dependencies:** Full inventory of traits vs. current **`FrozenDictionary`** coverage.
- **Exit criteria:** Correct pool sizes for contested/contextual traits; **`RequiemNexus.Domain.Tests`** updated.

### O-12 — `CharacterPackTab` naming

- **Goal:** Optional alignment between early plan wording (**`CharacterPackTab`**) and shipped **`CharacterDetailsPackTab`** ([`CharacterDetailsPackTab.razor`](../src/RequiemNexus.Web/Components/Pages/CharacterSheet/CharacterDetailsPackTab.razor)).
- **Primary files:** Component rename + all references.
- **Dependencies:** Low value — defer unless churn is acceptable.
- **Exit criteria:** **`dotnet build`** + grep clean.

---

## Section 8 — Plan closure (Phase 20 vs. backlog)

> **Quick reference:** See the [Status Summary](#status-summary) for completed Wave items. Post–§7 work: [Optional backlog](#optional-backlog).

| Scope | Status |
|---|---|
| **Waves 1–4** (Section 5) | **Delivered** — seed pipeline, reference cache, query/modifier/rite refactors, `CharacterDetails` partials, loading/error/modal polish, `DanseMacabre` tabs, `EncounterParts/`, `CharacterAdvancement` sections |
| **Post-Wave sweep (2026-04-04)** | **Delivered** — `CharacterProgressionSnapshotDto` (#5), `EncounterManager.NpcPicker/SmartLaunch` partials (#20), `MeleeAttackResolveModal` + `Account/Manage` skeletons (#13), `CampaignDetailsParts/` (#30), `GlimpseSocialManeuverParts/` (#32), `StorytellerGlimpseOverview` (#33) |
| **§7 consolidated backlog** | **Closed** — all rows delivered or N/A; do not reopen Wave scope without **`mission.md`** / owner decision |
| **This document** | **Delivery record** for Waves 1–4 + §7; **optional** execution items live in [Optional backlog](#optional-backlog) until implemented or explicitly cancelled |
| **Next step** | New technical work: pick from [Optional backlog](#optional-backlog) with explicit scope — see [Next step (post–§7)](#agent-next-step) |

---

## Appendix A — Reference: Patterns to Follow and Avoid

Future extractions and polish: see [Optional backlog](#optional-backlog). This appendix is pattern reference only.

### Exemplary Patterns

| File | Pattern |
|---|---|
| `SessionHub.cs` | Thin hub — all logic delegated to injected services |
| `DbInitializer.cs` + `Data/Seeding/*` | Thin orchestrator + ordered `ISeeder` implementations; structured logging per seeder |
| `StorytellerGlimpseService.GetCampaignVitalsAsync` | Projected DTO query — only needed columns fetched |
| `CharacterCreation/` step components | Section decomposition pattern for large wizard pages |
| `CharacterSheet/*AdvancementSection*.razor` | Advancement page sections (paired with `CharacterAdvancement.razor` orchestration) |
| `DanseMacabreTabs/*` | Tab panel extraction without colliding folder/page Razor type names |
| `EncounterParts/*` + `EncounterManager.*.razor.cs` | `EncounterManager` decomposition — UI in `EncounterParts/`; code-behind partials `NpcPicker` / `SmartLaunch` |
| `CharacterProgressionSnapshotDto` | Returned from Beats/XP mutations; UI patches tracked sheet without full reload |
| `GlimpseSocialManeuvers`, `GlimpsePendingRequests` | Social + pending panels with scoped `*.razor.css`; overview uses `StorytellerGlimpseOverview` + `::deep` (2026-04-04) |
| `SessionService.cs` | Consistent load/verify/proceed pattern per method |
| `IModifierProvider` + `ModifierService` | Open/closed modifier aggregation — add a provider + DI registration instead of editing the orchestrator |
| `IRiteActivationStrategy` + `SorceryActivationService` | Open/closed blood sorcery activation — add a strategy + DI registration per tradition |
| `SkeletonLoader.razor`, `LoadingContainer.razor` | Loading state components — use everywhere |
| `app-chrome.css` — `.alert` + `.alert-rn` | Shared Gothic alert chrome — hub **`alert-rn alert-*`** and auth **`alert alert-*`** both get base padding/background |
| `InitiativeTracker.SignalR.razor.cs`, `InitiativeTracker.EncounterLoad.razor.cs`, `IInitiativeTrackerDragState` | Large tracker page — isolate SignalR subscriptions, cancelable load pipeline, scoped drag state (#19) |
| `InitiativeParts/*` (`InitiativeOrderList`, `InitiativeOrderRow`, `InitiativeAddParticipantPanel`, …) | Initiative tracker — markup decomposition mirroring `EncounterParts/` (#19) |
| `InitiativeTracker.State` / `.AddParticipant` / `.Announcements` / `.Tilts` / `.NpcCombat` / `.EncounterFlow` / `.Modals` / `.Display` | Initiative tracker — code-behind partials (#19) |
| `DiceRollerStandardPanel` | Standard pool / trait / modifier / again-rote UI for `DiceRollerModal` (#17) |

### Anti-Patterns (what to avoid)

| File | Anti-Pattern |
|---|---|
| `CharacterDetails.razor.cs` (pre-refactor) | God partial — 20 injected services, all concerns in one class; the canonical example of what decomposition should eliminate |
| `DbInitializer.cs` (pre-refactor) | God static class — 20+ seed methods, N+1 loops, `Console.WriteLine` diagnostics *(superseded — see exemplary row above)* |
| `EncounterManager.razor.cs` (pre–2026-04-03) | Was error-field proliferation; improved via toast for service failures + fewer inline validation fields + `EncounterParts/` UI extraction — optional further consolidation if new flows add strings |
