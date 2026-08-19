using System;
using System.Collections.Generic;
using OffTheGrid.Data;
using OffTheGrid.Data.Balance;
using OffTheGrid.Sim.Body;
using OffTheGrid.Sim.Events;
using OffTheGrid.Sim.Fire;
using OffTheGrid.Sim.Food;
using OffTheGrid.Sim.Logging;
using OffTheGrid.Sim.Morale;
using OffTheGrid.Sim.Nutrition;
using OffTheGrid.Sim.Record;
using OffTheGrid.Data.Gear;
using OffTheGrid.Data.Tables;
using OffTheGrid.Sim.Time;

namespace OffTheGrid.Sim;

/// <summary>How one day was spent. The caller fills this from player input or rival AI.</summary>
public sealed class DayPlan
{
    /// <summary>Activity for each available slot. Extra entries beyond the day's slots are ignored.</summary>
    public required IReadOnlyList<Activity> Slots { get; init; }

    /// <summary>
    /// Food handed directly to the player, bypassing harvesting. Test and
    /// scenario use only - real play fills the larder by working slots.
    /// </summary>
    public Macros? DirectRation { get; init; }

    public float TerrainMultiplier { get; init; } = 1.0f;
    public bool ShelterInadequate { get; init; }
    public bool SoakedAtSleep { get; init; }
    public bool HasPhoto { get; init; }
}

/// <summary>Result of stepping one day.</summary>
public readonly record struct DayResult
{
    public int DayNumber { get; init; }
    public int SlotsAvailable { get; init; }
    public float BurnKcal { get; init; }
    public AcuteEventKind Event { get; init; }
    public float FireQuality { get; init; }
    public float WoodKg { get; init; }
    public float LocalDepletion { get; init; }
    public bool RelocationAvailable { get; init; }
    public bool Collapsed { get; init; }
    public float RawKg { get; init; }
    public float PreservedKg { get; init; }
    public float HarvestedKg { get; init; }
    public float LarderDaysOfFood { get; init; }
    public bool FoodInsecure { get; init; }
    public float UsableIntakeKcal { get; init; }
    public float WastedIntakeKcal { get; init; }
    public float NetKcal { get; init; }
    public bool ProteinCeilingBound { get; init; }
    public MoraleDayTotals Morale { get; init; }
    public EndCondition EndCondition { get; init; }
}

/// <summary>
/// One contestant's run: body, morale, and the day loop that couples them.
///
/// This is the headless entry point. The game drives it a day at a time and
/// projects snapshots for display; the solver drives it in a tight loop and never
/// projects at all. Rivals run this same class at lower fidelity - same physics,
/// expected-value inputs (B8).
///
/// Mutable by design, per A15. See BodyState for the reasoning.
/// </summary>
public sealed class Run
{
    private readonly BalanceProvider balance;
    private readonly int fitness;
    private readonly IReadOnlyDictionary<AttributeKind, int> attributes;

    public Run(
        ulong seed,
        Sex sex,
        float heightCm,
        int ageYears,
        float weightKg,
        float bodyFatPercent,
        IReadOnlyDictionary<AttributeKind, int> attributes,
        Loadout? gear = null,
        BalanceProvider? balanceProvider = null,
        Biome? biome = null,
        SeasonSchedule? schedule = null)
    {
        Gear = gear ?? Loadout.Standard;
        Biome = biome ?? Biome.VancouverIsland;
        Schedule = schedule ?? SeasonSchedule.Standard;
        balance = balanceProvider ?? new BalanceProvider();
        var b = balance.Current;

        Rng = new Rng(seed);
        Log = new SimLog();
        Body = new BodyState(sex, heightCm, ageYears, weightKg, bodyFatPercent);
        Morale = new MoraleState(attributes[AttributeKind.Resolve], b);

        fitness = attributes[AttributeKind.Fitness];
        this.attributes = attributes;
        Larder = new Larder();

        // Drop-point lottery. Spec 8.2 is explicit that a poor drop is a real and
        // fair outcome; it is what makes exploration a strategy rather than
        // fog-clearing busywork.
        TerritoryQuality = 0.7f + Rng.Stream("world.droppoint").NextFloat() * 0.6f;

        Record = new RunRecord
        {
            Seed = seed,
            Sex = sex,
            StartWeightKg = weightKg,
            StartBodyFatPercent = bodyFatPercent,
            HeightCm = heightCm,
            AgeYears = ageYears,
            Attributes = attributes
        };
    }

