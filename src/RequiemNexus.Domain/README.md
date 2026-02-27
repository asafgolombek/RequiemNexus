# 🧠 RequiemNexus.Domain

## Why this project exists

The **RequiemNexus.Domain** project encapsulates the core business logic, rules, and rules engine of Vampire: The Requiem.

According to our architectural mandate, the Domain Layer is **stateless, deterministic, and fully unit-testable**. It represents the rules of the game without knowing anything about how data is stored (Data layer) or how it is displayed (Web layer). It owns its own models, invariants, and calculations.

## 📖 Learning Artifacts

To adhere to the Antigravity system, all changes here must prioritize understanding.

### ✅ Intentionally Simple Example

When designing a Domain rule, keep it explicit, testable, and completely ignorant of the database or UI.

```csharp
public class AttributeCalculator
{
    // A simple, explicit calculation with no external dependencies
    public int CalculateDefense(int dexterity, int wits, int athletics)
    {
        return Math.Min(dexterity, wits) + athletics;
    }
}
```

### ❌ Intentionally Wrong Example (Anti-Pattern)

Do **NOT** write logic that creates hidden side-effects, reaches out to infrastructure concerns, or throws away traceability.

```csharp
public class BadAttributeCalculator
{
    private readonly ApplicationDbContext _db;

    // WRONG: Domain logic should never directly query the Database or have implicit state dependencies!
    public BadAttributeCalculator(ApplicationDbContext db)
    {
        _db = db;
    }

    public void UpdateDefense(Guid characterId)
    {
        // HIDDEN STATE & SIDE EFFECT: Reading from DB, updating without explicit inputs...
        var character = _db.Characters.Find(characterId);
        // ... magic happens here ...
        _db.SaveChanges(); // WRONG: Domain has no business calling SaveChanges.
    }
}
```

**Why it's wrong:**
If `CalculateDefense()` fails or returns an unexpected value, in the "good" example we simply pass in `(dex, wits, athletics)` and write a unit test. In the "bad" example, we have to seed a database to test logic. It violates the Antigravity principles of Traceability and Observability.
