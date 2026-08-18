# OFF THE GRID — Agent Workflow & Build Verification v0.1

*Companion to `02-technical-implementation.md`. This document covers **how code gets written and verified in the repo**, not what the code does. It exists because the tech doc specifies test coverage thoroughly and specifies build verification not at all.*

> **Read §12 before trusting this document.** Some commands here — specifically Unity's batch-mode flags and the CLAUDE.md/skills frontmatter — are written from recall and need validating against current docs. They are flagged individually. Everything on the `dotnet` side and the repo structure is standard and stable.

---

## 1. What was missing

Audit of the four existing documents found **zero** coverage of: `CLAUDE.md`, agent scoping, MCP integration, batch-mode compilation, `dotnet` commands, GitHub Actions workflows, logging conventions, or token strategy. Every instance of "skill" and "hook" in those docs is the game-design sense — *skill ladder*, *test hooks* — not the agentic sense.

What **does** exist and is good: the pure-C# sim boundary, the `UnityEngine`-reference CI guard, cross-device determinism testing at M0, the ~50-row QA matrix, and the `BalanceAssert` suite.

The gap is a **feedback loop**. An agent editing C# in a Unity project has no idea whether its code compiles, because Unity compiles in the editor by default. Without a gate it writes plausible, well-structured, non-building code and neither of you finds out for an hour.

---

## 2. The core insight — the architecture already solved the hard part

`OffTheGrid.Sim` having zero Unity dependencies is not just good for testability. It is the single most valuable property this repo has for agent work, because it means **most tasks never need to load Unity at all.**

| Path | Build time | Test time | Needs Unity? |
|---|---|---|---|
| `OffTheGrid.Sim` + `OffTheGrid.Tests` | ~2 s | ~5 s | ❌ No |
| Unity assemblies | 30–90 s | 60–180 s | ✅ Yes |

A 15–40× faster loop for the majority of the work. **Protect this boundary aggressively** — the moment a `UnityEngine` reference leaks into the sim, the fast path is gone and agent iteration slows by an order of magnitude. The existing CI guard is doing more work than the tech doc credits it for.

### 2.1 Token cost by session scope

Estimated against the assembly layout in tech doc §2.1 (~137k tokens of code at full build-out):

| Session scope | Code context | % of repo |
|---|---|---|
| Body sim bugfix | ~33,000 | 24% |
| New minigame (Unity only) | ~39,600 | 29% |
| Balance tuning (sim only) | ~41,800 | 30% |
| **Unscoped full-stack** | **~137,500** | **100%** |

**Rule: one assembly boundary per session.** Sim work and Unity work are separate sessions with separate context. A task that genuinely spans both — a new minigame needing new sim plumbing — should be split into two sessions with the `MinigameContext`/`MinigameResult` structs as the handoff contract. Those structs already exist precisely to make that seam clean (tech doc §2.3).

This is the whole token strategy. It costs nothing to adopt and saves 3–4× on the average task.

---

## 3. Repo layout

```
/
├── CLAUDE.md                     # root conventions — see §4
├── .claude/
│   ├── skills/
│   │   ├── sim-verify/SKILL.md
│   │   ├── unity-compile/SKILL.md
│   │   ├── unity-test/SKILL.md
│   │   ├── balance-check/SKILL.md
│   │   └── determinism-check/SKILL.md
│   └── settings.json
├── .github/workflows/
│   ├── sim.yml                   # fast, every push
│   └── unity.yml                 # gated, slower
├── src/
│   ├── OffTheGrid.Sim/              # CLAUDE.md (scoped)
│   ├── OffTheGrid.Data/
│   └── OffTheGrid.Tests/
├── unity/OffTheGrid/                # Unity project
│   ├── Assets/
│   │   ├── Scripts/              # CLAUDE.md (scoped)
│   │   └── Editor/BuildVerify.cs # §6
│   └── ProjectSettings/
├── tools/
│   ├── verify-sim.sh
│   ├── verify-unity.sh
│   └── parse-test-results.py
└── OffTheGrid.sln
```

