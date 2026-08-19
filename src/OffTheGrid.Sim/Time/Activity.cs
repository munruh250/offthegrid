using System;

namespace OffTheGrid.Sim.Time;

/// <summary>
/// What a slot can be spent on, with its metabolic cost. Design spec 5.2.
///
/// Movement-type activities additionally scale by (W/80)^1.15, which is the
/// anti-fasting-build lever: a heavy body pays superlinearly to move. Note that
/// balance doc 7.2 found this is NOT sufficient on its own - morale's idleness
/// penalty is what actually defeats the fasting strategy.
/// </summary>
public enum Activity
{
    Sleep,
    Rest,
    WhittleComfortProject,
    DryGearAtFire,
    Fishing,
    Foraging,
    TrapLine,
    HuntingStalk,
    ShelterBuild,
    Sawing,
    Exploring,
    ChoppingWood,
    HaulingLogs,

    /// <summary>Boil bone for marrow fat. The protein-free fat path (B3).</summary>
    RenderMarrow,

    /// <summary>Smoke, dry or pack away raw food before it turns.</summary>
    PreserveFood,

    /// <summary>Build a cache, rack or cold store.</summary>
    BuildCamp
}

public static class ActivityExtensions
{
    /// <summary>Metabolic equivalent. Design spec 5.2 table.</summary>
    public static float Met(this Activity activity) => activity switch
    {
        Activity.Sleep => 0.95f,
        Activity.Rest => 1.2f,
        Activity.DryGearAtFire => 1.5f,
        Activity.WhittleComfortProject => 2.0f,
        Activity.Fishing => 3.0f,
        Activity.Foraging => 3.5f,
        Activity.TrapLine => 4.0f,
        Activity.HuntingStalk => 4.5f,
        Activity.ShelterBuild => 5.0f,
        Activity.Sawing => 5.5f,
        Activity.Exploring => 6.0f,
        Activity.ChoppingWood => 6.3f,
        Activity.HaulingLogs => 8.0f,
        Activity.RenderMarrow => 2.2f,
        Activity.PreserveFood => 2.6f,
        Activity.BuildCamp => 4.2f,
        _ => throw new ArgumentOutOfRangeException(nameof(activity))
    };

    /// <summary>
    /// Movement-type activities pay the superlinear mass penalty. Design spec 5.2.
    /// </summary>
    public static bool IsMovement(this Activity activity) => activity switch
    {
        Activity.Foraging => true,
        Activity.TrapLine => true,
        Activity.HuntingStalk => true,
        Activity.Exploring => true,
        Activity.HaulingLogs => true,
        _ => false
    };

    /// <summary>
    /// Whether this counts as build or craft progress for the morale idleness
    /// check. Design spec 5.6 penalises "no build/craft progress" - resting and
    /// foraging do not clear it, building and whittling do.
    /// </summary>
    public static bool IsBuildProgress(this Activity activity) => activity switch
    {
        Activity.WhittleComfortProject => true,
        Activity.ShelterBuild => true,
        Activity.Sawing => true,
        Activity.ChoppingWood => true,
        Activity.RenderMarrow => true,
        Activity.PreserveFood => true,
        Activity.BuildCamp => true,
        _ => false
    };
}
