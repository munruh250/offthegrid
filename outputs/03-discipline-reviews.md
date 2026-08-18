# LAST OUT — Discipline Review Pass v0.1

*Review of `01-design-spec.md` and `02-technical-implementation.md`. Seven roles. Balanced: what's working, what concerns me, what I need.*

**Consolidated action list is §8. §9 records the resolution of all five disagreements. §10 covers additional rulings. §11 is what remains open.**

> **Status: all five cross-discipline disagreements (D1–D5) resolved in review session 2026-08-15.** Both other docs updated to match. Sections §1–§7 below are the original review as written; concerns superseded by a ruling are marked inline.

> **Update — balance pass complete (`04-balance-economy.md`).** Food, fuel, shelter and gear-attrition numbers now exist and were validated in the sim. **One finding overturns a spec assumption:** the fasting build is *not* defeated by physiology (it reaches day 90), it's defeated by morale's idleness penalty. Morale is therefore load-bearing for balance, not just theme — which raises the stakes on the D5 legibility work considerably. Morale constants retuned (they were ~2× too harsh). Three new technical risks: R13–R15.

> **Update — agent workflow pass (`05-agent-workflow.md`).** Audit found **zero** coverage of build verification, CI structure, logging conventions or agent scoping across all four prior documents; every prior instance of "skill" and "hook" was the game-design sense. The notable finding: the pure-C# sim boundary buys a **15–40× faster verification loop**, which makes the §2.1 architecture decision load-bearing for a second, previously unstated reason. New actions A33–A36 (three of them **pre-M0**) and risk R16.

### Rulings at a glance

| # | Question | Ruling |
|---|---|---|
| D1 | MVP minigame count | **Two: archery + fire.** Trap placement cut |
| D2 | Accessibility depth | **Auto-resolve, no audio variants.** Structural hygiene from M2 |
| D3 | Determinism | **Test in CI at M0**, decide fixed-point from data |
| D4 | Business model | **Free.** Biomes + cosmetics. No ads, no paid gear |
| D5 | Morale legibility | **Attribution at point of change**, plus a ≥70% measurable gate |
| — | Is morale the only death? | **No — four end conditions**, distribution shifts with skill |
| Q4 | Camp relocation | **IN**, consent-gated, costs the shelter |
| — | Telemetry depth | **Full run-history records**, not just counters |

---

## 1. Executive Producer

### Working

The scope discipline is genuinely unusual for a first spec. One biome, three minigames, six archetypes, placeholder art, and an explicit out-list. Most designs at this stage have twice the surface and no cut lines. The M1 gate is the right instinct — you've identified the one question that invalidates everything and put it first.

The competitive scan was done before the design was locked, not after. That's the right order and it changed the design (scenarios, journal, accessibility all came from it).

### Concerns

**C1 — Intellectual property. This is my biggest flag and it isn't a design issue.**

The design is explicitly modelled on a specific trademarked television property. The ten-item mechanic, the ten-contestant elimination format, the tap-out language, the medical check-in, the specific gear master list — cumulatively this is closer to the source than "inspired by." Game *formats* and *mechanics* generally aren't protected, but trademark and trade dress are, and "Alone" is an active mark on a currently-airing show.

Concretely: the working title `LAST OUT` is fine. Using the show's name, its visual identity, its item list verbatim, or marketing copy that positions the game as an adaptation is not. I'm not a lawyer and this needs a real one before any store listing copy is written — but budget for the conversation now, not at soft launch. The cheap outcome is "change some names." The expensive outcome is a takedown two weeks after launch.

**C2 — Monetisation is unresolved and it gates everything else.** ✅ *Resolved — D4: free, biomes + cosmetics.* **New EP item: F2P with paid content packs implies a live-ops content cadence that hasn't been costed.** Q10 is still open, and it determines art budget, live-ops commitment, telemetry volume, and whether scenarios are content packs or free updates. A premium mobile title at ~$6 in 2026 has a brutal discovery problem — you're competing against free with no ad spend. I'd want this decided before M2, not before launch.

**C3 — The journal is a writing workstream with no owner.** The design spec says it plainly: this is partly a writing project. Thirty cause-of-death templates, an event catalogue, journal fragments, scenario framing, tutorial copy. That's a real person's job for months and it's currently invisible in the milestones.

