# OFF THE GRID — Technical Implementation v0.1

*Build verification, agent workflow, CI structure and logging conventions live in **`05-agent-workflow.md`**. This document specifies what the code does; that one specifies how it gets written and verified.*

*Companion to `01-design-spec.md`. MVP-scoped. Placeholder art throughout.*

---

## 1. Platform and engine

| | |
|---|---|
| **Engine** | Unity 6.3 LTS (6000.3.x) |
| **Pipeline** | URP 2D Renderer |
| **Scripting backend** | IL2CPP, .NET Standard 2.1 |
| **Targets** | iOS 16+, Android 10+ (API 29) |
| **Orientation** | Portrait primary; landscape optional for archery `[T1]` |
| **Min device** | iPhone SE 2nd gen / Snapdragon 7-series, 4 GB RAM |
| **Frame target** | 60 fps in minigames, 30 fps capped in menus/map (battery) |
| **Offline** | Fully playable offline. Network only for Daily Challenge seed, leaderboards, telemetry. |

Built-In RP is deprecated from 6.5 onward and unsuitable for new titles; URP 2D is the only sensible choice and gives us 2D lights for free (firelight, dusk, storm) which does a lot of the atmospheric work placeholder art can't.

---

## 2. Architecture

### 2.1 The central decision

> **The simulation is a pure C# library with zero UnityEngine dependencies.**

`LastOut.Sim` compiles as its own assembly definition (asmdef) referencing nothing from Unity. Unity is a *renderer and input layer* that observes sim state.

Why this matters more than usual here:

1. **Headless balance solving.** Q2 (does the fasting build win?) and Q3 (does the gear meta collapse?) are answered by running 100,000 simulated runs on a build server overnight, not by playtesting. Impossible if the sim is tangled in MonoBehaviours.
2. **Unit testability.** The calorie/thermoregulation math in §5 of the design spec is the game. It needs test coverage, not manual verification.
3. **Rival simulation reuses the same code path** at lower fidelity — one source of truth for body mechanics.
4. **Determinism.** Required for the Daily Challenge.

```
LastOut.Sim/            (asmdef, no Unity refs)
  Body/                 BMR, thermo, mass partitioning, morale
  World/                Map gen, tiles, depletion, weather
  Actions/              Slot resolution, risk profiles
  Events/               Event catalogue, triggers, escalation
  Rivals/               Low-fidelity contestant sim
  Rng/                  Deterministic PRNG + named streams
  Balance/              Solver harness, headless runner

LastOut.Game/           (Unity)
  Presentation/         Map view, camp view, card UI
  Minigames/            Archery, Fire, ForageID
  Journal/
  Audio/
  Accessibility/
  Persistence/

LastOut.Data/           ScriptableObjects + JSON tables
LastOut.Tests/          NUnit, runs headless in CI
LastOut.Tools/          Editor: tuning inspector, sim replay viewer
```

### 2.2 Sim/view contract

The sim exposes immutable state snapshots and a command queue. The view never mutates sim state directly.

```csharp
public interface ISimulation {
    GameState Current { get; }              // immutable snapshot
    IReadOnlyList<ActionOption> AvailableActions();
    ResolutionResult Submit(ActionCommand cmd);   // advances one slot
    IReadOnlyList<SimEvent> DrainEvents();  // for view/audio/journal
}
```

`ResolutionResult` carries a **full delta** — kcal in/out, mass change, morale change, clo change, tile depletion, events fired. The UI renders that delta as the result card, and the journal consumes the same object. One data source for both.

### 2.3 Minigame handshake

Minigames do not compute outcomes. They return a normalised **performance scalar** `p ∈ [0,1]` which the sim converts into yield.

```csharp
// Sim → minigame
public readonly struct MinigameContext {
    public float ShakeAmplitude;    // from §9.1 of design spec
    public float HoldDecay;
    public float DifficultyScalar;
    public int   AttributeEffective;
    public WeatherState Weather;
    public ulong Seed;              // deterministic
}

// Minigame → sim
public readonly struct MinigameResult {
    public float Performance;       // 0..1
    public bool  Skipped;           // → sim substitutes 0.70 × expected
}
```

This keeps *all* balance in the sim, so tuning archery yield never requires touching minigame code, and auto-resolve (§7.3) is a one-line substitution.

---

## 3. Determinism and RNG

