namespace OffTheGrid.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using OffTheGrid.Data;
using OffTheGrid.Data.Gear;
using OffTheGrid.Data.Tables;
using OffTheGrid.Sim.Food;
using OffTheGrid.Sim.Time;

/// <summary>
/// Guards the control biome's defining property. If ProvingGround stops being
/// even, every conclusion drawn from it becomes invalid, so this is checked
/// rather than assumed.
/// </summary>
public sealed class BiomeTests
{
    private static double ExpectedKcalPerSlot(Biome biome, Season season, Activity activity)
    {
        var rng = new Rng(31);
        double total = 0;
        const int trials = 9000;
        for (int i = 0; i < trials; i++)
        {
            var r = Harvest.Resolve(activity, season, 5, Loadout.Standard, rng, 1f, biome);
            total += r.ProteinG * 4 + r.FatG * 9 + r.CarbohydrateG * 4;
        }
        return total / trials;
    }

    private static readonly Activity[] Routes =
        [Activity.Fishing, Activity.TrapLine, Activity.HuntingStalk, Activity.Foraging];

    [Fact]
    public void ControlBiomeGivesEveryRouteAComparableReturn()
    {
        foreach (var season in Enum.GetValues<Season>())
        {
            var yields = Routes.Select(a => ExpectedKcalPerSlot(Biome.ProvingGround, season, a)).ToArray();
            double spread = yields.Max() / yields.Min();
            Assert.True(spread < 2.0,
                $"{season}: control biome spread was {spread:F1}x — it is no longer a control");
        }
    }

    [Fact]
    public void ControlBiomeIsMarkedlyEvenerThanVancouverIsland()
    {
        static double WorstSpread(Biome b) =>
            Enum.GetValues<Season>()
                .Select(s => Routes.Select(a => ExpectedKcalPerSlot(b, s, a)).ToArray())
                .Max(y => y.Max() / y.Min());

        Assert.True(WorstSpread(Biome.ProvingGround) < WorstSpread(Biome.VancouverIsland) / 3.0);
    }

    [Fact]
    public void VancouverIslandKeepsItsFishingCharacter()
    {
        // Vancouver Island's character is the SHAPE of its fishing season, not
        // fishing out-earning every other route. The earlier version of this test
        // asserted the latter, and it was guarding an imbalance rather than an
        // identity: fishing returned 5.5x a slot in the salmon run against
        // trapping's 0.5x, which made every other route pointless.
        double runFishing = ExpectedKcalPerSlot(Biome.VancouverIsland, Season.SalmonRun, Activity.Fishing);
        double winterFishing = ExpectedKcalPerSlot(Biome.VancouverIsland, Season.Winter, Activity.Fishing);

        Assert.True(winterFishing < runFishing / 6.0,
            "the salmon RUN must collapse afterwards, or it is not a run");

        // And the routes that replace it must actually be there in winter.
        double winterTrapping = ExpectedKcalPerSlot(Biome.VancouverIsland, Season.Winter, Activity.TrapLine);
        Assert.True(winterTrapping > winterFishing * 2.0,
            "something must take over when the water goes dead");
    }

    [Fact]
    public void EveryFoodSourceAppearsInTheControlBiome()
    {
        var present = new HashSet<FoodSource>();
        foreach (var season in Enum.GetValues<Season>())
        foreach (var activity in Routes)
        foreach (var e in Biome.ProvingGround.EncountersFor(season, activity))
            present.Add(e.Source);

        var missing = Enum.GetValues<FoodSource>().Where(f => !present.Contains(f)).ToArray();
        Assert.True(missing.Length == 0, $"control biome omits: {string.Join(", ", missing)}");
    }

    [Fact]
    public void BothBiomesRunTheSameTemperatureCurve()
    {
        // The cold economy must be exercised identically, or the control is not
        // isolating the food variable alone.
        for (int day = 1; day <= 90; day += 10)
        {
            Assert.Equal(
                Biome.VancouverIsland.NightTemperature(day, SeasonSchedule.Standard),
                Biome.ProvingGround.NightTemperature(day, SeasonSchedule.Standard), 0.001f);
        }
    }
}
