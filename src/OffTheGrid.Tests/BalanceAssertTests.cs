namespace OffTheGrid.Tests;

using System.Linq;
using Xunit;
using Xunit.Abstractions;
using OffTheGrid.Sim.Balance;

public sealed class BalanceAssertTests(ITestOutputHelper output)
{
    [Fact]
    public void EveryBalanceCheckPasses()
    {
        var results = BalanceAssert.RunAll();
        foreach (var r in results) output.WriteLine(r.ToString());

        var failures = results.Where(r => !r.Passed).ToArray();
        Assert.True(failures.Length == 0,
            "balance checks failed:\n" + string.Join("\n", failures.Select(f => f.ToString())));
    }

    [Fact]
    public void FastingBuildLosesToCompetentPlay()
    {
        // Q2. Do not weaken this. It took an entire morale system to achieve.
        var result = BalanceAssert.FastingBuildLosesToCompetentPlay();
        output.WriteLine(result.ToString());
        Assert.True(result.Passed, result.Detail);
    }

    [Fact]
    public void SuiteCoversTheLoadBearingProperties()
    {
        var names = BalanceAssert.RunAll().Select(r => r.Name).ToArray();

        Assert.Contains(nameof(BalanceAssert.FastingBuildLosesToCompetentPlay), names);
        Assert.Contains(nameof(BalanceAssert.CompetentPlayerReachesDay60), names);
        Assert.Contains(nameof(BalanceAssert.OnlyBearSustainsAlone), names);
        Assert.Contains(nameof(BalanceAssert.RelocationIsNotADeathSpiral), names);
    }
}
