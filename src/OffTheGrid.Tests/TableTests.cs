namespace OffTheGrid.Tests;

using System;
using System.Linq;
using Xunit;
using OffTheGrid.Data.Balance;
using OffTheGrid.Data.Tables;
using OffTheGrid.Sim.Nutrition;

/// <summary>
/// These check the tables against the CLAIMS the balance doc derives from them,
/// not merely that the numbers were copied. A transcription test that only
/// restates the source catches nothing.
/// </summary>
public sealed class TableTests
{
    private static readonly BalanceData Balance = BalanceData.Default;

    [Fact]
    public void BearIsTheOnlyAnimalSustainableAlone()
    {
        // Balance doc 3.3's central table. Every food's max safe kcal/day for an
        // 85 kg player, against a ~2,850 kcal burn.
        var sustainable = FoodTable.All
            .Where(f => NutritionModel.MaxSafeKcalPerDay(
                new Macros(f.ProteinG, f.FatG, 0f), 85f, Balance) >= 2500f)
            .Select(f => f.Source)
            .ToArray();

        Assert.Equal(new[] { FoodSource.BlackBear }, sustainable);
    }

    [Fact]
    public void MooseAndElkAreTheWorstFoodsDespiteBeingTheBiggest()
    {
        // The B7 finding, straight out of the table: the largest animal in the
        // woods is also one of the leanest, and you cannot live on it.
        float elk = NutritionModel.MaxSafeKcalPerDay(
            Macros(FoodSource.RooseveltElk), 85f, Balance);
        float hare = NutritionModel.MaxSafeKcalPerDay(
            Macros(FoodSource.SnowshoeHare), 85f, Balance);

        Assert.True(elk < hare,
            $"elk ({elk:F0}) should sustain less per day than a hare ({hare:F0}) despite being 200x the mass");
    }

    [Fact]
    public void ElkHasVastlyMoreGrossCaloriesThanItCanEverDeliver()
    {
        // The trap-disguised-as-jackpot property.
        var elk = FoodTable.Get(FoodSource.RooseveltElk);
        float maxSafeDaily = NutritionModel.MaxSafeKcalPerDay(Macros(FoodSource.RooseveltElk), 85f, Balance);

        Assert.True(elk.Kcal > 170_000f);
        Assert.True(maxSafeDaily < 1_500f);
    }

    [Fact]
    public void OnlyBearClearsFiftyPercentFatByEnoughToMatter()
    {
        var bear = FoodTable.Get(FoodSource.BlackBear);
        Assert.InRange(bear.FatCalorieFraction, 0.60f, 0.70f);

        // Chinook is genuinely fatty but its fat is chained to its protein, which
        // is why it still cannot sustain a player alone (B3).
        var chinook = FoodTable.Get(FoodSource.ChinookSalmon);
        Assert.InRange(chinook.FatCalorieFraction, 0.45f, 0.55f);
        Assert.True(NutritionModel.MaxSafeKcalPerDay(Macros(FoodSource.ChinookSalmon), 85f, Balance) < 2000f);
    }

    [Fact]
    public void PreservationCompressesTheYieldRatio()
    {
        // Balance doc 3.2: raw elk:trout is unusable; after preservation caps it
        // lands near the 7-10x target. This is why preservation is the top lever.
        var elk = FoodTable.Get(FoodSource.RooseveltElk);
        var trout = FoodTable.Get(FoodSource.CutthroatTrout);

        float rawRatio = elk.Kcal / trout.Kcal;
        Assert.True(rawRatio > 250f, $"raw ratio should be enormous, got {rawRatio:F0}");

        // A smoke rack holds ~25 kg; the rest rots.
        const float rackCapacityKg = 25f;
        float elkBanked = PreservationTable.YieldAfterLoss(
            PreservationMethod.SmokeRack, MathF.Min(elk.EdibleKg, rackCapacityKg));
        float troutBanked = MathF.Min(trout.EdibleKg, rackCapacityKg);

        float bankedRatio = (elkBanked / elk.EdibleKg * elk.Kcal) / trout.Kcal;
        Assert.InRange(bankedRatio, 20f, 70f);
        Assert.True(bankedRatio < rawRatio / 4f, "preservation must compress the ratio substantially");
    }

