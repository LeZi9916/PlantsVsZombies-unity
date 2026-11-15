# Simulation ownership refactor verification

The simulation now has a command facade, shared match state, mode runners,
ordered entity stores, query indexes, battle services, and plant/zombie capability
owners. The original 19,164-line `GameSimulation` is a 1,557-line composition root
and forwarding API. See `Assets/Scripts/Simulation/README.md` for update-order,
lifecycle, state ownership, and API migration contracts.

The legacy demo and the shared upgrade-plant capability still use their existing
species dispatch inside their domain collaborators. Species selection remains
explicit in factories and pure native rule functions. The facade no longer owns
species dispatch or actor construction. The changes do not introduce per-actor
Unity updates or plant/zombie object pools.

## Baseline and deterministic comparison

Baseline revision: `4e3dfc6`. An untouched source archive and compiled standalone
AdventureTests assembly were retained under `.unity-runs/simulation-refactor/baseline`.
Both assemblies were loaded in separate contexts by `SimulationParityTests`.

- Five terrain fixtures: 500 update calls each, with injected representative plants
  and all 25 non-boss zombie species.
- All 55 catalogued independent modes: 200 update calls each after explicit fixture
  admission into Playing. These short probes do not complete the modes.
- 13,500 state observations match across the two implementations. Input intervals
  cycle through `.003`, `.017`, `.035`, `.005`, `0`, and `.01` seconds and include
  pause/resume. These are frame update calls, including paused calls, rather than
  13,500 native combat ticks.
- Comparison includes baseline-defined entity/effect/board/mode state, mowers,
  wave schedule, seed cooldowns, frame accumulators, ordered game/wave events, and
  the complete combat random snapshot. Required-value aliases are excluded only
  when their underlying member exists; `RequiredSeeds` remains covered.
- Serialization follows reference paths without a depth cutoff and rejects
  uncovered state types. Reference cycles and callbacks are identified explicitly.

The existing public-action campaign bot was also run on both assemblies. The first
34 levels won with reward commits and checkpoint reloads. Both stopped at 4-5 with
identical per-level CSV records, including the failure. This is a baseline bot
limitation; the refactor does not claim a successful 50-level playthrough.

## CPU and allocation measurements

.NET 9, same host, fixed seed, five paired repetitions with alternating old/new
execution order. Each run warms 200 native ticks and measures 1,000 individual
ticks. Results below are the medians of run medians and run P95 values. Scheduled
spawns are disabled; explicit fixture health keeps planted units and targets alive.
Reflection and state recording are outside the timed region.

| Fixture | Median before / after | P95 before / after | Median reduction |
| --- | --- | --- | --- |
| 30 plants, 10 zombies | 0.0100 / 0.0098 ms | 0.0127 / 0.0125 ms | 2.0% |
| 45 plants, 100 zombies | 0.1329 / 0.1274 ms | 0.1490 / 0.1480 ms | 4.1% |
| 45 plants, 300 zombies | 0.4984 / 0.4344 ms | 0.5416 / 0.4717 ms | 12.8% |

The smaller differences are close to measurement variation. The dense fixture
shows about 13% lower median and P95 CPU time. Allocations are identical for each
paired workload: 224,120 / 848,120 / 1,538,120 bytes across five measured runs,
including gameplay effects. Separate warmed tests retain zero-allocation checks.
The lane-query regression verifies 20 ordered candidates instead of 100 total
actors without changing nearest-target or tie selection.

These pure .NET measurements do not establish Unity Player or target-device
performance. Concurrent presentation work has its own pinned-source report in
`Assets/Tests/PresentationPerformance.md`.

## Unity and Player results

All Unity validation used **6000.3.17f1**, isolated source projects and a real
macOS graphics device. Main scene assets, Player settings and generated root IDE
projects were not used as output.

| Run | Passed | Failed | Skipped |
| --- | ---: | ---: | ---: |
| Baseline EditMode | 1112 | 1 | 0 |
| Refactored simulation EditMode | 1124 | 1 | 0 |
| Baseline PlayMode with original resources | 412 | 9 | 7 |
| Refactored simulation PlayMode | 413 | 9 | 7 |
| Integration with the parallel presentation snapshot: EditMode | 1125 | 1 | 0 |
| Integration with the parallel presentation snapshot: PlayMode | 422 | 9 | 7 |

