using System;
using System.Collections.Generic;
using System.Linq;
using OffTheGrid.Data;
using OffTheGrid.Data.Gear;
using OffTheGrid.Sim.Nutrition;
using OffTheGrid.Sim.Record;
using OffTheGrid.Sim.Time;

namespace OffTheGrid.Sim.Balance;

/// <summary>One configuration to sweep: a build plus a play strategy.</summary>
public sealed class Scenario
{
    public required string Name { get; init; }
    public required float WeightKg { get; init; }
    public required float BodyFatPercent { get; init; }
    public Sex Sex { get; init; } = Sex.Male;
    public float HeightCm { get; init; } = 180f;
    public int AgeYears { get; init; } = 35;
    public required IReadOnlyDictionary<AttributeKind, int> Attributes { get; init; }
    public required Activity[] Slots { get; init; }
    public Loadout Gear { get; init; } = Loadout.Standard;
}

/// <summary>Aggregate outcome of sweeping one scenario.</summary>
public readonly record struct ScenarioOutcome
{
    public required string Scenario { get; init; }
    public required int Runs { get; init; }
    public required float MeanDaysSurvived { get; init; }
    public required int MinDays { get; init; }
    public required int MaxDays { get; init; }
    public required IReadOnlyDictionary<EndCondition, int> EndConditions { get; init; }

    /// <summary>
    /// Share of runs ended by the most common cause. Design spec 5.5 sets a 60%
    /// dominance ceiling - no single end condition should account for more than
    /// that, or the other three are decoration.
    /// </summary>
    public float DominantShare =>
        Runs == 0 ? 0f : EndConditions.Values.Max() / (float)Runs;

    public EndCondition DominantCause =>
        EndConditions.Count == 0
            ? EndCondition.None
            : EndConditions.OrderByDescending(kv => kv.Value).First().Key;
}

/// <summary>
/// Headless sweep harness. Runs scenarios to completion and aggregates outcomes.
///
/// [NOTE] The seed dimension is wired but currently degenerate: the day loop does
/// not yet consume RNG, so every seed for a given scenario produces an identical
/// run. Seed sweeps become meaningful once hunt resolution, weather and events
/// land. Until then the useful axis is the SCENARIO sweep - comparing builds and
/// strategies - which is already informative.
///
/// Allocation matters here (A15/C9). This never calls Snapshot() or Breakdown().
/// </summary>
public static class Solver
{
    public static ScenarioOutcome Sweep(Scenario scenario, int runs = 1, int dayCap = 200)
    {
        if (runs <= 0) throw new ArgumentOutOfRangeException(nameof(runs));

        var endConditions = new Dictionary<EndCondition, int>();
        int total = 0, min = int.MaxValue, max = 0;

        for (int i = 0; i < runs; i++)
        {
            var run = new Run(
                seed: (ulong)i,
                scenario.Sex, scenario.HeightCm, scenario.AgeYears,
                scenario.WeightKg, scenario.BodyFatPercent,
                scenario.Attributes,
                scenario.Gear);

            var plan = new DayPlan { Slots = scenario.Slots };

            while (!run.IsOver && run.DayNumber < dayCap) run.StepDay(plan);

            int days = run.DayNumber;
            total += days;
            min = Math.Min(min, days);
            max = Math.Max(max, days);

            var end = run.Record.EndCondition;
            endConditions[end] = endConditions.GetValueOrDefault(end) + 1;
        }

        return new ScenarioOutcome
        {
            Scenario = scenario.Name,
            Runs = runs,
            MeanDaysSurvived = total / (float)runs,
            MinDays = min,
            MaxDays = max,
            EndConditions = endConditions
        };
    }

    public static IReadOnlyList<ScenarioOutcome> SweepAll(
        IEnumerable<Scenario> scenarios, int runs = 1, int dayCap = 200) =>
        scenarios.Select(s => Sweep(s, runs, dayCap)).ToArray();

    // ---- standard scenarios ----