**C4 — Team size and duration aren't in either doc.** M0–M5 has no dates or headcount. I can't sanity-check scope without them.

### Need

- Legal review scheduled before M2
- Q10 decided before M2
- Named owner for narrative/writing
- M0–M5 with dates and headcount

---

## 2. Technical Director

### Working

**The pure-C# sim library is the correct call and I'd fight for it.** It's the difference between a project that can answer its own balance questions and one that guesses. The headless solver isn't a nice-to-have here — Q2 and Q3 are unanswerable by playtest, and this architecture makes them a cron job.

Named RNG streams show someone has shipped a game with a daily challenge before. Adding an event roll without invalidating existing seeds is the kind of thing you only think of after it's bitten you.

Unity 6.3 LTS with URP 2D is right. Built-In is deprecated from 6.5; there's no argument.

### Concerns

**C5 — Server-side replay validation is a bigger commitment than one line implies.** It means the sim DLL runs on a server, which means .NET hosting, a deploy pipeline, and **version-locking**: a client on sim v1.2 submitting to a server on v1.3 produces a false-negative cheat flag on every honest submission. You need per-version replay endpoints or a hard client update gate. T8 (build vs PlayFab/Nakama) is not a minor question.

**C6 — Float determinism is treated too optimistically.** ✅ *Resolved — D3: CI test at M0 with a pre-agreed decision rule.* Slot-boundary quantisation is a reasonable first bet, but the doc frames fixed-point as a fallback you probably won't need. IL2CPP across ARM variants with fast-math flags can diverge *within* a slot in ways that cross a rounding boundary. I want the cross-device replay test in CI at **M0**, not M5, so we find out while it's cheap to change.

**C7 — Two-store save with cloud sync on profile only.** Sensible, but profile contains the attribute carry (+8 cap), which is the one persistent thing with competitive value. Sync conflicts there are a cheating vector. Server-authoritative profile, or accept it.

### Need

- Cross-device determinism test in CI at M0
- T8 decided at M0 (it shapes M5)
- Position on server-authoritative profile

---

## 3. Lead Engineer

### Working

The sim/view contract is clean, and `ResolutionResult` carrying a full delta consumed by both UI and journal means one data source and no drift between what the card says and what the journal records. That's a bug class eliminated by design.

The minigame handshake — context in, scalar out, no `GameState` access — makes auto-resolve a one-line substitution and means I can build and tune minigames without touching balance. I like this a lot.

Turn-based sim with microsecond ticks means performance is a non-issue. That's a luxury.

### Concerns

**C8 — The asmdef boundary will be violated within a month unless it's enforced.** ✅ *Accepted — CI guard and `Int2`/`Float2` moved into M0.* Someone will need "just one" `Vector2` or `Mathf.Clamp` in the sim. Add a CI check that fails the build if `LastOut.Sim` references `UnityEngine`. Cheap, and it's the only thing that keeps the architecture honest.

`Vector2Int` already appears in the `CampState`/`WorldState` schemas in the tech doc — that's a UnityEngine type. Needs a sim-local `Int2`. Minor, but it's the first violation and it's already in the spec.

**C9 — Immutable snapshots at 300 slots/run × 100k solver runs = allocation pressure.** For the game it's irrelevant. For the headless solver it's the difference between an overnight job and a four-day job. Suggest the sim expose a mutable-internal / immutable-view split, or struct-based state with explicit copy, rather than allocating a fresh snapshot per slot.

**C10 — `BalanceConfig` as a ScriptableObject loaded into a plain struct is right, but there's no hot-reload story.** QA tuning during a session means an editor-only path to re-inject `BalanceData` mid-run. Small, high-value, easy to forget.

**C11 — 20-day rolling decision trace for cause-of-death** needs a defined memory bound and a defined behaviour when the run exceeds it. Also: does it survive save/restore? If not, the analysis is wrong for anyone who backgrounded the app.

### Need

- CI rule blocking `UnityEngine` in the sim asmdef
- `Int2`/`Float2` sim-local math types
- Decision on state allocation strategy before the solver is written
- Trace persistence in the save schema

---

## 4. Lead Game Designer

### Working

