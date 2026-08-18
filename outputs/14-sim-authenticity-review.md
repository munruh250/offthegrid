# OFF THE GRID — Sim Authenticity Review v0.1

*A design review of the M0 simulation against the show it is modelled on. Written from measurements of the current build, not from reading the code. Every number below came from a sweep.*

> **Headline:** the sim currently models a **competent forager in an abundant landscape**. The show is about **hungry people in a landscape that mostly gives them nothing**. The economy is roughly right in aggregate — competent play lands on day 59, matching the balance doc — but it arrives there by a completely different route than the show does. The mean is correct and the *shape* is wrong.

---

## 1. The single biggest divergence: the drop-out curve

On the show, tap-outs are heavily **front-loaded**. A meaningful fraction of any cast leaves within the first week, some within the first 72 hours. The reasons are almost never physiological — it is fear, a catastrophic first night, homesickness that arrives faster than expected, an injury, or simply discovering that the reality is nothing like the plan. The back half of a season is a long thin tail of two or three people going very deep.

The sim cannot produce this. Measured spread of outcomes:

| Strategy | Mean day | p10 | p90 | Full spread |
|---|---|---|---|---|
| Work hard all run | 59 | 56 | 62 | **12 days** |
| Bank the run, then coast | 49 | 48 | 50 | 3 days |
| Pure conserve | 24 | 24 | 24 | **0 days** |

**The sim is a metronome.** Nobody leaves in week one. Nobody goes to day 90. The show spans roughly day 1 to day 100; this spans day 56 to day 62.

The cause is structural: morale starts at 70 + 3×Resolve and decays at most about −8/day, so the earliest mathematically possible exit is around day 12. **There is no acute shock in the model at all.** Memory events are implemented (`ApplyMemoryEvent`) and never fired. Weather events do not exist. Injury does not exist. Wildlife pressure does not exist.

> **Recommendation R-A1.** Add an acute-event layer before adding anything else. The show's characteristic exits are *events*, not attrition. Without them the run is a smooth slide and every contestant slides at the same rate.

> **Recommendation R-A2.** Early-run morale should be more volatile, not less. Consider a "settling-in" period over roughly the first 7–10 days where memory events fire at elevated frequency and Resolve matters most. That reproduces the front-loaded curve honestly, from the same mechanism the show runs on, rather than by an authored early-death rule.

---

## 2. Food acquisition is far too generous

Measured per-slot success rates at skill 5, 4,000 trials each:

| Activity | Season | Success | Mean kcal/slot |
|---|---|---|---|
| Fishing | Salmon run | **86.5%** | **3,898** |
| Fishing | Tapering | 63.6% | 866 |
| Fishing | Lean | 42.0% | 230 |
| Fishing | Winter | 16.3% | 80 |
| Hunting (stalk) | Salmon run | **38.8%** | **4,579** |
| Hunting (stalk) | Tapering | 39.8% | 4,968 |
| Hunting (stalk) | Lean | 31.5% | 2,152 |
| Hunting (stalk) | Winter | 22.4% | 1,366 |
| Foraging | Salmon run | 60.7% | 121 |
| Foraging | Lean | 22.4% | 47 |

### 2.1 Stalk hunting at ~39% per slot is not survival

This is the worst number in the build. A ~39% chance of taking an animal per hunting slot, sustained, means big game is a routine event. The measured consequence: **4.1 big-game kills per run.**

On the show, most contestants never kill a big animal at all. A deer is a season-defining moment that reshapes someone's entire run, and it happens to a small minority of any cast. Four per person per season is not a survival contest, it is a butcher's round.

**Foraging is the one line that reads correctly** — high reliability, near-worthless calories. That is exactly the coastal intertidal experience: you will always find limpets and mussels, and they will never save you.

### 2.2 The missing mechanic: encounter is not conversion

This is the structural fix, and it matters more than any rate tweak.

The sim rolls **one** probability that collapses "did you find an animal" and "did you kill it" into a single event. On the show these are utterly different. Contestants see game constantly and connect rarely. The most common hunting beat in the entire series is *seeing something and not getting it* — the missed shot, the spooked deer, the wounded animal lost in the brush.

