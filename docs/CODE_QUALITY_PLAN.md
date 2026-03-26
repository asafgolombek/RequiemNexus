# 🩸 Code Quality Plan — Optimization, Security, Duplication & Large-File Refactoring

> _"Every line of code is an intentional strike against technical debt."_

**Generated:** 2026-03-25
**Scope:** All non-migration `.cs` and `.razor` files under `src/`
**Method:** Static analysis + line-count audit + pattern grep across the full codebase

---

## 📊 Executive Summary

| Category | Findings | Severity |
|----------|----------|----------|
| Security issues | 0 violations | ✅ Clean |
| Bare `catch` blocks | 9 instances | 🔴 High (seeder fallbacks) / 🟡 Medium (one in UI) |
| Missing `AsSplitQuery()` | 3 query sites | 🟡 Medium |
| Large C# service files (300+ lines) | 13 files | 🟡 Medium |
| Large Razor / code-behind files (250+ lines) | 11 files | 🟡 Medium |
| `Program.cs` (480 lines — no split strategy) | 1 file | 🟡 Medium |
| Code duplication — seeder JSON-fallback pattern | 7 seeder files | 🟡 Medium |
| Code duplication — Razor component patterns | Multiple pages | 🟢 Low |
| Missing structured logging in catch branches | 1 instance | 🟡 Medium |
| Magic strings in seed fallback logic | 2 services | 🟢 Low |

---

## 🔒 1. Security

**Result: No violations found.** The Masquerade authorization pattern is applied consistently.

All mutation methods across 25+ services follow the required 4-step sequence:
1. Extract `userId` from the method parameter (always sourced from `ClaimsPrincipal` at the Hub / controller boundary — never from a request DTO).
2. Call `_authHelper.RequireStorytellerAsync()` or `RequireCharacterOwnerAsync()` before any DB write.
3. Verify entity ownership in the EF query itself (e.g., `.Where(c => c.ApplicationUserId == userId)`).
4. Perform mutation + `SaveChangesAsync`.

`SessionHub.cs` (251 lines) extracts `UserId` from `Context.UserIdentifier` (ASP.NET Core identity claim) on every method. No raw SQL or string interpolation in queries. No IDOR vulnerabilities found.

**No action required.**

---

## 🐛 2. Bare `catch` Blocks

### Pattern A — Seeder JSON fallback (7 files) 🔴

Seven seeder files use a bare `catch { return fallback; }` pattern to silently fall back to an in-memory seed when the embedded JSON file cannot be parsed. If a JSON file is malformed (missing comma, encoding issue), the app starts with incomplete seed data and no error is logged — this is a silent data integrity failure at startup.

| File | Line | Fallback |
|------|------|---------|
| `RequiemNexus.Data/SeedData/SorceryRiteSeedData.cs` | 47 + 76 | `GetMinimalCruac()` / `GetMinimalTheban()` |
| `RequiemNexus.Data/SeedData/BloodlineSeedData.cs` | 128 | `GetAllBloodlines()` |
| `RequiemNexus.Data/SeedData/CovenantSeedData.cs` | 64 | `GetAllCovenants()` |
| `RequiemNexus.Data/SeedData/MeritSeedData.cs` | 96 | `GetAllMerits()` |
| `RequiemNexus.Data/SeedData/CoilSeedData.cs` | 37 | `GetMinimalSeed()` |
| `RequiemNexus.Data/SeedData/DevotionSeedData.cs` | 194 | `GetSampleDevotions()` |

**Fix for each:**
```csharp
// Before
catch
{
    return GetMinimalCruac();
}

// After
catch (Exception ex)
{
    logger.LogError(ex, "Failed to parse {FileName}; falling back to in-memory seed. Verify JSON integrity.", fileName);
    return GetMinimalCruac();
}
```

**Requirement:** Each seeder must receive an `ILogger` (or `ILogger<T>`) parameter and log the exception before returning the fallback. This is the only change — fallback behavior is intentional and correct.

### Pattern B — UI roller fallback (1 file) 🟡

**File:** `RequiemNexus.Web/Components/Pages/CharacterDetails.razor.cs:621`

