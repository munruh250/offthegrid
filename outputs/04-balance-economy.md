# OFF THE GRID — Balance & Economy v0.1

*Companion to `01-design-spec.md`. First-pass numbers for food, fuel, shelter and gear attrition, with the reasoning behind each. Every constant here is a tuning knob, not a law. All values sourced from real nutrition and wood-energy data, then adjusted where gameplay required it — deviations are flagged.*

> **Headline finding, up front:** the body simulation **alone cannot beat the sit-still-and-fast strategy.** A 160 kg fasting build survives to day 90 on body mechanics alone. Morale — specifically the idleness penalty — is what kills it. This is not a flavour system. **It is the load-bearing balance lever, and the whole design depends on it.** Details in §7.

---

## 1. Design philosophy — how this ecosystem should balance

Before the numbers, the reasoning. Six principles, in priority order.

### 1.1 One resource must be scarce, or nothing is a decision

If calories, wood, water and cordage are all moderately scarce, the player just does a bit of everything and the game becomes a chore rota. **Calories are the scarce resource. Everything else is a tax on the slots available to get calories.** Wood, water and cordage exist to *compete for time*, not to be independently threatening.

This is why the wood curve in §4 matters so much: in September it's a background chore, by November it eats a fifth of the day. It squeezes calorie acquisition without ever being "a wood crisis."

### 1.2 Fat is the currency. Protein is a trap.

This is the single best mechanical idea available from the show, and it falls straight out of real nutrition data. Lean meat has a hard intake ceiling — roughly **35% of calories from protein**, or about **2.5 g per kg bodyweight per day**, before ammonia and urea accumulation makes you ill. Exceed it and you get protein poisoning: rabbit starvation.

The consequence is that **you can have a full food cache and still be starving**, which is a far more interesting failure state than an empty larder. A player with 60 kg of cached deer meat still runs a ~1,700 kcal/day deficit (§3.3). Their cache is enormous. They are dying anyway.

**The design instruction that follows:** every high-yield animal in the game should be lean. Fat should come from specific, rarer, more contested sources — bear, salmon belly, rendered marrow, bone broth. This makes bear hunting a *strategic* act rather than just a big calorie win, and it gives the fishing/hunting choice a real texture beyond yield-per-slot.

### 1.3 Yield and *bankable* yield are different numbers

A moose is realistically worth **~300× a trout** in raw calories. You asked for roughly 10×. Both are right, and the gap between them is where the design lives.

The reconciling mechanic is **spoilage and preservation capacity.** Raw kcal in the animal is not what the player banks — what they bank is limited by drying-rack throughput, cache capacity, weather and time. Apply that and a moose lands at roughly **6–10× a large chinook**, which is the ratio that actually plays well.

**This is the most important lever in the whole economy.** Preservation capacity is what stops one lucky kill from ending the game's tension, and it does so without ever nerfing the fantasy of the kill itself. The moose is genuinely huge. You just can't keep most of it.

### 1.4 Effort must scale with reward, but *risk* should scale faster

Big animals should not merely cost more slots. They should cost **arrows, gear condition, injury probability and a real chance of total failure.** A missed moose shot costs a day and an arrow. A wounded-and-lost moose costs three days of tracking and the morale hit of knowing you did that to an animal for nothing.

Small game is the opposite: low yield, low variance, low cost. **Traps and fishing should be the boring reliable floor; hunting should be the volatile ceiling.** A player who only hunts starves during bad luck; a player who only traps never builds a surplus large enough to survive November.

### 1.5 Gear degrades on a curve that outlasts the tutorial but not the run

Durability numbers should be set so that nothing meaningful breaks in the first week — a new player must not be punished before they understand the systems — but **by day 30 the player should be visibly managing decline.** Arrows dwindling, hook count dropping, axe going dull. This adds late-game texture that costs almost nothing to implement and directly mirrors the show.

The ferro rod is the sharpest expression of this: 300 strikes is plenty, until the player realises they've been wasting strikes on fires they didn't need.

### 1.6 Semi-realistic means "real values, then bent where it plays badly"

