using System;
using System.Collections.Generic;
using OffTheGrid.Data;

namespace OffTheGrid.Sim.Logging;

/// <summary>
/// Logging interface for sim events.
/// Used for debugging, telemetry, and cross-device determinism verification.
/// Implementations should record every state-affecting decision.
/// </summary>
public interface ISimLog
{
    void LogEvent(string eventType, string details);

    /// <summary>
    /// Stable checksum of everything logged so far. Two runs of the same seed on
    /// different devices MUST produce the same value — this is the assertion the
    /// cross-device determinism test makes.
    /// </summary>
    ulong GetChecksum();
}

/// <summary>
/// Accumulating log with a rolling checksum. The checksum folds each event in as
/// it arrives, so it is order-sensitive by construction.
/// </summary>
public sealed class SimLog : ISimLog
{
    private readonly List<string> events = new();
    private ulong checksum = 14695981039346656037;

    public IReadOnlyList<string> Events => events;

    public void LogEvent(string eventType, string details)
    {
        var line = $"[{eventType}] {details}";
        events.Add(line);
        checksum ^= Rng.StableHash(line);
        checksum *= 1099511628211;
    }

    public ulong GetChecksum() => checksum;
}