    /// <summary>Edible mass that counts as a "large food success". A good salmon or better.</summary>
    /// <summary>Edible mass at which a catch delivers the full morale reward.</summary>
    private const float LargeCatchKg = 12f;

    public Rng Rng { get; }
    public SimLog Log { get; }
    public BodyState Body { get; }
    public MoraleState Morale { get; }
    public RunRecord Record { get; }
    public Larder Larder { get; }

    /// <summary>The ten items. Design spec 4.4.</summary>
    public Loadout Gear { get; }

    /// <summary>Where this run is taking place.</summary>
    public Biome Biome { get; }

    /// <summary>When the seasons turn for this run.</summary>
    public SeasonSchedule Schedule { get; }

    /// <summary>
    /// True once the shelter and fire can hold the coldest night this run will
    /// see. This - not a day count - is the arc the player is racing.
    /// </summary>
    public bool IsWinterized =>
        AvailableClo + Fire.Firewood.FireClo(1f)
        >= ShelterTable.CloDemandForNightTemp(Biome.NightTemperature(Schedule.WinterArrives, Schedule))
           - 0.14f * (attributes[AttributeKind.ColdAdaptation] - 5);

    /// <summary>
    /// How worked-out the ground around camp is, 1.0 fresh down to near zero.
    /// Design spec 8.2: animals live on the map and hunted tiles thin out. This
    /// is what eventually forces a move, and it is the pressure doc 12's
    /// relocation triggers were written against.
    /// </summary>
    public float LocalDepletion { get; private set; } = 1f;

    /// <summary>Effective richness of the ground actually being worked.</summary>
    public float EffectiveTerritory => TerritoryQuality * LocalDepletion;

    /// <summary>Consecutive days the food trigger has been satisfied. Doc 12 s2.1.</summary>
    public int FoodTriggerDays { get; private set; }

    /// <summary>Consecutive nights the shelter trigger has been satisfied. Doc 12 s2.2.</summary>
    public int ShelterTriggerDays { get; private set; }

    /// <summary>Times this run has relocated.</summary>
    public int Relocations { get; private set; }

    /// <summary>
    /// Whether relocation is currently available. Doc 12: relocation requires an
    /// ACTIVE TRIGGER - it is not available on demand, which is the primary
    /// guard against farming its net-positive morale cycle.
    /// </summary>
    public bool CanRelocate(BalanceData b) =>
        FoodTriggerDays >= b.TriggerConfirmDays || ShelterTriggerDays >= b.TriggerConfirmDays;

    /// <summary>
    /// Move camp. Doc 12: lose the shelter entirely, carry one cache at most,
    /// arrive on fresh ground. The morale hit scales with what was abandoned and
    /// is capped below the rebuild reward so the cycle is never a death spiral.
    /// </summary>
    public bool Relocate(BalanceData b)
    {
        if (IsOver || !CanRelocate(b)) return false;

        float hit = Math.Min(
            b.ShelterLossMoralePerSlot * ShelterTable.Get(Shelter).Slots,
            b.ShelterLossMoraleCap);
        Morale.ApplyEvent(MoraleSource.ShelterLost, -hit, b);

        // Carry capacity is roughly one cache pit's worth - the sharpest decision
        // in the system, and it falls straight out of bodyweight.
        float carryKg = Body.WeightKg * b.CarryFractionOfBodyweight;
        Larder.TrimTo(carryKg);

        // Cordage survives only under the softened variant.
        if (b.RelocationVariant == RelocationVariant.TotalLoss) WoodKg = 0f;

        Shelter = ShelterTier.None;
        ShelterProgressSlots = 0f;
        Larder.CapacityKg = Larder.CapacityFor(0, attributes[AttributeKind.Bushcraft]);

        LocalDepletion = 1f;
        FoodTriggerDays = 0;
        ShelterTriggerDays = 0;
        Relocations++;

        Record.Trace.Add(new TraceEntry
        {
            Day = DayNumber, Slot = 0, Kind = TraceKind.Relocation,
            Code = "relocation.moved", Magnitude = hit
        });
        return true;
    }

