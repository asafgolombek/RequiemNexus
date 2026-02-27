# 🗄️ RequiemNexus.Data

## Why this project exists

The **RequiemNexus.Data** project is our Infrastructure layer. Its sole responsibility is persisting and retrieving data using **Entity Framework Core**.

Infrastructure **serves** the domain, never the reverse. It translates Domain concepts into database tables and performs migrations. It owns the `DbContext`, the migrations, and the logic to seed or wipe data.

## 📖 Learning Artifacts

### ✅ Intentionally Simple Example

Data access should be explicit and return exactly what the application layer asks for, without embedding business rules into the query (unless strictly for performance, which must be documented).

```csharp
public async Task<Character?> GetCharacterByIdAsync(Guid characterId, CancellationToken ct)
{
    // Simple, explicit retrieval with AsNoTracking for read-only efficiency
    return await _context.Characters
        .AsNoTracking()
        .FirstOrDefaultAsync(c => c.Id == characterId, ct);
}
```

### ❌ Intentionally Wrong Example (Anti-Pattern)

Do **NOT** perform business logic or derive stats while querying, and do not rely on implicit lazy-loading to hide database calls.

```csharp
public Character GetCharacterAndCalculate(Guid characterId)
{
    var character = _context.Characters.Find(characterId);

    // WRONG: The Data layer is doing Domain layer responsibilities!
    character.CalculatedDefense = Math.Min(character.Dexterity, character.Wits) + character.Athletics;

    _context.SaveChanges(); // Unexpected explicit side-effect during a fetch!

    return character;
}
```

**Why it's wrong:**
The Data layer's job is _I/O_, not rules processing. Placing rule calculations in the data layer means those rules cannot be tested without a database. Additionally, performing a save operation during what should be a "read" violates the Principle of Least Surprise.
