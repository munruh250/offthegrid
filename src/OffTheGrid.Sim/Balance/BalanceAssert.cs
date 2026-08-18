using System;
using System.Collections.Generic;
using OffTheGrid.Data;
using OffTheGrid.Data.Balance;
using OffTheGrid.Sim.Nutrition;
using OffTheGrid.Sim.Record;
using OffTheGrid.Sim.Time;

namespace OffTheGrid.Sim.Balance;

/// <summary>Outcome of one balance check.</summary>
public readonly record struct BalanceCheckResult(string Name, bool Passed, string Detail)
{
    public override string ToString() => $"{(Passed ? "PASS" : "FAIL")}  {Name}  -  {Detail}";
}

/// <summary>
/// The properties the game must keep, expressed as executable checks rather than
/// prose in a design document.
///
/// These return results rather than throwing, so the solver can run the whole
/// suite across a seed sweep and report a distribution. Tests assert on them
/// individually.
///
/// DO NOT weaken a check to make it pass. Each one guards a property that cost
/// real design work to achieve - FastingBuildLosesTo in particular took an entire
/// morale system, and it is exactly the kind of assertion that gets "fixed" by
/// loosening a tolerance. If a check fails, the model changed; find out why.
/// </summary>
public static class BalanceAssert
{
    private static IReadOnlyDictionary<AttributeKind, int> Build(
        int bushcraft, int hunting, int foraging, int fitness, int resolve, int cold) =>
        new Dictionary<AttributeKind, int>
        {
            [AttributeKind.Bushcraft] = bushcraft,
            [AttributeKind.Hunting] = hunting,
            [AttributeKind.Foraging] = foraging,
            [AttributeKind.Fitness] = fitness,
            [AttributeKind.Resolve] = resolve,
            [AttributeKind.ColdAdaptation] = cold
        };

    private static readonly Activity[] IdleSlots =
        [Activity.Rest, Activity.Rest, Activity.Rest, Activity.Rest, Activity.Rest];

    /// <summary>
    /// A competent day: build, fish, hunt, rest twice. Calibrated over a 50-seed
    /// sweep - this plan averages 59.3 days against doc 7.3's recorded 59.
    ///
    /// Adding a FOURTH productive slot makes things WORSE (53.1 days), because the
    /// extra slot burns more than it returns once the protein ceiling caps what
    /// can be absorbed. There is an optimal work level, and it is not "work
    /// everything".
    /// </summary>
    private static readonly Activity[] ActiveSlots =
        [Activity.ShelterBuild, Activity.Fishing, Activity.HuntingStalk, Activity.Rest, Activity.Rest];

    /// <summary>
    /// Q2, the balance target. A fasting build must lose to competent active play.
    ///
    /// Balance doc 7.2 found the body simulation ALONE cannot achieve this - a
    /// 160 kg faster reaches day 90 on physiology. Morale's idleness penalty is
    /// what defeats it (7.3). This check is the guard on that finding.
    /// </summary>
    public static BalanceCheckResult FastingBuildLosesToCompetentPlay()
    {
        int fasting = SurviveDays(weightKg: 160f, bodyFatPercent: 42f, resolve: 5, slots: IdleSlots);
        int competent = SurviveDays(weightKg: 85f, bodyFatPercent: 20f, resolve: 6, slots: ActiveSlots);

        bool passed = competent > fasting;
        return new BalanceCheckResult(
            nameof(FastingBuildLosesToCompetentPlay),
            passed,
            $"fasting reached day {fasting}, competent reached day {competent}");
    }

    /// <summary>Balance doc 7.1: the food economy must support a 60-day run.</summary>
    public static BalanceCheckResult CompetentPlayerReachesDay60()
    {
        int days = SurviveDays(weightKg: 85f, bodyFatPercent: 20f, resolve: 6, slots: ActiveSlots);

        return new BalanceCheckResult(
            nameof(CompetentPlayerReachesDay60),
            days >= 55,
            $"mean day {days} across the sweep (doc 7.3 records 59)");
    }

