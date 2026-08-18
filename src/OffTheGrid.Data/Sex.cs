namespace OffTheGrid.Data;

/// <summary>
/// Biological sex, which the body model needs because Mifflin-St Jeor, essential
/// body-fat floors, and the medical-pull thresholds all differ (design spec 4.3,
/// 5.1, 6). Resolves Q12.
///
/// This is a physiological parameter, not a character-identity one. If the game
/// later offers gender presentation as a separate cosmetic/narrative choice, it
/// belongs in its own type — do not overload this one.
/// </summary>
public enum Sex
{
    Male,
    Female
}

public static class SexExtensions
{
    /// <summary>Mifflin-St Jeor constant term. Design spec 5.1.</summary>
    public static float BmrConstant(this Sex sex) => sex switch
    {
        Sex.Male => 5f,
        Sex.Female => -161f,
        _ => 5f
    };

    /// <summary>
    /// Body-fat percentage below which the medic pulls you. Design spec 6.
    /// </summary>
    public static float MedicalPullBodyFatPercent(this Sex sex) => sex switch
    {
        Sex.Male => 6f,
        Sex.Female => 12f,
        _ => 6f
    };

    /// <summary>
    /// Lowest body-fat percentage selectable at character creation.
    /// Design spec 4.3 — "BF% 8-45, sex-gated floors".
    /// </summary>
    public static float CharacterCreationBodyFatFloor(this Sex sex) => sex switch
    {
        Sex.Male => 8f,
        Sex.Female => 14f,
        _ => 8f
    };
}