```csharp
// Before
catch
{
    _rollerBaseDice = 0;
    _isRollerOpen = true;
}

// After
catch (Exception ex)
{
    Logger.LogWarning(ex, "Failed to resolve trait {TraitId} for dice roller on character {CharacterId}", traitId, Id);
    _rollerBaseDice = 0;
    _isRollerOpen = true;
}
```

### Pattern C — JS interop platform detection (1 file) 🟢 Acceptable

**File:** `RequiemNexus.Web/Services/PlatformShortcutHintService.cs:28`

This is a JS interop fallback for `getPaletteShortcutLabel` — used only for display. The exception is genuinely unimportant (JS not loaded yet on pre-render). Consider `catch (JSException)` for clarity but this is lowest priority.

---

## ⚡ 3. Query Performance

### 3.1 Missing `AsSplitQuery()` on multi-collection eager loads 🟡

EF Core generates a cartesian product when a single LINQ query includes multiple collection navigations via `.Include()`. For a character with 10 merits, 5 disciplines, and 8 attributes, the unmitigated join is `10 × 5 × 8 = 400 rows` for what is actually 23 entities. `AsSplitQuery()` replaces this with separate SQL queries per collection, eliminating the explosion.

**`CampaignService.cs` already uses `AsSplitQuery()` at line 67 — this is the reference pattern.**

**Sites to fix:**

| File | Method | Include count | Fix |
|------|--------|--------------|-----|
| `CharacterManagementService.cs:57` | `GetCharacterByIdAsync` | 16 `.Include()` chains | Add `.AsSplitQuery()` before `.FirstOrDefaultAsync()` |
| `CoilService.cs:138` | Character load for coil purchase | 12 `.Include()` chains | Add `.AsSplitQuery()` |
| `CharacterExportService.cs` | `ExportCharacterAsync` | 14+ `.Include()` chains | Add `.AsSplitQuery()` |

```csharp
// Pattern to apply (CharacterManagementService.cs example)
return await dbContext.Characters
    .Include(c => c.Attributes)
    .Include(c => c.Skills)
    .Include(c => c.Merits).ThenInclude(m => m.Merit)
    // ... remaining includes ...
    .AsSplitQuery()          // ← ADD THIS
    .AsNoTracking()
    .FirstOrDefaultAsync(c => c.Id == id && c.ApplicationUserId == userId);
```

### 3.2 In-memory LINQ after DB materialization 🟢 Acceptable

`EncounterService.cs` materializes a `List<InitiativeEntry>` and then performs in-memory `.Where()` / `.OrderBy()`. For encounter sizes ≤ 50 combatants (typical table session) this is negligible. Document the size assumption with a comment; no code change required.

### 3.3 ModifierService — 5 sequential DB queries 🟢 Acceptable

`ModifierService.GetModifiersForCharacterAsync()` runs 5 `AsNoTracking` queries against different tables. They are intentionally separated to avoid cartesian explosion. The method is called once per character sheet render, not in loops. No change required; add an OpenTelemetry activity span to track combined latency if needed in Phase 20.

---

## 🔁 4. Code Duplication

### 4.1 Seeder JSON fallback pattern — consolidate 🟡

Every seeder in `RequiemNexus.Data/SeedData/` follows the same three-step pattern:
1. Build path to embedded JSON resource
2. Parse JSON
3. Bare `catch { return in-memory fallback; }`

This structure is repeated 7× with no shared base. A helper utility would eliminate all duplication and centralize the logging fix from §2:

**New file:** `RequiemNexus.Data/SeedData/SeedDataLoader.cs`

```csharp
/// <summary>
/// Utility for loading seed data from embedded JSON with a typed fallback on parse failure.
/// </summary>
internal static class SeedDataLoader
{
    internal static T LoadOrFallback<T>(
        string embeddedResourcePath,
        Func<JsonElement, T> parser,
        Func<T> fallback,
        ILogger logger)
    {
        try
        {
            using var stream = typeof(SeedDataLoader).Assembly
                .GetManifestResourceStream(embeddedResourcePath)
                ?? throw new InvalidOperationException($"Embedded resource '{embeddedResourcePath}' not found.");
            using var doc = JsonDocument.Parse(stream);
            return parser(doc.RootElement);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load seed data from '{Resource}'; using in-memory fallback.", embeddedResourcePath);
            return fallback();
        }
    }
}
```

