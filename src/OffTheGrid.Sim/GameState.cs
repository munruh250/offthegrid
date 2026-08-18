namespace OffTheGrid.Sim;

/// <summary>
/// Immutable snapshot of the game state at a point in time.
/// The view reads this; the sim generates it.
/// </summary>
public readonly struct GameState
{
    public int DayNumber { get; init; }
    public int HourOfDay { get; init; }

    public GameState()
    {
        DayNumber = 1;
        HourOfDay = 0;
    }

    public override string ToString() => $"GameState(day={DayNumber} hour={HourOfDay})";
}
