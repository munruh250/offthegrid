using System;
using System.Collections.Generic;
using System.Linq;
using OffTheGrid.Data;
using OffTheGrid.Data.Balance;
using OffTheGrid.Data.Gear;
using OffTheGrid.Sim.Food;
using OffTheGrid.Sim.Record;
using OffTheGrid.Sim.Time;

namespace OffTheGrid.Sim.Contest;

/// <summary>One contestant in the field.</summary>
public sealed class Contestant
{
    public required string Name { get; init; }
    public required Run Run { get; init; }
    public required Personality Personality { get; init; }

    /// <summary>True for the human player. The player never uses RivalPolicy.</summary>
    public bool IsPlayer { get; init; }

    public int DayEnded { get; set; }
    public EndCondition EndCondition => Run.Record.EndCondition;
    public bool IsOut => Run.IsOver;
}

/// <summary>Standings as of a given day - what a check-in would tell you.</summary>
public readonly record struct Standings(int Day, int Remaining, int Total, IReadOnlyList<string> RecentlyOut);

/// <summary>
/// Ten contestants, one winner. Design spec 12.1: last one standing, binary.
///
/// WHY THIS MATTERS BEYOND HAVING AN ENDING. The win condition is RELATIVE, and
/// that changes what the game is balanced for. Measured, the archetypes carry
/// fat batteries from 66,528 to 233,926 kcal - a 167,000 kcal spread worth ~88
/// days of survival, roughly five times what the entire attribute budget is
/// worth. Against an absolute target ("reach day 60") the heaviest build simply
/// wins and no attribute tuning can close that.
///
/// Against a FIELD, the battery only has to cover the contest. If the roster
/// taps out around day 35 then every build has enough, and the contest is decided
/// by decisions and crises instead of by starting body fat. The field is
/// therefore both the ending AND the mechanism that makes build variety work.
/// </summary>
public sealed class Contest
{
    private readonly BalanceData balance = BalanceData.Default;

    public Contest(
        ulong seed,
        IReadOnlyList<ContestantSpec> roster,
        Biome? biome = null,
        SeasonSchedule? schedule = null)
    {
        Seed = seed;
        Biome = biome ?? Biome.VancouverIsland;
        Schedule = schedule ?? SeasonSchedule.Standard;

        var built = new List<Contestant>();
        for (int i = 0; i < roster.Count; i++)
        {
            var spec = roster[i];

            // Each contestant gets its own deterministic stream, derived from the
            // contest seed and its slot. Same contest seed reproduces the same
            // field exactly; different slots never share draws.
            ulong contestantSeed = Rng.StableHash($"contestant.{i}") ^ seed;

            built.Add(new Contestant
            {
                Name = spec.Name,
                Personality = spec.Personality,
                IsPlayer = spec.IsPlayer,
                Run = new Run(
                    contestantSeed, spec.Sex, spec.HeightCm, spec.AgeYears,
                    spec.WeightKg, spec.BodyFatPercent, spec.Attributes,
                    spec.Gear, null, Biome, Schedule)
            });
        }

        Field = built;
    }

    public ulong Seed { get; }
    public Biome Biome { get; }
    public SeasonSchedule Schedule { get; }
    public IReadOnlyList<Contestant> Field { get; }

    public int Day { get; private set; }
    public bool IsOver { get; private set; }

    /// <summary>Who won, once the contest is over. Null if the field wiped out simultaneously.</summary>
    public Contestant? Winner { get; private set; }

    public Contestant? Player => Field.FirstOrDefault(c => c.IsPlayer);
    public int Remaining => Field.Count(c => !c.IsOut);

    /// <summary>
    /// Advance one day for everyone still in. The player's plan is supplied;
    /// rivals decide for themselves.
    /// </summary>
    public void StepDay(Activity[]? playerPlan = null)
    {
        if (IsOver) throw new InvalidOperationException("contest is over");
        Day++;

        foreach (var c in Field)
        {
            if (c.IsOut) continue;

            int slots = Calendar.SlotsForDay(c.Run.DayNumber + 1);
            var plan = c.IsPlayer && playerPlan is not null
                ? playerPlan
                : RivalPolicy.PlanDay(c.Run, c.Personality, slots);

            c.Run.StepDay(new DayPlan { Slots = plan });

            if (c.Run.IsOver && c.DayEnded == 0) c.DayEnded = c.Run.DayNumber;
        }

        Resolve();
    }

    private void Resolve()
    {
        var standing = Field.Where(c => !c.IsOut).ToArray();

        if (standing.Length == 1)
        {
            IsOver = true;
            Winner = standing[0];
            Winner.Run.Record.EndCondition = EndCondition.LastOut;
            Winner.Run.Record.CauseCode = "lastout.won";
            Winner.DayEnded = Winner.Run.DayNumber;
        }
        else if (standing.Length == 0)
        {
            // Everyone out on the same day. Whoever got furthest takes it.
            IsOver = true;
            Winner = Field.OrderByDescending(c => c.DayEnded).FirstOrDefault();
        }
    }

    /// <summary>
    /// What a check-in reveals. Design spec 11.1 priced this at half a day; it is
    /// modelled as free and periodic instead, which is both what the show does
    /// (the crew comes to you) and better paced - news that lands every couple of
    /// weeks reads, where a live counter stops being looked at.
    /// </summary>
    public Standings CheckIn(int sinceDay = 0) => new(
        Day,
        Remaining,
        Field.Count,
        Field.Where(c => c.IsOut && c.DayEnded > sinceDay)
             .OrderBy(c => c.DayEnded)
             .Select(c => $"{c.Name} (day {c.DayEnded}, {c.Run.Record.CauseCode})")
             .ToArray());

    /// <summary>Final placings, best first.</summary>
    public IReadOnlyList<Contestant> Placings =>
        Field.OrderByDescending(c => c.IsOut ? c.DayEnded : int.MaxValue).ToArray();
}

/// <summary>The definition of a contestant before the contest starts.</summary>
public sealed class ContestantSpec
{
    public required string Name { get; init; }
    public required IReadOnlyDictionary<AttributeKind, int> Attributes { get; init; }
    public required float WeightKg { get; init; }
    public required float BodyFatPercent { get; init; }
    public Personality Personality { get; init; } = Personality.SteadyProvider;
    public Loadout Gear { get; init; } = Loadout.Standard;
    public Sex Sex { get; init; } = Sex.Male;
    public float HeightCm { get; init; } = 178f;
    public int AgeYears { get; init; } = 35;
    public bool IsPlayer { get; init; }
}
