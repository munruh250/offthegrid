# OFF THE GRID — Device Matrix (A14) v0.1

*Resolves A14 from `03-discipline-reviews.md`, which was blocking A3 (cross-device determinism test in CI at M0).*

> **The key distinction this document makes:** the **determinism matrix** and the **performance matrix** are different lists chosen on different principles. Determinism needs *codegen diversity* — the smallest set that exercises every compiler path. Performance needs a *floor* — the weakest device that must hold frame rate. Conflating them produces a list that is simultaneously too long and missing the cases that matter.

---

## 1. Determinism matrix

What actually causes cross-device float divergence is not the device. It is the **codegen path**: which compiler, which architecture, which optimisation flags. Two different Snapdragon phones running the same IL2CPP build are, for determinism purposes, the same test.

The axes that matter:

| Axis | Values |
|---|---|
| Runtime | Mono (editor) vs **IL2CPP** (device builds) |
| Architecture | x86_64 vs **ARM64** |
| Toolchain | MSVC / Clang-Linux / **Clang-Android (NDK)** / Clang-iOS |

### 1.1 Confirmed matrix

| # | Target | Runtime | Arch | Covers | Status |
|---|---|---|---|---|---|
| D1 | GitHub Actions runner (Linux) | Mono | x86_64 | CI baseline; the reference checksum | ✅ Available now |
| D2 | Dev Mac (Apple Silicon) | Mono | ARM64 | Mono across architectures — isolates arch from runtime | ✅ Available now |
| D3 | **Android phone** | **IL2CPP** | ARM64 | NDK Clang codegen; the shipping path | ✅ **Confirmed for this project** |

**D1 vs D2 isolates architecture.** Same runtime, different arch. A divergence here is a pure float/arch problem.

**D2 vs D3 isolates the runtime and toolchain.** Same architecture, different compiler and optimisation regime. A divergence here is IL2CPP or NDK flags — historically where fast-math divergence shows up first, which is exactly what C6 in `03` worried about.

Three targets covering three independent axes is a genuine matrix, not a token gesture.

### 1.2 Known gap — iOS

**iOS IL2CPP (Clang-iOS) is not currently covered.** This is a real hole and should be recorded as such rather than quietly tolerated.

| Risk | Assessment |
|---|---|
| Likelihood of iOS-only divergence given Android passes | **Low.** Both are Clang-derived ARM64 IL2CPP targets with similar float behaviour. |
| Consequence if it happens | **High.** Saved replays and the determinism guarantee break on half the install base. |
| Cost to close | One iOS device, or a paid CI runner with a Mac image. |

**Recommendation:** proceed to M0 on D1–D3. The three-target matrix is enough to catch the *class* of bug C6 identified, and catching it on Android is nearly as informative as catching it on iOS. But **close this gap before M2**, when the device matrix also has to serve the performance floor and accessibility testing. Add it to the M2 entry criteria rather than leaving it as an open action that drifts.

> `[D-Q1]` Is a cloud device farm (Firebase Test Lab, BrowserStack) sufficient for the iOS determinism run, or does it need a physical device? A farm run is enough for a checksum comparison, which is all this test does. Probably yes, and it is cheap.

---

## 2. Performance floor — deferred to M2

Performance is a **different question with a different list**, and it is not blocking M0. Recording the principle so the decision is not re-litigated later:

- The floor is a **market decision**, not a technical one — how far down the install base do you support?
- `03` C19 correctly notes "iPhone SE 2nd gen / Snapdragon 7-series" is a floor statement, not a matrix.
- The URP 2D renderer and a slot-based turn structure mean this game is unlikely to be GPU-bound. The real perf risks are **map/FOV computation** and **rival simulation**, both CPU and both in the sim.
- Which means: **the perf floor mostly tests the sim, and the sim is the thing already covered by D1–D3.**

**Action:** revisit at M2 entry, alongside the accessibility Tier 1 work already scheduled there. Do not spend budget on a device list before there is a build worth measuring.

---

## 3. Effect on A3 and M0

A3 (cross-device determinism test in CI at M0) is **unblocked**. The M0 test can be built now:

1. Fix a seed and a starting state
2. Run 60 simulated days, recording per-slot state hashes via `ISimLog`
3. Compare final checksums across D1, D2, D3
4. Fail the build on any mismatch

D1 runs in CI on every push. D2 runs locally via `verify-sim.sh`. **D3 is the one that needs harness work** — an Android build that runs the replay headlessly and reports a checksum. That is genuinely new infrastructure and should be scoped explicitly rather than assumed.

> `[D-Q2]` How does the D3 checksum get back to CI? Options: manual run gated at milestone boundaries, a nightly job against a connected device, or a device-farm invocation. Manual-at-milestone is fine for M0 and costs nothing — but decide before the first time it is inconvenient, not after.

The pre-agreed decision rule from `03` D3 still applies: if the replay diverges, the quantisation-vs-fixed-point decision fires per tech doc §3.1. **Do not pre-emptively switch to fixed point.** Slot-boundary quantisation is the cheaper bet and the test exists to find out whether it holds.

---

*Status: A14 resolved for M0. iOS gap recorded and scheduled for M2. Performance floor deliberately deferred.*
