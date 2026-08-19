# OFF THE GRID — Screen Inventory & Element Spec v0.1

*Every screen, everything on it, and what each element reads from the simulation. Written before UI work starts so that the build has a target rather than a vibe.*

> **The rule this document exists to serve.** Design spec §5.6.1 sets it: *the player is never told something changed without being told why.* Doc 15 found the cost of breaking it — five separate constants existed in the design docs and were read by nothing. **A number the player cannot inspect is a number they will not trust**, and the ≥70% cause-of-death gate (§5.6.3) is measured against exactly that.
>
> The design ruling on this was made explicitly: the game gets **inspectable state screens** rather than transient toasts. Everything the simulation uses is reachable if the player goes and looks.

---

## 0. Visual direction — locked

From doc 07, unchanged:

- **Broadcast lower-third HUD.** Angular clipped tab, hard left rule, dense stat strip.
- **No rounded corners. No bordered cards.** Type and rules do the separating.
- **Archivo**, weights 300–900. Extreme weight and size contrast.
- **Seasonal palette** interpolates on `t = (day − 1) / 59`, Cedar & Lichen → Cold Front. *The UI itself is a difficulty readout.*
- Grain texture over everything. Asymmetric alignment.

**Target references:** survey sheets, field journals, orienteering maps, expedition logbooks. Not game UI.

---

## 1. Screen map

Fifteen screens in three tiers.

| Tier | Screens |
|---|---|
| **Always reachable** | Camp · Map · Shelter · Gear · Larder · Journal |
| **Moment** | Day summary · Check-in · Crisis prompt · Attempt meter · End of run |
| **Setup** | Character · Loadout · Biome & scenario · Minigame scenes |

---

## 2. Camp — the day loop

The default screen. Everything else is reached from here.

| Element | Reads from | Notes |
|---|---|---|
| Day counter + season | `Run.DayNumber`, `Calendar.SeasonForDay` | Season named, not numbered — "lean season", not "phase 3" |
| Slot row (3–7) | `Calendar.SlotsForDay` | **The slot count shrinking is the difficulty curve.** It must be visible, not inferred |
| Assigned activity per slot | `DayPlan.Slots` | Tap to change. Shows cost in kcal *before* committing |
| Morale bar + mood glyph | `MoraleState.Current` | Tappable → full attribution breakdown |
| Body strip | `BodySnapshot` — weight, BF%, condition | Weight loss shown as a delta, not just a figure |
| Food readout | `Larder.DaysOfFood`, `RawKg`, `PreservedKg` | **Raw shown separately.** Raw is on a clock and that must be legible |
| Warmth readout | `AvailableClo` + `FireClo` vs `CloDemandTonight` | Shown as a *gap*, which is what relocation trigger B keys off |
| Wood | `Run.WoodKg` vs `Firewood.NightlyDemandKg` | "Two nights of wood", not a kilogram figure |
| Weather look-ahead | one-day forecast | **Required by doc 17** — every crisis needs a signal before it fires |
| Contestants remaining | `Contest.Remaining` | Only in check-in mode |

**Element that does not exist yet and is required:** the **collapse warning**. `Run.CollapseRiskIfPushing(n)` returns the odds; the player must see them before committing movement slots.

---

## 3. Shelter

| Element | Reads from |
|---|---|
| Current tier, named in plain language | `Run.Shelter` → *"day 3: basic coverage" → "roofed" → "winter-proofed"* |
| Clo contributed, and the running total | `ShelterTable.Get(tier).Clo`, `AvailableClo` |
| Progress toward next tier | `ShelterProgressSlots` vs the tier's cost |
| **What the next tier needs** | slots, logs, cordage — **and the tools**, since axe/saw are hard gates |
| Tonight's demand, and the gap | `CloDemandTonight` |
| Camp structures built | `Larder.Built` |
| Structure under construction | `Run.BuildingNow` |

**The stage names matter more than the numbers.** "Winter-proofed" tells a player they have solved a problem; "3.0 clo" does not.

---

## 4. Gear

The screen doc 17 depends on most — it is where crisis signals live permanently.

| Element | Reads from |
|---|---|
| Ten items, with condition | `Loadout`, plus per-item wear once the crisis system lands |
| **What each item contributes** | warmth, what it unlocks, what it multiplies |
| Hard gates called out | *"No bow — big game is not available to you"* |
| Wear warnings | *"the bowstring is starting to fray"* — persistent, not a toast |
| Repair cost | slots and materials |

Crafted items show their own contribution — a beaver-fur headpiece reads **"+3 warm"**, never a silent adjustment to a hidden total.

**This is the answer to the consent rule.** A player who can inspect gear condition, shelter stage and territory can reconstruct why they died.

---

## 5. Larder

