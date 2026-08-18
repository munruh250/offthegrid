using System;
using System.Collections.Generic;
using OffTheGrid.Data;
using OffTheGrid.Data.Balance;
using OffTheGrid.Sim.Body;
using OffTheGrid.Sim.Food;
using OffTheGrid.Sim.Logging;
using OffTheGrid.Sim.Morale;
using OffTheGrid.Sim.Nutrition;
using OffTheGrid.Sim.Record;
using OffTheGrid.Data.Tables;
using OffTheGrid.Sim.Time;

namespace OffTheGrid.Sim;

/// <summary>How one day was spent. The caller fills this from player input or rival AI.</summary>
public sealed class DayPlan
{
    /// <summary>Activity for each available slot. Extra entries beyond the day's slots are ignored.</summary>
    public required IReadOnlyList<Activity> Slots { get; init; }

    /// <summary>
    /// Food handed directly to the player, bypassing harvesting. Test and
    /// scenario use only - real play fills the larder by working slots.
    /// </summary>
    public Macros? DirectRation { get; init; }

    public float TerrainMultiplier { get; init; } = 1.0f;
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
    public float HarvestedKg { get; init; }
    public float LarderDaysOfFood { get; init; }
    public bool FoodInsecure { get; init; }
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
    private readonly IReadOnlyDictionary<AttributeKind, int> attributes;

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
        this.attributes = attributes;
        Larder = new Larder();

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

    /// <summary>Edible mass that counts as a "large food success". A good salmon or better.</summary>
    private const float LargeCatchKg = 4f;

    public Rng Rng { get; }
    public SimLog Log { get; }
    public BodyState Body { get; }
    public MoraleState Morale { get; }
    public RunRecord Record { get; }
    public Larder Larder { get; }

    /// <summary>Shelter built so far. Drives clo, and its milestones drive morale.</summary>
    public ShelterTier Shelter { get; private set; } = ShelterTier.None;

    /// <summary>Slots invested toward the next tier.</summary>
    public float ShelterProgressSlots { get; private set; }

    /// <summary>Insulation available at night: clothing, bag, and whatever is built.</summary>
    public float AvailableClo =>
        ShelterTable.BaseClothingClo + ShelterTable.SleepingBagClo + ShelterTable.Get(Shelter).Clo;

    /// <summary>
    /// Spend a slot on shelter. Bushcraft makes the slot worth more, which is
    /// where that attribute finally earns its keep.
    /// </summary>
    private void AdvanceShelter(BalanceData b)
    {
        var next = NextTier(Shelter);
        if (next == Shelter) return;   // cabin is the ceiling

        float bushcraft = attributes[AttributeKind.Bushcraft];
        ShelterProgressSlots += Harvest.SkillMultiplier((int)bushcraft);

        int needed = ShelterTable.Get(next).Slots - ShelterTable.Get(Shelter).Slots;
        if (ShelterProgressSlots < needed) return;

        ShelterProgressSlots -= needed;
        Shelter = next;

        // Spec 5.6: shelter tier milestone is +12.
        Morale.ApplyEvent(MoraleSource.ShelterMilestone, b.MoraleShelterMilestone, b);
        Record.Trace.Add(new TraceEntry
        {
            Day = DayNumber, Slot = 0, Kind = TraceKind.Action,
            Code = $"shelter.{next}".ToLowerInvariant(),
            Magnitude = ShelterTable.Get(next).Clo
        });
    }

    private static ShelterTier NextTier(ShelterTier current) => current switch
    {
        ShelterTier.None => ShelterTier.TarpLeanTo,
        ShelterTier.TarpLeanTo => ShelterTier.DebrisHut,
        ShelterTier.DebrisHut => ShelterTier.AFrame,
        ShelterTier.AFrame => ShelterTier.ReflectorWallCamp,
        ShelterTier.ReflectorWallCamp => ShelterTier.LogShelter,
        ShelterTier.LogShelter => ShelterTier.LogCabin,
        _ => ShelterTier.LogCabin
    };

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

        var season = Calendar.SeasonForDay(DayNumber);
        float harvestedKg = 0f;

        for (int i = 0; i < Math.Min(slots, plan.Slots.Count); i++)
        {
            var activity = plan.Slots[i];
            burn += EnergyModel.ExcessKcalForSlot(activity, Body, fitness, plan.TerrainMultiplier, b);
            if (activity.IsBuildProgress())
            {
                anyBuildProgress = true;
                if (activity == Activity.ShelterBuild) AdvanceShelter(b);
            }

            // Working a slot may produce food. This is where Hunting and Foraging
            // finally matter, and where two runs on different seeds diverge.
            var governing = Harvest.GoverningAttribute(activity);
            if (governing.HasValue)
            {
                var caught = Harvest.Resolve(activity, season, attributes[governing.Value], Rng);
                if (caught.CaughtSomething)
                {
                    Larder.Add(caught.ProteinG, caught.FatG);
                    harvestedKg += caught.EdibleKg;

                    // Spec 5.6: a large food success is worth +10 morale. Without
                    // this every run is a one-way slide, because the daily losses
                    // have nothing to push back against.
                    if (caught.EdibleKg >= LargeCatchKg)
                        Morale.ApplyEvent(MoraleSource.LargeFoodSuccess, b.MoraleLargeFoodSuccess, b);
                    Record.Trace.Add(new TraceEntry
                    {
                        Day = DayNumber, Slot = i, Kind = TraceKind.Action,
                        Code = $"harvest.{caught.Source}".ToLowerInvariant(),
                        Magnitude = caught.EdibleKg
                    });
                }
            }
        }

        // ---- intake ----
        // Appetite is what the day cost. The larder rarely covers it, and the
        // protein ceiling then caps what of it the body can actually use.
        var meal = plan.DirectRation ?? Larder.Eat(burn, Body.WeightKg, b);
        var nutrition = NutritionModel.Evaluate(meal, Body.WeightKg, b);

        Larder.ApplyDailySpoilage();

        float daysOfFood = Larder.DaysOfFood(Body.WeightKg, b);

        // A DirectRation means food is being supplied outside the larder (tests
        // and scenarios), so it also supplies food SECURITY. Without this the
        // player reads as insecure while being handed a full meal every day.
        bool foodInsecure = plan.DirectRation is null && daysOfFood < 1f;

        // ---- body ----
        float net = nutrition.UsableKcal - burn;
        Body.ApplyEnergyBalance(net, b);

        // ---- morale at the day boundary ----
        var moraleTotals = Morale.EvaluateDay(new MoraleDayInputs
        {
            FoodInsecure = foodInsecure,
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
            HarvestedKg = harvestedKg,
            LarderDaysOfFood = daysOfFood,
            FoodInsecure = foodInsecure,
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
