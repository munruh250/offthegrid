# OFF THE GRID — Build Diversity Audit v0.1

*Measures whether the sim supports many viable ways to play or exactly one. Every figure is from a 250-seed sweep against the current build.*

> **Headline: there is a meta, and it is narrow.** One attribute is worth 2.1× the next and 55× the worst. One gear item is worth 3× the next and five of ten items are worth literally nothing. Four of five archetypes want the identical daily plan. The Endurance Athlete is currently **worse than the fasting build the whole design exists to defeat.**

---

## 0. A correction, and the bug it uncovered

An earlier claim in this project's analysis — *"once you are protein-ceiling-limited, more food is worth almost nothing"* — was **wrong as stated**, and the objection to it was right.

The accurate version: a surplus has no value for **today's** energy, but full value for every day you cannot harvest. That is precisely what caching is for, and banking the salmon run to live on later is the central strategic move of the format.

The sim did not show this because of an implementation error, not a design property. Two faults:

1. **Shelf life was implemented as an exponential decay constant** rather than a safe window. A drying rack retained only 36% of a cache after 30 days and 13% after 60. Properly dried or smoked food keeps for months.
2. **The processing loss was never applied at all.** Balance doc §4's 15/25/5% figures existed in the table and were read nowhere.

Both are fixed. The cap on banking is now what doc §4 says it should be — **rack capacity, processing slots, and the loss taken on the way in** — not rot. This is the correct model and it restores caching as a real strategy.

Worth recording as a pattern: this is the **fifth** time a constant existed in the design docs and nothing read it (morale gains, comfort projects, marrow, Cold Adaptation, processing loss). A doc-to-code coverage check would be cheap and would have caught all five.

---

## 1. Attribute value, measured

Days survived per point, each attribute measured **on a plan that actually exercises it** — measuring Foraging on a plan with no foraging slot says nothing.

| Attribute | At 2 | At 8 | **Days per point** |
|---|---|---|---|
| **Resolve** | 21.6 | 44.8 | **3.86** |
| Bushcraft | 28.8 | 39.7 | 1.82 |
| Cold Adaptation* | 19.4 | 28.2 | 1.47 |
| Hunting | 28.9 | 30.6 | 0.29 |
| Fitness | 22.5 | 23.4 | 0.15 |
| Foraging | 27.0 | 27.5 | 0.07 |

\* measured with a loadout that cannot build good shelter — see §1.2.

### 1.1 Resolve is the meta

At 3.86 days per point it is **2.1× Bushcraft and 55× Foraging**. A player optimising will pour points into Resolve and treat the rest as flavour.

It is defensible that Resolve should be the strongest attribute — mental toughness genuinely is the dominant factor in the format. The problem is the *margin*. At this ratio the other five are not weaker choices, they are wrong ones.

The cause is structural: Resolve gates the tap-out decision **directly**, while every other attribute only works indirectly, by improving food or shelter, which improves morale, which then reduces tap-out pressure. Direct beats indirect by a wide margin.

> **Recommendation D1.** Give at least two other attributes a direct line to the exit condition rather than a laundered one. Bushcraft already half-qualifies through comfort projects (repeatable morale income) — that is exactly why it ranks second and is the model to copy.

### 1.2 Cold Adaptation is insurance against your own failure

It measured **0.00** on a standard loadout and 1.47 only when the kit could not build adequate shelter. That is because a well-sheltered player never carries a clo deficit, so the attribute has nothing to act on.

An attribute that only pays out when you have already failed at something else is a bad pick by construction — nobody drafts insurance against a failure they intend to avoid.

Note this was **worse before this session**: Cold Adaptation had *zero reads anywhere in the sim*. It now offsets clo demand, and a thermoregulation cost was added so that clo has a calorie consequence rather than only a morale flag. That also gave balance doc §6's entire firewood economy something to attach to — it previously had no mechanical effect at all.

> **Recommendation D2.** Give Cold Adaptation an always-on effect, not a conditional one. Sleep quality, or a flat reduction in overnight burn, would pay every night rather than only on nights you mismanaged.

### 1.3 Hunting, Fitness and Foraging are rounding errors

All three sit under 0.3 days per point. The reason is the same in each case: **fishing dominates every other food route**, so the attributes governing the other routes have nothing to be good at.

- **Hunting** moves big-game conversion from 7.6% to 12.4% across its range. On a 12% encounter rate that is 0.9% versus 1.5% per slot — a real relative gain that is an irrelevant absolute one.
- **Foraging** was worth 0.07 even after adding berries and lowering its base conversion to leave skill some headroom.
- **Fitness** now drives prospecting, which does pay (+10 days at Fitness 9 versus never prospecting) — but only on a plan built around it, and that plan is worse overall than simply fishing.

