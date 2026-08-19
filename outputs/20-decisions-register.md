# OFF THE GRID — Decisions Register

*Every ruling made, with its evidence and its consequence. The reasoning lives in the linked documents; this is the index of what is settled so that no one re-opens a closed question by accident.*

**Status key:** ✅ decided · 🔬 decided from measurement · ⚠️ decided but supersedes an earlier doc · ❓ still open

---

## 1. Simulation architecture

| # | Decision | Status | Where |
|---|---|---|---|
| A15 | Mutable internal state with an on-demand read-only projection. The solver never snapshots; the game snapshots once per slot. | ✅ | 18 |
| Q12 | Sex is a physiological parameter — Mifflin–St Jeor constant, body-fat floors, medical thresholds. Both supported from M0. | ✅ | 18 |
| A21/C11 | `RunRecord` + a fixed-capacity ring-buffer decision trace that survives save/restore. | ✅ | 18 |
| C10 | `BalanceProvider` swaps constants between slots, never mid-slot, and marks the run an unclean balance sample. | ✅ | 18 |
| B8 | **Rivals run the same physics as the player.** Fidelity is reduced by feeding expected-value inputs, not different metabolism. | ✅ | 18 §2.9 |
| B11 | Energy is charged as **excess over resting**, not BMR + full MET. The naive reading double-counts the baseline and makes the game unwinnable. | 🔬 | 18 §1 |
| — | Determinism: FNV-1a seeding from (runSeed, streamName). Never `string.GetHashCode()`, which .NET randomises per process. | 🔬 | 18 |

## 2. Balance constants

| # | Decision | Status | Where |
|---|---|---|---|
| — | Protein ceiling **3.2 g/kg**, not 2.5. At 2.5 the day-60 arc was structurally unreachable at any slot count. | ⚠️ | 18 §1 |
| — | Resolve rescaled: `82 + 0.8·Resolve`, memory divisor 42, fragility divisor 56. **Supersedes design spec §5.6.** | ⚠️ | 04 §7.5 |
| — | Thermoregulation **320 kcal per clo of deficit**. At 90 it was cheaper to be cold than to fix it, and shelter-builders died more often than players who ignored shelter. | 🔬 | 18 §1 |
| — | Deficit partition 0.88 from fat, derived from doc 7.1's validated weight curve. | 🔬 | 18 §1 |
| — | Marrow yields 60 g fat/kg bone. The doc's 180 kcal/kg prices marrow alone; a rendering slot also recovers grease and trimmed fat. | ⚠️ | 18 §1 |
| — | Every food route returns **2–3× its slot cost in its own season**. Sixteen of sixteen within 0.2. | 🔬 | 15, 18 |

## 3. Attributes

| # | Decision | Status | Where |
|---|---|---|---|
| **Q1** | **Fishing is split out of Hunting** and is its own attribute — seven total, pool 38. Hunting governed three of four food routes; two contestants on identical loadouts differed 22% to 3% on nothing but Hunting 6 vs 3. | 🔬 ⚠️ | 01 §4.1, 18 §2.1 |
| — | Attributes split by verb: **Fishing** (nets, lines, weirs) · **Hunting** (stalk and snare, what you pursue) · **Foraging** (shore and plants, what you gather). | ✅ | 18 §2.1 |
| — | Cold Adaptation gains an **always-on** effect. It was insurance against your own failure — worth 0.00 with a decent shelter. | 🔬 | 18 §1 |
| — | Fitness's value lives in **burn efficiency**; its work-capacity bonus is absorbed by the protein ceiling. | 🔬 | 18 §1 |
| — | Target values: Resolve 1.25, Bushcraft/Cold/Hunting 1.00, Foraging 0.95, Fitness 0.83 days per point. Measured spread now 1.67×, from 55×. | ✅ | 15 |

## 4. Body and bodies

| # | Decision | Status | Where |
|---|---|---|---|
| — | **Lean mass buys physical capacity.** It previously cost calories and bought nothing, producing a skinny-fat exploit that took 63% of contests. | 🔬 | 18 §2.6 |
| — | Capacity **decays as the body wastes** — the body-failing thesis applied to what you can still get done. | ✅ | 18 §2.6 |
| — | Extreme body ranges are **kept** in the roster. | ✅ | — |

## 5. Food, camp and preservation

| # | Decision | Status | Where |
|---|---|---|---|
| — | A kill arrives **raw** and spoils in ~3.5 days, or not at all below freezing. Processing is a slot decision. Food rots because you did not get to it. | ✅ | 18 §2.3 |
| — | Five camp structures, each a different bet. **Cold cache only functions below freezing.** | ✅ | 18 §2.2 |
| B3 | Fat comes from a **protein-free path** — marrow, grease, trimmings — not from adding fatty animals. Every animal has bones. | 🔬 | 15 |
| B7 | **Elk is a trap disguised as a jackpot**, not a run-trivialiser: 177,390 kcal that delivers 1,315/day. | 🔬 | 15 |
| — | Carbohydrate **bypasses the protein ceiling entirely** — berries are the only food that does. | ✅ | 18 |
| — | Trapping is hare-dominant and returns the same in every season. Hunting carries small game as well as big. | ✅ | 18 |
| — | Fishing narrows across the run rather than collapsing; lingcod is resident year-round. You can still fish through ice. | ✅ | 18 |

