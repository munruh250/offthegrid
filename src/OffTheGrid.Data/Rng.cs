using System;
using System.Collections.Generic;

namespace OffTheGrid.Data;

/// <summary>
/// Deterministic, cross-platform RNG using PCG32.
///
/// Two rules make this deterministic, and both are load-bearing:
///
///   1. Streams are seeded from (runSeed, streamName) via FNV-1a. NEVER
///      string.GetHashCode() — .NET Core randomises string hashing per process,
///      so GetHashCode-seeded streams differ on every launch.
///   2. This is an INSTANCE, not a static. A solver running many runs in
///      parallel needs independent RNG per run; shared static state would
///      cross-contaminate draws between runs.
///
/// Adding a new RNG consumer means adding a NAMED STREAM, not reusing one.
/// Reusing a stream shifts every downstream draw and breaks saved replays.
/// </summary>
public sealed class Rng(ulong runSeed)
{
    private readonly Dictionary<string, RngStream> streams = new(StringComparer.Ordinal);

    public ulong RunSeed { get; } = runSeed;

    /// <summary>
    /// Get a named RNG stream for this run. Same name returns the same instance.
    /// </summary>
    public RngStream Stream(string name)
    {
        if (!streams.TryGetValue(name, out var stream))
        {
            stream = new RngStream(name, StableHash(name) ^ RunSeed);
            streams[name] = stream;
        }
        return stream;
    }

    /// <summary>
    /// FNV-1a over UTF-16 code units. Stable across processes, runtimes and
    /// platforms — which is the entire point. Do not replace with GetHashCode.
    /// </summary>
    public static ulong StableHash(string s)
    {
        const ulong offsetBasis = 14695981039346656037;
        const ulong prime = 1099511628211;

        ulong hash = offsetBasis;
        foreach (char c in s)
        {
            hash ^= (byte)(c & 0xFF);
            hash *= prime;
            hash ^= (byte)(c >> 8);
            hash *= prime;
        }
        return hash;
    }

    public sealed class RngStream
    {
        private readonly string name;
        private ulong state;
        private readonly ulong inc;

        internal RngStream(string name, ulong seed)
        {
            this.name = name;
            inc = (seed << 1) | 1;
            state = 0;
            Next();
            state += seed;
            Next();
        }

        public uint Next()
        {
            ulong oldState = state;
            state = unchecked(oldState * 6364136223846793005ul + inc);
            uint xorShifted = (uint)(((oldState >> 18) ^ oldState) >> 27);
            int rot = (int)(oldState >> 59);
            return (xorShifted >> rot) | (xorShifted << ((-rot) & 31));
        }

        /// <summary>Uniform in [0, maxExclusive), rejection-sampled to avoid modulo bias.</summary>
        public int Next(int maxExclusive)
        {
            if (maxExclusive <= 0) throw new ArgumentOutOfRangeException(nameof(maxExclusive));

            uint bound = (uint)maxExclusive;
            uint threshold = (uint)(-(int)bound) % bound;
            while (true)
            {
                uint r = Next();
                if (r >= threshold) return (int)(r % bound);
            }
        }

        public float NextFloat() => Next() * (1.0f / 4294967296.0f);
        public bool NextBool() => (Next() & 1) == 0;

        public override string ToString() => $"Rng.Stream({name})";
    }
}
