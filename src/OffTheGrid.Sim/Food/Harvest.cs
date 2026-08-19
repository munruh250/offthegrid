using System;
using System.Collections.Generic;
using OffTheGrid.Data;
using OffTheGrid.Data.Gear;
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
    public float CarbohydrateG { get; init; }

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

        // Gathering. Recognising what is safe and worth taking is the job, and
        // it is where Foraging skill lives. A base of 0.95 left no headroom at
        // all - measured, the attribute was worth 0.1 days per point across its
        // entire range even on a plan built around it.
        Activity.Foraging => source switch
        {
            FoodSource.Berries => 0.55f,
            FoodSource.SeaweedAndKelp => 0.72f,
            _ => 0.80f
        },

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

    /// <summary>
    /// How many independent chances a slot gets.
    ///
    /// Some routes are plural by nature and were being modelled as singular. A
    /// trap LINE is a line - ten snares checked on a circuit can yield several
    /// animals, not one. Foraging is 2.2 hours of gathering, not a single
    /// handful. A gillnet fishes while you do something else; a hook does not.
    /// Stalking is genuinely one animal.
    ///
    /// This is what lets trapping reach a competitive return without pushing its
    /// encounter probabilities past 1.0, where they saturate and stop responding
    /// to tuning at all.
    /// </summary>
    public static int DrawsPerSlot(Activity activity, Loadout gear) => activity switch
    {
        Activity.TrapLine => 3,
        Activity.Foraging => 3,
        Activity.Fishing => gear.Has(GearItem.Gillnet) ? 2 : 1,
        _ => 1
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
    /// <summary>What a food source demands of the kit.</summary>
    public static ActivityRequirement RequirementFor(FoodSource source, Activity activity) => activity switch
    {
        Activity.Fishing => ActivityRequirement.Fishing,
        Activity.TrapLine => ActivityRequirement.Trapping,
        Activity.Foraging => ActivityRequirement.Foraging,
        Activity.HuntingStalk => source is FoodSource.RooseveltElk or FoodSource.BlackBear or FoodSource.BlacktailDeer
            ? ActivityRequirement.BigGame
            : ActivityRequirement.SmallGame,
        _ => ActivityRequirement.None
    };

    /// <summary>
    /// Resolve one slot. Rolls encounter, then conversion.
    ///
    /// Gear gates and multiplies. Without a bow the big animals still walk past
    /// you - the encounter happens, and you watch it leave. That is deliberate:
    /// seeing what you cannot take is a sharper lesson about your loadout than
    /// never seeing it, and it is exactly what the show puts on screen.
    ///
    /// <paramref name="territoryQuality"/> is the prospecting term from design
    /// spec 8.2 - animals live on the map, your drop point may genuinely be poor,
    /// and exploring finds better ground.
    /// </summary>
    /// <summary>
    /// Resolve one slot, which may be several independent chances - see
    /// <see cref="DrawsPerSlot"/>. Returns the combined haul.
    /// </summary>
    public static HarvestResult Resolve(
        Activity activity, Season season, int attribute, Loadout gear, Rng rng,
        float territoryQuality = 1f, Biome? biome = null)
    {
        int draws = DrawsPerSlot(activity, gear);
        if (draws == 1) return ResolveOnce(activity, season, attribute, gear, rng, territoryQuality, biome);

        HarvestResult combined = default;
        for (int i = 0; i < draws; i++)
        {
            var one = ResolveOnce(activity, season, attribute, gear, rng, territoryQuality, biome);
            if (!one.Encountered) continue;

            combined = new HarvestResult
            {
                Encountered = true,
                EncounteredSource = one.EncounteredSource ?? combined.EncounteredSource,
                Source = one.Source ?? combined.Source,
                EdibleKg = combined.EdibleKg + one.EdibleKg,
                ProteinG = combined.ProteinG + one.ProteinG,
                FatG = combined.FatG + one.FatG,
                CarbohydrateG = combined.CarbohydrateG + one.CarbohydrateG
            };
        }
        return combined;
    }

    private static HarvestResult ResolveOnce(
        Activity activity, Season season, int attribute, Loadout gear, Rng rng,
        float territoryQuality = 1f, Biome? biome = null)
    {
        var encounters = (biome ?? Biome.VancouverIsland).EncountersFor(season, activity);
        if (encounters.Length == 0) return default;

        // Can this even be attempted? Fishing without tackle is not fishing.
        var activityRequirement = activity switch
        {
            Activity.Fishing => ActivityRequirement.Fishing,
            Activity.TrapLine => ActivityRequirement.Trapping,
            Activity.Foraging => ActivityRequirement.Foraging,
            _ => ActivityRequirement.None
        };
        if (activityRequirement != ActivityRequirement.None && !GearEffects.CanPerform(gear, activityRequirement))
            return default;

        float yieldMultiplier = GearEffects.YieldMultiplier(gear, activityRequirement);

        float roll = rng.Stream("harvest.encounter").NextFloat();
        float cumulative = 0f;

        foreach (var encounter in encounters)
        {
            cumulative += encounter.EncounterProbability * yieldMultiplier;
            if (roll >= cumulative) continue;

            // Encountered. Now: do you have what it takes to convert it?
            var need = RequirementFor(encounter.Source, activity);
            if (!GearEffects.CanPerform(gear, need))
                return new HarvestResult { Encountered = true, EncounteredSource = encounter.Source };

            float conversion = BaseConversion(encounter.Source, activity) * SkillMultiplier(attribute);
            bool taken = rng.Stream("harvest.conversion").NextFloat() < conversion;

            return taken
                ? Take(encounter.Source, rng, territoryQuality)
                : new HarvestResult { Encountered = true, EncounteredSource = encounter.Source };
        }

        return default;
    }

    /// <summary>
    /// Turn a successful conversion into an animal, with individual size variance
    /// scaled by how good the ground is.
    ///
    /// Territory quality applies to SIZE rather than to encounter frequency. That
    /// is not a workaround, it is the correct place for it: encounter rates in a
    /// good season already sum close to 1, so a frequency multiplier saturates
    /// and does nothing. Condition, on the other hand, has no ceiling - animals
    /// in good habitat are simply bigger and fatter, which is both true and the
    /// thing a prospecting player is actually looking for.
    /// </summary>
    private static HarvestResult Take(FoodSource source, Rng rng, float territoryQuality = 1f)
    {
        var entry = FoodTable.Get(source);

        float sizeFactor = 0.75f + rng.Stream("harvest.size").NextFloat() * 0.5f;
        float kg = entry.EdibleKg * sizeFactor * MathF.Sqrt(territoryQuality);
        var (protein, fat, carbs) = entry.MacrosForKg(kg);

        // Good ground means animals in good CONDITION, and condition is carried
        // as fat. This matters far more than it looks: a ceiling-limited player
        // gains almost nothing from more food, because the ceiling caps what they
        // can process either way. What they gain from is a better fat-to-protein
        // ratio, which is exactly what a well-fed animal provides. Scaling only
        // size would leave prospecting worthless - measured, it was.
        fat *= territoryQuality * territoryQuality;

        return new HarvestResult
        {
            Encountered = true,
            EncounteredSource = source,
            Source = source,
            EdibleKg = kg,
            ProteinG = protein,
            FatG = fat,
            CarbohydrateG = carbs
        };
    }
}
