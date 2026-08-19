using System;
using System.Collections.Generic;
using OffTheGrid.Data.Gear;
using OffTheGrid.Sim.Time;

namespace OffTheGrid.Sim.Contest;

/// <summary>
/// How a contestant plays. Design spec 11: rivals carry a personality bias, and
/// it is the thing that stops nine simulations being statistical noise.
/// </summary>
public enum Personality
{
    /// <summary>Chases big game. Feasts and starves.</summary>
    AggressiveHunter,

    /// <summary>Shelter and camp first, food second. Slow, hard to kill by cold.</summary>
    PatientBuilder,

    /// <summary>Minimises exertion, banks what it finds, coasts.</summary>
    ConservativeRester,

    /// <summary>Works the water and the shoreline. Steady, unspectacular.</summary>
    SteadyProvider
}

/// <summary>
/// A rival's daily decision-making, as needs scored and weighted by temperament.
///
/// Deliberately simple - design spec 11 puts rivals at "lower fidelity, resolved
/// at attribute-expected values." They run the SAME physics as the player (B8);
/// what differs is that they never play a minigame, so they land on expected
/// value while a skilled player beats it and a poor one falls below. That gap is
/// what the contest is actually measuring.
///
/// The same scoring code produces visibly different lives, because the weights
/// differ: the aggressive hunter takes a shot the patient builder would decline.
/// </summary>
public static class RivalPolicy
{
    /// <summary>Choose today's slots for a rival.</summary>
    public static Activity[] PlanDay(Run run, Personality personality, int slots)
    {
        var balance = OffTheGrid.Data.Balance.BalanceData.Default;

        // --- read the pressures ---
        float foodDays = run.Larder.DaysOfFood(run.Body.WeightKg, balance);
        bool hungry = foodDays < 2f;
        bool starving = foodDays < 0.5f;

        float warmth = run.AvailableClo + Fire.Firewood.FireClo(run.LastFireQuality);
        float coldGap = run.CloDemandTonight(run.DayNumber + 1) - warmth;
        bool cold = coldGap > 0f;
        bool freezing = coldGap > 0.8f;

        bool lowMorale = run.Morale.Current < 40f;
        bool woodShort = run.WoodKg < Fire.Firewood.NightlyDemandKg(
            run.NightTempForDay(run.DayNumber + 1), run.Shelter);

        // --- score the options ---
        var scores = new Dictionary<Activity, float>
        {
            [Activity.Fishing] = 10f,
            [Activity.TrapLine] = 9f,
            [Activity.HuntingStalk] = 6f,
            [Activity.Foraging] = 7f,
            [Activity.ShelterBuild] = 5f,
            [Activity.ChoppingWood] = 4f,
            [Activity.WhittleComfortProject] = 5f,
            [Activity.RenderMarrow] = 3f,
            [Activity.Rest] = 2f,
        };

        void Boost(Activity a, float by) => scores[a] = scores.GetValueOrDefault(a) + by;

        if (hungry) { Boost(Activity.Fishing, 8); Boost(Activity.TrapLine, 8); Boost(Activity.Foraging, 6); Boost(Activity.HuntingStalk, 4); }
        if (starving) { Boost(Activity.Fishing, 10); Boost(Activity.TrapLine, 10); Boost(Activity.Foraging, 8); }
        if (cold) { Boost(Activity.ShelterBuild, 7); Boost(Activity.ChoppingWood, 6); }
        if (freezing) { Boost(Activity.ShelterBuild, 10); Boost(Activity.ChoppingWood, 10); }
        if (woodShort) Boost(Activity.ChoppingWood, 8);
        if (lowMorale) Boost(Activity.WhittleComfortProject, 9);
        if (run.Larder.BoneKg > 5f) Boost(Activity.RenderMarrow, 5);

        // --- temperament ---
        switch (personality)
        {
            case Personality.AggressiveHunter:
                Boost(Activity.HuntingStalk, 9);
                Boost(Activity.Rest, -2);
                Boost(Activity.ShelterBuild, -2);
                break;
            case Personality.PatientBuilder:
                Boost(Activity.ShelterBuild, 7);
                Boost(Activity.ChoppingWood, 4);
                Boost(Activity.WhittleComfortProject, 3);
                Boost(Activity.HuntingStalk, -3);
                break;
            case Personality.ConservativeRester:
                Boost(Activity.Rest, 7);
                Boost(Activity.TrapLine, 3);          // passive income suits them
                Boost(Activity.HuntingStalk, -4);
                Boost(Activity.Exploring, -5);
                break;
            case Personality.SteadyProvider:
                Boost(Activity.Fishing, 4);
                Boost(Activity.Foraging, 3);
                Boost(Activity.TrapLine, 2);
                break;
        }

        // Can't do what the kit won't allow.
        if (!GearEffects.CanPerform(run.Gear, ActivityRequirement.Fishing)) scores[Activity.Fishing] = -99f;
        if (!GearEffects.CanPerform(run.Gear, ActivityRequirement.Trapping)) scores[Activity.TrapLine] = -99f;
        if (!GearEffects.CanPerform(run.Gear, ActivityRequirement.BigGame)) scores[Activity.HuntingStalk] -= 6f;
        if (!GearEffects.CanPerform(run.Gear, ActivityRequirement.Rendering)) scores[Activity.RenderMarrow] = -99f;

        // Don't push movement work while about to black out.
        if (run.BodyConditionRatio < Run.CollapseConditionThreshold)
        {
            foreach (var a in new[] { Activity.HuntingStalk, Activity.Foraging, Activity.Exploring, Activity.TrapLine })
                if (scores.ContainsKey(a)) scores[a] -= 12f;
            Boost(Activity.Rest, 8);
        }

        // --- take the best N, allowing repeats of the single best food slot ---
        var ordered = new List<KeyValuePair<Activity, float>>(scores);
        ordered.Sort((x, y) => y.Value.CompareTo(x.Value));

        var plan = new Activity[slots];
        for (int i = 0; i < slots; i++)
            plan[i] = ordered[Math.Min(i, ordered.Count - 1)].Key;

        return plan;
    }
}
