using System.Collections.Generic;
using OffTheGrid.Data;
using OffTheGrid.Data.Gear;
using OffTheGrid.Sim.Record;

namespace OffTheGrid.Sim.Contest;

/// <summary>
/// The standard field of ten.
///
/// EVERY contestant spends the same 38 attribute points, differently, across
/// SEVEN attributes. That is
/// deliberate and it is the fairness guarantee: nobody in the field is handed
/// better numbers, only different priorities, which lead to different decisions
/// and therefore different crises.
///
/// GEAR MATCHES THE SHEET. The loadouts were written before gear gated anything,
/// and once it did, several builds were left unable to execute their own
/// strategy - Moss carried Bushcraft 9 and no bow, which closed off a route worth
/// 2.6x a slot entirely, and no amount of shelter skill converts into food.
///
/// The rule applied here: a contestant's four discretionary items must let their
/// BEST attributes actually produce something. A build may still close a route
/// off deliberately - that is a real choice - but only when what remains open is
/// something their sheet is good at.
///
/// AND EVERY CONTESTANT PRIORITISES A FOOD METHOD - at least a 7 in Fishing,
/// Hunting or Foraging. A build with no way to eat is not an interesting
/// archetype, it is a drafting mistake, and the field should not contain one by
/// construction. Nobody arrives on that beach without a plan for eating.
/// </summary>
public static class Roster
{
    /// <summary>Seven attributes, 38 points each. Order: bush, hunt, fish, forage, fit, resolve, cold.</summary>
    private static IReadOnlyDictionary<AttributeKind, int> Spread(
        int bushcraft, int hunting, int fishing, int foraging, int fitness, int resolve, int cold) =>
        new Dictionary<AttributeKind, int>
        {
            [AttributeKind.Bushcraft] = bushcraft,
            [AttributeKind.Hunting] = hunting,
            [AttributeKind.Fishing] = fishing,
            [AttributeKind.Foraging] = foraging,
            [AttributeKind.Fitness] = fitness,
            [AttributeKind.Resolve] = resolve,
            [AttributeKind.ColdAdaptation] = cold
        };

    private static Loadout Kit(params GearItem[] items) => new(items);

    private static readonly GearItem[] Core =
    [
        GearItem.Knife, GearItem.Pot, GearItem.SleepingBag, GearItem.Tarp,
        GearItem.Paracord, GearItem.FerroRod
    ];

    private static Loadout With(params GearItem[] extra)
    {
        var all = new List<GearItem>(Core);
        all.AddRange(extra);
        return new Loadout([.. all]);
    }

    /// <summary>Nine rivals plus a player slot. All spreads total 33 points.</summary>
    public static IReadOnlyList<ContestantSpec> Standard(
        IReadOnlyDictionary<AttributeKind, int>? playerAttributes = null,
        Loadout? playerGear = null,
        float playerWeightKg = 86f,
        float playerBodyFatPercent = 24f) =>
    [
        new ContestantSpec
        {
            Name = "You", IsPlayer = true,
            Attributes = playerAttributes ?? Spread(5, 5, 7, 5, 5, 6, 5),
            Gear = playerGear ?? With(GearItem.Axe, GearItem.FishingLineAndHooks, GearItem.SnareWire, GearItem.BowAndArrows),
            WeightKg = playerWeightKg, BodyFatPercent = playerBodyFatPercent
        },

        new ContestantSpec { Name = "Dana",   Personality = Personality.AggressiveHunter,
            Attributes = Spread(4, 9, 4, 3, 7, 6, 5), WeightKg = 90, BodyFatPercent = 22,
            // Hunting 9. Bow is mandatory; the saw traded for tackle so a bad hunting week is survivable.
            // Hunting 8, Fitness 9. Light and mobile - bow, snares, a line, and just enough axe.
            Gear = With(GearItem.BowAndArrows, GearItem.SnareWire, GearItem.FishingLineAndHooks, GearItem.Axe) },

        new ContestantSpec { Name = "Moss",   Personality = Personality.PatientBuilder,
            Attributes = Spread(9, 3, 7, 4, 4, 6, 5), WeightKg = 88, BodyFatPercent = 27,
            // Bushcraft 9, Hunting 4. Deliberately no bow - but a builder who can maintain a net gets a food route his sheet is actually good at.
            Gear = With(GearItem.Axe, GearItem.Saw, GearItem.Gillnet, GearItem.SnareWire) },

        new ContestantSpec { Name = "Wren",   Personality = Personality.SteadyProvider,
            Attributes = Spread(5, 4, 8, 4, 4, 8, 5), WeightKg = 82, BodyFatPercent = 25,
            // Resolve 8, balanced sheet. Steady kit.
            Gear = With(GearItem.Gillnet, GearItem.SnareWire, GearItem.Axe, GearItem.FishingLineAndHooks) },

        new ContestantSpec { Name = "Cobb",   Personality = Personality.ConservativeRester,
            Attributes = Spread(6, 7, 4, 4, 3, 8, 6), WeightKg = 104, BodyFatPercent = 33,
            // Resolve 9, Fitness 3. Passive income and a warm camp - the trap line and the log shelter.
            Gear = With(GearItem.SnareWire, GearItem.FishingLineAndHooks, GearItem.Axe, GearItem.Saw) },

        new ContestantSpec { Name = "Ilse",   Personality = Personality.SteadyProvider, Sex = Sex.Female,
            Attributes = Spread(5, 4, 5, 9, 4, 6, 5), WeightKg = 74, BodyFatPercent = 30,
            // Foraging 8 needs no gear, so the discretionary slots go to the routes that do.
            Gear = With(GearItem.Gillnet, GearItem.SnareWire, GearItem.Axe, GearItem.FishingLineAndHooks) },

        new ContestantSpec { Name = "Tapio",  Personality = Personality.PatientBuilder,
            Attributes = Spread(7, 4, 7, 3, 4, 5, 8), WeightKg = 92, BodyFatPercent = 26,
            // Bushcraft 7, Cold Adaptation 8. Builds the shelter that makes his cold resistance count, and nets rather than lines.
            Gear = With(GearItem.Axe, GearItem.Saw, GearItem.Gillnet, GearItem.SnareWire) },

        new ContestantSpec { Name = "Rhodes", Personality = Personality.AggressiveHunter,
            Attributes = Spread(3, 8, 4, 4, 9, 6, 4), WeightKg = 76, BodyFatPercent = 15,
            // Hunting 8, Fitness 9. Light and mobile - bow, snares, a line, and just enough axe.
            Gear = With(GearItem.BowAndArrows, GearItem.SnareWire, GearItem.FishingLineAndHooks, GearItem.Axe) },

        new ContestantSpec { Name = "Marta",  Personality = Personality.ConservativeRester, Sex = Sex.Female,
            Attributes = Spread(5, 4, 4, 8, 3, 8, 6), WeightKg = 79, BodyFatPercent = 34,
            // Resolve 8, Hunting 3. No bow is right for the sheet - so both fishing options instead.
            Gear = With(GearItem.SnareWire, GearItem.Gillnet, GearItem.FishingLineAndHooks, GearItem.Axe) },

        new ContestantSpec { Name = "Osei",   Personality = Personality.SteadyProvider,
            Attributes = Spread(4, 6, 7, 5, 5, 6, 5), WeightKg = 85, BodyFatPercent = 21,
            // Hunting 7, Foraging 6. The all-rounder, and kitted like one.
            Gear = With(GearItem.BowAndArrows, GearItem.FishingLineAndHooks, GearItem.SnareWire, GearItem.Axe) },
    ];
}
