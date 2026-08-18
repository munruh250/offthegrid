---
name: balance-check
description: Run the balance solver and output before/after diffs. Use after any change to balance constants in LastOut.Data, or when tuning the morale/food/shelter economy. The output is the evidence for a balance PR.
---

# Check Balance

    ./tools/parse-test-results.py --run-solver

Generates:
- `balance-report-before.txt` — The baseline
- `balance-report-after.txt` — After your change
- Diffs showing which metrics changed and by how much

## Interpreting results

**No changes** → Your edit didn't affect balance (it's in a constant or default that the solver doesn't exercise).

**Expected changes** → The solver reflects the change you made. Good. Include the diff in the PR body.

**Unexpected changes** → A side effect. Investigate; a balance change should be surgical. Do not merge if you can't explain every line of the diff.

**Solver failure** → The balance model is broken. Check that `BalanceAssert.*` still passes.

---

**Note:** The balance solver (tools/parse-test-results.py) is a placeholder. It should run
the determinism test harness multiple times with different RNG seeds and output aggregate
metrics: avg survival day, fasting-build loss rate, protein-ceiling bind rate, etc. 
See `outputs/04` §7 for the metrics that matter.
