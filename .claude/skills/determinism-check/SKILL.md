---
name: determinism-check
description: Verify that the same replay produces identical checksums on different .NET versions and platforms (Windows, macOS, Linux). Critical for saved-game integrity. Run after any change to Rng, GameCommand serialization, or attribute/food calculations.
---

# Check Determinism

    ./tools/determinism-check.sh

Runs the same replay 10 times with different platform/runtime configurations
and verifies that the `ISimLog` checksum matches every time.

Configurations tested:
- .NET 9.0 on macOS
- .NET 9.0 on Linux (via docker)
- net9.0-windows (if on Windows)

## Interpreting results

**Checksum mismatch** → A platform-specific float difference or RNG inconsistency. 
This breaks saved replays. Find it:
- Did you add a new `Rng.Stream()`? Ensure it's named and documented.
- Did you change order of RNG draws? This shifts every downstream result.
- Did you add a float accumulation (body degradation, calories consumed)? 
  Quantise it or track it as an int internally.

**Success** → Determinism is solid for this change.

---

**Note:** The determinism harness (tools/determinism-check.sh) is a skeleton. 
It should:
1. Pick a seed and a fixed starting state
2. Run the sim for 60 steps with that seed on each platform
3. Compare ISimLog checksums across all runs
4. Print pass/fail + timing for each config

See `outputs/02` §4 (Cross-Device Determinism) for the full spec.
