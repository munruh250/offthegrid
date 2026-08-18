using System;
using System.Collections.Generic;

namespace OffTheGrid.Data.Tables;

public enum PreservationMethod
{
    None,
    SmokeRack,
    DryingRack,
    CachePit
}

/// <param name="Method">Which method.</param>
/// <param name="KgPerSlot">Throughput - kg processed per action slot.</param>
/// <param name="LossFraction">Fraction lost in processing.</param>
/// <param name="ShelfLifeDays">How long the result keeps.</param>
public readonly record struct PreservationEntry(
    PreservationMethod Method,
    float KgPerSlot,
    float LossFraction,
    int ShelfLifeDays);

/// <summary>
/// Preservation, transcribed from balance doc 4.
///
/// Balance doc 8 names this the TOP tuning lever: "Never tune raw kcal content of
/// animals - cap what can be preserved instead." It is what compresses a moose
/// from 300x a trout down to 7-10x, and it is what gives every big kill a real
/// constraint rather than a free win.
///
/// Doc 4 works the example: processing a 121 kg elk on a default smoke rack takes
/// ~15 slots, three to five full days of doing nothing else, while the rest rots.
/// Realistically the player banks 30-50 kg and loses the remainder.
/// </summary>
public static class PreservationTable
{
    private static readonly Dictionary<PreservationMethod, PreservationEntry> entries = new()
    {
        [PreservationMethod.SmokeRack]  = new(PreservationMethod.SmokeRack,   8f,  0.15f, 20),
        [PreservationMethod.DryingRack] = new(PreservationMethod.DryingRack, 12f,  0.25f, 30),
        [PreservationMethod.CachePit]   = new(PreservationMethod.CachePit,   20f,  0.05f, 12)
    };

    public static PreservationEntry Get(PreservationMethod method) => entries[method];

    public static IReadOnlyCollection<PreservationEntry> All => entries.Values;

    /// <summary>Slots needed to process a given mass with a method.</summary>
    public static float SlotsToProcess(PreservationMethod method, float kg) =>
        kg / entries[method].KgPerSlot;

    /// <summary>Mass actually banked after processing losses.</summary>
    public static float YieldAfterLoss(PreservationMethod method, float kg) =>
        kg * (1f - entries[method].LossFraction);
}
