---
name: sim-verify
description: Build and test the pure C# simulation library. Use after ANY change to src/OffTheGrid.Sim, src/LastOut.Data, or src/OffTheGrid.Tests. Fast (~7s) — run it freely, and always before reporting a sim task complete. Does not require Unity.
---

# Verify the simulation

    ./tools/verify-sim.sh

Runs:
- `dotnet build src/OffTheGrid.Sim -c Release -warnaserror`
- `dotnet test src/OffTheGrid.Tests -c Release --logger "console;verbosity=minimal" --no-build`
- Checks that no `using UnityEngine` appears in src/OffTheGrid.Sim/

## Interpreting results

**Build error** → Fix it. Warnings are errors here; do not suppress.

**Test failure** → Read the assertion. Do NOT adjust the expected value to match actual unless the change was intentional and stated in the task.

**Check failure** → A UnityEngine reference leaked into the sim. Remove it; use Int2/Float2 or add a sim-local type. Never satisfy this check by editing the verification itself.

This is the most serious failure in the repo. The Sim boundary is what keeps agent iteration fast.