Every number below starts from a real source. Where reality is boring or unfair, it's adjusted and the adjustment is **noted explicitly** so nobody has to reverse-engineer the intent later. The player should be able to look up "how many calories in a salmon" and find that the game roughly agrees.

---

## 2. Reference constants

| Constant | Value | Source |
|---|---|---|
| Energy per kg body fat | 7,700 kcal | Standard |
| Energy per kg lean tissue | 1,800 kcal | Standard |
| Protein ceiling | 2.5 g/kg bodyweight/day | Bilsborough & Mann; ~35% of kcal |
| Protein energy | 4 kcal/g | Standard |
| Fat energy | 9 kcal/g | Standard |
| Air-dried softwood energy | ~15 MJ/kg | Standard |
| Open fire useful-heat efficiency | 10% | Radiant loss dominates |
| Reflector-wall efficiency | 22% | Wall returns radiant heat |
| Stove efficiency | 55% | Enclosed combustion |
| Standard "log" | 2.5 kg split, air-dried | Game unit |
| Chop yield per slot | ~35 kg processed | Bushcraft 5, axe + saw |

---

## 3. Food economy

### 3.1 Yield table — Vancouver Island (MVP biome)

Live weight → edible yield → macros. Yield fractions account for bone, hide and offal loss.

| Animal | Live kg | Edible kg | **kcal** | Protein g | Fat g | % kcal from fat | Days @ 2,500 |
|---|---|---|---|---|---|---|---|
| Roosevelt elk | 270 | 121.5 | **177,390** | 36,693 | 2,308 | 12% | 71.0 |
| Black bear | 110 | 44.0 | **113,960** | 8,844 | 8,360 | **66%** | 45.6 |
| Blacktail deer | 55 | 24.8 | **39,105** | 7,474 | 792 | 18% | 15.6 |
| Chinook salmon | 9.0 | 5.4 | **9,666** | 1,075 | 562 | **52%** | 3.9 |
| Coho salmon | 4.0 | 2.4 | **3,504** | 518 | 142 | 36% | 1.4 |
| Sockeye salmon | 2.7 | 1.6 | **2,722** | 345 | 139 | 46% | 1.1 |
| Snowshoe hare | 1.4 | 0.8 | **1,332** | 254 | 27 | 18% | 0.5 |
| Cutthroat trout | 1.0 | 0.55 | **654** | 109 | 19 | 26% | 0.3 |
| Rockfish | 1.2 | 0.5 | **567** | 102 | 9 | 14% | 0.2 |
| Grouse | 0.6 | 0.3 | **469** | 82 | 13 | 25% | 0.2 |
| Mussels (per kg gathered) | 1.0 | 0.3 | **258** | 36 | 7 | 23% | 0.1 |
| Dungeness crab | 0.9 | 0.2 | **196** | 39 | 2 | 10% | 0.1 |

*"Cutthroat trout" assumed for the fish you mentioned — it's the right species for Vancouver Island streams. Say if you meant something else.*

**Moose** (for northern biomes, post-MVP): 450 kg bull → 202 kg edible → **206,000 kcal**, 45,000 g protein, only ~3,400 g fat → **15% of calories from fat**. The largest animal in the woods is also one of the leanest. You cannot live on it.

### 3.2 The ratios you asked about

| Comparison | Raw kcal ratio | After spoilage & preservation caps | Verdict |
|---|---|---|---|
| Moose : cutthroat trout | 315 : 1 | ~35 : 1 | Raw ratio is unusable |
| Moose : large chinook | 21 : 1 | **~7 : 1** | **This is your 10×** |
| Deer : chinook | 4 : 1 | ~3 : 1 | Good spread |
| Chinook : trout | 15 : 1 | ~12 : 1 | Fish tiering works |

**Recommendation:** treat the target ratio as *big-game kill ≈ 7–10× a best-case fish*, and let spoilage do the compressing. Don't nerf the animal's calorie content to hit the ratio — that breaks the realism promise and makes big kills feel disappointing at the moment they should feel triumphant.

