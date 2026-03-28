using RequiemNexus.Domain.Enums;

namespace RequiemNexus.Application.Models;

/// <summary>Outcome of a single hunt attempt.</summary>
public record HuntResult(
    int Successes,
    int VitaeGained,
    ResonanceOutcome Resonance,
    string PoolDescription,
    string NarrativeDescription,
    bool TerritoryBonusApplied);
