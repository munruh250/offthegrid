namespace OffTheGrid.Tests;

using System.Collections.Generic;
using System.Linq;
using Xunit;
using OffTheGrid.Data;
using OffTheGrid.Sim;
using OffTheGrid.Sim.Nutrition;
using OffTheGrid.Sim.Record;
using OffTheGrid.Sim.Time;

public sealed class RunTests
{
    private static IReadOnlyDictionary<AttributeKind, int> Competent => new Dictionary<AttributeKind, int>
    {
        [AttributeKind.Bushcraft] = 5,
        [AttributeKind.Hunting] = 6,
        [AttributeKind.Foraging] = 3,
        [AttributeKind.Fitness] = 8,
        [AttributeKind.Resolve] = 10,
        [AttributeKind.ColdAdaptation] = 5
    };

    private static Run NewRun(ulong seed = 1, float weightKg = 85, float bodyFatPercent = 20) =>
        new(seed, Sex.Male, heightCm: 180, ageYears: 35, weightKg, bodyFatPercent, Competent);

    /// <summary>
    /// An ordinary day: some real work, and rest for the remaining slots. Filling
    /// every slot with heavy labour is not affordable - see
    /// <see cref="AllHeavyDayIsUnaffordable"/>, which is the point.
    /// </summary>
    private static DayPlan WorkingDay(Macros eaten, bool foodInsecure = false) => new()
    {
        Slots = new[] { Activity.ShelterBuild, Activity.WhittleComfortProject, Activity.Rest, Activity.Rest, Activity.Rest },
        DirectRation = eaten
    };

    private static DayPlan AllHeavyDay(Macros eaten) => new()
    {
        Slots = new[] { Activity.HaulingLogs, Activity.ChoppingWood, Activity.Exploring, Activity.HuntingStalk, Activity.Sawing },
        DirectRation = eaten
    };

    private static DayPlan IdleDay => new()
    {
        Slots = new[] { Activity.Rest, Activity.Rest, Activity.Rest, Activity.Rest, Activity.Rest }
    };

    // Roughly a day's worth of bear: fatty, sustainable alone, ~2,780 kcal.
    private static Macros BearRation => new(ProteinG: 200, FatG: 220, CarbohydrateG: 0);

    // Same calories from a lean source. Should waste heavily.
    private static Macros LeanRation => new(ProteinG: 700, FatG: 30, CarbohydrateG: 0);

    [Fact]
    public void RunStartsUnfinished()
    {
        var run = NewRun();
        Assert.False(run.IsOver);
        Assert.Equal(0, run.DayNumber);
        Assert.Equal(EndCondition.None, run.Record.EndCondition);
    }

    [Fact]
    public void StepDayAdvancesAndBurnsEnergy()
    {
        var run = NewRun();
        var result = run.StepDay(WorkingDay(BearRation));

        Assert.Equal(1, result.DayNumber);
        Assert.Equal(5, result.SlotsAvailable);
        // Balance doc 3.3 puts a day's burn at ~2,850 kcal for an 85 kg player.
        Assert.InRange(result.BurnKcal, 2300f, 3400f);
    }

    [Fact]
    public void SlotsShrinkAsTheSeasonAdvances()
    {
        // Fed directly so the run survives long enough to observe the slot curve.
        var run = NewRun();
        // Fed directly AND building, so neither starvation nor the idleness
        // penalty ends the run before the slot curve can be observed.
        var fed = new DayPlan
        {
            Slots = new[] { Activity.ShelterBuild, Activity.WhittleComfortProject, Activity.Rest, Activity.Rest, Activity.Rest },
            DirectRation = new Macros(200f, 400f, 0f)
        };

        var early = run.StepDay(fed);
        for (int i = 0; i < 58; i++) run.StepDay(fed);
        var late = run.StepDay(fed);

        Assert.Equal(5, early.SlotsAvailable);
        Assert.Equal(3, late.SlotsAvailable);
    }

    [Fact]
    public void LeanFoodWastesCaloriesAndFattyFoodDoesNot()
    {
        // The signature mechanic, end to end through the day loop.
        var lean = NewRun();
        var leanDay = lean.StepDay(WorkingDay(LeanRation));

        var fatty = NewRun();
        var fattyDay = fatty.StepDay(WorkingDay(BearRation));

        Assert.True(leanDay.ProteinCeilingBound, "a heavy lean ration must bind the ceiling");
        Assert.True(leanDay.WastedIntakeKcal > 0f);

        Assert.False(fattyDay.ProteinCeilingBound);
        Assert.Equal(0f, fattyDay.WastedIntakeKcal, 0.01f);
    }

    [Fact]
    public void FullCacheStillStarving()
    {
        // B1 in one test. The player eats a large lean ration every single day -
        // gross calories well above their burn - and wastes away regardless,
        // because the protein ceiling caps what the body can extract.
        var run = NewRun();
        int day = 0;
        int daysBound = 0;
        float totalGross = 0f, totalUsable = 0f, totalBurn = 0f;

        while (!run.IsOver && day < 200)
        {
            day++;
            var result = run.StepDay(WorkingDay(LeanRation));
            if (result.ProteinCeilingBound) daysBound++;
            totalGross += result.UsableIntakeKcal + result.WastedIntakeKcal;
            totalUsable += result.UsableIntakeKcal;
            totalBurn += result.BurnKcal;
        }

        Assert.True(run.IsOver, "a diet of pure lean meat must end the run");
        Assert.Equal(day, daysBound);
        Assert.True(totalGross > totalBurn,
            "the player was eating MORE gross calories than they burned");
        Assert.True(totalUsable < totalBurn * 0.6f,
            "yet could absorb well under what they needed");
    }

