namespace OffTheGrid.Tests;

using Xunit;
using OffTheGrid.Data;
using OffTheGrid.Data.Balance;
using OffTheGrid.Sim.Body;

/// <summary>
/// Pins the body model against balance doc 7.1's validation curve — the curve
/// BalanceAssert will eventually be written against, so the deficit partition
/// has to reproduce it.
/// </summary>
public sealed class BalanceCurveTests
{
    // Doc 7.1: 85 kg / 20% BF, competent play, day 60 at 65.6 kg / 11.6% BF.
    private const float FatLostKg = 17.0f - 7.6f;
    private const float LeanLostKg = 68.0f - 58.0f;

    private static float TotalRunDeficit(BalanceData b) =>
        FatLostKg * b.KcalPerKgFat + LeanLostKg * b.KcalPerKgLeanTissue;

    [Fact]
    public void DeficitPartitionReproducesDoc71Curve()
    {
        var balance = BalanceData.Default;
        var body = new BodyState(Sex.Male, heightCm: 180, ageYears: 35, weightKg: 85, bodyFatPercent: 20);

        body.ApplyEnergyBalance(-TotalRunDeficit(balance), balance);

        Assert.Equal(65.6f, body.WeightKg, 1.5f);
        Assert.Equal(11.6f, body.BodyFatPercent, 1.5f);
    }

    [Fact]
    public void PartitionConstantMatchesDocDerivation()
    {
        // 88/12 by energy, which reads as 48/52 by mass because lean tissue is
        // ~1020 kcal/kg against fat's 7700.
        Assert.Equal(0.88f, BalanceData.Default.DeficitFractionFromFat, 0.005f);
    }

    [Fact]
    public void CompetentPlayerStaysAboveMedicalPullAtDay60()
    {
        // Doc 7.1: "comfortably inside all medical pull thresholds".
        var balance = BalanceData.Default;
        var body = new BodyState(Sex.Male, 180, 35, 85, 20);

        body.ApplyEnergyBalance(-TotalRunDeficit(balance), balance);

        Assert.True(body.BodyFatPercent > Sex.Male.MedicalPullBodyFatPercent(),
            $"BF {body.BodyFatPercent:F1}% must clear the {Sex.Male.MedicalPullBodyFatPercent()}% pull threshold");
        Assert.True(body.WeightLossFraction < balance.MedicalPullMaxWeightLossFraction,
            $"weight loss {body.WeightLossFraction:P0} must stay under {balance.MedicalPullMaxWeightLossFraction:P0}");
        Assert.True(body.Bmi > balance.MedicalPullMinBmi,
            $"BMI {body.Bmi:F1} must clear {balance.MedicalPullMinBmi}");
    }

    [Fact]
    public void FemaleHitsPullThresholdEarlierThanMale()
    {
        // Why Q12 blocks: the same deficit lands differently, because the pull
        // threshold is 12% for female against 6% for male.
        var balance = BalanceData.Default;
        var male = new BodyState(Sex.Male, 180, 35, 85, 20);
        var female = new BodyState(Sex.Female, 180, 35, 85, 20);

        float deficit = -TotalRunDeficit(balance);
        male.ApplyEnergyBalance(deficit, balance);
        female.ApplyEnergyBalance(deficit, balance);

        Assert.True(male.BodyFatPercent > Sex.Male.MedicalPullBodyFatPercent());
        Assert.True(female.BodyFatPercent < Sex.Female.MedicalPullBodyFatPercent(),
            "identical deficit puts a female body under its pull threshold - the run is not symmetric");
    }
}
