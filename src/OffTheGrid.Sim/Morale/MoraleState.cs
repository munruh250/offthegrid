using System;
using System.Collections.Generic;
using OffTheGrid.Data.Balance;

namespace OffTheGrid.Sim.Morale;

/// <summary>Inputs the morale model needs for one day. Gathered by the caller from world state.</summary>
public readonly record struct MoraleDayInputs
{
    /// <summary>True when there is no cached surplus of at least one day.</summary>
    public bool FoodInsecure { get; init; }

    /// <summary>True when shelter clo is below what the night temperature demands.</summary>
    public bool ShelterInadequate { get; init; }

    /// <summary>True when no build or craft progress was made today.</summary>
    public bool NoBuildProgress { get; init; }

    /// <summary>True when the player slept soaked.</summary>
    public bool SoakedAtSleep { get; init; }

    /// <summary>Fraction of starting bodyweight lost so far, e.g. 0.12 for 12%.</summary>
    public float WeightLossFraction { get; init; }

    /// <summary>True when the player carries the photo gear item.</summary>
    public bool HasPhoto { get; init; }
}

/// <summary>
/// Morale, as mutable internal state with per-source attribution. Design spec 5.6.
///
/// Two constraints shaped this, and they pull in opposite directions:
///
///   ATTRIBUTION (5.6.1). Every change must remain explicable, so contributions
///   cannot be summed away. The player can tap the HUD at any moment and see
///   which modifiers are active and what each is worth.
///
///   ALLOCATION (A15/C9). The solver runs ~100k times. Allocating a list of
///   contributions per day would be ~6M allocations.
///
/// Both are satisfied by a fixed float array indexed by <see cref="MoraleSource"/>,
/// reused across days. Attribution comes for free because the index IS the reason.
///
/// Morale is load-bearing for balance, not theme: the body sim alone cannot beat
/// the sit-still-and-fast strategy (balance doc 7.2). The idleness penalty is what
/// defeats it. Making morale more legible must never make it weaker.
/// </summary>
public sealed class MoraleState
{
    private static readonly int SourceCount = Enum.GetValues<MoraleSource>().Length;

    private readonly float[] todayBySource;

    public MoraleState(int resolve, BalanceData balance)
    {
        if (resolve is < 1 or > 10) throw new ArgumentOutOfRangeException(nameof(resolve));

        Resolve = resolve;
        todayBySource = new float[SourceCount];
        Current = balance.MoraleStartBase + balance.MoraleStartPerResolve * resolve;
    }

    public int Resolve { get; }

    public float Current { get; private set; }

    /// <summary>Consecutive days with no build or craft progress. Drives the idleness stack.</summary>
    public int ConsecutiveIdleDays { get; private set; }

    /// <summary>Lifetime morale gained from the photo, against its cap.</summary>
    public float PhotoGainedTotal { get; private set; }

    public bool IsInWarningBand(BalanceData balance) => Current < balance.MoraleWarningBand;

    /// <summary>Tap-out is at zero. Design spec 5.6.</summary>
    public bool HasTappedOut => Current <= 0f;

    /// <summary>
    /// Record a one-off morale event that happens mid-day rather than at the day
    /// boundary — a completed project, a good kill, losing a shelter to relocation.
    /// Attributed like everything else.
    /// </summary>
    public void ApplyEvent(MoraleSource source, float value, BalanceData balance)
    {
        todayBySource[(int)source] += value;
        Current = Math.Clamp(Current + value, 0f, balance.MoraleMax);
    }

    /// <summary>
    /// Memory event, scaled by Resolve. Design spec 5.6: -(5 to 20) * (1 - Resolve/12).
    /// The caller supplies the raw magnitude from the event catalogue.
    /// </summary>
    public void ApplyMemoryEvent(float rawMagnitude, BalanceData balance)
    {
        float scaled = rawMagnitude * (1f - Resolve / balance.MoraleMemoryResolveDivisor);
        ApplyEvent(MoraleSource.MemoryEvent, scaled, balance);
    }

    /// <summary>
    /// Start a new day. Clears the attribution accumulator so mid-day events and
    /// the day-boundary modifiers land in the same bucket.
    ///
    /// Clearing happens HERE rather than at the end of EvaluateDay so the
    /// breakdown stays readable for the whole day — the HUD can be tapped at any
    /// moment, and the day summary card is built after the boundary.
    /// </summary>
    public void BeginDay() => Array.Clear(todayBySource);

