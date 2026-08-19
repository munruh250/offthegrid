using System;
using OffTheGrid.Data;
using OffTheGrid.Data.Balance;
using OffTheGrid.Sim.Morale;

namespace OffTheGrid.Sim.Events;

/// <summary>An acute event that landed on a given day.</summary>
public readonly record struct AcuteEvent(AcuteEventKind Kind, float Magnitude, string Code);

public enum AcuteEventKind
{
    None,

    /// <summary>Homesickness, isolation, the weight of the decision. Design spec 5.6.</summary>
    MemoryEvent,

    /// <summary>Rain, wind, a bad night. Drives SoakedAtSleep.</summary>
    Storm,

    /// <summary>Cut, burn, strain. Costs slots and morale.</summary>
    Injury
}

/// <summary>
/// The acute layer: shocks rather than attrition.
///
/// WHY THIS EXISTS. Without it the run is a smooth slide and every contestant
/// slides at the same rate - the v0.1 sim produced a 12-day spread across 60
/// seeds and could not put anyone out before day 24. The show's characteristic
/// exits are EVENTS, and they are heavily front-loaded: a meaningful fraction of
/// any cast leaves in the first week, for reasons that are almost never
/// physiological.
///
/// RESOLVE IS THE GATE, on both axes. A low-Resolve contestant meets shocks MORE
/// OFTEN and each one hits HARDER. That is what makes the early drop-out curve
/// emerge from the roster's composition rather than from a rule that says "some
/// people leave early", and it is what finally makes Resolve a meaningful pick
/// at character creation rather than a slow-acting modifier.
///
/// RNG streams: events.trigger, events.magnitude, events.kind.
/// </summary>
public static class AcuteEvents
{
    /// <summary>
    /// The settling-in window. Design intent: the first days are when the
    /// decision is still reversible in the contestant's mind, and when the gap
    /// between the imagined experience and the real one is widest.
    /// </summary>
    public const int SettlingInDays = 10;

    /// <summary>
    /// Daily chance of an acute event, before Resolve.
    ///
    /// Elevated during settling-in, then a lower steady rate. The late-run rise
    /// is deliberate too: isolation compounds, and the design already weights
    /// memory events by day count.
    /// </summary>
    public static float BaseEventChance(int dayNumber) => dayNumber switch
    {
        <= SettlingInDays => 0.34f,
        <= 30 => 0.10f,
        <= 50 => 0.13f,
        _ => 0.17f
    };

    /// <summary>
    /// How much more often a low-Resolve contestant is hit. Resolve 1 meets
    /// events at ~1.8x the rate of Resolve 10.
    /// </summary>
    public static float ResolveFrequencyMultiplier(int resolve, BalanceData balance) =>
        1.4f - 0.8f * (resolve / 10f);

