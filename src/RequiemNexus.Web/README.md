# 🌐 RequiemNexus.Web

## Why this project exists

The **RequiemNexus.Web** project represents the Presentation layer. This project holds UI components, Blazor Server routing, Controller endpoints, and wiring for dependency injection.

This layer **contains no business rules** and **no direct database access**. It relies entirely on Application/Domain Application Services to orchestrate workflows. It renders state and forwards user intents to the backend.

## 📖 Learning Artifacts

### ✅ Intentionally Simple Example

A UI component should receive state and emit an event when an action is taken. It relies on the Application/Service layer to persist that intent.

```razor
@page "/character/{Id:guid}"
@inject ICharacterService CharacterService

<button @onclick="RollDice">Roll Strength + Brawl</button>

@code {
    [Parameter] public Guid Id { get; set; }

    private async Task RollDice()
    {
        // UI simply delegates to the Application service
        var result = await CharacterService.RollAttributeAndSkillAsync(Id, "Strength", "Brawl");
        // Update local state to render result
    }
}
```

### ❌ Intentionally Wrong Example (Anti-Pattern)

Do **NOT** inject exactly what you want (like the DbContext) into the UI to bypass the Application layer.

```razor
@page "/character/{Id:guid}"
@inject ApplicationDbContext DbContext  <!-- WRONG: UI shouldn't know the DB exists -->

<button @onclick="HealCharacter">Heal 1 Health</button>

@code {
    [Parameter] public Guid Id { get; set; }

    private async Task HealCharacter()
    {
        // WRONG: UI is making database queries and changing state directly!
        var character = await DbContext.Characters.FindAsync(Id);
        character.CurrentHealth += 1; // WRONG: Writing Domain rules in the UI!
        await DbContext.SaveChangesAsync();
    }
}
```

**Why it's wrong:**
When business logic (e.g., character health caps, healing rules) lives in the UI component, it cannot be reused (what if an API needs to heal the character?). Injecting `DbContext` directly into the presentation layer destroys traceability, leading to "magic" data changes that other developers cannot trace safely.
