namespace OffTheGrid.Tests;

using System.Linq;
using Xunit;
using OffTheGrid.Data.Balance;
using OffTheGrid.Data.Tables;
using OffTheGrid.Sim.Food;

/// <summary>
/// The raw/preserved split. A kill is no longer automatically banked or
/// automatically lost - it arrives raw, on a clock, and what happens next is a
/// decision.
/// </summary>
public sealed class PreservationTests
{
    private static readonly BalanceData Balance = BalanceData.Default;

    private static Larder WithDeer(params CampStructure[] structures)
    {
        var larder = new Larder { CapacityKg = 200f };
        foreach (var s in structures) larder.AddStructure(s);

        var deer = FoodTable.Get(FoodSource.BlacktailDeer);
        larder.Add(deer.ProteinG, deer.FatG, deer.CarbohydrateG, deer.EdibleKg);
        return larder;
    }

    [Fact]
    public void AKillArrivesRawNotBanked()
    {
        var larder = WithDeer();
        Assert.True(larder.RawKg > 20f, "a deer should land as raw mass");
        Assert.Equal(0f, larder.PreservedKg, 0.01f);
    }

    [Fact]
    public void RawFoodSpoilsFastAndPreservedDoesNot()
    {
        var raw = WithDeer();
        var preserved = WithDeer(CampStructure.DryingRack);
        preserved.Preserve(slots: 10f);

        for (int day = 0; day < 10; day++)
        {
            raw.ApplyDailySpoilage(8f);
            preserved.ApplyDailySpoilage(8f);
        }

        Assert.True(raw.StoredKg < 2f, $"unworked meat should be gone in ten days, {raw.StoredKg:F1} kg left");
        Assert.True(preserved.StoredKg > 8f, $"preserved meat should keep, only {preserved.StoredKg:F1} kg left");
    }

    [Fact]
    public void PreservingRequiresSomethingToPreserveWith()
    {
        // No rack, no processing - and that is the cost of skipping the build.
        var noRack = WithDeer(CampStructure.LightCache);
        Assert.Equal(0f, noRack.Preserve(slots: 5f), 0.01f);

        var withRack = WithDeer(CampStructure.SmokeRack);
        Assert.True(withRack.Preserve(slots: 5f) > 10f);
    }

    [Fact]
    public void ProcessingTakesRealTime()
    {
        // Doc 4's elk example: a big kill is an obligation, not a windfall. One
        // slot does not bank a deer.
        var larder = WithDeer(CampStructure.SmokeRack);
        float before = larder.RawKg;
        larder.Preserve(slots: 1f);

        Assert.True(larder.RawKg > 0f, "one slot must not clear a whole deer");
        Assert.True(larder.RawKg < before);
    }

    [Fact]
    public void ProcessingTakesItsLossOnTheWayIn()
    {
        var drying = WithDeer(CampStructure.DryingRack);   // 25% loss, fast
        var smoking = WithDeer(CampStructure.SmokeRack);   // 15% loss, slow

        drying.Preserve(slots: 20f);
        smoking.Preserve(slots: 20f);

        Assert.True(smoking.PreservedKg > drying.PreservedKg,
            "the smoke rack wastes less than the drying rack");
    }

    [Fact]
    public void ColdCacheIsWorthlessWarmAndUnbeatableFrozen()
    {
        var warm = WithDeer(CampStructure.ColdCache);
        var frozen = WithDeer(CampStructure.ColdCache);

        for (int day = 0; day < 20; day++)
        {
            warm.ApplyDailySpoilage(9f);
            frozen.ApplyDailySpoilage(-6f);
        }

        Assert.True(warm.StoredKg < 1f, "a cold cache does nothing in mild weather");
        Assert.True(frozen.StoredKg > 20f, "below freezing, food simply stops ageing");
    }

    [Fact]
    public void ColdCacheAddsNoCapacityUntilItIsCold()
    {
        var larder = new Larder();
        larder.AddStructure(CampStructure.ColdCache);

        Assert.True(larder.CapacityFromStructures(nightTempC: 10f)
                  < larder.CapacityFromStructures(nightTempC: -5f));
    }

    [Fact]
    public void FreshMeatIsEatenBeforePreservedStores()
    {
        var larder = WithDeer(CampStructure.SmokeRack);
        larder.Preserve(slots: 1f);

        float preservedBefore = larder.PreservedKg;
        larder.Eat(appetiteKcal: 1500f, bodyweightKg: 85f, Balance);

        Assert.Equal(preservedBefore, larder.PreservedKg, 0.5f);
    }

    [Fact]
    public void StructuresOfferPredatorProtection()
    {
        var exposed = new Larder();
        var cached = new Larder();
        cached.AddStructure(CampStructure.CachePit);

        Assert.True(cached.PredatorProtection > exposed.PredatorProtection);
    }

    [Fact]
    public void EveryStructureIsWorthSomething()
    {
        foreach (var e in CampStructures.All)
        {
            bool useful = e.CapacityKg > 0f || e.ProcessKgPerSlot > 0f || e.PredatorProtection > 0f;
            Assert.True(useful, $"{e.Structure} does nothing");
            Assert.True(e.BuildSlots > 0, $"{e.Structure} is free to build");
        }
    }
}
