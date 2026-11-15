# Simulation ownership parity

Requires .NET SDK 9. Build the standalone AdventureTests project at the baseline
and current source revisions, then pass the two compiled assemblies and an ignored
output directory to this executable. Assemblies are loaded in separate contexts.
The tool runs 500 frame updates on five terrain fixtures and 200 on every catalogued
independent mode, including pause/resume, and compares ordered events plus all
baseline-defined entity, effect, board and mode state, mowers, wave schedule, seed
cooldowns, frame accumulators, and the combat random snapshot. Frame intervals
include fractional, multi-tick and zero deltas. No depth cutoff silently skips
state; unsupported state types fail validation. Add `--traces-only` after the
output directory to omit performance measurements.
Fixtures deliberately inject actors; these are not public-action playthroughs.

```sh
dotnet run --project Tools/Tests/SimulationParityTests/SimulationParityTests.csproj -c Release -- BASELINE_DLL CURRENT_DLL .unity-runs/simulation-parity
```

Tick measurements use 30 plants/10 zombies and 45 plants/100 or 300 zombies.
Fixtures keep plants and targets alive with explicit high health and disable
scheduled spawns. Each implementation warms 200 native updates and measures 1000
individual updates. Five paired runs alternate execution order; the report gives
the median of run medians and the median of run P95 values. Reflection and state
serialization are outside the measured interval. Allocation totals include gameplay
spawns and effects; they are not a steady-state zero-allocation claim. This tool
neither starts Unity nor verifies rendered pixels, audio, or target platforms.

## Real-scene smoke Player

`Unity/Editor/SimulationSmokeBuild.cs` is an optional Unity-only test driver, excluded
from this .NET project. Copy it into `Assets/Editor` of an isolated `.unity-runs`
project containing the runtime assets and generated atlases, then use Unity
6000.3.17f1 with `-executeMethod PlantsVsZombies.Tests.Editor.SimulationSmokeBuild.Build`
and `-pvz-simulation-player <ignored-output.app>`. It builds the existing Lawn scene
for macOS Mono and refuses to run against the main project.

Launch that Player with `-pvz-adventure-smoke -pvz-smoke-output <ignored-directory>`
to exercise the existing campaign/input/screenshot smoke fixture. The switch uses
temporary profiles. A build alone does not verify the Player, pixels, other
platforms, IL2CPP, or real OS input.
