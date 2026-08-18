# OFF THE GRID.Sim — Pure C# Game Logic

This is the core game simulation. It has ZERO UnityEngine dependencies.
It compiles and runs standalone on any .NET platform (Windows, macOS, Linux, iOS, Android).

## The sacred boundary

Every line in this folder must pass this check:

```bash
./tools/check-no-unity-refs.sh src/OffTheGrid.Sim
```

If you reference UnityEngine anywhere in this tree, the check fails and the build fails.
This is not a style preference — it is the only thing that keeps the ~7s compile loop fast.

When you need something that *feels* like it should be UnityEngine (Vector2, Random, etc.):
- `Vector2` / `Vector3` → use sim-local `Float2` / `Float3` (in OffTheGrid.Data.Sim)
- `Vector2Int` → use sim-local `Int2`
- `Random` → use `Rng.Stream("name")` for a PCG32 stream
- `Physics2D.Raycast` → build your own. Sim is simulation, not rendering.

## Code layout

```
OffTheGrid.Sim/
├── CLAUDE.md                      (you are here)
├── GameState.cs                   (immutable core snapshot)
├── GameCommand.cs                 (input + RNG results)
├── Simulation.cs                  (deterministic step function)
├── Attributes/
├── Body/
├── Food/
├── Map/
├── Weather/
├── CheckIn.cs
└── Logging/ISimLog.cs
```

## Types you will write

- **Immutable snapshots** (`readonly struct`): body state, inventory, map FOV, weather, etc.
  These are written once per slot and read for display.
- **Commands** (`sealed class` or `readonly struct`): player input, RNG results, weather events.
  These accumulate in a queue and drive the simulation forward.
- **Constants** (`static class`): all numbers live in OffTheGrid.Data, not here.

## Determinism is non-negotiable

Every call to `Rng.Stream("someName").Next()` must be:
- Pinned to a *named* stream (not a reused one)
- Documented in a comment if its order matters
- Represented in test save-file checksums (cross-device testing)

A "minor" RNG change that shifts draw order breaks every saved replay and test.
Do not do it.

## Testing

- Tests live in `../OffTheGrid.Tests/`, not here.
- Aim for ~50 unit tests per mechanic (see `outputs/02` §3.2 for the QA matrix).
- Use `BalanceAssert.*` for game-critical invariants. Never weaken them to make a test pass.
- Cross-device determinism: the test harness will run the same replay on multiple platforms
  and verify the checksum matches. If it doesn't, you have a float or RNG order problem.

## Do not

- Do not add `using UnityEngine` or any UnityEngine reference.
- Do not use LINQ in hot paths (per-slot attribute decay, per-frame food consumption).
- Do not call `DateTime.Now()` — use the sim's deterministic clock.
- Do not store state that cannot be serialized (file handles, delegates to UI code, etc.).