**Daylight driving the slot count is the best idea in the document.** It gives the season mechanical teeth, compresses the action economy exactly when the fiction says it should, and removes the need for an authored difficulty curve. It also means the late game is *structurally* tenser, not just numerically harder.

The consent rule (§10.1) is the right response to the frustration risk and it's stated strongly enough to actually hold. Weather as the sole exception is well-chosen — nobody blames a game for rain.

Gear-multiplies-strengths is the correct answer to loadout homogenisation.

### Concerns

**C12 — Morale is doing too much and it's the least legible system.** ✅ *Resolved — D5, via attribution rather than the forecast proposed here.* It's the primary tap-out condition, the anti-fasting lever, the family/isolation system, and the reward for comfort projects. Four jobs. And unlike calories — which the player can count — morale's inputs are a table of small modifiers the player never sees summed. If a player taps out at day 26 from morale and can't reconstruct why, that's the exact failure the cause-of-death analysis exists to prevent, and it'll be hardest here.

Recommend: surface a **morale forecast** in the UI ("at this rate: 8 days"), not just a bar. Let the player see the trajectory they're on.

**C13 — Three minigames but seven activities means four core actions are menu-and-roll from day one.** ✅ *Resolved — D1, though not in design's favour: two minigames, five auto-resolved. The agency cost is accepted knowingly.* Trapping and fishing are two of the most emotionally significant food sources on the show, and in MVP they're a dice roll. The player's felt agency is narrower than the design reads. I'd rather ship **two** minigames done excellently (archery, fire) and one more activity type with real decision texture — trap placement is a *judgement* puzzle with no twitch component and is probably cheap.

**C14 — The stalk phase and the shot are two different games.** Stalk is stealth/positioning, shot is twitch/timing. On a phone, in one 30-second interaction, that may be one idea too many even before wind. Consider: stalk determines *starting distance and shot difficulty*, then hands off — rather than being a free-movement phase.

**C15 — Nothing in the design creates variety between runs of the same archetype in the same biome.** ⚠️ *Partly addressed — relocation adds a late-game branch; scenarios are now core. Sharpened by F2P, since the free biome must sustain play alone.* Map seed varies, weather varies, but the decision *space* is identical. Scenarios address this; the campaign doesn't. Worth watching after M3.

### Need

- Morale forecast UI
- Decision on C13 (trap placement in, or accept four rolled activities)
- Stalk scope decision before M1

---

## 5. Lead Tester / QA

### Working

The property-based tests (no NaN, no negative mass, seed+log determinism across platforms) are specified at the right level. The nightly balance solver is effectively an automated design-QA function and I've never had one before.

M1 as a kill gate gives me a clear, early, meaningful thing to test.

### Concerns

**C16 — "Does the shake feel fair" is the project's critical risk and is not a testable statement.** I need it operationalised before M1 or the gate is a vibe check. Proposal: blind A/B with the shake model on and off at matched hit rates. If players report the shake-on build as *harder* → good. If they report it as *unfair* or *broken* → fail. Same measured hit rate, different attribution. That's the actual question.

**C17 — Compression is the hardest thing in the build to test and the easiest to ship broken.** The predicate has seven clauses; the failure mode ("silently killed the player") is severe and rare. Manual testing will not find it. I need a deterministic fuzz harness running compressed and uncompressed playthroughs of the same seed and command log, asserting identical outcomes and asserting no compressed day ever ends a run.

**C18 — Accessibility cannot be validated at M4.** ✅ *Upheld — testing starts at M2, and this was the one accessibility item defended against deferral.* VoiceOver/TalkBack failures are architectural, not cosmetic — focus order, dynamic announcement, gesture conflicts with a drag-based archery input. If we build the UI at M2 and test screen readers at M4, we rebuild the UI. Testing starts at M2.

**C19 — I have no device matrix.** "iPhone SE 2nd gen / Snapdragon 7-series" is a floor, not a matrix. Need the list before M2.

### Test coverage matrix

Every mechanic, per the brief. **A** = automated, **P** = property/fuzz, **M** = manual, **B** = balance solver, **T** = telemetry.

