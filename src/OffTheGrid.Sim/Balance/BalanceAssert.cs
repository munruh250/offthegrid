using System;
using System.Collections.Generic;
using OffTheGrid.Data;
using OffTheGrid.Data.Balance;
using OffTheGrid.Sim.Food;
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
    /// A competent day: build, fish, run the trap line, whittle, rest.
    ///
    /// Found by plan search over 150 seeds. Two things drive it. The trap line is
    /// the reliable small-game floor - stalking is high-variance and mostly
    /// misses. And whittling is the only REPEATABLE morale income: shelter
    /// milestones run out after six tiers and big kills are luck, so a plan
    /// without comfort projects taps out in the forties no matter how well it is
    /// fed. Swapping the hunt slot for whittling is worth roughly seven days.
    /// </summary>
    private static readonly Activity[] ActiveSlots =
        [Activity.ShelterBuild, Activity.Fishing, Activity.TrapLine, Activity.WhittleComfortProject, Activity.Rest];

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

    /// <summary>
    /// Competent play must complete the WINTERIZATION ARC - reach shelter and
    /// fuel that hold the coldest night - before winter arrives.
    ///
    /// This replaces a day-count check, and the replacement is the point. Once
    /// the season schedule is a scenario parameter, "did the player reach day 60"
    /// measures the wrong thing: a run ending on day 43 under a standard schedule
    /// looks like failure, when under a short-summer schedule the same player
    /// would have survived winter and won. The arc is what the player is actually
    /// racing, and it holds whenever winter lands.
    /// </summary>
    public static BalanceCheckResult CompetentPlayerWinterizesInTime(
        SeasonSchedule? schedule = null, Biome? biome = null)
    {
        var sched = schedule ?? SeasonSchedule.Standard;
        int winterized = 0, reachedWinter = 0;
        const int seeds = 25;

        for (ulong seed = 0; seed < seeds; seed++)
        {
            var run = NewRun(seed, 85f, 20f, resolve: 6, schedule: sched, biome: biome);
            var plan = new DayPlan
            {
                Slots = [Activity.ShelterBuild, Activity.ChoppingWood, Activity.Fishing, Activity.TrapLine, Activity.WhittleComfortProject]
            };
            bool everWinterized = false;

            while (!run.IsOver && run.DayNumber < sched.WinterArrives)
            {
                run.StepDay(plan);
                if (run.IsWinterized) everWinterized = true;
            }

            if (everWinterized) winterized++;
            if (!run.IsOver) reachedWinter++;
        }

        float rate = winterized / (float)seeds;
        return new BalanceCheckResult(
            nameof(CompetentPlayerWinterizesInTime),
            rate >= 0.7f,
            $"{rate:P0} winterized before day {sched.WinterArrives}; {reachedWinter * 100 / seeds}% still alive at winter");
    }

    /// <summary>
    /// A short summer must be genuinely harder than a standard one. If the
    /// schedule parameter does not change outcomes, movable seasons are cosmetic.
    ///
    /// Measured as DAYS SURVIVED rather than as a winterization rate. The first
    /// version of this check compared how many players got their shelter up, and
    /// it could not fail: even a three-week summer leaves enough slots to build,
    /// because shelter was never the binding constraint - food and time were.
    /// Survival captures the whole squeeze, which is what a shorter season
    /// actually tightens.
    /// </summary>
    public static BalanceCheckResult ShortSummerIsHarderThanStandard()
    {
        static float MeanDays(SeasonSchedule sched)
        {
            const int seeds = 25;
            int total = 0;
            for (ulong seed = 0; seed < seeds; seed++)
            {
                var run = NewRun(seed, 85f, 20f, resolve: 6, schedule: sched, biome: Biome.BorealInterior);
                var plan = new DayPlan
                {
                    Slots = [Activity.ShelterBuild, Activity.ChoppingWood, Activity.Fishing, Activity.TrapLine, Activity.WhittleComfortProject]
                };
                while (!run.IsOver && run.DayNumber < 120) run.StepDay(plan);
                total += run.DayNumber;
            }
            return total / (float)seeds;
        }

        float standard = MeanDays(SeasonSchedule.Standard);
        float shortSummer = MeanDays(SeasonSchedule.ShortSummer);

        return new BalanceCheckResult(
            nameof(ShortSummerIsHarderThanStandard),
            shortSummer < standard,
            $"mean days survived - standard {standard:F1}, short summer {shortSummer:F1}");
    }

    /// <summary>
    /// A player who never winterizes must be punished for it once winter lands.
    /// If not, the shelter, fuel and cold economies are decorative.
    /// </summary>
    public static BalanceCheckResult FailingToWinterizeIsPunished()
    {
        const int seeds = 25;
        int preparedAlive = 0, unpreparedAlive = 0;
        var sched = SeasonSchedule.ShortSummer;
        int checkDay = sched.WinterArrives + 14;

        for (ulong seed = 0; seed < seeds; seed++)
        {
            // Builds shelter and cuts wood.
            var prepared = NewRun(seed, 85f, 20f, 6, sched, Biome.BorealInterior);
            var preparedPlan = new DayPlan { Slots = [Activity.ShelterBuild, Activity.ChoppingWood, Activity.Fishing, Activity.TrapLine, Activity.WhittleComfortProject] };
            while (!prepared.IsOver && prepared.DayNumber < checkDay) prepared.StepDay(preparedPlan);
            if (!prepared.IsOver) preparedAlive++;

            // Ignores shelter entirely and chases food.
            var unprepared = NewRun(seed, 85f, 20f, 6, sched, Biome.BorealInterior);
            var unpreparedPlan = new DayPlan { Slots = [Activity.Fishing, Activity.TrapLine, Activity.Foraging, Activity.Fishing, Activity.WhittleComfortProject] };
            while (!unprepared.IsOver && unprepared.DayNumber < checkDay) unprepared.StepDay(unpreparedPlan);
            if (!unprepared.IsOver) unparedGuard(ref unpreparedAlive);
        }

        return new BalanceCheckResult(
            nameof(FailingToWinterizeIsPunished),
            preparedAlive > unpreparedAlive,
            $"14 days into an arctic winter: prepared {preparedAlive}/{seeds} alive, unprepared {unpreparedAlive}/{seeds}");

        static void unparedGuard(ref int n) => n++;
    }

    /// <summary>
    /// An idle, food-insecure player must fail fast.
    ///
    /// Balance doc 7.3 records day 12 for this scenario, from morale attrition
    /// alone. The measured figure is now ~6, because the model has since gained
    /// VOLUNTARY TAP-OUT - a contestant doing nothing while starving does not
    /// grind their morale bar down to zero over twelve days, they decide to leave.
    /// That is both more faithful to the format and a legitimate reason for the
    /// number to move, so the check is rebased rather than the mechanism removed.
    ///
    /// The property being guarded is unchanged: idle-and-starving fails fast, and
    /// far faster than competent play. If this climbs back toward the competent
    /// figure, Q2 is at risk.
    /// </summary>
    public static BalanceCheckResult IdlePlayerFailsFast()
    {
        int days = SurviveDays(weightKg: 85f, bodyFatPercent: 20f, resolve: 5, slots: IdleSlots);

        return new BalanceCheckResult(
            nameof(IdlePlayerFailsFast),
            days is >= 3 and <= 14,
            $"failed on day {days} (doc 7.3 records 12 for morale-only attrition)");
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
        CompetentPlayerWinterizesInTime(),
        ShortSummerIsHarderThanStandard(),
        FailingToWinterizeIsPunished(),
        IdlePlayerFailsFast(),
        OnlyBearSustainsAlone(),
        RelocationIsNotADeathSpiral(),
        SeasonCompressesActionEconomy()
    ];

    /// <summary>
    /// Mean days survived across a seed sweep. Averaging is required now that
    /// harvesting consumes RNG - a single run varies by 10+ days and a check
    /// built on one seed would flap.
    /// </summary>
    private static Run NewRun(ulong seed, float weightKg, float bodyFatPercent, int resolve,
                              SeasonSchedule? schedule = null, Biome? biome = null) =>
        new(seed, Sex.Male, 180, 35, weightKg, bodyFatPercent,
            Build(5, 6, 3, 8, resolve, 5), null, null, biome, schedule);

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
