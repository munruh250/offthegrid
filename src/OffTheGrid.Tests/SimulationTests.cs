namespace OffTheGrid.Tests;

using Xunit;
using OffTheGrid.Sim;

public sealed class SimulationTests
{
    [Fact]
    public void SimulationStartsAtDay1Hour0()
    {
        var sim = new Simulation();
        Assert.Equal(1, sim.CurrentState.DayNumber);
        Assert.Equal(0, sim.CurrentState.HourOfDay);
    }

    [Fact]
    public void PassTimeCommandAdvancesHours()
    {
        var sim = new Simulation();
        var next = sim.Step(new GameCommand.PassTime(6));
        Assert.Equal(1, next.DayNumber);
        Assert.Equal(6, next.HourOfDay);
    }

    [Fact]
    public void HoursWrapToNextDay()
    {
        var sim = new Simulation();
        sim.Step(new GameCommand.PassTime(20));
        var next = sim.Step(new GameCommand.PassTime(8));
        Assert.Equal(2, next.DayNumber);
        Assert.Equal(4, next.HourOfDay);
    }
}
