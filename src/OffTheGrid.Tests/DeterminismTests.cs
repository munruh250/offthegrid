namespace OffTheGrid.Tests;

using Xunit;
using OffTheGrid.Data;
using OffTheGrid.Sim.Logging;

/// <summary>
/// These tests exist to catch silent determinism breaks. The PINNED values below
/// are not arbitrary — if one of them changes, saved replays and the cross-device
/// checksum comparison are both invalidated.
///
/// If a test here fails, do NOT update the expected value to match. Find out what
/// changed the draw order or the hash.
/// </summary>
public sealed class DeterminismTests
{
    [Fact]
    public void StableHashIsStableAcrossProcesses()
    {
        // The bug this guards against: string.GetHashCode() is randomised per
        // process in .NET Core, so anything seeded from it differs every launch.
        // These values were computed once and must never change.
        Assert.Equal(13225811171902394117UL, Rng.StableHash("weather"));
        Assert.Equal(10544339119461939354UL, Rng.StableHash("hunt"));
        Assert.Equal(14695981039346656037UL, Rng.StableHash(""));
    }

    [Fact]
    public void SameSeedProducesSameDraws()
    {
        var a = new Rng(12345);
        var b = new Rng(12345);

        for (int i = 0; i < 100; i++)
        {
            Assert.Equal(a.Stream("weather").Next(), b.Stream("weather").Next());
        }
    }

    [Fact]
    public void DifferentSeedsProduceDifferentDraws()
    {
        var a = new Rng(1);
        var b = new Rng(2);

        var drawsA = new uint[32];
        var drawsB = new uint[32];
        for (int i = 0; i < 32; i++)
        {
            drawsA[i] = a.Stream("weather").Next();
            drawsB[i] = b.Stream("weather").Next();
        }

        Assert.NotEqual(drawsA, drawsB);
    }

    [Fact]
    public void NamedStreamsAreIndependent()
    {
        // Drawing from one stream must not shift another. This is what makes it
        // safe to add a new RNG consumer without breaking existing replays.
        var control = new Rng(999);
        var expected = new uint[16];
        for (int i = 0; i < 16; i++) expected[i] = control.Stream("hunt").Next();

        var interleaved = new Rng(999);
        var actual = new uint[16];
        for (int i = 0; i < 16; i++)
        {
            interleaved.Stream("weather").Next();
            interleaved.Stream("morale").Next();
            actual[i] = interleaved.Stream("hunt").Next();
        }

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void StreamInstanceIsReusedPerName()
    {
        var rng = new Rng(7);
        Assert.Same(rng.Stream("hunt"), rng.Stream("hunt"));
    }

    [Fact]
    public void BoundedDrawStaysInRange()
    {
        var rng = new Rng(42);
        var stream = rng.Stream("test");
        for (int i = 0; i < 10_000; i++)
        {
            int v = stream.Next(6);
            Assert.InRange(v, 0, 5);
        }
    }

    [Fact]
    public void ChecksumIsOrderSensitive()
    {
        var a = new SimLog();
        a.LogEvent("hunt", "deer");
        a.LogEvent("weather", "rain");

        var b = new SimLog();
        b.LogEvent("weather", "rain");
        b.LogEvent("hunt", "deer");

        Assert.NotEqual(a.GetChecksum(), b.GetChecksum());
    }

    [Fact]
    public void IdenticalLogsProduceIdenticalChecksums()
    {
        var a = new SimLog();
        var b = new SimLog();
        foreach (var log in new[] { a, b })
        {
            log.LogEvent("hunt", "deer");
            log.LogEvent("weather", "rain");
            log.LogEvent("morale", "-1.0");
        }

        Assert.Equal(a.GetChecksum(), b.GetChecksum());
    }
}
