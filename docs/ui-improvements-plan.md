# 🩸 UI/UX Improvements Plan

> This document is the authoritative plan for standalone visual and interaction improvements to the Requiem Nexus web application. These changes are **independent of Phase 7 realtime features** — they can be developed in any order, and in parallel with SignalR work.
>
> For UI components that require an active SignalR session (presence bar, roll history feed, live vitals), see `docs/plan.md` **Area 12**.

---

## Architectural Constraints

- No new NuGet packages — all improvements use pure CSS, Blazor component patterns, and minimal JS interop via the existing `wwwroot/app.js`
- No business logic enters the Web layer — all new code is presentation only
- `ToastService` and `CommandPaletteService` are Web-layer singletons (not Application layer services)
- All drag-and-drop interactions (Wave 6) are purely UI ordering — no domain invariants are touched
- Every new type lives in its own `.cs` / `.razor` file (Rule of One)

---

## Implementation Order

| Wave | What | New Files | Key Modified Files |
|------|------|-----------|--------------------|
| 1a | Toast system | `ToastService.cs`, `ToastContainer.razor(.css)` | `Program.cs`, `MainLayout.razor` |
| 1b | Micro-interactions | — | `app.css`, `app.js` |
| 1c | Skeleton loader | `SkeletonLoader.razor(.css)` | Character sheet pages, campaign list |
| 2 | Atmosphere | — | `MainLayout.razor`, `MainLayout.razor.css`, `app.css` |
| 3 | Dice roll wow | — | `DiceRollerModal.razor`, `DiceRollerModal.razor.css`, `app.js` |
| 4 | Character sheet | — | `CharacterVitals.razor(.css)`, `DotScale.razor.css`, `app.css` |
| 5 | Auth form polish | `ManageLayout.razor.css` | `Login.razor.css`, `ManageLayout.razor`, `ConfirmEmail.razor`, `app.css` |
| 6 | Initiative tracker | `InitiativeTracker.razor.css` | `InitiativeTracker.razor` |
| 7 | Command palette | `CommandPalette.razor(.css)`, `CommandPaletteService.cs` | `Program.cs`, `MainLayout.razor`, `app.js` |

> **Cross-plan dependency:** Wave 1a must be complete before Phase 7 Area 12c (realtime roll toasts) can be wired.

---

## Wave 1 — Foundation

### 1a. Toast Notification System

**New files:**
- `src/RequiemNexus.Web/Services/ToastService.cs`
- `src/RequiemNexus.Web/Components/UI/ToastContainer.razor`
- `src/RequiemNexus.Web/Components/UI/ToastContainer.razor.css`

**`ToastService`** — Web-layer singleton:
- `Show(string title, string message, ToastType type, int durationMs = 3000)`
- `ToastType` enum: `Success`, `Error`, `Warning`, `Info`
- Maintains an internal `List<ToastItem>` and exposes `OnToastsChanged` event for the container to re-render
- `ToastItem` record: `Id` (Guid), `Title`, `Message`, `Type`, `DurationMs`, `CreatedAt`
- Register in `Program.cs` as `builder.Services.AddSingleton<ToastService>()`

**`ToastContainer`** — renders the floating stack:
- Fixed top-right position (`position: fixed; top: 1.5rem; right: 1.5rem; z-index: 9999`)
- Subscribes to `ToastService.OnToastsChanged` in `OnInitialized`, unsubscribes in `Dispose`
- Each toast auto-dismisses after `DurationMs` via `System.Timers.Timer`
- Stack: newest on top, max 4 visible simultaneously (oldest auto-removed if stack full)

**Visual design:**
- Charcoal (`var(--charcoal)`) background with `backdrop-filter: blur(8px)`
- Crimson left border (4px) for Error; `var(--success-emerald)` for Success; `var(--warning-amber)` for Warning; `var(--muted-gray)` for Info
- Title in Cinzel, message in Raleway, body text `var(--bone-white)`
- Progress bar: thin bar at toast bottom depletes `width: 100% → 0%` over `DurationMs` via CSS animation
- Entry: slide in from right (`translateX(120%) → translateX(0)`, 0.25s ease-out)
- Exit: fade out + slide right (`opacity: 1 → 0`, `translateX(0) → translateX(120%)`, 0.2s)
- Add `<ToastContainer />` to `MainLayout.razor`