**Nested `CLAUDE.md` files matter here.** A root file plus one in `src/OffTheGrid.Sim/` and one in `unity/OffTheGrid/Assets/Scripts/` lets each side carry its own rules without an agent loading both. The sim file says "never reference UnityEngine"; the Unity file says "never put game logic here."

### 3.1 Branch and PR conventions

- Trunk-based, short-lived branches: `feat/`, `fix/`, `balance/`, `chore/`
- **`balance/` branches must include the solver output diff in the PR body.** A balance change without before/after numbers is unreviewable.
- Agents commit; agents do not merge. PR review is a human gate.
- Squash merge, conventional commit subjects.

---

## 4. CLAUDE.md — root file

> ⚠️ Structure and content are sound; if the CLAUDE.md discovery/precedence behaviour has changed, adjust placement accordingly (§12).

Keep it short. A long CLAUDE.md is loaded into every session and is pure overhead — this file is a tax on every task, so it should contain only what is load-bearing.

```markdown
# OFF THE GRID

Mobile survival-contest game. Unity 6.3 LTS + URP 2D.
Design docs in /docs. Read them only when the task requires design context.

## Architecture — the one rule that matters

`src/OffTheGrid.Sim/` is a pure C# library with ZERO UnityEngine dependencies.
It is the game. Unity is a renderer and input layer.

- NEVER add a `UnityEngine` using/reference to OffTheGrid.Sim. CI blocks it.
- NEVER put game logic in `unity/`. Presentation and input only.
- Sim exposes immutable snapshots + a command queue. The view never mutates sim state.
- Minigames return a normalised scalar 0..1. They do not compute outcomes.

## Session scoping — read this before opening files

Work on ONE assembly per session. Do not load sim and Unity code together.
Cross-boundary work: split into two sessions, use MinigameContext /
MinigameResult / ResolutionResult as the handoff contract.

## Verification — non-negotiable

Sim changes:    run the `sim-verify` skill (~7s). Always. Before claiming done.
Unity changes:  run `unity-compile`, then `unity-test`. Slow — batch your edits
                and gate once, don't compile per file.
Balance changes: run `balance-check`. A balance change without solver output is
                not reviewable.

Never report a task complete without a passing verification run in the transcript.
If a gate fails, fix it — do not describe the failure and stop.

## Determinism — this breaks silently

- Use `Rng.Stream(name)` (PCG32). NEVER System.Random or UnityEngine.Random.
- No `DateTime.Now`, no unordered dictionary iteration, no float accumulation
  across slot boundaries without quantisation.
- Adding a new RNG consumer means adding a NAMED STREAM, not reusing one.
  Reusing a stream shifts every downstream draw and breaks saved replays.

## Style

- C# 12, file-scoped namespaces, `sealed` by default.
- Sim types: readonly structs where practical. Avoid LINQ in per-slot paths.
- Sim-local `Int2`/`Float2` — not Vector2Int, which is a Unity type.
- No abbreviations in public names. `kcal` and `clo` are domain terms, keep them.
- Balance constants live in OffTheGrid.Data tables, never inline in logic.

## Do not

- Do not add dependencies without asking.
- Do not change balance constants to make a test pass. Ask.
- Do not disable or weaken a failing assertion. Especially not
  `BalanceAssert.FastingBuildLosesTo()` — see docs/04 §7.3.
```

That last line matters more than it looks. `FastingBuildLosesTo` guards a property that took a whole morale system to achieve. It is exactly the kind of assertion an agent will "fix" by loosening the tolerance.

---

## 5. Skills

Each skill is a folder with a `SKILL.md`. The pattern: a description precise enough to trigger reliably, then the command, then how to interpret the output.

> ⚠️ Frontmatter field names should be checked against current docs (§12). The commands inside are the substantive part.

### 5.1 `sim-verify` — the workhorse