    /// <summary>
    /// What a work slot actually returns, combining trained Fitness with the
    /// lean mass to apply it. An attribute without a body behind it is a claim,
    /// not a capability - and this is what stops "minimise muscle, maximise fat"
    /// being a free win.
    /// </summary>
    public float WorkCapacity =>
        Body.PhysicalCapacity * (0.72f + 0.056f * attributes[AttributeKind.Fitness]);

    /// <summary>Camp structure currently under construction, if any.</summary>
    public CampStructure? BuildingNow { get; private set; }

    private float campProgressSlots;

    /// <summary>
    /// Choose what to build next at camp. Ordered by what the run needs: a light
    /// cache first because it is cheap and keeps animals off, then something that
    /// PROCESSES, and finally the cold cache once the nights will support it.
    /// </summary>
    private CampStructure? NextStructure()
    {
        bool freezingSoon = Biome.NightTemperature(
            Math.Min(DayNumber + 10, Schedule.WinterArrives), Schedule) <= CampStructures.FreezingThresholdC;

        if (!Larder.Has(CampStructure.LightCache)) return CampStructure.LightCache;
        if (!Larder.Has(CampStructure.DryingRack)) return CampStructure.DryingRack;
        if (!Larder.Has(CampStructure.SmokeRack)) return CampStructure.SmokeRack;
        if (freezingSoon && !Larder.Has(CampStructure.ColdCache)) return CampStructure.ColdCache;
        if (!Larder.Has(CampStructure.CachePit)) return CampStructure.CachePit;
        return null;
    }

    private void AdvanceCampStructure(BalanceData b)
    {
        BuildingNow ??= NextStructure();
        if (BuildingNow is null) return;

        var entry = CampStructures.Get(BuildingNow.Value);
        campProgressSlots += WorkCapacity;
        if (campProgressSlots < entry.BuildSlots) return;

        campProgressSlots -= entry.BuildSlots;
        Larder.AddStructure(BuildingNow.Value);
        Record.Trace.Add(new TraceEntry
        {
            Day = DayNumber, Slot = 0, Kind = TraceKind.Action,
            Code = $"camp.{BuildingNow.Value}".ToLowerInvariant(),
            Magnitude = entry.CapacityKg
        });
        BuildingNow = null;
    }

    /// <summary>Body condition below which pushing movement work risks collapse.</summary>
    public const float CollapseConditionThreshold = 0.28f;

    /// <summary>True if the player blacked out today. Costs the day, never the run.</summary>
    public bool Collapsed { get; private set; }

    /// <summary>How many times this run has ended in a blackout.</summary>
    public int CollapseCount { get; private set; }

    /// <summary>
    /// Chance of collapsing if the player pushes movement work today. Surfaced so
    /// the UI can WARN before the slots are committed - the decision only counts
    /// as a decision if the risk was visible.
    /// </summary>
    public float CollapseRiskIfPushing(int movementSlots)
    {
        if (BodyConditionRatio >= CollapseConditionThreshold || movementSlots <= 0) return 0f;
        float shortfall = 1f - BodyConditionRatio / CollapseConditionThreshold;
        return 0.30f * shortfall * MathF.Min(1f, movementSlots / 3f);
    }

    /// <summary>Accumulated injury. Design spec 5.5's incident end condition.</summary>
    public float InjuryBurden { get; private set; }

    /// <summary>Firewood in hand, kg. Balance doc 4.</summary>
    public float WoodKg { get; private set; }

    /// <summary>How well last night's fire was fed, 0 to 1.</summary>
    public float LastFireQuality { get; private set; } = 1f;

    /// <summary>
    /// How good the ground around camp is, as a multiplier on encounter rates.
    /// Design spec 8.2: animals live on the map, your drop point may genuinely be
    /// poor, and a good area exists somewhere. Exploring finds it.
    /// </summary>
    public float TerritoryQuality { get; private set; }

    /// <summary>Shelter built so far. Drives clo, and its milestones drive morale.</summary>
    public ShelterTier Shelter { get; private set; } = ShelterTier.None;

    /// <summary>Slots invested toward the next tier.</summary>
    public float ShelterProgressSlots { get; private set; }

    /// <summary>Comfort projects finished. Spec 5.6 puts each at +14 morale.</summary>
    public int ComfortProjectsCompleted { get; private set; }

    private float comfortProgressSlots;

    /// <summary>Slots of whittling per finished comfort project.</summary>
    private const float SlotsPerComfortProject = 3f;

