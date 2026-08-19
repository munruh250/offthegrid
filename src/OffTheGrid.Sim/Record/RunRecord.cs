using System;
using System.Collections.Generic;
using OffTheGrid.Data;

namespace OffTheGrid.Sim.Record;

/// <summary>How a run ended. Design spec 6 — four end conditions.</summary>
public enum EndCondition
{
    /// <summary>Still going.</summary>
    None,

    /// <summary>Player chose to tap out. Morale-driven in practice.</summary>
    TapOut,

    /// <summary>Medic pulled them: body fat below the sex-gated floor, >30% weight loss, or BMI &lt; 17.</summary>
    MedicalPull,

    /// <summary>Injury or acute event ended the run.</summary>
    Incident,

    /// <summary>All nine rivals ended their runs first.</summary>
    LastOut
}

/// <summary>
/// The complete, serialisable record of one run. Resolves A21.
///
/// Three consumers, and the schema has to serve all of them:
///   - SAVE/RESTORE. A run resumed from disk must continue identically.
///   - DETERMINISM TEST. Seed + command log replayed on another device must
///     produce the same FinalChecksum (doc 13).
///   - TELEMETRY AND CAUSE-OF-DEATH. The trace is what tells the player why they
///     died, which is measured against the >=70% self-identification gate.
/// </summary>
public sealed class RunRecord
{
    /// <summary>Seed for this run. With the command log, this fully determines the run.</summary>
    public required ulong Seed { get; init; }

    /// <summary>Schema version, so old saves can be migrated rather than discarded.</summary>
    public int SchemaVersion { get; init; } = 1;

    public required Sex Sex { get; init; }
    public required float StartWeightKg { get; init; }
    public required float StartBodyFatPercent { get; init; }
    public required float HeightCm { get; init; }
    public required int AgeYears { get; init; }

    /// <summary>Attribute values at creation, indexed by <see cref="AttributeKind"/>.</summary>
    public required IReadOnlyDictionary<AttributeKind, int> Attributes { get; init; }

    public int DaysSurvived { get; set; }
    public EndCondition EndCondition { get; set; } = EndCondition.None;

    /// <summary>
    /// Stable identifier for the proximate cause, e.g. "morale.idleness".
    /// Distinct from EndCondition: the condition is the rule that fired, the
    /// cause is the story. Design spec 6 is explicit that reporting only the
    /// rule ("you were pulled at 17.1 BMI") is a rules citation, not an
    /// explanation.
    /// </summary>
    public string? CauseCode { get; set; }

    /// <summary>Rolling decision trace. Bounded and serialisable — see <see cref="DecisionTrace"/>.</summary>
    public DecisionTrace Trace { get; init; } = new();

    /// <summary>ISimLog checksum at end of run. This is what the cross-device test compares.</summary>
    public ulong FinalChecksum { get; set; }

    /// <summary>
    /// False if balance constants were hot-reloaded mid-run (C10). Such a run is
    /// not a valid balance sample and the solver must discard it.
    /// </summary>
    public bool IsCleanBalanceSample { get; set; } = true;
}

/// <summary>
/// The six attributes. Design spec 4.1.
/// Named AttributeKind rather than Attribute so it does not shadow System.Attribute.
/// </summary>
public enum AttributeKind
{
    Bushcraft,

    /// <summary>Stalking and trapping - what you PURSUE.</summary>
    Hunting,

    /// <summary>
    /// Nets, lines and weirs. [Q1 ANSWERED] Design spec 4.1 folded this into
    /// Hunting and flagged "split it out if playtest shows Hunting is
    /// over-picked." It was: Hunting governed three of four food routes against
    /// Foraging's one, and two contestants on identical loadouts differed 22% to
    /// 3% on nothing but Hunting 6 against Hunting 3.
    ///
    /// Its own attribute rather than folded into Foraging - reading water and
    /// setting a net has nothing to do with knowing which berries are safe.
    /// </summary>
    Fishing,

    /// <summary>Berries, shellfish, plants - what you GATHER, and what you can identify.</summary>
    Foraging,

    Fitness,
    Resolve,
    ColdAdaptation
}
