# OFF THE GRID — Crisis System & Attribute Identity v0.1

*Specification for options B and C from the build-diversity audit (doc 15). Addresses the measured finding that Resolve is worth 2.4 days per point while four of the other five attributes sit under 0.8.*

> **The core move:** every crisis becomes a **consequence of something the player did**, signalled in advance, arriving at a moment when addressing it costs something they want. A crisis the player caused and ignored is a lesson. A crisis that arrives from nowhere is the game cheating — the same failure mode the protein ceiling has, and the same fix.

---

## 1. Why Resolve wins, and what actually changes it

Resolve sits **inside** the tap-out roll:

```
chance = 0.062 × worn × fragility(Resolve) × phase × bodyPressure × crisis
```

Every other attribute reaches that roll through three steps of dilution — attribute → food or warmth → morale → `worn`. Each step absorbs effect. Worse, **crisis frequency is lopsided**: memory events are ~75% of crises early and ~45% later, and memory events are precisely what Resolve defends.

So Resolve is both the only attribute with a direct line *and* the defender of the most common threat. Rescaling it does not fix this — measured, moving the divisor from 12 to 16 shifted it only 3.86 → 3.24.

**The fix is to give every attribute a crisis to defend against, and to spread crisis frequency across them.**

---

## 2. The three-stage model

Every crisis runs the same shape. This is what separates it from a dice roll.

```
CAUSE      something the player did, or chose not to do
  ↓
SIGNAL     a visible warning, with lead time, that costs something to act on
  ↓
CRISIS     fires only if the signal was ignored, or bad luck compounds it
```

**Consent rule compliance (§10.1) lives in the SIGNAL stage.** If a crisis can fire without a preceding signal, it is unfair by construction and should not ship in that form.

### 2.1 The pressure moment — and why it should be emergent

The design goal is that a warning arrives when acting on it is expensive. The naive implementation is to time warnings against opportunity artificially. **Do not do that** — it is authored tension, and players smell it.

Instead, exploit the fact that **the cause and the opportunity usually share a root**:

- **Bow wear accumulates from shooting.** So the "your bowstring is fraying" warning naturally arrives after a run of hunting — which is exactly when you have found game and want to keep hunting.
- **Cache raid risk scales with cache size.** So "something has been at the edge of camp" arrives when you have had a *good* season and have the most to lose.
- **Injury risk scales with working hard while depleted.** So it arrives when you are already behind and can least afford to rest.

**The crisis intensity rises with your success or your desperation, automatically.** No authoring required, and every warning lands on a genuine dilemma because the player's own situation created it.

---

## 3. Crisis taxonomy

Six crises, one per attribute. Each row is a complete chain.

### 3.1 Gear failure — **Bushcraft**

| | |
|---|---|
| **Cause** | Using gear accrues wear. Bow shots, net sets, axe swings, cordage strain. Balance doc §7 already prices this (bow 120 shots self-made / 400 gear; rebuild = 4 slots). |
| **Signal** | At ~70% wear: *"the bowstring is starting to fray."* Visible in the gear readout continuously, not just as a toast. |
| **Decision** | Spend a slot repairing — and repair needs cordage, which competes with shelter — or keep using it and accept the risk. |
| **Crisis** | The item breaks. The bow is unusable until rebuilt (4 slots); a torn net is worse, because fishing is the calorie backbone. |
| **Bushcraft's role** | Slows wear, halves repair cost, and at high skill allows field repair without spare materials. |
| **Emergent pressure** | Wear comes from *use*, so the warning arrives mid-hunt, when game is around. |

### 3.2 Cache raid — **Hunting**

| | |
|---|---|
| **Cause** | A large cache, poorly sited or unprotected. Balance doc §4 already states "bear risk on caches should scale with cache size — the bigger your surplus, the more attractive your camp." This is specced and unimplemented. |
| **Signal** | Tracks and sign at the edge of camp, escalating over 2–3 days. Read earlier and more clearly with high Hunting. |
| **Decision** | Spend slots hanging the cache, moving it, or building a deterrent — against continuing to fish the run. |
| **Crisis** | A bear takes 30–70% of the cache. Small chance of injury on top. |
| **Hunting's role** | Reads sign earlier (longer lead time), and deterrence is more effective. |
| **Emergent pressure** | Risk scales with surplus, so it peaks exactly when you are winning. |

### 3.3 Storm and exposure — **Cold Adaptation**

| | |
|---|---|
| **Cause** | Shelter tier below what the season demands; wet gear left undried; no fuel reserve. |
| **Signal** | Weather turns a day ahead. *"The wind is backing round to the south-east."* |
| **Decision** | Spend slots reinforcing shelter, drying gear and cutting extra wood — or take the productive slots and gamble. |
| **Crisis** | Soaked at sleep, shelter damage, a large morale hit and a large thermoregulation bill. In severe cases the shelter is destroyed, which is a **relocation trigger** (doc 12). |
| **Cold Adaptation's role** | Reduces clo demand, reduces severity, and preserves sleep quality in a poor shelter — the specced role it has never had. |
| **Emergent pressure** | Storms escalate with the season, arriving as daylight shrinks and every slot is already contested. |