    /// <summary>
    /// Spend a slot on a comfort project - a spoon, a chair, a carving.
    ///
    /// This is the player's only REPEATABLE morale income. Shelter milestones run
    /// out after six tiers and large-food successes are luck; comfort projects are
    /// the one source a player can choose to generate, which is exactly why spec
    /// 5.6 prices them at +14 and why balance doc 7.3's competent runs depend on
    /// them. Without this, morale is a one-way slide and even perfect play taps
    /// out in the forties.
    /// </summary>
    private void AdvanceComfortProject(BalanceData b)
    {
        comfortProgressSlots += Harvest.SkillMultiplier(attributes[AttributeKind.Bushcraft]);
        if (comfortProgressSlots < SlotsPerComfortProject) return;

        comfortProgressSlots -= SlotsPerComfortProject;
        ComfortProjectsCompleted++;

        // Spec 4.1: Resolve governs "morale gained per comfort project". Another
        // clause that was specified and never implemented.
        float payout = b.MoraleProjectCompleted * (0.75f + 0.05f * attributes[AttributeKind.Resolve]);
        Morale.ApplyEvent(MoraleSource.ComfortProject, payout, b);
        Record.Trace.Add(new TraceEntry
        {
            Day = DayNumber, Slot = 0, Kind = TraceKind.MoraleEvent,
            Code = "project.comfort", Magnitude = b.MoraleProjectCompleted
        });
    }

    /// <summary>
    /// How far the body is from the medical floor: 1 at full condition, 0 at the
    /// pull threshold. Feeds the tap-out decision as well as the pull check.
    /// </summary>
    public float BodyConditionRatio
    {
        get
        {
            float floor = Body.Sex.MedicalPullBodyFatPercent();
            float start = Record.StartBodyFatPercent;
            if (start <= floor) return 0f;
            return Math.Clamp((Body.BodyFatPercent - floor) / (start - floor), 0f, 1f);
        }
    }

    /// <summary>Insulation available at night: clothing, bag, and whatever is built.</summary>
    public float AvailableClo => Gear.ClothingClo + ShelterTable.Get(Shelter).Clo;

    /// <summary>
    /// Clo demand after Cold Adaptation. Design spec 4.1 gives the attribute a
    /// "thermoneutral offset" - it was previously read NOWHERE in the sim, making
    /// it the one attribute with no effect at all.
    /// </summary>
    public float CloDemandTonight(int dayNumber)
    {
        float raw = ShelterTable.CloDemandForNightTemp(NightTempForDay(dayNumber));
        float offset = 0.14f * (attributes[AttributeKind.ColdAdaptation] - 5);
        return Math.Max(0f, raw - offset);
    }

    /// <summary>
    /// Spend a slot on shelter. Bushcraft makes the slot worth more, which is
    /// where that attribute finally earns its keep.
    /// </summary>
    private void AdvanceShelter(BalanceData b)
    {
        var next = NextTier(Shelter);
        if (next == Shelter) return;   // cabin is the ceiling

        // You cannot build what your kit cannot cut. Balance doc 5: the log
        // shelter needs an axe AND a saw.
        if (next > GearEffects.MaxShelterTier(Gear)) return;

        float bushcraft = attributes[AttributeKind.Bushcraft];
        ShelterProgressSlots += Harvest.SkillMultiplier((int)bushcraft);

        int needed = ShelterTable.Get(next).Slots - ShelterTable.Get(Shelter).Slots;
        if (ShelterProgressSlots < needed) return;

        ShelterProgressSlots -= needed;
        Shelter = next;

        // A better camp stores more. This is what turns preservation into the
        // lever doc 8 claims it is.


        // Spec 5.6: shelter tier milestone is +12.
        Morale.ApplyEvent(MoraleSource.ShelterMilestone, b.MoraleShelterMilestone, b);
        Record.Trace.Add(new TraceEntry
        {
            Day = DayNumber, Slot = 0, Kind = TraceKind.Action,
            Code = $"shelter.{next}".ToLowerInvariant(),
            Magnitude = ShelterTable.Get(next).Clo
        });
    }

    /// <summary>
    /// Night temperature for the MVP biome, falling across the run. Balance doc
    /// 5.2 and 6.1 both key off this - it drives clo demand and firewood need.
    /// </summary>
    public float NightTempForDay(int dayNumber) => Biome.NightTemperature(dayNumber, Schedule);

