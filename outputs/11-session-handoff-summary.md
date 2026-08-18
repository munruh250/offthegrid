# OFF THE GRID — Design Session Handoff Summary

Session date: August 16–17, 2026
Current phase: UI/UX direction locked; design system started
Status: Ready for next iteration on desktop

## What you've built so far

Four complete design documents (all in `/outputs/`):

1. **01-design-spec.md** — 901 lines. The game's complete mechanical spec. Core thesis: difficulty rises because the player's body is failing, not via an authored curve. Single-player with 9 simulated rivals. Six attributes, free-form body setup, decay-driven morale system. Win condition: last man standing.
2. **02-technical-implementation.md** — 680 lines. Unity 6.3 LTS + URP 2D. Pure-C# sim library (`LastOut.Sim`) with zero UnityEngine dependencies. Determinism at M0, cross-device testing, ~50-row QA matrix per mechanic, `BalanceAssert` suite. Pre-M0 now includes agent workflow and CI setup.
3. **03-discipline-reviews.md** — 450 lines. Seven-role balanced review (EP, Tech Director, Lead Engineer, Lead Designer, QA, Marketing, Analytics). 28 concerns raised, 32 actions, 5 rulings decided and folded in. All open questions tracked.
4. **04-balance-economy.md** — 459 lines. All numeric values for food, fuel, shelter, gear attrition. Validation includes: competent player reaches day 60 ✓, fasting build defeated by morale not physiology ✓, protein ceiling binds as designed ✓. Morale constants retuned. Tuning-lever priority order specified.

NEW this session:

5. **05-agent-workflow.md** — 548 lines. How code gets written and verified in the GitHub repo. CLAUDE.md conventions, five verification skills (sim-verify, unity-compile, unity-test, balance-check, determinism-check), CI split, logging conventions. Token strategy: sim/Unity work in separate agent sessions.

Interactive design studies (all live in browser, respond to day slider):

6. **06-design-directions.html** — Five palettes × five HUDs × five type pairings, live-blended. Starting point for direction.
7. **07-ui-mockups.html** — Nine full screens (camp day loop, map, archery, morale expanded, nutrition, check-in, end-of-run, loadout, summary). Broadcast lower-third HUD locked. Seasonal palette (Cedar→Cold Front) interpolates across the run as a visual difficulty meter.
8. **08-layout-map-studies.html** — Three layout experiments (edge instrument, survey sheet, broadcast lower-third) × five map treatments (hex survey, painted grid, vertices, woodcut, cartographer). Broadcast lower-third chosen as direction.
9. **09-map-techniques.html** — Six cartography conventions tested: Voronoi territories, node network, orienteering contour, watershed basins, corkboard collage, range rings. All held broadcast HUD constant.
10. **10-merged-map-system.html** — The final map direction: Voronoi territories as base, elevation-gated orienteering contour fading in on high ground, route planning line appearing on demand (node-network style). Tappable hero map with two gate-curve comparisons. This is shipping.

## Design decisions locked

### Visual direction

- **Palette:** Archivo (single family, 300–900 weights). Cedar & Lichen (September) → Cold Front (November). Interpolates on `t=(day-1)/59`.
- **HUD:** Broadcast lower-third. Angular clipped tab, hard left rule, dense stat strip. No rounded corners, no bordered cards.
- **Map:** Territories + elevation-gated contour + on-demand route. Territories always visible, contour reveals on ridges, route line appears when a destination is chosen.
- **Layouts:** Type and rules do the separating, not boxes. Grain texture over everything. Extreme weight/size contrast. Asymmetric alignment.

### Mechanical (confirmed)

- Balance target **Q2**: Fasting build loses to competent play via morale's idleness penalty, not physiology. Tested and locked.
- **Morale is load-bearing.** Remove it and the game has a dominant degenerate strategy. Morale constants retuned (roughly 2× less harsh than v0.1).
- **Protein ceiling** is the highest-risk item (B1). Requires plain-language framing ("You're eating enough. It's the wrong thing.") plus a visual bar showing the ceiling bind.
- **Preservation capacity** is the top balance lever. Never tune raw kcal content of animals — cap what can be preserved instead.
- Morale/nutrition/RNG systems all require specific legibility work to avoid reading as the game cheating (R14, R15, R16).