| Mechanic | Coverage | Specific test |
|---|---|---|
| BMR / Mifflin–St Jeor | A | Match published values, ±0.5 kcal, both sexes, age/height/weight sweep |
| Adaptive thermogenesis | A | −10% cap at 15% loss; monotonic; no discontinuity at boundary |
| Activity kcal / MET | A | Known MET × mass × duration; movement exponent applied only to movement-type |
| Mass partitioning (Forbes) | A + P | Energy conserved; `p_lean` 0.20→0.50 across BF sweep; never negative mass |
| Thermoneutral / clo stack | A | Monotonic in each input; wet penalty ×0.35 applies per-source not globally |
| Shivering cap | A | `3.0 × lean_kg`; high-BF/low-lean body fails to close gap (the anti-fast lever) |
| Hypothermia trigger | A | Fires at −2.0 °C sustained 3 slots, not 2, not 4 |
| Medical pull thresholds | A | Each of BF floor, 30% loss, BMI 17 fires independently |
| Morale accounting | A + T | All modifiers sum correctly; bounds 0–100; stacking decay caps at −8 |
| **Morale attribution** | **A + M** | HUD breakdown matches sim modifiers exactly; day summary picks the true top 2–3; "other" line sums the remainder |
| **Morale suggestion layer** | M | Never more than one active; never repeats a cause in a run; fires only in warning band |
| **Cause self-report** | T | ≥70% correct identification (§5.6.3) — the legibility gate |
| **End-condition coverage** | A | All four endings reachable; medical pull always precedes metabolic collapse; win fires when rival 9 ends |
| **Cause distribution by skill band** | **B** | No cause >60% at any band; dominant cause shifts novice→expert as designed |
| Memory events | M + T | Both choices available; "work through it" lingering decay applies 3 days |
| Slot count from daylight | A | Solar formula vs NOAA reference, 49.7°N, day 1–90; clamp 3–7 holds |
| Compression | **P** | Fuzz: compressed vs uncompressed same seed → identical outcome; **never fatal** |
| Auto-resolve | **A + M** | Exactly 0.70 × expected; used by compression; **now the permanent path for 5 of 7 activities — must read as a costed choice, not a penalty** |
| Map generation | A + P | Deterministic from seed; quality audit floor never violated; camp always ≤2 tiles from water |
| Depletion / recovery | A | +0.02/day toward cap; never exceeds cap; seasonal migration shifts caps on schedule |
| Weather | A + M | Forecast 2-day visibility accurate; storm cadence 6–11 days; escalation with day index |
| Beachcombing | A + T | Tide + storm gated; salvage table weights; never balance-critical (assert survivable without it) |
| Archery shake | **M (A/B)** | C16 blind test; Perlin not white noise; wobble scales with energy ratio |
| **Camp relocation** | **M + A** | Triggers fire on food exhaustion and shelter gap; signals visible ≥2 days before viability collapses (consent rule); shelter loss applied; fog behaviour per T11 |
| Archery ballistics | A | Deterministic given seed + input trace |
| Stalk (vision/sound/wind) | M | Legibility on min-spec device, portrait; wind flag on and off |
| Fire starting | M + B | Ferro rod vs none, dry vs wet — four-quadrant success rate sane |
| Forage ID (auto-resolve) | A | Illness roll against Foraging attribute; learned IDs persist and raise auto-resolve success rate |
| Gear effects | B | Each item measurably changes outcomes; no item is strictly dominated |
| Loadout meta | **B** | Q3 — does the show's known six dominate? Win rate spread across sampled loadouts |
| Fasting build | **B** | Q2 — 160 kg reaches day 30–38; 85 kg competent reaches 55+ |
| Rival sim | A + B | Tap-outs emergent; distribution of rival tap-out days is plausible, not clustered |
| Check-in intel | M + T | Half-day cost applied; standings accurate; three visibility modes behave |
| Consent rule | **M (audit)** | Every negative outcome traceable to a shown risk. Weather is the only exception. **Audit every event in the catalogue.** |
| Event escalation | A | Severity/frequency scale with day index |
| Cause-of-death analysis | A + M | Inflection point deterministic; survives save/restore; all ~30 templates reachable |
| Persistence | A | Attribute carry caps at +8; map intel per biome; no gear/stats carry |
| **Run-history record** | A + P | Every field captured; survives save/restore; replays to the same outcome; upload retries offline; opt-out suppresses upload only |
| **IAP / entitlements** | A + M | Paid biome locked pre-purchase, unlocked post; **no purchasable item affects outcomes**; offline play unaffected; restore-purchases works |
| Save/restore | **P** | Kill app at every slot boundary; restore identical; schema migration v1→v2 |
| Determinism | **P** | Same seed + log → identical result on every target device **and the server host**; per-slot state hashes; runs in CI from M0 |
| Tutorial | M + T | Cannot be failed; every mechanic covered; abandonment rate per step |
| Accessibility (Tier 1) | M | VoiceOver + TalkBack end-to-end, every screen, **from M2**; focus order; announcements on state change; no gesture conflict with archery drag |
| Daily Challenge | A + M | Same seed all users; one attempt enforced; server replay validates |