    /// <summary>
    /// Spend a slot ranging out to find better ground. Design spec 8.2's
    /// prospecting model, and the thing Fitness is actually FOR.
    ///
    /// Before this, Fitness bought only an ~8% energy efficiency multiplier -
    /// which against the Endurance Athlete's 10 kg fat deficit was no contest,
    /// and the archetype was simply worse. Prospecting gives a high-Fitness build
    /// a lane: range out cheaply, find the good valley, then harvest it.
    /// </summary>
    private void Prospect()
    {
        int fitness = attributes[AttributeKind.Fitness];

        // Fitness gets its own curve, steeper than the generic skill multiplier:
        // 0.86 at Fitness 3 against 1.58 at Fitness 9. Ranging out is the thing
        // this attribute is supposed to be FOR, so the spread has to be wide
        // enough to be worth building around.
        float fitnessScale = (0.5f + 0.12f * fitness) * Body.PhysicalCapacity;

        // Diminishing returns. The first day out finds the obvious good ground;
        // the tenth is refining. This also stops exploration being a strictly
        // dominant opener.
        float headroom = (MaxTerritoryQuality - TerritoryQuality)
                       / (MaxTerritoryQuality - 1.0f);

        float gain = 0.055f * fitnessScale * Math.Max(0.15f, headroom);
        TerritoryQuality = Math.Min(TerritoryQuality + gain, MaxTerritoryQuality);
    }

    /// <summary>The best ground findable in a biome. Exploration has diminishing returns.</summary>
    public const float MaxTerritoryQuality = 1.9f;

    private static ShelterTier NextTier(ShelterTier current) => current switch
    {
        ShelterTier.None => ShelterTier.TarpLeanTo,
        ShelterTier.TarpLeanTo => ShelterTier.DebrisHut,
        ShelterTier.DebrisHut => ShelterTier.AFrame,
        ShelterTier.AFrame => ShelterTier.ReflectorWallCamp,
        ShelterTier.ReflectorWallCamp => ShelterTier.LogShelter,
        ShelterTier.LogShelter => ShelterTier.LogCabin,
        _ => ShelterTier.LogCabin
    };

    public int DayNumber { get; private set; }
    public bool IsOver => Record.EndCondition != EndCondition.None;