---

### 1b. Micro-Interactions

All changes in `wwwroot/app.css` and `wwwroot/app.js`.

**Crimson focus rings:**
```css
:focus-visible {
    outline: 2px solid var(--crimson);
    outline-offset: 2px;
    box-shadow: 0 0 0 4px rgba(139, 0, 0, 0.2);
    transition: box-shadow 0.2s ease;
}
```

**Button ripple** (`.btn-primary`, `.btn-login`, `.btn-secondary`):
- `position: relative; overflow: hidden` on button
- `::after` pseudo-element: circle, `scale(0)`, `opacity: 0.3`, crimson background
- On click: JS adds `.ripple-active` class; CSS transitions `scale(0 → 2.5)` + `opacity(0.3 → 0)` over 0.5s
- `app.js` snippet: attach `click` listener to all buttons on `DOMContentLoaded`, position `::after` at click coordinates via CSS custom properties `--ripple-x`, `--ripple-y`

**Long-press confirm** (`.btn-danger-hold`):
- `::before` fills from `width: 0% → 100%` (crimson) over 1.5s on `mousedown`/`touchstart`
- `mouseup`/`touchend` before completion cancels (`animation-play-state: paused`, reset)
- Fires the button's `click` event only on completion (JS `animationend` listener)
- Use on: Delete Account, Retire Character, Delete Campaign, Leave Campaign — replace inline confirm dialogs

**Input focus glow normalization:**
- Ensure all `.input-container:focus-within` rules use a consistent `box-shadow` transition of `0.4s ease`
- Unify across `Login.razor.css`, `app.css` global `.input-grimoire` — one canonical definition in `app.css`

---

### 1c. Skeleton Loading Component

**New files:**
- `src/RequiemNexus.Web/Components/UI/SkeletonLoader.razor`
- `src/RequiemNexus.Web/Components/UI/SkeletonLoader.razor.css`

**Parameters:**
- `Variant`: `"rows"` (default) | `"card"` | `"sheet"`
- `Count`: number of rows (for `"rows"` variant, default 3)

**Animation:**
```css
@keyframes shimmer {
    0% { background-position: -200% 0; }
    100% { background-position: 200% 0; }
}
.skeleton-block {
    background: linear-gradient(90deg, var(--charcoal) 25%, var(--ash-gray) 50%, var(--charcoal) 75%);
    background-size: 200% 100%;
    animation: shimmer 1.6s infinite linear;
    border-radius: 4px;
}
```

**Usage:** Replace `<LoadingSpinner />` in:
- Character sheet tab panels while data loads
- Campaign list while campaigns are fetched
- Character roster on the Home dashboard

---

## Wave 2 — Atmosphere

All changes in `wwwroot/app.css`, `Components/Layout/MainLayout.razor`, and `Components/Layout/MainLayout.razor.css`.

### 2a. Drifting Particle System (pure CSS, zero JS)

Add to `MainLayout.razor` inside the layout wrapper, before `@Body`:
```razor
<div class="particles" aria-hidden="true">
    @for (int i = 1; i <= 12; i++)
    {
        <span class="particle particle-@i"></span>
    }
</div>
```

CSS in `MainLayout.razor.css`:
```css
.particles { position: fixed; inset: 0; pointer-events: none; z-index: 0; overflow: hidden; }

.particle {
    position: absolute;
    bottom: -10px;
    width: 3px;
    height: 3px;
    border-radius: 50%;
    background: var(--bone-white);
    animation: drift linear infinite;
}

@keyframes drift {
    0%   { transform: translateY(0) translateX(0); opacity: 0; }
    10%  { opacity: var(--p-opacity, 0.05); }
    50%  { transform: translateY(-50vh) translateX(var(--p-sway, 20px)); }
    90%  { opacity: var(--p-opacity, 0.05); }
    100% { transform: translateY(-110vh) translateX(0); opacity: 0; }
}
```

Each `.particle-N` sets individual values:
```css
.particle-1  { left: 8%;  --p-opacity: 0.04; animation-duration: 22s; animation-delay: 0s;   width: 2px; height: 2px; }
.particle-2  { left: 17%; --p-opacity: 0.06; animation-duration: 31s; animation-delay: 3s;   --p-sway: -15px; }
/* ... 12 total, varied positions, durations (15–40s), delays (0–20s), sizes (2–4px) */
```