### Need

- C16 test design agreed before M1
- Compression fuzz harness resourced at M3
- Accessibility testing from M2
- Device matrix

---

## 6. Marketing

### Working

**"A survival contest where the real enemy is your own decay"** is a genuinely good line and the differentiation is real: nobody in the mobile survival space is simulating rivals. The closest comparable is text-based, which means a stylised 2D game is visually differentiated by default.

Accessibility is a real story with a real, engaged, underserved community that talks to each other. That's earned coverage, not bought.

### Concerns

**C20 — The pitch is legally unspeakable.** Per C1, the most compelling one-line description of this game is *"the Alone TV show as a game,"* and that's exactly the sentence we probably can't say. Every asset needs to communicate the fantasy without the shorthand. That's harder and it needs to be solved in the design of the store page, not after.

**C21 — "Placeholder art throughout" plus premium pricing is a contradiction I'll have to resolve.** ⚠️ *Partly resolved — F2P removes the pay-before-seeing problem, but store-listing screenshots still sell the install, and the free biome now carries the whole first impression. Art budget still unaddressed.* The doc itself says art becomes the marketing. A premium mobile purchase is an act of faith based almost entirely on screenshots. Whatever the art budget currently is, it's the single highest-leverage marketing spend available.

**C22 — There's no shareable artefact.** The journal is the obvious candidate — an end-of-run card ("Tapped out day 34. Lost 11 kg. Outlasted 6.") is inherently shareable, gives us organic reach, and costs almost nothing since the journal already exists. Currently it's specified as a private retention feature. Make it exportable.

**C23 — The Daily Challenge is the discovery engine and it's specced as a feature.** Same seed, everyone, 24 hours, one attempt is a content treadmill that generates conversation daily. It should be positioned as a pillar, not a mode.

### Need

- Positioning language cleared by legal
- Art direction budget decision at M2
- Shareable end-of-run card in scope

---

## 7. Analytics

### Working

The telemetry list maps events to specific open questions rather than collecting everything and hoping. `minigame_result` capturing shake amplitude and energy ratio alongside performance is exactly what's needed to answer C16 quantitatively after launch, not just in playtest.

Naming *distribution of final day by run index* as the single most important derived metric is correct. If run 5 isn't better than run 1, the knowledge-persistence model has failed and Q8 is unsolved.

### Concerns

**C24 — Premium pricing means a small N, and a small N means slow answers.** ✅ *Resolved — D4. F2P gives the volume this concern asked for.* A free title gets 100k installs and answers a balance question in a week. A premium title might get 5k and take a quarter. The balance solver partly compensates — but the solver can't tell us how *humans* respond to the shake model. Set expectations: post-launch balance iteration will be slow.

**C25 — No event for the moment of failure that isn't the run ending.** ✅ *Added — `deficit_inflection`, fired live.* I want `deficit_inflection` fired live when the sim's cause-of-death analyser would identify the point of no return — not reconstructed after. That lets us measure how many players are *already dead and still playing*, which is probably the real day-30 plateau experience.

**C26 — No rage-quit signal.** ✅ *Added — `app_background` with screen and last-action context.* `session_end` with duration doesn't distinguish "finished playing" from "closed the app mid-minigame after a missed shot." Need `app_background` with context (screen, last action, minigame performance). That's the C16 fairness question answered in the wild.

**C27 — Nothing measures whether intel is priced right.** ✅ *Addressed — check-in survival delta, plus the weekly medical read now bundles body and morale data, making the spend an easier sell.* `checkin_taken` records that it happened. I need the counterfactual: check-in uptake by day, by remaining-rival count, and survival delta between players who take check-ins and those who don't. If nobody spends half a day on it, the mechanic is dead and we should know by week two.