The Daily Challenge (same seed, everyone, 24h, one attempt) makes determinism a hard requirement, and leaderboards make it an anti-cheat requirement.

**Do not use `System.Random` or `UnityEngine.Random`.**

Implement **PCG32** with **named streams** — separate independent generators per concern, so adding a new event roll doesn't shift the map generation of an existing seed:

```csharp
var rng = new SimRandom(worldSeed);
rng.Stream(RngStream.MapGen)
rng.Stream(RngStream.Weather)
rng.Stream(RngStream.AnimalBehaviour)
rng.Stream(RngStream.EventTriggers)
rng.Stream(RngStream.RivalSim)
rng.Stream(RngStream.Salvage)
```

**Float determinism:** cross-platform IEEE-754 divergence is a real risk for a leaderboard. Two mitigations, in order of preference:

1. **Quantise all sim state at slot boundaries** — round kcal to 1, mass to 0.001 kg, morale to 0.01, temperatures to 0.01 °C. Intra-slot float drift cannot accumulate across a 300-slot run. This is cheap and almost certainly sufficient.
2. Full fixed-point (Q16.16) if cross-device replay tests show divergence anyway. Costly; do not do this pre-emptively.

### 3.1 Resolution path — DECIDED (was `T2` / `D3`)

**The cross-device replay test goes into CI at M0. The fixed-point decision waits for its output.**

This was a disagreement between "quantisation probably suffices" and "fixed-point is the only safe answer," and it is not an argument worth having in the abstract — it's a measurable property of the target hardware. Measure it while the sim is small enough that converting it is cheap.

**Test definition:**

```
Given:  N fixed seeds × a recorded command log per seed
Run:    headless sim on every device in the target matrix
        + the server-side .NET host
Assert: byte-identical end state and byte-identical
        per-slot quantised state hash
```

**Requirements:**

- Runs in CI on every commit to `LastOut.Sim`, on real devices (not just editor) — IL2CPP + ARM is the whole point
- Must include the **server .NET host**, since a client/server mismatch is the failure that produces false cheat flags
- Must include at least one older ARM device and one current one, plus both iOS and Android
- Per-slot hashes, not just final state, so divergence is localised to the slot and system that caused it

**Decision rule:**

| Result at M0 | Action |
|---|---|
| Zero divergence across the matrix | Ship quantisation. Keep the test in CI permanently as a regression guard. |
| Divergence localised to specific systems | Quantise those systems more aggressively; retest |
| Divergence widespread or unstable | Convert to Q16.16 **now**, at M0, while the sim is ~2k lines rather than ~20k |

**Why M0 specifically:** the cost of converting to fixed-point scales with the size of the sim. At M0 it's a week. At M4 it's a rewrite of the thing every other system depends on, plus revalidation of every balance number produced by the solver in between.

**Server validation** for Daily Challenge submissions: the client uploads the seed and the full command log; the server replays it headless (same `LastOut.Sim` DLL) and verifies the reported result. Cheating requires reproducing a valid command sequence, not editing a score.

---

## 4. Data model

### 4.1 Where data lives

| Data | Storage | Why |
|---|---|---|
| Gear items, attribute defs, archetypes | **ScriptableObject** | Designer-editable in Editor, no code deploy |
| Tuning curves & constants | **ScriptableObject** (`BalanceConfig`) | Single hot-swappable asset; lets QA A/B tune |
| Event catalogue | **JSON** | Volume; writer-editable outside Unity; localisable |
| Journal text fragments | **JSON** + localisation table | Writing is a workstream, not a code task (see design spec §13.2) |
| Species, forage tables, salvage tables | **JSON** | Tabular, will be iterated by analytics |
| Scenarios | **JSON** | Server-deliverable post-launch without a client update |

`BalanceConfig` is loaded into a plain C# `BalanceData` struct at sim init — the sim never touches a ScriptableObject directly.

### 4.2 Core schemas

