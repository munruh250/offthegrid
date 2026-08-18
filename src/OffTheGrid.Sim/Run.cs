using System;
using System.Collections.Generic;
using OffTheGrid.Data;
using OffTheGrid.Data.Balance;
using OffTheGrid.Sim.Body;
using OffTheGrid.Sim.Logging;
using OffTheGrid.Sim.Morale;
using OffTheGrid.Sim.Nutrition;
using OffTheGrid.Sim.Record;
using OffTheGrid.Sim.Time;

namespace OffTheGrid.Sim;

/// <summary>How one day was spent. The caller fills this from player input or rival AI.</summary>
public sealed class DayPlan
{
    /// <summary>Activity for each available slot. Extra entries beyond the day's slots are ignored.</summary>
    public required IReadOnlyList<Activity> Slots { get; init; }

    /// <summary>What was eaten today.</summary>
    public Macros Eaten { get; init; }

    public float TerrainMultiplier { get; init; } = 1.0f;
    public bool FoodInsecure { get; init; }
    public bool ShelterInadequate { get; init; }
    public bool SoakedAtSleep { get; init; }
    public bool HasPhoto { get; init; }
}

/// <summary>Result of stepping one day.</summary>
public readonly record struct DayResult
{
    public int DayNumber { get; init; }
    public int SlotsAvailable { get; init; }
    public float BurnKcal { get; init; }
    public float UsableIntakeKcal { get; init; }
    public float WastedIntakeKcal { get; init; }
    public float NetKcal { get; init; }
    public bool ProteinCeilingBound { get; init; }
    public MoraleDayTotals Morale { get; init; }
    public EndCondition EndCondition { get; init; }
}

/// <summary>
/// One contestant's run: body, morale, and the day loop that couples them.
///
/// This is the headless entry point. The game drives it a day at a time and
/// projects snapshots for display; the solver drives it in a tight loop and never
/// projects at all. Rivals run this same class at lower fidelity - same physics,
/// expected-value inputs (B8).
///
/// Mutable by design, per A15. See BodyState for the reasoning.
/// </summary>
public sealed class Run
{
    private readonly BalanceProvider balance;
    private readonly int fitness;

    public Run(
        ulong seed,
        Sex sex,
        float heightCm,
        int ageYears,
        float weightKg,
        float bodyFatPercent,
        IReadOnlyDictionary<AttributeKind, int> attributes,
        BalanceProvider? balanceProvider = null)
    {
        balance = balanceProvider ?? new BalanceProvider();
        var b = balance.Current;

        Rng = new Rng(seed);
        Log = new SimLog();
        Body = new BodyState(sex, heightCm, ageYears, weightKg, bodyFatPercent);
        Morale = new MoraleState(attributes[AttributeKind.Resolve], b);

        fitness = attributes[AttributeKind.Fitness];

        Record = new RunRecord
        {
            Seed = seed,
            Sex = sex,
            StartWeightKg = weightKg,
            StartBodyFatPercent = bodyFatPercent,
            HeightCm = heightCm,
            AgeYears = ageYears,
            Attributes = attributes
        };
    }

    public Rng Rng { get; }
    public SimLog Log { get; }
    public BodyState Body { get; }
    public MoraleState Morale { get; }
    public RunRecord Record { get; }

    public int DayNumber { get; private set; }
    public bool IsOver => Record.EndCondition != EndCondition.None;

    /// <summary>
    /// Advance one day. Order matters and is fixed: activity burn, then intake,
    /// then body, then morale at the boundary, then end conditions. Changing this
    /// order changes every downstream RNG draw and breaks saved replays.
    /// </summary>
    public DayResult StepDay(DayPlan plan)
    {
        if (IsOver) throw new InvalidOperationException("run has already ended");

        // Balance constants are read ONCE per day and held. A hot reload landing
        // mid-day would apply different constants to different parts of one tick.
        var b = balance.Current;

        DayNumber++;
        Morale.BeginDay();

        int slots = Calendar.SlotsForDay(DayNumber);

        // ---- burn ----
        float burn = Body.EffectiveBasalMetabolicRate(b);
        bool anyBuildProgress = false;

        for (int i = 0; i < Math.Min(slots, plan.Slots.Count); i++)
        {
            var activity = plan.Slots[i];
            burn += EnergyModel.ExcessKcalForSlot(activity, Body, fitness, plan.TerrainMultiplier, b);
            if (activity.IsBuildProgress()) anyBuildProgress = true;
        }

        // ---- intake, through the protein ceiling ----
        var nutrition = NutritionModel.Evaluate(plan.Eaten, Body.WeightKg, b);

        // ---- body ----
        float net = nutrition.UsableKcal - burn;
        Body.ApplyEnergyBalance(net, b);

        // ---- morale at the day boundary ----
        var moraleTotals = Morale.EvaluateDay(new MoraleDayInputs
        {
            FoodInsecure = plan.FoodInsecure,
            ShelterInadequate = plan.ShelterInadequate,
            NoBuildProgress = !anyBuildProgress,
            SoakedAtSleep = plan.SoakedAtSleep,
            WeightLossFraction = Body.WeightLossFraction,
            HasPhoto = plan.HasPhoto
        }, b);

        Log.LogEvent("day", $"{DayNumber}|{Body.WeightKg:F2}|{Morale.Current:F2}|{net:F1}");

        Record.Trace.Add(new TraceEntry
        {
            Day = DayNumber,
            Slot = 0,
            Kind = TraceKind.NutritionEvent,
            Code = nutrition.ProteinCeilingBound ? "nutrition.ceiling.bound" : "nutrition.ok",
            Magnitude = net
        });

        var end = CheckEndConditions(b);
        if (end != EndCondition.None) Finish(end);

        Record.DaysSurvived = DayNumber;

        return new DayResult
        {
            DayNumber = DayNumber,
            SlotsAvailable = slots,
            BurnKcal = burn,
            UsableIntakeKcal = nutrition.UsableKcal,
            WastedIntakeKcal = nutrition.WastedKcal,
            NetKcal = net,
            ProteinCeilingBound = nutrition.ProteinCeilingBound,
            Morale = moraleTotals,
            EndCondition = end
        };
    }

    /// <summary>Design spec 6. Checked in a fixed order so the recorded cause is deterministic.</summary>
    private EndCondition CheckEndConditions(BalanceData b)
    {
        if (Morale.HasTappedOut) return EndCondition.TapOut;

        if (Body.BodyFatPercent < Body.Sex.MedicalPullBodyFatPercent()) return EndCondition.MedicalPull;
        if (Body.WeightLossFraction > b.MedicalPullMaxWeightLossFraction) return EndCondition.MedicalPull;
        if (Body.Bmi < b.MedicalPullMinBmi) return EndCondition.MedicalPull;

        return EndCondition.None;
    }

    private void Finish(EndCondition end)
    {
        Record.EndCondition = end;
        Record.CauseCode = DeriveCauseCode(end);
        Record.FinalChecksum = Log.GetChecksum();
        Record.IsCleanBalanceSample = balance.IsCleanSample;
    }

    /// <summary>
    /// The story behind the rule that fired. Design spec 6 is explicit that
    /// reporting only the rule ("pulled at 17.1 BMI") is a rules citation, not an
    /// explanation - the copy has to name the underlying cause.
    /// </summary>
    private string DeriveCauseCode(EndCondition end)
    {
        if (end != EndCondition.TapOut) return "medical.wasting";

        var top = Morale.Breakdown().TopMovers(1).Top;
        return top.Count > 0 ? $"morale.{top[0].Source}".ToLowerInvariant() : "morale.unknown";
    }
}
