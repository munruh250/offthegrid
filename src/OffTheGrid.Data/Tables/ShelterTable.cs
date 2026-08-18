using System;
using System.Collections.Generic;

namespace OffTheGrid.Data.Tables;

public enum ShelterTier
{
    None,
    TarpLeanTo,
    DebrisHut,
    AFrame,
    ReflectorWallCamp,
    LogShelter,
    LogCabin
}

/// <param name="Tier">Which shelter.</param>
/// <param name="Slots">Build cost in action slots. Also drives the relocation morale hit (doc 12).</param>
/// <param name="Logs">Logs required.</param>
/// <param name="CordageM">Cordage required, in metres.</param>
/// <param name="Clo">Insulation contributed.</param>
public readonly record struct ShelterEntry(
    ShelterTier Tier,
    int Slots,
    int Logs,
    int CordageM,
    float Clo);

/// <summary>Shelter economy, transcribed from balance doc 5.</summary>
public static class ShelterTable
{
    private static readonly Dictionary<ShelterTier, ShelterEntry> entries = new()
    {
        [ShelterTier.None]              = new(ShelterTier.None,               0,   0,  0, 0.0f),
        [ShelterTier.TarpLeanTo]        = new(ShelterTier.TarpLeanTo,         1,   4,  6, 0.4f),
        [ShelterTier.DebrisHut]         = new(ShelterTier.DebrisHut,          3,  12,  0, 1.1f),
        [ShelterTier.AFrame]            = new(ShelterTier.AFrame,             5,  26, 12, 1.6f),
        [ShelterTier.ReflectorWallCamp] = new(ShelterTier.ReflectorWallCamp,  8,  48, 20, 2.2f),
        [ShelterTier.LogShelter]        = new(ShelterTier.LogShelter,        16, 110, 35, 3.0f),
        [ShelterTier.LogCabin]          = new(ShelterTier.LogCabin,          28, 210, 60, 4.2f)
    };

    public static ShelterEntry Get(ShelterTier tier) => entries[tier];

    public static IReadOnlyCollection<ShelterEntry> All => entries.Values;

    /// <summary>
    /// Total clo needed to sleep at a given night temperature. Balance doc 5.2.
    /// Interpolated between the doc's points.
    /// </summary>
    public static float CloDemandForNightTemp(float celsius)
    {
        (float temp, float clo)[] points =
        [
            (12f, 2.0f), (8f, 2.8f), (4f, 3.6f), (0f, 4.4f), (-5f, 5.4f)
        ];

        if (celsius >= points[0].temp) return points[0].clo;
        if (celsius <= points[^1].temp) return points[^1].clo;

        for (int i = 0; i < points.Length - 1; i++)
        {
            var (t0, c0) = points[i];
            var (t1, c1) = points[i + 1];
            if (celsius <= t0 && celsius >= t1)
            {
                float f = (t0 - celsius) / (t0 - t1);
                return c0 + f * (c1 - c0);
            }
        }

        return points[^1].clo;
    }

    /// <summary>Issued clothing plus sleeping bag, before any shelter. Balance doc 5.2.</summary>
    public const float BaseClothingClo = 1.5f;
    public const float SleepingBagClo = 1.5f;
}