```csharp
struct BodyState {
    float WeightKg, LeanMassKg, FatMassKg;
    float CoreTempDeficitC;
    int   KcalToday, KcalRequiredToday;
    float Morale;                       // 0..100
    float WetnessByLayer;               // applied as clo multiplier
    HealthFlags Conditions;             // Infection|Ankle|Dysentery|Hypothermia
}

struct Attributes {
    byte Bushcraft, Hunting, Foraging, Fitness, Resolve, ColdAdapt;
    // Effective values derived at runtime: Fitness_eff = f(lean ratio)
}

struct Tile {
    TileSubtype Subtype;
    float GameDensity, GameDensityCap;
    SpeciesWeights Species;
    float ForageYield, Wood;
    WaterQuality Water;
    float TerrainCost;
    HazardFlags Hazards;
    bool Explored;
}

struct CampState {
    ShelterTier Tier;
    float ClothingClo, ShelterClo, FireClo, BeddingClo;
    float FoodCacheKcal;
    bool  CacheSecured;                 // bear raid gate
    List<ItemInstance> Gear;            // durability tracked
    int   DaysSinceBuildProgress;       // morale decay stacking
}

struct WorldState {
    int Day, SlotIndex, SlotsToday;
    DateTime InGameDate;                // drives daylight → slot count
    WeatherState Weather;
    WeatherForecast Forecast;           // 2-day visibility
    Tile[,] Map;                        // 12×12
    Vector2Int CampTile;
    TideState Tide;                     // gates beachcombing
}

struct RivalState {                     // ×9, low fidelity
    Attributes Attr;
    ItemId[] Loadout;                   // 10
    float TileQuality;                  // abstracted, not a real map
    BodyState Body;                     // same struct, coarser updates
    PersonalityBias Bias;
    int?  TapOutDay;                    // null while active
}
```

### 4.2a Nutrition and gear-condition schemas

Two additions driven by `04-balance-economy.md`:

```csharp
public struct FoodValue {
    public int   Kcal;
    public float ProteinG;
    public float FatG;
}

public struct NutritionState {
    public int   KcalToday;
    public float ProteinG, FatG;
    public float ProteinCeilingG;      // 2.5 * bodyweight
    public float UsableKcalToday;      // excess protein excluded
}

public struct GearCondition {
    public GearId Id;
    public int    UsesRemaining;       // durability, see balance doc §6.1
    public float  Sharpness;           // 0-1, axes/saws only
    public int    ConsumableCount;     // arrows, hooks, ferro strikes
}
```

`GearCondition` must be part of the save schema and the `RunRecord` — attrition state at time of death is a balance signal.

### 4.2b Logging

```csharp
public interface ISimLog {
    void Trace(SimSystem sys, string msg);
    void Info (SimSystem sys, string msg);
    void Warn (SimSystem sys, string msg);
    void Error(SimSystem sys, string msg);
}
```

`ISimLog` is a sim-side interface with a no-op default; Unity supplies a `Debug.Log` adapter. **The sim never calls `Debug.Log` directly** — that would be a UnityEngine dependency and would break the fast verification path (§13.1).

Every line carries seed, day, slot and subsystem, so any log line can be replayed to its exact state:

```
[seed:8F2A1C4B][d34/s2][Morale] -3.5 => 41.2 | food_insecure:-2.0 idle:-1.0 wtloss:-0.5
```

Morale logs the itemised `MoraleBreakdown`, never just the delta — the same object that drives the HUD. Nutrition logs when the protein ceiling binds, with numbers: "full cache, still starving" is correct behaviour that reads exactly like a bug (R14). Conventions in `05-agent-workflow.md` §7.

### 4.3 Slot count derivation

```csharp
float daylight = Daylight.HoursAt(latitude: 49.7f, date: world.InGameDate);
world.SlotsToday = Math.Clamp((int)(daylight / 2.2f), 3, 7);
```

Use a real solar-position formula (NOAA approximation), not a lookup table — it makes adding biomes at other latitudes free.

---

## 5. Tick model

**The sim is turn-based. There is no `Update()` loop in the sim.**

```
Submit(action)
  ├─ Validate against AvailableActions()
  ├─ Compute activity kcal          (MET × 1.05 × W × terrain × fitness × mass^1.15)
  ├─ Run minigame (or auto-resolve) → performance scalar
  ├─ Convert performance → yield    (food kcal, wood, materials, intel)
  ├─ Apply thermoregulation for slot duration
  ├─ Roll consented risks (visible in the action card)
  ├─ Apply deltas, quantise
  ├─ Advance slot
  └─ If slots exhausted → EndOfDay()

EndOfDay()
  ├─ Overnight thermoregulation (sleep, shelter clo, wetness)
  ├─ Mass partitioning from net deficit  (Forbes)
  ├─ Morale accounting
  ├─ Tile depletion recovery, seasonal drift
  ├─ Weather advance + forecast roll
  ├─ Rival sim tick ×9
  ├─ Check tap-out / medical / hypothermia (player and rivals)
  ├─ Journal entry generation
  └─ Compression check → next day slot-by-slot or auto-resolved
```

