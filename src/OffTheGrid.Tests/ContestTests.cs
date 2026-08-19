namespace OffTheGrid.Tests;

using System.Linq;
using Xunit;
using OffTheGrid.Data;
using OffTheGrid.Sim.Contest;
using OffTheGrid.Sim.Record;

public sealed class ContestTests
{
    private static Contest RunToEnd(ulong seed)
    {
        var contest = new Contest(seed, Roster.Standard());
        while (!contest.IsOver && contest.Day < 140) contest.StepDay();
        return contest;
    }

    [Fact]
    public void ContestProducesAWinner()
    {
        // EndCondition.LastOut had never once executed before the contest existed
        // - the game had no way to be won.
        var contest = RunToEnd(1);

        Assert.True(contest.IsOver);
        Assert.NotNull(contest.Winner);
        Assert.Equal(EndCondition.LastOut, contest.Winner!.Run.Record.EndCondition);
    }

    [Fact]
    public void EveryContestantSpendsTheSameAttributePoints()
    {
        // The fairness guarantee: nobody in the field is handed better numbers,
        // only different priorities.
        foreach (var spec in Roster.Standard())
        {
            int total = spec.Attributes.Values.Sum();
            Assert.Equal(33, total);
        }
    }

    [Fact]
    public void ContestIsDeterministic()
    {
        var a = RunToEnd(42);
        var b = RunToEnd(42);

        Assert.Equal(a.Winner!.Name, b.Winner!.Name);
        Assert.Equal(a.Day, b.Day);
        Assert.Equal(
            a.Field.Select(c => c.DayEnded),
            b.Field.Select(c => c.DayEnded));
    }

    [Fact]
    public void DifferentSeedsProduceDifferentContests()
    {
        var results = Enumerable.Range(0, 30)
            .Select(i => RunToEnd((ulong)i).Winner!.Name)
            .Distinct()
            .ToArray();

        Assert.True(results.Length > 1, "the same contestant must not always win");
    }

    [Fact]
    public void ContestantsShareTheirRunSeedWithNobody()
    {
        var contest = new Contest(7, Roster.Standard());
        var seeds = contest.Field.Select(c => c.Run.Rng.RunSeed).ToArray();
        Assert.Equal(seeds.Length, seeds.Distinct().Count());
    }

    [Fact]
    public void TapOutsAreSpreadRatherThanClumped()
    {
        // If the field dies in a clump, every check-in reports the same thing and
        // the standings are worthless. Spread is what makes intel worth having.
        var days = new System.Collections.Generic.List<int>();
        for (ulong seed = 0; seed < 40; seed++)
            days.AddRange(RunToEnd(seed).Field.Where(c => !c.IsPlayer).Select(c => c.DayEnded));

        days.Sort();
        int p25 = days[days.Count / 4], p75 = days[days.Count * 3 / 4];

        Assert.True(p75 - p25 >= 8, $"tap-outs are too clumped: p25 {p25}, p75 {p75}");
        Assert.True(days[0] < 20, "somebody should go out early");
        Assert.True(days[^1] > 50, "somebody should go deep");
    }

    [Fact]
    public void CheckInReportsWhoHasGoneOut()
    {
        var contest = new Contest(3, Roster.Standard());
        for (int i = 0; i < 40 && !contest.IsOver; i++) contest.StepDay();

        var standings = contest.CheckIn();
        Assert.Equal(10, standings.Total);
        Assert.True(standings.Remaining < 10, "somebody should be out by day 40");
        Assert.NotEmpty(standings.RecentlyOut);
    }

    [Fact]
    public void RivalsPlayDifferentlyByTemperament()
    {
        // Same code, different weights - the thing that stops nine simulations
        // being statistical noise.
        var contest = new Contest(11, Roster.Standard());
        for (int i = 0; i < 12; i++) contest.StepDay();

        var hunter = contest.Field.First(c => c.Personality == Personality.AggressiveHunter);
        var builder = contest.Field.First(c => c.Personality == Personality.PatientBuilder);

        Assert.True(builder.Run.Shelter >= hunter.Run.Shelter,
            "the patient builder should be further along on shelter than the aggressive hunter");
    }

    [Fact]
    public void PlayerCanSupplyTheirOwnPlan()
    {
        var contest = new Contest(5, Roster.Standard());
        var plan = new[]
        {
            OffTheGrid.Sim.Time.Activity.Fishing,
            OffTheGrid.Sim.Time.Activity.Fishing,
            OffTheGrid.Sim.Time.Activity.WhittleComfortProject,
            OffTheGrid.Sim.Time.Activity.Rest,
            OffTheGrid.Sim.Time.Activity.Rest
        };

        // Checks that the PLAYER'S plan is what gets applied - not that the
        // player survives. An early crisis tap-out is designed behaviour, so
        // asserting survival made this test flap on whichever seed rolled one.
        int before = contest.Player!.Run.DayNumber;
        for (int i = 0; i < 10 && !contest.IsOver && !contest.Player!.IsOut; i++)
            contest.StepDay(plan);

        Assert.True(contest.Player!.Run.DayNumber > before, "the player's days should advance");
        Assert.Contains(contest.Field, c => !c.IsPlayer && c.Run.DayNumber > before);
    }
}
