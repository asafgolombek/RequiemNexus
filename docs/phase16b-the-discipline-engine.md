# Phase 16b: The Discipline Engine — Power Activation

**Status: complete** (see [`mission.md`](./mission.md) Phase 16b section).

Companion to [`mission.md`](./mission.md). This document is the implementation record for Phase 16b.

**Delivered:** Execution groups A–D are complete in the repository (Domain `ActivationCost`, Application `DisciplineActivationService`, Web activation UI, tests, DI). **D3** — the six Phase 16b bullets live under `## Phase 16b` in [`rules-interpretations.md`](./rules-interpretations.md). The remainder of this file preserves the original plan as Grimoire (file paths, steps, and test matrix).

---

## Objective

Activate Discipline powers with typed cost enforcement and dice-pool resolution. Phase 19 delivered `DisciplinePower.PoolDefinitionJson` — Phase 16b consumes it.

**Exit criteria:**
- Characters can activate Discipline powers from the character sheet
- Vitae / Willpower costs are deducted atomically before the roll
- Pool is resolved via `ITraitResolver` (full modifiers) and published to the dice feed
- Powers with `null PoolDefinitionJson` remain display-only (no Activate button)
- All targeted tests pass (`DisciplineActivationServiceTests`, `ActivationCostTests`); `dotnet build` and `dotnet format` are clean (`scripts/test-local.ps1`)

---

## Dependency & Context

- **Unblocked by:** Phase 19 (`DisciplinePower.PoolDefinitionJson` column + seed data)
- **Parallel track:** Phase 17 (Humanity & Conditions) — no code conflict; see Coordination note
- **Primary reference implementation:** `SorceryActivationService` / `ISorceryActivationService` — follow this pattern exactly
- **Test scaffold reference:** `SorceryServiceTests.cs` — use the same `CreateSqliteContextAsync` / `SqliteTeardown` pattern

---

## Architectural Decisions

- **Activation is a wrapper around `ITraitResolver`.** `DisciplineActivationService` reads `PoolDefinitionJson`, calls `TraitResolver.ResolvePoolAsync` (full modifiers), deducts cost via `VitaeService` / `WillpowerService`, and posts to dice feed.
- **Cost deduction is atomic.** Vitae and Willpower spends go through their respective services inside an EF transaction — same pattern as rite activation.
- **Powers with `null PoolDefinitionJson` are display-only.** The Activate button is suppressed in the UI; `ActivatePowerAsync` throws `InvalidOperationException` as a safety net.
- **`ActivationCost` lives in Domain.** String parsing of `DisciplinePower.Cost` (`"1 Vitae"`, `"1 Willpower"`, `"—"`) is encapsulated in a value object — the service layer never does raw string matching.
- **Authorization: `RequireCharacterAccessAsync`.** Owner or Storyteller may activate, consistent with in-play character actions (`HuntingService`, `FrenzyService`, `VitaeService`). Note: `SorceryActivationService` uses the narrower `RequireCharacterOwnerAsync` — rites are player-only; discipline activation intentionally extends to ST for NPC and in-session use.
- **`"1 Vitae or 1 Willpower"` defaults to Vitae.** Player choice UI is deferred to Phase 18 content pass (recorded in rules log).

---

## Execution Order

Groups must be completed in order (each unlocks the next):

```
A (Domain VO) → B (Application Service) → C (UI) → D (Tests + Rules Log)
```

---

## Group A — Domain Value Object

**Purpose:** Typed cost representation; isolates string parsing from business logic.

### A1. `src/RequiemNexus.Domain/Models/ActivationCostType.cs` *(new)*

```csharp
namespace RequiemNexus.Domain.Models;

public enum ActivationCostType
{
    None,
    Vitae,
    Willpower,
}
```

### A2. `src/RequiemNexus.Domain/Models/ActivationCost.cs` *(new)*