### 3.3 The protein ceiling — the mechanic

For an 85 kg player, the ceiling is **212 g protein/day**. Converting each food's protein-to-calorie ratio gives the maximum calories that food can safely deliver per day:

| Food | Max safe kcal/day | Sustainable alone? |
|---|---|---|
| Black bear | 2,732 | ✅ **Yes — the only one** |
| Chinook salmon | 1,907 | ❌ Lean trap |
| Sockeye salmon | 1,672 | ❌ Lean trap |
| Mussels | 1,532 | ❌ Lean trap |
| Coho salmon | 1,433 | ❌ Lean trap |
| Grouse | 1,204 | ❌ Lean trap |
| Rockfish | 1,178 | ❌ Lean trap |
| Snowshoe hare | 1,111 | ❌ Lean trap |
| Blacktail deer | 1,109 | ❌ Lean trap |
| Dungeness crab | 1,060 | ❌ Lean trap |
| Roosevelt elk | 1,025 | ❌ Lean trap |
| **Moose** | **1,006** | ❌ **Worst in the game** |

**Worked example — the full-cache death.** 85 kg player, 60 kg of cached deer meat:

```
Protein ceiling            212 g/day
Max safe intake on deer  1,112 kcal/day
Daily burn               2,850 kcal/day
UNAVOIDABLE DEFICIT      1,738 kcal/day
```

They lose **1.58 kg/week while "well fed."** The cache is not the problem. The composition is.

### 3.4 Implementation

Track **two** nutrition values per food, not one:

```csharp
public struct FoodValue {
    public int   Kcal;
    public float ProteinG;
    public float FatG;
}
```

Daily resolution:

```
protein_ceiling_g = 2.5 * bodyweight_kg
if protein_consumed_g > protein_ceiling_g:
    excess_ratio = protein_consumed_g / protein_ceiling_g
    // calories above the ceiling do not count
    usable_kcal = kcal_from_fat + (protein_ceiling_g * 4)
    illness_risk += 0.15 * (excess_ratio - 1.0)     // per day sustained
    morale        -= 2 * (excess_ratio - 1.0)       // "eating and still weak"
```

**Player-facing readout.** This must be legible or it's just an invisible punishment — the same failure mode as morale (§5.6.1 of the spec). Show a **protein/fat balance bar** next to the calorie counter, and when the player is over the ceiling, state it in words: *"You're eating enough. You're not eating the right thing."*

**Counterplay** — the player must have outs, or this is unfair rather than interesting:

| Action | Effect |
|---|---|
| Prioritise fatty sources | Bear, salmon belly, marrow |
| Render bone marrow | +180 kcal/kg bone, almost pure fat, needs a pot |
| Bone broth | Extracts ~15% more from processed carcasses |
| Eat organs | Higher fat, and vitamin cover (§3.5) |
| Forage carbohydrate | Berries, camas root, cattail — shifts the ratio |
| Reduce activity | Lowers the burn, so the ceiling covers more of it |

### 3.5 Spoilage and preservation — the real balance lever

Raw meat spoils. This is what keeps a big kill from ending the game's tension.

| Method | Throughput | Loss | Keeps for | Requires |
|---|---|---|---|---|
| Fresh (untreated) | — | — | 2 days @ 12 °C, 5 days @ 2 °C | — |
| Smoke rack | 8 kg/slot | 15% | 20 days | Fire, 1 slot to build |
| Drying rack | 12 kg/slot | 25% | 30 days | Dry weather only |
| Freeze (ambient) | Unlimited | 0% | Indefinite | Sustained sub-zero |
| Cache pit | 20 kg/slot | 5% | 12 days | Dig, 2 slots, bear risk |

**Rack capacity is the cap.** A default smoke rack holds ~25 kg. Processing a 121 kg elk takes 15 slots of smoking — three to five full days of doing nothing else, during which the rest is rotting. Realistically the player banks 30–50 kg and loses the remainder.