The final focused ownership suite additionally passes **13/13**, including a
production Update-only animation transition check added after the integrated run.

The existing EditMode failure is
`AdventureBoardViewTests.ExplosionNamesMatchEnumTextWithoutRepeatedFormatting`
(222 allocated bytes). The nine existing PlayMode failures are the
`GamePresentationPerformanceTests.WarmAdventureUpdateAndPresentDoesNotAllocateManagedMemory`
scenarios. They also fail in the baseline; none were weakened or skipped to make
this refactor pass. Seven opt-in/resource-dependent PlayMode cases remain skipped.

New regressions cover index invalidation, two-cell occupancy, layer and dynamic
eligibility, stable row order, ID replacement, reference membership, nested
snapshots, capability-owned state, read-only public state, frame remainder/stop
semantics, nested scratch leases, warmed indexes, dancer queries and actual
production drawing without combat-state mutation. Original numerical assertions
remain in place; explicit fixture synchronization replaces reliance on a renderer
query to repair manually altered animation state.

A real **macOS arm64 Mono Player** built from `Lawn.unity` completed the existing
adventure smoke: **74 host/campaign/input checks and 22 screenshots**, exit code 0.
All PNGs decode at 800x600 with more than 1% nonblack pixels. The fixture covers an
earned first battle, reward/persistence, terrain and mode interaction, imitation
pixels, and coin arrival. Screenshots are not a native-game pixel comparison.

The old smoke fixture failed before and after the refactor at its immediate vase
assertion. It now checks hammer admission, retention before the final tick, and
exactly one vase opening on completion. Its imitation check captures the actual
production draw without requiring a normal actor RenderTexture; its coin check
waits for native arrival before asserting exactly one credit. Gameplay timings
and drawing rules were not changed for these fixture repairs.

## Other checks and reproduction

Independent results include 83 advanced-plant assertions, 115 mode assertions,
60,579 wave assertions, 13,887 spawn assertions, 37,702 original RNG checks, and
823 level-one evidence plus 231 experimental integration assertions. Focused
reward-projectile, Fume-shroom, multi-head and coin-feedback regressions pass.
Existing standalone failures in the umbrella carrier, hypnotized dancer walking,
and submerged Starfruit fixtures reproduce on the baseline assembly.

The architecture scanner's positive/negative self-tests pass. Its repository-wide
scan still reports existing CRLF violations; this refactor introduces no new
source, asset-pair or GUID violations. Existing GUIDs are preserved and final
`git diff --check` passes.

Representative commands, from the repository root:

```sh
dotnet build Tools/Tests/AdventureTests/AdventureTests.csproj -c Release
dotnet run --project Tools/Tests/SimulationParityTests/SimulationParityTests.csproj -c Release -- BASELINE_DLL CURRENT_DLL .unity-runs/simulation-parity --traces-only
dotnet run --project Tools/Tests/ModeTests/ModeTests.csproj -c Release
dotnet run --project Tools/Tests/ArchitectureTests/ArchitectureTests.csproj -c Release -- --self-test
pwsh -NoProfile -File Tools/Tests/Run-UnityTests.ps1 -Unity UNITY_6000_3_17F1 -Platform EditMode
pwsh -NoProfile -File Tools/Tests/Run-UnityTests.ps1 -Unity UNITY_6000_3_17F1 -Platform PlayMode
```

Native-reference tests require the read-only original executable and documented
oracle dependencies. Original-artwork tests use `PVZ_BOARD_STRESS_ASSETS`. See
`Tools/Tests/SimulationParityTests/README.md` for the optional isolated Player
build driver. Local raw XML, logs, parity records, measurements, source hashes,
Player artifacts and the combined summary are under
`.unity-runs/simulation-refactor`; they are ignored verification artifacts.

Windows, Linux, Android, iOS, other ABIs, IL2CPP execution, and real OS
mouse/keyboard interaction remain **unverified**. The full suite is not all green
because the baseline failures remain visible.
