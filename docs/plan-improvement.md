# RequiemNexus — Technical Improvement Plan

> **Scope:** Performance, large-file decomposition (migrations excluded), UI/UX consistency, and SOLID principle enforcement.
> **Date:** 2026-04-02 (line counts correct as of this date — they will drift). **Web decomposition inventory** (§1.1 / §1.3 campaign & combat UI) extended 2026-04-03.
> **Status:** P1–P2 items targeting Phase 20 polish; P3–P4 items are post-Phase 20 tech-debt. See `docs/mission.md` Phase 20 section.
> **Wave 1 (2026-04-02):** §3.1 `DbInitializer` N+1 / per-item saves addressed; §4.5 production `Console.WriteLine` removed in Web (`SessionClientService`, `CharacterDetails.OpenReference` → `ILogger`). §3.6: `HasIndex` for `Ghoul.RegnantCharacterId` / `RegnantNpcId` and `BloodBond.RegnantCharacterId` / `RegnantNpcId` added to fluent configuration — these indexes already appear in `ApplicationDbContextModelSnapshot` (no new migration).
> **Wave 2 (2026-04-03):** `ISeeder` pipeline in `RequiemNexus.Data/Seeding/` — `DbInitializer` orchestrates migrations + Identity roles + ordered seeders; `AddRequiemDataSeeders()` registers implementations. **`IReferenceDataCache`** (§3.3): `ReferenceDataCache` + `ReferenceDataCacheWarmupHostedService` in Web; contract in `Application.Contracts`; catalog consumers (`ClanService`, `DisciplineService`, `MeritService`, `CoilService`, `SorceryService`, `CovenantService`, `BloodlineService`, `DevotionService`, `CharacterMeritService`, `CharacterDisciplineService`, `CharacterManagementService`, `HumanityService`, `GhoulManagementService`) use the cache for official rows. **Full-pipeline seed test:** `DbInitializerTests.InitializeAsync_FullPipeline_PopulatesCoreCatalogTables` asserts non-zero counts for core seeded tables after `InitializeAsync`.
> **Wave 3 (2026-04-03):** **`ICharacterQueryService`** / **`CharacterQueryService`** (§1.2 / backlog #8); **§3.2 (partial)** — Beats/XP reload skip + `CharacterUpdateDto` progression fields + hub echo suppression. **`IModifierProvider`** (§2.2 / backlog #9) — `ConditionModifierProvider`, `CoilModifierProvider`, `WoundTrackModifierProvider`, `EquipmentModifierProvider`; `ModifierService` aggregates by `Order`; shared `PassiveModifierJsonSerializerOptions`; tests: `ModifierProviderTests` + updated `ModifierServiceTests`. **`IRiteActivationStrategy`** (§2.2 / backlog #10) — `CruacActivationStrategy`, `ThebanActivationStrategy`, `NecromancyActivationStrategy`; `SorceryActivationService` resolves by tradition; initial character load uses `Include(Rites).ThenInclude(SorceryRiteDefinition)` (removes fallback `CharacterRites` query).
> **Wave 4 (2026-04-03):** §4.1 — `SkeletonLoader` variants **`tracker`** / **`encounter-list`** / **`panel`** (compact embeds) in `SkeletonLoader.razor.css`; campaign hub pages + **character sheet sections** (Blood Bonds, Lineage, Ghouls, Predatory Aura, Social Maneuvers), **Glimpse** panels (`BloodBondsPanel`, `GhoulsTab`, `PredatoryAuraHistoryPanel`), **NPC/Faction detail**, **CampaignCharacterView** / **Advancement**, **join** page, modals (`EditLineage`, `GhoulDisciplineAccessEditor`, `PlayerWeaponDamageRollModal`), **Account/Manage** pages, and **`LoadingContainer`** use `<SkeletonLoader>` instead of italic loading paragraphs. §4.2 **(partial):** `CampaignDetails` / `CampaignJoin` / invite modal — service failures → **`ToastService.Show(..., ToastType.Error)`**; removed danger-zone `feedback-error` blocks and join `alert-rn` for caught exceptions; **`PlayerInviteModal`** no longer takes `InviteError`. **`EncounterManager`** — exception paths → toast; dropped **`_renameEncounterError`** / **`_smartLaunchConfirmError`**; **`_createError`** / **`_chronicleAddError`** / **`_improvError`** retain **inline validation only**. Remaining **`alert-rn`** on hub pages are **inline validation / empty-state** (e.g. missing campaign id, load failure message binding); auth pages keep field-adjacent alerts per §4.2. §4.3 **(2026-04-03):** `CharacterDetails` — unified **`_pendingModal`** + **`IsOpeningModal`**, `InvokeAsync(StateHasChanged)` before awaits, **`.btn-inline-spinner`** in **`CharacterDetails.razor.css`**; buttons: Apply Bloodline/Covenant, Learn Rite, Chosen Mystery, Purchase Coil. **`CharacterAdvancement`** — same for **Request rite** + **`CharacterAdvancement.razor.css`**. §1.1 **`CharacterDetails.razor.cs` decomposition (2026-04-03):** feature partials under `Components/Pages/` — **`CharacterDetails.State`**, **`.Session`**, **`.Export`**, **`.DiceRoller`**, **`.Rites`**, **`.Assets`**, **`.DisciplinePools`**, **`.Progression`**, **`.Modals.Faction`**, **`.Modals.Sorcery`**; injects consolidated in **`CharacterDetails.razor`** (removed unused **`IAdvancementService`**). **Post–Wave 4 (P3):** optional §4.2 polish on **`CharacterAdvancement`** / residual alerts; §1.3 large-Razor extractions (InitiativeTracker children, `DiceRollerModal`, etc.).
> **Review:** See `docs/plan-improvement-review.md` for open questions, answers, and rationale.

---

## Priority Legend

| Level | Meaning |
|---|---|
| **P1 — Critical** | Correctness/performance risk or severe maintainability debt — fix before adding new features |
| **P2 — High** | Architectural violation or notable UX inconsistency — address in Phase 20 refactor sprint |
| **P3 — Medium** | Code hygiene, minor perf wins, UX polish — post-Phase 20 |
| **P4 — Low** | Nice-to-have, very low risk |

---

## Section 1 — Large Files

Files over ~300 lines are a maintenance red flag. The list below excludes the `Migrations/` folder. Baseline line counts as of 2026-04-02; additional Web paths in §1.3 use counts as of 2026-04-03.

### 1.1 God Components in `Web/`

#### `CharacterDetails.razor.cs` — ~1,430 lines (P1)

**Problem:** A single partial class that owns SignalR hub lifecycle, full character state reload, 20 modal open/close flags, dice-roller state, PDF/JSON export, covenant apply/leave, beat/XP mutation, asset procurement, tab navigation, and cookie SSR/interactive bridging. It injects 20 services.

**Fix:** Split along feature domains into focused partial classes and/or child components:

| Responsibility | Extract To |
|---|---|
| Modal state + triggers for bloodline/covenant/coil/mystery/rite | `CharacterDetails.Modals.razor.cs` |
| Dice roller state + feed publication | `CharacterDetails.DiceRoller.razor.cs` |
| Export (PDF + JSON) | `CharacterDetails.Export.razor.cs` |
| Asset procurement + inventory | `CharacterDetails.Assets.razor.cs` |
| SignalR hub + reload orchestration | `CharacterDetails.Session.razor.cs` |
| Inline "Pack" tab markup | New `CharacterPackTab.razor` component |

Each partial class injects only the services it actually needs. The `OpenReference` stub that calls `Console.WriteLine` must be implemented or removed — see backlog item #16.

**Exit criteria:** No single partial class exceeds 300 lines; each partial injects ≤ 5 services.

**Tests:** Existing component-level tests must pass unchanged. New partial boundaries require no new tests unless logic is extracted to services.

#### `InitiativeTracker.razor.cs` + `InitiativeTracker.razor` + `InitiativeTracker.razor.css` — ~793 + ~440 + scoped CSS (P2 architecture; P3 markup splits)

**Problem:** Drag-and-drop state, tilt/target selection, NPC roll modal, melee attack modal, and SignalR broadcast subscriptions are all in one class. The `SemaphoreSlim _loadEncounterLock` is a symptom of concurrency being masked rather than architecturally prevented. The `.razor` file still owns a large initiative grid, add-participant flow, tilt UI, and combat modals alongside `InitiativeTracker.razor.css`.

**Fix (code-behind / services — P2):** Extract `InitiativeTrackerSignalRHandler` partial class (or equivalent, e.g. `InitiativeTracker.SignalR.razor.cs`) for all hub subscriptions (`InitiativeUpdated`, `CharacterUpdated`). Move drag state into a dedicated `DragDropService` (scoped). Replace the semaphore with a `CancellationTokenSource` properly chained to `OnParametersSetAsync` / the encounter load pipeline.

**Fix (markup / partials — P3, Wave 4):** Optional child components — e.g. `InitiativeOrderList.razor`, `InitiativeAddParticipantPanel.razor`, `InitiativeTiltPanel.razor`, `InitiativeCombatModals.razor` (or grouped modal wrapper). Optional partials — `InitiativeTracker.EncounterLoad.razor.cs`, `InitiativeTracker.CombatActions.razor.cs` — toward a ~300-line-per-file maintenance target. When extracting children, move related rules from `InitiativeTracker.razor.css` with the owning component (or keep shared class names on the page shell). Replace “Loading encounter…” with `<SkeletonLoader>` / `<LoadingContainer>` per §4.1.

**See also:** §1.3 (`InitiativeTracker`) for a Razor-focused summary; backlog **#19**.

#### `EncounterManager.razor.cs` — ~745 lines + `EncounterManager.razor` ~466 lines (P2)

**Problem:** Encounter create/edit form, chronicle note form, participant management, NPC import, and smart launch logic are all inline. 7 separate error string fields: `_createError`, `_addError`, `_actionFeedback`, `_renameEncounterError`, `_chronicleAddError`, `_improvError`, `_smartLaunchConfirmError`.

**Fix:** Extract `EncounterFormPanel.razor` and `EncounterParticipantPanel.razor` child components. Each owns its own scoped error state (inline text for form validation, `ToastService` for unexpected errors) — see Section 4.2 for the hybrid error policy.

---

### 1.2 Large Service Files in `Application/`

#### `DbInitializer.cs` — ~1,056 lines (P1 — see also Section 3.1)

**Problem:** A static class with 20+ private seed methods. Adding new seed data requires editing one enormous file. N+1 patterns inside several seed methods compound the issue.

**Fix:** Introduce `ISeeder` in the **Data** project (not Application or Web — seeders must not reference Web):
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

`DbInitializer` becomes a 30-line orchestrator that discovers `ISeeder` implementations and calls them in `Order` sequence. Each seeder receives `ILogger` and logs via structured logging — no `Console.WriteLine`.

**Exit criteria:** `DbInitializer.cs` ≤ 50 lines. Each seeder class ≤ 200 lines.

**Tests:** Integration test that runs the full seeder pipeline against an in-memory or test DB, asserting non-zero counts for each seeded table.

#### `CharacterExportService.cs` — ~379 lines (P2)

**Problem:** One class owns two completely unrelated output formats (JSON via `System.Text.Json` and PDF via QuestPDF). Different dependencies, different failure modes, different performance profiles.

**Fix:**
- Extract `ICharacterJsonExportService` / `CharacterJsonExportService`
- Extract `ICharacterPdfExportService` / `CharacterPdfExportService`
- Remove the combined `ICharacterExportService` or keep as a thin facade that delegates

#### `CharacterManagementService.cs` — ~407 lines, 8 constructor parameters (P2)

**Problem:** Handles character CRUD, Beat/XP adjustment, embargo enforcement, reload-after-mutation, campaign kindred listing for rites, and deep-include graph loading. 8 injected dependencies is a strong SRP smell.

**Fix:**
- Extract `ICharacterQueryService` / `CharacterQueryService` for all read operations (deep loads, include graph)
- Extract `ICharacterProgressionService` for Beat/XP/embargo mutations
- `CharacterManagementService` becomes a thin facade or is retired

#### `CampaignService.cs` — ~496 lines (P2)

**Problem:** Campaign CRUD, player enrollment, invite token generation, Lore entry management, and Session prep note management coexist.

**Fix:**
- Extract `ICampaignLoreService` and `ICampaignSessionPrepService`
- Merge the two `AnyAsync` authorization checks into the main projection query (see Section 3.4)

---

### 1.3 Large Razor Views

#### `DiceRollerModal.razor` — ~702 lines (P3)

**Problem:** The extended-rite roll state machine, potency calculation, and exceptional-success checkbox logic are inline in `@code` inside a modal meant to be reusable.

**Fix:** Extract `RiteExtendedRollPanel.razor` as a child component. The parent `DiceRollerModal` delegates to it when extended rite mode is active.

**Additional options:** Move `@code` to `DiceRollerModal.razor.cs` (partial) to shrink the `.razor` file without behavior change (same pattern as `CampaignDetails`, `EncounterManager`). Optionally extract a second child for the **standard** pool / trait / again-options block so the parent stays a thin shell. Child components should use scoped `.razor.css` or shared class names coordinated with [DiceRollerModal.razor.css](src/RequiemNexus.Web/Components/UI/DiceRollerModal.razor.css) to avoid visual drift.

#### `CharacterAdvancement.razor` — ~680 lines (P3) — **partial (2026-04-03)**

**Problem:** Advancement logic (`TryUpgrade`, `TryUpgradeDiscipline`, `AddNewMerit`, `AddNewDiscipline`) is inline in `@code` alongside large markup sections for Attributes, Skills, Merits, Disciplines, and Devotions.

**Fix:** Following the pattern already set by `CharacterCreation/` step components, extract:
- `<AttributesAdvancementSection>`, `<SkillsAdvancementSection>`
- `<MeritsAdvancementSection>`, `<DisciplinesAdvancementSection>`, `<DevotionsAdvancementSection>`

Each section owns its own loading state, scoped error display, and service calls.

**Delivered (2026-04-03):** Sections live under `Web/Components/Pages/CharacterSheet/` (`AttributesAdvancementSection`, `SkillsAdvancementSection`, `DisciplinesAdvancementListSection`, `MeritsAdvancementSection`, `NewDisciplineAdvancementSection`, `DevotionsAdvancementSection`, `BloodSorceryAdvancementSection`). Page keeps orchestration and `@code`. §4.2: **`ToastService`** for `catch` / post–ST-ack failures; **inline** `_validationMessage` (`.advancement-validation-message`) for XP/spec/eligibility text; removed top `alert alert-danger`.

#### `CampaignDetails.razor` + `CampaignDetails.razor.cs` — ~377 + ~592 lines (P3)

**Problem:** ~970 lines combined; load state, ST vs player UI, session start/stop, invite modal, lore/prep tabs, roster, hidden perception rolls, and SignalR/session wiring in one page.

**Fix:** **Child components** (UI-only; parent keeps orchestration): e.g. `CampaignSessionBanner.razor`, `CampaignRosterPanel.razor`, `CampaignLorePrepTabs.razor`, `CampaignInviteModal.razor`, `StorytellerPerceptionPanel.razor` — data + `EventCallback`s in, no new business rules in Web. **Partial classes:** `CampaignDetails.Session.razor.cs`, `CampaignDetails.Roster.razor.cs`, `CampaignDetails.LorePrep.razor.cs` (mirrors §1.1 `CharacterDetails.*` pattern). Replace `<p><em>Loading campaign...</em></p>` per §4.1.

#### `DanseMacabre.razor` — ~308 lines (P3) — **partial (2026-04-03)**

**Problem:** Single file with three tab panels (territories, power structure, NPCs) and `@code` from ~line 180.

**Fix:** Per-tab components under `Web/Components/Pages/Campaigns/DanseMacabre/` (or `Web/Components/Campaigns/DanseMacabre/`): e.g. `DanseTerritoriesTab.razor`, `DansePowerStructureTab.razor`, `DanseNpcsTab.razor`; page holds tab index and `CampaignId`. Optional `DanseMacabre.razor.cs` for all code-behind. Replace ad-hoc loading per §4.1.

**Delivered (2026-04-03):** Tab panels in `Web/Components/Pages/Campaigns/DanseMacabreTabs/` (`DanseTerritoriesTab`, `DansePowerStructureTab`, `DanseNpcsTab`) — **not** a subfolder named `DanseMacabre` alongside `DanseMacabre.razor` (avoids Razor duplicate type name). Parent keeps modals and `@code`.

#### `EncounterManager.razor` + `EncounterManager.razor.cs` — ~466 + ~745 lines (P2 / P3)

**Problem / fix:** Align with §1.1 — `EncounterFormPanel.razor`, `EncounterParticipantPanel.razor`; hybrid errors per §4.2.

**Additional markup splits:** NPC picker modes (stat block, template, chronicle NPC, improv) as subcomponents to shrink the `.razor`. Optional partials: `EncounterManager.NpcPicker.razor.cs`, `EncounterManager.SmartLaunch.razor.cs` if any file remains over ~300 lines. Replace “Loading encounters…” per §4.1.

#### `GlimpseSocialManeuvers.razor` — ~449 lines (P3)

**Problem:** Threshold config, new-maneuver form, and per-maneuver list/actions in one file; `@code` ~200 lines. Embedded from `StorytellerGlimpse.razor`.

**Fix:** `GlimpseSocialManeuverThresholdRow.razor`, `GlimpseSocialManeuverNewForm.razor`, `GlimpseSocialManeuverRow.razor` / `GlimpseSocialManeuverCard.razor`; optional `GlimpseSocialManeuvers.razor.cs`. **Constraint:** preserve parameter surface for `StorytellerGlimpse` (`Vitals`, `Npcs`, `Maneuvers`, campaign id, etc.) — do not change public parameters without updating the parent.

#### `InitiativeTracker` — see §1.1 (P2 + P3)

**Summary:** Combined ~1,233 lines (`.razor` + `.razor.cs`) plus `InitiativeTracker.razor.css`. Architectural work (SignalR partial, `DragDropService`, `CancellationTokenSource` vs. semaphore) lives in **§1.1**. Markup/CSS/loading refinements are **§1.3 / Wave 4**; single backlog item **#19** (no duplicate row).

#### `StorytellerGlimpse.razor` + `.razor.cs` + `.razor.css` — ~420 + ~538 + ~473 lines (P3)

**Problem:** ST dashboard with tab bar; overview tab remains large after partial extraction of `GlimpseSocialManeuvers` and `GlimpsePendingRequests` (social / approvals). Overview holds degeneration banners, passive predatory aura, coterie Beat drag-source, pinned NPCs, character vitals grid (drag-drop Beat/XP, lineage), and modals. Code-behind includes hub connect, `SessionClient.ChronicleUpdated`, awards, drag-drop, degeneration modal.

**Fix:** **Child components:** e.g. `GlimpseOverviewPanel.razor`, or finer `GlimpseDegenerationBanners.razor`, `GlimpsePassiveAuraCard.razor`, `GlimpseCoterieBeatCard.razor`, `GlimpseCharacterVitalsCard.razor` (one PC card; parent maps `_vitals`). **Partials:** e.g. `StorytellerGlimpse.Session.razor.cs`, `StorytellerGlimpse.Overview.razor.cs`, `StorytellerGlimpse.Awards.razor.cs`. **CSS:** relocate selectors from [StorytellerGlimpse.razor.css](src/RequiemNexus.Web/Components/Pages/Campaigns/StorytellerGlimpse.razor.css) into matching `*.razor.css` for children; page keeps tab bar + layout; use `::deep` only where needed. Replace “Loading campaign vitals…” per §4.1.

---

## Section 2 — SOLID Principles

### 2.1 Single Responsibility Principle

| Violation | File | Fix |
|---|---|---|
| God component (20 services, all modal/dice/export/session state) | `CharacterDetails.razor.cs` | See 1.1 above |
| God static seeder | `DbInitializer.cs` | See 1.2 above |
| JSON + PDF export in one class | `CharacterExportService.cs` | See 1.2 above |
| Character CRUD + Beat + reload + campaign listing | `CharacterManagementService.cs` | See 1.2 above |

### 2.2 Open/Closed Principle

#### `ModifierService.cs` — ~375 lines (P2) — **delivered 2026-04-03**

**Problem:** Aggregates modifiers from Conditions, Coils, Devotions, Covenant benefits, and equipment using `if` chains on `ModifierSourceType`. Adding a new source requires editing this class.

**Fix (as implemented):** `IModifierProvider` with `Order`, `SourceType`, and `GetModifiersAsync(int characterId, CancellationToken)` returning `PassiveModifier` (current product shape; not `Character` entity). Providers: `ConditionModifierProvider`, `CoilModifierProvider`, `WoundTrackModifierProvider`, `EquipmentModifierProvider`. `ModifierService` aggregates ordered providers. *(Devotion / Covenant / Bloodline / Merit sources remain future providers if rules add DB-backed passive modifiers for them.)*

**Tests:** `ModifierProviderTests` (per provider) + existing `ModifierServiceTests` through the composed `ModifierService`.

#### `TraitResolver.cs` — ~326 lines (P3)

**Problem:** Large `if`/`switch` chains mapping `PoolTraitType` enum values to character properties. Every new trait added to the game requires editing this class.

**Fix:** Replace the switch with a dictionary of `Func<Character, int>` keyed on `PoolTraitType`, built once in the constructor:
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

#### `ICharacterService` covers reads and writes (P3)

Components that only need to read character state still depend on the full `ICharacterService` interface. Split:
- `ICharacterReader` — `GetCharacterByIdAsync`, `GetAllCharactersAsync`
- `ICharacterWriter` — `CreateCharacterAsync`, `UpdateCharacterAsync`, `DeleteCharacterAsync`

Razor components and read-only services depend only on `ICharacterReader`.

### 2.4 Dependency Inversion Principle

#### `SessionClientService` exposes 12 raw `event Action<T>` fields (P3)

**Problem:** Components subscribe via `+=` and must unsubscribe in `DisposeAsync`. Missed unsubscriptions leak component references. The pattern is fragile under Blazor navigation.

**Fix — zero new NuGet packages:** Implement a lightweight subscription token pattern using only BCL types. No `System.Reactive` or third-party Rx library:
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

### 3.1 Eliminate N+1 Queries in `DbInitializer`

#### `UpdateDisciplineAcquisitionMetadataAsync` (P1)

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

#### `SeedCoilsAsync` — `SaveChangesAsync` inside a loop (P1)

**Problem:** One DB round-trip per coil during initial seed.

**Fix:** Replace per-item saves with:
```csharp
await context.CoilDefinitions.AddRangeAsync(coilsToAdd);
await context.SaveChangesAsync();
```

#### `SeedDeferredAssetCapabilitiesAsync` — `AnyAsync` inside a loop (P1)

**Problem:** One existence-check query per capability.

**Fix:** Pre-load existing capability IDs into a `HashSet<int>`, then filter in memory before a single `AddRangeAsync`.

### 3.2 Reduce Character Reload Cost (P1)

**Problem:** `CharacterManagementService.GetCharacterByIdAsync` fires ~15 SQL queries (14-Include split query) and is called after **every** mutation — adding a Beat, toggling a discipline, equipping an item.

**Fixes:**
1. For mutations that return their own result (e.g., Beat addition), return the delta from the service and patch the in-memory `Character` object rather than reloading.
2. Introduce `ICharacterPatchEvent` published via the event bus after each mutation. `CharacterDetails` subscribes and applies the patch, triggering a full reload only for operations that structurally change the Include graph (e.g., buying a new Discipline).
3. For read-only displays (ST Glimpse, Campaign member lists), use projected DTOs rather than the entity graph.

**Exit criteria:** A Beat-add action triggers ≤ 2 DB queries (mutation + minimal confirmation), down from ~15.

**Tests:** Integration test asserting query count for Beat-add via `EF Core event counting` or `MiniProfiler`.

### 3.3 Add Reference-Data Caching (P1)

**Problem:** Zero caching exists in the codebase. Every page load that needs Clan/Discipline/Merit/Covenant/Rite/Coil definitions re-queries the database. These records are seeded once and never change at runtime.

**Fix:** Register a `IReferenceDataCache` singleton:
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

#### `CoilService.GetEligibleCoilsAsync`

**Problem:** All coil definitions are loaded from DB, then filtered in memory.

**Fix:**
```csharp
var existingSlugs = character.Coils.Select(c => c.CoilDefinitionSlug).ToHashSet();
return await _dbContext.CoilDefinitions
    .Where(c => c.MinBloodPotency <= character.BloodPotency
             && !existingSlugs.Contains(c.Slug))
    .Include(c => c.Scale)
    .AsNoTracking()
    .ToListAsync();
```

#### `CampaignService.GetCampaignByIdAsync` — 3 round-trips (P2)

**Problem:** Two `AnyAsync` authorization checks followed by the main load query.

**Fix:** Fetch a minimal campaign DTO in a single query that includes the caller's membership status. Perform the authorization check in memory on the returned DTO. If unauthorized, return `null` / throw `NotFoundException` — **do not return sensitive campaign fields to non-members**. This must follow the standard Masquerade 4-step `AuthorizationHelper` sequence even though the check is now in-memory; the authorization helper must still be invoked to maintain audit compliance. This refactor is purely a round-trip optimization — authorization semantics are unchanged.

> **HTTP status mapping:** Whether the unauthorized path surfaces as HTTP 404 (to avoid leaking campaign existence) or HTTP 403 (explicit denial) is an implementation-time decision. Document the chosen mapping in the Application layer when implementing — the plan's invariant is authorization-first, not status-code-prescriptive.

### 3.5 Move CPU-Bound Work Off the Render Thread (P3)

`CharacterExportService.ExportCharacterAsPdf` calls synchronous QuestPDF APIs on the Blazor Server render thread.

**Fix:**
```csharp
public async Task<byte[]> ExportCharacterAsPdfAsync(Character character)
{
    return await Task.Run(() => BuildPdfDocument(character));
}
```
Similarly, `JsonSerializer.Serialize` for large character graphs should be called inside `Task.Run`.

**Concurrency warning:** Unbounded `Task.Run` under load can starve the Blazor Server thread pool. For Phase 20 (low concurrent user count), bare `Task.Run` is acceptable. If concurrent export load increases in the future, replace with a **bounded `Channel<T>`-based export queue** or a dedicated `IHostedService` worker. Do not use `Task.Run` as a permanent scaling solution.

### 3.6 Fix Missing Database Indexes (P3)

All index additions require an **EF Core migration** — do not add raw SQL. Add `HasIndex` calls to the relevant entity configuration, then run `dotnet ef migrations add <name>`.

| Table / Configuration | Missing Index | Impact |
|---|---|---|
| `Ghoul` | `RegnantCharacterId`, `RegnantNpcId` | Ghoul management and blood bond queries filter on these |
| Asset discriminator | Discriminator column in `Asset` TPH hierarchy | Type-filtered asset catalog queries scan the full table |
| `BloodBond` | Confirm `ThrallCharacterId` and `RegnantCharacterId` are both indexed | `BloodBondQueryService` filters on both |

---

## Section 4 — UI/UX Consistency

### 4.1 Standardize Loading States (P2)

**Problem:** At least five different loading patterns exist across pages:
- `<SkeletonLoader Variant="sheet" />` — Character details ✓
- `<SkeletonLoader Variant="card" Count="3" />` — Campaigns index ✓
- `<SkeletonLoader Variant="encounter-list" />` — Encounter manager ✓ (2026-04-03)
- `<SkeletonLoader Variant="tracker" />` — Initiative tracker ✓ (2026-04-03)
- Custom loading markup — Blood Bonds Panel, Ghouls Tab, character sheet embeds, NPC detail, account pages ✓ (2026-04-03 sweep); spot-check with `rg` for new pages ✗

**Fix:** Mandate `<SkeletonLoader>` or `<LoadingContainer>` for all loading states. Delete ad-hoc loading markup. Add skeleton variants for remaining page types (**tracker** ✓, **encounter-list** ✓, NPC detail — pending).

**Delivered (2026-04-03):** `CampaignDetails` (`card`), `StorytellerGlimpse` (`sheet`), `DanseMacabre` (`rows`); new CSS variants in `SkeletonLoader.razor.css`.

### 4.2 Standardize Error Display — Hybrid Policy (P2)

**Problem:**
- `CharacterDetails.razor.cs` uses `ToastService.Show(...)` — correct pattern
- `CampaignDetails.razor.cs` uses `private string? _errorMessage` rendered as a raw Bootstrap alert div
- `EncounterManager.razor.cs` uses 7 separate string fields

**Policy — hybrid approach:**
- **`ToastService`** for global/unexpected errors (service failures, auth errors, unhandled exceptions)
- **Inline text** (small `<span class="text-danger">`) for form validation errors that must sit field-adjacent for accessibility and screen-reader compliance

**Fix:** Remove all raw Bootstrap alert divs. Replace unexpected-error string fields with `ToastService.Show(..., ToastType.Error)` (or a thin `ShowError` helper if added). Retain (or introduce) a single `string? _validationError` per modal/form for inline validation only — do not proliferate to 7 fields.

**Delivered (2026-04-03, partial):** `CampaignDetails` (leave/delete/remove/invite failures → toast); `CampaignJoin` (join API exceptions → toast); `PlayerInviteModal` (invite errors via toast from parent); `EncounterManager` (rename/smart-launch/service catches → toast; create/chronicle/improv keep inline messages only for validation).

### 4.3 Add Intermediate Loading on Modal Triggers (P2)

**Problem:** Several modal trigger buttons in `CharacterDetails.razor.cs` (`OpenLearnRiteModal`, `OpenApplyBloodlineModal`, `OpenChosenMysteryModal`, `OpenLearnCoilModal`) start async service calls but show no visual feedback between click and modal open.

**Fix:** A single `string? _pendingModal` field tracks which modal is loading. The triggering button renders as disabled with a spinner while `_pendingModal == nameof(OpenLearnRiteModal)`. Cleared after the modal data is ready.

**Delivered (2026-04-03):** `CharacterDetails` — `OpenApplyBloodlineModal`, `OpenApplyCovenantModal`, `OpenLearnRiteModal`, `OpenChosenMysteryModal`, `OpenLearnCoilModal` set/clear `_pendingModal` in `try`/`finally`; markup uses `disabled`, `aria-busy`, and inline spinner + “Opening…” label. **`CharacterAdvancement`** — same pattern for **Request rite** (`CharacterAdvancement.razor.css` spinner).

### 4.4 Extract `CharacterVitals.razor` Tracker Sub-Components (P3)

**Problem:** Health, Willpower, and Vitae trackers are rendered in one ~283-line component with per-tracker animation flags, JS interop, and inline SVG.

**Fix:** Extract `<HealthTracker>`, `<WillpowerTracker>`, `<VitaeTracker>` as independent components. Each owns its own animation flag and JS interop. `CharacterVitals.razor` becomes a layout wrapper.

### 4.5 Audit and Clean Up `Console.WriteLine` (P2)

A full audit of production code (excluding test infrastructure, where `Console.WriteLine` is acceptable) reveals at minimum:

| Location | Fix |
|---|---|
| `CharacterDetails.razor.cs` — `OpenReference` stub | Implement the rules-reference side panel, or remove call sites and delete the method |
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
13. **Remaining large component extractions** (post–Wave 4 / P3): 1.1 `InitiativeTracker` / `EncounterManager`, 1.3 `DiceRollerModal` / `CharacterAdvancement` / **`CampaignDetails`** / **`DanseMacabre`** / **`EncounterManager` markup** / **`GlimpseSocialManeuvers`** / **`StorytellerGlimpse`** (overview + CSS split) / **`InitiativeTracker` .razor + partials alongside §1.1 SignalR–drag work**, 4.4 vitals

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
| 1 | P1 | Large File / SOLID | Decompose `CharacterDetails.razor.cs` into feature-scoped partials |
| 2 | P1 | Performance | `DbInitializer` — eliminate N+1 saves in `SeedCoilsAsync` and `SeedDeferredAssetCapabilitiesAsync` |
| 3 | P1 | Performance | `DbInitializer.UpdateDisciplineAcquisitionMetadataAsync` — pre-load disciplines into dictionary |
| 4 | P1 | Performance | Add `IReferenceDataCache` singleton for static seed data |
| 5 | P1 | Performance | Reduce `GetCharacterByIdAsync` reload on every mutation — patch events or delta returns |
| 6 | P2 | SOLID / Large File | Decompose `DbInitializer.cs` into `ISeeder` implementations (Data project) |
| 7 | P2 | SOLID | Split `CharacterExportService` into JSON and PDF export services |
| 8 | P2 | SOLID | Extract `CharacterQueryService` from `CharacterManagementService` |
| 9 | P2 | SOLID | Introduce `IModifierProvider` per source type in `ModifierService` — **done (2026-04-03)** — Condition / Coil / WoundTrack / Equipment providers + aggregator |
| 10 | P2 | SOLID | Introduce `IRiteActivationStrategy` per tradition in `SorceryActivationService` — **done (2026-04-03)** |
| 11 | P2 | Performance | Push coil eligibility filter into EF query in `CoilService` |
| 12 | P2 | Performance | Reduce 3 round-trips in `CampaignService.GetCampaignByIdAsync` (Masquerade-safe DTO) |
| 13 | P2 | UI/UX | Standardize all loading states to `<SkeletonLoader>` / `<LoadingContainer>` — **partial (2026-04-03)** — see §4.1 |
| 14 | P2 | UI/UX | Standardize error surfaces — `ToastService` for global errors, inline text for form validation |
| 15 | P2 | UI/UX | Add intermediate loading feedback on modal trigger buttons — **done (2026-04-03):** `CharacterDetails` + `CharacterAdvancement` (rite) `_pendingModal` + spinners |
| 16 | P2 | Logging | Audit and replace all `Console.WriteLine` in production code with `ILogger`; implement or remove `OpenReference` stub |
| 17 | P3 | Large File | Extract `RiteExtendedRollPanel.razor` from `DiceRollerModal.razor` |
| 18 | P3 | Large File | Extract section components from `CharacterAdvancement.razor` — **done (2026-04-03)** — see §1.3 `CharacterAdvancement`; extract `RiteExtendedRollPanel` from `DiceRollerModal` still open |
| 19 | P3 | Large File | Decompose `InitiativeTracker` — SignalR partial (or `InitiativeTracker.SignalR.razor.cs`), scoped `DragDropService`, `CancellationTokenSource` load pipeline (§1.1); optional `InitiativeTracker.razor` child components + `InitiativeTracker.razor.css` co-split (§1.3) |
| 20 | P3 | Large File | Consolidate `EncounterManager.razor.cs` error fields; extract form child components |
| 21 | P3 | SOLID | Add `ICharacterReader` / `ICharacterWriter` split to `ICharacterService` |
| 22 | P3 | SOLID | Replace `TraitResolver` switch with `Dictionary<PoolTraitType, Func<Character, int>>` (sync-only paths) |
| 23 | P3 | SOLID | Introduce `ISessionEventBus` (zero-new-package subscription token pattern) |
| 24 | P3 | Performance | Wrap QuestPDF + `JsonSerializer` in `Task.Run` with documented scale-up path |
| 25 | P3 | UI/UX | Extract `<HealthTracker>`, `<WillpowerTracker>`, `<VitaeTracker>` from `CharacterVitals.razor` |
| 26 | P3 | Database | Add `HasIndex` + migration for `Ghoul.RegnantCharacterId`, `Ghoul.RegnantNpcId` |
| 27 | P3 | Database | Confirm and add `HasIndex` + migration for `BloodBond` thrall/regnant columns |
| 28 | P4 | Database | Add explicit discriminator index on `Asset` TPH table |
| 29 | P4 | Performance | Merge double character lookup in `PredatoryAuraService.ResolveLashOutAsync` |
| 30 | P3 | Large File | Decompose `CampaignDetails` (`.razor` + `.razor.cs`, ~970 lines combined) into child components and/or feature partials — session/roster, lore/prep, invite, ST perception (§1.3) |
| 31 | P3 | Large File | Decompose `DanseMacabre.razor` into per-tab components (territories / power structure / NPCs) — **tabs extracted (2026-04-03)** to `DanseMacabreTabs/`; optional `DanseMacabre.razor.cs` still open |
| 32 | P3 | Large File | Decompose `GlimpseSocialManeuvers.razor` into threshold row, new-maneuver form, and per-maneuver card/row components; optional `.razor.cs`; preserve parameter surface for `StorytellerGlimpse` (§1.3) |
| 33 | P3 | Large File | Decompose `StorytellerGlimpse` — overview tab into child components and/or partials; split `StorytellerGlimpse.razor.css` with extracted components; optional `StorytellerGlimpse.Session.razor.cs` for hub wiring (§1.3) |

**Section 7 deduplication:** Do not add new rows for `DiceRollerModal` (**#17**), `EncounterManager` (**#20**), or a second `InitiativeTracker` row (**#19** covers `.razor` + CSS extensions).

---

## Appendix A — Reference: Patterns to Follow and Avoid

### Exemplary Patterns

| File | Pattern |
|---|---|
| `SessionHub.cs` | Thin hub — all logic delegated to injected services |
| `StorytellerGlimpseService.GetCampaignVitalsAsync` | Projected DTO query — only needed columns fetched |
| `CharacterCreation/` step components | Section decomposition pattern for large wizard pages |
| `GlimpseSocialManeuvers`, `GlimpsePendingRequests` | Partial decomposition of `StorytellerGlimpse` — further overview/CSS splits in §1.3 |
| `SessionService.cs` | Consistent load/verify/proceed pattern per method |
| `IModifierProvider` + `ModifierService` | Open/closed modifier aggregation — add a provider + DI registration instead of editing the orchestrator |
| `IRiteActivationStrategy` + `SorceryActivationService` | Open/closed blood sorcery activation — add a strategy + DI registration per tradition |
| `SkeletonLoader.razor`, `LoadingContainer.razor` | Loading state components — use everywhere |

### Anti-Patterns (what to avoid)

| File | Anti-Pattern |
|---|---|
| `CharacterDetails.razor.cs` (pre-refactor) | God partial — 20 injected services, all concerns in one class; the canonical example of what decomposition should eliminate |
| `DbInitializer.cs` (pre-refactor) | God static class — 20+ seed methods, N+1 loops, `Console.WriteLine` diagnostics |
| `EncounterManager.razor.cs` | Error-field proliferation — 7 separate `string?` fields for 7 error states |