    /// <summary>
    /// Advance one day. Order matters and is fixed: activity burn, then intake,
    /// then body, then morale at the boundary, then end conditions. Changing this
    /// order changes every downstream RNG draw and breaks saved replays.
    /// </summary>
    public DayResult StepDay(DayPlan plan)
    {
        if (IsOver) throw new InvalidOperationException("run has already ended");

        // Balance constants are read ONCE per day and held. A hot reload landing
        // mid-day would apply different constants to different parts of one tick.
        var b = balance.Current;

        DayNumber++;
        Morale.BeginDay();

        int slots = Calendar.SlotsForDay(DayNumber);

        // ---- acute events ----
        // Rolled BEFORE slots are spent, because an injury costs you the day it
        // happens, and a storm decides whether you sleep dry.
        var acute = AcuteEvents.Roll(DayNumber, Morale.Resolve, Shelter != ShelterTier.None, Rng, b);
        bool soaked = plan.SoakedAtSleep;
        bool voluntaryTapOut = false;
        bool gearIsDry = false;
        bool severeInjury = false;
        Collapsed = false;

        switch (acute.Kind)
        {
            case AcuteEventKind.MemoryEvent:
                Morale.ApplyMemoryEvent(acute.Magnitude, b);
                break;
            case AcuteEventKind.Storm:
                if (acute.Magnitude > 0f) soaked = true;
                break;
            case AcuteEventKind.Injury:
                slots = Math.Max(1, slots - (int)acute.Magnitude);
                InjuryBurden += acute.Magnitude;
                // A bad injury, or an accumulation of them on a wasted body, ends
                // the run. EndCondition.Incident was previously unreachable - the
                // sim could not produce one of its own four documented endings.
                if (acute.Magnitude > 2.75f && Rng.Stream("events.injurysevere").NextFloat() < 0.18f * (2f - BodyConditionRatio))
                    severeInjury = true;
                break;
        }

        if (acute.Kind != AcuteEventKind.None)
        {
            Record.Trace.Add(new TraceEntry
            {
                Day = DayNumber, Slot = 0,
                Kind = acute.Kind == AcuteEventKind.MemoryEvent ? TraceKind.MoraleEvent
                     : acute.Kind == AcuteEventKind.Injury ? TraceKind.Injury
                     : TraceKind.WeatherEvent,
                Code = acute.Code,
                Magnitude = acute.Magnitude
            });
        }

        // ---- burn ----
        float burn = Body.EffectiveBasalMetabolicRate(b);
        bool anyBuildProgress = false;

        var season = Calendar.SeasonForDay(DayNumber, Schedule);
        float harvestedKg = 0f;

        // How desperate the player was when the day started, for scaling the
        // relief a catch brings.
        float larderDaysBefore = Larder.DaysOfFood(Body.WeightKg, b);

        for (int i = 0; i < Math.Min(slots, plan.Slots.Count); i++)
        {
            var activity = plan.Slots[i];
            burn += EnergyModel.ExcessKcalForSlot(activity, Body, fitness, plan.TerrainMultiplier * Biome.TerrainMultiplier, b);
            if (activity.IsBuildProgress())
            {
                anyBuildProgress = true;
                if (activity == Activity.ShelterBuild) AdvanceShelter(b);
                if (activity == Activity.RenderMarrow && GearEffects.CanPerform(Gear, ActivityRequirement.Rendering))
                    Larder.RenderMarrow(1f, b);
                if (activity == Activity.WhittleComfortProject) AdvanceComfortProject(b);
                if (activity == Activity.PreserveFood) Larder.Preserve(1f, WorkCapacity);
                if (activity == Activity.BuildCamp) AdvanceCampStructure(b);
            }

            if (activity == Activity.Exploring) Prospect();

            float wood = Firewood.YieldPerSlot(activity, Gear, attributes[AttributeKind.Bushcraft]);
            if (wood > 0f) WoodKg += wood * WorkCapacity;

            // Drying gear was inert. Wet insulation is barely insulation, so this
            // is the answer to a storm rather than a wasted slot.
            if (activity == Activity.DryGearAtFire && LastFireQuality > 0.3f) gearIsDry = true;

            // Working a slot may produce food. This is where Hunting and Foraging
            // finally matter, and where two runs on different seeds diverge.
            var governing = Harvest.GoverningAttribute(activity);
            if (governing.HasValue)
            {
                var caught = Harvest.Resolve(activity, season, attributes[governing.Value], Gear, Rng, EffectiveTerritory, Biome);
                if (caught.CaughtSomething)
                {
                    // How much you bring back scales with what your body can
                    // actually carry and process. Hunting decides whether you
                    // catch it; Fitness and lean mass decide how much of it
                    // reaches camp.
                    float haul = WorkCapacity;
                    Larder.Add(caught.ProteinG * haul, caught.FatG * haul,
                               caught.CarbohydrateG * haul, caught.EdibleKg * haul);
                    harvestedKg += caught.EdibleKg;

                    // Working the ground thins it. Doc 12's trigger A is this
                    // number crossing a threshold.
                    LocalDepletion = Math.Max(0.15f, LocalDepletion - 0.004f * caught.EdibleKg);

                    // Spec 5.6: a large food success is worth +10 morale. Without
                    // this every run is a one-way slide, because the daily losses
                    // have nothing to push back against.
                    // EVERY catch is a lift, and the lift is bigger when you are
                    // desperate. A hare is 0.8 kg and used to clear no threshold
                    // at all, so a starving player could snare a rabbit and feel
                    // nothing - which is exactly backwards. Someone who has not
                    // eaten in three days is elated by a rabbit; someone with a
                    // full rack is mildly pleased.
                    //
                    // This is deliberately a MORALE reward rather than a
                    // nutritional one. The macro model stays honest - a lean
                    // animal really is lean - while the moment of catching it
                    // reads the way it should.
                    float sizeTerm = 2f + (b.MoraleLargeFoodSuccess - 2f)
                                          * MathF.Min(1f, caught.EdibleKg / LargeCatchKg);
                    float desperation = 1f + 1.5f * (1f - MathF.Min(1f, larderDaysBefore));
                    var source = activity == Activity.Foraging
                        ? MoraleSource.BeachcombFind
                        : MoraleSource.LargeFoodSuccess;
                    Morale.ApplyEvent(source, sizeTerm * desperation, b);
                    Record.Trace.Add(new TraceEntry
                    {
                        Day = DayNumber, Slot = i, Kind = TraceKind.Action,
                        Code = $"harvest.{caught.Source}".ToLowerInvariant(),
                        Magnitude = caught.EdibleKg
                    });
                }
            }
        }

        // ---- collapse from exhaustion ----
        // The exploration consequence. NOT a fall, NOT fatal, and NOT a dice roll
        // against the player: it fires only when someone pushes movement-heavy
        // work while critically depleted, which is a decision they can see coming
        // and can decline by resting. Terrain is priced in calories (above); this
        // is what those calories eventually buy you.
        float exertion = 0f;
        for (int i = 0; i < Math.Min(slots, plan.Slots.Count); i++)
            if (plan.Slots[i].IsMovement()) exertion += 1f;

        if (exertion > 0f && BodyConditionRatio < CollapseConditionThreshold)
        {
            float shortfall = 1f - BodyConditionRatio / CollapseConditionThreshold;
            float chance = 0.30f * shortfall * MathF.Min(1f, exertion / 3f);
            if (Rng.Stream("body.collapse").NextFloat() < chance)
            {
                Collapsed = true;
                CollapseCount++;
                Morale.ApplyEvent(MoraleSource.WeightLoss, -4f, b);
                Record.Trace.Add(new TraceEntry
                {
                    Day = DayNumber, Slot = 0, Kind = TraceKind.Injury,
                    Code = "body.collapse", Magnitude = exertion
                });
            }
        }

        // The decision to walk is available every day; a crisis amplifies it.
        if (AcuteEvents.ConsidersTappingOut(
                DayNumber, Morale.Resolve, Morale.Current, BodyConditionRatio,
                acute.Kind == AcuteEventKind.MemoryEvent, Rng, b))
        {
            voluntaryTapOut = true;
        }

        // ---- the fire ----
        // Burn what the night demands, or as much of it as there is. A fire that
        // runs out at 2am is worth a fraction of one that runs to dawn.
        float woodDemand = Firewood.NightlyDemandKg(NightTempForDay(DayNumber), Shelter);
        float woodBurned = Math.Min(WoodKg, woodDemand);
        WoodKg -= woodBurned;

        // Getting the fire lit at all is a gear question. GearEffects.
        // FireReliability existed and was called from nowhere, which made the
        // ferro rod a strictly dead pick.
        float reliability = GearEffects.FireReliability(Gear);
        LastFireQuality = (woodDemand <= 0f ? 1f : woodBurned / woodDemand) * reliability;

        // Cold costs calories, not just morale - and the fire is what stands
        // between the player and that bill.
        float warmth = AvailableClo + Firewood.FireClo(LastFireQuality);
        float cloDeficit = CloDemandTonight(DayNumber) - warmth;
        burn += EnergyModel.ThermoregulationKcal(cloDeficit, Body.WeightKg, b);

        // Cold Adaptation pays EVERY night, not only on nights you mismanaged.
        // Design spec 4.1 gives it "sleep quality in poor shelter" alongside the
        // thermoneutral offset; modelled as a small always-on reduction in
        // overnight metabolic cost. Before this it was pure insurance - worth
        // 1.37 days per point with a bad kit and exactly 0.00 with a good one,
        // which makes it a stat nobody drafts on purpose.
        burn -= EnergyModel.SleepQualitySaving(
            attributes[AttributeKind.ColdAdaptation], Body.WeightKg, NightTempForDay(DayNumber));

        // ---- intake ----
        // Appetite is what the day cost. The larder rarely covers it, and the
        // protein ceiling then caps what of it the body can actually use.
        var meal = plan.DirectRation ?? Larder.Eat(burn, Body.WeightKg, b);
        var nutrition = NutritionModel.Evaluate(meal, Body.WeightKg, b);

        Larder.CapacityKg = Larder.CapacityFromStructures(NightTempForDay(DayNumber))
                          + attributes[AttributeKind.Bushcraft];
        Larder.ApplyDailySpoilage(NightTempForDay(DayNumber));

        float daysOfFood = Larder.DaysOfFood(Body.WeightKg, b);

        // A DirectRation means food is being supplied outside the larder (tests
        // and scenarios), so it also supplies food SECURITY. Without this the
        // player reads as insecure while being handed a full meal every day.
        bool foodInsecure = plan.DirectRation is null && daysOfFood < 1f;

        // ---- body ----
        float net = nutrition.UsableKcal - burn;
        Body.ApplyEnergyBalance(net, b);

        // ---- morale at the day boundary ----
        var moraleTotals = Morale.EvaluateDay(new MoraleDayInputs
        {
            FoodInsecure = foodInsecure,
            ShelterInadequate = warmth < CloDemandTonight(DayNumber),
            NoBuildProgress = !anyBuildProgress,
            SoakedAtSleep = soaked && !gearIsDry,
            WeightLossFraction = Body.WeightLossFraction,
            HasPhoto = plan.HasPhoto
        }, b);

        Log.LogEvent("day", $"{DayNumber}|{Body.WeightKg:F2}|{Morale.Current:F2}|{net:F1}");

        Record.Trace.Add(new TraceEntry
        {
            Day = DayNumber,
            Slot = 0,
            Kind = TraceKind.NutritionEvent,
            Code = nutrition.ProteinCeilingBound ? "nutrition.ceiling.bound" : "nutrition.ok",
            Magnitude = net
        });

        // Doc 12 triggers, evaluated at the day boundary.
        FoodTriggerDays = LocalDepletion < b.FoodTriggerThreshold ? FoodTriggerDays + 1 : 0;
        ShelterTriggerDays = (CloDemandTonight(DayNumber) - warmth) > b.CloGapThreshold
            ? ShelterTriggerDays + 1 : 0;

        var end = CheckEndConditions(b, voluntaryTapOut, severeInjury);
        if (end != EndCondition.None) Finish(end);

        Record.DaysSurvived = DayNumber;

        return new DayResult
        {
            DayNumber = DayNumber,
            SlotsAvailable = slots,
            BurnKcal = burn,
            Event = acute.Kind,
            FireQuality = LastFireQuality,
            WoodKg = WoodKg,
            LocalDepletion = LocalDepletion,
            RelocationAvailable = CanRelocate(b),
            Collapsed = Collapsed,
            RawKg = Larder.RawKg,
            PreservedKg = Larder.PreservedKg,
            HarvestedKg = harvestedKg,
            LarderDaysOfFood = daysOfFood,
            FoodInsecure = foodInsecure,
            UsableIntakeKcal = nutrition.UsableKcal,
            WastedIntakeKcal = nutrition.WastedKcal,
            NetKcal = net,
            ProteinCeilingBound = nutrition.ProteinCeilingBound,
            Morale = moraleTotals,
            EndCondition = end
        };
    }