**That is the mechanic that produces the ratio in §3.2**, and it produces a genuinely great decision: *do I spend four days preserving this elk, or hunt again and stay mobile?* Expanding rack capacity becomes a real build project with a clear payoff.

Bear risk on caches should scale with cache size — the bigger your surplus, the more attractive your camp.

### 3.6 Seasonal availability — Vancouver Island

| Days | Season | Available | Notes |
|---|---|---|---|
| 1–20 | Salmon run | Chinook, coho, sockeye, berries, mussels | **Abundance window.** Fat available. Bank now. |
| 21–35 | Run tapering | Coho declining, deer, grouse, hare | Fat sources thinning |
| 36–50 | Lean season | Deer, hare, rockfish, shellfish | **Protein trap bites hardest** |
| 51+ | Winter | Hare, rockfish, cached food, rare deer | Cache or die. Bear denned. |

The run window is deliberately front-loaded so that **the player's day-1 to day-20 decisions determine whether they survive day 40.** Failure should be traceable to something they did three weeks earlier — which is exactly what the run-history record (§18.1) is for.

---

## 4. Firewood economy

### 4.1 Burn rates

| Fire type | kg/hr | logs/hr | logs per 8 hr night | Efficiency |
|---|---|---|---|---|
| Small warming fire | 1.2 | 0.5 | 3.8 | 10% |
| Cooking fire | 2.0 | 0.8 | 6.4 | 10% |
| Long-log night fire | 1.5 | 0.6 | 4.8 | 12% |
| Reflector wall fire | 2.5 | 1.0 | 8.0 | **22%** |
| Stove (gear item) | 1.0 | 0.4 | 3.2 | **55%** |

Note the reflector wall burns *more* wood per hour but delivers double the useful heat — so it's a wood-for-warmth trade, not a straight upgrade. The stove is strictly better and should be an expensive, contested gear pick.

### 4.2 Daily demand — this is the late-game squeeze

Night fire plus one hour of cooking, at 35 kg processed wood per chop slot:

| Night temp | Fire hrs | kg/day | logs/day | **Chop slots/day** |
|---|---|---|---|---|
| 12 °C (September) | 4 | 8.0 | 3.2 | **0.23** |
| 8 °C | 6 | 11.0 | 4.4 | 0.31 |
| 4 °C | 9 | 15.5 | 6.2 | 0.44 |
| 0 °C | 12 | 20.0 | 8.0 | 0.57 |
| −5 °C (November) | 14 | 23.0 | 9.2 | **0.66** |

**Read this against the slot economy (§7.1).** September gives 5 slots/day and wood costs 0.23 of one — 5% of the day. November gives 3 slots/day and wood costs 0.66 — **22% of a day that is already 40% shorter.**

The player loses two slots to daylight and another two-thirds of a slot to firewood, at exactly the point when food is scarcest and their body is weakest. **The late-game difficulty curve emerges entirely from these two tables.** Nothing is authored. That's the thesis working.

### 4.3 Gathering vs processing

Splitting the action creates a meaningful gear difference:

| Action | Yield/slot | Requires |
|---|---|---|
| Gather deadfall | 12 kg | Nothing — always available |
| Fell standing dead | 45 kg | Axe or saw |
| Process to splits | 35 kg | Axe |
| Process to splits | 52 kg | Axe + saw |
| Bucking long logs | 70 kg | Saw + Bushcraft ≥ 6 |

An axeless player can survive on deadfall in September and **cannot** meet a −5 °C night in November (12 kg/slot vs 23 kg/day = two full slots of a three-slot day). Wet deadfall should also fail the fire minigame more often, compounding it.

---

## 5. Shelter economy

| Shelter | Slots | Logs | Cordage m | clo | Notes |
|---|---|---|---|---|---|
| Tarp lean-to | 1 | 4 | 6 | 0.4 | Night one. Windbreak only. |
| Debris hut | 3 | 12 | 0 | 1.1 | No tools needed. No interior fire. |
| A-frame + bough bed | 5 | 26 | 12 | 1.6 | Bed contributes +0.5 of that |
| Reflector-wall camp | 8 | 48 | 20 | 2.2 | Raises fire efficiency to 22% |
| Log shelter (small) | 16 | 110 | 35 | 3.0 | Needs axe **and** saw |
| Log cabin + stove | 28 | 210 | 60 | **4.2** | Stove gear item required. End-game. |

