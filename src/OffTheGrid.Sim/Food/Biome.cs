using System;
using System.Collections.Generic;
using OffTheGrid.Data.Tables;
using OffTheGrid.Sim.Time;

namespace OffTheGrid.Sim.Food;

/// <summary>
/// A place, expressed as what it offers per slot and how cold it gets.
///
/// Biomes were previously a single hardcoded table, which had a consequence that
/// only became visible once route balance was measured: every result the project
/// had was from ONE biome, and that biome is fishing-dominant by design. Archetype
/// balance could not be assessed, because a build that is weak on Vancouver Island
/// might simply be a correct build in the wrong place.
///
/// Making this data rather than code allows a controlled comparison - see
/// <see cref="ProvingGround"/>.
/// </summary>
public sealed class Biome
{
    public required string Name { get; init; }
    public required IReadOnlyDictionary<(Season, Activity), Encounter[]> Encounters { get; init; }

    /// <summary>Night temperature on a given run day.</summary>
    public required Func<int, float> NightTemperature { get; init; }

    public Encounter[] EncountersFor(Season season, Activity activity) =>
        Encounters.TryGetValue((season, activity), out var e) ? e : [];

    /// <summary>Vancouver Island. The MVP biome, and a strongly FISHING-led one.</summary>
    public static Biome VancouverIsland { get; } = new()
    {
        Name = "Vancouver Island",
        NightTemperature = day => 12f - 17f * Math.Clamp((day - 1) / 75f, 0f, 1f),
        Encounters = new Dictionary<(Season, Activity), Encounter[]>
        {
            // --- Salmon run: the abundance window. Fat available, bank now. ---
            [(Season.SalmonRun, Activity.Fishing)] =
            [
                new(FoodSource.ChinookSalmon, 0.34f),
                new(FoodSource.CohoSalmon, 0.30f),
                new(FoodSource.SockeyeSalmon, 0.26f),
                new(FoodSource.CutthroatTrout, 0.14f)
            ],
            [(Season.SalmonRun, Activity.HuntingStalk)] =
            [
                new(FoodSource.RooseveltElk, 0.03f),
                new(FoodSource.BlacktailDeer, 0.12f),
                new(FoodSource.BlackBear, 0.04f),
                new(FoodSource.SnowshoeHare, 0.18f),
                new(FoodSource.Grouse, 0.16f)
            ],
            [(Season.SalmonRun, Activity.TrapLine)] =
            [
                new(FoodSource.SnowshoeHare, 0.22f),
                new(FoodSource.Grouse, 0.12f)
            ],
            [(Season.SalmonRun, Activity.Foraging)] =
            [
                new(FoodSource.Berries, 0.52f),
                new(FoodSource.Mussels, 0.34f),
                new(FoodSource.SeaweedAndKelp, 0.14f),
                new(FoodSource.DungenessCrab, 0.10f)
            ],

            // --- Tapering: coho declining, fat sources thinning. ---
            [(Season.RunTapering, Activity.Fishing)] =
            [
                new(FoodSource.CohoSalmon, 0.16f),
                new(FoodSource.CutthroatTrout, 0.20f),
                new(FoodSource.Rockfish, 0.18f)
            ],
            [(Season.RunTapering, Activity.HuntingStalk)] =
            [
                new(FoodSource.RooseveltElk, 0.03f),
                new(FoodSource.BlacktailDeer, 0.18f),
                new(FoodSource.BlackBear, 0.04f),
                new(FoodSource.SnowshoeHare, 0.18f),
                new(FoodSource.Grouse, 0.14f)
            ],
            [(Season.RunTapering, Activity.TrapLine)] =
            [
                new(FoodSource.SnowshoeHare, 0.26f),
                new(FoodSource.Grouse, 0.14f)
            ],
            [(Season.RunTapering, Activity.Foraging)] =
            [
                new(FoodSource.Berries, 0.30f),
                new(FoodSource.Mussels, 0.34f),
                new(FoodSource.SeaweedAndKelp, 0.16f),
                new(FoodSource.DungenessCrab, 0.08f)
            ],

            // --- Lean season: the protein trap bites hardest. ---
            [(Season.Lean, Activity.Fishing)] =
            [
                new(FoodSource.CutthroatTrout, 0.16f),
                new(FoodSource.Rockfish, 0.18f)
            ],
            [(Season.Lean, Activity.HuntingStalk)] =
            [
                new(FoodSource.BlacktailDeer, 0.24f),
                new(FoodSource.SnowshoeHare, 0.22f),
                new(FoodSource.Grouse, 0.14f)
            ],
            [(Season.Lean, Activity.TrapLine)] =
            [
                new(FoodSource.SnowshoeHare, 0.34f),
                new(FoodSource.Grouse, 0.18f)
            ],
            [(Season.Lean, Activity.Foraging)] =
            [
                new(FoodSource.Mussels, 0.36f),
                new(FoodSource.SeaweedAndKelp, 0.22f)
            ],

            // --- Winter: cache or die. Bear denned, no fat source at all. ---
            [(Season.Winter, Activity.Fishing)] =
            [
                new(FoodSource.Rockfish, 0.12f)
            ],
            [(Season.Winter, Activity.HuntingStalk)] =
            [
                new(FoodSource.BlacktailDeer, 0.20f),
                new(FoodSource.SnowshoeHare, 0.18f),
                new(FoodSource.Grouse, 0.10f)
            ],
            [(Season.Winter, Activity.TrapLine)] =
            [
                new(FoodSource.SnowshoeHare, 0.30f),
                new(FoodSource.Grouse, 0.16f)
            ],
            [(Season.Winter, Activity.Foraging)] =
            [
                new(FoodSource.Mussels, 0.24f),
                new(FoodSource.SeaweedAndKelp, 0.18f)
            ]
        }
    };

