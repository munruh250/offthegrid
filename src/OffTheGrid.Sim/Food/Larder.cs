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
    private float carbohydrateG;
    private float boneKg;

    /// <summary>
    /// Drying rack by default: 30-day shelf life against the cache pit's 12.
    /// Doc 4.3 tells the player to bank during the salmon run, and banking is
    /// impossible on a 12-day shelf life - a cache pit loses everything before
    /// the lean season arrives. The 25% processing loss is the price of that.
    /// </summary>
    public PreservationMethod Method { get; set; } = PreservationMethod.DryingRack;

    /// <summary>
    /// Capacity in kg of edible mass. Beyond this a harvest is simply lost.
    ///
    /// This SCALES with camp investment rather than being a constant. Balance doc
    /// 8 names preservation the top tuning lever, but a fixed cap means investment
    /// cannot buy a strategy - it just tops off the same small tank, and every
    /// contestant ends up playing the same grind. A cache that grows is what makes
    /// "bank hard during the run, then coast" a real alternative shape of run,
    /// which is how people actually survive the back half on the show.
    /// </summary>
    public float CapacityKg { get; set; } = BaseCapacityKg;

    public const float BaseCapacityKg = 15f;

    /// <summary>Capacity earned by camp tier and bushcraft skill.</summary>
    public static float CapacityFor(int shelterTierIndex, int bushcraft) =>
        BaseCapacityKg + 6f * shelterTierIndex + bushcraft;

    public float ProteinG => proteinG;
    public float FatG => fatG;

    /// <summary>Stored carbohydrate. Bypasses the protein ceiling entirely.</summary>
    public float CarbohydrateG => carbohydrateG;

    /// <summary>
    /// Bone held from past kills, awaiting rendering. Balance doc 3.4 puts marrow
    /// at ~180 kcal/kg of bone, almost pure fat.
    /// </summary>
    public float BoneKg => boneKg;

    /// <summary>Rough edible mass, from macro density. Used against capacity.</summary>
    public float StoredKg => (proteinG + fatG + carbohydrateG) / 1000f * 4f;

    public float GrossKcal(BalanceData balance) =>
        proteinG * balance.KcalPerGramProtein
      + fatG * balance.KcalPerGramFat
      + carbohydrateG * balance.KcalPerGramCarbohydrate;

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

    /// <summary>Bone recovered per kg of edible mass. Bigger animals carry more.</summary>
    public const float BoneFractionOfEdible = 0.22f;

    /// <summary>
    /// Add a harvest, including the bone that came with it.
    ///
    /// The preservation method's processing loss is applied HERE, on the way in.
    /// Balance doc 4 is explicit that this - together with rack capacity and the
    /// slots processing costs - is what caps banking, NOT aggressive rot: "the
    /// player banks 30-50 kg and loses the remainder."
    /// </summary>
    public void Add(float protein, float fat, float carbohydrate = 0f, float edibleKg = 0f)
    {
        boneKg += edibleKg * BoneFractionOfEdible;

        float processingLoss = Method == PreservationMethod.None
            ? 0f
            : PreservationTable.Get(Method).LossFraction;
        float kept = 1f - processingLoss;
        protein *= kept;
        fat *= kept;
        carbohydrate *= kept;
        proteinG += protein;
        fatG += fat;
        carbohydrateG += carbohydrate;

        float overflow = StoredKg - CapacityKg;
        if (overflow > 0f)
        {
            float keep = CapacityKg / StoredKg;
            proteinG *= keep;
            fatG *= keep;
            carbohydrateG *= keep;
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
        var meal = new Macros(proteinG * fraction, fatG * fraction, carbohydrateG * fraction);

        proteinG -= meal.ProteinG;
        fatG -= meal.FatG;
        carbohydrateG -= meal.CarbohydrateG;
        return meal;
    }

    /// <summary>
    /// Cut the store down to what can be carried. Doc 12: you can move one cache
    /// and no more, and everything else is abandoned.
    /// </summary>
    public void TrimTo(float carryKg)
    {
        if (StoredKg <= carryKg || StoredKg <= 0f) return;
        float keep = carryKg / StoredKg;
        proteinG *= keep;
        fatG *= keep;
        carbohydrateG *= keep;
        boneKg *= keep;
    }

    /// <summary>
    /// Render stored bone into fat. Resolves B3.
    ///
    /// The fat economy's real problem was never "too few fatty animals" - it is
    /// that in EVERY animal except bear, fat arrives bonded to protein, so the
    /// ceiling caps it before the fat is exhausted. Chinook is 52% fat by calories
    /// and still cannot sustain a player alone.
    ///
    /// Marrow is the protein-free fat path. Every animal has bones, so this turns
    /// the fat economy from an RNG gate ("did a bear spawn?") into a skill-and-
    /// tool investment ("did I keep the bones, and spend the slots?"). It also
    /// gives the elk's otherwise-useless lean mass a partial redemption.
    ///
    /// Returns kg of bone actually processed.
    /// </summary>
    public float RenderMarrow(float slots, BalanceData balance)
    {
        const float bonePerSlot = 8f;

        // 60 g/kg, not 20.
        //
        // Balance doc 3.4's "+180 kcal/kg bone" prices MARROW alone. What a slot
        // of rendering actually recovers is marrow plus bone grease plus the fat
        // trimmed from the carcass and the organs - which is how the fat in a
        // lean-animal biome is really obtained, and the whole point of B3's
        // protein-free fat path.
        //
        // At 20 g/kg a day's catch yielded ~200 kcal, which is a rounding error
        // against a 2,400 kcal/day deficit, so the Pot was a dead gear pick and
        // the fat economy had exactly one source: bear. At 60 g/kg a good day's
        // bones are worth ~600 ceiling-free kcal, which makes rendering a real
        // decision and gives every lean kill a second use.
        const float fatGramsPerKgBone = 60f;

        float processed = Math.Min(boneKg, slots * bonePerSlot);
        if (processed <= 0f) return 0f;

        boneKg -= processed;
        fatG += processed * fatGramsPerKgBone;
        return processed;
    }

    /// <summary>
    /// Daily spoilage, derived from the preservation method's shelf life. A cache
    /// pit keeps 12 days, a drying rack 30 - expressed as a per-day decay so it
    /// applies smoothly rather than as a cliff.
    /// </summary>
    public void ApplyDailySpoilage(float nightTempCelsius = 10f)
    {
        int shelfLife = Method == PreservationMethod.None
            ? 3
            : PreservationTable.Get(Method).ShelfLifeDays;

        // Cold is a preservation method. Below freezing the cache essentially
        // holds, which is how northern contestants actually bank a big kill.
        float coldBonus = nightTempCelsius switch
        {
            <= -5f => 6.0f,
            <= 0f => 4.0f,
            <= 5f => 2.0f,
            _ => 1.0f
        };

        // A shelf life is a SAFE WINDOW, not a half-life. Properly dried or
        // smoked food keeps for months; it does not lose a thirtieth of itself
        // every day. Treating the doc's figure as a decay constant destroyed 64%
        // of a cache in 30 days, which made stockpiling pointless and is simply
        // wrong - banking the salmon run to live on later is the central
        // strategic move of the whole format.
        //
        // The cap on banking is rack CAPACITY and the SLOTS processing costs
        // (balance doc 4), plus the processing loss taken on the way in. Not rot.
        const float SafeWindowMultiplier = 5f;
        float effectiveShelfLife = shelfLife * coldBonus * SafeWindowMultiplier;
        float keep = 1f - 1f / effectiveShelfLife;
        proteinG *= keep;
        fatG *= keep;
    }
}