Runtime cost is trivial (microseconds). This is why 100k headless runs for balance solving is practical.

### 5.1 Compression

```csharp
bool ShouldCompress(GameState s) =>
       s.Camp.Tier >= RequiredTierFor(s.Weather)
    && s.Camp.FoodCacheKcal >= 3 * s.Body.KcalRequiredToday
    && s.Camp.FireSecure
    && s.Body.Morale > 25
    && !s.PendingEvents.Any()
    && !s.Forecast.HasSevereWithin(2)
    && !s.IsCheckInDay;
```

Compressed days run the identical `EndOfDay` path with all slots auto-resolved at 0.70 expected, then present a **summary card**. Any predicate flipping false breaks compression and returns control immediately, mid-day.

**Critical:** compression must never silently kill the player. If a compressed day would end the run, break out one day earlier and hand control back with a warning. `[T3]`

---

## 6. Map generation

12×12, deterministic from `RngStream.MapGen`.

1. **Coastline pass** — one map edge is tidal shore (Vancouver Island profile); carve an estuary inlet
2. **Hydrology** — river from high edge to estuary, seeded riverbank subtypes
3. **Elevation** — 2-octave value noise → ridge/valley, drives `TerrainCost` 0.8–2.2
4. **Subtype assignment** — weighted by elevation + water distance
5. **Resource seeding** — `GameDensity`, `ForageYield`, `Wood` from subtype tables with noise variance
6. **Hazard tagging** — scree on steep, river-crossing on water-adjacent, bear sign near salmon-bearing water
7. **Camp placement** — randomised within a central 4×4, guaranteed water access within 2 tiles
8. **Quality audit** — reject-and-reroll if total reachable calories within 4 tiles of camp fall below a floor. *This is the anti-unwinnable-start guard and it matters — see the closest comparable's launch difficulty complaints.*

**Depletion/recovery:**
```csharp
tile.GameDensity -= huntPressure * SpeciesRecoveryRate[species];
tile.GameDensity  = Min(tile.GameDensityCap,
                        tile.GameDensity + 0.02f * daysElapsed);
```
Plus seasonal migration: `GameDensityCap` shifts by subtype across the run (salmon spike days 18–34, deer move to lower elevation with first snow), so the optimal camp changes over a run.

---

## 7. Minigames

Each is a self-contained scene loaded additively, receives `MinigameContext`, returns `MinigameResult`. **No minigame reads or writes `GameState`.**

### 7.1 Archery

- **Input:** touch drag down-and-back to draw, release to fire. Retro Bowl idiom.
- **Draw:** distance → power; over-draw penalised
- **Wobble:** reticle offset via 2-octave Perlin sampled at `time × holdDecay`, amplitude `40 × shake` px. Perlin (not white noise) so it reads as *organic body sway*, not glitching. **This is the single most important feel target in the game.**
- **Arc:** simple ballistic, 2D projectile solver, ~9.8 m/s² scaled to screen
- **Preview arc:** partial, fades at range; length = `f(Hunting_eff)`
- **Stalk phase:** top-down approach before the shot. Animal has vision cone (angle + range), hearing radius, and wind vector. Player moves tile-fractionally; noise = `f(terrain, Hunting_eff, movement speed)`.
  - `[T4]` **Wind ships behind a feature flag.** Three simultaneous variables may be one dial too many on a phone. Build it, flag it, cut it if the stalk reads as noise in playtest.
- **Output:** `Performance` from hit location (vital/wound/miss) × animal size

### 7.2 Fire starting

- Pressure-hold / rhythm input on the ember
- Difficulty inputs: fuel dryness, wind speed, precipitation, tinder quality, ferro rod presence
- With ferro rod: trivial dry, achievable wet. Without: a genuine threat.
- **Do not cut this to save scope.** It is what turns the ferro rod from a stat into a decision.

### 7.3 Foraging ID — *deferred, post-MVP*

**Cut from MVP** (design D1: two minigames ship). Auto-resolves in MVP. Spec retained for later:

- Grid of 6–12 specimens; identify edible vs lookalike under a soft time limit
- `Foraging` reduces lookalike count and extends inspection time
- Wrong pick → dysentery/illness roll

