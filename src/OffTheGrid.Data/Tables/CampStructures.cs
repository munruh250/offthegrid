using System;
using System.Collections.Generic;

namespace OffTheGrid.Data.Tables;

/// <summary>Things you can build at camp beyond the shelter itself.</summary>
public enum CampStructure
{
    /// <summary>A hung bag or a raised platform. Cheap, small, keeps animals off.</summary>
    LightCache,

    /// <summary>Dug and lined. More room, better hidden, but the food still ages.</summary>
    CachePit,

    /// <summary>Smokes meat. Slow throughput, good keeping, modest loss.</summary>
    SmokeRack,

    /// <summary>Air-dries. Faster throughput, longest keeping, highest loss.</summary>
    DryingRack,

    /// <summary>
    /// A cache dug into shade and snow. Useless while it is warm and unbeatable
    /// once it is not - below freezing the food simply stops ageing.
    /// </summary>
    ColdCache
}

/// <param name="Structure">Which structure.</param>
/// <param name="BuildSlots">Slots to build, before body capacity is applied.</param>
/// <param name="CapacityKg">Storage it adds.</param>
/// <param name="ProcessKgPerSlot">Raw mass it can convert to preserved per slot. Zero if it does not process.</param>
/// <param name="ProcessLoss">Fraction lost in processing.</param>
/// <param name="ShelfLifeDays">How long what it holds keeps.</param>
/// <param name="PredatorProtection">0 to 1. Reduces the chance a raid finds the cache.</param>
/// <param name="RequiresFreezing">Only functions once nights are below freezing.</param>
public readonly record struct CampStructureEntry(
    CampStructure Structure,
    int BuildSlots,
    float CapacityKg,
    float ProcessKgPerSlot,
    float ProcessLoss,
    int ShelfLifeDays,
    float PredatorProtection,
    bool RequiresFreezing);

/// <summary>
/// Camp structures. Extends balance doc 4, which prices preservation THROUGHPUT
/// and shelf life but never gave the player anything to build.
///
/// The design point: preservation is doc 8's top tuning lever, and a lever the
/// player cannot choose to pull is not a lever. Each structure trades build slots
/// now against food kept later, and they are genuinely different bets - the
/// drying rack is fast and wasteful, the smoke rack slow and careful, and the
/// cold cache is worthless in September and unbeatable in November.
/// </summary>
public static class CampStructures
{
    private static readonly Dictionary<CampStructure, CampStructureEntry> entries = new()
    {
        [CampStructure.LightCache] = new(CampStructure.LightCache,
            BuildSlots: 2, CapacityKg: 10f, ProcessKgPerSlot: 0f, ProcessLoss: 0f,
            ShelfLifeDays: 6, PredatorProtection: 0.45f, RequiresFreezing: false),

        [CampStructure.CachePit] = new(CampStructure.CachePit,
            BuildSlots: 3, CapacityKg: 22f, ProcessKgPerSlot: 0f, ProcessLoss: 0.05f,
            ShelfLifeDays: 12, PredatorProtection: 0.70f, RequiresFreezing: false),

        [CampStructure.SmokeRack] = new(CampStructure.SmokeRack,
            BuildSlots: 4, CapacityKg: 14f, ProcessKgPerSlot: 8f, ProcessLoss: 0.15f,
            ShelfLifeDays: 20, PredatorProtection: 0.30f, RequiresFreezing: false),

        [CampStructure.DryingRack] = new(CampStructure.DryingRack,
            BuildSlots: 3, CapacityKg: 12f, ProcessKgPerSlot: 12f, ProcessLoss: 0.25f,
            ShelfLifeDays: 30, PredatorProtection: 0.20f, RequiresFreezing: false),

        [CampStructure.ColdCache] = new(CampStructure.ColdCache,
            BuildSlots: 5, CapacityKg: 34f, ProcessKgPerSlot: 0f, ProcessLoss: 0f,
            ShelfLifeDays: 120, PredatorProtection: 0.80f, RequiresFreezing: true),
    };

    public static CampStructureEntry Get(CampStructure structure) => entries[structure];
    public static IReadOnlyCollection<CampStructureEntry> All => entries.Values;

    /// <summary>Night temperature at or below which a cold cache works.</summary>
    public const float FreezingThresholdC = 1.0f;
}
