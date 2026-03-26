namespace RequiemNexus.Domain.Events;

/// <summary>
/// Raised synchronously when a character's Vitae reaches 0 during a spend operation (in-process dispatch).
/// </summary>
/// <param name="CharacterId">The character whose Vitae pool is empty after the spend.</param>
public record VitaeDepletedEvent(int CharacterId);