Each seeder's load method then becomes a one-liner:
```csharp
return SeedDataLoader.LoadOrFallback(
    "RequiemNexus.Data.SeedSource.coils.json",
    ParseCoils,
    GetMinimalSeed,
    logger);
```

### 4.2 Razor — repeated modal trigger button 🟢

The pattern `<button @onclick="OpenXxxModal" class="btn btn-secondary btn-sm">...</button>` followed by a matching `@if (_isXxxModalOpen) { <XxxModal .../> }` appears 15+ times across `CharacterDetails.razor`, `InitiativeTracker.razor`, and `CampaignDetails.razor`.

**Extract:** `RequiemNexus.Web/Components/UI/ModalTriggerButton.razor`

```razor
<button type="button" class="@ButtonClass" @onclick="OnClick" disabled="@Disabled">
    @Label
</button>

@code {
    [Parameter] public string Label { get; set; } = string.Empty;
    [Parameter] public string ButtonClass { get; set; } = "btn btn-secondary btn-sm";
    [Parameter] public EventCallback OnClick { get; set; }
    [Parameter] public bool Disabled { get; set; }
}
```

### 4.3 Razor — repeated vitals row pattern 🟢

`InitiativeTracker.razor` and `StorytellerGlimpse` views both render HP/Willpower/Vitae dot-scale rows using nearly identical markup. Extract into:

**`RequiemNexus.Web/Components/UI/VitalsRow.razor`** — takes `CurrentHealth`, `MaxHealth`, `HealthDamage`, `CurrentWillpower`, `MaxWillpower`, `CurrentVitae`, `MaxVitae` as parameters.

---

## ✂️ 5. Large Files — C# Services

### 5.1 `SocialManeuveringService.cs` — 666 lines 🟡

**Responsibility mix:** Social maneuver creation, roll resolution, impression-condition mapping, door tracking, and query projections all live in one class.

**Split into:**

| New File | Responsibility | Lines (est.) |
|----------|---------------|-------------|
| `SocialManeuveringService.cs` | Query methods (get, list, project) + door tracking | ~200 |
| `SocialManeuverRollService.cs` | `RollApproachAsync`, `RollFinalActAsync`, contested roll logic | ~200 |
| `SocialManeuverConditionService.cs` | Condition application on success/failure + ST approval gate | ~200 |

Contracts: `ISocialManeuveringService` stays; add `ISocialManeuverRollService` and `ISocialManeuverConditionService`.

### 5.2 `EncounterService.cs` — 569 lines 🟡

**Responsibility mix:** Encounter lifecycle (create/resolve), initiative ordering, participant management (add/remove PC, NPC, template-NPC), damage tracking, and the encounter-clone path for Storyteller Glimpse.

**Split into:**

| New File | Responsibility | Lines (est.) |
|----------|---------------|-------------|
| `EncounterService.cs` | Lifecycle (create, resolve, list, clone) | ~200 |
| `EncounterParticipantService.cs` | Add/remove PCs, NPCs, template-NPCs from encounter | ~180 |
| `EncounterInitiativeService.cs` | Set initiative order, recalculate tiers, advance round | ~150 |

### 5.3 `BloodBondService.cs` — 537 lines 🟡

**Responsibility mix:** Bond recording, condition application, fading schedule, alert generation, and Blood Bond query projections.

**Split into:**

| New File | Responsibility | Lines (est.) |
|----------|---------------|-------------|
| `BloodBondService.cs` | Bond CRUD + query projections | ~200 |
| `BloodBondConditionService.cs` | Condition application from bond (Vinculum, Addicted) | ~150 |
| `BloodBondFadingService.cs` | Fading logic, interval checks, ST alerts | ~150 |

### 5.4 `CoilService.cs` — 552 lines 🟡

**Responsibility mix:** Coil/Scale purchase, prerequisite validation, XP cost calculation, Mystery Scale selection, and Crucible Ritual access.

**Split into:**

| New File | Responsibility | Lines (est.) |
|----------|---------------|-------------|
| `CoilService.cs` | Purchase flow, XP deduction, save | ~200 |
| `CoilPrerequisiteService.cs` | Prerequisite chain resolution (Coil → Scale → Mystery → Covenant check) | ~180 |
| `MysteryScaleService.cs` | Mystery Scale selection and Crucible Ritual access | ~130 |

### 5.5 `CampaignService.cs` — 496 lines 🟡