    [Fact]
    public void ProcessingAnElkCostsDaysOfSlots()
    {
        // Doc 4: ~15 slots of smoking for a 121 kg elk - three to five full days
        // of doing nothing else, during which the rest is rotting.
        float slots = PreservationTable.SlotsToProcess(PreservationMethod.SmokeRack, 121.5f);
        Assert.InRange(slots, 14f, 16f);
    }

    [Fact]
    public void CachePitIsFastestButSpoilsSoonest()
    {
        var cache = PreservationTable.Get(PreservationMethod.CachePit);
        var drying = PreservationTable.Get(PreservationMethod.DryingRack);

        Assert.True(cache.KgPerSlot > drying.KgPerSlot);
        Assert.True(cache.ShelfLifeDays < drying.ShelfLifeDays);
        Assert.True(cache.LossFraction < drying.LossFraction);
    }

    // ---- shelter ----

    [Fact]
    public void ShelterBuildCurveMatchesDoc()
    {
        // Doc 5.1: at ~0.5 slots/day the log shelter is a 32-day project and the
        // cabin 56 - the cabin is a flex, not a survival strategy.
        Assert.Equal(32f, ShelterTable.Get(ShelterTier.LogShelter).Slots / 0.5f, 0.1f);
        Assert.Equal(56f, ShelterTable.Get(ShelterTier.LogCabin).Slots / 0.5f, 0.1f);
    }

    [Fact]
    public void BagPlusAFrameHoldsToAboutFreezing()
    {
        // Doc 5.2: "bag + A-frame = 4.6 clo, which holds to about 0 C."
        float available = ShelterTable.BaseClothingClo
                        + ShelterTable.SleepingBagClo
                        + ShelterTable.Get(ShelterTier.AFrame).Clo;

        Assert.Equal(4.6f, available, 0.01f);
        Assert.True(available >= ShelterTable.CloDemandForNightTemp(0f) - 0.01f);
        Assert.True(available < ShelterTable.CloDemandForNightTemp(-5f));
    }

    [Fact]
    public void RelocationTriggerBFiresWhenAFrameMeetsNovember()
    {
        // Doc 12 s2.2: trigger at clo_gap > 0.8. An A-frame at -5 C is exactly the
        // case that should fire it.
        float available = ShelterTable.BaseClothingClo
                        + ShelterTable.SleepingBagClo
                        + ShelterTable.Get(ShelterTier.AFrame).Clo;

        float gap = ShelterTable.CloDemandForNightTemp(-5f) - available;
        Assert.True(gap > Balance.CloGapThreshold, $"gap was {gap:F2}, threshold {Balance.CloGapThreshold}");
    }

    [Fact]
    public void LogShelterClosesTheGapThatAFrameCannot()
    {
        float logShelter = ShelterTable.BaseClothingClo
                         + ShelterTable.SleepingBagClo
                         + ShelterTable.Get(ShelterTier.LogShelter).Clo;

        Assert.True(logShelter >= ShelterTable.CloDemandForNightTemp(-5f));
    }

    [Fact]
    public void ReflectorWallIsASmallCloGainForItsCost()
    {
        // Doc 5: only +0.6 clo over the A-frame for 3 extra slots - it pays for
        // itself through fire efficiency, not insulation. A player reading only
        // the clo column would wrongly skip it.
        float delta = ShelterTable.Get(ShelterTier.ReflectorWallCamp).Clo
                    - ShelterTable.Get(ShelterTier.AFrame).Clo;
        Assert.Equal(0.6f, delta, 0.01f);
    }

    [Fact]
    public void ShelterSlotsDriveRelocationMoraleHit()
    {
        // Doc 12 s4: hit = min(1.0 * slots, 10).
        foreach (var shelter in ShelterTable.All)
        {
            float hit = MathF.Min(
                Balance.ShelterLossMoralePerSlot * shelter.Slots,
                Balance.ShelterLossMoraleCap);

            Assert.True(hit <= Balance.MoraleProjectCompleted,
                $"{shelter.Tier} loss ({hit}) must not exceed the rebuild reward");
        }
    }

    private static Macros Macros(FoodSource source)
    {
        var f = FoodTable.Get(source);
        return new Macros(f.ProteinG, f.FatG, 0f);
    }
}
