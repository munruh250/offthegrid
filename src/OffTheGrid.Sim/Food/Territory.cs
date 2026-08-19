using System;
using System.Collections.Generic;
using OffTheGrid.Data;
using OffTheGrid.Sim.Time;

namespace OffTheGrid.Sim.Food;

/// <summary>
/// How good the ground around camp is, PER ROUTE.
///
/// This was a single number, and that made exploration a front-load-or-skip
/// decision: one generic quality figure that rose smoothly and helped everything
/// equally. Splitting it by route changes the shape of the decision entirely.
///
/// A drop point is now genuinely varied - you might land on a berry slope with
/// dead water, or a game trail with nothing to gather. Exploring DISCOVERS
/// specific ground, in discrete steps rather than a smooth ramp, so "there is a
/// creek two valleys north" is a moment rather than a percentage. And it means
/// what you find shapes what you should do, which is the prospecting model design
/// spec 8.2 actually describes.
/// </summary>
public sealed class Territory
{
    private readonly Dictionary<Activity, float> quality = new();

    public const float Floor = 0.55f;
    public const float Ceiling = 2.1f;

    public Territory(Rng rng)
    {
        // Each route rolled INDEPENDENTLY. A poor drop for one thing is often a
        // good drop for another, which is what makes reading your ground a skill.
        foreach (var route in Routes)
            quality[route] = 0.7f + rng.Stream($"world.drop.{route}").NextFloat() * 0.6f;
    }

    public static readonly Activity[] Routes =
        [Activity.Fishing, Activity.HuntingStalk, Activity.TrapLine, Activity.Foraging];

    public float For(Activity activity) =>
        quality.TryGetValue(activity, out var q) ? q : 1f;

    /// <summary>Average across routes. Used for readouts, not for resolution.</summary>
    public float Overall
    {
        get
        {
            float sum = 0f;
            foreach (var r in Routes) sum += quality[r];
            return sum / Routes.Length;
        }
    }

    /// <summary>The route this ground is best for - what a scout would tell you.</summary>
    public Activity Best
    {
        get
        {
            Activity best = Routes[0];
            foreach (var r in Routes) if (quality[r] > quality[best]) best = r;
            return best;
        }
    }

    /// <summary>
    /// Spend a slot ranging out. Finds ground for ONE route - whichever is
    /// currently weakest, because that is what a scout is looking for - and finds
    /// it in a step rather than a trickle.
    ///
    /// Returns the route improved, so the presentation layer can say what was
    /// found rather than showing a bar creeping upward.
    /// </summary>
    public Activity Prospect(float effectiveness, IReadOnlyList<Activity>? worked = null)
    {
        // A scout looks for what you NEED. Restricting the search to the routes
        // actually being worked matters more than it sounds: an earlier version
        // improved the globally weakest route, which was often one the player
        // never used, and that made a slot of exploring worth almost nothing.
        var candidates = worked is { Count: > 0 } ? worked : Routes;

        Activity target = candidates[0];
        foreach (var r in candidates)
            if (quality.ContainsKey(r) && quality[r] < quality[target]) target = r;

        float headroom = (Ceiling - quality[target]) / (Ceiling - 1.0f);
        quality[target] = Math.Min(Ceiling, quality[target] + 0.30f * effectiveness * Math.Max(0.25f, headroom));

        // Ranging out also gives you a general read on the country - every route
        // gains a little, so a slot spent scouting is never wasted.
        foreach (var r in Routes)
            quality[r] = Math.Min(Ceiling, quality[r] + 0.045f * effectiveness);

        return target;
    }

    /// <summary>Working ground thins it out. Applies to the route being worked.</summary>
    public void Deplete(Activity activity, float amount)
    {
        if (!quality.ContainsKey(activity)) return;
        quality[activity] = Math.Max(Floor, quality[activity] - amount);
    }
}