```markdown
---
name: sim-verify
description: Build and test the pure C# simulation library. Use after ANY change
  to src/OffTheGrid.Sim, src/OffTheGrid.Data, or src/OffTheGrid.Tests. Fast (~7s) — run it
  freely, and always before reporting a sim task complete. Does not require Unity.
---

# Verify the simulation

    ./tools/verify-sim.sh

Runs:
  dotnet build src/OffTheGrid.Sim -warnaserror
  dotnet test  src/OffTheGrid.Tests --logger "console;verbosity=minimal"
  ./tools/check-no-unity-refs.sh

## Interpreting results

- Build error → fix it. Warnings are errors here; do not suppress.
- Test failure → read the assertion. Do NOT adjust the expected value to match
  actual unless the change was intentional and stated in the task.
- `check-no-unity-refs` failure → a UnityEngine reference leaked into the sim.
  This is the most serious failure in the repo. Remove it; use Int2/Float2 or
  add a sim-local type. Never satisfy it by editing the check.
```

`-warnaserror` is deliberate. Agents are good at producing code that compiles with warnings, and unused-variable or unreachable-code warnings in a determinism-critical sim are worth surfacing immediately.

### 5.2 `unity-compile` — the missing gate

This is the one that was absent and matters most.

```markdown
---
name: unity-compile
description: Compile-check the Unity project in batch mode. Use after editing any
  C# under unity/OffTheGrid/Assets. SLOW (30-90s) and takes an exclusive project
  lock — batch your edits and run this once, not per file. Required before
  reporting any Unity task complete.
---

# Unity compile gate

    ./tools/verify-unity.sh compile

Wraps:

    "$UNITY_PATH" -batchmode -nographics -logFile - \
      -projectPath unity/OffTheGrid \
      -executeMethod OffTheGrid.Editor.BuildVerify.CompileCheck \
      -quit

Exit 0 = clean. Non-zero = compile errors, printed as
`ERROR <file>(<line>,<col>): <message>`.

## Constraints — read these

- Unity holds an exclusive lock on the project. If the Unity Editor is open on
  this project, this WILL fail or hang. Close the editor, or use the MCP path
  (§8) which drives the already-open editor instead.
- Do not run concurrently with `unity-test`.
- If it hangs past ~3 minutes, kill it and report. Do not retry blindly — a
  hung batch-mode Unity can leave a stale lock file requiring manual cleanup.
```

### 5.3 `unity-test`

```markdown
---
name: unity-test
description: Run Unity Test Framework tests in batch mode and report results.
  Use after unity-compile passes, for any change touching Unity assemblies.
  VERY SLOW (1-3 min). EditMode by default; PlayMode only when the change
  affects runtime behaviour.
---

# Unity tests

    ./tools/verify-unity.sh test editmode
    ./tools/verify-unity.sh test playmode

Wraps:

    "$UNITY_PATH" -batchmode -nographics -logFile - \
      -projectPath unity/OffTheGrid \
      -runTests -testPlatform EditMode \
      -testResults artifacts/unity-tests.xml

Then: `python3 tools/parse-test-results.py artifacts/unity-tests.xml`

## Exit codes
  0 = all passed · 2 = tests failed · 3 = run could not start

## Critical
Do NOT add `-quit` to a `-runTests` invocation. Unity will exit before the tests
run and report success. This is a silent false pass — the worst possible failure
mode for an automated gate.

Parse the XML for results. Do not judge pass/fail by scraping the log text.
```

That `-quit` interaction is a real and well-known trap, and it produces a *green* result, which makes it dangerous in exactly the way an agent workflow can't tolerate.

### 5.4 `balance-check`