**Ships in MVP regardless of the minigame:** the illness roll (rolled against `Foraging` instead of played) and **learned ID persistence** — a `HashSet<SpeciesId>` in the profile save, which is the cleanest expression of "knowledge, not power." Learned IDs raise the auto-resolve success rate, so the knowledge model stays visible without the interaction.

> **Engineering consequence of the two-minigame cut:** the `IMinigame` handshake (§2.3) matters *more*, not less. Five of seven activities are auto-resolve-only in MVP, so the auto-resolve path is a shipping feature on the critical path, not a fallback. It needs the same test coverage as a played minigame.

### 7.3a Trap placement — *post-MVP, first candidate*

Cut from MVP on cost: a terrain-reading judgement puzzle requires authored terrain semantics and legible environmental art, not just a new input handler. Flagged as the **strongest post-MVP minigame candidate** because it adds decision texture with no twitch requirement — which also makes it the most accessible of the seven.

### 7.4 Accessibility — DECIDED (was `T5` / `D2`)

**Auto-resolve is the accessibility path for all minigames in MVP. No audio variants.**

The audio archery variant is cut. Auto-resolve (§7.3) at 0.70× attribute-expected is the fallback, and it costs nothing because compression already depends on it.

**However — structural accessibility is not deferred, and it is an engineering requirement from M2:**

| Requirement | Why it can't wait |
|---|---|
| Semantic labels on every interactive element | Retrofitting means touching every prefab |
| Deterministic focus order per screen | Determined by hierarchy construction; cheap now, a rebuild later |
| Announcement channel for sim state changes | Needs a hook in the `ResolutionResult` consumer, i.e. in the architecture |
| No unavoidable gesture conflicts | The archery drag input is the risk; must be checked as it's built |

**Implementation note.** Add an `IAnnouncer` interface at the view layer consuming the same `ResolutionResult` that drives UI and journal. One data source, three consumers. Screen-reader output then cannot drift from displayed output, because it isn't a parallel implementation.

**Tier 2 — post-launch:** audio-cue archery (stereo pan + pitch for reticle offset), rhythmic audio fire, haptic fishing tension. Tier 1 exists to make Tier 2 additive rather than architectural.

---

## 8. Rival simulation

Nine `RivalState` instances, ticked once per day in `EndOfDay`.

- **Same body equations**, different resolution: one aggregated daily action mix rather than per-slot decisions
- Action mix chosen by `PersonalityBias` weights against current needs (hungry → hunt-weighted; cold → shelter-weighted)
- Minigame outcomes substituted with `expected(attribute) + variance(seed)`
- Tap-out from the same causes as the player (§5.5 design spec) — **emergent, never scripted**

Cost per day: ~9 × a few hundred float ops. Negligible.

```csharp
// Post-MVP hook, stubbed now so the interface doesn't change later
interface IRivalSource {
    RivalState[] GetRoster(int biomeId, int playerTier, ulong seed);
}
class LocalAiRivalSource   : IRivalSource { }   // MVP
class AsyncPlayerRivalSource : IRivalSource { } // v1.1 — replays recorded runs
```

Recording real runs for async replay requires the command log we're already capturing for anti-cheat (§3). **Build the log now, use it later.** `[T6]`

---

## 9. Save and persistence

Two separate stores:

| Store | Contents | Format |
|---|---|---|
| **Run save** | Full `GameState` + command log | Binary, versioned, one slot, autosaved every slot |
| **Profile** | Map intel per biome, learned species IDs, attribute carry (+8 cap), journal archive, scenario records | JSON |

**Autosave every slot.** Mobile apps get killed. Losing a 60-minute run to a phone call is unacceptable and would be the top App Store complaint.

**Migration:** every save carries a schema version. Write a migration path from day one, or the first balance patch invalidates every in-progress run.

**Cloud sync:** `[T7]` iCloud/Google Play Saves for profile only, not run state — avoids conflict resolution on active runs.

---

## 10. Q8 layered retention — implementation

Design direction: all three mitigations, layered.

### 10.1 Cause-of-death analysis

The sim retains a **rolling 20-day decision trace**. On run end, a deterministic analyser walks it and identifies the inflection point:

```csharp
// Find the day after which cumulative deficit became unrecoverable
// given remaining fat stores and achievable daily intake for that map
int PointOfNoReturn(RunHistory h);
```