---

## 2. Gear value, measured

Days lost when each item is dropped from a full kit, on a fish-and-trap plan.

| Item removed | Days | Cost |
|---|---|---|
| **Fishing line and hooks** | 28.2 | **−11.1** |
| Axe | 35.5 | −3.8 |
| Snare wire | 38.3 | −1.1 |
| Sleeping bag | 38.7 | −0.7 |
| Saw | 38.9 | −0.4 |
| Knife, Bow, Pot, Tarp, Paracord | 39.4 | **0.0** |
| *Line swapped for gillnet* | 44.0 | **+4.6** |

**Five of ten items are worth exactly nothing**, and tackle is worth 3× the next item. The optimal loadout is "tackle, ideally a gillnet, plus an axe, then whatever."

One methodological caveat, stated because it matters: this was measured on a **fish-and-trap plan**, so the bow and pot were never used. Their true value is not zero — it is *conditional on a plan nobody has a reason to run*. That is the same finding by a different route.

> **Recommendation D3.** The ten items should not all be food-access tools competing on one axis. Items that change *what you can survive* (shelter, warmth, fire reliability, injury recovery) currently barely register, because those systems have thin mechanical consequences. Deepening them is what makes a varied loadout viable.

---

## 3. Archetypes at their best plan

Each archetype run against five candidate plans; the best result shown.

| Archetype | Best plan | Days |
|---|---|---|
| Commercial Fisherman | build / fish / trap / whittle | **47.5** |
| Ex-Military | build / fish / trap / whittle | 42.0 |
| Bushcraft Instructor | build / fish / trap / whittle | 40.6 |
| **Fasting build (idle)** | *(control — should lose)* | **28.4** |
| Endurance Athlete | explore / fish / trap / whittle | **27.8** |

Two serious problems:

**The Endurance Athlete loses to the fasting build.** The degenerate strategy the entire morale system exists to defeat currently outperforms a legitimate archetype. Q2 technically still passes, because it compares against *competent* play — but an archetype the game offers as a valid choice being worse than the anti-pattern is a clear failure.

**Four of five archetypes want the identical plan.** Only the Athlete differs, and its distinct plan is the worst-performing one. The archetypes differ on paper and converge in practice.

---

## 4. Root cause

Three findings above reduce to one:

**Fishing is the only competitive calorie route, so everything that is not fishing is a rounding error.** That single fact produces the attribute imbalance (Hunting/Foraging/Fitness govern routes nobody runs), the gear imbalance (tackle is mandatory, five items are inert), and the archetype convergence (every viable plan contains a fishing slot).

Fixing the meta means giving each route a genuine niche, not nudging numbers:

| Route | Intended niche | Current state |
|---|---|---|
| **Fishing** | Enormous early during the run, collapses after | Strong early *and* competitive late |
| **Trapping** | Low, steady, reliable; the floor that prevents starvation | Roughly correct — the one healthy route |
| **Hunting** | Rare, huge, and only worth it if you can preserve the kill | Too rare to matter; preservation now fixed, so worth re-measuring |
| **Foraging** | Poor calories but **carbohydrate — the only food that bypasses the protein ceiling** | Under-tuned; the ceiling-bypass is the strongest unexploited idea in the design |

The foraging line deserves emphasis. Berries are currently the **only** food in the game that is carbohydrate-dominant, which means a forager can absorb more total daily energy than a hunter sitting on a lean cache. That is a real, mechanically distinct strategy that already exists in the model and is simply priced too low to use.

---

## 5. Priority

| # | Item | Why |
|---|---|---|
| 1 | Give each food route a season where it is clearly best | Single root cause of all three imbalances |
| 2 | Price the carbohydrate ceiling-bypass properly | Strongest existing idea; makes Foraging a real path |
| 3 | Fix the Endurance Athlete | An offered archetype losing to the anti-pattern is a bug, not a balance nudge |
| 4 | Give Cold Adaptation an always-on effect | Remove the insurance-only shape |
| 5 | Deepen warmth, fire and injury | Makes non-food gear worth carrying |
| 6 | Re-measure hunting now that preservation works | The route may already be better than it reads |

Resolve's dominance was narrowed this pass (divisor 12 → 16, base rate compensated) but at 3.86 days per point it is **still the meta** and item 1 is the real fix — other attributes need routes worth being good at before their numbers mean anything.

---

*Status: v0.1, measured against the M0 build at 137 passing tests. Reproducible from the sweeps in `Solver` and the marginal-value probes.*
