using System;
using OffTheGrid.Data.Balance;

namespace OffTheGrid.Sim.Nutrition;

/// <summary>Macronutrient content of a quantity of food.</summary>
public readonly record struct Macros(float ProteinG, float FatG, float CarbohydrateG)
{
    public static Macros operator +(Macros a, Macros b) =>
        new(a.ProteinG + b.ProteinG, a.FatG + b.FatG, a.CarbohydrateG + b.CarbohydrateG);

    public static Macros operator *(Macros m, float scale) =>
        new(m.ProteinG * scale, m.FatG * scale, m.CarbohydrateG * scale);
}

/// <summary>What the body actually got, versus what was eaten.</summary>
public readonly record struct NutritionResult
{
    /// <summary>Calories the body can actually use today.</summary>
    public float UsableKcal { get; init; }

    /// <summary>Calories present in the food but unusable because protein is capped.</summary>
    public float WastedKcal { get; init; }

    /// <summary>Protein eaten, in grams.</summary>
    public float ProteinConsumedG { get; init; }

    /// <summary>The ceiling that applied, in grams.</summary>
    public float ProteinCeilingG { get; init; }

    /// <summary>
    /// True when protein intake hit the ceiling and calories were left on the
    /// table. THIS is the "full cache, still starving" state, and it is the flag
    /// the legibility UI (B1) keys off. If this is true and the player was not
    /// told, the game reads as cheating.
    /// </summary>
    public bool ProteinCeilingBound => ProteinConsumedG >= ProteinCeilingG && WastedKcal > 0f;

    public float ProteinCeilingUtilisation =>
        ProteinCeilingG <= 0f ? 0f : ProteinConsumedG / ProteinCeilingG;
}

/// <summary>
/// The protein ceiling — rabbit starvation — as a pure function. Balance doc 3.3.
///
/// Resolves B8. Rivals run THIS SAME MODEL, not a cheaper approximation. Two
/// reasons:
///
///   1. If rivals ran different physics, a rival could live on lean meat that
///      would kill the player. The game's central mechanical truth would become
///      player-only, which is the "the game is cheating" failure mode (R14-R16)
///      displaced onto the rivals, and it would corrupt check-in intel.
///   2. It is free. ~9 rivals x 60 days = 540 evaluations of a few multiplies.
///      The expensive part of the player sim is minigames and map/FOV, not this.
///
/// Rival fidelity is reduced by feeding this model expected-value inputs (no
/// minigames, attribute-derived yields with variance) — NOT by giving rivals a
/// different metabolism.
/// </summary>
public static class NutritionModel
{
    /// <summary>Grams of protein per day this body can process. Balance doc 3.3.</summary>
    public static float ProteinCeilingG(float bodyweightKg, BalanceData balance) =>
        bodyweightKg * balance.ProteinCeilingGramsPerKg;

    /// <summary>Gross calories in a quantity of food, ignoring whether the body can use them.</summary>
    public static float GrossKcal(Macros macros, BalanceData balance) =>
        macros.ProteinG * balance.KcalPerGramProtein
      + macros.FatG * balance.KcalPerGramFat
      + macros.CarbohydrateG * balance.KcalPerGramCarbohydrate;

    /// <summary>
    /// Calories the body can actually extract, after the protein ceiling binds.
    ///
    /// Fat and carbohydrate are always fully available. Protein above the ceiling
    /// is not merely wasted — in reality it is actively harmful — but for balance
    /// purposes it is modelled as unavailable energy, which produces the
    /// "full cache, still starving" outcome the design wants.
    /// </summary>
    public static NutritionResult Evaluate(Macros eaten, float bodyweightKg, BalanceData balance)
    {
        float ceiling = ProteinCeilingG(bodyweightKg, balance);
        float usableProteinG = MathF.Min(eaten.ProteinG, ceiling);

        float usable = usableProteinG * balance.KcalPerGramProtein
                     + eaten.FatG * balance.KcalPerGramFat
                     + eaten.CarbohydrateG * balance.KcalPerGramCarbohydrate;

        float wasted = (eaten.ProteinG - usableProteinG) * balance.KcalPerGramProtein;

        return new NutritionResult
        {
            UsableKcal = usable,
            WastedKcal = MathF.Max(0f, wasted),
            ProteinConsumedG = eaten.ProteinG,
            ProteinCeilingG = ceiling
        };
    }

    /// <summary>
    /// Maximum calories per day this food can safely deliver if eaten alone.
    /// This is the column in balance doc 3.3 where black bear is the only entry
    /// that clears a full day's burn.
    /// </summary>
    public static float MaxSafeKcalPerDay(Macros foodComposition, float bodyweightKg, BalanceData balance)
    {
        if (foodComposition.ProteinG <= 0f) return float.PositiveInfinity;

        float ceiling = ProteinCeilingG(bodyweightKg, balance);
        float scale = ceiling / foodComposition.ProteinG;
        return GrossKcal(foodComposition * scale, balance);
    }
}