    /// <summary>
    /// Balance doc 7.3 records day 12 for an idle, food-insecure player. The
    /// morale model reproduces this independently; if it moves, a constant
    /// drifted or the idleness stack changed shape.
    /// </summary>
    public static BalanceCheckResult IdlePlayerTapsOutAroundDayTwelve()
    {
        int days = SurviveDays(weightKg: 85f, bodyFatPercent: 20f, resolve: 5, slots: IdleSlots);

        return new BalanceCheckResult(
            nameof(IdlePlayerTapsOutAroundDayTwelve),
            days is >= 10 and <= 14,
            $"tapped out on day {days}, expected 12");
    }

    /// <summary>
    /// Balance doc 3.3: only black bear can sustain a player alone. If a second
    /// food clears the bar, the fat economy has drifted and B3 needs revisiting.
    /// </summary>
    public static BalanceCheckResult OnlyBearSustainsAlone()
    {
        var balance = BalanceData.Default;
        var sustaining = new List<string>();

        foreach (var food in Data.Tables.FoodTable.All)
        {
            float maxSafe = NutritionModel.MaxSafeKcalPerDay(
                new Macros(food.ProteinG, food.FatG, 0f), 85f, balance);
            if (maxSafe >= 2500f) sustaining.Add(food.Source.ToString());
        }

        bool passed = sustaining.Count == 1 && sustaining[0] == nameof(Data.Tables.FoodSource.BlackBear);
        return new BalanceCheckResult(
            nameof(OnlyBearSustainsAlone),
            passed,
            sustaining.Count == 0 ? "nothing sustains alone" : $"sustaining: {string.Join(", ", sustaining)}");
    }

    /// <summary>
    /// Doc 12 s4.1: the shelter-loss morale hit must never exceed the rebuild
    /// reward, or relocation is a guaranteed death spiral and never fires.
    /// </summary>
    public static BalanceCheckResult RelocationIsNotADeathSpiral()
    {
        var b = BalanceData.Default;
        bool passed = b.ShelterLossMoraleCap < b.MoraleProjectCompleted;

        return new BalanceCheckResult(
            nameof(RelocationIsNotADeathSpiral),
            passed,
            $"loss cap {b.ShelterLossMoraleCap}, rebuild reward {b.MoraleProjectCompleted}");
    }

    /// <summary>
    /// Spec 7.1: the season must compress the action economy without an authored
    /// curve. Day 60 should give meaningfully fewer slots than day 1.
    /// </summary>
    public static BalanceCheckResult SeasonCompressesActionEconomy()
    {
        int early = Calendar.SlotsForDay(1);
        int late = Calendar.SlotsForDay(60);

        return new BalanceCheckResult(
            nameof(SeasonCompressesActionEconomy),
            late < early,
            $"day 1 gives {early} slots, day 60 gives {late}");
    }

    /// <summary>Run every check. The solver calls this per configuration.</summary>
    public static IReadOnlyList<BalanceCheckResult> RunAll() =>
    [
        FastingBuildLosesToCompetentPlay(),
        CompetentPlayerReachesDay60(),
        IdlePlayerTapsOutAroundDayTwelve(),
        OnlyBearSustainsAlone(),
        RelocationIsNotADeathSpiral(),
        SeasonCompressesActionEconomy()
    ];

    /// <summary>
    /// Mean days survived across a seed sweep. Averaging is required now that
    /// harvesting consumes RNG - a single run varies by 10+ days and a check
    /// built on one seed would flap.
    /// </summary>
    private static int SurviveDays(
        float weightKg, float bodyFatPercent, int resolve, Activity[] slots,
        int seeds = 25, int cap = 120)
    {
        int total = 0;
        for (ulong seed = 0; seed < (ulong)seeds; seed++)
        {
            var run = new Run(
                seed, Sex.Male, heightCm: 180, ageYears: 35,
                weightKg, bodyFatPercent,
                Build(5, 6, 3, 8, resolve, 5));

            var plan = new DayPlan { Slots = slots };
            while (!run.IsOver && run.DayNumber < cap) run.StepDay(plan);
            total += run.DayNumber;
        }
        return total / seeds;
    }
}