    [Fact]
    public void LeanDietEndsRunSoonerThanFattyDiet()
    {
        static int Survive(Macros ration)
        {
            var run = NewRun();
            while (!run.IsOver && run.DayNumber < 200) run.StepDay(WorkingDay(ration));
            return run.DayNumber;
        }

        Assert.True(Survive(LeanRation) < Survive(BearRation));
    }

    [Fact]
    public void IdlePlayerTapsOutOnMorale()
    {
        var run = NewRun();
        int day = 0;
        while (!run.IsOver && day < 200)
        {
            day++;
            run.StepDay(IdleDay);
        }

        Assert.True(run.IsOver);
        Assert.Equal(EndCondition.TapOut, run.Record.EndCondition);
        Assert.StartsWith("morale.", run.Record.CauseCode);
    }

    [Fact]
    public void CauseCodeNamesTheStoryNotJustTheRule()
    {
        // Spec 6: "pulled at 17.1 BMI" is a rules citation. The cause code has to
        // carry the underlying reason.
        var run = NewRun();
        while (!run.IsOver && run.DayNumber < 200) run.StepDay(IdleDay);

        Assert.NotNull(run.Record.CauseCode);
        Assert.NotEqual("morale.unknown", run.Record.CauseCode);
    }

    [Fact]
    public void BuildingClearsTheIdlenessPenalty()
    {
        var building = NewRun();
        var resting = NewRun();

        for (int i = 0; i < 10; i++)
        {
            building.StepDay(WorkingDay(BearRation));
            resting.StepDay(new DayPlan
            {
                Slots = new[] { Activity.Rest, Activity.Rest, Activity.Rest, Activity.Rest, Activity.Rest },
                DirectRation = BearRation
            });
        }

        Assert.Equal(0, building.Morale.ConsecutiveIdleDays);
        Assert.Equal(10, resting.Morale.ConsecutiveIdleDays);
        Assert.True(building.Morale.Current > resting.Morale.Current);
    }

    [Fact]
    public void AllHeavyDayIsUnaffordable()
    {
        // Balance doc 1.1: "Calories are the scarce resource. Everything else is a
        // tax on the slots available to get calories." Filling every slot with
        // heavy labour burns far more than any single day's ration returns, so the
        // slot economy is self-limiting without needing a stamina bar.
        var run = NewRun();
        var heavy = run.StepDay(AllHeavyDay(BearRation));

        Assert.True(heavy.BurnKcal > 4000f, $"an all-heavy day should be brutal, got {heavy.BurnKcal:F0}");
        Assert.True(heavy.NetKcal < -1000f, "and it should leave a large deficit");
    }

    [Fact]
    public void CompetentPlayerHoldsWeightEarly()
    {
        // Balance doc 7.1: a competent player loses only ~1.6 kg over days 1-10.
        var run = NewRun();
        for (int i = 0; i < 10; i++) run.StepDay(WorkingDay(BearRation));

        float lost = 85f - run.Body.WeightKg;
        Assert.InRange(lost, 0f, 4f);
        Assert.False(run.IsOver);
    }

    [Fact]
    public void HeavyBodyPaysMoreToMove()
    {
        // The superlinear movement cost, (W/80)^1.15.
        var light = NewRun(weightKg: 70, bodyFatPercent: 12);
        var heavy = NewRun(weightKg: 140, bodyFatPercent: 42);

        var movingDay = new DayPlan
        {
            Slots = new[] { Activity.Exploring, Activity.Exploring, Activity.Exploring },
            DirectRation = default(Macros)
        };


        float lightBurn = light.StepDay(movingDay).BurnKcal;
        float heavyBurn = heavy.StepDay(movingDay).BurnKcal;

        Assert.True(heavyBurn > lightBurn * 2f,
            $"heavy body should pay superlinearly: {heavyBurn:F0} vs {lightBurn:F0}");
    }

    // ---- determinism ----

    [Fact]
    public void SameSeedAndPlanProduceSameChecksum()
    {
        // The property the cross-device test asserts (doc 13).
        static ulong RunToEnd(ulong seed)
        {
            var run = new Run(seed, Sex.Male, 180, 35, 85, 20, Competent);
            while (!run.IsOver && run.DayNumber < 100) run.StepDay(IdleDay);
            return run.Record.FinalChecksum;
        }

        Assert.Equal(RunToEnd(42), RunToEnd(42));
    }

    [Fact]
    public void RunRecordCapturesTheWholeRun()
    {
        var run = NewRun(seed: 777);
        while (!run.IsOver && run.DayNumber < 200) run.StepDay(IdleDay);

        Assert.Equal(777UL, run.Record.Seed);
        Assert.True(run.Record.DaysSurvived > 0);
        Assert.NotEqual(EndCondition.None, run.Record.EndCondition);
        Assert.NotEqual(0UL, run.Record.FinalChecksum);
        Assert.True(run.Record.IsCleanBalanceSample);
        Assert.NotEmpty(run.Record.Trace.ToOrderedList());
    }

    [Fact]
    public void FinishedRunRejectsFurtherDays()
    {
        var run = NewRun();
        while (!run.IsOver && run.DayNumber < 200) run.StepDay(IdleDay);

        Assert.Throws<System.InvalidOperationException>(() => run.StepDay(IdleDay));
    }
}
