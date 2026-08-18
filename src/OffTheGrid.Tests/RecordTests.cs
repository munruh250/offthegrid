namespace OffTheGrid.Tests;

using System.Collections.Generic;
using Xunit;
using OffTheGrid.Data;
using OffTheGrid.Data.Balance;
using OffTheGrid.Sim.Record;

/// <summary>A21 (RunRecord) and C11 (bounded, persistable trace).</summary>
public sealed class RecordTests
{
    private static TraceEntry Entry(int day, string code) => new()
    {
        Day = day,
        Slot = 0,
        Kind = TraceKind.Action,
        Code = code,
        Magnitude = 0f
    };

    [Fact]
    public void TraceIsMemoryBounded()
    {
        // C11's first question. The buffer must never grow, however long the run.
        var trace = new DecisionTrace(capacity: 64);
        for (int i = 0; i < 10_000; i++) trace.Add(Entry(i / 5, $"a{i}"));

        Assert.Equal(64, trace.Capacity);
        Assert.Equal(64, trace.Count);
        Assert.True(trace.HasOverflowed);
    }

    [Fact]
    public void TraceDiscardsOldestFirst()
    {
        var trace = new DecisionTrace(capacity: 3);
        trace.Add(Entry(1, "oldest"));
        trace.Add(Entry(2, "middle"));
        trace.Add(Entry(3, "newest"));
        trace.Add(Entry(4, "newer still"));

        var entries = trace.ToOrderedList();
        Assert.Equal(3, entries.Length);
        Assert.Equal("middle", entries[0].Code);
        Assert.Equal("newer still", entries[2].Code);
    }

    [Fact]
    public void TraceKeepsChronologicalOrder()
    {
        var trace = new DecisionTrace(capacity: 8);
        for (int i = 0; i < 5; i++) trace.Add(Entry(i, $"day{i}"));

        var entries = trace.ToOrderedList();
        for (int i = 0; i < 5; i++) Assert.Equal($"day{i}", entries[i].Code);
    }

    [Fact]
    public void TraceSurvivesSaveRestore()
    {
        // C11's second question, and the one that actually matters on mobile:
        // essentially every session gets backgrounded, so a trace that did not
        // persist would make cause-of-death wrong for most real runs.
        var original = new DecisionTrace(capacity: 16);
        for (int i = 0; i < 10; i++) original.Add(Entry(i, $"e{i}"));

        var serialised = original.ToSerialisable();
        var restored = DecisionTrace.FromSerialisable(serialised, capacity: 16);

        Assert.Equal(original.Count, restored.Count);
        Assert.Equal(original.ToOrderedList(), restored.ToOrderedList());
    }

    [Fact]
    public void TraceSurvivesSaveRestoreAfterOverflow()
    {
        var original = new DecisionTrace(capacity: 4);
        for (int i = 0; i < 20; i++) original.Add(Entry(i, $"e{i}"));

        var restored = DecisionTrace.FromSerialisable(original.ToSerialisable(), capacity: 4);
        Assert.Equal(original.ToOrderedList(), restored.ToOrderedList());
    }

    [Fact]
    public void DefaultCapacityCoversTwentyDayWindow()
    {
        // 5 slots/day x 20 days = 100 actions, plus events. 256 leaves headroom.
        var trace = new DecisionTrace();
        for (int day = 1; day <= 20; day++)
            for (int slot = 0; slot < 5; slot++)
                trace.Add(Entry(day, $"d{day}s{slot}"));

        Assert.False(trace.HasOverflowed);
        Assert.Equal(100, trace.Count);
    }

    [Fact]
    public void RecentDaysFiltersToWindow()
    {
        var trace = new DecisionTrace();
        for (int day = 1; day <= 40; day++) trace.Add(Entry(day, $"day{day}"));

        var recent = trace.RecentDays(currentDay: 40, days: 20);
        Assert.Equal(20, recent.Count);
        Assert.Equal(21, recent[0].Day);
        Assert.Equal(40, recent[^1].Day);
    }