Output is a short, specific, non-judgemental card:
> *"Your deficit became unrecoverable around day 22. You had 6 slots of daylight left that week and spent 11 of 14 on firewood — your shelter was already Tier 2. The elk sign on the north ridge went unexplored."*

This is authored text with slotted variables, not generated prose. Maybe 30 templates covers the failure taxonomy.

### 10.2 Personal-best framing

Post-run screen compares against *your* history first, leaderboard second. Track: longest run, most weight retained, earliest Tier 3 shelter, largest single harvest, most tiles explored. Multiple axes means a day-30 player who explored the whole map still beat something.

### 10.3 Scenarios as the skill ladder

Scenarios carry an explicit difficulty rating and a suggested order. They teach specific competencies in isolation (cold management, gear scarcity, a time-boxed salmon run). Campaign is the aspiration; scenarios are the practice ground and the short-session hook.

**Telemetry must validate this actually works** — see §11.

---

## 11. Telemetry

Minimal, privacy-respecting, no ad SDKs.

**Required events:**

| Event | Fields | Answers |
|---|---|---|
| `run_end` | day, cause, archetype, loadout, start weight/BF, final weight, map seed | Q2 fasting balance, Q3 gear meta |
| `slot_action` | day, action type, minigame perf, skipped? | Auto-resolve usage rate, mid-game sag |
| `minigame_result` | type, performance, shake amplitude, energy ratio | **Does shake read as fair?** — correlate perf drop with rage-quit |
| `compression_enter/exit` | day, trigger | Does compression skip the game? |
| `tutorial_step` | step, completed, abandoned | Onboarding cliff |
| `session_end` | duration, days advanced | Run-length target of 45–75 min |
| `checkin_taken` | day, remaining rivals, **survival delta vs non-takers** | Is intel worth half a day? |
| `deficit_inflection` | day, cause, morale, cache days | Fired **live** when the point of no return is crossed — measures how many players are already dead and still playing |
| `app_background` | screen, last action, last minigame perf | Rage-quit signal. Distinguishes "finished" from "closed the app after a missed shot" |
| `purchase_offer_shown` / `purchase_made` | day reached, biome/cosmetic, price | Conversion by furthest day (§15.4 of design spec) |
| `cause_self_report` | reported cause, actual cause | The legibility gate — ≥70% correct (§5.6.3 of design spec) |

### 11.1 Run-history records — the balance instrument

**Beyond events: every run uploads a full replayable life-cycle record.** Counters say what happened; the record says why, and can be re-interrogated later without shipping new telemetry.

```csharp
// LastOut.Sim — no Unity types
public sealed class RunRecord {
    public RunSetup      Setup;        // seed, attributes, loadout, body, biome, mode
    public CommandLog    Decisions;    // the SAME log used for Daily Challenge replay
    public DaySnapshot[] Timeline;     // one per day
    public MinigameLog[] Minigames;    // perf + shake + energy + cold + lean ratio
    public RivalOutcome[] Rivals;      // tap-out day + cause per rival
    public RunEnding     Ending;       // final day, cause, inflection point, self-report
}

public struct DaySnapshot {
    public int   Day;
    public float WeightKg, LeanKg, FatKg;
    public float Morale;
    public MoraleBreakdown Modifiers;  // itemised — powers the HUD AND the analysis
    public float CoreDeficitC;
    public float CacheDays;
}
```

**This is close to free.** `CommandLog` already exists for Daily Challenge server validation — it *is* the decision trace. `MoraleBreakdown` already exists to drive the HUD attribution view (§5.6.1 of design spec). The new work is `DaySnapshot` capture at `EndOfDay`, serialisation, and batched upload on run end.

**Sizing:** ~300 commands + ~60 day snapshots + ~80 minigame entries ≈ **20–40 KB per run uncompressed**, well under 10 KB gzipped. Batch on run end, retry on next launch if offline, cap local queue at 20 runs.

**Derived views to build first:**

1. **Distribution of final day by run index** — the single most important metric. If run 5 isn't better than run 1, the knowledge-persistence model has failed and Q8 is unsolved.
2. Cause-of-death distribution **by skill band** — flags any dead pillar (§5.5 of design spec)
3. Loadout pick rate vs. solver-optimal — where the design miscommunicates
4. Self-reported vs. actual cause — the legibility gate
5. Conversion by furthest day reached — the plateau as a commercial signal
6. Shake amplitude vs. `app_background` within 60s — the fairness question answered in the wild