```markdown
---
name: balance-check
description: Run the BalanceAssert suite and, for tuning changes, the solver
  sweeps. Use for ANY change to body/morale/food/wood constants or to
  OffTheGrid.Sim/Balance. Produces the before/after numbers required in a
  balance/ PR body.
---

# Balance verification

Fast gate (~30s), always:

    dotnet run --project src/OffTheGrid.Sim/Balance -- --assert-only

Full sweep (minutes to hours), for tuning changes:

    dotnet run --project src/OffTheGrid.Sim/Balance -- \
      --runs 100000 --archetypes all --loadouts sampled \
      --out artifacts/balance-$(git rev-parse --short HEAD).json

    python3 tools/balance-diff.py artifacts/balance-BASE.json artifacts/balance-HEAD.json

## Assertions that must never be weakened
  FastingBuildLosesTo(Competent)   docs/04 §7.2-7.3 — the most important
                                   assertion in the codebase
  CompetentRunLength(55, 70)
  NoCauseExceeds(0.60)
  GearAttritionNeverRunEnding()

If one fails, the change is wrong, not the assertion. Report and stop.
```

### 5.5 `determinism-check`

```markdown
---
name: determinism-check
description: Verify seed + command log replays to a byte-identical end state.
  Use after any change to RNG usage, slot resolution order, floating-point math,
  or save serialisation. Cheap on host (~20s); the full device matrix is CI-only.
---

# Determinism

    dotnet run --project src/OffTheGrid.Sim/Balance -- --replay-verify --seeds 64

Asserts byte-identical end state and per-slot quantised hashes across 64 fixed
seeds on the host runtime.

Cross-device (IL2CPP + ARM) verification runs in CI only — see docs/02 §3.1.
Host passing does NOT prove device determinism.

## Common causes of a failure here
  - System.Random / UnityEngine.Random introduced
  - New RNG consumer reusing an existing named stream (shifts downstream draws)
  - Dictionary/HashSet iteration order relied upon
  - Float accumulated across a slot boundary without quantisation
  - DateTime.Now or Environment.TickCount reaching sim code
```

---

## 6. The compile-check editor script

`unity-compile` needs this to exist. Batch-mode Unity's own exit code on compile failure has historically been unreliable, so the gate should assert explicitly rather than trusting it.

> ⚠️ The compilation-callback API surface has moved between Unity versions. Verify against 6.3 docs.

```csharp
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace OffTheGrid.Editor
{
    public static class BuildVerify
    {
        public static void CompileCheck()
        {
            var failed = false;

            CompilationPipeline.assemblyCompilationFinished += (asm, messages) =>
            {
                foreach (var m in messages.Where(m => m.type == CompilerMessageType.Error))
                {
                    failed = true;
                    // Machine-parseable, one error per line, agent-readable.
                    Console.WriteLine($"ERROR {m.file}({m.line},{m.column}): {m.message}");
                }
            };

            AssetDatabase.Refresh();
            CompilationPipeline.RequestScriptCompilation();
            EditorApplication.UnlockReloadAssemblies();

            if (EditorUtility.scriptCompilationFailed || failed)
            {
                Console.WriteLine("COMPILE FAILED");
                EditorApplication.Exit(1);
            }

            Console.WriteLine("COMPILE OK");
            EditorApplication.Exit(0);
        }
    }
}
```

Deliberately writing one error per line in a fixed format. An agent parsing Unity's raw log output is unreliable; a stable `ERROR file(line,col): message` contract is not.

---

## 7. Logging conventions

Nothing on this in the existing docs. For a deterministic sim it matters, and it matters as much for the agent debugging a divergence as for you.

### 7.1 Structure

Every sim log line carries the coordinates needed to reproduce it:

```
[seed:8F2A1C4B][d34/s2][Body] deficit=-1738 kcal usable=1112 protein_ceiling_bound=true
[seed:8F2A1C4B][d34/s2][Morale] -3.5 => 41.2 | food_insecure:-2.0 idle:-1.0 wtloss:-0.5
[seed:8F2A1C4B][d34/s3][Rng:AnimalBehaviour] draw#412 => 0.7734
```

Seed, day, slot, subsystem. Given any line you can replay to that exact state. Without the seed tag a log is an anecdote.

```csharp
public interface ISimLog {
    void Trace(SimSystem sys, string msg);   // per-slot detail, off in release
    void Info (SimSystem sys, string msg);   // state transitions
    void Warn (SimSystem sys, string msg);   // recoverable anomaly
    void Error(SimSystem sys, string msg);   // invariant violated
}
```

