namespace OffTheGrid.Tests;

using Xunit;
using OffTheGrid.Sim.Time;

/// <summary>Checked against the daylight/slot table in design spec 7.1.</summary>
public sealed class CalendarTests
{
    [Theory]
    // Spec 7.1 table: day, stated daylight hours, stated slots.
    [InlineData(1, 12.6f, 5)]
    [InlineData(15, 11.5f, 5)]
    [InlineData(30, 10.4f, 4)]
    [InlineData(45, 9.4f, 4)]
    [InlineData(60, 8.6f, 3)]
    [InlineData(75, 8.0f, 3)]
    public void MatchesSpecDaylightTable(int day, float expectedHours, int expectedSlots)
    {
        Assert.Equal(expectedHours, Calendar.DaylightHours(day), 0.15f);
        Assert.Equal(expectedSlots, Calendar.SlotsForDay(day));
    }

    [Fact]
    public void DaylightShrinksMonotonicallyAcrossARun()
    {
        for (int day = 2; day <= 90; day++)
        {
            Assert.True(Calendar.DaylightHours(day) < Calendar.DaylightHours(day - 1),
                $"daylight must keep shrinking; day {day} did not");
        }
    }

    [Fact]
    public void SlotsNeverLeaveTheClamp()
    {
        for (int day = 1; day <= 120; day++)
        {
            Assert.InRange(Calendar.SlotsForDay(day), Calendar.MinSlotsPerDay, Calendar.MaxSlotsPerDay);
        }
    }

    [Fact]
    public void SeasonCompressesTheActionEconomy()
    {
        // The thesis: difficulty rises without an authored curve. Two fewer slots
        // per day by day 60 is a 40% cut in actions, and it emerges from daylight.
        Assert.Equal(5, Calendar.SlotsForDay(1));
        Assert.Equal(3, Calendar.SlotsForDay(60));
    }

    [Fact]
    public void RunHitsRoughly300Decisions()
    {
        // Spec 7.1 target: ~5 slots/day x 60 days ~= 300 decisions per run.
        int total = 0;
        for (int day = 1; day <= 60; day++) total += Calendar.SlotsForDay(day);
        Assert.InRange(total, 240, 300);
    }

    [Theory]
    [InlineData(1, Season.SalmonRun)]
    [InlineData(20, Season.SalmonRun)]
    [InlineData(21, Season.RunTapering)]
    [InlineData(35, Season.RunTapering)]
    [InlineData(36, Season.Lean)]
    [InlineData(50, Season.Lean)]
    [InlineData(51, Season.Winter)]
    [InlineData(75, Season.Winter)]
    public void SeasonBoundariesMatchBalanceDoc(int day, Season expected)
    {
        Assert.Equal(expected, Calendar.SeasonForDay(day));
    }

    [Fact]
    public void PaletteRunsZeroToOneAcrossSixtyDays()
    {
        Assert.Equal(0f, Calendar.SeasonalPaletteT(1), 0.001f);
        Assert.Equal(1f, Calendar.SeasonalPaletteT(60), 0.001f);
        Assert.Equal(1f, Calendar.SeasonalPaletteT(90), 0.001f);
    }
}
