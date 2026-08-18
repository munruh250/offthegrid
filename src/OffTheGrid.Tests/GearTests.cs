namespace OffTheGrid.Tests;

using System.Collections.Generic;
using Xunit;
using OffTheGrid.Data;
using OffTheGrid.Data.Gear;
using OffTheGrid.Data.Tables;
using OffTheGrid.Sim;
using OffTheGrid.Sim.Food;
using OffTheGrid.Sim.Record;
using OffTheGrid.Sim.Time;

/// <summary>Gear gating (R-A8) and the attribute effects that had no implementation.</summary>
public sealed class GearTests
{
    private static Dictionary<AttributeKind, int> Attrs(int fitness = 5, int cold = 5, int resolve = 6) => new()
    {
        [AttributeKind.Bushcraft] = 5,
        [AttributeKind.Hunting] = 6,
        [AttributeKind.Foraging] = 5,
        [AttributeKind.Fitness] = fitness,
        [AttributeKind.Resolve] = resolve,
        [AttributeKind.ColdAdaptation] = cold
    };

    private static readonly Loadout NoBow = new(
        GearItem.Axe, GearItem.Saw, GearItem.Knife, GearItem.SnareWire,
        GearItem.FishingLineAndHooks, GearItem.Pot, GearItem.SleepingBag,
        GearItem.Tarp, GearItem.Paracord, GearItem.FerroRod);

    [Fact]
    public void LoadoutIsCappedAtTenItems()
    {
        Assert.Equal(10, Loadout.Standard.Count);
        Assert.Throws<System.ArgumentException>(() => new Loadout(
            GearItem.Axe, GearItem.Saw, GearItem.Knife, GearItem.BowAndArrows,
            GearItem.SnareWire, GearItem.Gillnet, GearItem.FishingLineAndHooks,
            GearItem.Pot, GearItem.SleepingBag, GearItem.Tarp, GearItem.Paracord));
    }

    [Fact]
    public void NoBowMeansBigGameIsSeenButNeverTaken()
    {
        // The gate is hard, and the encounter still happens - you watch it leave.
        var rng = new Rng(4);
        int encounteredBig = 0, tookBig = 0;

        for (int i = 0; i < 4000; i++)
        {
            var r = Harvest.Resolve(Activity.HuntingStalk, Season.SalmonRun, 6, NoBow, rng);
            bool isBig = r.EncounteredSource is FoodSource.BlacktailDeer or FoodSource.BlackBear or FoodSource.RooseveltElk;
            if (isBig)
            {
                encounteredBig++;
                if (r.CaughtSomething) tookBig++;
            }
        }

        Assert.True(encounteredBig > 0, "big game should still be encountered without a bow");
        Assert.Equal(0, tookBig);
    }

    [Fact]
    public void NoTackleMeansNoFishingAtAll()
    {
        var noTackle = new Loadout(GearItem.Axe, GearItem.Knife, GearItem.SnareWire, GearItem.Pot);
        var rng = new Rng(5);

        for (int i = 0; i < 500; i++)
        {
            var r = Harvest.Resolve(Activity.Fishing, Season.SalmonRun, 6, noTackle, rng);
            Assert.False(r.Encountered);
        }
    }

    [Fact]
    public void GillnetOutfishesLine()
    {
        var net = new Loadout(GearItem.Gillnet, GearItem.Knife);
        var line = new Loadout(GearItem.FishingLineAndHooks, GearItem.Knife);

        static int Catches(Loadout gear, ulong seed)
        {
            var rng = new Rng(seed);
            int n = 0;
            for (int i = 0; i < 3000; i++)
                if (Harvest.Resolve(Activity.Fishing, Season.Lean, 6, gear, rng).CaughtSomething) n++;
            return n;
        }

        Assert.True(Catches(net, 11) > Catches(line, 11) * 1.3f);
    }

    [Fact]
    public void TrappingNeedsWireOrCordage()
    {
        var bare = new Loadout(GearItem.Axe, GearItem.Knife);
        var rng = new Rng(6);
        for (int i = 0; i < 300; i++)
            Assert.False(Harvest.Resolve(Activity.TrapLine, Season.Lean, 6, bare, rng).Encountered);
    }