    /// <summary>
    /// Apply the standing daily modifiers at the day boundary and advance the
    /// idleness counter. Returns scalars only; call <see cref="Breakdown"/> for
    /// the attribution, which the solver never needs.
    /// </summary>
    public MoraleDayTotals EvaluateDay(MoraleDayInputs inputs, BalanceData balance)
    {
        float before = Current;

        Add(MoraleSource.BaseDecay, balance.MoraleBaseDailyDecay);

        if (inputs.FoodInsecure)
            Add(MoraleSource.FoodInsecure, balance.MoraleFoodInsecure);

        if (inputs.ShelterInadequate)
            Add(MoraleSource.ShelterInadequate, balance.MoraleShelterInadequate);

        if (inputs.SoakedAtSleep)
            Add(MoraleSource.SoakedAtSleep, balance.MoraleSoakedAtSleep);

        // Idleness stacks per consecutive day and clamps at the cap. This is the
        // lever that defeats the fasting build - see the class remarks.
        if (inputs.NoBuildProgress)
        {
            ConsecutiveIdleDays++;
            float idle = Math.Max(
                balance.MoraleIdlenessStepPerDay * ConsecutiveIdleDays,
                balance.MoraleIdlenessCap);
            Add(MoraleSource.Idleness, idle);
        }
        else
        {
            ConsecutiveIdleDays = 0;
        }

        if (inputs.WeightLossFraction > 0f)
        {
            float steps = inputs.WeightLossFraction / 0.05f;
            Add(MoraleSource.WeightLoss, balance.MoraleWeightLossPer5Percent * steps);
        }

        if (inputs.HasPhoto && PhotoGainedTotal < balance.MoralePhotoLifetimeCap)
        {
            float gain = Math.Min(
                balance.MoralePhotoPerDay,
                balance.MoralePhotoLifetimeCap - PhotoGainedTotal);
            PhotoGainedTotal += gain;
            Add(MoraleSource.Photo, gain);
        }

        Current = Math.Clamp(Current, 0f, balance.MoraleMax);

        return new MoraleDayTotals
        {
            MoraleBefore = before,
            MoraleAfter = Current,
            Delta = Current - before
        };

        void Add(MoraleSource source, float value)
        {
            todayBySource[(int)source] += value;
            Current += value;
        }
    }

    /// <summary>
    /// Project today's attribution into an allocated breakdown. The game calls
    /// this when the player taps the morale bar and once for the day summary
    /// card; the solver never calls it at all.
    /// </summary>
    public MoraleBreakdown Breakdown()
    {
        int active = 0;
        for (int i = 0; i < todayBySource.Length; i++)
            if (todayBySource[i] != 0f) active++;

        var contributions = new MoraleContribution[active];
        int n = 0;
        for (int i = 0; i < todayBySource.Length; i++)
        {
            if (todayBySource[i] != 0f)
                contributions[n++] = new MoraleContribution((MoraleSource)i, todayBySource[i]);
        }

        Array.Sort(contributions, static (a, b) =>
            Math.Abs(b.Value).CompareTo(Math.Abs(a.Value)));

        return new MoraleBreakdown(contributions);
    }
}

/// <summary>Scalar result of a day boundary. Allocation-free.</summary>
public readonly record struct MoraleDayTotals
{
    public float MoraleBefore { get; init; }
    public float MoraleAfter { get; init; }
    public float Delta { get; init; }
}

/// <summary>One attributed morale change.</summary>
public readonly record struct MoraleContribution(MoraleSource Source, float Value)
{
    public string Label => Source.Label();
}

/// <summary>
/// Today's morale changes, largest first. Serves all three attribution tiers from
/// design spec 5.6.1 — the tappable HUD breakdown (all of it), the day summary
/// card (<see cref="TopMovers"/>), and the end-of-run cause-of-death read.
/// </summary>
public readonly struct MoraleBreakdown(MoraleContribution[] contributions)
{
    public IReadOnlyList<MoraleContribution> Contributions => contributions;

    public float Total
    {
        get
        {
            float sum = 0f;
            foreach (var c in contributions) sum += c.Value;
            return sum;
        }
    }

    /// <summary>
    /// Tier 2: the largest few movers, with everything else collapsed into one
    /// "other" line. Design spec 5.6.1 is explicit that showing all ~40 daily
    /// modifiers is toast-spam that trains the player to dismiss without reading.
    /// </summary>
    public (IReadOnlyList<MoraleContribution> Top, float Other) TopMovers(int count = 3)
    {
        if (contributions.Length <= count)
            return (contributions, 0f);

        var top = new MoraleContribution[count];
        Array.Copy(contributions, top, count);

        float other = 0f;
        for (int i = count; i < contributions.Length; i++) other += contributions[i].Value;

        return (top, other);
    }
}
