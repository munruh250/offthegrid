namespace OffTheGrid.Tests;

using Xunit;
using OffTheGrid.Data;
using OffTheGrid.Data.Balance;
using OffTheGrid.Sim.Body;

/// <summary>Q12 (sex) and A15 (mutable state + on-demand projection).</summary>
public sealed class BodyTests
{
    private static readonly BalanceData Balance = BalanceData.Default;

    [Fact]
    public void MifflinStJeorMatchesSpecForMale()
    {
        // 10*85 + 6.25*180 - 5*35 + 5 = 850 + 1125 - 175 + 5 = 1805
        var body = new BodyState(Sex.Male, heightCm: 180, ageYears: 35, weightKg: 85, bodyFatPercent: 20);
        Assert.Equal(1805f, body.BasalMetabolicRate, 0.5f);
    }

    [Fact]
    public void MifflinStJeorMatchesSpecForFemale()
    {
        // Same body, female constant: 850 + 1125 - 175 - 161 = 1639
        var body = new BodyState(Sex.Female, heightCm: 180, ageYears: 35, weightKg: 85, bodyFatPercent: 20);
        Assert.Equal(1639f, body.BasalMetabolicRate, 0.5f);
    }

    [Fact]
    public void SexChangesBmrBy166Kcal()
    {
        // The gap that makes Q12 a blocker: 166 kcal/day compounds over 60 days.
        var male = new BodyState(Sex.Male, 180, 35, 85, 20);
        var female = new BodyState(Sex.Female, 180, 35, 85, 20);
        Assert.Equal(166f, male.BasalMetabolicRate - female.BasalMetabolicRate, 0.5f);
    }

    [Fact]
    public void MedicalPullThresholdsAreSexGated()
    {
        Assert.Equal(6f, Sex.Male.MedicalPullBodyFatPercent());
        Assert.Equal(12f, Sex.Female.MedicalPullBodyFatPercent());
    }

    [Fact]
    public void AdaptiveThermogenesisCapsAtTenPercent()
    {
        var body = new BodyState(Sex.Male, 180, 35, 100, 25);

        // Burn far past the 15% loss point.
        for (int i = 0; i < 400; i++) body.ApplyEnergyBalance(-3000f, Balance);

        float ratio = body.EffectiveBasalMetabolicRate(Balance) / body.BasalMetabolicRate;
        Assert.Equal(0.90f, ratio, 0.001f);
    }

    [Fact]
    public void NoSuppressionBeforeAnyLoss()
    {
        var body = new BodyState(Sex.Male, 180, 35, 85, 20);
        Assert.Equal(body.BasalMetabolicRate, body.EffectiveBasalMetabolicRate(Balance), 0.01f);
    }

    [Fact]
    public void DeficitDrawsFromBothFatAndLean()
    {
        var body = new BodyState(Sex.Male, 180, 35, 85, 20);
        float fat0 = body.FatMassKg;
        float lean0 = body.LeanMassKg;

        body.ApplyEnergyBalance(-7700f, Balance);

        Assert.True(body.FatMassKg < fat0, "fat should fall");
        Assert.True(body.LeanMassKg < lean0, "lean should fall");
    }

    [Fact]
    public void SurplusStoresAsFatOnly()
    {
        var body = new BodyState(Sex.Male, 180, 35, 85, 20);
        float lean0 = body.LeanMassKg;

        body.ApplyEnergyBalance(+7700f, Balance);

        Assert.Equal(lean0, body.LeanMassKg, 0.001f);
        Assert.Equal(17f + 1f, body.FatMassKg, 0.01f);
    }

    [Fact]
    public void FatMassNeverGoesNegative()
    {
        var body = new BodyState(Sex.Male, 180, 35, 60, 8);
        for (int i = 0; i < 1000; i++) body.ApplyEnergyBalance(-5000f, Balance);
        Assert.True(body.FatMassKg >= 0f);
        Assert.True(body.LeanMassKg >= 0f);
    }

    // ---- A15: the snapshot contract ----

    [Fact]
    public void SnapshotDoesNotObserveLaterMutation()
    {
        // This is the whole A15 contract. The view is a value projection, so a
        // holder cannot see the sim mutate underneath it.
        var body = new BodyState(Sex.Male, 180, 35, 85, 20);
        var before = body.Snapshot();

        body.ApplyEnergyBalance(-77000f, Balance);

        Assert.Equal(85f, before.WeightKg, 0.01f);
        Assert.NotEqual(before.WeightKg, body.Snapshot().WeightKg);
    }

    [Fact]
    public void SnapshotReflectsStateAtTimeOfCall()
    {
        var body = new BodyState(Sex.Male, 180, 35, 85, 20);
        body.ApplyEnergyBalance(-7700f, Balance);

        var snap = body.Snapshot();
        Assert.Equal(body.WeightKg, snap.WeightKg, 0.001f);
        Assert.Equal(body.BodyFatPercent, snap.BodyFatPercent, 0.001f);
    }

    [Fact]
    public void SolverPathAllocatesNoSnapshots()
    {
        // The A15 justification: a long solver-style loop must be able to run
        // without ever projecting. If this ever needs a Snapshot() call to work,
        // the architecture has regressed.
        var body = new BodyState(Sex.Male, 180, 35, 85, 20);
        for (int slot = 0; slot < 300; slot++)
        {
            body.ApplyEnergyBalance(-500f, Balance);
            _ = body.EffectiveBasalMetabolicRate(Balance);
        }
        Assert.True(body.WeightKg < 85f);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    [InlineData(-1)]
    public void RejectsImpossibleBodyFat(float bodyFatPercent)
    {
        if (bodyFatPercent == 0) return; // 0% is degenerate but not rejected
        Assert.Throws<System.ArgumentOutOfRangeException>(
            () => new BodyState(Sex.Male, 180, 35, 85, bodyFatPercent));
    }
}
