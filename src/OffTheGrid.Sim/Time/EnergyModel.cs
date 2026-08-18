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

    /// <summary>Total metabolic cost of a slot, ignoring the BMR overlap. For display and reference.</summary>
    public static float KcalForSlot(
        Activity activity,
        BodyState body,
        int fitness,
        float terrainMultiplier,
        BalanceData balance) =>
        KcalPerHour(activity, body.WeightKg, fitness, terrainMultiplier, balance) * Calendar.HoursPerSlot;
}
