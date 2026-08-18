using System;
using System.Collections.Generic;

namespace OffTheGrid.Data.Gear;

/// <summary>
/// The item list contestants choose ten from. Design spec 4.4.
///
/// Only mechanically load-bearing items are modelled. The full list is longer and
/// includes flavour picks, but these are the ones that gate or multiply an
/// activity - which is the point of the mechanic. A loadout that changes nothing
/// is not a decision.
/// </summary>
public enum GearItem
{
    // --- Cutting and building ---
    Axe,
    Saw,
    Knife,

    // --- Hunting ---
    BowAndArrows,
    SnareWire,

    // --- Fishing ---
    Gillnet,
    FishingLineAndHooks,

    // --- Camp ---
    Pot,
    SleepingBag,
    Tarp,
    Paracord,
    FerroRod
}

/// <summary>
/// A contestant's ten items.
///
/// This is the show's signature decision and the primary determinant of what food
/// is actually reachable: a gillnet in a salmon creek is a different game from a
/// line and hook, and without a bow, big game is theoretical. Before this existed
/// every contestant had identical access to the landscape, which made the
/// archetype comparison meaningless - they differed on paper and played the same.
/// </summary>
public sealed class Loadout
{
    public const int MaxItems = 10;

    private readonly HashSet<GearItem> items;

    public Loadout(params GearItem[] chosen)
    {
        items = [.. chosen];
        if (items.Count > MaxItems)
            throw new ArgumentException($"a loadout is at most {MaxItems} items, got {items.Count}");
    }

    public bool Has(GearItem item) => items.Contains(item);
    public int Count => items.Count;
    public IReadOnlyCollection<GearItem> Items => items;

    /// <summary>Insulation from carried gear. Balance doc 5.2 puts the bag at +1.5 clo.</summary>
    public float ClothingClo => 1.5f + (Has(GearItem.SleepingBag) ? 1.5f : 0f);

    /// <summary>Some cordage is needed for most shelter work; paracord removes the slot tax.</summary>
    public bool HasCordage => Has(GearItem.Paracord);

    /// <summary>A generalist kit that can do a bit of everything. Used as a default in tests.</summary>
    public static Loadout Standard { get; } = new(
        GearItem.Axe, GearItem.Saw, GearItem.Knife,
        GearItem.BowAndArrows, GearItem.SnareWire,
        GearItem.FishingLineAndHooks, GearItem.Pot,
        GearItem.SleepingBag, GearItem.Tarp, GearItem.Paracord);
}
