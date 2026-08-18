# OFF THE GRID — Camp Relocation System v0.1

*Companion to `01-design-spec.md` §7.4 (which decided relocation ships) and `04-balance-economy.md` (whose constants this spec must not break). This document supplies the numbers §7.4 deliberately left open: trigger thresholds, cost constants, morale event sizing, and the resolution of B6.*

> **Headline finding, up front:** relocation is **morale-positive across its full cycle** and does not threaten the tuned constants in `04` §7.4. The reason is structural — relocation is intensely *active*, so it never engages the idleness penalty that carries the whole balance model, and it terminates in a shelter rebuild, which is a project completion worth **+14**. The danger is not the morale arithmetic. It is a **2–4 day trough** between losing the old shelter and completing the new one. Design the trough, not the total. Details in §5.

---

## 1. What relocation is for

§7.4 identifies the problem precisely: every existing late-game pressure source *squeezes* rather than *diversifies*. Shrinking daylight, depleting tiles, falling temperature and escalating events all make the same decision harder. None of them make it different.

Relocation is the one late-game choice that is not a smaller version of an early-game choice. Its job is to convert a tightening arithmetic problem back into a **strategic decision with unknown upside**.

That framing constrains everything below. If relocation becomes obligatory, it is just another squeeze. If it becomes free, it is not a decision. It must sit in the narrow band where it is **available, expensive, and sometimes correct**.

---

## 2. Triggers

Two triggers, per §7.4. Both must satisfy the consent rule (§10.1): the player sees the reason **before** committing.

### 2.1 Trigger A — local food exhaustion

Each tile carries a `YieldFraction` in `[0,1]`, starting at 1.0, decremented by extraction and recovering slowly (§8.2). Camp **catchment** is every tile within one slot of travel — radius 2 at standard movement.

| Parameter | Value | Reasoning |
|---|---|---|
| Catchment radius | 2 tiles | One slot of travel each way leaves time to hunt |
| Trigger threshold | mean catchment `YieldFraction` **< 0.35** | See below |
| Confirmation window | 3 consecutive days | Prevents a single bad weather day firing it |
| **Visible degradation begins** | **0.60** | Consent rule — signal precedes trigger by many days |

**Why 0.35.** Below roughly a third of original yield, expected calories returned per hunting slot falls under the calories a weakened late-game body spends acquiring them. That is the real inflection point — the moment the catchment becomes net-negative — not an arbitrary tuning number. It moves with player condition, which is correct: a strong player can work a thinner catchment than a starving one.

> `[R1]` The threshold should scale with player mass. A 65 kg day-50 player has a lower burn and can profitably work a thinner catchment than an 85 kg day-10 player. Provisionally: `threshold = 0.35 × (burn_today / 2850)`. Validate in the solver.

**Player-visible signal.** Tile yield indicators degrade continuously from 0.60 down. Tracks and sign become sparse in the map read. By the time the trigger fires the player has watched the area thin out for a week or more. This is the strongest consent-rule surface in the game and should be treated as such.

### 2.2 Trigger B — shelter inadequate for the season

From `04` §5.2, `clo_gap = clo_demanded(night_temp) − clo_available`.

| Parameter | Value | Reasoning |
|---|---|---|
| Trigger threshold | `clo_gap` **> 0.8** | See below |
| Confirmation window | 3 consecutive nights | Weather noise |
| **Hard additional condition** | **Site cannot support the next shelter tier** | Critical — see below |
| Visible signal | clo available vs. demanded, shown as a widening gap in the status readout | Already specced in §7.4 |

**Why 0.8.** From the `04` §5.2 demand table, 0.8 clo is approximately the span between holding 0 °C and failing at −5 °C. At that gap the player is losing sleep quality and burning extra calories shivering, but is not in immediate danger. It is a warning with runway, which is what a relocation trigger must be.

**The site-quality condition is load-bearing.** Relocating for shelter reasons is only rational if the shelter *cannot be fixed in place*. Otherwise the correct answer is "build the next tier here," and a trigger that fires anyway is teaching the player to distrust the game. So Trigger B requires the current site to fail at least one of:

- **Timber density** insufficient for the next tier's log count (log shelter needs 110; cabin needs 210)
- **Exposure** — wind-exposed aspect that caps effective clo regardless of build
- **Water access** lost (seasonal creek dried, or freeze)

If the site *can* support an upgrade, Trigger B does not fire and the status readout should say so plainly: *"This site can hold you. You need a better shelter, not a new valley."*

---

## 3. Costs

Per the session decision, relocation cost ships as a **tunable constant with two pre-built configurations**, A/B tested at playtest. Both live in `OffTheGrid.Data`; switching is a config flip, not a code change.

### 3.1 Fixed costs (both variants)