**C28 — Q3 needs the solver *and* live data.** The solver tells us the theoretically dominant loadout. Telemetry tells us what players actually pick, which is usually the *perceived* optimum. The gap between those two is where the design is miscommunicating.

### Need

- `deficit_inflection`, `app_background`, expanded `checkin_taken`
- Loadout pick-rate vs solver-optimal dashboard
- Realistic expectations on post-launch iteration speed given premium N

---

## 8. Consolidated actions

*Updated after the 2026-08-15 rulings. ✅ = closed by a ruling.*

### Before / at M0
| # | Action | Owner | Status |
|---|---|---|---|
| A1 | **Legal review of IP exposure** (C1, C20) | EP | **OPEN — highest consequence** |
| A2 | Operationalise the shake fairness test — blind A/B, matched hit rates (C16) | QA + Design | Open |
| A3 | Cross-device determinism test in CI, incl. server host (C6) | Tech Director | ✅ Scoped into M0 |
| A4 | CI rule: no `UnityEngine` in sim asmdef; add `Int2`/`Float2` (C8) | Lead Engineer | ✅ Scoped into M0 |
| A5 | Stalk phase scope decision (C14) | Design | Open |
| A6 | T8: backend build vs buy (C5) — **now also needs IAP/entitlements** | Tech Director | Open, scope grew |
| A7 | **M0–M5 dates and headcount** (C4) | EP | **OPEN** |
| A21 | `RunRecord` schema defined in the sim | Lead Engineer | ✅ Scoped into M0 |

### Before / at M2
| # | Action | Owner | Status |
|---|---|---|---|
| A8 | Q10 monetisation decided (C2) | EP | ✅ **Free — biomes + cosmetics** |
| A9 | **Named narrative/writing owner** (C3) | EP | **OPEN** |
| A10 | Art direction budget (C21) | EP + Marketing | Open — free biome now carries the whole first impression |
| A11 | MVP minigame count (C13) | Design | ✅ **Two: archery + fire** |
| A12 | Morale forecast UI (C12) | Design | ✅ **Superseded — attribution instead of forecast** |
| A13 | Accessibility Tier 1 built in; screen-reader testing begins (C18) | QA + Eng | ✅ Confirmed at M2 |
| A14 | Device matrix (C19) — **also the determinism test matrix** | QA + EP | Open, now blocking A3 |
| A15 | State allocation strategy for the solver (C9) | Lead Engineer | Open |
| A22 | Morale attribution: HUD breakdown + day summary card | Design + Eng | ✅ Specced |
| A23 | Cosmetic surface audit — what is actually skinnable? (Q15) | Design + Art | **NEW** |

### Before / at M3
| # | Action | Owner | Status |
|---|---|---|---|
| A16 | Compression fuzz harness (C17) | QA + Eng | Open |
| A17 | Consent-rule audit of the full event catalogue — **now including relocation triggers** | Design + QA | Open, scope grew |
| A18 | Telemetry additions (C25–C27) | Analytics | ✅ Specced |
| A19 | Shareable end-of-run journal card (C22) | Marketing + Design | Open — higher value under F2P |
| A20 | Trace persistence in save schema (C11) | Lead Engineer | ✅ Folded into `RunRecord` |
| A24 | Camp relocation implementation + playtest (Q4, Q16) | Design + Eng | **NEW** |

### Before / at M5
| # | Action | Owner | Status |
|---|---|---|---|
| A25 | Full run-history upload pipeline + consent flow (T12) | Analytics + Eng | **NEW** |
| A26 | IAP integration; **assert no purchasable item affects outcomes** | Eng + QA | **NEW** |
| A27 | Biome pricing and cadence (Q13); which biome ships free (Q14) | Product | **NEW** |
| A28 | Live-ops staffing for content cadence | EP | **NEW — uncosted** |
| A29 | Solver run: cause distribution by skill band | Analytics | **NEW** |
| A30 | `BalanceAssert` suite + 6 solver sweeps, incl. `FastingBuildLosesTo` | Analytics + Eng | **NEW — into M0** |
| A31 | **Protein/fat legibility UI** — bar + plain-language framing (`B1`, R14) | Design | **NEW — into M2** |
| A32 | Preservation/spoilage system — top balance lever, must not feel like a chore (R15) | Design + Eng | **NEW — into M3** |
| A33 | `CLAUDE.md` (root + 2 scoped) + five verification skills; validate flagged commands | Eng | **NEW — pre-M0** |
| A34 | `BuildVerify.CompileCheck` + `verify-unity.sh` with MCP-then-batch fallback | Eng | **NEW — pre-M0** |
| A35 | `ISimLog` seed/day/slot/subsystem convention; morale + nutrition breakdowns | Eng | **NEW — M0** |
| A36 | Split CI: `sim.yml` every push, `unity.yml` gated + nightly | Eng | **NEW — pre-M0** |