| Element | Reads from |
|---|---|
| **Raw vs preserved, separated** | `Larder.RawKg`, `PreservedKg` |
| Days of food | `DaysOfFood` — protein over the daily ceiling |
| Macro split, protein against the ceiling | `NutritionModel` — **the protein/fat bar from B1** |
| The ceiling-bound state, in words | *"You're eating enough. You're not eating the right thing."* |
| Capacity used | `Larder.CapacityKg` |
| Spoilage rate today | driven by night temperature |
| Bone awaiting rendering | `Larder.BoneKg` |
| Processing throughput | `ProcessingThroughputPerSlot`, or *"nothing built to preserve with"* |

**B1 lives here** — the highest-risk item in the whole design. "Full cache, still starving" is correct and reads as a cheat unless this screen sells it.

---

## 6. Map and scouting

Doc 10's merged system: Voronoi territories, elevation-gated contour, on-demand route line.

| Element | Reads from |
|---|---|
| Territory quality **per route** | `Territory.For(activity)` — four figures, not one |
| Best route here | `Territory.Best` — *"this is fishing country"* |
| Local depletion | `Run.LocalDepletion` — **visible from 0.60**, trigger fires at 0.35 |
| Last discovery | `Run.LastDiscovery` — *"found a berry slope"*, a moment |
| Relocation availability + reason | `CanRelocate`, trigger state |
| Terrain difficulty per area | before entry, since terrain is a calorie cost |
| Carried map intel from past runs | design spec §12.2 |

---

## 7. Morale, expanded

Design spec §5.6.1's tier 1. **The same component renders the cause-of-death analysis** — same data, built once.

| Element | Reads from |
|---|---|
| Every active modifier, with value and source | `MoraleBreakdown.Contributions` |
| Ordered by magnitude | already sorted |
| Plain-language labels | `MoraleSource.Label()` |
| Band state | warning below 25 |
| Consecutive idle days | `ConsecutiveIdleDays` |

---

## 8. Day summary

Tier 2. **Two or three largest movers, everything else as one "other" line** — `MoraleBreakdown.TopMovers(3)`. Spec §5.6.1 is explicit that showing all ~40 daily modifiers is toast-spam that trains players to dismiss without reading.

Also: harvest, net calories, weight change, what the fire held, tomorrow's weather.

---

## 9. Check-in — standings

**Free and periodic**, every 10–14 days. Design ruling: the show's medical check is mandatory and free — the crew comes to you — so charging half a day for it is *less* authentic, not more.

| Element | Reads from |
|---|---|
| How many remain | `Contest.Remaining` |
| Who has gone out since last time | `Standings.RecentlyOut` |
| Their day and cause | trace |
| Your body composition, read back | `BodySnapshot` |
| Morale trend across the week | `RunRecord.Trace` |

Periodic beats continuous: *"three more gone since last week"* lands where a live counter does not.

---

## 10. Crisis prompt

Doc 17. Every crisis needs a **signal before it fires** — this is where the signal is answered.

| Element | Notes |
|---|---|
| What is happening, plainly | *"The wind is backing round to the south-east"* |
| What it will cost | so the decision is informed |
| The options, with their costs | act now, or take the risk |
| What it traces back to | ~70% consequence, ~30% indifferent nature — **and the player should be able to tell which** |

> `[Playtest gate]` Watch for **misattribution** — a player blaming themselves for an act of nature, or shrugging off a consequence as bad luck. Either means the signalling is wrong regardless of the ratio.

---

## 11. Attempt meter

The shared commit interaction from doc 17 §C1. **Not a minigame** — one component reused across chopping, hauling, climbing, crossings and stalking.

```
CHOPPING      [=========|##|===============]
                        ^ 5% window

  You are exhausted and badly underfed.
  Your hands are not steady.    [ SWING ]  [ REST INSTEAD ]
```

- Zone width from body condition, fatigue, attribute, tool quality
- **Failure severity scales with how badly it was missed** — not a second roll
- **Appears only above a risk threshold.** Its appearance on a previously automatic action *is itself the signal*
- Auto-resolve uses **identical odds**, and still states them
- Accessibility Tier 1: a non-timing variant, never a push to auto-resolve

---

## 12. Character creation

**Seven attributes now, 38-point pool**, soft cap 8.

| Element | Notes |
|---|---|
| Seven sliders with live point count | Bushcraft · Hunting · **Fishing** · Foraging · Fitness · Resolve · Cold Adaptation |
| **What each level actually does** | the per-level effects, visible while choosing |
| Body setup: weight, body fat | 55–160 kg, sex-gated BF floors |
| **Battery readout** | fat mass in kcal — the single most consequential number on the screen |
| Archetype presets | default for new players; custom unlocks after one completed run |
| **Food-plan warning** | *"You have no prioritised way to get food."* Design ruling: nobody arrives without a plan for eating |

---

## 13. Loadout

| Element | Notes |
|---|---|
| Ten slots from the item list | |
| **Hard gates stated on the item** | *"Without a bow, big game is unavailable"* |
| Multipliers stated | *"A gillnet fishes twice per slot"* |
| Biome hint | a bow is near-dead weight on Vancouver Island and first pick in the Arctic |
| Warning on an unworkable kit | no tackle, no snare, no bow = no food route at all |

