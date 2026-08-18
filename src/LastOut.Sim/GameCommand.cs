namespace LastOut.Sim;

/// <summary>
/// A command from the player or environment that mutates the game state.
/// Accumulated in a queue and processed by Simulation.Step().
/// </summary>
public abstract record GameCommand
{
    public sealed record PassTime(int Hours) : GameCommand;
    public sealed record PlayerInput(string Action) : GameCommand;
}