    [Fact]
    public void RunRecordCarriesEverythingNeededToReplay()
    {
        var record = new RunRecord
        {
            Seed = 987654321,
            Sex = Sex.Female,
            StartWeightKg = 78,
            StartBodyFatPercent = 22,
            HeightCm = 168,
            AgeYears = 34,
            Attributes = new Dictionary<AttributeKind, int>
            {
                [AttributeKind.Bushcraft] = 5,
                [AttributeKind.Hunting] = 3,
                [AttributeKind.Foraging] = 8,
                [AttributeKind.Fitness] = 5,
                [AttributeKind.Resolve] = 6,
                [AttributeKind.ColdAdaptation] = 6
            }
        };

        Assert.Equal(1, record.SchemaVersion);
        Assert.Equal(EndCondition.None, record.EndCondition);
        Assert.True(record.IsCleanBalanceSample);
        Assert.Equal(6, record.Attributes.Count);
    }

    [Fact]
    public void HotReloadMarksRunAsUnclean()
    {
        // C10 + A21 interacting: a run whose constants changed mid-flight is not
        // a valid balance sample and the solver must be able to discard it.
        var provider = new BalanceProvider(BalanceData.Default);
        Assert.True(provider.IsCleanSample);

        provider.Reload(new BalanceData { MoraleBaseDailyDecay = -2.0f });

        Assert.False(provider.IsCleanSample);
        Assert.Equal(1, provider.ReloadCount);
        Assert.Equal(-2.0f, provider.Current.MoraleBaseDailyDecay);
    }

    [Fact]
    public void HotReloadRaisesEvent()
    {
        var provider = new BalanceProvider(BalanceData.Default);
        BalanceData? seen = null;
        provider.Reloaded += d => seen = d;

        var replacement = new BalanceData { MoraleProjectCompleted = 20f };
        provider.Reload(replacement);

        Assert.NotNull(seen);
        Assert.Equal(20f, seen!.MoraleProjectCompleted);
    }

    [Fact]
    public void TunedMoraleConstantsMatchBalanceDoc()
    {
        // Balance doc 7.4. These defeat the fasting build; if one drifts,
        // BalanceAssert.FastingBuildLosesTo is the thing that should catch it,
        // but pin them here too so a careless edit fails fast.
        var b = BalanceData.Default;
        Assert.Equal(-1.0f, b.MoraleBaseDailyDecay);
        Assert.Equal(-2.0f, b.MoraleFoodInsecure);
        Assert.Equal(-1.0f, b.MoraleIdlenessStepPerDay);
        Assert.Equal(-5.0f, b.MoraleIdlenessCap);
        Assert.Equal(-0.5f, b.MoraleWeightLossPer5Percent);
        Assert.Equal(14.0f, b.MoraleProjectCompleted);
    }

    [Fact]
    public void RelocationConstantsMatchDoc12()
    {
        var b = BalanceData.Default;
        Assert.Equal(1.0f, b.ShelterLossMoralePerSlot);
        Assert.Equal(10.0f, b.ShelterLossMoraleCap);
        Assert.Equal(0.35f, b.FoodTriggerThreshold);
        Assert.Equal(0.60f, b.FoodVisibleDegradation);
        Assert.Equal(0.8f, b.CloGapThreshold);
        Assert.Equal(RelocationVariant.TotalLoss, b.RelocationVariant);
    }

    [Fact]
    public void ShelterLossMoraleNeverExceedsRebuildReward()
    {
        // Doc 12 s4.1 - the load-bearing constraint. If loss could exceed the
        // rebuild reward, relocation would be a guaranteed death spiral and would
        // ship without ever firing.
        var b = BalanceData.Default;
        Assert.True(b.ShelterLossMoraleCap < b.MoraleProjectCompleted,
            "shelter loss cap must stay below the project-completion reward");
    }
}
