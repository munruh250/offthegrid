# OFF THE GRID — As-Built Simulation Reference v1.0

*What the code actually does, where it differs from docs 01–04, and why. Written because the simulation moved a long way during balance work and the design documents no longer described it.*

> **How to use this.** Where this document and docs 01–04 disagree, **this one is correct** — it is generated against the shipping code and every figure is reproducible from a `Solver` sweep. Docs 01–04 remain the design intent and the reasoning; this is the state of the build.

---

## 1. Constants that changed

| Constant | Doc | **As built** | Why |
|---|---|---|---|
| Protein ceiling | 2.5 g/kg (04 §3.3) | **3.2 g/kg** | At 2.5 the day-60 arc was structurally unreachable at any slot count — a 2,402 kcal/day deficit against a 1,869 budget. 2.5 sits at the conservative end of the literature. **Ratified.** |
| Morale start | `70 + 3·Resolve` (01 §5.6) | **`82 + 0.8·Resolve`** | Resolve measured 3.24 days per point against the next attribute's 1.03. Rescaled twice toward a 1.25 target; now 1.39. **Ratified, balance doc §7.5.** |
| Memory resolve divisor | 12 | **42** | Same rescale. |
| Tap-out fragility divisor | — | **56** | New — see §2.4. |
| Fitness efficiency | 0.02/point (01 §5.2) | **0.068/point** | Fitness's work-capacity bonus is absorbed by the protein ceiling; burn efficiency is its only uncapped lever. |
| Deficit partition | not specified | **0.88 from fat** | Derived from doc 7.1's validated weight curve. Reads as 48/52 by mass because lean tissue is ~1,020 kcal/kg against fat's 7,700. |
| Lean tissue energy | not specified | **1,020 kcal/kg** | Needed once lean mass became a tracked quantity. |
| Marrow yield | 180 kcal/kg bone (04 §3.4) | **60 g fat/kg** (~540 kcal) | The doc figure prices marrow alone; a rendering slot recovers marrow, bone grease and trimmed fat. |
| Thermoregulation | not specified | **320 kcal per clo of deficit** | At the original 90 it was *cheaper to be cold than to fix it* — measured, shelter-builders died more often than players who ignored shelter entirely. |

---

## 2. Systems that did not exist in docs 01–04

### 2.1 Attributes — now **seven**, pool **38**

Fishing was split out of Hunting. `[Q1]` in design spec §4.1 flagged exactly this; the evidence: Hunting governed **three of four food routes** against Foraging's one, and two contestants on identical loadouts differed 22% to 3% on nothing but Hunting 6 against Hunting 3.

| Attribute | Governs | Days/point |
|---|---|---|
| Bushcraft | shelter, camp, firewood, comfort projects, cache size | 0.91 |
| Hunting | stalking and the trap line — what you **pursue** | 0.83 |
| **Fishing** | nets, lines, weirs | — |
| Foraging | shore, berries, plants — what you **gather** and identify | 0.98 |
| Fitness | burn efficiency, work output, scouting | 0.93 |
| Resolve | starting morale, crisis resistance, tap-out | 1.39 |
| Cold Adaptation | clo demand offset, overnight sleep quality | 1.08 |

Spread 1.67× — from 55× before balance work.

### 2.2 Camp structures

Five buildable structures beyond shelter. Balance doc §4 priced preservation throughput and shelf life but gave the player nothing to build.

| Structure | Slots | Capacity | Process | Loss | Shelf | Predator | Notes |
|---|---|---|---|---|---|---|---|
| Light cache | 2 | 10 kg | — | — | 6 d | 45% | Cheap, keeps animals off |
| Cache pit | 3 | 22 kg | — | 5% | 12 d | 70% | Best hiding |
| Drying rack | 3 | 12 kg | 12 kg/slot | 25% | 30 d | 20% | Fast, wasteful |
| Smoke rack | 4 | 14 kg | 8 kg/slot | 15% | 20 d | 30% | Slow, careful |
| Cold cache | 5 | 34 kg | — | — | 120 d | 80% | **Only functions below freezing** |

### 2.3 Raw versus preserved food

A kill arrives **raw** and spoils in ~3.5 days — or not at all below freezing. A `PreserveFood` slot converts raw to stores at the throughput of whatever rack is built, taking its loss on the way in.

