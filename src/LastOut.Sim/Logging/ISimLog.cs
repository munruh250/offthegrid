using System;
using System.Collections.Generic;

namespace LastOut.Sim.Logging;

/// <summary>
/// Logging interface for sim events.
/// Used for debugging, telemetry, and cross-device determinism verification.
/// Implementations should serialize every state-affecting decision.
/// </summary>
public interface ISimLog
{
    void LogEvent(string eventType, string details);
    string GetChecksum();
}

public sealed class ConsoleSimLog : ISimLog
{
    private readonly List<string> events = new();

    public void LogEvent(string eventType, string details)
    {
        events.Add($"[{eventType}] {details}");
    }

    public string GetChecksum()
    {
        // Simple checksum for now; use SHA256 in real implementation
        var combined = string.Concat(events);
        return combined.GetHashCode().ToString("X8");
    }
}
