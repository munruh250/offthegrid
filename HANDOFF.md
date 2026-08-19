# OFF THE GRID — Handoff for Pre-Code Review

**Paste this as the opening message of a new session.**

---

## What this project is

**OFF THE GRID** is a mobile survival-contest game — Unity 6.3 LTS + URP 2D, built on a pure-C# simulation library with zero UnityEngine dependencies. Ten contestants dropped solo; last one standing wins. Its thesis: **difficulty rises because the player's body is failing, not because of an authored curve.**

Repo: `https://github.com/munruh250/offthegrid` · working directory contains `src/`, `unity/`, `outputs/`, `tools/`.

## Where it stands

The **simulation is built and balanced**. 178 tests passing, eight balance gates green, `./tools/verify-sim.sh` runs the whole gate in ~7 seconds.

The **Unity project is an empty scaffold** — no scenes, no assemblies. Nothing has been built on the presentation side.

**No code should be written in this session.** The goal is to reach the final state *before* coding starts.

## Your job

Do a deep-dive review of every document in `outputs/` and the code in `src/`, and work with me — **iteratively, looping until we're both confident** — on four things:

1. **Are we over-complicating the design?** The simulation grew a lot of interacting systems during balance work. Some may be earning their keep and some may not. Say so plainly where a system could be cut or collapsed without losing what makes the game work.

2. **Are we over-complicating the code, and is it following best practice?** Review `src/OffTheGrid.Sim` and `src/OffTheGrid.Data` for structure, naming, testability, and anything that will be painful to build a UI against.

3. **Is the implementation plan set up for token-efficient model routing?** `CLAUDE.md` has a routing table and `.claude/agents/` has three model-pinned subagents. `outputs/05-agent-workflow.md` has the scoping strategy. Assess whether the plan actually minimises context per task, and improve it.

4. **Do the UI specs and layouts hold up to AAA quality?** `outputs/19-ui-screens.md` specs fifteen screens and their elements; `outputs/07-ui-mockups.html` and `10-merged-map-system.html` carry the locked visual direction. Push on whether these are genuinely good, and whether they'll work on a phone.

**Loop until we're confident in the mobile app plan.** Ask me questions. Disagree where you disagree.

## Read these first, in this order

| Doc | What it is |
|---|---|
| **`outputs/18-as-built-reference.md`** | **Start here.** What the code actually does, and where it differs from the original design docs. Where 18 and 01–04 disagree, 18 is correct. |
| **`outputs/20-decisions-register.md`** | Every ruling made, with evidence. Do not re-open these without new evidence. |
| `outputs/19-ui-screens.md` | Fifteen screens, every element, what each reads from the sim, and a build order. |
| `outputs/01-design-spec.md` | The game. Design intent and reasoning. |
| `outputs/02-technical-implementation.md` | The codebase plan. Note: predates most of the sim. |
| `outputs/04-balance-economy.md` | Numbers and validation. §7.5 carries the ratified Resolve rescale. |
| `outputs/05-agent-workflow.md` | How code gets written and verified here. |
| `outputs/17-crisis-and-attribute-design.md` | Specced, not built. The next system. |
| `outputs/15-build-diversity-audit.md` | How balance was diagnosed. Useful for method, not current numbers. |
| `outputs/07`, `10` (HTML) | Locked visual direction. Open in a browser. |

Docs 03, 11, 12, 13, 14, 16 are context; read if a question points at them.

## Conventions that matter

- **`src/OffTheGrid.Sim` has ZERO UnityEngine dependencies.** CI blocks it. This is what keeps the compile loop at 7s instead of 90s.
- **One assembly per session.** Sim work and Unity work are separate contexts.
- **Never report a task complete without a passing verification run in the transcript.**
- Determinism: named RNG streams via `Rng.Stream(name)`. Never `System.Random`, never `string.GetHashCode()`.
- Balance constants live in `OffTheGrid.Data`, never inline.

`CLAUDE.md` carries all of this plus the model-routing table.

## What NOT to redo

The balance work is done and I don't want it re-litigated. Sixteen of sixteen route returns are within 0.2 of target; seven attributes are within 0.17 of each other, down from a 55× spread. If you think something there is wrong, say so — but bring evidence, and don't start from scratch.

## Known open, and deliberately so

- **A1 — legal review of IP exposure.** Highest consequence. Blocks store copy, not code.
- **B1 — protein ceiling legibility.** The design's highest-risk item; only a playtest can answer it.
- Dana wins 33.2% of contests. Either the stalk is slightly strong or it's roster variance.
- Should a pure builder be viable? Bushcraft currently has no path to food.
- Conservative resters win ~0%, **by design ruling** — viable after a big kill or as a late-game move.
- `unity/` is an empty scaffold, and `unity-compile` / `unity-test` are placeholder skills pointing at a `BuildVerify.cs` that does not exist.

## What I want out of this session

A plan I'm confident enough in to start building against — simplified where it's over-built, sound on code structure, efficient on model routing, and with UI specs I'd be happy to hand a designer. **No code.**
