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

    /// <summary>Night temperature at the start of a run.</summary>
    public required float StartNightTempC { get; init; }

    /// <summary>Night temperature once winter has fully arrived. THIS is what makes a biome cold.</summary>
    public required float WinterNightTempC { get; init; }

    /// <summary>
    /// Night temperature on a given day, interpolated against the season
    /// schedule so that an early winter is genuinely an early COLD, not just an
    /// early relabelling of the food tables.
    /// </summary>
    public float NightTemperature(int dayNumber, SeasonSchedule schedule)
    {
        float t = Math.Clamp(dayNumber / (float)schedule.WinterArrives, 0f, 1.35f);
        return StartNightTempC - (StartNightTempC - WinterNightTempC) * t;
    }

    /// <summary>
    /// How hard the ground is to move over, as a multiplier on movement-type
    /// activity cost.
    ///
    /// Terrain is priced in CALORIES, not in injury rolls. Rough country does not
    /// throw dice at you - it charges you more to cross, and that charge is what
    /// eventually puts you in the position where pushing on is a real decision.
    /// The multiplier was plumbed through the whole energy model from the start
    /// and never set to anything but 1.0.
    /// </summary>
    public float TerrainMultiplier { get; init; } = 1.0f;

    public Encounter[] EncountersFor(Season season, Activity activity) =>
        Encounters.TryGetValue((season, activity), out var e) ? e : [];

    /// <summary>Vancouver Island. The MVP biome, and a strongly FISHING-led one.</summary>
    public static Biome VancouverIsland { get; } = new()
    {
        Name = "Vancouver Island",
        // Steep, wet, root-tangled coastal rainforest - genuinely hard going.
        // Held at 1.12 rather than the ~1.22 the terrain arguably deserves: the
        // sim is already ~13 days short of its day-60 target, and terrain is the
        // THIRD mechanic in a row to push the competent-play check against its
        // threshold. See the note in doc 15 - run length now gates further work.
        // Steep, wet, root-tangled coastal rainforest - genuinely hard going.
        TerrainMultiplier = 1.22f,
        // A MILD biome. Vancouver Island in November sits near freezing, which is
        // why cold is not what kills you here - food scarcity is. The cold
        // economy is not broken in this biome, it is simply not the threat.
        StartNightTempC = 12f,
        WinterNightTempC = -5f,
        Encounters = new Dictionary<(Season, Activity), Encounter[]>
        {
            // TARGET RETURNS (gross kcal per slot ÷ kcal spent), signed off:
            //   Fishing    2.0 -> 1.0     narrows across the run rather than
            //                             collapsing; you can still ice fish
            //   Trap line  ~2.0 flat      a snare line works all year
            //   Foraging   2.3 / 2.0 / 1.3 / 0.9
            //   Hunting    peaks mid-run, with small game as a floor
            //
            // Hunting carries small game as well as big. A stalking hunter takes
            // grouse and squirrel far more often than deer - the big animal is
            // the payoff, not the whole route. Traps are hare-dominant, which is
            // what a snare line actually catches.

            // --- SALMON RUN ---
            [(Season.SalmonRun, Activity.Fishing)] =
            [
                new(FoodSource.ChinookSalmon, 0.11f),
                new(FoodSource.CohoSalmon, 0.11f),
                new(FoodSource.SockeyeSalmon, 0.10f),
                new(FoodSource.CutthroatTrout, 0.09f)
            ],
            [(Season.SalmonRun, Activity.HuntingStalk)] =
            [
                new(FoodSource.RooseveltElk, 0.04f),
                new(FoodSource.BlackBear, 0.06f),
                new(FoodSource.BlacktailDeer, 0.20f),
                new(FoodSource.Grouse, 0.22f),
                new(FoodSource.Squirrel, 0.20f)
            ],
            [(Season.SalmonRun, Activity.TrapLine)] =
            [
                new(FoodSource.SnowshoeHare, 0.30f),
                new(FoodSource.Grouse, 0.10f),
                new(FoodSource.Squirrel, 0.08f)
            ],
            [(Season.SalmonRun, Activity.Foraging)] =
            [
                new(FoodSource.Berries, 0.20f),
                new(FoodSource.Mussels, 0.14f),
                new(FoodSource.SeaweedAndKelp, 0.10f),
                new(FoodSource.DungenessCrab, 0.06f)
            ],

            // --- TAPERING ---
            [(Season.RunTapering, Activity.Fishing)] =
            [
                new(FoodSource.CohoSalmon, 0.14f),
                new(FoodSource.Lingcod, 0.16f),
                new(FoodSource.CutthroatTrout, 0.20f),
                new(FoodSource.Rockfish, 0.19f)
            ],
            [(Season.RunTapering, Activity.HuntingStalk)] =
            [
                new(FoodSource.RooseveltElk, 0.04f),
                new(FoodSource.BlackBear, 0.06f),
                new(FoodSource.BlacktailDeer, 0.22f),
                new(FoodSource.Grouse, 0.20f),
                new(FoodSource.Squirrel, 0.18f)
            ],
            [(Season.RunTapering, Activity.TrapLine)] =
            [
                new(FoodSource.SnowshoeHare, 0.30f),
                new(FoodSource.Grouse, 0.10f),
                new(FoodSource.Squirrel, 0.08f)
            ],
            [(Season.RunTapering, Activity.Foraging)] =
            [
                new(FoodSource.Berries, 0.16f),
                new(FoodSource.Mussels, 0.15f),
                new(FoodSource.SeaweedAndKelp, 0.12f),
                new(FoodSource.DungenessCrab, 0.05f)
            ],

            // --- LEAN ---
            [(Season.Lean, Activity.Fishing)] =
            [
                new(FoodSource.Lingcod, 0.21f),
                new(FoodSource.CutthroatTrout, 0.26f),
                new(FoodSource.Rockfish, 0.28f)
            ],
            [(Season.Lean, Activity.HuntingStalk)] =
            [
                new(FoodSource.BlacktailDeer, 0.42f),
                new(FoodSource.BlackBear, 0.04f),
                new(FoodSource.Grouse, 0.20f),
                new(FoodSource.Squirrel, 0.18f)
            ],
            [(Season.Lean, Activity.TrapLine)] =
            [
                new(FoodSource.SnowshoeHare, 0.30f),
                new(FoodSource.Grouse, 0.10f),
                new(FoodSource.Squirrel, 0.08f)
            ],
            [(Season.Lean, Activity.Foraging)] =
            [
                new(FoodSource.Mussels, 0.18f),
                new(FoodSource.SeaweedAndKelp, 0.15f),
                new(FoodSource.Berries, 0.04f)
            ],

            // --- WINTER: you can still fish through ice. Bear denned. ---
            [(Season.Winter, Activity.Fishing)] =
            [
                new(FoodSource.Lingcod, 0.15f),
                new(FoodSource.CutthroatTrout, 0.24f),
                new(FoodSource.Rockfish, 0.26f)
            ],
            [(Season.Winter, Activity.HuntingStalk)] =
            [
                new(FoodSource.BlacktailDeer, 0.38f),
                new(FoodSource.Grouse, 0.18f),
                new(FoodSource.Squirrel, 0.16f)
            ],
            [(Season.Winter, Activity.TrapLine)] =
            [
                new(FoodSource.SnowshoeHare, 0.30f),
                new(FoodSource.Grouse, 0.10f),
                new(FoodSource.Squirrel, 0.08f)
            ],
            [(Season.Winter, Activity.Foraging)] =
            [
                new(FoodSource.Mussels, 0.17f),
                new(FoodSource.SeaweedAndKelp, 0.15f)
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
        TerrainMultiplier = 1.0f,
        StartNightTempC = 12f,
        WinterNightTempC = -5f,
        Encounters = BuildEvenTable()
    };

    /// <summary>
    /// Builds the control table: four routes, equal expected yield, declining
    /// together. Season multipliers are identical across routes so no route has a
    /// seasonal niche - that is the whole point of a control.
    /// </summary>
    /// <summary>
    /// A genuinely COLD biome. Same systems, completely different threat: at
    /// -25 C a night demands roughly 9 clo, against the ~6.5 a sleeping bag,
    /// A-frame and fire provide. Here the axe, the saw, the log shelter and the
    /// whole fuel economy are survival rather than luxury - and a build that is
    /// weak on Vancouver Island may be correct here.
    /// </summary>
    public static Biome BorealInterior { get; } = new()
    {
        Name = "Boreal Interior",
        TerrainMultiplier = 1.08f,
        StartNightTempC = 5f,
        // -15 C, not -25. At -25 a night demands ~9.4 clo against a maximum
        // achievable ~9.1 (clothing + bag + log cabin + fire), so NOBODY can
        // winterize and the shelter-builder simply has less food than the player
        // who ignored shelter - measured, and exactly backwards.
        //
        // -15 demands 7.4: an A-frame plus fire (6.5) is NOT enough, a log
        // shelter plus fire (7.9) IS. Winterizing therefore means committing 16
        // slots to the log shelter, which is the decision this biome exists to
        // pose.
        WinterNightTempC = -15f,
        Encounters = BuildEvenTable()
    };

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
                (FoodSource.SockeyeSalmon, 0.14f), (FoodSource.CutthroatTrout, 0.16f),
                (FoodSource.Rockfish, 0.16f), (FoodSource.Lingcod, 0.08f)]),

            // Divided by the draws-per-slot each route gets, so the CONTROL
            // stays even after plural routes started rolling three times.
            (Activity.TrapLine, [
                (FoodSource.SnowshoeHare, 0.19f), (FoodSource.Grouse, 0.13f)]),

            (Activity.HuntingStalk, [
                (FoodSource.RooseveltElk, 0.02f), (FoodSource.BlackBear, 0.03f),
                (FoodSource.BlacktailDeer, 0.11f), (FoodSource.SnowshoeHare, 0.24f),
                (FoodSource.Grouse, 0.22f), (FoodSource.Squirrel, 0.14f)]),

            (Activity.Foraging, [
                (FoodSource.Berries, 0.10f), (FoodSource.Mussels, 0.09f),
                (FoodSource.SeaweedAndKelp, 0.08f), (FoodSource.DungenessCrab, 0.05f)]),
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