Splitting the roll fixes several things at once:

- **It restores the show's dominant hunting experience.** Encounters become common; kills stay rare.
- **It gives the M1 archery minigame a job.** The minigame *is* the conversion roll. Right now the auto-resolve path has no seam for it to slot into.
- **It gives arrow scarcity teeth (B5).** Arrows are currently near-costless because you effectively never miss. A miss that costs an arrow and a slot is what makes the scarcity mean something.
- **It separates two tuning levers that are currently welded together** — how rich the land is, versus how good the player is.

> **Recommendation R-A3.** Split `Harvest.Resolve` into an encounter roll and a conversion roll. Suggested starting shape for big game with a bow: **15–25% encounter per slot, 8–15% conversion given encounter** — a net 1.5–3% per slot, producing roughly one to three opportunities and zero to one kills across a run. Attribute should mostly move *conversion*, not encounter; skill makes you a better shot, it does not put more deer in the valley.

### 2.3 Trapping produces literally nothing

`TrapLine` has a MET cost, is mapped to the Hunting attribute, and **has no entry in any encounter table**. Measured success rate: **0.0% in every season.** A player who runs a trap line burns calories for a guaranteed return of zero.

This is not a tuning problem, it is a hole. And it is a conspicuous one, because **trapping is the canonical small-game method on the show.** Stalking rabbits with a bow is not how anyone eats; a snare line set once and checked daily is. It is also the mechanically interesting version — an up-front investment that pays a passive trickle, which is a completely different decision shape from spending a slot to hunt.

> **Recommendation R-A4.** Implement the trap line as set-up cost plus passive yield: several slots to establish, then ~15–25% per check for a hare or grouse. This should be the reliable small-game floor that keeps a competent player alive between wins.

### 2.4 Roosevelt elk cannot be encountered

The elk exists in `FoodTable` at 177,390 kcal and appears in **no** encounter table. It cannot be caught.

Balance question **B7** asks whether a lucky day-3 elk trivialises month one. That question is currently unanswerable, because the elk is not in the game.

---

## 3. Is there a real choice between working and conserving?

Asked directly, and measured. The answer is **no — working hard strictly dominates.**

| Strategy | Mean day | Big kills | Total catches |
|---|---|---|---|
| Work hard all run | **59** | 4.1 | 59 |
| Bank the run, then light activity | 53 | 1.4 | 45 |
| Bank the run, then conserve | 49 | 1.4 | 27 |
| Forage only | 34 | 0.0 | 31 |
| Trap line only | 33 | 0.0 | 0 |
| Pure conserve | 24 | 0.0 | 0 |

The design intent (balance doc §7.2–7.3) was that the fasting build must lose. **It does, and that part is working.** But it has been defeated so completely that there is now no viable conservation play at any point in the run — and on the show, deliberate energy conservation is a real and correct tactic. Contestants who make it deep routinely shift from active foraging to sitting still and burning as little as possible. That is a legitimate late-game strategy and the sim has no room for it.

Two mechanisms are jointly responsible:

1. **The larder cap is too small and spoilage too aggressive** for banking to fund a meaningful coast. Doc §8 names preservation the top tuning lever, but a fixed 25 kg cap means preservation investment cannot actually buy a strategy — it just tops off the same small tank.
2. **The idleness penalty punishes low activity generally**, rather than punishing *purposelessness* specifically. Whittling clears it, which is right — but the food system then starves the conserving player anyway.

> **Recommendation R-A5.** Make cache capacity a *built thing that scales with investment*, not a constant. That is what turns preservation into the lever doc §8 claims it is, and it is what opens the bank-and-coast line as a real alternative to grinding slots. The target is not "conserving wins" — it is "conserving is competitive in the back half, after a strong first three weeks."

> **Recommendation R-A6.** The current optimum is a narrow band: three productive slots beats both two and four. A single dominant strategy with penalties on either side is a design smell. There should be at least two viable shapes of run — grind, and bank-then-coast — with different risk profiles.

