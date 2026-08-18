---
name: unity-test
description: Run Unity play-mode and edit-mode tests. Use after unity-compile passes and you want to verify game logic at the rendering layer. Slower than unity-compile (~120–180s). Do not run until unity-compile passes.
---

# Run Unity Tests

    cd unity/LastOut && unity -projectPath . -runTests -batchmode -logFile -

Or via the Editor:
1. Window > General > Test Runner
2. PlayMode and EditMode tabs
3. Run All

## Interpreting results

**Test failure** → Read the failure message. Fix it; do not weaken assertions.

**Timeout** → Unity is stuck. Check the log for import errors or infinite loops.

**Success** → The task is complete.

---

**Note:** This skill is a placeholder. Populate `unity/LastOut/Assets/Tests/` with actual test classes
once you start implementing the presentation layer. Edit-mode tests verify UI logic; PlayMode tests verify
the rendering of sim snapshots.