### 2b. Deepen Card Hover Glow

Normalize across all card types in `app.css`:
```css
.campaign-card:hover,
.character-card:hover,
.widget-card:hover {
    transform: translateY(-3px);
    box-shadow: 0 12px 40px rgba(0, 0, 0, 0.5), 0 0 30px rgba(139, 0, 0, 0.25);
    transition: transform 0.3s ease, box-shadow 0.4s ease;
}
```

### 2c. Viewport Bottom Fog Layer

In `app.css` on the `.grimoire-bg` element:
```css
.grimoire-bg::after {
    content: '';
    position: fixed;
    bottom: 0;
    left: 0;
    right: 0;
    height: 120px;
    background: linear-gradient(to top, rgba(0, 0, 0, 0.45) 0%, transparent 100%);
    pointer-events: none;
    z-index: 1;
}
```

---

## Wave 3 — Dice Roll Wow

Target files: `Components/UI/DiceRollerModal.razor`, `DiceRollerModal.razor.css`, `wwwroot/app.js`

### 3a. Per-Die Staggered Cascade Reveal

In the `@foreach` loop over `_lastResult.DiceRolled`, add a loop index:
```razor
@{ int dieIndex = 0; }
@foreach (var die in _lastResult.DiceRolled)
{
    <span class="die-result @(die >= 8 ? "success-die" : "fail-die")"
          style="--die-index: @dieIndex">@die</span>
    dieIndex++;
}
```

CSS:
```css
.die-result {
    opacity: 0;
    transform: translateY(-20px);
    animation: dieLand 0.35s cubic-bezier(0.34, 1.56, 0.64, 1) forwards;
    animation-delay: calc(var(--die-index) * 80ms);
}

@keyframes dieLand {
    to { opacity: 1; transform: translateY(0); }
}
```

### 3b. Success Counter Odometer

Add to `wwwroot/app.js`:
```js
window.countUp = function (elementId, target, duration) {
    const el = document.getElementById(elementId);
    if (!el) return;
    const start = performance.now();
    const step = (timestamp) => {
        const progress = Math.min((timestamp - start) / duration, 1);
        el.textContent = Math.floor(progress * target);
        if (progress < 1) requestAnimationFrame(step);
        else el.textContent = target;
    };
    requestAnimationFrame(step);
};
```

In `DiceRollerModal.razor` `@code`:
```csharp
private string _successCountId = $"success-count-{Guid.NewGuid():N}";

// after roll completes:
await JS.InvokeVoidAsync("countUp", _successCountId, _lastResult.Successes, 600);
```

In markup: `<span id="@_successCountId" class="success-count">0</span>`

### 3c. Dramatic Failure Screen Shake

CSS:
```css
@keyframes shake {
    0%, 100% { transform: translateX(0); }
    20%       { transform: translateX(-6px); }
    40%       { transform: translateX(6px); }
    60%       { transform: translateX(-4px); }
    80%       { transform: translateX(4px); }
}
.shake { animation: shake 0.4s ease-in-out; }
```

C# in the roll handler:
```csharp
if (_lastResult.IsDramaticFailure)
{
    _resultBoxClass = "shake";
    await InvokeAsync(StateHasChanged);
    await Task.Delay(500);
    _resultBoxClass = string.Empty;
}
```

### 3d. Exceptional Success Particle Burst

Markup (conditional):
```razor
@if (_showBurst)
{
    <div class="particle-burst" aria-hidden="true">
        @for (int a = 0; a < 8; a++)
        {
            <span class="burst-particle" style="--angle: @(a * 45)deg"></span>
        }
    </div>
}
```

CSS:
```css
.particle-burst { position: absolute; inset: 0; pointer-events: none; }

.burst-particle {
    position: absolute;
    top: 50%; left: 50%;
    width: 6px; height: 6px;
    border-radius: 50%;
    background: var(--crimson-glow);
    animation: burst 0.6s ease-out forwards;
}

@keyframes burst {
    from { transform: translate(-50%, -50%) rotate(var(--angle)) translateY(0); opacity: 1; }
    to   { transform: translate(-50%, -50%) rotate(var(--angle)) translateY(-60px); opacity: 0; }
}
```

