using System.Collections.Generic;
using OffTheGrid.Data;
using OffTheGrid.Data.Gear;
using OffTheGrid.Sim.Record;

namespace OffTheGrid.Sim.Contest;

/// <summary>
/// The standard field of ten.
///
/// EVERY contestant spends the same 33 attribute points, differently. That is
/// deliberate and it is the fairness guarantee: nobody in the field is handed
/// better numbers, only different priorities, which lead to different decisions
/// and therefore different crises.
/// </summary>
public static class Roster
{
    private static IReadOnlyDictionary<AttributeKind, int> Spread(
        int bushcraft, int hunting, int foraging, int fitness, int resolve, int cold) =>
        new Dictionary<AttributeKind, int>
        {
            [AttributeKind.Bushcraft] = bushcraft,
            [AttributeKind.Hunting] = hunting,
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
            Attributes = playerAttributes ?? Spread(5, 6, 5, 5, 7, 5),
            Gear = playerGear ?? With(GearItem.Axe, GearItem.FishingLineAndHooks, GearItem.SnareWire, GearItem.BowAndArrows),
            WeightKg = playerWeightKg, BodyFatPercent = playerBodyFatPercent
        },

        new ContestantSpec { Name = "Dana",   Personality = Personality.AggressiveHunter,
            Attributes = Spread(4, 9, 3, 7, 5, 5), WeightKg = 90, BodyFatPercent = 22,
            Gear = With(GearItem.BowAndArrows, GearItem.Axe, GearItem.SnareWire, GearItem.Saw) },

        new ContestantSpec { Name = "Moss",   Personality = Personality.PatientBuilder,
            Attributes = Spread(9, 4, 5, 4, 6, 5), WeightKg = 88, BodyFatPercent = 27,
            Gear = With(GearItem.Axe, GearItem.Saw, GearItem.SnareWire, GearItem.FishingLineAndHooks) },

        new ContestantSpec { Name = "Wren",   Personality = Personality.SteadyProvider,
            Attributes = Spread(5, 6, 4, 5, 8, 5), WeightKg = 82, BodyFatPercent = 25,
            Gear = With(GearItem.Gillnet, GearItem.Axe, GearItem.SnareWire, GearItem.FishingLineAndHooks) },

        new ContestantSpec { Name = "Cobb",   Personality = Personality.ConservativeRester,
            Attributes = Spread(6, 4, 5, 3, 9, 6), WeightKg = 104, BodyFatPercent = 33,
            Gear = With(GearItem.Axe, GearItem.SnareWire, GearItem.FishingLineAndHooks, GearItem.Saw) },

        new ContestantSpec { Name = "Ilse",   Personality = Personality.SteadyProvider, Sex = Sex.Female,
            Attributes = Spread(6, 5, 8, 4, 6, 4), WeightKg = 74, BodyFatPercent = 30,
            Gear = With(GearItem.FishingLineAndHooks, GearItem.SnareWire, GearItem.Axe, GearItem.Gillnet) },

        new ContestantSpec { Name = "Tapio",  Personality = Personality.PatientBuilder,
            Attributes = Spread(7, 5, 4, 5, 4, 8), WeightKg = 92, BodyFatPercent = 26,
            Gear = With(GearItem.Axe, GearItem.Saw, GearItem.FishingLineAndHooks, GearItem.SnareWire) },

        new ContestantSpec { Name = "Rhodes", Personality = Personality.AggressiveHunter,
            Attributes = Spread(3, 8, 4, 9, 5, 4), WeightKg = 76, BodyFatPercent = 15,
            Gear = With(GearItem.BowAndArrows, GearItem.SnareWire, GearItem.Axe, GearItem.FishingLineAndHooks) },

        new ContestantSpec { Name = "Marta",  Personality = Personality.ConservativeRester, Sex = Sex.Female,
            Attributes = Spread(6, 3, 6, 4, 8, 6), WeightKg = 79, BodyFatPercent = 34,
            Gear = With(GearItem.SnareWire, GearItem.FishingLineAndHooks, GearItem.Axe, GearItem.Pot) },

        new ContestantSpec { Name = "Osei",   Personality = Personality.SteadyProvider,
            Attributes = Spread(5, 7, 6, 6, 5, 4), WeightKg = 85, BodyFatPercent = 21,
            Gear = With(GearItem.FishingLineAndHooks, GearItem.SnareWire, GearItem.Axe, GearItem.BowAndArrows) },
    ];
}
