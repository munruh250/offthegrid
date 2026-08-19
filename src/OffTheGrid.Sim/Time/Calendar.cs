using System;
using OffTheGrid.Data.Balance;

namespace OffTheGrid.Sim.Time;

/// <summary>
/// Daylight and the slot economy. Design spec 7.1.
///
/// This is where the late-game difficulty curve comes from, and it is worth being
/// explicit that NOTHING here is authored. Daylight shortens because the season
/// advances; slots fall out of daylight; firewood demand rises as temperature
/// drops. The player has fewer actions per day at exactly the point each action
/// costs more and their body is weakest.
///
/// Balance doc 6.2 traces it: September gives 5 slots and wood costs 0.23 of one.
/// November gives 3 slots and wood costs 0.66 - 22% of a day that is already 40%
/// shorter. That is the whole difficulty curve, emergent from two tables.
/// </summary>
public static class Calendar
{
    public const int MinSlotsPerDay = 3;
    public const int MaxSlotsPerDay = 7;

    /// <summary>Hours of daylight per action slot. Design spec 7.1.</summary>
    public const float HoursPerSlot = 2.2f;

    /// <summary>
    /// Daylight hours on a given run day. Day 1 is 15 September on Vancouver
    /// Island (49.5 N), the MVP biome.
    ///
    /// Modelled as a cosine about the winter solstice rather than interpolated
    /// from a table, so it stays smooth and extends past day 75 without a cliff.
    /// Checked against the spec 7.1 table in tests.
    /// </summary>
    public static float DaylightHours(int dayNumber)
    {
        if (dayNumber < 1) throw new ArgumentOutOfRangeException(nameof(dayNumber));

        // Day 1 = 15 Sep = day-of-year 258. Solstice = day-of-year 355.
        int dayOfYear = 258 + (dayNumber - 1);
        float daysFromSolstice = dayOfYear - 355f;

        // Minus, not plus: the solstice is the MINIMUM. Fitted to the spec 7.1
        // table for 49.5 N, which it reproduces to within ~0.1 h at every listed day.
        const float meanDaylight = 12.15f;
        const float amplitude = 4.4f;

        return meanDaylight - amplitude * MathF.Cos(2f * MathF.PI * daysFromSolstice / 365f);
    }

    /// <summary>
    /// Action slots available on a given day. Design spec 7.1:
    /// slots = clamp(floor(daylight / 2.2), 3, 7)
    /// </summary>
    public static int SlotsForDay(int dayNumber)
    {
        int raw = (int)MathF.Floor(DaylightHours(dayNumber) / HoursPerSlot);
        return Math.Clamp(raw, MinSlotsPerDay, MaxSlotsPerDay);
    }

    /// <summary>
    /// Seasonal phase, which drives the food tables. Balance doc 4.3.
    /// Defaults to the standard schedule; scenarios supply their own.
    /// </summary>
    public static Season SeasonForDay(int dayNumber, SeasonSchedule? schedule = null) =>
        (schedule ?? SeasonSchedule.Standard).SeasonForDay(dayNumber);

    /// <summary>
    /// Palette interpolation parameter for the presentation layer, t = (day-1)/59.
    /// Cedar and Lichen at 0, Cold Front at 1. Lives here because the visual arc
    /// and the mechanical arc are the same arc - the UI is a difficulty readout.
    /// </summary>
    public static float SeasonalPaletteT(int dayNumber) =>
        Math.Clamp((dayNumber - 1) / 59f, 0f, 1f);
}

/// <summary>Seasonal phase. Balance doc 4.3.</summary>
public enum Season
{
    /// <summary>Days 1-20. Abundance window - fat is available, bank it now.</summary>
    SalmonRun,

    /// <summary>Days 21-35. Coho declining, fat sources thinning.</summary>
    RunTapering,

    /// <summary>Days 36-50. The squeeze.</summary>
    Lean,

    /// <summary>Days 51+. Cache or die. Bear denned.</summary>
    Winter
}