    /// <summary>
    /// A CONTROL biome. Not a real place and not intended to ship.
    ///
    /// Every route returns roughly the same expected calories per slot, every food
    /// source is present, and the seasonal decline applies EQUALLY to all four
    /// routes rather than gutting one and sparing another. Temperature still falls
    /// on the same curve, so the cold economy is exercised.
    ///
    /// The purpose is to isolate player choice from place. On Vancouver Island a
    /// fishing build wins because the biome hands it a 7x advantage for twenty
    /// days; here, if one build still wins, that is a property of the BUILD.
    /// </summary>
    public static Biome ProvingGround { get; } = new()
    {
        Name = "Proving Ground (control)",
        NightTemperature = day => 12f - 17f * Math.Clamp((day - 1) / 75f, 0f, 1f),
        Encounters = BuildEvenTable()
    };

    /// <summary>
    /// Builds the control table: four routes, equal expected yield, declining
    /// together. Season multipliers are identical across routes so no route has a
    /// seasonal niche - that is the whole point of a control.
    /// </summary>
    private static Dictionary<(Season, Activity), Encounter[]> BuildEvenTable()
    {
        var table = new Dictionary<(Season, Activity), Encounter[]>();

        float[] seasonScale = [1.00f, 0.82f, 0.64f, 0.50f];
        Season[] seasons = [Season.SalmonRun, Season.RunTapering, Season.Lean, Season.Winter];

        // Per-route composition, tuned so each returns ~800 gross kcal/slot at
        // scale 1.0 after conversion. Every food source in the game appears.
        (Activity activity, (FoodSource source, float p)[] mix)[] routes =
        [
            (Activity.Fishing, [
                (FoodSource.ChinookSalmon, 0.13f), (FoodSource.CohoSalmon, 0.14f),
                (FoodSource.SockeyeSalmon, 0.14f), (FoodSource.CutthroatTrout, 0.20f),
                (FoodSource.Rockfish, 0.20f)]),

            (Activity.TrapLine, [
                (FoodSource.SnowshoeHare, 0.58f), (FoodSource.Grouse, 0.38f)]),

            (Activity.HuntingStalk, [
                (FoodSource.RooseveltElk, 0.02f), (FoodSource.BlackBear, 0.03f),
                (FoodSource.BlacktailDeer, 0.11f), (FoodSource.SnowshoeHare, 0.30f),
                (FoodSource.Grouse, 0.30f)]),

            (Activity.Foraging, [
                (FoodSource.Berries, 0.30f), (FoodSource.Mussels, 0.26f),
                (FoodSource.SeaweedAndKelp, 0.24f), (FoodSource.DungenessCrab, 0.16f)]),
        ];

        for (int i = 0; i < seasons.Length; i++)
        foreach (var (activity, mix) in routes)
        {
            var encounters = new Encounter[mix.Length];
            for (int j = 0; j < mix.Length; j++)
                encounters[j] = new Encounter(mix[j].source, mix[j].p * seasonScale[i]);
            table[(seasons[i], activity)] = encounters;
        }

        return table;
    }
}