---

## 9. Cross-discipline disagreements — ALL RESOLVED

*Ruled by the project lead in review session, 2026-08-15.*

### D1 — Minigame count in MVP → **RESOLVED: two, archery + fire**

Design wanted three (archery, fire, trap placement). EP wanted one. QA wanted fewer for test surface reasons.

**Ruling:** archery and fire ship. Foraging ID drops to auto-resolve. **Trap placement is cut** — it's a judgement puzzle, which means authored terrain content and legible environmental art, a real design and art cost for an unvalidated mechanic. It becomes the first post-MVP minigame candidate.

The two survivors were chosen because they test different things: archery is the decay carrier, fire is the gear carrier. No redundancy.

**Accepted cost:** four food-producing activities are menu-and-roll in MVP, which narrows felt agency. Traded deliberately for two mechanics at ship quality.

**Knock-on for engineering:** auto-resolve is now a shipping feature covering five of seven activities, not a fallback. It needs test coverage equal to a played minigame (R12).

### D2 — Accessibility depth → **RESOLVED: two tiers, split by cost type**

**Ruling:** the audio archery variant is cut. Auto-resolve is the accessibility path for minigames in MVP.

**But** structural screen-reader support (labels, focus order, dynamic announcements, gesture conflict avoidance) stays in scope and starts at **M2**, on cost-avoidance grounds rather than values grounds: those failures are architectural, so retrofitting means rebuilding the UI. Days now versus weeks later.

**Stated plainly in the spec:** a blind player in MVP does not experience the signature mechanic. That's a real gap and it weakens the earned-media angle. Accepted as a scope trade, and Tier 1 is what keeps Tier 2 cheap to add post-launch.

### D3 — Determinism approach → **RESOLVED: measure at M0**

**Ruling:** the cross-device replay test goes into CI at M0. The quantisation-vs-fixed-point decision waits for its output, with a pre-agreed decision rule (tech doc §3.1).

Both positions were arguing about an empirical property of the target hardware. Measuring at M0 costs little and settles it; the conversion cost, if needed, scales with sim size, so M0 is the cheapest possible moment to find out.

The server .NET host is included in the test matrix, since client/server skew is the failure that produces false cheat flags on honest submissions (C5).

### D4 — Premium vs free → **RESOLVED: free, monetised on biomes and cosmetics**

**Ruling:** free to download. Additional biomes are the primary paid content; cosmetics secondary. **No ads, no timers, no energy, no paid gear, no paid attributes.**

This resolves marketing's discovery objection (C21) and analytics' sample-size objection (C24) at once, without compromising the no-manipulation constraint. What's sold is *more game* — the one IAP category that doesn't require designing a problem to sell the cure.

**Gear was considered and rejected on design grounds.** Gear multiplies strengths (spec §6.2), so paid gear is a paid advantage by construction, and in a last-out contest with a shared-seed Daily Challenge that's pay-to-win in the most visible way possible. Cosmetics and biomes carry no competitive weight, which is precisely why they're the right products.

**New risks accepted (R9, R10):**
- The free biome carries both demo and retention, a higher bar than "MVP content"
- The day-30 plateau now sits on top of the conversion moment. Under premium a drifting player was a retention stat; under F2P they churn *instead of* paying. This promotes §12.3 and §7.4 from polish to conversion-critical.
- Scenarios and the Daily Challenge become the free-tier retention engine, not extras
- At least one post-launch biome should ship free, so variety is felt before it's charged for

### D5 — Is morale legible enough to be a primary fail state? → **RESOLVED: attribution, not forecast**

