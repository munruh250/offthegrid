namespace OffTheGrid.Tests;

using Xunit;
using OffTheGrid.Data;
using OffTheGrid.Data.Balance;
using OffTheGrid.Sim.Body;

/// <summary>
/// Guards the property that closed the skinny-fat exploit: lean mass has to BUY
/// something, or minimising muscle while maximising fat is a free win.
/// </summary>
public sealed class BodyCapacityTests
{
    [Fact]
    public void LeanMassBuysPhysicalCapacity()
    {
        var skinnyFat = new BodyState(Sex.Female, 178, 35, 79, 34);   // ~52 kg lean
        var lean = new BodyState(Sex.Male, 178, 35, 76, 15);          // ~65 kg lean

        Assert.True(lean.PhysicalCapacity > skinnyFat.PhysicalCapacity,
            "a body with more lean mass must be able to do more work");
    }

    [Fact]
    public void TheFattestBodyIsNotAutomaticallyTheMostCapable()
    {
        var heavy = new BodyState(Sex.Male, 178, 35, 104, 33);
        var moderate = new BodyState(Sex.Male, 178, 35, 85, 21);

        // Heavy carries more lean in absolute terms, so it IS more capable - but
        // it also pays far more in BMR and movement. Capability must not simply
        // track total mass.
        Assert.True(heavy.PhysicalCapacity >= moderate.PhysicalCapacity);
        Assert.True(heavy.BasalMetabolicRate > moderate.BasalMetabolicRate + 150f);
    }

    [Fact]
    public void CapacityDecaysAsTheBodyWastes()
    {
        // The body-failing thesis applied to what you can still get DONE, which
        // is the one place it was missing.
        var body = new BodyState(Sex.Male, 178, 35, 85, 22);
        float before = body.PhysicalCapacity;

        for (int i = 0; i < 60; i++) body.ApplyEnergyBalance(-1900f, BalanceData.Default);

        Assert.True(body.PhysicalCapacity < before,
            "a wasted body must be less capable than a fresh one");
    }

    [Fact]
    public void CapacityIsBounded()
    {
        var tiny = new BodyState(Sex.Female, 150, 35, 45, 30);
        var huge = new BodyState(Sex.Male, 200, 35, 140, 20);

        Assert.InRange(tiny.PhysicalCapacity, 0.65f, 1.25f);
        Assert.InRange(huge.PhysicalCapacity, 0.65f, 1.25f);
    }

    [Fact]
    public void SnapshotCarriesCapacity()
    {
        var body = new BodyState(Sex.Male, 178, 35, 85, 22);
        Assert.Equal(body.PhysicalCapacity, body.Snapshot().PhysicalCapacity, 0.001f);
    }
}
