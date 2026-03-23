using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace RequiemNexus.Application.RealTime;

/// <summary>
/// OpenTelemetry metrics for Phase 12 relationship subsystems (lineage, bonds, aura, ghouls).
/// Instrument names follow the <c>requiem.*</c> convention used by <see cref="RealTimeMetrics"/>.
/// </summary>
public class RelationshipWebMetrics
{
    private readonly Counter<long> _lineageMutations;
    private readonly Counter<long> _bloodBondStageChanges;
    private readonly Counter<long> _predatoryAuraContestsResolved;
    private readonly Counter<long> _ghoulMutations;

    /// <summary>
    /// Initializes a new instance of the <see cref="RelationshipWebMetrics"/> class.
    /// </summary>
    /// <param name="meterFactory">The factory used to create the application meter.</param>
    public RelationshipWebMetrics(IMeterFactory meterFactory)
    {
        Meter meter = meterFactory.Create("RequiemNexus.Relationships");

        _lineageMutations = meter.CreateCounter<long>(
            "requiem.relationships.lineage_mutations_total",
            "changes",
            "Kindred lineage updates and Blood Sympathy rolls");

        _bloodBondStageChanges = meter.CreateCounter<long>(
            "requiem.relationships.blood_bond_stage_changes_total",
            "changes",
            "Blood Bond stage transitions (feeding, fade)");

        _predatoryAuraContestsResolved = meter.CreateCounter<long>(
            "requiem.relationships.predatory_aura_contests_resolved_total",
            "contests",
            "Predatory Aura Lash Out contests resolved");

        _ghoulMutations = meter.CreateCounter<long>(
            "requiem.relationships.ghoul_mutations_total",
            "changes",
            "Ghoul create/update/feed/release operations");
    }

    /// <summary>Records a lineage-related mutation or Blood Sympathy roll.</summary>
    /// <param name="operation">Stable operation key (e.g. <c>set_pc_sire</c>, <c>blood_sympathy_roll</c>).</param>
    public void RecordLineageMutation(string operation)
    {
        TagList tags = default;
        tags.Add("operation", operation);
        _lineageMutations.Add(1, tags);
    }

    /// <summary>Records a Blood Bond stage change after persistence.</summary>
    /// <param name="toStage">Target stage (1–3).</param>
    public void RecordBloodBondStageChange(int toStage)
    {
        TagList tags = default;
        tags.Add("to_stage", toStage);
        _bloodBondStageChanges.Add(1, tags);
    }

    /// <summary>Records a completed Predatory Aura contest.</summary>
    public void RecordPredatoryAuraContestResolved()
    {
        _predatoryAuraContestsResolved.Add(1);
    }

    /// <summary>Records a ghoul management mutation.</summary>
    /// <param name="operation">Stable operation key (e.g. <c>create</c>, <c>feed</c>).</param>
    public void RecordGhoulMutation(string operation)
    {
        TagList tags = default;
        tags.Add("operation", operation);
        _ghoulMutations.Add(1, tags);
    }
}
