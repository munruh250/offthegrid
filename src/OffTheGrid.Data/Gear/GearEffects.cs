using System;
using OffTheGrid.Data.Tables;

namespace OffTheGrid.Data.Gear;

/// <summary>
/// How gear gates and multiplies activities.
///
/// Two kinds of effect, deliberately:
///   GATES are hard. No bow means no big game - not "big game at a penalty".
///     That is what makes a loadout a real commitment rather than a stat spread.
///   MULTIPLIERS are soft. A gillnet does not unlock fishing, it transforms it.
///
/// Keeping both is what stops the ten items collapsing into "take the ten best".
/// </summary>
public static class GearEffects
{
    /// <summary>Can this activity be attempted at all with this kit?</summary>
    public static bool CanPerform(Loadout gear, ActivityRequirement requirement) => requirement switch
    {
        ActivityRequirement.Fishing =>
            gear.Has(GearItem.Gillnet) || gear.Has(GearItem.FishingLineAndHooks),

        ActivityRequirement.Trapping =>
            gear.Has(GearItem.SnareWire) || gear.Has(GearItem.Paracord),

        // Foraging and small-game opportunism need nothing.
        ActivityRequirement.Foraging => true,
        ActivityRequirement.SmallGame => true,

        // Big game genuinely requires a weapon.
        ActivityRequirement.BigGame => gear.Has(GearItem.BowAndArrows),

        // Boiling bone needs something to boil it in.
        ActivityRequirement.Rendering => gear.Has(GearItem.Pot),

        ActivityRequirement.Chopping => gear.Has(GearItem.Axe),

        _ => true
    };

    /// <summary>Yield multiplier from the kit carried, once the activity is possible.</summary>
    public static float YieldMultiplier(Loadout gear, ActivityRequirement requirement) => requirement switch
    {
        // A net fishes while you do something else - expressed as a second draw
        // per slot (see Harvest.DrawsPerSlot), NOT as a yield multiplier on top.
        // Stacking both made a gillnet worth 3.5x a line, which is more than any
        // single item should be.
        ActivityRequirement.Fishing => 1.0f,

        // Improvised cordage snares work, badly.
        ActivityRequirement.Trapping =>
            gear.Has(GearItem.SnareWire) ? 1.0f : 0.55f,

        // A knife makes butchering and processing less wasteful.
        ActivityRequirement.Foraging =>
            gear.Has(GearItem.Knife) ? 1.15f : 1.0f,

        _ => 1.0f
    };

    /// <summary>
    /// Highest shelter tier this kit can build. Balance doc 5 has the log shelter
    /// needing an axe AND a saw; without either you are on debris huts.
    /// </summary>
    public static ShelterTier MaxShelterTier(Loadout gear)
    {
        if (gear.Has(GearItem.Axe) && gear.Has(GearItem.Saw)) return ShelterTier.LogCabin;
        if (gear.Has(GearItem.Axe)) return ShelterTier.ReflectorWallCamp;
        if (gear.Has(GearItem.Tarp)) return ShelterTier.DebrisHut;
        return ShelterTier.DebrisHut;
    }

    /// <summary>A ferro rod makes fire reliable; without one it costs slots and sometimes fails.</summary>
    public static float FireReliability(Loadout gear) =>
        gear.Has(GearItem.FerroRod) ? 1.0f : 0.65f;
}

/// <summary>What an activity needs from the kit.</summary>
public enum ActivityRequirement
{
    None,
    Fishing,
    Trapping,
    Foraging,
    SmallGame,
    BigGame,
    Rendering,
    Chopping
}