```csharp
namespace RequiemNexus.Domain.Models;

/// <summary>
/// Typed representation of a Discipline power's activation cost.
/// Parsed from the seeded <c>Cost</c> string on a discipline power (e.g. "1 Vitae", "1 Willpower", "—").
/// </summary>
public sealed record ActivationCost(ActivationCostType Type, int Amount)
{
    /// <summary>Zero-cost power ("—", empty, or null).</summary>
    public static readonly ActivationCost None = new(ActivationCostType.None, 0);

    public bool IsNone => Type == ActivationCostType.None;

    /// <summary>
    /// Parses a cost string such as "1 Vitae", "2 Vitae", "1 Willpower",
    /// "1 Vitae or 1 Willpower", "—", or empty/null.
    /// Returns <see cref="None"/> for unrecognised or empty strings.
    /// </summary>
    public static ActivationCost Parse(string? costString)
    {
        if (string.IsNullOrWhiteSpace(costString))
            return None;

        var trimmed = costString.Trim().TrimStart('—', '-', '–');
        if (string.IsNullOrWhiteSpace(trimmed))
            return None;

        // Handle "N Vitae or N Willpower" — default to Vitae (see rules-interpretations.md)
        var parts = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
            return None;

        int amount = int.TryParse(parts[0], out var parsed) ? parsed : 1;
        return parts[1].ToLowerInvariant() switch
        {
            "vitae" => new ActivationCost(ActivationCostType.Vitae, amount),
            "willpower" => new ActivationCost(ActivationCostType.Willpower, amount),
            _ => None,
        };
    }
}
```

**Parse rules:**
- `null`, `""`, `"—"`, `"–"`, `"-"` → `None`
- `"1 Vitae"` → `Vitae, 1`; `"2 Vitae"` → `Vitae, 2`
- `"1 Willpower"` → `Willpower, 1`
- `"1 Vitae or 1 Willpower"` → `Vitae, 1` (first token wins; see rules log)
- Unknown type word → `None` (service logs Warning)

---

## Group B — Application Service

**Purpose:** Core activation logic following the `SorceryActivationService` pattern.

### B1. `src/RequiemNexus.Application/Contracts/IDisciplineActivationService.cs` *(new)*

```csharp
namespace RequiemNexus.Application.Contracts;

/// <summary>
/// Handles Discipline power activation: pool resolution, cost deduction, and dice-feed publication.
/// Separated from <see cref="ICharacterDisciplineService"/> (XP acquisition) to keep each contract focused.
/// </summary>
public interface IDisciplineActivationService
{
    /// <summary>
    /// Resolves the dice pool for a power WITHOUT spending resources.
    /// Returns 0 when the power has no <c>PoolDefinitionJson</c>.
    /// Uses sync pool resolution (no passive modifiers) — preview only.
    /// </summary>
    Task<int> ResolveActivationPoolAsync(int characterId, int disciplinePowerId, string userId);

    /// <summary>
    /// Deducts the activation cost, resolves the full pool (with passive modifiers),
    /// publishes to the dice feed, and returns the resolved pool size.
    /// Throws <see cref="InvalidOperationException"/> when resources are insufficient,
    /// the power has no <c>PoolDefinitionJson</c>, or the character lacks the required rating.
    /// </summary>
    Task<int> ActivatePowerAsync(int characterId, int disciplinePowerId, string userId);
}
```

### B2. `src/RequiemNexus.Application/Services/DisciplineActivationService.cs` *(new)*

