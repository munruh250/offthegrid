using System;
using OffTheGrid.Data.Gear;
using OffTheGrid.Data.Tables;
using OffTheGrid.Sim.Time;

namespace OffTheGrid.Sim.Fire;

/// <summary>
/// The fuel economy. Balance doc 4.
///
/// Doc 4.2 states outright that "the late-game difficulty curve emerges entirely
/// from these two tables" - daylight shrinking while wood demand climbs. None of
/// it existed in the sim: ChoppingWood, Sawing and HaulingLogs all cost calories
/// and produced nothing, and the fire had no mechanical presence at all.
///
/// With thermoregulation now costing real calories, fuel is the second resource
/// axis the game needs. It is also where the axe, the saw and Bushcraft finally
/// earn their place in a loadout that was otherwise all fishing tackle.
/// </summary>
public static class Firewood
{
    /// <summary>
    /// Wood a slot yields, by tool. Balance doc 4.3.
    ///
    /// The axeless player is the point of this table: 12 kg/slot of deadfall
    /// covers a September night and cannot cover November, which is a gear
    /// decision made in the first five minutes killing you on day fifty.
    /// </summary>
    public static float YieldPerSlot(Activity activity, Loadout gear, int bushcraft)
    {
        bool axe = gear.Has(GearItem.Axe);
        bool saw = gear.Has(GearItem.Saw);

        float baseYield = activity switch
        {
            Activity.ChoppingWood =>
                (axe, saw) switch
                {
                    (true, true) => 52f,   // process to splits, both tools
                    (true, false) => 35f,  // process to splits, axe only
                    _ => 12f               // gather deadfall - always available
                },

            Activity.Sawing =>
                saw && bushcraft >= 6 ? 70f     // bucking long logs
                : saw ? 45f                      // fell standing dead
                : 12f,

            Activity.HaulingLogs => axe || saw ? 45f : 12f,

            _ => 0f
        };

        // Bushcraft makes every fuel slot go further.
        return baseYield * (0.8f + 0.04f * bushcraft);
    }

    /// <summary>
    /// Wood a night costs, by temperature. Balance doc 4.2, interpolated.
    /// </summary>
    public static float NightlyDemandKg(float nightTempCelsius, ShelterTier shelter)
    {
        (float temp, float kg)[] points =
        [
            (12f, 8.0f), (8f, 11.0f), (4f, 15.5f), (0f, 20.0f), (-5f, 23.0f)
        ];

        float demand = points[^1].kg;
        if (nightTempCelsius >= points[0].temp) demand = points[0].kg;
        else
        {
            for (int i = 0; i < points.Length - 1; i++)
            {
                var (t0, k0) = points[i];
                var (t1, k1) = points[i + 1];
                if (nightTempCelsius <= t0 && nightTempCelsius >= t1)
                {
                    float f = (t0 - nightTempCelsius) / (t0 - t1);
                    demand = k0 + f * (k1 - k0);
                    break;
                }
            }
        }

        // A reflector wall burns MORE wood per hour but returns double the useful
        // heat (doc 4.1) - a wood-for-warmth trade, not a straight upgrade. Net
        // it needs less fuel for the same warmth.
        float efficiency = shelter >= ShelterTier.ReflectorWallCamp ? 0.62f : 1.0f;
        return demand * efficiency;
    }

    /// <summary>
    /// Effective insulation a fire contributes, scaled by how well it is fed.
    /// A fire running on fumes is worth much less than a fire running all night.
    /// </summary>
    public static float FireClo(float fuelRatio) =>
        1.9f * Math.Clamp(fuelRatio, 0f, 1f);
}