### 3.4 Injury — **Fitness**

| | |
|---|---|
| **Cause** | Heavy activity (hauling, chopping, stalking) while body condition is low. |
| **Signal** | *"You are getting clumsy with the axe."* Fires when working hard below a condition threshold. |
| **Decision** | Rest or switch to light work — losing the slot — or push on. |
| **Crisis** | Injury costs slots for several days; severe injury can end the run. |
| **Fitness's role** | Lower risk at the same workload, and faster recovery. |
| **Emergent pressure** | Risk rises as you wear down, so it lands when you are already behind and can least afford to stop. |

### 3.5 Bad forage — **Foraging**

| | |
|---|---|
| **Cause** | Eating something you have not confidently identified. Only tempting when you are hungry — which is the point. |
| **Signal** | Unknown plants are shown *as* unknown, with confidence rising as Foraging identifies more. The uncertainty is the warning. |
| **Decision** | **The best decision in the system.** Starving, with an unidentified mushroom in hand: eat it or don't. |
| **Crisis** | Illness costing 2–4 days of reduced slots, plus a morale hit and lost stomach contents. |
| **Foraging's role** | Identifies more plants confidently, shrinking the gamble. Design spec §4.1 already lists "poison avoidance" as its job. |
| **Emergent pressure** | Temptation scales with desperation. This needs no timing at all. |

### 3.6 Loneliness and memory — **Resolve**

| | |
|---|---|
| **Cause** | Time, isolation, low morale, and stretches without completed projects. |
| **Signal** | Journal tone shifts; the morale band desaturates (already specced, §5.6.1). |
| **Decision** | Spend a slot on a comfort project, or keep grinding food. |
| **Crisis** | Memory event, with the tap-out roll. |
| **Resolve's role** | Unchanged. This stays its domain. |

---

## 4. Rebalancing crisis frequency

Giving each attribute a crisis achieves nothing if memory events remain 75% of them. Proposed distribution, and it should shift across the run:

| Crisis | Settling-in (1–10) | Grind (11–45) | Late (46+) |
|---|---|---|---|
| Memory / loneliness | 55% | 30% | 30% |
| Storm / exposure | 15% | 20% | 30% |
| Gear failure | 10% | 20% | 15% |
| Injury | 10% | 12% | 15% |
| Cache raid | 5% | 12% | 5% |
| Bad forage | 5% | 6% | 5% |

Resolve stays the single most-defended threat — which is faithful, and it should remain the strongest attribute. The target is **narrowing the gap from ~8× to roughly 2–3×**, not flattening it.

Cache raids peak mid-run because that is when caches are largest; storms peak late; memory events dominate early. Each attribute has a season where it is the one keeping you alive.

---

## 5. Option C — morale income per attribute

Bushcraft ranks second *only* because comfort projects give it repeatable morale income. Extend the pattern so every attribute has a way to generate morale, not merely resist its loss.

| Attribute | Morale income | Status |
|---|---|---|
| Bushcraft | Comfort project completed, +14 scaled by skill | exists |
| Hunting | A good kill, scaled by size and desperation | exists |
| Foraging | A beachcomb or berry-patch find | exists |
| **Fitness** | **Cresting a ridge / reaching new ground** — a prospecting payoff, +6 first time per area | **new** |
| **Cold Adaptation** | **A warm night in a shelter that beat the weather**, +3 when clo comfortably exceeds demand on a cold night | **new** |
| Resolve | Multiplies the payout of every other source | exists |

The two new ones are deliberately small and frequent rather than large and rare. They are there to make the attribute *felt* every few days, which is as much a legibility fix as a balance one.

---

## 6. What this costs

**Sim work is modest.** The acute-event system, morale attribution, the trace, and relocation triggers all exist. This is mostly new event types plus a gear-wear counter.

**UI work is the real cost, and it is not optional.** Every crisis needs its signal visible *before* it fires:

- A continuous gear-condition readout, not a one-off toast
- A one-day weather look-ahead
- Camp sign that escalates over days
- Forage confidence shown per plant
- A body-condition cue that reads as "clumsy," not as a number

Without the signals this system is strictly worse than what exists now, because it adds RNG deaths to a game that already has legibility risk. **Ship the signals or do not ship the crises.**

---

## 7. Open questions for the designer

| # | Question |
|---|---|
| C1 | Should crises be survivable-but-costly by default, with death only on compounding bad luck, or lethal on a single bad roll? The show has both and they feel very different. |
| C2 | Should gear wear be visible as a number, or only as descriptive text? A number is legible; text preserves the field-craft feel. |
| C3 | Should a bad-forage gamble be *possible to win*? A poisoning that is always punished teaches "never gamble," which removes the decision. |
| C4 | Do crises stack? Two at once is dramatic and is how real runs unravel, but it multiplies the unfairness risk. |
| C5 | Should high skill remove a crisis type entirely, or only blunt it? Removal makes builds feel powerful; blunting keeps tension everywhere. |

---

*Status: v0.1 specification, not yet implemented. Sequenced after doc 15's items 2 and 3, since crisis tuning against a 44-day mean run would be fitting curves to a game most players never finish.*
