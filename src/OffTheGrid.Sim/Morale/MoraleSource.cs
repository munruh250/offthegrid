namespace OffTheGrid.Sim.Morale;

/// <summary>
/// Every distinct reason morale can move. Design spec 5.6.
///
/// This enum is the attribution vocabulary. Design spec 5.6.1 sets the rule:
/// "the player is never told morale dropped without being told why." Every
/// contribution carries one of these, so the breakdown is always reconstructable
/// — and the same data serves the live HUD, the day summary card, and the
/// end-of-run cause-of-death analysis. One vocabulary, three surfaces.
///
/// Adding a source means adding a member here, not folding it into an existing
/// one. A modifier with no name of its own is invisible to the player.
/// </summary>
public enum MoraleSource
{
    // Losses
    BaseDecay,
    FoodInsecure,
    ShelterInadequate,
    Idleness,
    WeightLoss,
    SoakedAtSleep,
    MemoryEvent,
    ShelterLost,

    // Gains
    ComfortProject,
    LargeFoodSuccess,
    ShelterMilestone,
    BeachcombFind,
    Photo
}

public static class MoraleSourceExtensions
{
    /// <summary>
    /// Player-facing label. Kept next to the enum so a new source cannot ship
    /// without one — an unlabelled modifier would appear in the breakdown as a
    /// blank row, which is worse than not showing it.
    /// </summary>
    public static string Label(this MoraleSource source) => source switch
    {
        MoraleSource.BaseDecay => "Time out here",
        MoraleSource.FoodInsecure => "Nothing put by",
        MoraleSource.ShelterInadequate => "Shelter too cold",
        MoraleSource.Idleness => "Nothing built",
        MoraleSource.WeightLoss => "Weight lost",
        MoraleSource.SoakedAtSleep => "Slept wet",
        MoraleSource.MemoryEvent => "Thinking of home",
        MoraleSource.ShelterLost => "Left camp behind",
        MoraleSource.ComfortProject => "Finished a project",
        MoraleSource.LargeFoodSuccess => "Good kill",
        MoraleSource.ShelterMilestone => "Shelter improved",
        MoraleSource.BeachcombFind => "Beach find",
        MoraleSource.Photo => "Photograph",
        _ => source.ToString()
    };
}
