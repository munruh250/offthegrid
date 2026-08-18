using System;

namespace OffTheGrid.Data.Balance;

/// <summary>
/// Indirection between the sim and its balance constants, so QA can re-inject
/// BalanceData mid-run without restarting. Resolves C10.
///
/// The sim reads <see cref="Current"/> at the START of each slot and holds that
/// reference for the whole slot. That is deliberate: a reload landing mid-slot
/// would apply different constants to different parts of one tick, which is both
/// a correctness bug and an unreproducible one.
///
/// Reloads are recorded in the run log, because a run whose constants changed
/// halfway through is not a valid balance sample and the solver must be able to
/// discard it.
/// </summary>
public sealed class BalanceProvider(BalanceData initial)
{
    private BalanceData current = initial;

    public BalanceData Current => current;

    /// <summary>Incremented on every reload. Non-zero means this run is not a clean sample.</summary>
    public int ReloadCount { get; private set; }

    /// <summary>
    /// Raised after a reload lands. The sim subscribes to log it; the editor
    /// tuning inspector subscribes to refresh its display.
    /// </summary>
    public event Action<BalanceData>? Reloaded;

    public BalanceProvider() : this(BalanceData.Default) { }

    /// <summary>
    /// Swap the constants. Editor and test paths only — there is no shipping
    /// code path that calls this, and a released build never reloads.
    /// </summary>
    public void Reload(BalanceData replacement)
    {
        ArgumentNullException.ThrowIfNull(replacement);
        current = replacement;
        ReloadCount++;
        Reloaded?.Invoke(replacement);
    }

    /// <summary>True if this run's constants never changed, so it is a valid balance sample.</summary>
    public bool IsCleanSample => ReloadCount == 0;
}
