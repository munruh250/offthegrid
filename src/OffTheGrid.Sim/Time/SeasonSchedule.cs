using System;

namespace OffTheGrid.Sim.Time;

/// <summary>
/// When the seasons turn. A scenario parameter, not a constant.
///
/// Fixed season dates made the whole balance frame wrong. If winter always
/// arrives on day 51, then "did the player reach day 60" is the only question
/// worth asking - and a run that ends on day 43 looks like a failure rather than
/// like a player who never faced winter at all.
///
/// Making the schedule movable turns difficulty into a change in the SHAPE of
/// the problem rather than a nudge to its numbers. "You have three weeks to
/// winterize" and "you have seven" are different games built from identical
/// systems, and only one of them makes the fuel economy matter.
/// </summary>
public sealed record SeasonSchedule(int AbundanceEnds, int TaperingEnds, int LeanEnds)
{
    /// <summary>The day winter arrives - the deadline the whole run is measured against.</summary>
    public int WinterArrives => LeanEnds + 1;

    public Season SeasonForDay(int dayNumber)
    {
        if (dayNumber <= AbundanceEnds) return Season.SalmonRun;
        if (dayNumber <= TaperingEnds) return Season.RunTapering;
        if (dayNumber <= LeanEnds) return Season.Lean;
        return Season.Winter;
    }

    /// <summary>
    /// How far through the pre-winter preparation window a day sits, 0 to 1.
    /// This is the clock the player is really racing, and it is what balance
    /// checks should measure against instead of an absolute day count.
    /// </summary>
    public float WinterizationProgress(int dayNumber) =>
        Math.Clamp(dayNumber / (float)WinterArrives, 0f, 1f);

    /// <summary>Vancouver Island as specced. Balance doc 3.4.</summary>
    public static SeasonSchedule Standard { get; } = new(20, 35, 50);

    /// <summary>Three weeks to winterize. The fuel and shelter economies become the game.</summary>
    public static SeasonSchedule ShortSummer { get; } = new(8, 14, 21);

    /// <summary>A generous run. Tests whether the player gets complacent.</summary>
    public static SeasonSchedule LongFall { get; } = new(28, 48, 70);
}