## 6. Crises and lethality

| # | Decision | Status | Where |
|---|---|---|---|
| C1 | **A lethal outcome must follow a player action with the odds shown** — via the Attempt Meter, never a roll. | ✅ | 17 |
| — | The Attempt Meter appears **only above a risk threshold**. Its appearance on a previously automatic action is itself the signal. | ✅ | 17 |
| — | Auto-resolve uses **identical odds** and still states them. Accessibility gets a non-timing variant, never a push to auto-resolve. | ✅ | 17 |
| C2 | Inspectable state screens, not toasts. Numbers **and** plain-language stages. | ✅ | 17, 19 |
| C3 | A bad-forage gamble is **winnable**. Foraging skill sharpens the *hints*, not a hidden risk number. | ✅ | 17 |
| C4 | Crises **stack**, and roughly **70% consequence / 30% indifferent nature**. Acts of nature still get signals — you cannot prevent them, only prepare. | ✅ | 17 |
| C5 | High skill **blunts** a crisis, never removes it. | ✅ | 17 |
| — | Terrain is priced in **calories**, not injury rolls. Exploration's consequence is **collapse** — costs the day, never the run. | ✅ | 18 |

## 7. The contest

| # | Decision | Status | Where |
|---|---|---|---|
| — | **The win condition is relative.** Outlast nine others; the battery only has to cover the contest. This is what makes build variety work at all. | 🔬 | 18 §2.9 |
| — | Every contestant spends **38 points across seven attributes**, and **every one prioritises a food method at 7+**. Asserted by test. | ✅ | 18 §2.9 |
| — | Check-ins are **free and periodic**, not a half-day cost. The show's medical check is mandatory and free — the crew comes to you. | ✅ | 19 §9 |
| — | Four temperaments as **small leans**, not obsessions. Large swings made three of four distort a sensible plan while the fourth merely played well. | 🔬 | 18 |
| — | The **conservative rester wins ~0%, by design.** Viable after a big kill or as a late-game move to stretch the last days. A rester is not idle — they build caches and shelter first. | ✅ | — |

## 8. Seasons, biomes and place

| # | Decision | Status | Where |
|---|---|---|---|
| — | Seasons are a **scenario parameter**: Standard day 51, Short Summer day 21, Long Fall day 70. Difficulty changes the *shape* of the problem, not its numbers. | ✅ | 18 §2.7 |
| — | Balance gates measure the **winterization arc**, not a day count. Once winter is movable, "did they reach day 60" measures the wrong thing. | 🔬 | 18 |
| — | **Biomes keep their character** rather than each being internally balanced. Vancouver Island is a food biome; the Boreal Interior is a cold one. Archetype viability is measured *across* the set. | ✅ | 15 |
| — | Ground quality is tracked **per route**, rolled independently at the drop. | ✅ | 18 §2.8 |
| Q4/Q16 | Relocation ships. Cost is a tunable A/B; shelter-loss morale is **capped below the rebuild reward** so the cycle is never a guaranteed death spiral. | ✅ | 12 |

## 9. UI

| # | Decision | Status | Where |
|---|---|---|---|
| U1 | Gear condition reads in **uses remaining**, not a percentage. Per-item durability tuning in `GearDurability`. | ✅ | 19 §16 |
| U2 | The seasonal palette follows the **season schedule**, not a fixed day count. | ✅ | 19 §16 |
| U3 | The drop reveals **~10% of the country** (tunable) and its *character* per route — promising, workable, thin, poor — not what is over the ridge. | ✅ | 19 §16 |
| U4 | Rivals never use the Attempt Meter. **Check-ins report how someone went out.** | ✅ | 19 §16 |
| U5 | Larder and Nutrition are **one screen**. Splitting them breaks the causal link B1 depends on. | ✅ | 19 §16 |
| — | Visual direction locked: broadcast lower-third, no rounded corners, no bordered cards, Archivo, seasonal palette. | ✅ | 07 |

## 10. Still open

| # | Question | Owner |
|---|---|---|
| **A1** | **Legal review of IP exposure.** Highest-consequence open item. Blocks store copy, not code. | EP |
| B1 | Is the protein ceiling legible enough to be fair? The design's highest-risk item. Answerable only by playtest. | Design |
| — | Dana at 33.2% — is the stalk a shade too strong, or is that roster variance? | Playtest |
| — | Should a pure builder be viable? Bushcraft has no path to food at all. | Design |
| A7 | M0–M5 dates and headcount. | EP |
| A9 | Named narrative/writing owner. | EP |
| A14 | iOS added to the determinism matrix before M2. | QA |
| U-open | Doc 19 §16 is fully resolved; new UI questions will arise at first build. | Design |

---

*Maintained alongside doc 18. A decision recorded here should not be re-opened without new evidence.*