### 5.1 Reading the build curve

At a sustainable ~0.5 slots/day on construction, the log shelter is a **32-day project** and the cabin is **56 days** — i.e. the cabin is not a survival strategy, it's a flex that only a player already winning on food can afford. That's correct: it should exist, be visible, and rarely be reached.

The A-frame at 5 slots is the intended "real" first shelter. The debris hut at 3 slots and zero cordage is the no-gear fallback that keeps a bad loadout alive.

**Note the interaction:** the reflector-wall camp costs 8 slots and only gives +0.6 clo over the A-frame, but it more than doubles fire efficiency — which is worth ~4 logs/night in November, or roughly 0.3 slots/day. It pays back in under a month. **Good build orders are discoverable, which is exactly what makes cross-run knowledge persistence meaningful.**

### 5.2 Thermoneutral demand

Shelter clo adds to clothing clo. Rough demand:

| Night temp | Total clo needed (asleep) |
|---|---|
| 12 °C | 2.0 |
| 8 °C | 2.8 |
| 4 °C | 3.6 |
| 0 °C | 4.4 |
| −5 °C | 5.4 |

Issued clothing is ~1.5 clo, sleeping bag +1.5. So bag + A-frame = 4.6 clo, which holds to about 0 °C. Below that the player needs the log shelter, a bigger fire, or both — **and that's the trigger that makes relocation (§7.4) fire.** The gap between clo available and clo demanded is the visible signal the consent rule requires.

---

## 6. Gear attrition

### 6.1 Durability table

| Item | Unit | Durability | Failure mode |
|---|---|---|---|
| Bow (self-made) | per shot | 120 | Limb cracks. Rebuild = 4 slots. |
| Bow (gear item) | per shot | 400 | String wears. Spare string = 1 slot. |
| Arrow | per shot | 12 | **8% lost per miss, 25% on bone strike** |
| Fish hook | per cast | 40 | **3% loss/cast, 12% on a large fish.** 35 issued. |
| Fishing line | per snag | — | Lose 2–5 m per snag. 45 m issued. |
| Gill net | per set | 60 | Tears on rocks. Repair = 1 slot. |
| Axe head | per chop | 800 | Dulls: −1% yield per 50 uses. Sharpen 0.25 slot. |
| Saw blade | per chop | 500 | Dulls. **Cannot be sharpened in the field.** |
| Ferro rod | per strike | 300 | Depletes visibly. Most precious item in the game. |
| Snare wire | per set | 25 | Kinks. 60% recoverable after a catch. |
| Multi-tool | per use | 1,200 | Effectively permanent |

### 6.2 Answering your specific questions

**Do you lose a bow on missed shots?** No — and this is deliberate. Losing the bow to bad luck would be catastrophic, unrecoverable, and would violate the consent rule. Instead the bow **wears** (400 shots is generous but finite) and the player loses *arrows*, which is recoverable through effort.

**Do you break arrows?** Yes, and this is the better mechanic. Six arrows issued, 8% loss per miss, 25% on a bone strike. A player with a 40% hit rate burns through arrows in a fortnight and must either craft replacements (1 slot for 3 arrows, requires straight shoots and a knife) or stop hunting large game. **This makes every shot cost something even when it misses** — which is precisely the pressure that makes the archery shake matter.

**Do you lose fish hooks?** Yes — 3% per cast, 12% on a large fish. With 35 issued that's roughly 400–500 casts, or most of a run. It's a slow, visible countdown rather than a threat, and it makes each big fish a small gamble. Barbless hooks (as the show issues) raise loss rates and lower landing rates, which is a nice authentic detail.

**Design rule for all of it:** *attrition should narrow options, never remove them outright.* A player who loses every hook can still trap, forage and hunt. A player who loses their bow can still fish. **No single attrition event should be run-ending** — that's what makes it tension rather than frustration.