**Split into:**

| New File | Responsibility | Lines (est.) |
|----------|---------------|-------------|
| `CampaignService.cs` | Campaign CRUD, character roster | ~200 |
| `CampaignInviteService.cs` | Player invite, accept, decline, kick | ~150 |
| `SessionPrepNoteService.cs` | Session prep notes CRUD | ~100 |

### 5.6 `GhoulManagementService.cs` — 519 lines 🟡

**Split into:**

| New File | Responsibility | Lines (est.) |
|----------|---------------|-------------|
| `GhoulManagementService.cs` | Ghoul CRUD, Discipline access, query | ~250 |
| `GhoulFeedingService.cs` | Feeding roll, Vitae tracking, aging alerts | ~200 |

### 5.7 `KindredLineageService.cs` — 490 lines 🟡

**Split into:**

| New File | Responsibility | Lines (est.) |
|----------|---------------|-------------|
| `KindredLineageService.cs` | Lineage chain traversal, sire lookup | ~250 |
| `BloodSympathyRollService.cs` | Blood Sympathy dice pool resolution (also needed by Phase 18) | ~150 |

> **Note:** Phase 18 (The Wider Web) adds `BloodSympathyService.RollBloodSympathyAsync`. Extracting this now avoids a second refactor later.

### 5.8 `SorceryService.cs` — 486 lines 🟡

**Split into:**

| New File | Responsibility | Lines (est.) |
|----------|---------------|-------------|
| `SorceryService.cs` | Rite learning, prerequisite validation, catalog query | ~230 |
| `SorceryActivationService.cs` | Rite activation, Vitae/Willpower costs, Humanity stains, dice roll | ~200 |

### 5.9 `DbInitializer.cs` — 652 lines 🟡

`DbInitializer` already calls into `SeedData/*.cs` helpers, but the orchestration logic is 652 lines of sequential `await EnsureXxxAsync()` calls mixed with schema initialization.

**Split into:**

| New File | Responsibility |
|----------|---------------|
| `DbInitializer.cs` | Entry point: `InitializeAsync` → delegates only; no seeding logic inline |
| `SchemaInitializer.cs` | Migration + provider detection (≤100 lines) |
| `GameDataSeeder.cs` | Orchestrates all `EnsureXxxAsync()` calls (~200 lines) |

### 5.10 `Program.cs` — 480 lines 🟡

`Program.cs` registers every service, configures OpenTelemetry, Identity, SignalR, rate limiting, Redis, EF Core, and all hosted services inline. This is a common Blazor pattern but the file is difficult to navigate.

**Split using extension methods:**

| New File | Responsibility |
|----------|---------------|
| `Extensions/DataServiceExtensions.cs` | EF Core, Redis, DbContextFactory |
| `Extensions/IdentityServiceExtensions.cs` | ASP.NET Core Identity + cookie auth |
| `Extensions/ApplicationServiceExtensions.cs` | All Application-layer `AddScoped<I, T>()` registrations |
| `Extensions/ObservabilityServiceExtensions.cs` | Serilog, OpenTelemetry, Sentry |
| `Extensions/RateLimitingServiceExtensions.cs` | Rate limiter policies |
| `Program.cs` | `builder.Services.AddXxx()` calls only; each calls one extension | ~80 lines |

---

## ✂️ 6. Large Files — Razor Components

### 6.1 `InitiativeTracker.razor` — 1204 lines 🔴

The single largest file. Contains: initiative entry display, participant add/remove forms, NPC reveal toggles, round advancement, damage tracking, heal panel, attack modals, weapon damage modals, and dice roll display.

**Extract components:**

| New File | Contains | Lines (est.) |
|----------|----------|-------------|
| `Components/Campaigns/Combat/InitiativeEntryRow.razor` | Per-combatant row (HP bar, Willpower, Vitae, action buttons) | ~150 |
| `Components/Campaigns/Combat/AddParticipantPanel.razor` | "Add PC / NPC / Template" form panel | ~120 |
| `Components/Campaigns/Combat/RoundControls.razor` | Round counter, advance-round button, end-encounter button | ~60 |
| `Components/Campaigns/Combat/CombatDamageSummary.razor` | Damage log / roll history strip at the bottom | ~100 |
| `InitiativeTracker.razor` | Orchestration only: DI, state, SignalR listeners, layout shell | ~300 |