    [Fact]
    public void ShelterTierIsGatedByCuttingTools()
    {
        Assert.Equal(ShelterTier.LogCabin, GearEffects.MaxShelterTier(Loadout.Standard));
        Assert.Equal(ShelterTier.ReflectorWallCamp,
            GearEffects.MaxShelterTier(new Loadout(GearItem.Axe, GearItem.Tarp)));
        Assert.Equal(ShelterTier.DebrisHut,
            GearEffects.MaxShelterTier(new Loadout(GearItem.Knife, GearItem.Tarp)));
    }

    [Fact]
    public void SleepingBagIsWorthRealInsulation()
    {
        var withBag = new Loadout(GearItem.SleepingBag);
        var without = new Loadout(GearItem.Knife);
        Assert.Equal(1.5f, withBag.ClothingClo - without.ClothingClo, 0.01f);
    }

    // ---- attributes that previously had NO implementation ----

    [Fact]
    public void ColdAdaptationLowersCloDemand()
    {
        // Spec 4.1 gives Cold Adaptation a "thermoneutral offset". Before this it
        // was read nowhere in the sim - the one attribute with zero effect.
        var hardy = new Run(1, Sex.Male, 180, 35, 85, 20, Attrs(cold: 10), Loadout.Standard);
        var soft = new Run(1, Sex.Male, 180, 35, 85, 20, Attrs(cold: 1), Loadout.Standard);

        Assert.True(hardy.CloDemandTonight(50) < soft.CloDemandTonight(50));
    }

    [Fact]
    public void ProspectingImprovesTerritoryAndScalesWithFitness()
    {
        static float AfterExploring(int fitness)
        {
            var run = new Run(1, Sex.Male, 180, 35, 85, 20, Attrs(fitness), Loadout.Standard);
            var plan = new DayPlan { Slots = [Activity.Exploring, Activity.Exploring, Activity.Rest, Activity.Rest, Activity.Rest] };
            for (int i = 0; i < 5 && !run.IsOver; i++) run.StepDay(plan);
            return run.TerritoryQuality;
        }

        Assert.True(AfterExploring(9) > AfterExploring(3),
            "Fitness must make ranging out meaningfully more productive");
    }

    [Fact]
    public void DropPointQualityVariesBySeed()
    {
        // Spec 8.2: your drop point may genuinely be poor.
        var a = new Run(1, Sex.Male, 180, 35, 85, 20, Attrs(), Loadout.Standard);
        var b = new Run(99, Sex.Male, 180, 35, 85, 20, Attrs(), Loadout.Standard);
        Assert.NotEqual(a.TerritoryQuality, b.TerritoryQuality);
    }

    [Fact]
    public void ResolveIncreasesComfortProjectPayout()
    {
        // Spec 4.1: Resolve governs "morale gained per comfort project".
        // Measured with headroom: a high-Resolve contestant starts at the morale
        // ceiling, so gains would be silently clipped without knocking them down
        // first.
        static float ProjectPayout(int resolve)
        {
            var run = new Run(1, Sex.Male, 180, 35, 85, 20, Attrs(resolve: resolve), Loadout.Standard);
            run.Morale.BeginDay();
            run.Morale.ApplyEvent(OffTheGrid.Sim.Morale.MoraleSource.MemoryEvent, -50f, OffTheGrid.Data.Balance.BalanceData.Default);

            float before = run.Morale.Current;
            var plan = new DayPlan
            {
                Slots = [Activity.WhittleComfortProject, Activity.WhittleComfortProject, Activity.WhittleComfortProject, Activity.Rest, Activity.Rest],
                DirectRation = new OffTheGrid.Sim.Nutrition.Macros(200f, 400f, 0f)
            };
            run.StepDay(plan);
            return run.Morale.Current - before;
        }

        Assert.True(ProjectPayout(10) > ProjectPayout(2),
            "Resolve must increase what a comfort project is worth");
    }

    [Fact]
    public void BetterGroundMeansFatterAnimals()
    {
        // Territory quality applies to CONDITION, not encounter frequency. A
        // ceiling-limited player gains nothing from more food and everything from
        // a better fat-to-protein ratio.
        static float FatRatio(float territory)
        {
            var rng = new Rng(3);
            float protein = 0, fat = 0;
            for (int i = 0; i < 3000; i++)
            {
                var r = Harvest.Resolve(Activity.Fishing, Season.SalmonRun, 6, Loadout.Standard, rng, territory);
                protein += r.ProteinG;
                fat += r.FatG;
            }
            return fat / protein;
        }

        Assert.True(FatRatio(1.8f) > FatRatio(1.0f) * 1.4f);
    }
}