---

## 14. End of run

| Element | Reads from |
|---|---|
| Days survived, placing | `Contest.Placings` |
| **Cause, told as a story** | Spec §6: *"pulled at 17.1 BMI"* is a rules citation. *"You'd been in deficit for 19 straight days; the medic made the call"* is the same fact told properly |
| The last 20 days, as a trace | `DecisionTrace.RecentDays` |
| Weight and morale curves | |
| **"What ended your run?"** | one tap, measured against the sim's record — this is the §5.6.3 gate |
| Shareable summary card | marketing item A19 |

---

## 15. Build order

Sequenced so each screen unblocks the next, and so the earliest screens are the ones the sim can already feed.

| # | Screen | Why here |
|---|---|---|
| 1 | **Camp** | Nothing else is reachable without it |
| 2 | **Larder** | B1 is the highest-risk item in the design; needs the most iteration |
| 3 | **Morale expanded** | Same component as cause-of-death — built once, used twice |
| 4 | **Shelter + Gear** | Where crisis signals live; doc 17 cannot ship without them |
| 5 | **Attempt meter** | One component, unlocks every risky action |
| 6 | **Day summary** | Cheap once morale attribution exists |
| 7 | **Character + Loadout** | Needed before playtesting builds |
| 8 | **Map** | Largest single piece; doc 10 has the direction |
| 9 | **Check-in** | Meaningless until the contest is playable |
| 10 | **End of run** | Measures the §5.6.3 gate, so it needs everything above |

---

## 16. Resolved questions

### U1 — Gear condition reads in **USES REMAINING** ✅ DECIDED

Neither "a number" nor "descriptive text" as originally posed. A percentage does
not answer what the player actually wants to know — *will this last the week?* —
and text alone cannot separate fraying at 60% from fraying at 30%.

**"About 40 shots left in this string."** Precise, field-craft rather than a
health bar, and it answers the repair-or-hunt decision directly. A descriptive
state (*sound / worn / failing*) rides on top as the glanceable version.

`GearDurability` now carries per-item tuning: uses, repair cost, whether repair
consumes cordage, and the fraction at which the player is warned.

| Item | Uses | Repair | Warn at |
|---|---|---|---|
| Bow and arrows | **400** *(balance doc §7)* | 2 slots + cordage | 25% |
| Gillnet | 220 | 2 slots + cordage | 30% |
| Fishing line and hooks | 300 | 1 slot | 25% |
| Snare wire | 260 | 1 slot | 25% |
| Axe | 520 | 1 slot | 20% |
| Saw | 430 | 1 slot | 20% |
| Knife | 900 | 1 slot | 15% |
| Sleeping bag | 380 | 2 slots + cordage | 25% |
| Tarp | 300 | 1 slot + cordage | 25% |
| Paracord | 180 *(consumed)* | — | 30% |
| Pot | 4,000 | 1 slot | 10% |
| Ferro rod | 3,000 | — | 15% |

Things that take load fail within a run; things that only get carried do not.
Wear is attributed to the work that causes it — a hunting slot wears the bow, a
chopping slot wears the axe.

### U2 — Palette follows the **season schedule** ✅ DECIDED

`t = (day − 1) / (WinterArrives − 1)`, not the fixed `/59` doc 07 locked. Under a
short-summer scenario the old formula would show early autumn while snow was
falling. The UI is a difficulty readout, so it has to read the difficulty that is
actually happening.

### U3 — The drop shows **~10% of the country**, and its character ✅ DECIDED

Tunable, at `Territory.InitialExploredFraction`. The contestant can tell in their
first day what kind of ground this is — *promising / workable / thin / poor* for
each of fishing, hunting, trapping, foraging — and cannot tell what is over the
ridge.

Mechanically this is **two numbers per route**: what you have **found**, and what
the country **holds**. Scouting closes the gap between them, and a route already
at its potential will not improve however far you walk — which is itself
information, and the reason relocation exists.

The 90% unwalked is the whole argument for spending a slot exploring: your first
camp is a sample, not a verdict.

### U4 — Rivals never use the Attempt Meter ✅ DECIDED

They resolve at expected value, and that gap is precisely what makes the player's
minigame skill decide the contest (B8).

**But check-ins report how someone went out** — *"Rhodes broke his bow on day 31
and never recovered."* The data is already in the trace, it is free drama, and it
teaches a mechanic through someone else's mistake.

### U5 — Larder and Nutrition are **one screen** ✅ DECIDED

The entire point of B1 is that the player connects *"my cache is lean"* to *"I am
starving."* That is the hardest thing in the design to communicate and the
highest-risk item in the project. **Splitting it across two screens breaks the
exact causal link the design needs them to make.**

One screen: what you have on the left, what your body can do with it on the
right, and the protein ceiling bar between them as the thing that connects the
two. The moment the bar caps and the calorie counter stops rising **is** the
mechanic.

---

*Status: v0.1. Written against the as-built simulation (doc 18), not against docs 01–04.*