> **Privacy:** gameplay data only, no PII, no ad SDKs. Disclosed in store listing and settings, with an opt-out that disables upload without disabling the game.

---

## 12. Performance budgets

| Budget | Target |
|---|---|
| Sim tick | < 1 ms |
| Full day (compressed, incl. 9 rivals) | < 5 ms |
| Cold start → playable | < 3 s |
| Minigame frame time | < 16.6 ms |
| Memory | < 350 MB |
| APK/IPA | < 150 MB with placeholder art |
| Battery | 30 fps cap outside minigames; no continuous background sim |

None of this is tight — the game is turn-based with 2D sprites. The only real risk is UI allocation churn; use pooled card views and avoid per-frame LINQ.

---

## 13. Testing

`LastOut.Tests` runs headless in CI on every commit.

**Unit:** BMR against published Mifflin–St Jeor values · mass partitioning conserves energy · thermoregulation monotonicity · slot count from daylight across the calendar · morale bounds · RNG stream independence

**Property-based:** no action sequence produces negative mass, morale outside 0–100, or NaN · same seed + same command log = identical result, on every target platform

**Balance (nightly, headless):**
```
BalanceSolver --runs 100000 --archetypes all --loadouts sampled
  → win rate by archetype
  → win rate by loadout (Q3: does the show's known six dominate?)
  → survival curve by starting weight (Q2: does 160 kg beat 85 kg?)
  → median final day (target: competent play ≈ 55+, naive ≈ 12–20)
```

**Manual/QA:** minigame feel · accessibility with VoiceOver + TalkBack end-to-end · compression never kills silently · save/restore mid-run on app kill

### 13.1 Build verification — the missing loop

Testing coverage above says *what* to assert. It does not say how a change gets verified before it is claimed done, and that gap matters more than usual because Unity compiles in-editor by default — **code edited outside the editor has no compile feedback at all.**

Full workflow in `05-agent-workflow.md`. The ladder:

| Gate | Command | Cost | When |
|---|---|---|---|
| Sim build + test | `dotnet build -warnaserror` + `dotnet test` | ~7 s | Every sim change |
| Unity boundary guard | `check-no-unity-refs.sh` | <1 s | Every sim change |
| Balance assertions | `--assert-only` | ~30 s | Every constant change |
| Determinism (host) | `--replay-verify --seeds 64` | ~20 s | RNG / ordering / float / save changes |
| Unity compile | batch mode or MCP | 30–90 s | Every Unity change, batched |
| Unity tests | `-runTests` → XML | 1–3 min | After compile passes |
| Determinism (devices) | CI matrix, §3.1 | Nightly | — |

**The pure-C# sim boundary buys a 15–40× faster verification loop** for the majority of the work, because the fast four gates above cover the game's actual logic without Unity in the loop. This is a second, previously unstated reason the §2.1 architecture decision is load-bearing — and a second reason the `UnityEngine` reference guard must never be weakened to pass.

⚠️ **`-runTests` must never be combined with `-quit`** — Unity exits before tests run and reports success. A silent false pass is the worst failure mode for an automated gate.

---

## 14. Milestones

| M | Deliverable | Proves |
|---|---|---|
| **M-pre** | `CLAUDE.md` + five verification skills, `BuildVerify.CompileCheck`, split CI (`sim.yml` / `unity.yml`), `ISimLog` (`05-agent-workflow.md` A33–A36) | **The feedback loop exists before the thing it verifies.** Building M0 without it means retrofitting every guarantee. |
| **M0** | `LastOut.Sim` + tests + headless runner. **No Unity.** Plus: **cross-device determinism test in CI**, `Int2`/`Float2` types, `UnityEngine`-reference CI guard, `RunRecord` schema, **`BalanceAssert` suite + the six solver sweeps** (balance doc §9) | The body model works, is tunable, deterministic on real hardware, **and the fasting build provably loses** |
| **M1** | Archery vertical slice. One map tile, one animal, full shake model, fed vs starving states forced. | **The core feel question.** Kill or continue here. |
| **M2** | Full day loop, map, fog, camp, gear, **2 minigames (archery + fire)**, auto-resolve path, morale attribution HUD, placeholder art. **Accessibility Tier 1 built in from the first screen. Screen-reader testing starts here.** | The game exists, and its UI is not a retrofit liability |
| **M3** | Rivals, check-in intel, weather, events, compression, **camp relocation**, compression fuzz harness | The game is a contest |
| **M4** | Tutorial, journal, scenarios, Daily Challenge, day-summary card, weekly medical read | The game is playable by someone new |
| **M5** | Balance solver pass, full run-history telemetry, IAP integration (biomes + cosmetics), soft launch build | The game is fair, measurable, and sellable |

