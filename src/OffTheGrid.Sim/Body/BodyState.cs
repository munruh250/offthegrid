using System;
using OffTheGrid.Data;
using OffTheGrid.Data.Balance;

namespace OffTheGrid.Sim.Body;

/// <summary>
/// The body, as MUTABLE internal state. Resolves A15 / C9.
///
/// Why mutable: the headless solver runs ~100k runs x ~300 slots. Allocating an
/// immutable snapshot per slot is ~30M allocations and turns an overnight job
/// into a four-day one. The solver mutates in place and never snapshots.
///
/// The game snapshots once per slot via <see cref="Snapshot"/> — 300 allocations
/// per run, irrelevant. The returned view has no setters and no reference back
/// here, so the CLAUDE.md rule "the view never mutates sim state" holds exactly.
///
/// The invariant this replaces is NOT "state is immutable". It is "state is
/// mutated only by the sim, and only inside a slot boundary."
/// </summary>
public sealed class BodyState
{
    public Sex Sex { get; }
    public float HeightCm { get; }
    public int AgeYears { get; }
    public float StartWeightKg { get; }

    public float FatMassKg { get; private set; }
    public float LeanMassKg { get; private set; }

    public BodyState(Sex sex, float heightCm, int ageYears, float weightKg, float bodyFatPercent)
    {
        if (weightKg <= 0) throw new ArgumentOutOfRangeException(nameof(weightKg));
        if (heightCm <= 0) throw new ArgumentOutOfRangeException(nameof(heightCm));
        if (bodyFatPercent is < 0 or >= 100) throw new ArgumentOutOfRangeException(nameof(bodyFatPercent));

        Sex = sex;
        HeightCm = heightCm;
        AgeYears = ageYears;
        StartWeightKg = weightKg;

        FatMassKg = weightKg * (bodyFatPercent / 100f);
        LeanMassKg = weightKg - FatMassKg;
    }

    public float WeightKg => FatMassKg + LeanMassKg;
    public float BodyFatPercent => FatMassKg / WeightKg * 100f;
    public float Bmi => WeightKg / MathF.Pow(HeightCm / 100f, 2f);
    public float WeightLossFraction => (StartWeightKg - WeightKg) / StartWeightKg;

    /// <summary>Mifflin-St Jeor at current mass. Design spec 5.1.</summary>
    public float BasalMetabolicRate =>
        10f * WeightKg + 6.25f * HeightCm - 5f * AgeYears + Sex.BmrConstant();

    /// <summary>
    /// BMR after adaptive thermogenesis: metabolism suppresses beyond what mass
    /// loss alone predicts, capped at -10% once 15% of bodyweight is gone. This
    /// is why late-game contestants plateau rather than freefall.
    /// </summary>
    public float EffectiveBasalMetabolicRate(BalanceData balance)
    {
        float lossFraction = MathF.Max(0f, WeightLossFraction);
        float suppression = balance.AdaptiveThermogenesisMaxSuppression
                          * MathF.Min(1f, lossFraction / balance.AdaptiveThermogenesisLossFractionCap);
        return BasalMetabolicRate * (1f - suppression);
    }

    /// <summary>
    /// Apply an energy balance in kcal. Deficit is partitioned between fat and
    /// lean mass; surplus is stored as fat only.
    ///
    /// Mutates in place. See the class remarks for why that is deliberate.
    /// </summary>
    public void ApplyEnergyBalance(float kcal, BalanceData balance)
    {
        if (kcal >= 0f)
        {
            FatMassKg += kcal / balance.KcalPerKgFat;
            return;
        }

        float deficit = -kcal;
        float fromFatKcal = deficit * balance.DeficitFractionFromFat;
        float fromLeanKcal = deficit - fromFatKcal;

        FatMassKg = MathF.Max(0f, FatMassKg - fromFatKcal / balance.KcalPerKgFat);
        LeanMassKg = MathF.Max(0f, LeanMassKg - fromLeanKcal / balance.KcalPerKgLeanTissue);
    }

    /// <summary>
    /// Project into an immutable view. The game calls this once per slot; the
    /// solver never calls it.
    /// </summary>
    public BodySnapshot Snapshot() => new()
    {
        Sex = Sex,
        WeightKg = WeightKg,
        FatMassKg = FatMassKg,
        LeanMassKg = LeanMassKg,
        BodyFatPercent = BodyFatPercent,
        Bmi = Bmi,
        WeightLossFraction = WeightLossFraction
    };
}

/// <summary>
/// Immutable projection of <see cref="BodyState"/> for the presentation layer.
/// No setters, no reference back to the mutable state — a view holding one of
/// these cannot observe a later mutation.
/// </summary>
public readonly record struct BodySnapshot
{
    public Sex Sex { get; init; }
    public float WeightKg { get; init; }
    public float FatMassKg { get; init; }
    public float LeanMassKg { get; init; }
    public float BodyFatPercent { get; init; }
    public float Bmi { get; init; }
    public float WeightLossFraction { get; init; }
}
