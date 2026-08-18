using System;
using System.Collections.Generic;

namespace OffTheGrid.Data.Tables;

/// <summary>Every food source in the MVP biome (Vancouver Island).</summary>
public enum FoodSource
{
    RooseveltElk,
    BlackBear,
    BlacktailDeer,
    ChinookSalmon,
    CohoSalmon,
    SockeyeSalmon,
    SnowshoeHare,
    CutthroatTrout,
    Rockfish,
    Grouse,
    Mussels,
    DungenessCrab
}

/// <summary>
/// Composition of one whole animal, as edible mass.
/// </summary>
/// <param name="Source">Which animal.</param>
/// <param name="LiveKg">Live weight.</param>
/// <param name="EdibleKg">Edible mass after dressing.</param>
/// <param name="Kcal">Gross calories in the whole edible mass.</param>
/// <param name="ProteinG">Total protein.</param>
/// <param name="FatG">Total fat.</param>
public readonly record struct FoodEntry(
    FoodSource Source,
    float LiveKg,
    float EdibleKg,
    float Kcal,
    float ProteinG,
    float FatG)
{
    /// <summary>
    /// Fraction of this food's calories that come from fat. Balance doc 1.2: fat
    /// is the currency, protein is a trap. Only black bear clears 50% by enough
    /// to sustain a player alone.
    /// </summary>
    public float FatCalorieFraction => Kcal <= 0f ? 0f : FatG * 9f / Kcal;

    /// <summary>Macros for a given edible mass in kg, scaled linearly.</summary>
    public (float ProteinG, float FatG) MacrosForKg(float kg)
    {
        if (EdibleKg <= 0f) return (0f, 0f);
        float scale = kg / EdibleKg;
        return (ProteinG * scale, FatG * scale);
    }
}

/// <summary>
/// Food composition, transcribed from balance doc 2.
///
/// Values are as written in the doc and must not be adjusted here. Balance doc
/// 3.2 is explicit: never nerf an animal's calorie content to hit a ratio -
/// "that breaks the realism promise and makes big kills feel disappointing at the
/// moment they should feel triumphant." Preservation capacity is the tuning
/// lever, not raw yield.
/// </summary>
public static class FoodTable
{
    private static readonly Dictionary<FoodSource, FoodEntry> entries = new()
    {
        [FoodSource.RooseveltElk]   = new(FoodSource.RooseveltElk,   270f,  121.5f, 177_390f, 36_693f, 2_308f),
        [FoodSource.BlackBear]      = new(FoodSource.BlackBear,      110f,   44.0f, 113_960f,  8_844f, 8_360f),
        [FoodSource.BlacktailDeer]  = new(FoodSource.BlacktailDeer,   55f,   24.8f,  39_105f,  7_474f,   792f),
        [FoodSource.ChinookSalmon]  = new(FoodSource.ChinookSalmon,    9.0f,  5.4f,   9_666f,  1_075f,   562f),
        [FoodSource.CohoSalmon]     = new(FoodSource.CohoSalmon,       4.0f,  2.4f,   3_504f,    518f,   142f),
        [FoodSource.SockeyeSalmon]  = new(FoodSource.SockeyeSalmon,    2.7f,  1.6f,   2_722f,    345f,   139f),
        [FoodSource.SnowshoeHare]   = new(FoodSource.SnowshoeHare,     1.4f,  0.8f,   1_332f,    254f,    27f),
        [FoodSource.CutthroatTrout] = new(FoodSource.CutthroatTrout,   1.0f,  0.55f,    654f,    109f,    19f),
        [FoodSource.Rockfish]       = new(FoodSource.Rockfish,         1.2f,  0.5f,     567f,    102f,     9f),
        [FoodSource.Grouse]         = new(FoodSource.Grouse,           0.6f,  0.3f,     469f,     82f,    13f),
        [FoodSource.Mussels]        = new(FoodSource.Mussels,          1.0f,  0.3f,     258f,     36f,     7f),
        [FoodSource.DungenessCrab]  = new(FoodSource.DungenessCrab,    0.9f,  0.2f,     196f,     39f,     2f)
    };

    public static FoodEntry Get(FoodSource source) => entries[source];

    public static IReadOnlyCollection<FoodEntry> All => entries.Values;
}
