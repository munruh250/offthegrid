using System;
using System.Collections.Generic;
using OffTheGrid.Data;
using OffTheGrid.Data.Tables;
using OffTheGrid.Sim.Record;
using OffTheGrid.Sim.Time;

namespace OffTheGrid.Sim.Food;

/// <summary>What one slot of hunting, fishing, trapping or foraging produced.</summary>
public readonly record struct HarvestResult
{
    /// <summary>An animal was found. Says nothing about whether it was taken.</summary>
    public bool Encountered { get; init; }

    /// <summary>It was actually taken. Implies <see cref="Encountered"/>.</summary>
    public bool CaughtSomething => Source.HasValue;

    /// <summary>What was encountered, whether or not it was taken.</summary>
    public FoodSource? EncounteredSource { get; init; }

    public FoodSource? Source { get; init; }
    public float EdibleKg { get; init; }
    public float ProteinG { get; init; }
    public float FatG { get; init; }

    /// <summary>Found it and lost it. The show's most common hunting beat.</summary>
    public bool MissedOpportunity => Encountered && !CaughtSomething;
}

/// <summary>An animal you might meet, and how often.</summary>
public readonly record struct Encounter(FoodSource Source, float EncounterProbability);

/// <summary>
/// Food acquisition, in TWO rolls: did you find it, and did you take it.
///
/// This split is the most important thing in the food model. On the show,
/// seeing game and killing game are completely different events - contestants
/// see deer constantly and connect rarely, and the missed shot is the single
/// most common hunting beat in the series. Collapsing both into one probability
/// (as v0.1 did) produced ~39% success per hunting slot and 4.1 big-game kills
/// per run, which is a butcher's round rather than a survival contest.
///
/// It also earns its keep three other ways:
///   - The M1 archery minigame IS the conversion roll. This is the seam it
///     attaches to; without it there is nowhere to put it.
///   - Arrow scarcity (B5) only means something if you can miss.
///   - It separates two levers that were welded together: how rich the land is
///     (encounter) versus how good the player is (conversion).
///
/// Attributes move CONVERSION, not encounter. Skill makes you a better shot; it
/// does not put more deer in the valley.
///
/// RNG streams: harvest.encounter, harvest.conversion, harvest.size.
/// </summary>
public static class Harvest
{
    /// <summary>
    /// Skill scaling on conversion. Attribute 5 is baseline (1.0), 10 gives 1.4,
    /// 1 gives 0.68. Deliberately shallow - skill should tilt the odds, not
    /// remove the variance, or a high-attribute build stops feeling like survival.
    /// </summary>
    public static float SkillMultiplier(int attribute) => 0.6f + 0.08f * attribute;

    /// <summary>
    /// Chance of converting an encounter into food, before skill.
    ///
    /// Big game with a bow is brutal and should stay brutal: you get a shot, at
    /// range, on an animal that is about to be somewhere else. Shellfish convert
    /// at ~1.0 because gathering is not a contest - if you found it, you have it.
    /// </summary>
    public static float BaseConversion(FoodSource source, Activity activity) => activity switch
    {
        // A trap already did the work. If the line produced, you have the animal.
        Activity.TrapLine => 0.92f,

        // Gathering. Finding is the whole job.
        Activity.Foraging => 0.95f,

        // Line fishing: hooking is not landing.
        Activity.Fishing => source switch
        {
            FoodSource.ChinookSalmon => 0.38f,   // big, strong, most likely to break off
            FoodSource.CohoSalmon => 0.48f,
            FoodSource.SockeyeSalmon => 0.52f,
            _ => 0.58f
        },

        // Stalking with a bow.
        Activity.HuntingStalk => source switch
        {
            FoodSource.RooseveltElk => 0.06f,
            FoodSource.BlackBear => 0.07f,
            FoodSource.BlacktailDeer => 0.10f,
            FoodSource.SnowshoeHare => 0.22f,
            FoodSource.Grouse => 0.30f,
            _ => 0.20f
        },

        _ => 0.0f
    };