---

## 4. Exit causes are inverted relative to the show

Measured dominant cause, work-hard strategy: **`medical.wasting` at 65%.**

On the show, the overwhelming majority of exits are **voluntary tap-outs**. Medical pulls happen and matter, but they are the minority case. The sim has this backwards: it is primarily a starvation simulator with morale as a secondary cause, where the source material is primarily a psychological attrition contest with starvation as the backdrop.

There is also a tension inside the design that should be made conscious: **spec §5.5 sets a >60% dominance ceiling** for any single end condition — but the show itself is far more dominated than that by voluntary exit. The design is asking for more variety than the source material actually has.

That is a legitimate choice. Games are not documentaries, and four meaningfully different ways to lose is better play than one. But it should be a decision someone made on purpose, not a target inherited without noticing it conflicts with authenticity.

> **Recommendation R-A7.** Decide explicitly whether the target is show-authentic (tap-out dominant, ~70–80%) or game-varied (§5.5's <60% ceiling). Then tune toward it. Right now it is neither — it is medical-pull dominant, which matches neither the show nor the spec.

---

## 5. The signature mechanic that is entirely absent: the ten items

The sim has **no gear model at all**.

The 10-item loadout is the show's most recognisable decision and the one every viewer argues about. It is also the primary determinant of what food is actually reachable: a gillnet in a salmon creek is a different game from a line and hook; an axe and saw open shelter tiers that are otherwise unreachable; a bow makes big game *possible* rather than theoretical.

At present every contestant fishes, hunts, forages, and builds at identical rates regardless of what they brought. The most consequential choice in the source material currently has no mechanical existence.

This also invalidates the archetype comparison in a subtle way. The presets differ by attributes and body composition, but a real Ex-Military and a real Bushcraft Instructor differ enormously in *what they packed*, and that is most of why their runs diverge.

> **Recommendation R-A8.** Gear gating on food access is higher value than any further rate tuning. Until it exists, the encounter tables are describing a landscape rather than a *player's access to* a landscape, and those are not the same thing.

---

## 6. What the sim gets right

Worth stating plainly, because the list above is long:

- **The calorie economy is sound.** Competent play reaches day 59 against the doc's 59, from independently derived rates.
- **The protein ceiling works and is the most authentic thing in the build.** "Full cache, still starving" is real, it is well-documented, and it is correctly load-bearing here.
- **The seasonal squeeze is genuinely emergent.** Daylight shortens, slots fall from 5 to 3, firewood demand rises. Nothing authored.
- **Fat as the currency is correct** and matches how contestants actually talk about food.
- **The fasting build loses** — which was the whole point of the morale system, and it holds.
- **The work-level optimum is emergent, not enforced.** Four productive slots being worse than three, because the protein ceiling caps absorption, is exactly the kind of result a good model produces without being told to.

---

## 7. Priority order

| # | Item | Why first |
|---|---|---|
| 1 | **R-A3** — split encounter from conversion | Fixes the worst number, restores the show's dominant hunting beat, and gives M1's minigame a seam to attach to |
| 2 | **R-A4** — implement the trap line | It is a hole, not a tuning issue, and it is the show's actual small-game method |
| 3 | **R-A1/A2** — acute events and early volatility | Without these the drop-out curve can never resemble the show |
| 4 | **R-A8** — gear gating | The signature mechanic, and it invalidates archetype comparison until it exists |
| 5 | **R-A5** — scalable cache | Opens the second viable strategy and makes preservation the lever the doc claims |
| 6 | **R-A7** — decide the exit-cause target | Cheap, but everything downstream tunes toward it |

Rate tuning should come **after** items 1, 2 and 4. Tuning encounter probabilities while encounter and conversion are welded together, trapping returns nothing, and gear does not exist is fitting a curve to a model that is still the wrong shape.

---

*Status: v0.1, measured against the M0 build. All figures reproducible from `Solver` sweeps and the encounter audit.*
