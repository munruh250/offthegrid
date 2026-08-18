namespace LastOut.Sim;

/// <summary>
/// Deterministic simulation loop.
/// Accepts commands, produces immutable snapshots.
/// </summary>
public sealed class Simulation
{
    private GameState state = new();

    public GameState CurrentState => state;

    /// <summary>
    /// Advance the simulation by one step, applying the given command.
    /// Returns the new state (immutable snapshot).
    /// </summary>
    public GameState Step(GameCommand cmd)
    {
        return cmd switch
        {
            GameCommand.PassTime(int hours) => StepPassTime(hours),
            GameCommand.PlayerInput(string action) => StepPlayerInput(action),
            _ => state
        };
    }

    private GameState StepPassTime(int hours)
    {
        state = state with
        {
            DayNumber = state.DayNumber + (state.HourOfDay + hours) / 24,
            HourOfDay = (state.HourOfDay + hours) % 24
        };
        return state;
    }

    private GameState StepPlayerInput(string action)
    {
        // Placeholder: input handling will go here
        return state;
    }
}
