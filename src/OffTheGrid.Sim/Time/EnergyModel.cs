using System;
using OffTheGrid.Data.Balance;
using OffTheGrid.Sim.Body;

namespace OffTheGrid.Sim.Time;

/// <summary>Activity energy cost. Design spec 5.2.</summary>
public static class EnergyModel
{
    /// <summary>
    /// kcal_hr = MET * 1.05 * W * terrain_mult * fitness_mult
    /// fitness_mult = 1.00 - 0.02*(Fitness - 5)   // efficient bodies do more per kcal
    /// Movement-type activities additionally multiply by (W/80)^1.15.
    /// </summary>
    public static float KcalPerHour(
        Activity activity,
        float weightKg,
        int fitness,
        float terrainMultiplier,
        BalanceData balance)
    {
        float fitnessMultiplier = 1f - balance.FitnessEfficiencyPerPoint * (fitness - balance.FitnessBaseline);

        float kcal = activity.Met()
                   * balance.ActivityKcalConstant
                   * weightKg
                   * terrainMultiplier
                   * fitnessMultiplier;

        if (activity.IsMovement())
        {
            kcal *= MathF.Pow(weightKg / balance.MovementMassReference, balance.MovementMassExponent);
        }

        return kcal;
    }

    /// <summary>
    /// Energy a slot costs ON TOP of resting metabolism, which is what gets added
    /// to a full day's BMR.
    ///
    /// [B11] Design spec 5.1 and 5.2 give BMR and activity cost separately without
    /// saying whether they add. They cannot simply add: MET 1.0 IS resting
    /// metabolism, so BMR(24h) + full MET cost double-counts the baseline for every
    /// active hour.
    ///
    /// Balance doc 3.3 settles it. It states a daily burn of 2,850 kcal for an 85 kg
    /// player, against a BMR of 1,805 - leaving 1,045 kcal of activity across five
    /// 2.2 h slots. Under the naive reading that implies an average MET of 1.13,
    /// barely above sleep, which cannot be a day that includes building a shelter.
    /// Under excess-over-resting it implies MET 2.13, an ordinary mixed day with
    /// rest in it. So excess is the formulation the balance numbers assume.
    ///
    /// This matters: the naive reading burns ~5,600 kcal on a five-slot working day
    /// and makes the game unwinnable.
    /// </summary>
    public static float ExcessKcalForSlot(
        Activity activity,
        BodyState body,
        int fitness,
        float terrainMultiplier,
        BalanceData balance)
    {
        float active = KcalPerHour(activity, body.WeightKg, fitness, terrainMultiplier, balance);
        float resting = KcalPerHour(Activity.Rest, body.WeightKg, fitness, terrainMultiplier, balance)
                      / Activity.Rest.Met();   // MET 1.0 equivalent

        return MathF.Max(0f, active - resting) * Calendar.HoursPerSlot;
    }

    /// <summary>
    /// Extra calories burned overnight when insulation falls short of what the
    /// temperature demands.
    ///
    /// The sim previously had NO thermoregulation cost at all. Being cold only
    /// set a -4 morale flag, which left three things mechanically hollow: Cold
    /// Adaptation (its whole specced role is a thermoneutral offset), shelter clo
    /// above the morale threshold, and the entire firewood economy. Balance doc
    /// 6 builds a detailed fuel model whose output had nowhere to land.
    ///
    /// Roughly 90 kcal per clo of deficit per night, scaled by body mass - a
    /// smaller body loses heat faster relative to its reserves. Capped, because
    /// shivering has a ceiling (balance doc 7.2) and beyond it you get colder
    /// rather than burning more.
    /// </summary>
    public static float ThermoregulationKcal(float cloDeficit, float weightKg, BalanceData balance)
    {
        if (cloDeficit <= 0f) return 0f;

        float capped = MathF.Min(cloDeficit, 2.5f);
        float massTerm = MathF.Pow(70f / MathF.Max(weightKg, 40f), 0.4f);

        // 320 kcal per clo of deficit, not 90.
        //
        // At 90 the arithmetic said something absurd: a full night of cold stress
        // cost at most ~225 kcal, while a single slot spent building shelter cost
        // ~700 kcal plus the food that slot did not catch. Being cold was
        // literally cheaper than fixing it, and the balance gates caught it -
        // players who built shelter and cut wood died MORE often than players who
        // ignored both and fished.
        //
        // Shivering thermogenesis is genuinely expensive: several hundred kcal
        // over a night of real cold stress. 320 makes shelter and fuel worth the
        // slots they cost, which is the entire premise of balance doc 4 and 5.
        return capped * 320f * massTerm;
    }

    /// <summary>Total metabolic cost of a slot, ignoring the BMR overlap. For display and reference.</summary>
    public static float KcalForSlot(
        Activity activity,
        BodyState body,
        int fitness,
        float terrainMultiplier,
        BalanceData balance) =>
        KcalPerHour(activity, body.WeightKg, fitness, terrainMultiplier, balance) * Calendar.HoursPerSlot;
}