`ISimLog` lives in the sim as an interface with a no-op default. Unity supplies an adapter to `Debug.Log`. **The sim never calls `Debug.Log` directly** — that would be a UnityEngine dependency and would break the fast path.

### 7.2 What to log, specifically

The two systems that will generate the most debugging pain are morale and nutrition, because both produce outcomes the player (and an agent) finds surprising:

- **Morale**: always log the itemised breakdown, never just the delta. `MoraleBreakdown` already exists to drive the HUD (design spec §5.6.1) — log the same object. One data source for HUD, journal, and log.
- **Nutrition**: log when the protein ceiling binds, with the numbers. "Full cache, still starving" is correct behaviour that reads exactly like a bug (risk R14). A log line that shows the ceiling binding saves an hour of misdirected investigation every time.
- **RNG**: draw counter per named stream, at Trace. Determinism failures are almost always "a draw was added or reordered," and a draw count diff localises it instantly.
- **Slot resolution**: one Info line per slot with the `ResolutionResult` delta summary.

### 7.3 Levels by build

| Build | Level | Notes |
|---|---|---|
| Sim tests / headless | Trace | Full detail; it's fast and text-only |
| Unity editor | Info | Trace on per-subsystem toggle |
| Development build | Info | Ring buffer, last 2,000 lines into bug reports |
| Release | Warn | Plus the ring buffer for crash attachment |

Never string-interpolate a Trace message that will be discarded — guard it, or the sim's allocation profile gets wrecked by logging that never prints.

---

## 8. MCP server for Unity

You're driving Unity edits through an MCP server on your local machine. That's a real improvement over batch mode for iteration, with caveats.

**What it buys you:** the editor is already open and warm, so a compile check that costs 30–90 s in batch mode can come back in a few seconds. It also sidesteps the project-lock conflict entirely — the whole reason batch mode fights an open editor.

**The caveats:**

1. **Keep the batch-mode path working as a fallback.** MCP connections drop, editors crash, and the CI runner has no editor to talk to. `verify-unity.sh` should detect an available MCP endpoint and use it, falling back to batch mode otherwise. Same command, same contract, either way.
2. **Automatic edits raise the value of gates, not lower it.** An agent that can change project state without an explicit file-write step needs the compile-and-test gate more, because the blast radius is wider and less visible in the transcript.
3. **Verify what your server actually exposes.** They vary considerably — some drive compilation and tests, some only read state. Tell me which one you're using and I'll tighten this section; the workflow above works either way but the fast paths differ.
4. **Don't let it become the only path.** If the repo can only be verified on your machine with your editor open, CI can't gate PRs and the M0 cross-device determinism test has nowhere to run.

---

## 9. CI

Two workflows, deliberately split by cost.

**`sim.yml`** — every push, every PR. Runs on `ubuntu-latest`, no Unity, no licence, ~1 minute:

```yaml
name: sim
on: [push, pull_request]
jobs:
  verify:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with: { dotnet-version: '8.0.x' }
      - run: dotnet build src/OffTheGrid.Sim -warnaserror
      - run: ./tools/check-no-unity-refs.sh
      - run: dotnet test src/OffTheGrid.Tests
      - run: dotnet run --project src/OffTheGrid.Sim/Balance -- --assert-only
      - run: dotnet run --project src/OffTheGrid.Sim/Balance -- --replay-verify --seeds 64
```

**`unity.yml`** — PRs touching `unity/**`, plus nightly. Needs a licence in CI and a self-hosted or licensed runner; slower and more fragile, so it gates rather than blocks every commit.