    private static readonly Dictionary<(Season, Activity), Encounter[]> Table = new()
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
            new(FoodSource.SnowshoeHare, 0.20f),
            new(FoodSource.Grouse, 0.10f)
        ],
        [(Season.SalmonRun, Activity.Foraging)] =
        [
            new(FoodSource.Mussels, 0.55f),
            new(FoodSource.DungenessCrab, 0.18f)
        ],

        // --- Tapering: coho declining, fat sources thinning. ---
        [(Season.RunTapering, Activity.Fishing)] =
        [
            new(FoodSource.CohoSalmon, 0.26f),
            new(FoodSource.CutthroatTrout, 0.30f),
            new(FoodSource.Rockfish, 0.28f)
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
            new(FoodSource.SnowshoeHare, 0.20f),
            new(FoodSource.Grouse, 0.09f)
        ],
        [(Season.RunTapering, Activity.Foraging)] =
        [
            new(FoodSource.Mussels, 0.42f),
            new(FoodSource.DungenessCrab, 0.12f)
        ],

        // --- Lean season: the protein trap bites hardest. ---
        [(Season.Lean, Activity.Fishing)] =
        [
            new(FoodSource.CutthroatTrout, 0.34f),
            new(FoodSource.Rockfish, 0.36f)
        ],
        [(Season.Lean, Activity.HuntingStalk)] =
        [
            new(FoodSource.BlacktailDeer, 0.17f),
            new(FoodSource.SnowshoeHare, 0.24f),
            new(FoodSource.Grouse, 0.16f)
        ],
        [(Season.Lean, Activity.TrapLine)] =
        [
            new(FoodSource.SnowshoeHare, 0.26f),
            new(FoodSource.Grouse, 0.12f)
        ],
        [(Season.Lean, Activity.Foraging)] =
        [
            new(FoodSource.Mussels, 0.44f)
        ],

        // --- Winter: cache or die. Bear denned, no fat source at all. ---
        [(Season.Winter, Activity.Fishing)] =
        [
            new(FoodSource.Rockfish, 0.30f)
        ],
        [(Season.Winter, Activity.HuntingStalk)] =
        [
            new(FoodSource.BlacktailDeer, 0.13f),
            new(FoodSource.SnowshoeHare, 0.20f),
            new(FoodSource.Grouse, 0.11f)
        ],
        [(Season.Winter, Activity.TrapLine)] =
        [
            new(FoodSource.SnowshoeHare, 0.21f),
            new(FoodSource.Grouse, 0.09f)
        ],
        [(Season.Winter, Activity.Foraging)] =
        [
            new(FoodSource.Mussels, 0.30f)
        ]
    };

    /// <summary>Which attribute governs this activity. Design spec 4.1.</summary>
    public static AttributeKind? GoverningAttribute(Activity activity) => activity switch
    {
        Activity.Fishing => AttributeKind.Hunting,       // fishing folds into Hunting (Q1)
        Activity.HuntingStalk => AttributeKind.Hunting,
        Activity.TrapLine => AttributeKind.Hunting,
        Activity.Foraging => AttributeKind.Foraging,
        _ => null
    };

    /// <summary>
    /// Resolve one slot. Rolls encounter, then conversion. A result can be
    /// "encountered but missed", which is a real and common outcome and should be
    /// surfaced to the player rather than silently reported as nothing found.
    /// </summary>
    public static HarvestResult Resolve(Activity activity, Season season, int attribute, Rng rng)
    {
        if (!Table.TryGetValue((season, activity), out var encounters)) return default;

        float roll = rng.Stream("harvest.encounter").NextFloat();
        float cumulative = 0f;

        foreach (var encounter in encounters)
        {
            cumulative += encounter.EncounterProbability;
            if (roll >= cumulative) continue;

            float conversion = BaseConversion(encounter.Source, activity) * SkillMultiplier(attribute);
            bool taken = rng.Stream("harvest.conversion").NextFloat() < conversion;

            return taken
                ? Take(encounter.Source, rng)
                : new HarvestResult { Encountered = true, EncounteredSource = encounter.Source };
        }

        return default;
    }

    /// <summary>Turn a successful conversion into an animal, with individual size variance.</summary>
    private static HarvestResult Take(FoodSource source, Rng rng)
    {
        var entry = FoodTable.Get(source);

        float sizeFactor = 0.75f + rng.Stream("harvest.size").NextFloat() * 0.5f;
        float kg = entry.EdibleKg * sizeFactor;
        var (protein, fat) = entry.MacrosForKg(kg);

        return new HarvestResult
        {
            Encountered = true,
            EncounteredSource = source,
            Source = source,
            EdibleKg = kg,
            ProteinG = protein,
            FatG = fat
        };
    }
}