### 6.2 `EncounterManager.razor` — 1191 lines 🔴

Contains: encounter creation form, NPC template search and picker, custom NPC stat block entry, initiative seed form, encounter list, and encounter detail.

**Extract components:**

| New File | Contains | Lines (est.) |
|----------|----------|-------------|
| `Components/Campaigns/Combat/EncounterCreateForm.razor` | New encounter form (name, type, scene description) | ~100 |
| `Components/Campaigns/Combat/NpcTemplatePicker.razor` | Template search, filter, and select | ~150 |
| `Components/Campaigns/Combat/CustomNpcStatBlockForm.razor` | Ad-hoc NPC stat block entry | ~150 |
| `Components/Campaigns/Combat/EncounterListPanel.razor` | List of past/active encounters | ~80 |
| `EncounterManager.razor` | Orchestration only | ~250 |

### 6.3 `CampaignDetails.razor` — 947 lines 🟡

Contains: session start/stop, character roster, NPC roster, social maneuvers, factions, and lore notes — each a sizeable feature.

**Extract components:**

| New File | Contains | Lines (est.) |
|----------|----------|-------------|
| `Components/Campaigns/CampaignSessionPanel.razor` | Session start/stop, presence display | ~100 |
| `Components/Campaigns/CampaignRoster.razor` | Player character roster + invite management | ~150 |
| `Components/Campaigns/CampaignNpcRoster.razor` | Chronicle NPC list | ~100 |
| `CampaignDetails.razor` | Tabs/layout shell + state | ~200 |

### 6.4 `CharacterDetails.razor` — 738 lines 🟡

The markup half of the character sheet. Contains: attribute grid, skill grid, disciplines list, merits list, assets list, health track, Vitae panel, conditions list, tilts list, aspirations, banes, social maneuvers.

**Extract leaf components (all already data-driven; low risk):**

| New File | Contains |
|----------|----------|
| `Components/CharacterSheet/CharacterAttributeGrid.razor` | 9-attribute dot-scale grid |
| `Components/CharacterSheet/CharacterSkillList.razor` | Skill list with ratings |
| `Components/CharacterSheet/CharacterDisciplineList.razor` | Disciplines + power list with Activate buttons |
| `Components/CharacterSheet/CharacterAssetList.razor` | Equipment/armor/weapon list |
| `Components/CharacterSheet/CharacterConditionPanel.razor` | Conditions + tilts |

### 6.5 `CharacterDetails.razor.cs` — 1096 lines 🔴

The code-behind is a single partial class with ~90 fields and ~50 event handlers covering every modal, every tab, every SignalR listener, and every save operation on the character sheet.

**Refactor strategy — state object extraction:**

Instead of splitting into sub-pages (high risk), extract modal state into dedicated state objects:

| New File | Contains | Lines (est.) |
|----------|----------|-------------|
| `Pages/CharacterSheet/BloodlineModalState.cs` | Fields + handlers for bloodline apply/approve modals | ~80 |
| `Pages/CharacterSheet/CovenantModalState.cs` | Fields + handlers for covenant apply/leave modals | ~70 |
| `Pages/CharacterSheet/RiteModalState.cs` | Fields + handlers for rite learn/activate modals | ~70 |
| `Pages/CharacterSheet/CoilModalState.cs` | Fields + handlers for coil learn modals | ~60 |
| `Pages/CharacterSheet/DisciplineModalState.cs` | Fields + handlers for discipline advance modals | ~60 |
| `CharacterDetails.razor.cs` | Core lifecycle (`OnParametersSetAsync`, SignalR listeners, core save) | ~400 |

Each state object is injected or instantiated in the main code-behind and exposes `OpenAsync()`, `CloseAsync()`, and the current open/loading/error state. The main `razor.cs` delegates to them.

### 6.6 `CharacterAdvancement.razor` — 561 lines 🟡

**Extract:**

| New File | Contains |
|----------|----------|
| `Components/Advancement/XpSpendPanel.razor` | XP spend flow (attribute / skill / discipline selectors) |
| `Components/Advancement/MeritAdvancementPanel.razor` | Merit add/remove |
| `Components/Advancement/BeatLedger.razor` | Beat/XP history table |

### 6.7 `SocialManeuversSection.razor` — 403 lines 🟡