**Constructor (C# 14 primary constructor):**

```csharp
public sealed class DisciplineActivationService(
    ApplicationDbContext dbContext,
    IAuthorizationHelper authHelper,
    ITraitResolver traitResolver,
    IVitaeService vitaeService,
    IWillpowerService willpowerService,
    ISessionService sessionService,
    ILogger<DisciplineActivationService> logger) : IDisciplineActivationService
```

**Key implementation notes:**

- `_jsonOptions`: own static instance (same shape as `SorceryActivationService` — do not share)
- `ResolveActivationPoolAsync`:
  1. `await authHelper.RequireCharacterAccessAsync(characterId, userId, "resolve discipline pool")`
  2. Load character with `Include(c => c.Disciplines).Include(c => c.Attributes).Include(c => c.Skills)`
  3. Load `DisciplinePower` by `disciplinePowerId`; verify eligibility (`CharacterHasPowerEligibility`)
  4. If `power.PoolDefinitionJson` is null/empty → return `0` (no throw — preview path)
  5. `JsonSerializer.Deserialize<PoolDefinition>` → `traitResolver.ResolvePool(character, pool)` (sync)
  6. Wrap in try/catch; log **Error** with structured fields (CharacterId, PowerId) and return `0` on malformed JSON — matching `SorceryActivationService` severity for operability
- `ActivatePowerAsync`:
  1. `await authHelper.RequireCharacterAccessAsync(characterId, userId, "activate discipline power")`
  2. Load character with includes
  3. Load `DisciplinePower`; enforce eligibility → throw `InvalidOperationException` if not eligible
  4. Guard null `PoolDefinitionJson` → throw `InvalidOperationException("This power has no rollable pool.")`
  5. `ActivationCost cost = ActivationCost.Parse(power.Cost)` — log Warning if type is `None` but cost string is non-empty/non-dash
  6. Deduct cost inside `await using var tx = await dbContext.Database.BeginTransactionAsync()`:
     - Vitae: `await vitaeService.SpendVitaeAsync(characterId, userId, cost.Amount, $"Discipline: {power.Name}")`
     - Willpower: `await willpowerService.SpendWillpowerAsync(characterId, userId, cost.Amount)`
     - Check `Result.IsSuccess`; on failure throw `InvalidOperationException(result.Error)`
     - `await tx.CommitAsync()`
  7. Reload character (fresh query) to get post-spend trait values
  8. `JsonSerializer.Deserialize<PoolDefinition>` → `await traitResolver.ResolvePoolAsync(character, pool)` (async, full modifiers)
  9. Log structured event (CharacterId, PowerId, PowerName, CostType, CostAmount, PoolSize)
  10. If `!cost.IsNone`: `await sessionService.BroadcastCharacterUpdateAsync(characterId)`
  11. Return `poolSize`
  - **Do not call `PublishDiceRollAsync` or `RollDiceAsync` from the service.** The dice roll and dice-feed publication are performed by `DiceRollerModal` in the code-behind when it opens with the returned pool size. This matches `SorceryActivationService.BeginRiteActivationAsync` exactly.
- `CharacterHasPowerEligibility` (private):
  - `character.Disciplines.FirstOrDefault(d => d.DisciplineId == power.DisciplineId)?.Rating >= power.Level`

### B3. DI registration — `src/RequiemNexus.Web/Extensions/ApplicationServiceExtensions.cs` *(modify)*

Add one line adjacent to the `SorceryActivationService` registration:

```csharp
services.AddScoped<IDisciplineActivationService, DisciplineActivationService>();
```

---

## Group C — UI

**Purpose:** Activate button on character sheet powers; cost-preview confirm modal.

### C1. `src/RequiemNexus.Web/Components/Pages/CharacterDetails.razor` *(modify)*

In the `@foreach` discipline powers loop — add conditional Activate button after the pool/cost display:

```razor
@if (!string.IsNullOrEmpty(power.PoolDefinitionJson))
{
    <button class="btn-rn-ghost btn-roll-small"
            @onclick="() => OpenDisciplinePowerActivateModal(power)">
        Activate
    </button>
}
```

At the bottom of the file (adjacent to existing modals), add:

```razor
<DisciplinePowerActivateModal
    IsOpen="_isDisciplineActivateModalOpen"
    IsOpenChanged="(v) => _isDisciplineActivateModalOpen = v"
    Power="_disciplineActivatePower"
    ResolvedPool="_disciplineActivatePool"
    OnConfirmed="HandleDisciplineActivateConfirmedAsync" />
```

### C2. `src/RequiemNexus.Web/Components/Pages/CharacterDetails.razor.cs` *(modify)*

Inject service:
```csharp
[Inject]
private IDisciplineActivationService DisciplineActivationService { get; set; } = default!;
```

New state fields:
```csharp
private bool _isDisciplineActivateModalOpen = false;
private DisciplinePower? _disciplineActivatePower;
private int _disciplineActivatePool = 0;
```

New `OpenDisciplinePowerActivateModal`:
```csharp
private async Task OpenDisciplinePowerActivateModal(DisciplinePower power)
{
    if (_character == null || string.IsNullOrEmpty(_currentUserId))
        return;
    try
    {
        _disciplineActivatePower = power;
        _disciplineActivatePool = await DisciplineActivationService
            .ResolveActivationPoolAsync(_character.Id, power.Id, _currentUserId);
        _isDisciplineActivateModalOpen = true;
    }
    catch (Exception ex)
    {
        ToastService.Show("Discipline", ex.Message, ToastType.Error);
    }
}
```

New `HandleDisciplineActivateConfirmedAsync`:
```csharp
private async Task HandleDisciplineActivateConfirmedAsync()
{
    if (_character == null || _disciplineActivatePower == null || string.IsNullOrEmpty(_currentUserId))
        return;
    _isDisciplineActivateModalOpen = false;
    try
    {
        int dice = await DisciplineActivationService
            .ActivatePowerAsync(_character.Id, _disciplineActivatePower.Id, _currentUserId);
        _character = await CharacterService.ReloadCharacterAsync(_character.Id, _currentUserId);
        await ResolveDisciplinePowerPoolsAsync();
        _rollerTraitName = _disciplineActivatePower.Name;
        _rollerBaseDice = dice;
        _rollerFixedDicePool = null;
        _isRollerOpen = true;
    }
    catch (Exception ex)
    {
        ToastService.Show("Discipline", ex.Message, ToastType.Error);
        _character = await CharacterService.ReloadCharacterAsync(_character.Id, _currentUserId);
    }
}
```

**Design note:** Cost is paid in `ActivatePowerAsync`; the existing `DiceRollerModal` is then opened with the resolved fixed pool — matching the rite activation flow. No new rolling modal needed.

### C3. `src/RequiemNexus.Web/Components/UI/DisciplinePowerActivateModal.razor` *(new)*

Presentational only — no service injection. Parameters: `IsOpen`, `IsOpenChanged`, `Power` (`DisciplinePower?`), `ResolvedPool` (`int`), `OnConfirmed` (`EventCallback`).

Shows:
- Power name + level
- Pool count (`"Chance die"` when 0)
- Cost description (from `Power.Cost`) + deduction warning when non-free
- "Cancel" and "Confirm & Roll" buttons
- ARIA: `role="dialog"`, `aria-modal="true"`, `aria-labelledby`

---

## Group D — Tests & Rules Log

### D1. `tests/RequiemNexus.Application.Tests/DisciplineActivationServiceTests.cs` *(new)*

Use the `CreateSqliteContextAsync` / `SqliteTeardown` scaffold from `SorceryServiceTests.cs`.

| # | Test name | Asserts |
|---|-----------|---------|
| 1 | `ActivatePowerAsync_VitaeCost_DeductsVitaeAndReturnsPool` | Vitae decremented by cost amount; pool returned |
| 2 | `ActivatePowerAsync_WillpowerCost_DeductsWillpowerAndReturnsPool` | Willpower decremented; pool returned |
| 3 | `ActivatePowerAsync_NoCost_DoesNotDeductResourcesAndReturnsPool` | Cost `"—"` → Vitae and Willpower unchanged |
| 4 | `ActivatePowerAsync_InsufficientVitae_ThrowsInvalidOperationException` | `CurrentVitae = 0`, cost `"1 Vitae"` → throws |
| 5 | `ActivatePowerAsync_InsufficientWillpower_ThrowsInvalidOperationException` | `CurrentWillpower = 0`, cost `"1 Willpower"` → throws |
| 6 | `ActivatePowerAsync_NullPoolDefinitionJson_ThrowsInvalidOperationException` | `power.PoolDefinitionJson = null` → throws with "no rollable pool" |
| 7 | `ActivatePowerAsync_CharacterLacksRequiredRating_ThrowsInvalidOperationException` | Character has discipline at Rating 1, power is Level 2 → throws |
| 8 | `ResolveActivationPoolAsync_NullPool_ReturnsZero` | `power.PoolDefinitionJson = null` → returns 0, no throw |
| 9 | `ActivatePowerAsync_UsesResolvePoolAsyncForFinalPool` | Activation uses `ResolvePoolAsync` (modifier-aware final pool) |
| 10 | `ResolveActivationPoolAsync_UsesSyncResolvePoolOnly` | Preview uses `ResolvePool` only; `ResolvePoolAsync` never called |
| 11 | `ActivatePowerAsync_NonZeroCost_BroadcastsCharacterUpdate` | Non-free cost → `BroadcastCharacterUpdateAsync` called |
| 12 | `ActivatePowerAsync_ZeroCost_DoesNotBroadcastCharacterUpdate` | Cost `"—"` → `BroadcastCharacterUpdateAsync` not called |

### D2. `tests/RequiemNexus.Domain.Tests/ActivationCostTests.cs` *(new)*

Unit tests for `ActivationCost.Parse`:

| Input | Expected |
|-------|---------|
| `"1 Vitae"` | `Vitae, 1` |
| `"2 Vitae"` | `Vitae, 2` |
| `"1 Willpower"` | `Willpower, 1` |
| `"1 Vitae or 1 Willpower"` | `Vitae, 1` |
| `"—"` | `None` |
| `""` | `None` |
| `null` | `None` |
| `"1 VITAE"` | `Vitae, 1` (case-insensitive) |
| `"1 Rouse"` | `None` (unknown type) |

### D3. Append to `docs/rules-interpretations.md`

Add a `## Phase 16b — The Discipline Engine (power activation)` section with these 6 entries:

1. **`"1 Vitae or 1 Willpower"` cost string** — `ActivationCost.Parse` defaults to `Vitae`. Player-choice UI deferred to Phase 18 content pass.
2. **Powers with `null PoolDefinitionJson` are display-only** — Activate button suppressed; `ActivatePowerAsync` throws as safety net. Phase 18 content pass populates pools for all rollable powers.
3. **Modifier-aware vs. sync pool resolution** — `ResolveActivationPoolAsync` uses `traitResolver.ResolvePool` (sync, no modifiers) for the cost-preview modal. `ActivatePowerAsync` uses `traitResolver.ResolvePoolAsync` (async, full modifiers). Preview may differ from final pool by the sum of active passive modifiers — acceptable.
4. **Cost deduction ordering (before roll)** — VtR 2e: resources are spent before the roll is made. Cost is deducted inside a transaction before pool resolution. On malformed JSON failure after a committed transaction, cost remains spent — correct per the rules.
5. **Eligibility check: discipline must be at or above power level** — Character with Vigor 2 cannot activate a Vigor 3 power. Enforced via `CharacterDiscipline.Rating >= power.Level`. Explicit in VtR 2e core (p. 112).
6. **`BroadcastCharacterUpdateAsync` gating** — Called only when `!cost.IsNone`. Free powers do not change Vitae/Willpower; broadcasting an unchanged snapshot adds unnecessary SignalR traffic. Dice feed broadcast fires regardless.

---

## Files to Create

| File | Layer | Purpose |
|------|-------|---------|
| `src/RequiemNexus.Domain/Models/ActivationCostType.cs` | Domain | Enum |
| `src/RequiemNexus.Domain/Models/ActivationCost.cs` | Domain | Value object + parser |
| `src/RequiemNexus.Application/Contracts/IDisciplineActivationService.cs` | Application | Contract |
| `src/RequiemNexus.Application/Services/DisciplineActivationService.cs` | Application | Implementation |
| `src/RequiemNexus.Web/Components/UI/DisciplinePowerActivateModal.razor` | Web | Confirm modal (presentational) |
| `tests/RequiemNexus.Application.Tests/DisciplineActivationServiceTests.cs` | Tests | 12 service tests |
| `tests/RequiemNexus.Domain.Tests/ActivationCostTests.cs` | Tests | 9 parse unit tests |

## Files to Modify

| File | Change |
|------|--------|
| `src/RequiemNexus.Web/Components/Pages/CharacterDetails.razor` | Add conditional Activate button + modal declaration |
| `src/RequiemNexus.Web/Components/Pages/CharacterDetails.razor.cs` | Inject `IDisciplineActivationService`; add open/confirm handlers + 3 state fields |
| `src/RequiemNexus.Web/Extensions/ApplicationServiceExtensions.cs` | Register `IDisciplineActivationService` scoped |
| `docs/rules-interpretations.md` | Append Phase 16b section (6 entries) |

---

## Key Reference Files

| File | Why |
|------|-----|
| `src/RequiemNexus.Application/Services/SorceryActivationService.cs` | Primary pattern: transaction, dice feed, broadcast, logger |
| `src/RequiemNexus.Application/Contracts/ISorceryActivationService.cs` | Contract shape reference |
| `tests/RequiemNexus.Application.Tests/SorceryServiceTests.cs` | SQLite test scaffold to copy |
| `src/RequiemNexus.Web/Components/Pages/CharacterDetails.razor` (lines ~288–313) | Discipline powers loop location |
| `src/RequiemNexus.Domain/Models/PoolDefinition.cs` | Deserialization target for `PoolDefinitionJson` |

---

## Verification

1. `dotnet build` — zero warnings (`/warnaserror`)
2. `.\scripts\test-local.ps1` — all tests pass including `DisciplineActivationServiceTests` and `ActivationCostTests`
3. **Manual smoke:**
   - Open a character with Vigor; expand a power that has `PoolDefinitionJson` populated
   - Activate button is visible; click → modal shows pool count and cost description
   - Confirm → Vitae / Willpower deducted, dice feed entry appears, character sheet updates
4. Powers with `null PoolDefinitionJson` — no Activate button rendered
5. Insufficient resources → toast error, no resource deducted, no dice roll, character state unchanged

---

## Coordination — Phase 17 Parallel Track

Phase 17 adds `ConditionModifierSource` to `ModifierService`, which enriches `ITraitResolver.ResolvePoolAsync`. Phase 16b calls the same interface — condition penalties automatically apply to discipline activation once Phase 17 ships. **No Phase 16b code changes needed when Phase 17 merges.**

Only shared file: `ApplicationServiceExtensions.cs`. Two independent `AddScoped` lines — resolve any merge conflict with standard Git tooling. If merging both on the same day, merge Phase 17 first so activation immediately benefits from condition modifiers.