This is what makes a big kill an *obligation* rather than a windfall: several slots of processing, immediately, while the rest sits on a clock. Food rots because you did not get to it, not because the game decided.

### 2.4 Acute events and voluntary tap-out

Not in the original docs at all. Three event types — memory, storm, injury — with a settling-in window over the first ten days.

**Tap-out is a decision available every day**, not a morale bar reaching zero. Weighted in three phases: settling-in (×2.2), the day-20-to-45 grind (×1.6), then committed (×0.9). Resolve gates both crisis frequency and severity, which is what produces a front-loaded drop-out curve from the roster's composition rather than from a rule.

**Ruling (doc 17 §C1):** a lethal outcome must follow from a player action with the odds shown — via the Attempt Meter, not a roll.

### 2.5 Fire and firewood

Balance doc §4's fuel model had no mechanical effect; `ChoppingWood`, `Sawing` and `HaulingLogs` cost calories and produced nothing. Wood is now consumed nightly against a temperature-driven demand, and fire contributes up to **1.9 clo** scaled by how well it is fed. A ferro rod moves fire reliability from 65% to 100%.

### 2.6 Physical capacity

`PhysicalCapacity = lean mass ÷ reference lean for height`, clamped 0.65–1.25. Scales what a work slot returns.

Added because lean mass cost calories and bought nothing, producing a clean exploit: **minimise muscle, maximise fat.** The two winners of a 200-contest sweep carried ~52 kg lean against a field average of 64. It also decays as the body wastes — the body-failing thesis applied to *what you can still get done*.

### 2.7 Season schedule and biomes as data

Seasons are a **scenario parameter**: Standard (winter day 51), Short Summer (day 21), Long Fall (day 70). Biomes carry a temperature *range*, so an early winter is a genuinely early cold.

Two biomes exist: **Vancouver Island** (mild, food-led) and **Boreal Interior** (−15 °C, where the cold economy is the threat), plus a **Proving Ground** control used only for testing.

### 2.8 Territory, per route

Ground quality is tracked **per route** and rolled independently at the drop. Scouting finds ground for a route you are actually working, in steps rather than a trickle. Territory scales animal **condition** — fatter animals — not encounter frequency, because encounter rates saturate near 1.0 and a ceiling-limited player gains far more from fat than from quantity.

### 2.9 The contest

Ten contestants stepped in lockstep, each with a deterministic seed derived from the contest seed. Rivals run the **same physics**; they differ in never playing a minigame, so they land on expected value while a skilled player beats it. **That is what makes the minigames decide the contest.**

Four temperaments — aggressive hunter, patient builder, conservative rester, steady provider — as small leans on a shared need-scoring policy.

**Roster invariants, asserted by test:** 38 points each, seven attributes each, and **every contestant prioritises a food method at 7+**.

### 2.10 Relocation

Doc 12's system is implemented: local depletion, both triggers, the carry limit, and shelter-loss morale capped below the rebuild reward.

---

## 3. Current measured state

| Measure | Value |
|---|---|
| Route returns hitting target | 16 of 16, within 0.2 |
| Attribute spread | 1.67× |
| Distinct winners | 9 of 10 |
| Win band | 33 points *(target 10)* |
| Tap-outs | day 1 to 58, median 44 |
| Winner finishes | day 51 |
| Tests | 167 passing |
| Balance checks | 8 of 8 green |

**Known open:** Dana at 33.2% is the outlier — Hunting 9 on a route worth 2.6× a slot. Conservative resters take ~0% **by design ruling**: viable after a big kill or as a late-game move to stretch the last days, not a way to win.

---

## 4. What is still unbuilt

| Item | Where specced |
|---|---|
| Crisis system — six crises, one per attribute | doc 17, fully specced |
| The Attempt Meter | doc 17 §C1 |
| Minigames (archery, fire) | design spec §9 |
| Check-in intel surfacing | contest supports it; no UI |
| Cross-device determinism harness (Android) | doc 13 |
| Save/restore round-trip | `RunRecord` serialises; never tested end to end |
| All UI | doc 19 |

---

*Generated against the shipping code. Every figure reproducible from `Solver` sweeps and the marginal-value probes.*