### Milestones (updated)

- **M-pre** (before M0): CLAUDE.md, five skills, BuildVerify.CompileCheck, split CI (sim.yml / unity.yml)
- **M0:** LastOut.Sim + tests + headless runner + cross-device determinism + BalanceAssert + ISimLog conventions
- **M1:** Archery vertical slice — KILL GATE: does starving-player shake read as body failing or game cheating?
- **M2:** Full day loop, map, fog, camp, gear, 2 minigames, auto-resolve, morale attribution HUD, protein/fat legibility UI, accessibility Tier 1
- **M3:** Rivals, check-in, weather, events, compression, relocation, preservation, spoilage
- **M4:** Tutorial, journal, scenarios, Daily Challenge, summary card, medical read
- **M5:** Balance solver pass, telemetry, IAP, soft launch

### Your setup

- GitHub repo with version control, managing sim and Unity assemblies separately
- Unity local on your machine with an MCP server driving edits (agent workflow accounts for this)
- Artifact persistence is available if you need state across sessions (not currently used)

## What's still open

### Highest-risk items (take to M0)

- **A1 — Legal review.** The ten-item mechanic, ten-contestant elimination, tap-out language, medical check-in are cumulatively close to source IP. Need real lawyer before store copy. Working title "OFF THE GRID" is fine.
- **B1 — Protein ceiling legibility.** Full cache, still starving is correct but reads like a cheat. Framing and UI must sell it.
- **M1 playtest.** Does archery shake read as body failing or game cheating? This is the thesis at stake.

### Still to design

- **Relocation system** (camp destruction, forced travel, restock decisions) — design spec flags as Q4, playtest priority
- **Rivals** — AI simplicity, when they tap out, how they interact with the player intel system
- **Terrain generation** — the show's biome set (Vancouver Island MVP, then Alaska, Patagonia, etc.)
- **Post-launch biome cadence** — one free, pricing model on the rest

### Actions not yet started

- A2 Shake-fairness A/B test design
- A4–A8 (various — see 03-discipline-reviews.md for full list)
- A9 Named narrative/writing owner (journal, 30 death templates, event catalogue)
- A10 Art direction budget
- A14 Device matrix (blocking M0 cross-device test)

### Open balance questions

- **B2:** Should protein ceiling scale with Fitness or Cold Adaptation?
- **B3:** Is bear-as-only-fat-source too concentrated?
- **B4:** Does preservation micromanagement become tedious?
- **B5:** Arrow scarcity vs. archery as signature mechanic — tension or mistake?
- **B6:** Do morale constants survive relocation's shelter loss?
- **B7:** Elk at 177k kcal trivialises month one?
- **B8:** Should rival AI run the protein/fat model?

See 04-balance-economy.md §10 for full list.

## How to continue

### Next steps (immediate)

1. **Lock the map gate curve.** Choose between hard gate (abrupt at 0.58) or gradual gate (smooth 0.42–0.85). Both are coded and live in the hero map.
2. **Design the relocation system.** When does it trigger? What does it cost? How does it reshape the late game? This is marked as Q4 in the spec but is now on the critical path for playtesting.
3. **Design the rival AI.** Fidelity level? When do they tap out? How does the check-in intel window surface their state to the player?
4. **Settle the device matrix (A14).** Which phones, tablets, tablets-in-landscape do you need to test? M0 cross-device determinism test is blocked on this.

### For the next design session

- Bring the merged map system (10-merged-map-system.html) — it's your shipping direction
- Bring one of the full-screen mockups (07-ui-mockups.html) as a reference for component behaviour
- Have the balance doc (04) open for any tuning conversations
- Have the tech doc (02) open if you're discussing implementation constraints

### If you're handing to a designer

- Start with 06 (the direction explorer) to understand the palette/HUD/type axes
- Then 07 (full screens in context) to see the system in use
- Then 10 (the map) to understand the merged system
- The design docs (01, 02, 03, 04) have everything they need to know about what the game actually does