**Extract:**

| New File | Contains |
|----------|----------|
| `Components/CharacterSheet/SocialManeuverCard.razor` | Single maneuver card (progress, doors, roll button) |
| `Components/CharacterSheet/NewManeuverForm.razor` | New maneuver creation form |

### 6.8 `NpcDetail.razor` — 422 lines 🟡

**Extract:**

| New File | Contains |
|----------|----------|
| `Components/Campaigns/NpcStatBlock.razor` | Read-only stat block display |
| `Components/Campaigns/NpcRelationshipList.razor` | Relationship web section |

### 6.9 `DiceRollerModal.razor` — 407 lines 🟡

**Extract:**

| New File | Contains |
|----------|----------|
| `Components/UI/DicePoolInput.razor` | Pool builder (attribute + skill pickers, modifier field) |
| `Components/UI/RollResultDisplay.razor` | Dice result display (successes, dice breakdown, share button) |

### 6.10 `GlimpseSocialManeuvers.razor` + `SessionClientService.cs` — 326 + 506 lines

`SessionClientService.cs` is an acceptable size for a SignalR state service (it mirrors the server-side hub contract). No split required — document the size as intentional.

`GlimpseSocialManeuvers.razor` (326 lines) is a single-purpose Storyteller view. If it grows past 400 lines, extract the maneuver card as noted in §6.7.

---

## 🗺️ Implementation Priority

### Phase A — Quick wins (1–2 hours each; zero risk)
1. **Bare catch → typed catch + log** in all 7 seeder files and 1 UI file (§2)
2. **`AsSplitQuery()`** on 3 query sites (§3.1) — one-line change per site
3. **`SeedDataLoader` utility** to consolidate seeder JSON loading (§4.1) — new file + update 7 callers

### Phase B — Service splits (½ day each; medium risk)
Order matters — split bottom-up (leaf services first):

4. `SorceryService` → `SorceryActivationService` (§5.8) — needed by Phase 15 refactor of Vitae/Willpower
5. `KindredLineageService` → `BloodSympathyRollService` (§5.7) — unblocks Phase 18
6. `BloodBondService` splits (§5.3)
7. `SocialManeuveringService` splits (§5.1)
8. `EncounterService` splits (§5.2)
9. `CampaignService` splits (§5.5)
10. `GhoulManagementService` splits (§5.6)
11. `CoilService` splits (§5.4)

### Phase C — Infrastructure splits (1 day; low risk)
12. `Program.cs` extension methods (§5.10) — purely additive, all tests still pass
13. `DbInitializer` orchestration extraction (§5.9)

### Phase D — Razor component extraction (1–2 days; higher risk — requires Blazor state threading care)
14. `InitiativeTracker.razor` → combat sub-components (§6.1)
15. `EncounterManager.razor` → encounter sub-components (§6.2)
16. `CharacterDetails.razor.cs` → modal state objects (§6.5) — highest leverage
17. `CharacterDetails.razor` → leaf display components (§6.4)
18. `CampaignDetails.razor` → panel components (§6.3)
19. `CharacterAdvancement.razor` → advancement panels (§6.6)
20. `DiceRollerModal.razor` → input/result components (§6.9)

> **For all Razor splits:** Each extracted component must receive its data via `[Parameter]` or `[CascadingParameter]`. No service injection in leaf display components — services belong only in page-level components and state objects. Run `.\scripts\test-local.ps1` + E2E suite after each split.

---

## 📂 New Files Created by This Plan

