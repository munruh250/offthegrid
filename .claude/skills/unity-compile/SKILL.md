---
name: unity-compile
description: Compile the Unity project and verify no build errors. Use after changes to unity/LastOut/Assets/Scripts or any C# changes that affect the presentation layer. Slower than sim-verify (~60–90s depending on import time). Run this gate before any Unity-side task is complete.
---

# Compile Unity

    cd unity/LastOut && unity -projectPath . -executeMethod BuildVerify.Compile -quit -batchmode

Or via the Editor:
1. Open `unity/LastOut/` in Unity Editor
2. Menu: Assets > Run Compiler Check
3. Console should print: ✓ Compile check passed

## Interpreting results

**Build error** → Fix it. The project must compile clean in batch mode.

**Timeout** → Unity initialization took too long. Retry; if it consistently fails, the project may have import errors.

**Success** → Next step is `unity-test`.

---

**Note:** This skill is a placeholder. When you set up the local Unity Editor MCP server,
replace this with actual batch-mode verification commands. The gate structure is defined in
`unity/LastOut/Assets/Editor/BuildVerify.cs` (empty stub as of now — populate it when you 
connect Unity to the agent workflow).
