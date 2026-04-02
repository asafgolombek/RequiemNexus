using RequiemNexus.Data;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Application.Contracts;

/// <summary>
/// Application-lifetime snapshot of seeded reference catalog rows. Homebrew entities (<see cref="Clan.IsHomebrew"/>,
/// <see cref="Discipline.IsHomebrew"/>, <see cref="Merit.IsHomebrew"/>) are excluded and remain database-backed.
/// Warmed at host startup; restart the application to refresh. If admin editing of definitions is introduced later,
/// extend this contract with an explicit flush/reload API and invoke it after mutations.
/// </summary>
public interface IReferenceDataCache
{
    /// <summary>
    /// Gets a value indicating whether <see cref="LoadFromDatabaseAsync"/> has completed successfully at least once.
    /// </summary>
    bool IsInitialized { get; }

    /// <summary>
    /// Gets official clans only, with <see cref="Clan.ClanDisciplines"/> loaded.
    /// </summary>
    IReadOnlyList<Clan> ReferenceClans { get; }

    /// <summary>
    /// Gets official disciplines only, with covenant and bloodline navigations loaded.
    /// </summary>
    IReadOnlyList<Discipline> ReferenceDisciplines { get; }

    /// <summary>
    /// Gets official merits only, with <see cref="Merit.Prerequisites"/> loaded.
    /// </summary>
    IReadOnlyList<Merit> ReferenceMerits { get; }

    /// <summary>
    /// Gets all covenant definitions.
    /// </summary>
    IReadOnlyList<CovenantDefinition> CovenantDefinitions { get; }

    /// <summary>
    /// Gets all sorcery rite definitions with required covenant/clan navigations loaded.
    /// </summary>
    IReadOnlyList<SorceryRiteDefinition> SorceryRiteDefinitions { get; }

    /// <summary>
    /// Gets all scale definitions.
    /// </summary>
    IReadOnlyList<ScaleDefinition> ScaleDefinitions { get; }

    /// <summary>
    /// Gets all coil definitions with <see cref="CoilDefinition.Scale"/> loaded.
    /// </summary>
    IReadOnlyList<CoilDefinition> CoilDefinitions { get; }

    /// <summary>
    /// Gets all bloodline definitions with <see cref="BloodlineDefinition.AllowedParentClans"/> loaded.
    /// </summary>
    IReadOnlyList<BloodlineDefinition> BloodlineDefinitions { get; }

    /// <summary>
    /// Gets covenant–merit link rows (seeded junction catalog).
    /// </summary>
    IReadOnlyList<CovenantDefinitionMerit> CovenantDefinitionMerits { get; }

    /// <summary>
    /// Gets all devotion definitions with prerequisites and prerequisite discipline navigations loaded.
    /// </summary>
    IReadOnlyList<DevotionDefinition> DevotionDefinitions { get; }

    /// <summary>
    /// Loads reference data from the database into this cache. Safe to call multiple times; later calls no-op after the first successful load.
    /// </summary>
    /// <param name="context">Database context (typically a scoped instance from a factory).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task LoadFromDatabaseAsync(ApplicationDbContext context, CancellationToken cancellationToken = default);
}
