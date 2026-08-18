using System;
using System.Collections.Generic;

namespace OffTheGrid.Sim.Record;

/// <summary>One recorded decision or state-affecting event.</summary>
public readonly record struct TraceEntry
{
    public int Day { get; init; }
    public int Slot { get; init; }
    public TraceKind Kind { get; init; }

    /// <summary>Short stable identifier, e.g. "hunt.deer.miss". Not display text.</summary>
    public string Code { get; init; }

    /// <summary>Signed magnitude where one applies (morale delta, kcal, kg). Otherwise 0.</summary>
    public float Magnitude { get; init; }
}

public enum TraceKind
{
    Action,
    MoraleEvent,
    NutritionEvent,
    WeatherEvent,
    Injury,
    Relocation
}

/// <summary>
/// Fixed-capacity rolling trace of recent decisions, for cause-of-death
/// attribution. Resolves C11.
///
/// C11 asked two questions and both are answered here:
///
///   MEMORY BOUND. Capacity is fixed at construction and never grows. At 5 slots
///   per day over a 20-day window the worst case is ~100 action entries plus
///   events, so the default 256 covers it with headroom. When full, the oldest
///   entry is overwritten — the trace degrades by forgetting old days, which is
///   exactly the desired behaviour for a 20-day rolling window.
///
///   SAVE/RESTORE. The buffer serialises with the run. This matters more than it
///   looks: on mobile essentially every session is backgrounded at some point,
///   so a trace that did not persist would make cause-of-death analysis wrong for
///   most real runs — and that analysis is what the >=70% self-identification
///   gate (design spec 5.6.3 / D5) is measured against.
/// </summary>
public sealed class DecisionTrace
{
    public const int DefaultCapacity = 256;

    private readonly TraceEntry[] buffer;
    private int head;

    public DecisionTrace(int capacity = DefaultCapacity)
    {
        if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
        buffer = new TraceEntry[capacity];
    }

    public int Capacity => buffer.Length;

    /// <summary>Entries currently held, never more than <see cref="Capacity"/>.</summary>
    public int Count { get; private set; }

    /// <summary>True once the trace has started discarding its oldest entries.</summary>
    public bool HasOverflowed { get; private set; }

    public void Add(TraceEntry entry)
    {
        buffer[head] = entry;
        head = (head + 1) % buffer.Length;

        if (Count < buffer.Length) Count++;
        else HasOverflowed = true;
    }

    /// <summary>Entries oldest-first. Allocates, so this is an analysis path, not a per-slot one.</summary>
    public TraceEntry[] ToOrderedList()
    {
        var result = new TraceEntry[Count];
        int start = Count < buffer.Length ? 0 : head;
        for (int i = 0; i < Count; i++)
        {
            result[i] = buffer[(start + i) % buffer.Length];
        }
        return result;
    }

    /// <summary>Entries from the last <paramref name="days"/> days up to and including <paramref name="currentDay"/>.</summary>
    public IReadOnlyList<TraceEntry> RecentDays(int currentDay, int days)
    {
        int cutoff = currentDay - days + 1;
        var all = ToOrderedList();
        var result = new List<TraceEntry>();
        foreach (var e in all)
        {
            if (e.Day >= cutoff) result.Add(e);
        }
        return result;
    }

    /// <summary>Flat state for serialisation. Order-preserving, so a restored trace replays identically.</summary>
    public TraceEntry[] ToSerialisable() => ToOrderedList();

    public static DecisionTrace FromSerialisable(IReadOnlyList<TraceEntry> entries, int capacity = DefaultCapacity)
    {
        var trace = new DecisionTrace(capacity);
        foreach (var e in entries) trace.Add(e);
        return trace;
    }
}