C# toggle:
```csharp
if (_lastResult.IsExceptionalSuccess)
{
    _showBurst = true;
    await InvokeAsync(StateHasChanged);
    await Task.Delay(700);
    _showBurst = false;
}
```

### 3e. Modal Backdrop Blood Surge

Add `.backdrop-surge` class to the modal backdrop on each roll; remove after 300ms:
```css
@keyframes bloodSurge {
    0%   { background-color: rgba(0,0,0,0.5); }
    50%  { background-color: rgba(0,0,0,0.65); }
    100% { background-color: rgba(0,0,0,0.5); }
}
.backdrop-surge { animation: bloodSurge 0.3s ease; }
```

---

## Wave 4 — Character Sheet Visual

### 4a. Health Track as Damage Boxes

Replace the vitals progress bar in `CharacterVitals.razor` with a box track:
```razor
<div class="health-track">
    @for (int i = 0; i < MaxHealth; i++)
    {
        <span class="health-box @GetDamageClass(i)">
            @GetDamageSymbol(i)
        </span>
    }
</div>
```

Damage classes: `"empty"`, `"bashing"` (`/`), `"lethal"` (`✕`), `"aggravated"` (`☆`)

CSS in `CharacterVitals.razor.css`:
```css
.health-track { display: flex; gap: 4px; }

.health-box {
    width: 28px; height: 28px;
    border: 1px solid var(--muted-gray);
    border-radius: 4px;
    display: flex; align-items: center; justify-content: center;
    font-size: 0.8rem;
    transition: background-color 0.2s, transform 0.15s;
}

.health-box.bashing  { background: rgba(184, 134, 11, 0.2); border-color: var(--warning-amber); color: var(--warning-amber); }
.health-box.lethal   { background: rgba(139, 0, 0, 0.2);   border-color: var(--crimson);       color: var(--crimson); }
.health-box.aggravated { background: rgba(139,0,0,0.4);    border-color: var(--crimson-glow);   color: var(--bone-white); }
```

### 4b. Blood-Drop Vitae Visual

Inline SVG with data-bound `clipPath`:
```razor
<div class="vitae-container">
    <svg class="vitae-drop" viewBox="0 0 40 52" xmlns="http://www.w3.org/2000/svg">
        <defs>
            <clipPath id="vitae-fill-@_clipId">
                <rect x="0" y="@VitaeFillY" width="40" height="52" />
            </clipPath>
        </defs>
        <!-- Drop outline -->
        <path d="M20 2 C20 2 4 20 4 32 A16 16 0 0 0 36 32 C36 20 20 2 20 2Z"
              fill="none" stroke="var(--muted-gray)" stroke-width="1.5" />
        <!-- Fill layer -->
        <path d="M20 2 C20 2 4 20 4 32 A16 16 0 0 0 36 32 C36 20 20 2 20 2Z"
              fill="var(--crimson)" clip-path="url(#vitae-fill-@_clipId)"
              style="transition: clip-path 0.4s ease;" />
    </svg>
    <span class="vitae-label">@CurrentVitae / @MaxVitae</span>
</div>
```

`VitaeFillY` computed: `52 - (CurrentVitae / MaxVitae * 52)` — maps to SVG coordinate space.

### 4c. Dot Upgrade Bleed Animation

In `DotScale.razor.cs` (or `@code`): track the last-upgraded dot index in `_bleedingDot`. When a dot is filled:
```csharp
_bleedingDot = newValue - 1;
StateHasChanged();
await Task.Delay(400);
_bleedingDot = -1;
```

In markup: `class="dot @(i == _bleedingDot ? "bleeding" : "")"` in `DotScale.razor.css`:
```css
@keyframes bleed {
    0%   { box-shadow: 0 0 0 0 rgba(139,0,0,0.8); }
    50%  { box-shadow: 0 0 0 8px rgba(139,0,0,0.3); }
    100% { box-shadow: 0 0 0 0 rgba(139,0,0,0); }
}
.dot.bleeding { animation: bleed 0.4s ease-out; }
```

### 4d. Ornate Section Dividers

