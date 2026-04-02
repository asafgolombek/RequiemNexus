using RequiemNexus.Domain.Enums;

namespace RequiemNexus.Data.SeedData;

/// <summary>
/// One row from sorcery seed JSON (Crúac, Theban, or Necromancy) for idempotent catalog upserts.
/// </summary>
/// <param name="TargetSuccesses">Extended-action success threshold from source material (V:tR 2e).</param>
/// <param name="RequiresElder">True when catalog <c>Ranking</c> is the elder token (Blood Potency gate).</param>
public sealed record SorceryRiteCatalogEntry(
    string Name,
    int Rating,
    string Prerequisites,
    string Effect,
    SorceryType SorceryType,
    int TargetSuccesses,
    bool RequiresElder = false);
