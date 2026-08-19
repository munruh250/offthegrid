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
    private readonly Dictionary<Activity, float> potential = new();

    public const float Floor = 0.55f;
    public const float Ceiling = 2.1f;

    /// <summary>
    /// Fraction of the surrounding country the contestant has actually seen.
    /// Tunable - the show's contestants work a small fraction of what is
    /// available to them, and the unseen remainder is what makes ranging out
    /// worth a slot.
    /// </summary>
    public const float InitialExploredFraction = 0.10f;

    /// <summary>How much of the country has been seen, 0 to 1.</summary>
    public float ExploredFraction { get; private set; } = InitialExploredFraction;

    public Territory(Rng rng)
    {
        // TWO numbers per route.
        //
        // quality   is what you have FOUND - the ground you can currently work.
        // potential is what is out there to find, across the ~90% you have not
        //           walked yet.
        //
        // The drop tells you the CHARACTER of your immediate area - whether this
        // looks like fishing country or a game trail - and it does not tell you
        // what is over the ridge. That gap is the whole argument for exploring:
        // your first camp is a sample of a tenth of the map.
        foreach (var route in Routes)
        {
            float found = 0.70f + rng.Stream($"world.drop.{route}").NextFloat() * 0.55f;
            float best = found + rng.Stream($"world.potential.{route}").NextFloat() * 0.85f;

            quality[route] = found;
            potential[route] = Math.Min(Ceiling, best);
        }
    }

    /// <summary>
    /// The best this country could offer for a route, if it were all walked.
    /// Not shown to the player - it is what they are looking FOR.
    /// </summary>
    public float PotentialFor(Activity activity) =>
        potential.TryGetValue(activity, out var p) ? p : 1f;

    /// <summary>
    /// A plain-language read on the ground at the drop. This is what a contestant
    /// can tell in their first day: whether there is water worth fishing, sign
    /// worth following, shoreline worth walking.
    /// </summary>
    public string CharacterOf(Activity activity)
    {
        float q = For(activity);
        return q switch
        {
            >= 1.15f => "promising",
            >= 0.95f => "workable",
            >= 0.78f => "thin",
            _ => "poor"
        };
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
        // Ranging out opens country. With ~90% unwalked at the drop, there is a
        // long way to go and a real reason to go there.
        ExploredFraction = Math.Min(1f, ExploredFraction + 0.045f * effectiveness);

        // A scout looks for what you NEED. Restricting the search to the routes
        // actually being worked matters more than it sounds: an earlier version
        // improved the globally weakest route, which was often one the player
        // never used, and that made a slot of exploring worth almost nothing.
        var candidates = worked is { Count: > 0 } ? worked : Routes;

        Activity target = candidates[0];
        foreach (var r in candidates)
            if (quality.ContainsKey(r) && quality[r] < quality[target]) target = r;

        // You can only find what is actually out there. A route whose potential
        // is already reached will not improve however far you walk - which is
        // itself information, and the reason relocation exists.
        float gap = Math.Max(0f, potential[target] - quality[target]);
        quality[target] = Math.Min(potential[target], quality[target] + 0.42f * effectiveness * Math.Max(0.18f, gap));

        // And a general read on the country - every route gains a little against
        // its own potential, so a scouting slot is never wasted.
        foreach (var r in Routes)
            quality[r] = Math.Min(potential[r], quality[r] + 0.05f * effectiveness);

        return target;
    }

    /// <summary>Working ground thins it out. Applies to the route being worked.</summary>
    public void Deplete(Activity activity, float amount)
    {
        if (!quality.ContainsKey(activity)) return;
        quality[activity] = Math.Max(Floor, quality[activity] - amount);
    }
}
