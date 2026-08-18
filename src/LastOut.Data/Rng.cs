using System;
using System.Collections.Generic;

namespace LastOut.Data;

/// <summary>
/// Deterministic, cross-platform RNG using PCG32.
/// Named streams ensure determinism across platforms and saved replays.
/// </summary>
public sealed class Rng
{
    public sealed class RngStream
    {
        private readonly string name;
        private ulong state = 0x853c49e6748fea9b;
        private ulong inc = 0xda3e39cb94b95bdb;

        internal RngStream(string name)
        {
            this.name = name;
            // Seed from name hash to ensure reproducibility
            var hash = (ulong)name.GetHashCode();
            state = state ^ hash;
            inc = (inc + hash) | 1;
        }

        public uint Next()
        {
            ulong oldState = state;
            state = unchecked(oldState * 6364136223846793005ul + inc);
            uint xorShifted = (uint)(((oldState >> 18) ^ oldState) >> 27);
            int rot = (int)(oldState >> 59);
            return (xorShifted >> rot) | (xorShifted << (32 - rot));
        }

        public int Next(int maxExclusive) => (int)(Next() % (uint)maxExclusive);
        public float NextFloat() => Next() * (1.0f / 4294967296.0f);
        public bool NextBool() => (Next() & 1) == 0;

        public override string ToString() => $"Rng.Stream({name})";
    }

    private static readonly Dictionary<string, RngStream> streams = new();

    /// <summary>
    /// Get a named RNG stream. Returns the same instance on subsequent calls with the same name.
    /// Adding a new stream name means a new RNG consumer; reusing a name shifts all downstream draws.
    /// </summary>
    public static RngStream Stream(string name)
    {
        if (!streams.TryGetValue(name, out var stream))
        {
            stream = new RngStream(name);
            streams[name] = stream;
        }
        return stream;
    }

    public static void ResetAll() => streams.Clear();
}
