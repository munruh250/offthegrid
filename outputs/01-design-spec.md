# OFF THE GRID — Design Spec v0.1

*Working title. Unity, mobile-first, stylised 2D top-down/isometric.*

**Status:** Draft from design session. Open questions flagged `[Q]` throughout and collected in §19.

**Companion documents:** `02-technical-implementation.md` · `03-discipline-reviews.md` · **`04-balance-economy.md`** (all numeric values for food, fuel, shelter, gear attrition — plus the finding that morale, not physiology, is what defeats the fasting build)

---

## 1. Pitch

Ten strangers are dropped alone in the wilderness with ten items each. No crew, no camera operators, no contact. Last one who hasn't tapped out wins.

You play one of them. The other nine are simulated — you never see them, but they are out there, burning calories, building shelters, and quitting one by one.

**One-line:** *A survival contest where the real enemy is your own decay.*

## 2. Differentiators

Competitive scan (Aug 2026) found the field is dominated by zombie/base-builder sandboxes (Don't Starve, Day R, Whiteout Survival) and one very close comparable, **Survive Alone** (iOS, 2026) — a *text-based* single-player survival sim with scenario leaderboards and strong accessibility support.

| Vector | Us | Field |
|---|---|---|
| Framing | **Contest** — 9 simulated rivals, elimination | Solo survival or PvP base raiding |
| Presentation | Stylised 2D visual | Text-based (Survive Alone) or 3D sandbox |
| Agency under pressure | Skill-based minigames as session multipliers | Menu-and-roll resolution |
| Intel | Rival status as a purchasable resource | Post-hoc leaderboards only |
| Difficulty curve | Emerges from body decay, not a curve | Authored difficulty tiers |

**Known player pain in the closest comparable** (from store reviews): no tutorial, food too scarce, calories drain too fast, players dying within days. Developer is actively reducing difficulty post-launch. Treat this as free playtest data — **onboarding and early-game generosity are the failure mode to design against.**

## 3. Core design thesis

The game's subject is **decay**. Difficulty must rise because the player's body is failing, not because a difficulty curve says so.

Three systems carry this and must never be decoupled:

1. **Daylight shrinks** as the season turns → fewer actions per day (§7)
2. **Starvation degrades minigame performance** → the bow shakes because you're hungry (§9)
3. **Morale is a spend, not a meter** → you buy it with calories and daylight you can't spare (§6.4)

Day 50 is terrifying because of what you did on day 30.

---

## 4. Character creation

### 4.1 Attributes

Six attributes, range **1–10**, default starting pool **30 points**, soft cap 8 at creation.

| Attribute | Governs |
|---|---|
| **Bushcraft** | Shelter quality/build speed, tool crafting, fire reliability, cordage |
| **Hunting** | Bow steadiness, trap placement quality, stalk noise, fishing hookup rate |
| **Foraging** | Plant/berry/shellfish yield, ID minigame difficulty, poison avoidance |
| **Fitness** | Explore range per slot, haul capacity, terrain cost reduction — **raises calorie burn** |
| **Resolve** | Morale event resistance, morale gained per comfort project, shivering tolerance |
| **Cold Adaptation** | Thermoneutral offset, wet-cold resistance, sleep quality in poor shelter |

*Fishing is folded into Hunting.* `[Q1]` Split it out if playtest shows Hunting is over-picked.

**Origin** (city/region of birth) is flavour + a small biome-specific bonus (+1 effective Cold Adaptation in matching biome, dialogue colour). Never a hard wall.

### 4.2 Archetype presets

**New players get presets by default.** Custom point allocation unlocks after first completed run. This is the primary onboarding lever.

| Archetype | Bush | Hunt | Forage | Fit | Resolve | Cold | Start kg | BF% |
|---|---|---|---|---|---|---|---|---|
| Ex-Military | 5 | 6 | 3 | 8 | 6 | 5 | 84 | 18 |
| Bushcraft Instructor | 8 | 5 | 6 | 4 | 5 | 5 | 88 | 26 |
| Hunter/Trapper | 4 | 8 | 4 | 6 | 5 | 6 | 92 | 24 |
| Forager/Herbalist | 5 | 3 | 8 | 5 | 6 | 6 | 78 | 22 |
| Commercial Fisherman | 5 | 6 | 3 | 5 | 6 | 8 | 98 | 31 |
| Endurance Athlete | 3 | 4 | 4 | 9 | 7 | 6 | 72 | 12 |

### 4.3 Body setup

Player sets **starting weight** and **body fat %** freely within bounds (55–160 kg; BF% 8–45, sex-gated floors).

Fat is a battery, not a stat: **1 kg fat ≈ 7,700 kcal stored.** A 120 kg contestant at 35% BF carries ~42 kg fat ≈ 323,000 kcal.

**Anti-fasting-strategy levers** (all three required — see §5.5 for the balance target):

1. **Superlinear movement cost.** Movement-type activities take a mass penalty of `(W / 80)^1.15`, so exploration collapses at high mass.
2. **Cold is a curve, not a line.** Fat raises thermoneutral temperature, but low lean mass caps shivering output — a heavy sedentary body loses core temp *faster* once still (§5.3).
3. **Morale is the real killer.** Idle, cold, unproductive players break. The fasting build survives on paper and taps out at day 25 having built nothing.

---

## 5. Body simulation

All values kcal/day unless noted. Simulation ticks per **slot** (§7), not per second.

### 5.1 Basal metabolic rate

Mifflin–St Jeor:

```
BMR_male   = 10·W + 6.25·H − 5·A + 5
BMR_female = 10·W + 6.25·H − 5·A − 161
```
`W` kg, `H` cm, `A` years.

**Adaptive thermogenesis** — metabolism suppresses beyond what mass loss alone predicts:

```
loss_frac  = (W_start − W) / W_start
BMR_eff    = BMR(W) · (1 − 0.10 · min(1, loss_frac / 0.15))
```
Caps at −10% once 15% of body weight is gone. This is why late-game contestants plateau rather than freefall.

### 5.2 Activity cost

```
kcal_hr = MET · 1.05 · W · terrain_mult · fitness_mult
fitness_mult = 1.00 − 0.02·(Fitness − 5)      // efficient bodies do more per kcal
```
Movement-type activities additionally multiply by `(W/80)^1.15`.

| Activity | MET | Movement-type? |
|---|---|---|
| Sleep | 0.95 | — |
| Rest (awake, sheltered) | 1.2 | — |
| Whittle / comfort project | 2.0 | — |
| Dry gear at fire | 1.5 | — |
| Fishing (shore/net) | 3.0 | — |
| Foraging | 3.5 | ✔ |
| Trap line (set/check) | 4.0 | ✔ |
| Hunting (stalk) | 4.5 | ✔ |
| Shelter build | 5.0 | — |
| Sawing | 5.5 | — |
| Exploring (rough terrain) | 6.0 | ✔ |
| Chopping wood | 6.3 | — |
| Hauling logs | 8.0 | ✔ |

*Example:* 90 kg, Fitness 5, exploring 2.5 h flat terrain →
`6.0 · 1.05 · 90 · 1.0 · 1.0 · (90/80)^1.15 = 567 kcal/h × 2.5 = 1,418 kcal`. One slot.

### 5.3 Thermoregulation

```
T_neutral  = 28 − 0.30·BF% − 3.0·clo_total − 0.4·ColdAdapt
T_eff      = T_air − windchill − (8 if soaked else 0)
cold_kcal_hr = 5.0 · max(0, T_neutral − T_eff)
```

`clo_total` = clothing (fixed baseline ~1.0) + shelter tier + fire proximity + sleeping bag.

| Insulation source | clo |
|---|---|
| Issued clothing (baseline) | 1.0 |
| Debris/lean-to (T1) | +0.4 |
| A-frame, banked (T2) | +0.9 |
| Insulated hut w/ bed (T3) | +1.6 |
| Fire, tended | +0.8 (day) / +0.5 (night, decaying) |
| Sleeping bag (gear) | +1.5 |
| Wool blanket (issued) | +0.6 |
| **Wet gear** | ×0.35 to the affected source |

**Shivering.** If `cold_kcal_hr` exceeds what intake+stores can supply comfortably, the body shivers: up to `+250 kcal/h`, capped by lean mass —
`shiver_cap = 3.0 · lean_mass_kg` kcal/h.
When shivering can't close the gap, core temp falls → hypothermia state → forced medical evac at −2.0 °C core deficit sustained 3 slots.

**This is the second anti-fat lever:** a 130 kg / 40% BF body has high `T_neutral` (good) but low lean mass (~78 kg → cap 234 kcal/h) so it cannot generate heat once genuinely cold and wet.

**Wetness** is *not* a separate meter — it applies the `×0.35` insulation penalty and clears via a *Dry Gear* slot at the fire. Cost is time, which is the scarcest thing in the game.

### 5.4 Mass loss

Lean:fat partitioning shifts as fat depletes (Forbes-style):

```
BF_frac  = fat_kg / W
p_lean   = 0.20 + 0.30 · max(0, (0.20 − BF_frac) / 0.20)     // 0.20 → 0.50
Δmass_kg = deficit · [ p_lean/1100 + (1 − p_lean)/7700 ]
```
Lean tissue ≈ 1,100 kcal/kg (wet), fat ≈ 7,700 kcal/kg.

Consequence: lean players lose weight *faster* per kcal of deficit and lose muscle, which degrades Fitness-derived performance. Fat players lose slowly — until they hit 20% BF, then it accelerates.

**Lean loss degrades performance:**
`Fitness_eff = Fitness · (lean_now / lean_start)^1.5`
`shake_penalty` scales with energy ratio (§9.1).

### 5.4a Macronutrients — the protein ceiling

**Calories are not fungible.** Food carries three values, not one: kcal, protein grams, fat grams. Full tables in `04-balance-economy.md` §3.

Lean meat has a hard physiological intake ceiling — roughly **2.5 g protein per kg bodyweight per day** (~35% of calories) — beyond which ammonia and urea accumulate. This is rabbit starvation, and it's real.

```
protein_ceiling_g = 2.5 · bodyweight_kg
if protein_consumed_g > protein_ceiling_g:
    usable_kcal   = kcal_from_fat + (protein_ceiling_g · 4)   // excess doesn't count
    excess_ratio  = protein_consumed_g / protein_ceiling_g
    illness_risk += 0.15 · (excess_ratio − 1.0)
    morale       -= 2 · (excess_ratio − 1.0)
```

**Why this earns its complexity:** it produces the state where **you have a full cache and are still starving**, which is a far better failure than an empty larder. An 85 kg player with 60 kg of cached deer meat still runs a **1,738 kcal/day deficit** — losing 1.58 kg/week while apparently well fed.

Mechanically it makes fat the real currency, which reshapes the whole food economy: **only black bear is sustainable as a sole food source** in the MVP biome. Everything else — deer, elk, salmon, hare, and *moose* worst of all — caps below maintenance no matter how much the player kills.

**Legibility is mandatory here `[B1]`.** An invisible ceiling is indistinguishable from a cheating game. Requires a protein/fat balance bar beside the calorie readout, and plain-language framing at the moment it binds: *"You're eating enough. You're not eating the right thing."*

**Counterplay must always exist:** fatty sources (bear, salmon belly), rendered marrow, bone broth, organ meat, foraged carbohydrate, or lowering activity so the ceiling covers more of the burn.

### 5.5 How a run ends

There are **four** end conditions. Three are losses, one is the win.

| Cause | Type | Trigger |
|---|---|---|
| **Tap out** (voluntary) | Loss | Morale ≤ 0 |
| **Medical pull** (involuntary) | Loss | BF% < 6 (M) / 12 (F), **or** weight loss > 30% of start, **or** BMI < 17 |
| **Acute event** | Loss | Core deficit −2.0 °C sustained 3 slots (hypothermia), **or** health track reaches 0 from injury/illness (§10) |
| **Last out** | **Win** | All nine rivals have ended their runs |

**Starvation is not a separate end condition.** The medical pull thresholds always fire before true metabolic collapse — which is realistic, and matches the show, but creates a communication problem: a player who *experienced* starving is told they were "pulled." The cause-of-death copy (§12.3) must name the underlying story, not just the trigger. "You were pulled at 17.1 BMI" is a rules citation. "You'd been in deficit for 19 straight days; the medic made the call for you" is the same fact told properly.

#### Distribution target — no dominant cause

Morale is expected to be the *most common* loss (it is on the show), but if any single cause accounts for **> 60%** of endings at a given skill level, the other pillars aren't being engaged with and should be retuned.

The realistic shape is not an even split — it's a distribution that **shifts with skill**:

| Skill band | Expected dominant cause | Why |
|---|---|---|
| Novice | Acute event / morale | Cold mismanagement, no comfort projects, panic decisions |
| Competent | Morale | Body is managed; the head is the failure point |
| Expert | Medical pull | Deliberately pushing the body to outlast rivals — the "correct" way to lose |

That progression is itself a design goal: **the way you lose should change as you get better.** If it doesn't, the systems aren't layered, they're stacked.

> **Solver output (required):** cause-of-death distribution segmented by skill band. Cheap to compute in the headless solver, and it's the single clearest signal that a pillar is dead weight.

> **Balance target `[Q2]` — TESTED, and the original mitigation was wrong.** See `04-balance-economy.md` §7.2–7.3.
>
> The fasting build was expected to be beaten by superlinear movement cost and the shivering cap. **It isn't.** On body simulation alone a 160 kg fasting build reaches **day 90** and beats a competent 85 kg build by 16 days. 67 kg of fat is ~500,000 kcal of battery and no movement-exponent tuning overcomes it inside 90 days.
>
> **Morale defeats it — specifically the idleness penalty.** With morale in the loop: fasting-and-idle taps out **day 12**, competent-and-active reaches **day 59**. Target met by a 47-day margin. Do not raise the movement exponent; it isn't the lever that matters.

### 5.6 Morale

```
M ∈ [0, 100]
M_start = 70 + 3·Resolve
```

**Daily losses**

| Source | Δ |
|---|---|
| Base decay | **−1** |
| Food insecure (no cached surplus ≥ 1 day) | **−2** |
| Shelter clo below what temp demands | −4 |
| No build/craft progress (per consecutive day) | **−1 (stacks to −5)** |
| Per 5% body weight lost | **−0.5** |
| Soaked at sleep | −3 |
| Memory event | −(5 to 20) · (1 − Resolve/12) |

> **Values tuned in `04-balance-economy.md` §7.4.** The v0.1 constants were roughly 2× too harsh — every scenario died by day 22. Do not revert without re-running the solver.
>
> ⚠️ **Morale is load-bearing for balance, not just for theme.** The body simulation *alone cannot beat the sit-still-and-fast strategy* — a 160 kg fasting build survives to day 90 on physiology alone (§04 §7.2). The idleness penalty is what defeats it. **Making morale more legible (§5.6.1) must not make it weaker.**

**Gains** (all cost calories and a slot)

| Source | Δ |
|---|---|
| Comfort project completed | **+14** (was +8 to +18) |
| Large food success (deer, salmon run) | +10 |
| Shelter tier milestone | +12 |
| Beachcomb find | +5 |
| Photo (gear item), passive | +2/day, cap 20 total |

Warning band below **25** (UI desaturates, journal tone shifts). Tap out at 0.

#### 5.6.1 Attribution — morale must always be explicable

**Design rule: the player is never told morale dropped without being told why.**

Calories are countable — eaten minus burned. Morale is dozens of small modifiers the player never sees summed, which makes it the one system capable of ending a run for reasons the player can't reconstruct. That's a direct violation of the consent rule (§10.1) in spirit, even though no single modifier is unfair.

The fix is **attribution at the moment of change**, not a forecast. A forecast ("at this rate: 8 days") tells the player *when*; attribution teaches them the causal link, which is the thing that makes them better. Three tiers:

**Tier 1 — HUD, ambient, zero interruption.**
A mood emoji plus the morale bar. The emoji reflects the current band, and it is **tappable**: tapping expands a live breakdown of every active modifier with its value and source. Glanceable by default, deep on demand.

> Implementation note: this expanded view is the cause-of-death analysis (§12.3) rendered live. Same data, same component, built once.

**Tier 2 — end-of-day summary card.**
At each day boundary, surface the **two or three largest movers only** and swallow the rest into a single "other" line. With up to 40 modifiers a day, showing all of them is toast-spam and trains the player to dismiss without reading. This reuses the compression summary card (§7.2) — not a new surface.

**Tier 3 — weekly medical check.**
The deep read: full body composition, morale trend across the week, and the standings intel it already provided. This materially improves the value of the check-in spend (§11.1) — half a day now buys body, morale, *and* rival data, which makes it a much easier sell and gives the intel-pricing question (§19) a fairer test.

#### 5.6.2 Suggestion layer

When morale drops into the warning band, the game may offer a **quiet, occasional** nudge toward a remedy — a comfort project, a shelter upgrade. Constraints:

- Never more than one active suggestion at a time
- Never repeated for the same cause in the same run
- Phrased as an option, not an instruction
- It never tells the player their *best* move, only a *valid* one

The tone target is a journal margin note, not a companion character.

#### 5.6.3 Acceptance criterion

> **≥ 70% of players correctly identify their own cause of death in a post-run prompt.**

This makes legibility measurable rather than a matter of opinion, and it's a hard gate: the player has been *told* the reasons all run, so failure here is a genuine UI failure, not a tuning question. Measured via a one-tap post-run question ("what ended your run?") compared against the sim's recorded cause.

**Memory events** are the family/isolation system. Triggered semi-randomly, weighted by day count and low morale. Each offers a *choice*, never a flat penalty:

- *Sit with it* — lose the slot, take half the morale hit
- *Work through it* — keep the slot, take the full hit now, +2 lingering decay for 3 days

---

## 6. Gear

### 6.1 The ten items

Player selects **10 items from a fixed master list of ~40**. Locked for the run, mirroring the show. Biome is revealed *before* selection; drop point is randomised within it.

Standard issued kit (clothing, first aid, emergency comms, wool blanket) does **not** count against the ten.

**Master list (MVP subset — 24 items).** Full ~40 in the tech doc data table.

| # | Item | Primary effect |
|---|---|---|
| 1 | Axe | Wood yield ×1.8, enables T2/T3 shelter, log hauling |
| 2 | Saw | Wood yield ×1.5, −35% chop kcal, precise cuts for T3 |
| 3 | Ferro rod | Fire minigame trivial in dry, viable in wet |
| 4 | Sleeping bag | +1.5 clo at night |
| 5 | 2.5 qt pot | Boil water (removes illness roll), stew (+15% kcal extraction) |
| 6 | Fishing line + 35 barbless hooks | Unlocks angling minigame |
| 7 | Gill net | Passive fish yield per slot, tears (repair events) |
| 8 | Bow + 6 arrows | Unlocks archery minigame — **large game only vector** |
| 9 | Multi-tool | Repairs, +1 effective Bushcraft for crafting |
| 10 | 550 paracord (80 m) | Shelter lashing, trap triggers |
| 11 | Bank line | Cordage economy, trap lines, fishing backup |
| 12 | Sharpening stone | Prevents tool degradation |
| 13 | Small gauge wire (snare) | Small-game trapping, high efficiency per kcal |
| 14 | Slingshot | Small game, low kcal, no arrow scarcity |
| 15 | Trapping wire (heavy) | Larger snares |
| 16 | Canteen/water bottle | Water carry, reduces trips |
| 17 | Tarp | +0.7 clo shelter, instant rain protection |
| 18 | Bivvy bag | +0.9 clo, waterproof, no shelter needed early |
| 19 | Knife (hunting) | Baseline processing, butchery speed |
| 20 | Pocket knife | Carving/comfort projects, +2 morale per project |
| 21 | Scotch-eyed auger | Enables T3 shelter joinery, furniture |
| 22 | Frying pan | Cooking efficiency, +10% kcal extraction |
| 23 | Photo of family | +2 morale/day, cap 20; halves memory event severity |
| 24 | Emergency rations (2,000 kcal) | One-time buffer |

### 6.2 Gear design rule

> **Gear multiplies strengths; it does not patch weaknesses.**

A bow at Hunting 3 has a shake amplitude that makes anything past 20 m a coin flip — a wasted slot. This forces identity commitment and prevents a single dominant "safe generalist" stack.

**Known risk `[Q3]`:** The real show has a settled meta (axe/saw/ferro/bag/pot/line). If our sim reproduces it, the choice is fake. Mitigation: biome-gated value (gill net is elite on tidal coast, dead weight inland) plus archetype multipliers. **Requires a solver pass — see analytics review.**

### 6.3 Beachcombing

Storm-driven, tide-gated. After a storm event, shoreline tiles roll a salvage table for 2–3 days: buoys, rope, plastic barrels, tin cans, fishing floats, lumber, tarp scraps.

Purpose: a pressure valve that rewards the player who walks the beach instead of hunting, without undermining the locked ten. It is **hope, not a plan** — never balance the game around it.

---

## 7. Time and the day loop

### 7.1 Slot economy

A day is **3–7 action slots**, derived from daylight:

```
slots = clamp( floor(daylight_hours / 2.2), 3, 7 )
```

Vancouver Island, mid-September start:

| Day | Date | Daylight | Slots |
|---|---|---|---|
| 1 | Sep 15 | 12.6 h | 5 |
| 15 | Sep 29 | 11.5 h | 5 |
| 30 | Oct 14 | 10.4 h | 4 |
| 45 | Oct 29 | 9.4 h | 4 |
| 60 | Nov 13 | 8.6 h | 3 |
| 75 | Nov 28 | 8.0 h | 3 |

**The season itself compresses your action economy.** No authored difficulty curve required. Night slots exist but are limited to rest, fire tending, and low-value camp work.

Target: ~5 slots/day × 60 days ≈ **300 decisions per run**.

### 7.2 Pacing and compression

Target run length: **45–75 minutes**, not the 3–5 hours a naive 300-decision loop implies.

**Compression is a mechanic, not a settings toggle.** Once camp is stable (shelter tier ≥ needed, food cache ≥ 3 days, fire secure), days auto-resolve into a summary card. The game zooms back in when:

- Food cache drops below 2 days
- Weather event incoming
- Morale enters warning band
- A memory event fires
- Medical check-in day
- Player manually intervenes

Early days play slot-by-slot because your shots land and it feels good. Late days slow down again because every one matters. The mid-game — the identified sag risk, days 20–40 — is where compression does the heavy lifting.

### 7.3 Auto-resolve

Any minigame can be skipped and auto-resolved at **70% of attribute-expected outcome**. Prevents day-50 chore fatigue. Compression uses this internally.

In MVP, five of the seven activities auto-resolve permanently (§9) — so this path is load-bearing, not an accessibility afterthought. It must feel like a *choice with a cost*, not a penalty.

### 7.4 Late-game pressure and camp relocation

The identified plateau risk (§12.3) is that by day 30 a skilled player has camp built, food systems running and the biome learned — so the remaining days become arithmetic rather than decisions.

**Existing pressure sources** (already in the design, all of them squeeze rather than diversify):

| Source | Effect |
|---|---|
| Shrinking daylight (§7.1) | 5 slots → 3 slots. Fewer actions per day. |
| Resource depletion (§8.2) | Hunted tiles thin out; recovery is slower than extraction |
| Seasonal migration | Salmon run ends; some species reduce or den for winter |
| Falling temperature | Firewood demand rises → chopping and gathering compete for slots |
| Event escalation (§10.3) | Severity and frequency scale with day index |

The net effect is correct and intended: late game costs more, yields less, and every shot matters. **But pressure alone makes the late game harder without making it more varied.** A tighter version of the same decision is still the same decision.

#### Camp relocation — DECIDED (was `Q4`)

**Relocation ships as the late-game decision that is genuinely new rather than merely tighter.**

Rare on the show, but mechanically it's the one late-game choice that isn't a smaller version of an early-game choice. Two triggers, both player-facing:

| Trigger | Player-visible signal |
|---|---|
| **Local food exhausted** | Yield indicators on surrounding tiles visibly degrade over days; tracks/sign become sparse |
| **Shelter inadequate for the season** | Shelter clo vs. temperature demand shown as a widening gap in the status readout |

**Consent rule compliance (§10.1) is the hard constraint.** The player must be able to *see* the game thinning out or the shelter falling behind **before** committing to move. Relocation must never be a blind gamble — the upside is unknown (that's the tension), but the reason for leaving must be legible.

**Costs and stakes**

- **Lose the built shelter entirely.** No partial carry. This is what makes it a real sacrifice rather than a repositioning.
- Multiple slots of travel, at late-game calorie prices and reduced daylight
- New territory: unhunted game (the upside), unknown exposure and water access (the risk)
- Fog returns for the new area unless previously scouted

**Persistence synergy.** Map intel carried from previous runs (§12.2) is exactly what turns relocation from *desperate* into *informed* — a returning player knows there's a sheltered draw two valleys north. This is the strongest existing expression of "knowledge, not power," and relocation is where it pays off most.

> **Playtest gate:** does relocation read as a meaningful strategic pivot, or as the game confiscating the player's work? If the latter, reduce the shelter loss before reducing the trigger frequency.

> **Note on scope:** run length is 45–75 minutes, so day 30 is roughly 20 minutes in. The plateau may be substantially less severe than a long-session survival game would suffer. **Treat all of §7.4 as a playtest question, not a solved problem** — do not over-build against boredom that may not materialise.

---

## 8. World and map

### 8.1 Tiles

12×12 grid (144 tiles), camp placed in a randomised near-central tile. Fog-of-war; exploring reveals.

Per-tile data:

| Field | Range | Notes |
|---|---|---|
| `subtype` | enum | old-growth, second-growth, riverbank, tidal shore, muskeg, ridge, burn zone, estuary |
| `game_density` | 0.0–1.0 | Depletes on hunting pressure, recovers +0.02/day toward cap |
| `species_mix` | weights | deer, elk, rabbit, grouse, squirrel, salmon (seasonal), shellfish |
| `forage_yield` | 0.0–1.0 | Seasonal decay: berries gone by ~day 35 |
| `wood` | 0.0–1.0 | Standing dead vs green matters for fire |
| `water` | bool/quality | Boil requirement if stagnant |
| `terrain_cost` | 0.8–2.2 | Multiplies movement kcal |
| `hazard_profile` | tags | scree, river crossing, bear sign, cliff |

### 8.2 Exploration as prospecting

Animals **live on the map**; they are not spawned per-hunt. Your drop point may genuinely be poor — but a good area exists somewhere and calories will find it.

This solves the show's own bad-luck problem without faking it, and makes Fitness/exploration a real strategic axis rather than fog-clearing busywork.

Depletion + recovery + seasonal migration means the optimal camp location changes over a run. `[Q4]` Should camp relocation be possible mid-run, and at what cost?

### 8.3 Weather

The **only** system permitted to inflict unconsented harm (§10). Nobody blames a game for rain.

Rolling forecast, 2-day visibility. Vancouver Island profile: high rainfall, wind, temperature drift from ~14 °C day / 8 °C night (Sept) to ~7 °C / 1 °C (Nov). Storms every 6–11 days, escalating.

---

## 9. Activities and minigames

**Design rule: the minigame is a multiplier on the session, not the session.** ~30 seconds, then read the result.

| Minigame | Type | Feeds | MVP |
|---|---|---|---|
| **Archery** | Draw / arc / release, twitch | Large game | **✅ Ships** |
| **Fire starting** | Pressure-hold / rhythm | Fire, warmth, cooking | **✅ Ships** |
| **Fishing** | Cast + tension management | Fish | Auto-resolve |
| **Chopping** | Timing/rhythm | Wood yield | Auto-resolve |
| **Trap placement** | Terrain-reading judgement puzzle, no twitch | Small game | Auto-resolve |
| **Foraging ID** | Visual discrimination, lookalike traps | Plants, berries, shellfish | Auto-resolve |
| **Whittling** | Slow, calm, low-pressure | Morale | Auto-resolve |

> **DECIDED (was `D1`) — MVP ships two minigames: archery and fire.**
>
> They were chosen because they test *different* things. Archery is the decay carrier — it's where a failing body becomes felt input (§9.1). Fire is the gear carrier — it's the mechanic that makes the ferro rod a decision instead of a stat. Two minigames, two distinct jobs, no redundancy.
>
> **Trap placement is cut from MVP** despite being emotionally central to the source. It's a judgement puzzle, which means it needs authored terrain content and readable environmental art to work at all — a significant design and art cost for a mechanic nobody has validated. It is the strongest candidate for the first post-MVP minigame.
>
> Everything else auto-resolves at 0.70× expected (§7.3). This is a known cost: **four food-producing activities are menu-and-roll in MVP**, and that narrows felt agency. Accepted deliberately in exchange for two mechanics at ship quality rather than five at prototype quality.

### 9.1 Archery — the signature mechanic

Retro Bowl-style pull-back-and-down to draw. Further pull = more power. Arrow arcs with real drop. Trajectory preview arc is partial (fades at range, length set by Hunting).

**Shake model — this is where decay becomes felt:**

```
energy_ratio = min(1, kcal_today / kcal_required_today)
shake = 0.15·(1 − Hunting_eff/12)
      + 0.50·max(0, 1 − energy_ratio)
      + 0.20·cold_stress
      + 0.15·max(0, 1 − (lean_now/lean_start))
reticle_wobble_px = 40 · shake
hold_decay = 1.0 + 2.0·shake          // shake grows the longer you hold
```

A well-fed Hunting-8 player has a near-still reticle. A day-45 starving player at Hunting 8 has a wobble that makes a 30 m shot a genuine gamble. **Same player, same attribute — the body changed.**

**Stalk phase.** Before the shot: animals have a vision cone, hearing radius, and wind direction. Move closer for an easier shot, risk spooking. Hunting reduces effective noise and visual profile.

> `[Q5]` **Wind is flagged as a playtest toggle.** Three variables (sight/sound/wind) may be one dial too many on a phone screen. Ship it behind a flag; cut if the stalk reads as noise.

### 9.2 Fire starting

Difficulty scales with fuel dryness, wind, precipitation. With a ferro rod it is trivial dry / viable wet. Without one it is a real threat. **This is the minigame that makes gear choice sing** — cut it and the ferro rod becomes a stat.

### 9.3 Foraging ID — *post-MVP*

Visual discrimination against lookalikes. Real penalty for a wrong pick (illness track, §10). Foraging attribute reduces the number of lookalikes and increases inspection time.

**In MVP this auto-resolves.** Wrong-pick illness still occurs, rolled against the Foraging attribute rather than played — so the risk survives even though the interaction doesn't.

**Learned plant IDs persist between runs** (§12) — the clearest expression of "knowledge, not power." This persistence ships in MVP even without the minigame: learned IDs raise the auto-resolve success rate, so the knowledge model is still visible and still rewarded.

### 9.4 Escalation

Minigames get harder as the player decays, per §9.1 — not via authored difficulty tiers. Late game, a missed shot compounds: no meat → deeper deficit → worse shake → worse odds tomorrow. One fish is genuinely three more days.

---

## 10. Events and risk

### 10.1 The consent rule

> **No unprovoked misfortune.** Every negative outcome must be traceable to a decision the player made, with the risk visible beforehand.

You don't "roll an ankle." You *chose* to cross the scree slope to reach the good hunting ground, and the risk was shown as a tag on that action.

**Sole exception: weather.**

Every activity carries a visible risk profile in the action card:

```
FORAGE — Burn Zone (NE ridge)
  Yield:   ●●●●○   Terrain: 1.8×
  Risks:   Injury 12%  ·  Bear sign  ·  2.5h
```

### 10.2 Event categories

**Ambient threats (consented via location/action choice)**
- Bear raids unsecured food cache → lose cache, morale hit
- Wolves circling → sleep quality penalty, multi-night
- Rodents in shelter → slow food spoilage

**Body failures (consented)**
- Infected cut → worsens unless a rest slot is spent
- Rolled ankle → explore range halved, 4–7 days
- Dysentery → from drinking unboiled water (a choice, given a pot)

**Weather (unconsented, escalating)**
- Windstorm flattens shelter roof
- Cold snap doubles firewood burn for ~5 days
- Sustained rain → soaked gear, fire difficulty spike
- First frost / first snow as dated dramatic beats

**Temptations (the best category — pure player choice)**
- Rotting carcass upstream: ~4,000 kcal, illness roll
- Salmon run, 3 days only: enormous yield, demands every slot, all other work stops
- Bear cache: high reward, high danger
- Deep-water crossing to an untouched peninsula

**Equipment failures (consented via usage)**
- Axe head loosens → repair slot or reduced yield
- Gill net tears → repair or lose passive income
- Bowstring wears → accuracy penalty until re-cordaged

### 10.3 Escalation curve

Event severity and frequency scale with `day_index` and season, so the mid-game reads as *winter closing in* rather than a random-event lottery.

---

## 11. Rivals

Nine AI contestants run the **same simulation** at lower fidelity — no minigames, resolved at attribute-expected values with variance.

Each rival has: archetype attribute spread, a 10-item loadout drawn from the same master list, a randomised drop tile with its own quality, and a personality bias (aggressive hunter / patient builder / conservative rester).

**Tap-out is emergent, not scripted.** Rivals fail from the same causes the player does (§5.5). No name is guaranteed to drop on day 12.

`[Q6]` **Asynchronous rivals.** Post-MVP: seed the roster with real players' recorded runs where available, falling back to AI. This is the multiplayer hook, deliberately deferred — get people playing single-player first.

### 11.1 Intel as a resource

You never see rivals directly. You learn about them through **medical check-ins** — which the real show has anyway.

The crew arrives, weighs you, checks vitals. **Costs half a day.** In exchange you get a leaderboard glimpse: how many remain, rough standings.

The strategic weight is the point: six still out there → play conservative. Down to two → gamble.

**Difficulty setting:**

| Mode | Rival visibility |
|---|---|
| **Blind** | Nothing. Ever. Purist. |
| **Check-in** | Intel only via medical check-in slots (default) |
| **Broadcast** | Daily standings summary, free |

---

## 12. Win condition, scoring, persistence

### 12.1 Win condition

**Last one standing.** Binary. No composite score.

Consequence acknowledged: most runs end in a loss. The reward must come from elsewhere — see 12.2 and the journal (§13).

### 12.2 Persistence

**Knowledge, not power.**

| Persists | Does not |
|---|---|
| Map intel for a biome you've played | Attributes (beyond the cap below) |
| Plant/fungus IDs learned | Gear |
| Journal archive | Camp progress |
| Scenario/leaderboard records | Anything that trivialises a run |

**Capped attribute carry:** +1 point per 3 completed runs, **hard cap +8 total**, with fast diminishing returns. Enough to feel earned, never enough to be the reason you won.

`[Q7]` Matchmaking by tier — so a 100-run veteran isn't matched against a level-1 player — is **deferred with the async multiplayer work**. Noted, not designed.

### 12.3 The retention gap `[Q8]` — UNRESOLVED

Strong players optimise. Weak players die on day 8 and learn something. **The average player plateaus around day 30, loses every time, and can't tell why.**

Binary win/loss gives no partial credit. This is the single biggest open risk in the design and it is a *retention* problem, not a systems problem.

Candidate mitigations, none chosen:
- End-of-run **cause-of-death analysis** ("your deficit became unrecoverable on day 22, when you...")
- Personal-best framing that competes with your own history, not the leaderboard
- Rival-relative feedback ("you outlasted 6 of 9")
- Scenario mode as the skill-progression ladder, campaign as the aspiration

---

## 13. Scenarios, journal, accessibility

Three features borrowed from the closest comparable, all confirmed in scope.

### 13.1 Scenarios

Authored setups with **fixed biome, fixed drop point, sometimes pre-chosen gear, and a specific goal**. Because the parameters are locked, everyone plays an identical problem — which is what makes a leaderboard meaningful. It's a puzzle with a best solution, not a luck contest.

Examples: *"Five items and it's already October." · "Great shelter, no cutting tools." · "Salmon run in 3 days — you're 6 tiles inland."*

**Daily Challenge:** same seed for everyone, 24 hours, one attempt. Primary short-session hook and the answer to "where's the 5-minute session?"

### 13.2 Journal

Narrates your run back to you: major events, first frost, the day you took the deer, the day you stopped building.

**This is the loss-reward** — the thing you take away from a failed day 19. Note honestly: the emotional core of the show is confessionals and faces, and we get a hunger bar. **The journal is where that gap is closed or not closed, which makes this partly a writing project, not purely a systems project.** Resource it accordingly.

*(Rejected during design: player-authored video-diary confessionals with pre-filled response boxes. Doesn't translate — the player knows they're hungry, they don't feel they miss their family.)*

### 13.3 Accessibility — DECIDED (was `Q9` / `D2`)

**Scope reduced for MVP. Split into two tiers with different timelines.**

#### Tier 1 — ships in MVP: structural hygiene

Not a feature, an architectural property. Built into UI work **from M2 onward**, not retrofitted:

- Correct focus order on every screen
- Every interactive element labelled
- Dynamic state changes announced (morale drop, fire dying, daylight fading)
- Menus preserve position
- Action results readable in full
- Large tap targets
- No gesture that conflicts irreconcilably with screen-reader navigation

**Why this can't be deferred even though the rest can.** Screen-reader failures are architectural, not cosmetic. Focus order, announcement timing, and gesture conflicts with the drag-based archery input are all decided by how the UI is *built*. Build the UI at M2 and first test screen readers at M5 and the outcome is a UI rebuild. Doing it as you go costs days; retrofitting costs weeks. **This is a cost-avoidance decision, not a values decision.**

#### Tier 2 — deferred post-launch: dedicated non-visual minigame variants

**Cut from MVP:** the audio archery variant (drifting-tone sway, rhythm-based release) and haptic fishing tension.

**Auto-resolve (§7.3) is the accessibility path for minigame content in MVP.** At 0.70× attribute-expected outcome it is playable, coherent, and consistent with what every sighted player who skips a minigame receives.

**Acknowledged cost, stated plainly:** a blind player in MVP never experiences the signature mechanic. They play a strictly reduced version of the game's central idea. That is a real gap, it weakens accessibility as an earned-media story, and it is accepted as a deliberate scope trade rather than an oversight.

Tier 2 is the **first post-launch accessibility work**, and Tier 1 is what keeps it cheap to add later.

---

## 14. Tutorial

**A separate experience, not a hobbled first run.** Confirmed.

Covers: what each attribute does · one instance of each minigame · the slot economy · one or two example choice-events (not the full catalogue — don't spoil discovery) · reading a risk profile · the calorie/warmth relationship.

Cannot be failed. Archetype presets (§4.2) are the second half of the onboarding answer.

---

## 15. Monetisation — DECIDED (was `Q10` / `D4`)

**Free to download. Monetised on biomes and cosmetics only.**

### 15.1 The model

| | |
|---|---|
| **Download** | Free |
| **Paid content** | Additional biomes (the primary driver) |
| **Paid cosmetics** | Camp/character/journal skins |
| **Ads** | **None.** Not rewarded, not interstitial, not banner. |
| **Timers, energy, lives** | **None.** |
| **Paid gear** | **Explicitly excluded — see 15.2** |
| **Paid attributes / carry** | **Explicitly excluded** |

Rationale: this resolves marketing's discovery objection and analytics' sample-size objection simultaneously, without touching the "no ads, no manipulation" constraint. The thing being sold is *more game*, which is the only IAP that doesn't require designing a problem in order to sell the solution.

### 15.2 Why gear is not sold

Gear was considered as a purchasable category and **rejected on design grounds, not commercial ones.**

The gear design rule (§6.2) is that gear *multiplies strengths*. That makes any paid item a paid advantage by construction. In a last-out contest with leaderboards and a shared-seed Daily Challenge, that is pay-to-win in the most legible possible sense — and it would poison the one competitive surface the game has.

**Cosmetics and biomes carry no competitive weight. That's the whole reason they're the right products.**

### 15.3 Consequence — Vancouver Island now carries two jobs

The free biome must work as both the **demo** and the **retention loop**. It has to be complete enough to be worth playing indefinitely, and good enough to make a second biome feel necessary. That's a higher bar than "MVP content."

Two knock-ons:

**Scenarios move from nice-to-have to core.** They're short-form, high-variety content that extends the free biome's life without new terrain art. Combined with the Daily Challenge, they are the free-tier retention engine (§13.1).

**At least one post-launch biome should ship free.** A player needs to *feel* biome variety at least once before being asked to pay for it. Selling the first taste of the core value proposition is the reliable way to convert nobody.

### 15.4 The plateau now sits on the conversion moment

Under premium, a player drifting off at day 30 was a retention statistic — the money was already taken. Under free-to-play, **day 30 is approximately where the game asks for the purchase.** If the late game is flat, players don't churn quietly after paying; they churn *instead of* paying.

This raises the priority of §12.3 (the retention gap) and §7.4 (late-game pressure and relocation) from "polish" to "conversion-critical."

> **Analytics requirement:** conversion rate segmented by furthest day reached. If conversion collapses above day 25, the plateau is the commercial problem, not just the design one.

---

## 16. Art direction

Stylised 2D top-down/isometric. Placeholder art for the entire MVP.

**Warning carried from review:** the core loop is, stripped of presentation, picking from a menu five times a day and reading results. Don't Starve survives that because it is beautiful and strange. *"Simple 2D for ease of development"* is a scope decision, not an art direction — **art becomes the marketing, not an afterthought.** Budget a real art direction pass before soft launch.

---

## 17. MVP scope

**In:**
- One biome: Vancouver Island (temperate rainforest, tidal shore, salmon, deer, black bear, relentless rain) — **free tier, carries both demo and retention (§15.3)**
- 6 archetype presets, no custom allocation
- 24-item gear list
- **2 minigames: archery and fire** (§9 — decay carrier + gear carrier)
- All other activities auto-resolve
- Full body sim (§5) — this is the game
- Four end conditions with skill-banded distribution target (§5.5)
- Morale attribution: HUD breakdown + day summary + weekly medical read (§5.6.1)
- 12×12 map, fog, depletion
- **Camp relocation (§7.4)**
- 9 AI rivals, check-in intel
- Weather + 12 event types
- Journal, tutorial, one scenario + Daily Challenge
- Full run-history telemetry (§18.1)
- Placeholder art throughout
- Accessibility Tier 1 — structural hygiene from M2 (§13.3)

**Out (v1.1+):** additional biomes · custom attribute allocation · **foraging ID minigame** · fishing/chopping/trapping/whittling minigames · **trap placement** (strongest post-MVP candidate) · **non-visual minigame variants (accessibility Tier 2)** · async real-player rivals · matchmaking tiers · full 40-item list

---

## 18. Playtest priorities

In order:

1. **Does the archery shake read as *my body failing* or as *the game cheating*?** Everything depends on this.
2. **Is the fasting build actually beaten?** (§5.5 balance target)
3. **Does compression fix the day 20–40 sag,** or does it just skip the game?
4. **Is the stalk phase legible on a phone,** and does wind survive?
5. **Does a new player survive past day 10** on their first real run?
6. **Does the gear meta collapse** to the show's known-optimal six?
7. **Can players name their own cause of death?** (§5.6.3 — ≥70%)
8. **Does relocation feel like a pivot or a punishment?** (§7.4)

### 18.1 Run-history telemetry — the balance instrument

**Requirement: every run is recorded as a replayable life-cycle, not a set of event counters.**

Aggregate counters answer *what happened*. They cannot answer *why*, which is the only question that helps tune a game whose central mechanic is a feel judgement. A full run record lets a balance question be re-asked against historical data later, without shipping new telemetry and waiting a month.

**Per run, captured:**

| Layer | Contents |
|---|---|
| **Setup** | Seed, archetype or custom allocation, all six attribute values, starting weight and BF%, full 10-item loadout, biome, difficulty mode |
| **Decision trace** | Every action taken, in order, with slot index, day, tile, and the risk profile shown at the time of choice |
| **Minigame results** | Per attempt: performance scalar, shake amplitude, energy ratio, cold stress, lean-mass ratio, outcome |
| **Body timeline** | Snapshot each day: weight, lean/fat split, morale, morale modifier breakdown, core temp deficit, cache days |
| **Rival state** | Rival tap-out days and causes |
| **Ending** | Final day, cause (one of four, §5.5), the sim's identified point-of-no-return, and the player's post-run self-reported cause |

**This is nearly free architecturally.** The command log already required for Daily Challenge replay validation (§tech doc) *is* the decision trace. The work is persisting it, attaching the body timeline snapshots, and uploading — not building a new capture system.

**Primary derived views:**

1. Cause-of-death distribution **by skill band** (§5.5) — is any pillar dead weight?
2. Final day **by run index** — does run 5 beat run 1? If not, the knowledge-persistence model has failed.
3. Loadout pick rate vs. solver-optimal — the gap is where the design is miscommunicating (§Q3)
4. Self-reported vs. actual cause — the legibility gate (§5.6.3)
5. Conversion rate by furthest day reached (§15.4)
6. Check-in uptake and the survival delta between takers and non-takers

> **Privacy:** run records are gameplay data only. No PII. Disclose in the store listing and settings; provide an opt-out that disables upload without disabling the game.

---

## 19. Open questions

### Closed

| # | Question | Ruling |
|---|---|---|
| Q4 | Camp relocation mid-run | **IN.** Triggered by food exhaustion or seasonal shelter inadequacy; costs the built shelter; must be signalled before commitment (§7.4) |
| Q8 | Retention for the day-30 plateau | **All three mitigations layered** (§12.3) + relocation (§7.4). Now also conversion-critical under F2P (§15.4) |
| Q9 | Minigame accessibility | **Two tiers.** Structural hygiene in MVP from M2; non-visual minigame variants deferred post-launch (§13.3) |
| Q10 | Monetisation model | **Free to download; biomes + cosmetics only.** No ads, no timers, no paid gear (§15) |
| Q11 | Target run length | **45–75 minutes** confirmed |
| D1 | MVP minigame count | **Two: archery + fire.** Trap placement cut, first post-MVP candidate (§9) |
| D5 | Is morale legible enough to be a primary fail state? | **Solved by attribution, not forecast.** Plus a measurable gate: ≥70% correct self-identification of cause (§5.6.3) |
| — | Is morale the *only* death? | **No — four end conditions**, with a skill-banded distribution target and a >60% dominance ceiling (§5.5) |

### Open

| # | Question | Owner |
|---|---|---|
| Q1 | Split Fishing out of Hunting? | Design |
| Q2 | Fasting-build balance target confirmed? | ✅ **Closed — but not as designed.** Morale defeats it, physiology doesn't (§04 §7.2–7.3) |
| Q3 | Does gear choice collapse to a dominant stack? Needs solver | Analytics |
| Q5 | Does wind survive the stalk phase on mobile? | Design + Test |
| Q6 | Async real-player rivals — when, and how are runs recorded? | Tech + Product |
| Q7 | Matchmaking tiers | Deferred |
| Q12 | Sex/gender selection — affects BMR, BF floors, medical thresholds | Design |
| Q13 | **Biome pricing and release cadence** — how many paid biomes, at what price, how often? | Product |
| Q14 | **Which post-launch biome ships free** (§15.3) | Product |
| Q15 | **Cosmetic surface** — what is actually skinnable? Camp, character, journal? This game has less cosmetic surface than most F2P titles | Design + Art |
| Q16 | Does relocation read as a strategic pivot or as confiscation? | Design + Test |
| B1 | **Is the protein ceiling legible enough to be fair?** Highest-risk item in the economy | Design |
| B3 | Is bear-as-only-sustainable-fat-source too concentrated? | Design |
| B5 | Arrow scarcity vs archery being the signature mechanic — tension or mistake? | Design |
| B7 | Elk at 177,000 kcal — does a lucky day-3 elk trivialise month one? | Design + Analytics |
| B8 | Should rival AI run the protein/fat model, or a cheaper approximation? | Design + Tech |

*Full balance question list: `04-balance-economy.md` §10.*