| Cost | Value | Notes |
|---|---|---|
| Travel | 2–4 slots | Scales with distance and carried mass via the existing superlinear movement cost |
| Fog | Returns for the new area | Unless previously scouted this run, or carried as cross-run intel (§12.2) |
| Carry capacity | **~25% bodyweight, reduced by condition** | At day 40 (73 kg) ≈ 18 kg |
| Cache | **One cache pit's worth (20 kg), not two** | Falls out of carry capacity — see below |

**The carry cap is the sharpest decision in the system.** A cache pit holds 20 kg (`04` §4). Carry capacity at the point relocation typically fires is ~18–20 kg. So the player can move **one cache and no more**. Everything else is abandoned to the bears. That is a real, legible, agonising choice, and it costs nothing to implement because it falls out of constants that already exist.

### 3.2 Variant A — total loss (as specced in §7.4)

| Element | Outcome |
|---|---|
| Shelter structure | **Destroyed. No partial carry.** |
| Tools and gear | Carried |
| Cordage | **Lost with the structure** |
| Rebuild cost | Full tier cost from scratch |

This is §7.4 as written. The sacrifice is what makes relocation a pivot rather than a repositioning.

### 3.3 Variant B — softened

| Element | Outcome |
|---|---|
| Shelter structure | Destroyed |
| Tools and gear | Carried |
| **Cordage** | **Recovered, up to carry capacity** |
| Rebuild cost | Full slots and logs, **cordage waived** |

Cordage is the right thing to soften. `04` §5 flags it as "the quiet dependency" — it appears in shelter, traps, net repair and bowstrings, and the 1-slot-per-10-m natural-cordage cost is a genuine tax. Recovering 12–35 m of cordage saves 1–3.5 slots on rebuild without touching the structural sacrifice that makes relocation meaningful.

**Why this is the right A/B axis.** It varies the cost by roughly 15–20% of the total relocation burden while leaving the *shape* of the decision identical. Both variants still destroy the shelter; both still force the one-cache choice. If Variant A reads as confiscation at playtest, Variant B tells you whether the problem is magnitude or principle. If both read as confiscation, the problem is the trigger frequency, and §7.4's own guidance applies: **reduce shelter loss before reducing trigger frequency.**

---

## 4. Morale event sizing

The shelter-loss morale hit scales with **slots invested in the lost structure**, so that abandoning a debris hut is cheap and abandoning a log shelter is agony.

```
shelter_loss_morale = −min(1.0 × slots_invested, LOSS_CAP)
LOSS_CAP = 10.0
```

| Shelter | Slots | Morale hit |
|---|---|---|
| Tarp lean-to | 1 | −1 |
| Debris hut | 3 | −3 |
| A-frame + bough bed | 5 | −5 |
| Reflector-wall camp | 8 | −8 |
| Log shelter (small) | 16 | **−10** (capped) |
| Log cabin + stove | 28 | **−10** (capped) |

### 4.1 Why the cap is 10, and why that number is not arbitrary

**The shelter-loss hit must never exceed the rebuild reward.** Project completion is **+14** (`04` §7.4). If loss exceeded gain, relocation would be a guaranteed morale death spiral and no rational player would ever choose it — the mechanic would ship and never fire. Capping at 10 against a +14 rebuild makes the full cycle **net +4**.

That is the correct sign. §7.4 asks whether relocation reads as "a meaningful strategic pivot" or "the game confiscating the player's work." A mechanic that is net morale-positive across its cycle, while being expensive in slots and calories, is structurally a **pivot**. One that is net-negative on every axis is structurally a **punishment**. The cap is what puts it on the right side of that line.

### 4.2 The degenerate-strategy check

Net +4 morale per relocation raises an obvious question: can the player farm it?

**No, for two independent reasons:**

1. **Relocation requires an active trigger.** It is not available on demand. This is the primary governor and it is already in the design.
2. **The slot cost dominates.** Travel (2–4) + rebuild (5–16) + re-scouting the new catchment (2–3) is **10–23 slots**, i.e. 3–7 days at late-game daylight. Nobody farms +4 morale for four days of work.

> `[R2]` If playtest shows repeat relocation anyway, apply a decay to the rebuild bonus for repeated shelters of the same tier (+14 / +10 / +7). Do **not** implement this pre-emptively — it is complexity against a problem that probably does not exist.

---

## 5. B6 resolved — do the tuned morale constants survive relocation?

**Yes. And the reason is structural rather than numerical.**

The `04` §7.3 finding is that the **idleness penalty** carries the entire balance model — it is what defeats the fasting build, and §7.4 warns it "cannot be softened for legibility reasons without re-breaking Q2."

Relocation never touches it. Travel is activity. Rebuilding is activity. Re-scouting is activity. Across the entire relocation cycle the idleness counter stays at zero, and the cycle **terminates in a project completion**. The load-bearing negative lever is not engaged, and the largest positive lever is.

### 5.1 The trough — where relocation actually hurts

The risk is not the total. It is the **2–4 day window** between losing the shelter and completing the rebuild, during which the player carries:

