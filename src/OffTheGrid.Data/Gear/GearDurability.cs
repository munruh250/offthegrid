using System;
using System.Collections.Generic;

namespace OffTheGrid.Data.Gear;

/// <param name="Item">Which item.</param>
/// <param name="Uses">Uses before it fails. A "use" is one slot of the work it does.</param>
/// <param name="RepairSlots">Slots to bring it back from failing. Zero means unrepairable.</param>
/// <param name="NeedsCordageToRepair">Whether repair also consumes cordage.</param>
/// <param name="WarnAtFraction">Remaining fraction at which the player is told.</param>
public readonly record struct GearDurabilityEntry(
    GearItem Item,
    int Uses,
    int RepairSlots,
    bool NeedsCordageToRepair,
    float WarnAtFraction);

/// <summary>
/// How long each item lasts, in USES rather than a percentage.
///
/// The readout question was "number or descriptive text", and the answer is
/// neither as posed: a percentage does not answer what the player actually wants
/// to know, which is "will this last the week?" Uses remaining does - "about 40
/// shots left in this string" - and it is field-craft rather than a health bar.
///
/// Balance doc 7 already priced two of these (bow 120 self-made / 400 as a gear
/// item, rebuild 4 slots). The rest extend that with the same logic: things that
/// take load fail, things that only get carried do not.
/// </summary>
public static class GearDurability
{
    private static readonly Dictionary<GearItem, GearDurabilityEntry> entries = new()
    {
        // --- takes real load; will fail in a run ---
        [GearItem.BowAndArrows] = new(GearItem.BowAndArrows,
            Uses: 400, RepairSlots: 2, NeedsCordageToRepair: true, WarnAtFraction: 0.25f),

        [GearItem.Gillnet] = new(GearItem.Gillnet,
            Uses: 220, RepairSlots: 2, NeedsCordageToRepair: true, WarnAtFraction: 0.30f),

        [GearItem.SnareWire] = new(GearItem.SnareWire,
            Uses: 260, RepairSlots: 1, NeedsCordageToRepair: false, WarnAtFraction: 0.25f),

        [GearItem.FishingLineAndHooks] = new(GearItem.FishingLineAndHooks,
            Uses: 300, RepairSlots: 1, NeedsCordageToRepair: false, WarnAtFraction: 0.25f),

        // --- edge tools: dull and chip rather than snap ---
        [GearItem.Axe] = new(GearItem.Axe,
            Uses: 520, RepairSlots: 1, NeedsCordageToRepair: false, WarnAtFraction: 0.20f),

        [GearItem.Saw] = new(GearItem.Saw,
            Uses: 430, RepairSlots: 1, NeedsCordageToRepair: false, WarnAtFraction: 0.20f),

        [GearItem.Knife] = new(GearItem.Knife,
            Uses: 900, RepairSlots: 1, NeedsCordageToRepair: false, WarnAtFraction: 0.15f),

        // --- exposure wears these, not use ---
        [GearItem.SleepingBag] = new(GearItem.SleepingBag,
            Uses: 380, RepairSlots: 2, NeedsCordageToRepair: true, WarnAtFraction: 0.25f),

        [GearItem.Tarp] = new(GearItem.Tarp,
            Uses: 300, RepairSlots: 1, NeedsCordageToRepair: true, WarnAtFraction: 0.25f),

        // --- consumed rather than worn ---
        [GearItem.Paracord] = new(GearItem.Paracord,
            Uses: 180, RepairSlots: 0, NeedsCordageToRepair: false, WarnAtFraction: 0.30f),

        // --- effectively permanent ---
        [GearItem.Pot] = new(GearItem.Pot,
            Uses: 4000, RepairSlots: 1, NeedsCordageToRepair: false, WarnAtFraction: 0.10f),

        [GearItem.FerroRod] = new(GearItem.FerroRod,
            Uses: 3000, RepairSlots: 0, NeedsCordageToRepair: false, WarnAtFraction: 0.15f),

        [GearItem.Photograph] = new(GearItem.Photograph,
            Uses: 9999, RepairSlots: 0, NeedsCordageToRepair: false, WarnAtFraction: 0.05f),
    };

    public static GearDurabilityEntry Get(GearItem item) =>
        entries.TryGetValue(item, out var e)
            ? e
            : new GearDurabilityEntry(item, 9999, 0, false, 0.1f);

    public static IReadOnlyCollection<GearDurabilityEntry> All => entries.Values;

    /// <summary>Which items a slot of this work wears.</summary>
    public static IReadOnlyList<GearItem> WornBy(string activityName) => activityName switch
    {
        "HuntingStalk" => [GearItem.BowAndArrows],
        "Fishing" => [GearItem.Gillnet, GearItem.FishingLineAndHooks],
        "TrapLine" => [GearItem.SnareWire],
        "ChoppingWood" => [GearItem.Axe],
        "Sawing" => [GearItem.Saw],
        "ShelterBuild" => [GearItem.Axe, GearItem.Paracord],
        "BuildCamp" => [GearItem.Axe, GearItem.Paracord],
        "Foraging" => [GearItem.Knife],
        "PreserveFood" => [GearItem.Knife],
        _ => []
    };
}