    /// <summary>
    /// Does this crisis make them quit?
    ///
    /// Tap-out is a DECISION taken in a moment, not a bar reaching zero. On the
    /// show people leave while still physically capable - they hit a bad night,
    /// think about their family, and call it. Modelling tap-out purely as
    /// "morale reached 0" makes that impossible and pushes every exit to the far
    /// end of the run, which is why v0.1 could not put anyone out before day 24.
    ///
    /// Three factors, all of which match how it actually happens:
    ///   - How worn down they already are (low morale, higher chance).
    ///   - Resolve, which is the whole point of the attribute.
    ///   - How early it is. In the first days the decision still feels reversible
    ///     and the gap between the imagined trip and the real one is widest.
    ///     Deep into a run, people who are still there have already decided.
    /// </summary>
    /// <summary>
    /// Does the contestant quit today?
    ///
    /// Checked EVERY day, not only when a crisis fires. The option to walk is
    /// always on the table - that is the defining feature of the format - and a
    /// crisis merely amplifies a pressure that is already there. Gating tap-out
    /// on crisis events alone left the medic responsible for 77% of exits, which
    /// inverts the show, where the overwhelming majority of departures are
    /// voluntary.
    /// </summary>
    public static bool ConsidersTappingOut(
        int dayNumber, int resolve, float currentMorale, float bodyConditionRatio,
        bool crisisToday, Rng rng, BalanceData balance)
    {
        float worn = 1f - Math.Clamp(currentMorale / balance.MoraleMax, 0f, 1f);
        // Resolve divisor of 12 gave a 5x spread between Resolve 2 and 10, which
        // measured at 4.12 days per point - 2.6x the next best attribute and the
        // clear meta pick. 16 narrows it to ~2.3x: still the strongest single
        // stat, which is faithful to the format, without making the other five
        // decorative.
        float fragility = 1f - resolve / 56f;
        // Three phases, matching how the format actually plays out:
        //   settling-in  - the decision still feels reversible, shock is highest
        //   the grind    - the day-20-to-45 plateau the design itself flags
        //                  (spec 12.3). People leave here from tedium and
        //                  loneliness while still physically fine, and this is
        //                  the phase v0.1 had no model for at all.
        //   committed    - whoever is still out here has already decided
        float phase = dayNumber switch
        {
            <= SettlingInDays => 2.2f,
            <= 45 => 1.6f,
            _ => 0.9f
        };

        // Physical condition feeds the DECISION, not just the medical threshold.
        // On the show, contestants who are wasting usually tap before the medic
        // pulls them - they see what they have become, or their family does, and
        // they call it. Modelling the body purely as a hard pull threshold makes
        // every deep exit involuntary, which inverts the show: most exits there
        // are voluntary. bodyConditionRatio is 1 at full condition, 0 at the
        // medical floor.
        float wasted = 1f - Math.Clamp(bodyConditionRatio, 0f, 1f);
        float bodyPressure = 1f + 2.5f * wasted * wasted;

        // A crisis is a spike on top of the ambient daily pressure.
        float crisis = crisisToday ? 9f : 1f;

        // Base compensates for the wider Resolve divisor above: raising the divisor
        // narrows the spread but also shifts everyone toward fragile, so the
        // overall exit rate has to come back down to hold the survival curve.
        float chance = 0.062f * worn * fragility * phase * bodyPressure * crisis;
        return rng.Stream("events.tapout").NextFloat() < chance;
    }

    /// <summary>
    /// Roll the day's acute event, if any. Returns <see cref="AcuteEventKind.None"/>
    /// on most days.
    /// </summary>
    public static AcuteEvent Roll(int dayNumber, int resolve, bool inShelter, Rng rng, BalanceData balance)
    {
        float chance = BaseEventChance(dayNumber) * ResolveFrequencyMultiplier(resolve, balance);

        if (rng.Stream("events.trigger").NextFloat() >= chance) return default;

        float kindRoll = rng.Stream("events.kind").NextFloat();
        float magnitudeRoll = rng.Stream("events.magnitude").NextFloat();

        // Early on it is overwhelmingly psychological. Later, exposure and wear
        // take a larger share.
        bool early = dayNumber <= SettlingInDays;
        float memoryShare = early ? 0.75f : 0.45f;
        float stormShare = early ? 0.20f : 0.40f;

        if (kindRoll < memoryShare)
        {
            // Spec 5.6: -(5 to 20), scaled by Resolve inside MoraleState.
            float raw = balance.MoraleMemoryEventMin
                      + magnitudeRoll * (balance.MoraleMemoryEventMax - balance.MoraleMemoryEventMin);
            return new AcuteEvent(AcuteEventKind.MemoryEvent, raw, early ? "memory.dropshock" : "memory.isolation");
        }

        if (kindRoll < memoryShare + stormShare)
        {
            // A storm soaks you unless the shelter holds.
            return new AcuteEvent(AcuteEventKind.Storm, inShelter ? 0f : 1f,
                inShelter ? "weather.storm.sheltered" : "weather.storm.soaked");
        }

        // Injury costs slots the following day.
        float severity = 1f + magnitudeRoll * 2f;
        return new AcuteEvent(AcuteEventKind.Injury, severity, "injury.minor");
    }
}