**M1 is a gate, not a milestone.** If starving-player archery reads as the game cheating rather than the body failing, the entire design thesis fails and it should be reconsidered before M2 spend.

---

## 15. Technical risks

| # | Risk | Severity | Mitigation |
|---|---|---|---|
| R1 | Shake model feels unfair, not tragic | **Critical** | M1 gate; Perlin not white noise; heavy telemetry |
| R2 | Float divergence breaks Daily Challenge leaderboard | High | Quantise at slot boundaries; cross-device replay tests in CI |
| R3 | Compression skips the interesting parts | High | Conservative predicates; break-out on any threat; telemetry on compressed-day ratio |
| R4 | Gear meta collapses to the show's six | Medium | Nightly solver; biome-gated item value |
| R5 | Stalk phase illegible on a small screen | Medium | Wind behind a flag; portrait/landscape test |
| R6 | Save corruption on app kill | Medium | Slot-level autosave, versioned schema, migration path from v1 |
| R7 | Accessibility retrofitted rather than built in | Medium | Tier 1 structural work from M2; screen-reader testing from M2; `IAnnouncer` on the `ResolutionResult` path |
| R8 | Journal writing under-resourced | Medium | It is a writing workstream with a named owner, not a code task |
| R9 | **Plateau lands on the conversion moment** — F2P means day 30 is where the purchase is asked for | **High** | Relocation + scenarios as late-game variety; conversion-by-furthest-day telemetry from M5 |
| R10 | **Free biome must carry demo + retention alone** | High | Scenarios and Daily Challenge as the free-tier engine; at least one free post-launch biome |
| R11 | **Server/client sim version skew** produces false cheat flags | High | Per-version replay endpoints or hard client update gate; server included in the M0 determinism matrix |
| R12 | Auto-resolve is a shipping feature for 5 of 7 activities, not a fallback | Medium | Test coverage equal to a played minigame; must feel like a costed choice |
| R13 | **Morale tuning silently re-breaks the fasting build** — physiology alone does not defeat it (balance doc §7.2) | **High** | `BalanceAssert.FastingBuildLosesTo()` in CI from M0. Single most important assertion in the codebase. |
| R14 | **Protein ceiling reads as the game cheating** — invisible constraint, full cache, still dying | **High** | Protein/fat bar beside the calorie readout; plain-language framing when it binds; playtest `[B1]` |
| R16 | **Agent or contributor weakens a failing assertion instead of fixing the cause** — silently reverting a balance property that took a whole system to achieve | **High** | Explicit `CLAUDE.md` prohibition; PR review required on any tolerance change; `balance/` PRs must carry solver diffs |
| R15 | Preservation micromanagement becomes a chore — it's the top balance lever so it gets used heavily | Medium | Batch preservation actions; auto-resolve racks once set; watch slot-count telemetry |

---

## 16. Open technical questions

### Closed

| # | Question | Ruling |
|---|---|---|
| T2 | Quantisation vs fixed-point | **Measure, don't argue.** Cross-device replay test in CI at M0; decision rule in §3.1 |
| T5 | Accessibility approach | **Auto-resolve for all minigames in MVP.** No audio variants. Tier 1 structural work from M2 (§7.4) |

### Open

| # | Question |
|---|---|
| T1 | Portrait-only, or landscape for archery? |
| T3 | Exact compression break-out threshold before a fatal day |
| T4 | Does wind survive the stalk phase? |
| T6 | Async rival run-recording format and storage cost |
| T7 | Cloud save provider and conflict policy |
| T8 | Backend for Daily Challenge seeds + leaderboards — build, or use PlayFab/Nakama? **Now also needs an IAP/entitlement store** (§15 of design spec), which raises the buy-over-build case |
| T9 | Localisation scope at launch |
| T10 | **Entitlement verification for paid biomes** — server-validated, or store-receipt only? Offline play must not break |
| T11 | **Relocation and map state** — does fog reset, and does previous-run map intel apply to the new area? |
| T12 | **`RunRecord` upload consent flow** and opt-out plumbing |