### If you're handing to an engineer

- Start with 02 (tech implementation) — it's the spec for the codebase
- Then 05 (agent workflow) — it's how they build it with AI assistance
- Then 01 (design spec) §2.1 for the architecture that matters most (pure-C# sim boundary)
- Balance doc (04) for the constants they need to implement

## The story so far (compressed)

Started with a brainstorm on the TV show Alone and a thesis: difficulty rises because the body fails, not via an authored curve. That led to six attributes, free-form body setup, and a decay system where the player watches themselves get weaker.

Three discoveries came out of testing:

1. **Morale isn't just theme.** The fasting build can survive to day 90 on physics alone. Morale's idleness penalty is what kills it. That makes morale a primary balance lever, not a secondary one.
2. **Protein is a trap, fat is the currency.** Real nutrition data (rabbit starvation, protein ceiling at 2.5g/kg bodyweight/day) produces the mechanic where "full cache, still starving" is correct and interesting.
3. **Preservation capacity is stronger than animal yield.** A moose is realistically 300× a trout but only 7–10× after spoilage/drying caps. This makes the preservation rack the most powerful tuning lever and gives every big kill a real constraint.

The UI direction started with Claude-app defaults (dark panels, hairlines, pills) and got thrown out. Built instead from the show's own reference materials: survey sheets, field journals, cartography. No rounded corners, no bordered cards. Type and rules do the separating.

Map system evolved from hex grids (Civ style) to territories + elevation-gated contour + route planning. Three layers, one coherent read: where you are, where climbing gets interesting, where you've chosen to go.

Everything responds to the day slider — palettes interpolate, screens age, rivals fall, knowledge accumulates. The game is a 60-day arc and the UI itself expresses that arc.

## Files you need

All in `/mnt/user-data/outputs/`:

Documents (Markdown):

- `01-design-spec.md` — The game
- `02-technical-implementation.md` — The codebase
- `03-discipline-reviews.md` — Concerns & decisions
- `04-balance-economy.md` — Numbers & validation
- `05-agent-workflow.md` — How to build it

Interactive studies (open in browser):

- `06-design-directions.html` — Direction explorer
- `07-ui-mockups.html` — Full screens by day
- `08-layout-map-studies.html` — Layout & map experiments
- `09-map-techniques.html` — Six map techniques
- `10-merged-map-system.html` — Shipping map direction (tappable)

This file:

- `11-session-handoff-summary.md` — You are here

## Questions to ask next

- **Legal:** Can we use the ten-item / ten-contestant / medical-pull structure, or do we need to reskin it?
- **Design:** Does the elevation-gated contour feel right in playtesting, or should we gate on something else (time survived, distance from camp)?
- **Engineering:** Can the MCP server you're using reliably drive Unity compiles, or do we need a fallback to batch mode?
- **Balance:** Should the relocation cost be one shelter + half a day, or is that too punishing?
- **Analytics:** How do we track "did the player understand why they died" for the 70% cause-of-death gate?

## How the repo should be structured (from 05-agent-workflow.md)

```
/
├── CLAUDE.md                     # root conventions
├── .claude/skills/               # verify skills
├── .github/workflows/
│   ├── sim.yml                   # every push, ~1 min
│   └── unity.yml                 # gated, slower
├── src/
│   ├── LastOut.Sim/              # SCOPED CLAUDE.md
│   ├── LastOut.Data/
│   └── LastOut.Tests/
├── unity/LastOut/                # SCOPED CLAUDE.md
│   ├── Assets/Scripts/
│   └── Editor/BuildVerify.cs
├── tools/
│   ├── verify-sim.sh
│   ├── verify-unity.sh
│   └── parse-test-results.py
└── LastOut.sln
```

The pure-C# sim boundary is what makes the agent workflow fast — balance work never loads Unity (7s compile loop instead of 90s).

---

Ready to continue on desktop. Paste this handoff, reference any of the files above, and we can pick up where you want.
