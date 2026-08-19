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
        // The MVP biome SHOULD be lopsided - the salmon run is what makes it that
        // place. This guards against accidentally flattening it while tuning.
        double fishing = ExpectedKcalPerSlot(Biome.VancouverIsland, Season.SalmonRun, Activity.Fishing);
        double trapping = ExpectedKcalPerSlot(Biome.VancouverIsland, Season.SalmonRun, Activity.TrapLine);
        Assert.True(fishing > trapping * 3.0, "the salmon run must remain a genuine abundance window");

        // ...and it must collapse afterwards, or it is not a "run".
        double winterFishing = ExpectedKcalPerSlot(Biome.VancouverIsland, Season.Winter, Activity.Fishing);
        Assert.True(winterFishing < fishing / 10.0);
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