    private static IReadOnlyDictionary<AttributeKind, int> Attributes(
        int bushcraft, int hunting, int foraging, int fitness, int resolve, int cold,
        int fishing = 5) =>
        new Dictionary<AttributeKind, int>
        {
            [AttributeKind.Bushcraft] = bushcraft,
            [AttributeKind.Hunting] = hunting,
            [AttributeKind.Fishing] = fishing,
            [AttributeKind.Foraging] = foraging,
            [AttributeKind.Fitness] = fitness,
            [AttributeKind.Resolve] = resolve,
            [AttributeKind.ColdAdaptation] = cold
        };

    /// <summary>
    /// The archetype presets from design spec 4.2, each playing three productive
    /// slots to its own strength.
    ///
    /// Three is deliberate: a 50-seed sweep found four productive slots performs
    /// WORSE than three (53 days against 59), because the fourth burns more than
    /// the protein ceiling lets the player absorb. Holding the slot count equal
    /// across archetypes keeps the comparison about attributes and body
    /// composition rather than about who was handed the better plan.
    /// </summary>
    public static IReadOnlyList<Scenario> Archetypes =>
    [
        new()
        {
            Name = "Ex-Military",
            WeightKg = 84f, BodyFatPercent = 18f,
            Attributes = Attributes(5, 6, 3, 8, 6, 5),
            // Balanced field kit: can hunt, fish, trap and build.
            Gear = new Loadout(GearItem.BowAndArrows, GearItem.Axe, GearItem.Saw, GearItem.Knife,
                               GearItem.SnareWire, GearItem.FishingLineAndHooks, GearItem.Pot,
                               GearItem.SleepingBag, GearItem.Tarp, GearItem.FerroRod),
            Slots = [Activity.ShelterBuild, Activity.Fishing, Activity.TrapLine, Activity.WhittleComfortProject, Activity.Rest]
        },
        new()
        {
            Name = "Bushcraft Instructor",
            WeightKg = 88f, BodyFatPercent = 26f,
            Attributes = Attributes(8, 5, 6, 4, 5, 5),
            // Tools over weapons - no bow. Builds a cabin, cannot take big game.
            Gear = new Loadout(GearItem.Axe, GearItem.Saw, GearItem.Knife, GearItem.Pot,
                               GearItem.SnareWire, GearItem.FishingLineAndHooks, GearItem.Paracord,
                               GearItem.SleepingBag, GearItem.Tarp, GearItem.FerroRod),
            Slots = [Activity.ShelterBuild, Activity.TrapLine, Activity.Foraging, Activity.WhittleComfortProject, Activity.Rest]
        },
        new()
        {
            Name = "Commercial Fisherman",
            WeightKg = 98f, BodyFatPercent = 31f,
            Attributes = Attributes(5, 6, 3, 5, 6, 8),
            // The gillnet is the whole plan.
            Gear = new Loadout(GearItem.Gillnet, GearItem.FishingLineAndHooks, GearItem.Axe,
                               GearItem.Knife, GearItem.Pot, GearItem.SnareWire, GearItem.Paracord,
                               GearItem.SleepingBag, GearItem.Tarp, GearItem.FerroRod),
            Slots = [Activity.ShelterBuild, Activity.Fishing, Activity.TrapLine, Activity.WhittleComfortProject, Activity.Rest]
        },
        new()
        {
            Name = "Endurance Athlete",
            WeightKg = 72f, BodyFatPercent = 12f,
            Attributes = Attributes(3, 4, 4, 9, 7, 6),
            // Light, mobile kit. The plan is to RANGE - prospect out to better
            // ground early, then work it. Fitness 9 makes exploring cheap.
            Gear = new Loadout(GearItem.BowAndArrows, GearItem.Knife, GearItem.SnareWire,
                               GearItem.FishingLineAndHooks, GearItem.Pot, GearItem.Axe,
                               GearItem.Paracord, GearItem.SleepingBag, GearItem.Tarp, GearItem.FerroRod),
            Slots = [Activity.Exploring, Activity.Fishing, Activity.TrapLine, Activity.WhittleComfortProject, Activity.Rest]
        },
        new()
        {
            Name = "Fasting build (idle)",
            WeightKg = 160f, BodyFatPercent = 42f,
            Attributes = Attributes(4, 4, 4, 4, 5, 5),
            Gear = Loadout.Standard,
            Slots = [Activity.Rest, Activity.Rest, Activity.Rest, Activity.Rest, Activity.Rest]
        }
    ];
}
