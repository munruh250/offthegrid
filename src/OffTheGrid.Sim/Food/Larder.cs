using System;
using OffTheGrid.Data.Balance;
using OffTheGrid.Data.Tables;
using OffTheGrid.Sim.Nutrition;

namespace OffTheGrid.Sim.Food;

/// <summary>
/// Stored food, as mutable internal state (A15).
///
/// Food is held as macros rather than as a list of carcasses, because that is
/// what the body actually consumes and it keeps spoilage a single scalar decay
/// rather than per-item bookkeeping. Preservation method sets the decay rate.
///
/// Balance doc 8 names preservation the TOP tuning lever: never tune raw animal
/// yields, cap what can be kept instead. This class is where that cap lives.
/// </summary>
public sealed class Larder
{
    private float proteinG;
    private float fatG;

    /// <summary>
    /// Drying rack by default: 30-day shelf life against the cache pit's 12.
    /// Doc 4.3 tells the player to bank during the salmon run, and banking is
    /// impossible on a 12-day shelf life - a cache pit loses everything before
    /// the lean season arrives. The 25% processing loss is the price of that.
    /// </summary>
    public PreservationMethod Method { get; set; } = PreservationMethod.DryingRack;

    /// <summary>Capacity in kg of edible mass. Beyond this, a harvest is simply lost.</summary>
    public float CapacityKg { get; set; } = 25f;

    public float ProteinG => proteinG;
    public float FatG => fatG;

    /// <summary>Rough edible mass, from macro density. Used against capacity.</summary>
    public float StoredKg => (proteinG + fatG) / 1000f * 4f;

    public float GrossKcal(BalanceData balance) =>
        proteinG * balance.KcalPerGramProtein + fatG * balance.KcalPerGramFat;

    /// <summary>
    /// Days of food held: stored protein divided by the daily protein ceiling.
    ///
    /// Counter-intuitively a LEAN cache lasts longer in days while nourishing
    /// less per day, because the ceiling throttles how fast it can be eaten. Both
    /// halves of that are the protein mechanic working.
    /// </summary>
    public float DaysOfFood(float bodyweightKg, BalanceData balance)
    {
        float ceilingG = NutritionModel.ProteinCeilingG(bodyweightKg, balance);
        if (ceilingG <= 0f) return 0f;

        // Days of food is stored protein over the daily protein ceiling, because
        // the ceiling - not appetite, and not the day's burn - is what limits how
        // fast the larder can actually be eaten. Measuring against burn instead
        // understates supplies by roughly 2x and makes the player read as
        // food-insecure while sitting on nearly a week of meat.
        return proteinG / ceilingG;
    }

    /// <summary>Add a harvest. Anything over capacity is lost - see the class remarks.</summary>
    public void Add(float protein, float fat)
    {
        proteinG += protein;
        fatG += fat;

        float overflow = StoredKg - CapacityKg;
        if (overflow > 0f)
        {
            float keep = CapacityKg / StoredKg;
            proteinG *= keep;
            fatG *= keep;
        }
    }

    /// <summary>
    /// Eat toward <paramref name="appetiteKcal"/>, stopping at the protein ceiling.
    ///
    /// Draws protein and fat in the proportion stored, so a lean larder yields a
    /// lean meal - the player cannot pick the fat out of lean meat.
    ///
    /// The ceiling cap is not just realism, it is the sensible-actor rule: eating
    /// past the ceiling delivers ZERO extra usable energy while consuming stores,
    /// so no competent player would do it. Modelling appetite as "eat until burn
    /// is covered" instead makes the player devour their whole larder every day
    /// chasing calories they cannot absorb, and starve with a full cache for the
    /// wrong reason.
    ///
    /// It has a consequence worth noticing: the protein ceiling makes lean food
    /// LAST LONGER, because you physically cannot eat it fast. A lean cache is
    /// simultaneously less nourishing per day and slower to run out.
    /// </summary>
    public Macros Eat(float appetiteKcal, float bodyweightKg, BalanceData balance)
    {
        float available = GrossKcal(balance);
        if (available <= 0f || appetiteKcal <= 0f) return default;

        float fraction = Math.Min(1f, appetiteKcal / available);

        // Cap the draw so protein intake stops at the ceiling.
        float ceilingG = NutritionModel.ProteinCeilingG(bodyweightKg, balance);
        if (proteinG > 0f)
        {
            fraction = Math.Min(fraction, ceilingG / proteinG);
        }

        fraction = Math.Clamp(fraction, 0f, 1f);
        var meal = new Macros(proteinG * fraction, fatG * fraction, 0f);

        proteinG -= meal.ProteinG;
        fatG -= meal.FatG;
        return meal;
    }

    /// <summary>
    /// Daily spoilage, derived from the preservation method's shelf life. A cache
    /// pit keeps 12 days, a drying rack 30 - expressed as a per-day decay so it
    /// applies smoothly rather than as a cliff.
    /// </summary>
    public void ApplyDailySpoilage()
    {
        int shelfLife = Method == PreservationMethod.None
            ? 3
            : PreservationTable.Get(Method).ShelfLifeDays;

        float keep = 1f - 1f / shelfLife;
        proteinG *= keep;
        fatG *= keep;
    }
}