Add to `app.css`:
```css
.section-divider {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    margin: 1.5rem 0;
    color: var(--crimson);
}

.section-divider::before,
.section-divider::after {
    content: '';
    flex: 1;
    height: 1px;
    background: linear-gradient(to right, transparent, var(--muted-gray));
}

.section-divider::after {
    background: linear-gradient(to left, transparent, var(--muted-gray));
}

.section-divider-label {
    font-family: var(--font-heading);
    font-size: 0.7rem;
    letter-spacing: 0.15em;
    text-transform: uppercase;
    color: var(--crimson);
}
```

Usage in character sheet section headers:
```razor
<div class="section-divider">
    <span class="section-divider-label">◆ Attributes ◆</span>
</div>
```

---

## Wave 5 — Auth Form Polish

### 5a. Gothic Alert Variants

Override Bootstrap alert classes in `app.css`:
```css
.alert {
    border-radius: 8px;
    border: none;
    border-left: 4px solid;
    padding: 1rem 1.25rem;
    font-family: var(--font-primary);
    background: var(--charcoal);
}

.alert-danger  { border-left-color: var(--crimson);        color: #E8A0A0; }
.alert-success { border-left-color: var(--success-emerald); color: #8FD9A8; }
.alert-warning { border-left-color: var(--warning-amber);   color: #E8D48A; }
.alert-info    { border-left-color: var(--muted-gray);      color: var(--text-secondary); }
```

### 5b. OAuth Button Restyle

In `Login.razor.css`, replace the stock Bootstrap styling on `.oauth-btn`:
```css
.oauth-btn {
    background: var(--ash-gray);
    border: 1px solid var(--muted-gray);
    color: var(--bone-white);
    border-radius: 0.75rem;
    padding: 0.6rem 1.25rem;
    font-family: var(--font-primary);
    font-weight: 500;
    transition: border-color 0.2s, box-shadow 0.2s;
    cursor: pointer;
}

.oauth-btn:hover {
    border-color: var(--crimson);
    box-shadow: 0 0 12px rgba(139, 0, 0, 0.2);
    background: var(--charcoal);
}
```

Remove Bootstrap's `.btn` and `.btn-outline-secondary` classes from the OAuth buttons in `Login.razor`.

### 5c. ManageLayout Gothic Overhaul

Create `ManageLayout.razor.css` (new file) and update `ManageLayout.razor`:

**`ManageLayout.razor`** — replace Bootstrap grid with semantic CSS:
```razor
<div class="manage-wrapper">
    <aside class="manage-sidebar">
        <RequiemNexus.Web.Components.Pages.Account.Manage.ManageNavMenu />
    </aside>
    <main class="manage-content">
        <div class="manage-card">
            @Body
        </div>
    </main>
</div>
```

**`ManageLayout.razor.css`:**
```css
.manage-wrapper {
    display: grid;
    grid-template-columns: 240px 1fr;
    gap: 2rem;
    padding: 2rem;
    max-width: 1100px;
    margin: 0 auto;
    align-items: start;
}

.manage-sidebar {
    background: var(--charcoal);
    border: 1px solid var(--muted-gray);
    border-radius: 12px;
    padding: 1.5rem 0;
    position: sticky;
    top: 5rem;
}

.manage-card {
    background: var(--charcoal);
    border: 1px solid var(--muted-gray);
    border-top: 2px solid var(--crimson);
    border-radius: 12px;
    padding: 2rem;
}

@media (max-width: 768px) {
    .manage-wrapper { grid-template-columns: 1fr; }
    .manage-sidebar { position: static; }
}
```

Normalize all `--rn-*` token references within `ManageLayout.razor` scoped styles to the global `--bone-white`, `--charcoal`, `--crimson`, `--muted-gray` tokens.

### 5d. ConfirmEmail Visual Treatment

In `ConfirmEmail.razor`, add a conditional icon above the status message:
```razor
@if (_isSuccess)
{
    <div class="confirm-icon fade-in-up" aria-hidden="true">
        <!-- Blood-drop SVG (same as vitae drop, larger) -->
        <svg class="confirm-drop" viewBox="0 0 40 52">
            <path d="M20 2 C20 2 4 20 4 32 A16 16 0 0 0 36 32 C36 20 20 2 20 2Z"
                  fill="var(--crimson)" />
        </svg>
    </div>
    <p class="confirm-message">Your covenant is confirmed. Welcome to the night.</p>
}
else
{
    <div class="confirm-icon fade-in-up" aria-hidden="true">
        <span class="confirm-error-icon">⛓️</span>
    </div>
    <p class="confirm-message confirm-error">The link is broken or expired. Request a new one below.</p>
}
```