**Ruling:** the fix is telling the player *why* morale moved, at the moment it moves — not showing them a countdown to death. Attribution teaches the causal link; a forecast only tells them when.

Three tiers (spec §5.6.1):
1. **HUD** — mood emoji + bar, tappable to expand a live itemised breakdown
2. **End-of-day summary** — the two or three largest movers only, rest batched into "other." With up to 40 modifiers a day, showing everything is toast-spam that trains dismissal.
3. **Weekly medical check** — the deep read: body composition, morale trend, plus the rival intel it already gave

Both existing surfaces are reused: the day summary is the compression card, and the HUD breakdown is the cause-of-death analysis rendered live. Same data, same components.

**QA's framing adopted:** legibility is now a measurable gate — **≥70% of players correctly identify their own cause of death** in a one-tap post-run prompt. Since the player has been told the reasons all run, failure is a UI failure, not a tuning question.

**A suggestion layer** was added: quiet, occasional nudges toward a remedy when morale enters the warning band. One at a time, never repeated for the same cause, phrased as an option. Tone target is a journal margin note, not a companion.

---

## 10. Additional rulings from the same session

### Morale is not the only death — four end conditions

Raised by the project lead: the spec read as though morale were the sole fail state. It isn't, and this is now explicit (spec §5.5).

| Cause | Type |
|---|---|
| Tap out (morale ≤ 0) | Voluntary loss |
| Medical pull (BF floor / 30% loss / BMI 17) | Involuntary loss |
| Acute event (hypothermia, injury, illness) | Loss |
| Last out | **Win** |

**Starvation is not separate** — the medical pull always fires first. Realistic, but it creates a communication problem: a player who experienced starving is told they were "pulled." The cause-of-death copy must tell the story, not cite the rule.

**Distribution target:** no single cause above 60% at any skill level. The realistic shape isn't an even split but one that *shifts with skill* — novices die of cold and panic, competents of morale, experts of medical pull from deliberately pushing the body. **The way you lose should change as you get better.** Added as a required solver output, segmented by skill band.

### Camp relocation — Q4 closed, IN

Triggered by local food exhaustion or seasonal shelter inadequacy. Costs the built shelter entirely. Must obey the consent rule: the player has to *see* the game thinning out before committing, so it's never a blind gamble.

Rationale: existing late-game pressure (shrinking daylight, depletion, migration, cold, event escalation) makes the late game *harder* but not more *varied*. Relocation is the one late-game decision that isn't a smaller version of an early-game decision. It also gives cross-run map intel its strongest payoff.

**Scoped as a playtest question, not a solved problem.** At 45–75 minute runs, day 30 is ~20 minutes in, so the plateau may be far less severe than a long-session survival game would suffer. Explicit instruction in the spec: don't over-build against boredom that may not materialise.

### Run-history telemetry

Requested directly: full life-cycle records per run — setup, every decision, attribute and body state over time, and the ending — so future balance questions can be asked against historical data instead of waiting a month for new telemetry.

Nearly free architecturally: the Daily Challenge command log *is* the decision trace, and the morale breakdown already exists to drive the HUD. New work is day-snapshot capture and upload. ~20–40 KB per run, well under 10 KB gzipped.

Six derived views defined (tech doc §11.1), led by *final day by run index* — the test of whether knowledge-persistence actually works.

---

## 11. What's still open after this session

**Unblocked and unchanged:**
- **A1 — legal review of IP exposure (C1/C20).** Still the highest-consequence item, still unaddressed, and F2P makes it slightly worse: a free app gets more downloads and more attention.
- **A9 — named narrative/writing owner (C3).** Still invisible in milestones.
- **A7 — M0–M5 dates and headcount (C4).** Not yet written.

**New, created by the F2P ruling:**
- Q13 — biome pricing and release cadence
- Q14 — which post-launch biome ships free
- Q15 — what is actually skinnable? This game has less cosmetic surface than most F2P titles, and cosmetics are now a revenue line
- T10 — entitlement verification; offline play must not break
- Live-ops commitment: F2P with paid content packs implies an ongoing content cadence, which is a staffing question EP hasn't costed

**New, created by other rulings:**
- Q16 / T11 — does relocation read as pivot or confiscation, and how does map state behave
- T12 — run-record consent and opt-out plumbing