Nightly additionally: the 100k-run solver sweep, and the cross-device determinism matrix from tech doc §3.1 (which needs real ARM devices — that's a hardware question tied to open action A14, the device matrix).

**The important property:** the fast job covers the game's actual logic. Balance assertions, determinism, and the sim boundary are all validated in about a minute without Unity in the loop. That's the architecture paying off again.

---

## 10. Coding conventions worth stating

Agent-specific, because these are the mistakes that are easy to make and expensive here:

| Convention | Why |
|---|---|
| Balance constants in `OffTheGrid.Data` tables only | An inlined constant is invisible to the solver and to tuning |
| Readonly structs for sim state | Immutability is load-bearing for the snapshot contract |
| No LINQ in per-slot paths | Allocation in the tick loop; 100k-run solver amplifies it hugely |
| Named RNG streams, never reuse | Reuse shifts downstream draws and silently invalidates saved replays |
| No `DateTime.Now` in sim | Non-deterministic by construction |
| Sim-local `Int2`/`Float2` | `Vector2Int` is a Unity type; already leaked once |
| One assertion per test | A failing multi-assert test tells an agent less than it needs |
| Sim never calls `Debug.Log` | Would break the zero-Unity-dependency property |

---

## 11. Anti-patterns

Things that will happen if not explicitly forbidden:

1. **Loosening a failing assertion.** The single highest-risk agent behaviour in this repo. `FastingBuildLosesTo` is the one to guard hardest — the assertion looks arbitrary and its justification lives in a design document.
2. **Editing the `no-unity-refs` check to pass.** Satisfies the gate, destroys the property.
3. **Adding `-quit` to `-runTests`.** Green light, no tests run.
4. **Compiling after every file edit.** Correct instinct, wrong cadence at 30–90 s per run. Batch and gate once.
5. **Loading both sim and Unity context "to be safe."** 4× token cost for no benefit; the handoff structs exist so this isn't needed.
6. **Tuning a balance constant to fix a failing balance test.** Circular. Ask.
7. **Judging Unity test results by log scraping.** Parse the XML.

---

## 12. Verify before trusting

Written from recall, needs checking against current documentation before use:

| Item | Confidence | Check |
|---|---|---|
| `dotnet build` / `test` commands, GitHub Actions YAML | High — stable | — |
| Repo layout, session scoping, logging design | High — design choices, not APIs | — |
| Unity batch-mode flags (`-batchmode -nographics -executeMethod -runTests -testResults -testPlatform`) | Medium | Unity 6.3 command-line arguments docs |
| `-runTests` exit codes (0/2/3) and the `-quit` interaction | Medium — the `-quit` trap is real, exact codes less certain | Unity Test Framework docs |
| `CompilationPipeline` API in §6 | Medium — has moved between versions | Unity 6.3 scripting API |
| CLAUDE.md discovery, nesting, precedence | Medium | Current Claude Code docs |
| Skill frontmatter field names | Medium | Current Claude Code docs |
| MCP server capabilities | Unknown — depends which one | Your server's docs |

I'd rather flag these than have you discover a wrong flag at the point you're trying to unblock a build.

---

## 13. Folding into the tech doc

New actions:

| # | Action | Owner |
|---|---|---|
| A33 | Create `CLAUDE.md` (root + 2 scoped) and the five skills; validate the flagged commands in §12 | Eng |
| A34 | `BuildVerify.CompileCheck` editor script + `verify-unity.sh` with MCP-then-batch fallback | Eng |
| A35 | `ISimLog` + seed/day/slot/subsystem convention; morale and nutrition breakdowns logged | Eng |
| A36 | Split CI into `sim.yml` (every push) and `unity.yml` (gated + nightly) | Eng |

New risk:

| # | Risk | Sev | Mitigation |
|---|---|---|---|
| R16 | Agent weakens a failing assertion instead of fixing the cause — silently reverting a balance property that took a whole system to achieve | **High** | Explicit CLAUDE.md prohibition; PR review on any assertion tolerance change; `balance/` PRs require solver diffs |

**Milestone placement:** A33, A34 and A36 belong **before M0**, not inside it. M0 is where the sim, the determinism test and the BalanceAssert suite get built — the verification loop needs to exist before the thing it verifies, or M0 gets built without a feedback loop and the guarantees are retrofitted. A35 lands with M0.
