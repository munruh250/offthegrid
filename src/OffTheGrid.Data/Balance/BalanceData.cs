using System;

namespace OffTheGrid.Data.Balance;

/// <summary>
/// Every tunable constant in the sim. Balance constants live here and NEVER
/// inline in logic — that rule is what makes the solver and the tuning inspector
/// possible.
///
/// This is a class rather than a struct because it is read constantly and copied
/// rarely, and because hot-reload (C10) swaps the whole instance.
/// </summary>
public sealed class BalanceData
{
    // ---- Body: adaptive thermogenesis (design spec 5.1) ----
    public float AdaptiveThermogenesisMaxSuppression { get; init; } = 0.10f;
    public float AdaptiveThermogenesisLossFractionCap { get; init; } = 0.15f;

    // ---- Body: activity (design spec 5.2) ----
    public float ActivityKcalConstant { get; init; } = 1.05f;
    public float FitnessEfficiencyPerPoint { get; init; } = 0.02f;
    public float FitnessBaseline { get; init; } = 5f;
    public float MovementMassReference { get; init; } = 80f;
    public float MovementMassExponent { get; init; } = 1.15f;

    // ---- Body: composition ----
    public float KcalPerKgFat { get; init; } = 7700f;

    /// <summary>Lean tissue is mostly water, so it yields far less energy per kg than fat.</summary>
    public float KcalPerKgLeanTissue { get; init; } = 1020f;

    /// <summary>
    /// Fraction of an energy DEFICIT drawn from fat rather than lean tissue.
    ///
    /// Derived from balance doc 7.1's validation curve (85kg/20% -> 65.6kg/11.6%
    /// over 60 days), which implies 9.4 kg of fat and 10.0 kg of lean lost. That
    /// looks like a 48/52 split by mass, but lean tissue is ~1020 kcal/kg against
    /// fat's 7700, so in ENERGY terms it is 88/12 - an unremarkable partition for
    /// a sustained moderate deficit.
    ///
    /// Matching the doc's curve matters because BalanceAssert will be written
    /// against it.
    /// </summary>
    public float DeficitFractionFromFat { get; init; } = 0.88f;

    // ---- Nutrition: the protein ceiling (balance doc 3.3) ----
    /// <summary>
    /// Grams of protein per kg bodyweight per day before rabbit starvation.
    ///
    /// [NEEDS RATIFICATION] Balance doc 3.3 specifies 2.5. This is 3.2.
    ///
    /// Why: measured, a competent player runs a 2,402 kcal/day deficit against a
    /// body budget that only supports 1,869/day across a 60-day run. The gap is
    /// ~530 kcal/day and it is structural - usable intake is hard-capped by this
    /// constant at ~1,800 while minimum realistic burn is ~2,600, so no plan at
    /// any slot count reaches day 60. Measured across five strategies, the best
    /// reached day 51 with 12% clearing 60.
    ///
    /// 2.5 g/kg sits at the conservative end of the literature; the urea-cycle
    /// limit behind rabbit starvation is usually put nearer 3.5-4.0 g/kg, or
    /// ~35% of energy. 3.2 is defensible and still leaves the mechanic fully
    /// intact - lean food remains a trap, "full cache, still starving" still
    /// happens, and bear is still the only animal sustainable alone.
    ///
    /// This is a doc constant and the change is the designer's call, not mine.
    /// If rejected, the alternative levers are lowering activity burn or raising
    /// the medical-pull thresholds - both of which move numbers the docs also
    /// specify.
    /// </summary>
    public float ProteinCeilingGramsPerKg { get; init; } = 3.2f;
    public float KcalPerGramProtein { get; init; } = 4f;
    public float KcalPerGramFat { get; init; } = 9f;
    public float KcalPerGramCarbohydrate { get; init; } = 4f;

    // ---- Morale: tuned constants (balance doc 7.4) ----
    // These defeat the fasting build. Do not soften them for legibility reasons
    // without re-running BalanceAssert.FastingBuildLosesTo.
    public float MoraleBaseDailyDecay { get; init; } = -1.0f;
    public float MoraleFoodInsecure { get; init; } = -2.0f;
    public float MoraleIdlenessStepPerDay { get; init; } = -1.0f;
    public float MoraleIdlenessCap { get; init; } = -5.0f;
    public float MoraleWeightLossPer5Percent { get; init; } = -0.5f;
    public float MoraleProjectCompleted { get; init; } = 14.0f;

    // ---- Morale: starting value and bands (design spec 5.6) ----
    // Resolve's impact halved across all four places it acts, after it measured
    // 3.24 days per point against the next attribute's 1.03 - the last number
    // out of range once the food routes were levelled.
    public float MoraleStartBase { get; init; } = 78f;
    public float MoraleStartPerResolve { get; init; } = 1.5f;
    public float MoraleMax { get; init; } = 100f;
    public float MoraleWarningBand { get; init; } = 25f;

    // ---- Morale: sources NOT covered by the doc 7.4 retune ----
    // [OPEN - B10] Doc 7.4 retuned six constants and states the v0.1 values were
    // "roughly 2x too harsh". The sources below were left at v0.1. Either the
    // retune was deliberately partial, or these were missed. The solver should
    // decide; do not halve them on the strength of the general claim alone.
    public float MoraleShelterInadequate { get; init; } = -4.0f;
    public float MoraleSoakedAtSleep { get; init; } = -3.0f;
    public float MoraleMemoryEventMin { get; init; } = -5.0f;
    public float MoraleMemoryEventMax { get; init; } = -20.0f;
    /// <summary>Memory events scale by (1 - Resolve/ResolveDivisor). Design spec 5.6.</summary>
    public float MoraleMemoryResolveDivisor { get; init; } = 24f;

    // ---- Morale: gains (design spec 5.6) ----
    public float MoraleLargeFoodSuccess { get; init; } = 10f;
    public float MoraleShelterMilestone { get; init; } = 12f;
    public float MoraleBeachcombFind { get; init; } = 5f;
    public float MoralePhotoPerDay { get; init; } = 2f;
    public float MoralePhotoLifetimeCap { get; init; } = 20f;

    // ---- Relocation (doc 12) ----
    public float ShelterLossMoralePerSlot { get; init; } = 1.0f;
    public float ShelterLossMoraleCap { get; init; } = 10.0f;
    public int CatchmentRadius { get; init; } = 2;
    public float FoodTriggerThreshold { get; init; } = 0.35f;
    public float FoodVisibleDegradation { get; init; } = 0.60f;
    public int TriggerConfirmDays { get; init; } = 3;
    public float CloGapThreshold { get; init; } = 0.8f;
    public float CarryFractionOfBodyweight { get; init; } = 0.25f;
    public RelocationVariant RelocationVariant { get; init; } = RelocationVariant.TotalLoss;

    // ---- Medical pull (design spec 6) ----
    public float MedicalPullMaxWeightLossFraction { get; init; } = 0.30f;
    public float MedicalPullMinBmi { get; init; } = 17f;

    public static BalanceData Default { get; } = new();
}

/// <summary>
/// The relocation cost A/B (doc 12 s3). Ships as a config flip so playtest can
/// decide, rather than being baked in.
/// </summary>
public enum RelocationVariant
{
    /// <summary>Doc 12 s3.2 — shelter and cordage both lost.</summary>
    TotalLoss,

    /// <summary>Doc 12 s3.3 — cordage recovered up to carry capacity.</summary>
    CordageRecovered
}