---

## Wave 6 — Initiative Tracker Overhaul

Target: `Components/Pages/Campaigns/InitiativeTracker.razor` + new `InitiativeTracker.razor.css`

### Pulsing Current-Actor Row
```css
@keyframes activeRowPulse {
    0%, 100% { background: linear-gradient(90deg, rgba(139,0,0,0.12), transparent); }
    50%       { background: linear-gradient(90deg, rgba(139,0,0,0.22), transparent); }
}

.initiative-row.active {
    animation: activeRowPulse 2s ease-in-out infinite;
    border-left: 3px solid var(--crimson);
}
```

### Tilt Pill Badges
```razor
@foreach (var tilt in participant.Tilts)
{
    <span class="tilt-badge tilt-@tilt.ToLowerInvariant() fade-in">@tilt</span>
}
```

```css
.tilt-badge {
    display: inline-block;
    padding: 2px 8px;
    border-radius: 999px;
    font-size: 0.7rem;
    font-weight: 600;
    letter-spacing: 0.05em;
    text-transform: uppercase;
}
.tilt-blinded       { background: rgba(88,28,135,0.3); border: 1px solid #7C3AED; color: #C4B5FD; }
.tilt-knocked\ down { background: rgba(184,134,11,0.3); border: 1px solid var(--warning-amber); color: #FDE68A; }
.tilt-stunned       { background: rgba(139,0,0,0.3);   border: 1px solid var(--crimson);       color: #FCA5A5; }
```

### Inline Mini Health Track

Reuse the `.health-box` classes from Wave 4a in a condensed form:
```razor
<div class="mini-health-track">
    @for (int i = 0; i < participant.MaxHealth; i++)
    {
        <span class="health-box health-box-mini @GetDamageClass(participant, i)"></span>
    }
</div>
```

```css
.health-box-mini { width: 12px; height: 12px; font-size: 0.5rem; }
```

### Drag-to-Reorder (ST only)

Mark rows as draggable and wire standard HTML5 events:
```razor
<div class="initiative-row @(IsSt ? "draggable-row" : "")"
     draggable="@(IsSt ? "true" : "false")"
     @ondragstart="() => OnDragStart(participant)"
     @ondragover:preventDefault="true"
     @ondrop="() => OnDrop(participant)">
```

C# drag state:
```csharp
private InitiativeEntry? _draggedItem;

private void OnDragStart(InitiativeEntry item) => _draggedItem = item;

private void OnDrop(InitiativeEntry target)
{
    if (_draggedItem is null || _draggedItem == target) return;
    var idx = _participants.IndexOf(target);
    _participants.Remove(_draggedItem);
    _participants.Insert(idx, _draggedItem);
    _draggedItem = null;
}
```

### Turn Timer Arc (optional, ST-toggleable)

```razor
@if (_showTimer && IsSt)
{
    <svg class="turn-timer" viewBox="0 0 36 36" aria-label="Turn timer">
        <circle cx="18" cy="18" r="15.9" fill="none"
                stroke="var(--muted-gray)" stroke-width="2" />
        <circle cx="18" cy="18" r="15.9" fill="none"
                stroke="var(--crimson)" stroke-width="2"
                stroke-dasharray="100 100"
                style="stroke-dashoffset: @_timerDashOffset;
                       transition: stroke-dashoffset 1s linear;
                       transform: rotate(-90deg); transform-origin: 18px 18px;" />
    </svg>
}
```

`_timerDashOffset` decrements from 0 → 100 (full depletion) over `_turnDurationSeconds` via a 1-second `System.Timers.Timer`.

---

## Wave 7 — Command Palette

**New files:**
- `src/RequiemNexus.Web/Services/CommandPaletteService.cs`
- `src/RequiemNexus.Web/Components/UI/CommandPalette.razor`
- `src/RequiemNexus.Web/Components/UI/CommandPalette.razor.css`

### `CommandPaletteService`

Web-layer singleton:
```csharp
public sealed class CommandPaletteService
{
    public bool IsOpen { get; private set; }
    public event Action? OnStateChanged;

    public void Open()  { IsOpen = true;  OnStateChanged?.Invoke(); }
    public void Close() { IsOpen = false; OnStateChanged?.Invoke(); }
    public void Toggle() { if (IsOpen) Close(); else Open(); }
}
```

