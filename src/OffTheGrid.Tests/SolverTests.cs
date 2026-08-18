namespace OffTheGrid.Tests;

using System.Linq;
using Xunit;
using Xunit.Abstractions;
using OffTheGrid.Sim.Balance;
using OffTheGrid.Sim.Record;

public sealed class SolverTests(ITestOutputHelper output)
{
    [Fact]
    public void ArchetypeSweepReportsOutcomes()
    {
        var outcomes = Solver.SweepAll(Solver.Archetypes);

        output.WriteLine($"{"scenario",-24} {"days",6}  {"cause",-14}");
        foreach (var o in outcomes)
        {
            output.WriteLine($"{o.Scenario,-24} {o.MeanDaysSurvived,6:F0}  {o.DominantCause,-14}");
        }

        Assert.Equal(Solver.Archetypes.Count, outcomes.Count);
        Assert.All(outcomes, o => Assert.True(o.MeanDaysSurvived > 0));
    }

    [Fact]
    public void EveryArchetypeBeatsTheFastingBuild()
    {
        // Q2 across all presets, not just the one build BalanceAssert checks.
        var outcomes = Solver.SweepAll(Solver.Archetypes)
            .ToDictionary(o => o.Scenario, o => o.MeanDaysSurvived);

        float fasting = outcomes["Fasting build (idle)"];

        foreach (var (name, days) in outcomes.Where(kv => kv.Key != "Fasting build (idle)"))
        {
            Assert.True(days > fasting, $"{name} survived {days:F0}, fasting build survived {fasting:F0}");
        }
    }

    [Fact]
    public void SweepIsDeterministic()
    {
        var a = Solver.Sweep(Solver.Archetypes[0], runs: 3);
        var b = Solver.Sweep(Solver.Archetypes[0], runs: 3);

        Assert.Equal(a.MeanDaysSurvived, b.MeanDaysSurvived);
        Assert.Equal(a.DominantCause, b.DominantCause);
    }

    [Fact]
    public void SeedSweepIsCurrentlyDegenerate()
    {
        // Documents a known limitation rather than hiding it: the day loop does
        // not consume RNG yet, so every seed produces an identical run. This test
        // should START FAILING once hunt resolution or weather lands - at which
        // point delete it, because the seed dimension has become real.
        var outcome = Solver.Sweep(Solver.Archetypes[0], runs: 10);

        Assert.Equal(outcome.MinDays, outcome.MaxDays);
    }

    [Fact]
    public void DominanceCeilingIsMeasurable()
    {
        // Spec 5.5 sets a 60% ceiling on any single end condition. Not yet
        // assertable - with no RNG the distribution is degenerate - but the
        // measurement path exists and reports.
        var outcome = Solver.Sweep(Solver.Archetypes[0], runs: 5);

        output.WriteLine($"dominant cause: {outcome.DominantCause} at {outcome.DominantShare:P0}");
        Assert.InRange(outcome.DominantShare, 0f, 1f);
        Assert.NotEqual(EndCondition.None, outcome.DominantCause);
    }
}
