using System;
using System.Collections.Generic;
using OffTheGrid.Data;
using OffTheGrid.Data.Tables;
using OffTheGrid.Sim.Record;
using OffTheGrid.Sim.Time;

namespace OffTheGrid.Sim.Food;

/// <summary>What one slot of hunting, fishing or foraging produced.</summary>
public readonly record struct HarvestResult
{
    public bool CaughtSomething => Source.HasValue;
    public FoodSource? Source { get; init; }
    public float EdibleKg { get; init; }
    public float ProteinG { get; init; }
    public float FatG { get; init; }
}

/// <summary>One possible outcome of a slot, with its probability at skill 5.</summary>
public readonly record struct Encounter(FoodSource Source, float BaseProbability);

/// <summary>
/// Food acquisition. Resolves a slot into a catch or nothing, consuming RNG.
///
/// [B12] Encounter rates are DERIVED, not transcribed. Balance doc 8 lists animal
/// encounter rates as open tuning lever #4 ("controls expected income, affects
/// variance too"), so there is no table to copy. These are calibrated so that
/// competent play reproduces the intake curve implied by doc 7.1's validated
/// weight track - roughly 1,800 usable kcal/day during the salmon run falling to
/// ~1,200 by winter. The solver should confirm and refine them.
///
/// Note what actually binds. During the salmon run the larder is full and the
/// PROTEIN CEILING limits intake, not scarcity. Later, scarcity takes over. That
/// handoff is the shape of the run: early you are limited by what your body can
/// process, late by what you can find.
///
/// RNG streams: "harvest.encounter" and "harvest.size". Both are named consumers,
/// per the determinism rules - do not reuse another stream here.
/// </summary>
public static class Harvest
{
    /// <summary>
    /// Skill scaling. Attribute 5 is the baseline (multiplier 1.0), 10 gives 1.4,
    /// 1 gives 0.68. Deliberately shallow: skill should tilt the odds, not remove
    /// the variance, or a high-attribute build stops feeling like survival.
    /// </summary>
    public static float SkillMultiplier(int attribute) => 0.6f + 0.08f * attribute;

    private static readonly Dictionary<(Season, Activity), Encounter[]> Table = new()
    {
        // --- Salmon run: the abundance window. Fat is available; bank it now. ---
        // Chinook leads deliberately. It is the fattiest fish in the table (52% of
        // calories from fat against coho's 36%), and doc 4.3 calls the run the
        // "abundance window - fat available, bank now". Weighting the run toward
        // lean fish makes the salmon run nutritionally indistinguishable from the
        // lean season, which is not what the seasonal design says.
        [(Season.SalmonRun, Activity.Fishing)] =
        [
            new(FoodSource.ChinookSalmon, 0.26f),
            new(FoodSource.CohoSalmon, 0.24f),
            new(FoodSource.SockeyeSalmon, 0.20f),
            new(FoodSource.CutthroatTrout, 0.16f)
        ],
        [(Season.SalmonRun, Activity.HuntingStalk)] =
        [
            new(FoodSource.BlacktailDeer, 0.05f),
            new(FoodSource.BlackBear, 0.02f),
            new(FoodSource.SnowshoeHare, 0.16f),
            new(FoodSource.Grouse, 0.16f)
        ],
        [(Season.SalmonRun, Activity.Foraging)] =
        [
            new(FoodSource.Mussels, 0.45f),
            new(FoodSource.DungenessCrab, 0.16f)
        ],

        // --- Tapering: coho declining, fat sources thinning. ---
        [(Season.RunTapering, Activity.Fishing)] =
        [
            new(FoodSource.CohoSalmon, 0.18f),
            new(FoodSource.CutthroatTrout, 0.24f),
            new(FoodSource.Rockfish, 0.22f)
        ],
        [(Season.RunTapering, Activity.HuntingStalk)] =
        [
            new(FoodSource.BlacktailDeer, 0.06f),
            new(FoodSource.BlackBear, 0.02f),
            new(FoodSource.SnowshoeHare, 0.18f),
            new(FoodSource.Grouse, 0.14f)
        ],
        [(Season.RunTapering, Activity.Foraging)] =
        [
            new(FoodSource.Mussels, 0.34f),
            new(FoodSource.DungenessCrab, 0.10f)
        ],

        // --- Lean season: the protein trap bites hardest. ---
        [(Season.Lean, Activity.Fishing)] =
        [
            new(FoodSource.CutthroatTrout, 0.20f),
            new(FoodSource.Rockfish, 0.22f)
        ],
        [(Season.Lean, Activity.HuntingStalk)] =
        [
            new(FoodSource.BlacktailDeer, 0.05f),
            new(FoodSource.SnowshoeHare, 0.16f),
            new(FoodSource.Grouse, 0.10f)
        ],
        [(Season.Lean, Activity.Foraging)] =
        [
            new(FoodSource.Mussels, 0.22f)
        ],

        // --- Winter: cache or die. Bear denned - no fat source at all. ---
        [(Season.Winter, Activity.Fishing)] =
        [
            new(FoodSource.Rockfish, 0.16f)
        ],
        [(Season.Winter, Activity.HuntingStalk)] =
        [
            new(FoodSource.BlacktailDeer, 0.03f),
            new(FoodSource.SnowshoeHare, 0.13f),
            new(FoodSource.Grouse, 0.06f)
        ],
        [(Season.Winter, Activity.Foraging)] =
        [
            new(FoodSource.Mussels, 0.12f)
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
    /// Resolve one slot. Returns an empty result for activities that do not
    /// produce food, or when nothing was caught.
    /// </summary>
    public static HarvestResult Resolve(Activity activity, Season season, int attribute, Rng rng)
    {
        if (!Table.TryGetValue((season, activity), out var encounters)) return default;

        float skill = SkillMultiplier(attribute);
        float roll = rng.Stream("harvest.encounter").NextFloat();
        float cumulative = 0f;

        foreach (var encounter in encounters)
        {
            cumulative += encounter.BaseProbability * skill;
            if (roll < cumulative) return Take(encounter.Source, rng);
        }

        return default;
    }

    /// <summary>
    /// Turn an encounter into an actual animal, with size variance. Individuals
    /// vary, so a deer is not always the table's deer.
    /// </summary>
    private static HarvestResult Take(FoodSource source, Rng rng)
    {
        var entry = FoodTable.Get(source);

        // 0.75x to 1.25x the table individual.
        float sizeFactor = 0.75f + rng.Stream("harvest.size").NextFloat() * 0.5f;
        float kg = entry.EdibleKg * sizeFactor;
        var (protein, fat) = entry.MacrosForKg(kg);

        return new HarvestResult
        {
            Source = source,
            EdibleKg = kg,
            ProteinG = protein,
            FatG = fat
        };
    }
}
