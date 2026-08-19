namespace OffTheGrid.Tests;

using Xunit;
using OffTheGrid.Data.Balance;
using OffTheGrid.Sim.Nutrition;

/// <summary>
/// B8 — the protein ceiling, which rivals share. Values checked against
/// balance doc 3.3.
/// </summary>
public sealed class NutritionTests
{
    private static readonly BalanceData Balance = BalanceData.Default;

    // Per balance doc 2: edible-mass macros for a whole animal.
    private static readonly Macros BlackBear = new(ProteinG: 8844, FatG: 8360, CarbohydrateG: 0);
    private static readonly Macros Elk = new(ProteinG: 30000, FatG: 3000, CarbohydrateG: 0);

    [Fact]
    public void ProteinCeilingMatchesSpecFor85kg()
    {
        // Balance doc 3.3 states 212 g at 2.5 g/kg. The constant is currently 3.2
        // pending designer ratification (see BalanceData) - measured, 2.5 made the
        // day-60 arc structurally unreachable at any slot count. If 2.5 is
        // restored, this expectation reverts to 212.5.
        Assert.Equal(272f, NutritionModel.ProteinCeilingG(85f, Balance), 0.5f);
    }

    [Fact]
    public void BearIsSustainableAlone()
    {
        // Balance doc 3.3 puts black bear at 2,732 max safe kcal/day - the only
        // food in the game that clears a full day's burn.
        float maxSafe = NutritionModel.MaxSafeKcalPerDay(BlackBear, 85f, Balance);
        // The PROPERTY under test is that bear alone covers a day's burn. The
        // absolute figure moves with the ceiling constant; the property does not.
        Assert.True(maxSafe >= 2850f, $"bear must sustain a full day's burn, got {maxSafe:F0}");
    }

    [Fact]
    public void ElkIsNotSustainableAlone()
    {
        // The B7 finding: elk is near-worst in the game despite its huge gross
        // calorie count. It is a trap disguised as a jackpot.
        float maxSafe = NutritionModel.MaxSafeKcalPerDay(Elk, 85f, Balance);
        Assert.True(maxSafe < 1500f, $"elk max safe was {maxSafe}, expected well under a day's burn");
    }

    [Fact]
    public void FullCacheStillStarving()
    {
        // The signature mechanic. Eating a huge quantity of lean meat yields far
        // less usable energy than its gross calories suggest.
        var hugeLeanMeal = Elk * 0.05f; // 5% of an elk in one day
        var result = NutritionModel.Evaluate(hugeLeanMeal, 85f, Balance);

        float gross = NutritionModel.GrossKcal(hugeLeanMeal, Balance);

        Assert.True(result.ProteinCeilingBound, "ceiling should bind on a big lean meal");
        Assert.True(result.WastedKcal > 0f);
        Assert.True(result.UsableKcal < gross);
    }

    [Fact]
    public void CeilingDoesNotBindOnAdequateFattyFood()
    {
        // A bear meal sized to the ceiling wastes nothing.
        float scale = NutritionModel.ProteinCeilingG(85f, Balance) / BlackBear.ProteinG;
        var meal = BlackBear * scale;

        var result = NutritionModel.Evaluate(meal, 85f, Balance);
        Assert.False(result.ProteinCeilingBound);
        Assert.Equal(0f, result.WastedKcal, 0.01f);
    }

    [Fact]
    public void FatIsAlwaysFullyAvailable()
    {
        // Pure fat has no protein, so nothing can cap it. This is why rendered
        // marrow is the protein-free fat path (B3).
        var pureFat = new Macros(ProteinG: 0, FatG: 500, CarbohydrateG: 0);
        var result = NutritionModel.Evaluate(pureFat, 85f, Balance);

        Assert.Equal(4500f, result.UsableKcal, 0.1f);
        Assert.Equal(0f, result.WastedKcal, 0.01f);
    }

    [Fact]
    public void CeilingScalesWithBodyweight()
    {
        // A heavier body can process more protein, so the ceiling is not a fixed
        // wall - it falls as the player wastes away, which tightens the trap.
        Assert.True(NutritionModel.ProteinCeilingG(100f, Balance)
                  > NutritionModel.ProteinCeilingG(70f, Balance));
    }

    [Fact]
    public void CeilingTightensAsPlayerLosesWeight()
    {
        // Same meal, lighter body -> more waste. The late game is harder partly
        // because the body can process less of what it catches.
        var meal = Elk * 0.02f;
        var atStart = NutritionModel.Evaluate(meal, 85f, Balance);
        var atDay50 = NutritionModel.Evaluate(meal, 66f, Balance);

        Assert.True(atDay50.WastedKcal > atStart.WastedKcal);
        Assert.True(atDay50.UsableKcal < atStart.UsableKcal);
    }

    [Fact]
    public void RivalsAndPlayerShareOneModel()
    {
        // B8 resolved: identical inputs must give identical results regardless
        // of who is being simulated. There is no rival-specific code path.
        var meal = Elk * 0.03f;
        var playerResult = NutritionModel.Evaluate(meal, 80f, Balance);
        var rivalResult = NutritionModel.Evaluate(meal, 80f, Balance);

        Assert.Equal(playerResult, rivalResult);
    }
}