    /// <summary>Design spec 6. Checked in a fixed order so the recorded cause is deterministic.</summary>
    private EndCondition CheckEndConditions(BalanceData b, bool voluntaryTapOut, bool severeInjury)
    {
        if (severeInjury) return EndCondition.Incident;
        if (voluntaryTapOut || Morale.HasTappedOut) return EndCondition.TapOut;

        if (Body.BodyFatPercent < Body.Sex.MedicalPullBodyFatPercent()) return EndCondition.MedicalPull;
        if (Body.WeightLossFraction > b.MedicalPullMaxWeightLossFraction) return EndCondition.MedicalPull;
        if (Body.Bmi < b.MedicalPullMinBmi) return EndCondition.MedicalPull;

        return EndCondition.None;
    }

    private void Finish(EndCondition end)
    {
        Record.EndCondition = end;
        Record.CauseCode = DeriveCauseCode(end);
        Record.FinalChecksum = Log.GetChecksum();
        Record.IsCleanBalanceSample = balance.IsCleanSample;
    }

    /// <summary>
    /// The story behind the rule that fired. Design spec 6 is explicit that
    /// reporting only the rule ("pulled at 17.1 BMI") is a rules citation, not an
    /// explanation - the copy has to name the underlying cause.
    /// </summary>
    private string DeriveCauseCode(EndCondition end)
    {
        if (end == EndCondition.Incident) return "incident.injury";
        if (end != EndCondition.TapOut) return "medical.wasting";

        var top = Morale.Breakdown().TopMovers(1).Top;
        return top.Count > 0 ? $"morale.{top[0].Source}".ToLowerInvariant() : "morale.unknown";
    }
}