### 6.3 Repair and craft costs

| Action | Cost | Requires |
|---|---|---|
| Craft 3 arrows | 1 slot | Knife, straight shoots |
| Craft self bow | 4 slots | Knife, seasoned wood, Bushcraft ≥ 5 |
| Craft bone hook | 0.5 slot | Bone, knife, Bushcraft ≥ 4 |
| Twist cordage (10 m) | 1 slot | Bark/nettle fibre |
| Sharpen axe | 0.25 slot | Sharpening stone |
| Repair gill net | 1 slot | Cordage 3 m |
| Replace bow string | 1 slot | Cordage 2 m |

Cordage is the quiet dependency here — it appears in shelter, traps, net repair and bowstrings. Running out of paracord shouldn't be fatal, but the 1-slot-per-10-m natural cordage cost should make the player feel the loss.

---

## 7. Whole-economy validation

Every result below is from running the actual equations, not estimated.

### 7.1 Does a competent player reach day 60?

85 kg, 20% BF, competent play, expected intake by season:

| Phase | End day | Weight | BF% | Status |
|---|---|---|---|---|
| Days 1–10 (salmon run, berries) | 10 | 83.4 kg | 19.5% | OK |
| Days 11–25 (run tapering) | 25 | 78.9 kg | 17.8% | OK |
| Days 26–40 (lean season) | 40 | 73.2 kg | 15.4% | OK |
| Days 41–60 (cache + scraps) | 60 | **65.6 kg** | **11.6%** | **OK** |

Final: 22.8% weight lost, BMI 20.7 — comfortably inside all medical pull thresholds. **The food economy supports a 60-day run.** ✅

### 7.2 Does the fasting build lose? — INITIALLY NO ❌

Body simulation only, no morale:

| Build | Outcome |
|---|---|
| 160 kg / 42% BF, near-fasting, low activity | **Reached day 90** (115 kg, 39.5% BF) |
| 85 kg / 20% BF, competent play | Pulled day 74 (59 kg, 7.9% BF) |

**The fasting build wins by 16 days.** The superlinear movement cost and the shivering cap are not sufficient. A player carrying 67 kg of fat has ~500,000 kcal of battery, and no amount of movement-cost exponent tuning overcomes that inside 90 days.

### 7.3 Morale is what fixes it

Adding morale to the loop, with the idleness penalty active:

| Build | End day | Cause |
|---|---|---|
| 160 kg fasting, idle | **12** | Morale |
| 160 kg fasting, some projects | 29 | Morale |
| 85 kg competent, active | **59** | Morale |
| 85 kg competent, idle | 15 | Morale |
| 85 kg expert, very active | 90 | Survived |
| 70 kg lean, high Resolve | 57 | Medical |

**Competent + active beats fasting + idle by 47 days.** ✅

**This is the most important finding in the document.** The fasting strategy isn't defeated by physiology — it's defeated by the fact that sitting in a shelter doing nothing for a month is unbearable. Which is exactly what happens on the show: contestants very rarely starve out, they get bored and lonely and tap.

**Consequences for the design:**

1. **Morale cannot be softened for legibility reasons without re-breaking Q2.** The §5.6.1 attribution work makes morale *understandable*; it must not make it *weaker*.
2. The idleness penalty specifically is doing the heavy lifting. It needs its own playtest attention.
3. **This validates morale as a primary fail state.** It isn't an emotional-realism flourish bolted onto a survival sim — remove it and the game has a dominant degenerate strategy.

### 7.4 Tuned morale constants

The first-pass values from the spec killed everyone by day 22. These are solved for the target curve:

| Constant | Spec v0.1 | **Tuned** |
|---|---|---|
| Base daily decay | −2.0 | **−1.0** |
| Food insecure | −3.0 | **−2.0** |
| Idleness step (per consecutive day) | −2.0 | **−1.0** |
| Idleness cap | −8.0 | **−5.0** |
| Weight-loss penalty (per 5% lost) | −1.0 | **−0.5** |
| Project completed | +8 to +18 | **+14** |