| Pressure | Per day |
|---|---|
| Base decay | −1.0 |
| Shelter loss (one-off, day 0) | −1 to −10 |
| Food insecure (likely — cache halved, catchment unknown) | −2.0 |
| Weight loss continuing | −0.5 per 5% |
| **No project completion until rebuild lands** | — |

Worked case, A-frame relocation at day 40, 3-day rebuild:

```
Day 0   shelter loss                    −5.0
Day 0-3 base decay        3 × −1.0      −3.0
Day 0-3 food insecure     3 × −2.0      −6.0
                                      ───────
        trough total                   −14.0
Day 3   rebuild completes               +14.0
                                      ───────
        net across cycle                 0.0
```

An A-frame relocation is **morale-neutral** across its cycle and costs a −14 trough. A log-shelter relocation is capped at −10 loss, so it runs a −19 trough against +14, netting −5.

**This is the number that matters.** A player entering relocation with morale under ~20 will not survive the trough even though the cycle is neutral-to-positive. That is a legitimate and interesting failure mode — relocation is a manoeuvre you must be *healthy enough to attempt* — but it must be legible.

### 5.2 Required legibility work

The relocation confirmation screen must show the projected trough, not just the costs. Something like:

> **Moving will cost you.** Roughly three days before the new shelter stands.
> Your resolve is **34**. You should come out the other side around **20**.
> *Below 15 people stop making good decisions.*

This is the same class of work as the protein-ceiling framing (B1) and the morale attribution HUD (A22). It is not optional. An invisible trough is an invisible punishment, which is the exact failure mode `01` §5.6.1 exists to prevent.

> `[R3]` The projection must be honest about its own uncertainty. It cannot know whether the new catchment feeds the player. Show the trough as a range, and never show a recovery estimate the sim cannot stand behind.

---

## 6. Interaction with cross-run persistence

§7.4 identifies this as the strongest expression of "knowledge, not power" in the design, and it is worth stating the mechanism explicitly.

A first-time player relocating is making a **blind bet**: they know the current site is failing, they do not know the destination is better. A returning player who has run this biome before carries map intel (§12.2) and knows there is a sheltered draw two valleys north with standing timber.

Same mechanic. Same cost. Completely different decision quality. The knowledge did not make them stronger — it made them **informed**, which is the entire persistence thesis working in a single system.

**Design implication:** relocation destinations should be *memorable*. A generic "better tile" is not worth remembering across runs. Distinctive sites with legible trade-offs — the sheltered draw with poor fishing, the salmon creek with no timber — turn map intel into something a player actually carries in their head rather than in a save file.

---

## 7. Implementation notes

Sim-side, in `OffTheGrid.Sim`:

```
RelocationTrigger      evaluates A and B each day, exposes reason + confidence
RelocationCost         computed from variant config + carried mass + distance
RelocationCommand      player-issued, requires an active trigger
```

Constants belong in `OffTheGrid.Data`, never inline:

| Constant | Value |
|---|---|
| `CatchmentRadius` | 2 |
| `FoodTriggerThreshold` | 0.35 |
| `FoodVisibleDegradation` | 0.60 |
| `TriggerConfirmDays` | 3 |
| `CloGapThreshold` | 0.8 |
| `ShelterLossMoralePerSlot` | 1.0 |
| `ShelterLossMoraleCap` | 10.0 |
| `CarryFractionOfBodyweight` | 0.25 |
| `RelocationVariant` | `TotalLoss` \| `CordageRecovered` |

Balance assertions to add alongside the existing suite:

```csharp
BalanceAssert.RelocationCycleMoraleNonNegative(ShelterTier.AFrame);
BalanceAssert.RelocationTroughSurvivable(startingMorale: 35);
BalanceAssert.RelocationNotFarmable(runs: 1000);
```

The third one is the important one. It should fail if any solver run relocates more than twice without a trigger-forced reason.

---

## 8. Open questions

| # | Question | Notes |
|---|---|---|
| R1 | Should the food trigger scale with player burn? | Provisional formula in §2.1. Solver decides. |
| R2 | Does rebuild-bonus decay need to exist? | Only if playtest shows repeat relocation. Do not pre-build. |
| R3 | How honest can the trough projection be? | It cannot know the new catchment. Range, not point estimate. |
| R4 | Can relocation be *forced* by an event? | Camp destruction (fire, flood, bear) is in the event catalogue. Forced relocation skips the consent-rule signal — does it need an exemption, or should forced events never destroy shelter outright? **A17 scope.** |
| R5 | Do rivals relocate? | If rivals run the full sim (B8: yes), they should. Cheap. But it changes tap-out distribution and needs a solver pass. |

---

*Status: v0.1. Numbers are first-pass and solver-unvalidated. §5 (B6 resolution) is the load-bearing argument in this document and should be attacked first if anything here is wrong.*