Register in `Program.cs`: `builder.Services.AddSingleton<CommandPaletteService>()`

### Keyboard Trigger

Add to `wwwroot/app.js`:
```js
window.registerCommandPaletteShortcut = function (dotNetRef) {
    document.addEventListener('keydown', (e) => {
        if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
            e.preventDefault();
            dotNetRef.invokeMethodAsync('Toggle');
        }
    });
};
```

In `MainLayout.razor` `OnAfterRenderAsync`:
```csharp
await JS.InvokeVoidAsync("registerCommandPaletteShortcut", DotNetObjectReference.Create(this));

[JSInvokable]
public void Toggle() => PaletteService.Toggle();
```

### `CommandPalette.razor`

```razor
@if (PaletteService.IsOpen)
{
    <div class="palette-overlay" @onclick="Close" role="dialog" aria-modal="true" aria-label="Command palette">
        <div class="palette-card" @onclick:stopPropagation="true">
            <input @ref="_inputRef" class="palette-input" placeholder="Search characters, campaigns, pages..."
                   @bind-value="_query" @bind-value:event="oninput" @onkeydown="OnKeyDown" />
            <div class="palette-results">
                @foreach (var section in FilteredResults())
                {
                    <div class="palette-section">
                        <span class="palette-section-label">@section.Label</span>
                        @foreach (var item in section.Items)
                        {
                            <div class="palette-item @(item == _activeItem ? "active" : "")"
                                 @onclick="() => Navigate(item.Href)">
                                <span class="palette-item-icon">@item.Icon</span>
                                <span class="palette-item-label">@item.Name</span>
                            </div>
                        }
                    </div>
                }
            </div>
        </div>
    </div>
}
```

**Data source:** inject `ICharacterService` and `ICampaignService` (Application layer, same pattern as existing pages). Pre-load character and campaign names on open; filter client-side.

**Keyboard navigation:** `↑`/`↓` keys move `_activeItem` through the flat list; `Enter` navigates; `Escape` closes.

### Styling (`CommandPalette.razor.css`)

```css
.palette-overlay {
    position: fixed; inset: 0; z-index: 10000;
    background: rgba(0,0,0,0.6); backdrop-filter: blur(4px);
    display: flex; align-items: flex-start; justify-content: center;
    padding-top: 15vh;
    animation: fadeIn 0.15s ease;
}

.palette-card {
    background: var(--ash-gray);
    border: 1px solid var(--muted-gray);
    border-radius: 14px;
    width: 100%; max-width: 560px;
    box-shadow: 0 25px 60px rgba(0,0,0,0.7), 0 0 40px rgba(139,0,0,0.15);
    overflow: hidden;
    animation: slideDown 0.2s ease;
}

.palette-input {
    width: 100%; padding: 1.1rem 1.25rem;
    background: transparent; border: none;
    border-bottom: 1px solid var(--muted-gray);
    color: var(--bone-white); font-size: 1.05rem;
    font-family: var(--font-primary); outline: none;
}

.palette-results { max-height: 380px; overflow-y: auto; padding: 0.5rem 0; }

.palette-section-label {
    display: block; padding: 0.4rem 1rem;
    font-family: var(--font-heading); font-size: 0.65rem;
    letter-spacing: 0.15em; text-transform: uppercase;
    color: var(--crimson);
}

.palette-item {
    display: flex; align-items: center; gap: 0.75rem;
    padding: 0.6rem 1rem; cursor: pointer;
    color: var(--text-secondary);
    transition: background 0.1s, color 0.1s;
}

.palette-item:hover,
.palette-item.active {
    background: rgba(139,0,0,0.12); color: var(--bone-white);
}
```

---

## Verification Checklist

After each wave:
1. `dotnet build` — zero errors, zero warnings
2. `dotnet format --verify-no-changes` — passes
3. Visual check in Chrome (desktop + 375px mobile viewport)
4. Keyboard: Tab navigation through all forms, Ctrl+K opens palette, Escape closes modals
5. Dice roller: roll a pool of 10 — verify cascade, shake (force a dramatic failure via seed), particle burst (force exceptional success via seed)
6. Mobile: touch targets ≥ 44px on health boxes, tilt badges, initiative rows