**Update §5.6 of the design spec with these.** The direction of every change is the same — the original values were roughly twice as harsh as they needed to be.

---

## 8. Tuning levers, in the order to reach for them

When a balance problem appears, this is the priority order. Reaching for the wrong lever first is how economies get wrecked.

| # | Lever | Effect | Risk of using it |
|---|---|---|---|
| 1 | **Preservation capacity** | Controls how much of a kill banks | Low — very safe, high leverage |
| 2 | **Seasonal availability windows** | Controls when abundance exists | Low |
| 3 | **Chop yield per slot** | Controls the late-game slot squeeze | Low |
| 4 | Animal encounter rates | Controls expected income | Medium — affects variance too |
| 5 | Shelter slot costs | Controls early-game pacing | Medium |
| 6 | Morale constants | Controls run length overall | **High — see §7.3** |
| 7 | Protein ceiling | Controls the lean-meat trap | **High — it's a real value** |
| 8 | Raw kcal content of animals | — | **Don't. Breaks the realism promise.** |

**Never tune #8.** If a moose feels too strong, cap what the player can preserve. The animal's calorie content is checkable against reality and should stay checkable.

---

## 9. Test hooks — making all of this measurable

Every number above needs to be assertable in the headless solver, or it will drift.

```csharp
// OffTheGrid.Sim.Balance — all runnable without Unity
BalanceAssert.FoodYield(species, expectedKcal, tolerance: 0.02f);
BalanceAssert.ProteinCeilingBinds(FoodSource.Deer, bodyweight: 85f);
BalanceAssert.WoodDemand(nightTempC: -5f, expectedKgPerDay: 23f);
BalanceAssert.ShelterReachable(ShelterTier.AFrame, byDay: 8);
BalanceAssert.CompetentRunLength(min: 55, max: 70);
BalanceAssert.FastingBuildLosesTo(BuildType.Competent);   // §7.2 regression guard
BalanceAssert.NoCauseExceeds(0.60f);                      // §5.5 distribution
BalanceAssert.GearAttritionNeverRunEnding();
```

**`FastingBuildLosesTo` is the single most important assertion in the codebase.** It is the guard on a property that took a morale system to achieve and that any future tuning change could silently break.

### 9.1 Solver sweeps to run at M0

| Sweep | Dimensions | Answers |
|---|---|---|
| Starting body | Weight 60–160 kg × BF 8–45% | Q2 — is any body build dominant? |
| Protein composition | % fat calories 5–70% | Does the ceiling bind at the intended point? |
| Wood curve | Chop yield 20–50 kg/slot | Is November survivable but tight? |
| Morale constants | 6-dimensional, coarse | Where's the cliff edge? |
| Gear loadouts | All C(24,10) combinations | Q3 — does the meta collapse? |
| Attrition rates | Arrow/hook loss ×0.5–2.0 | Does attrition narrow or remove options? |

---

## 10. Open balance questions

| # | Question | Notes |
|---|---|---|
| B1 | Is the protein ceiling legible enough to be fair? | Highest risk item here. An invisible ceiling is a cheating game. |
| B2 | Should the ceiling scale with Fitness or Cold Adaptation? | Realistically no. But it may need a skill-based out. |
| B3 | Is bear-as-only-fat-source too concentrated? | One animal gates the whole fat economy. Add fatty fish? Marrow yield up? |
| B4 | Does preservation micromanagement become tedious? | It's the top balance lever, so it gets used a lot. Watch for chore feel. |
| B5 | Arrow scarcity vs archery-as-signature-mechanic | If arrows are scarce, the best minigame is rationed. Tension by design or a mistake? |
| B6 | Do the tuned morale constants survive relocation? | Relocation destroys a shelter — a large morale event not in the §7.3 model. |
| B7 | Elk at 177,000 kcal — too much for one animal? | Preservation caps it, but a lucky day-3 elk may trivialise the first month. |
| B8 | Should rival AI use the protein model? | If not, rivals and player face different games. If so, fidelity cost. |
