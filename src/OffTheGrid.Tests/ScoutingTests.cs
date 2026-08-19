namespace OffTheGrid.Tests;

using System.Linq;
using Xunit;
using OffTheGrid.Data;
using OffTheGrid.Data.Gear;
using OffTheGrid.Sim.Food;
using OffTheGrid.Sim.Time;

/// <summary>
/// The drop shows you the character of your immediate area and hides what is
/// over the ridge. That gap is the argument for spending a slot exploring.
/// </summary>
public sealed class ScoutingTests
{
    [Fact]
    public void MostOfTheCountryIsUnseenAtTheDrop()
    {
        var t = new Territory(new Rng(1));
        Assert.Equal(Territory.InitialExploredFraction, t.ExploredFraction, 0.001f);
        Assert.True(t.ExploredFraction <= 0.15f, "the drop should show a small fraction of the map");
    }

    [Fact]
    public void TheDropTellsYouWhatKindOfCountryThisIs()
    {
        // A contestant can tell in their first day whether there is water worth
        // fishing or sign worth following. They cannot tell what is two valleys on.
        var t = new Territory(new Rng(4));
        foreach (var route in Territory.Routes)
            Assert.False(string.IsNullOrWhiteSpace(t.CharacterOf(route)));
    }

    [Fact]
    public void EachRouteIsRolledIndependently()
    {
        // A poor drop for one thing is often a good drop for another - which is
        // what makes reading your ground a skill rather than a dice result.
        var anyDiffer = false;
        for (ulong seed = 0; seed < 20 && !anyDiffer; seed++)
        {
            var t = new Territory(new Rng(seed));
            var q = Territory.Routes.Select(t.For).ToArray();
            if (q.Max() - q.Min() > 0.25f) anyDiffer = true;
        }
        Assert.True(anyDiffer, "drops should vary meaningfully between routes");
    }

    [Fact]
    public void ThereIsAlwaysBetterGroundToFind()
    {
        for (ulong seed = 0; seed < 25; seed++)
        {
            var t = new Territory(new Rng(seed));
            foreach (var route in Territory.Routes)
                Assert.True(t.PotentialFor(route) >= t.For(route),
                    "what is out there must be at least as good as what you started on");
        }
    }

    [Fact]
    public void ScoutingOpensCountryAndImprovesGround()
    {
        var t = new Territory(new Rng(9));
        float exploredBefore = t.ExploredFraction;
        float qualityBefore = Territory.Routes.Sum(t.For);

        for (int i = 0; i < 6; i++) t.Prospect(1.0f);

        Assert.True(t.ExploredFraction > exploredBefore);
        Assert.True(Territory.Routes.Sum(t.For) > qualityBefore);
    }

    [Fact]
    public void ScoutingCannotExceedWhatTheCountryHolds()
    {
        // A route already at its potential will not improve however far you walk.
        // That is information too, and it is the reason relocation exists.
        var t = new Territory(new Rng(11));
        for (int i = 0; i < 100; i++) t.Prospect(1.5f);

        foreach (var route in Territory.Routes)
            Assert.True(t.For(route) <= t.PotentialFor(route) + 0.001f);
    }

    // ---- gear durability ----

    [Fact]
    public void LoadBearingGearFailsWithinARunAndCampGearDoesNot()
    {
        // Things that take load should fail; things that only get carried should not.
        Assert.True(GearDurability.Get(GearItem.BowAndArrows).Uses < 500);
        Assert.True(GearDurability.Get(GearItem.Gillnet).Uses < 300);
        Assert.True(GearDurability.Get(GearItem.Pot).Uses > 2000);
        Assert.True(GearDurability.Get(GearItem.FerroRod).Uses > 2000);
    }

    [Fact]
    public void EveryDurabilityEntryWarnsBeforeItFails()
    {
        // Doc 17: no crisis without a signal. A gear failure the player was never
        // warned about is the game cheating.
        foreach (var e in GearDurability.All)
            Assert.InRange(e.WarnAtFraction, 0.05f, 0.5f);
    }

    [Fact]
    public void TheBowMatchesTheBalanceDoc()
    {
        // Balance doc 7: bow as a gear item, 400 shots.
        Assert.Equal(400, GearDurability.Get(GearItem.BowAndArrows).Uses);
    }

    [Fact]
    public void WearIsAttributedToTheWorkThatCausesIt()
    {
        Assert.Contains(GearItem.BowAndArrows, GearDurability.WornBy("HuntingStalk"));
        Assert.Contains(GearItem.SnareWire, GearDurability.WornBy("TrapLine"));
        Assert.Contains(GearItem.Axe, GearDurability.WornBy("ChoppingWood"));
        Assert.Empty(GearDurability.WornBy("Rest"));
    }

    // ---- palette ----

    [Fact]
    public void PaletteReachesColdFrontWhenWinterArrives()
    {
        // Not on an absolute day count. Under a short summer the palette must
        // show winter when it is winter, not early autumn while snow falls.
        Assert.Equal(1f, Calendar.SeasonalPaletteT(SeasonSchedule.ShortSummer.WinterArrives,
            SeasonSchedule.ShortSummer), 0.05f);
        Assert.Equal(1f, Calendar.SeasonalPaletteT(SeasonSchedule.Standard.WinterArrives,
            SeasonSchedule.Standard), 0.05f);

        // Same day, different scenario, different point in the visual arc.
        Assert.True(Calendar.SeasonalPaletteT(20, SeasonSchedule.ShortSummer)
                  > Calendar.SeasonalPaletteT(20, SeasonSchedule.LongFall));
    }
}