| File | Category |
|------|---------|
| `RequiemNexus.Data/SeedData/SeedDataLoader.cs` | Utility |
| `RequiemNexus.Web/Extensions/DataServiceExtensions.cs` | Program split |
| `RequiemNexus.Web/Extensions/IdentityServiceExtensions.cs` | Program split |
| `RequiemNexus.Web/Extensions/ApplicationServiceExtensions.cs` | Program split |
| `RequiemNexus.Web/Extensions/ObservabilityServiceExtensions.cs` | Program split |
| `RequiemNexus.Web/Extensions/RateLimitingServiceExtensions.cs` | Program split |
| `RequiemNexus.Application/Services/SocialManeuverRollService.cs` | Service split |
| `RequiemNexus.Application/Services/SocialManeuverConditionService.cs` | Service split |
| `RequiemNexus.Application/Services/EncounterParticipantService.cs` | Service split |
| `RequiemNexus.Application/Services/EncounterInitiativeService.cs` | Service split |
| `RequiemNexus.Application/Services/BloodBondConditionService.cs` | Service split |
| `RequiemNexus.Application/Services/BloodBondFadingService.cs` | Service split |
| `RequiemNexus.Application/Services/CoilPrerequisiteService.cs` | Service split |
| `RequiemNexus.Application/Services/MysteryScaleService.cs` | Service split |
| `RequiemNexus.Application/Services/CampaignInviteService.cs` | Service split |
| `RequiemNexus.Application/Services/SessionPrepNoteService.cs` | Service split |
| `RequiemNexus.Application/Services/GhoulFeedingService.cs` | Service split |
| `RequiemNexus.Application/Services/BloodSympathyRollService.cs` | Service split + Phase 18 prep |
| `RequiemNexus.Application/Services/SorceryActivationService.cs` | Service split |
| `RequiemNexus.Data/SchemaInitializer.cs` | DbInitializer split |
| `RequiemNexus.Data/GameDataSeeder.cs` | DbInitializer split |
| `RequiemNexus.Web/Components/UI/ModalTriggerButton.razor` | Razor component |
| `RequiemNexus.Web/Components/UI/VitalsRow.razor` | Razor component |
| `RequiemNexus.Web/Components/UI/DicePoolInput.razor` | Razor component |
| `RequiemNexus.Web/Components/UI/RollResultDisplay.razor` | Razor component |
| `RequiemNexus.Web/Components/Campaigns/Combat/InitiativeEntryRow.razor` | Razor component |
| `RequiemNexus.Web/Components/Campaigns/Combat/AddParticipantPanel.razor` | Razor component |
| `RequiemNexus.Web/Components/Campaigns/Combat/RoundControls.razor` | Razor component |
| `RequiemNexus.Web/Components/Campaigns/Combat/CombatDamageSummary.razor` | Razor component |
| `RequiemNexus.Web/Components/Campaigns/Combat/EncounterCreateForm.razor` | Razor component |
| `RequiemNexus.Web/Components/Campaigns/Combat/NpcTemplatePicker.razor` | Razor component |
| `RequiemNexus.Web/Components/Campaigns/Combat/CustomNpcStatBlockForm.razor` | Razor component |
| `RequiemNexus.Web/Components/Campaigns/Combat/EncounterListPanel.razor` | Razor component |
| `RequiemNexus.Web/Components/Campaigns/CampaignSessionPanel.razor` | Razor component |
| `RequiemNexus.Web/Components/Campaigns/CampaignRoster.razor` | Razor component |
| `RequiemNexus.Web/Components/Campaigns/CampaignNpcRoster.razor` | Razor component |
| `RequiemNexus.Web/Components/CharacterSheet/CharacterAttributeGrid.razor` | Razor component |
| `RequiemNexus.Web/Components/CharacterSheet/CharacterSkillList.razor` | Razor component |
| `RequiemNexus.Web/Components/CharacterSheet/CharacterDisciplineList.razor` | Razor component |
| `RequiemNexus.Web/Components/CharacterSheet/CharacterAssetList.razor` | Razor component |
| `RequiemNexus.Web/Components/CharacterSheet/CharacterConditionPanel.razor` | Razor component |
| `RequiemNexus.Web/Components/CharacterSheet/SocialManeuverCard.razor` | Razor component |
| `RequiemNexus.Web/Components/CharacterSheet/NewManeuverForm.razor` | Razor component |
| `RequiemNexus.Web/Components/CharacterSheet/BeatLedger.razor` | Razor component |
| `RequiemNexus.Web/Pages/CharacterSheet/BloodlineModalState.cs` | Code-behind state |
| `RequiemNexus.Web/Pages/CharacterSheet/CovenantModalState.cs` | Code-behind state |
| `RequiemNexus.Web/Pages/CharacterSheet/RiteModalState.cs` | Code-behind state |
| `RequiemNexus.Web/Pages/CharacterSheet/CoilModalState.cs` | Code-behind state |
| `RequiemNexus.Web/Pages/CharacterSheet/DisciplineModalState.cs` | Code-behind state |

---

> _"The blood is the life… but clarity is the power."_
