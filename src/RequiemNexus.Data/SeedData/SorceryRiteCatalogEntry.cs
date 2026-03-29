using RequiemNexus.Domain.Enums;

namespace RequiemNexus.Data.SeedData;

/// <summary>
/// One row from sorcery seed JSON (Crúac, Theban, or Necromancy) for idempotent catalog upserts.
/// </summary>
public sealed record SorceryRiteCatalogEntry(
    string Name,
    int Rating,
    string Prerequisites,
    string Effect,
    SorceryType SorceryType);
