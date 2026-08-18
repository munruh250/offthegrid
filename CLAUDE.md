# LAST OUT

Mobile survival-contest game. Unity 6.3 LTS + URP 2D.
Design docs in `outputs/`. Read them only when the task requires design context.

## Architecture — the one rule that matters

`src/LastOut.Sim/` is a pure C# library with ZERO UnityEngine dependencies.
It is the game. Unity is a renderer and input layer.

- **NEVER** add a `UnityEngine` using/reference to LastOut.Sim. CI blocks it.
- **NEVER** put game logic in `unity/`. Presentation and input only.
- Sim exposes immutable snapshots + a command queue. The view never mutates sim state.
- Minigames return a normalised scalar 0..1. They do not compute outcomes.

## Session scoping — read this before opening files

Work on ONE assembly per session. Do not load sim and Unity code together.
Cross-boundary work: split into two sessions, use `MinigameContext` /
`MinigameResult` / `ResolutionResult` as the handoff contract.

## Verification — non-negotiable

- Sim changes:    run the `sim-verify` skill (~7s). Always. Before claiming done.
- Unity changes:  run `unity-compile`, then `unity-test`. Slow — batch your edits
                  and gate once, don't compile per file.
- Balance changes: run `balance-check`. A balance change without solver output is
                  not reviewable.

Never report a task complete without a passing verification run in the transcript.
If a gate fails, fix it — do not describe the failure and stop.

## Determinism — this breaks silently

- Use `Rng.Stream(name)` (PCG32). NEVER `System.Random` or `UnityEngine.Random`.
- No `DateTime.Now`, no unordered dictionary iteration, no float accumulation
  across slot boundaries without quantisation.
- Adding a new RNG consumer means adding a NAMED STREAM, not reusing one.
  Reusing a stream shifts every downstream draw and breaks saved replays.

## Style

- C# 12, file-scoped namespaces, `sealed` by default.
- Sim types: readonly structs where practical. Avoid LINQ in per-slot paths.
- Sim-local `Int2`/`Float2` — not Vector2Int, which is a Unity type.
- No abbreviations in public names. `kcal` and `clo` are domain terms, keep them.
- Balance constants live in `LastOut.Data` tables, never inline in logic.

## Do not

- Do not add dependencies without asking.
- Do not change balance constants to make a test pass. Ask.
- Do not disable or weaken a failing assertion. Especially not
  `BalanceAssert.FastingBuildLosesTo()` — see `outputs/04` §7.3.

The last one matters more than it looks. `FastingBuildLosesTo` guards a property
that took a whole morale system to achieve. It is exactly the kind of assertion
an agent will "fix" by loosening the tolerance.
