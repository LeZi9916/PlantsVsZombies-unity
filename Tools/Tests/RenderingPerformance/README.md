# Checkpoint rendering performance

Use the exact Unity version in `ProjectSettings/ProjectVersion.txt`. These opt-in
measurements load a user-supplied checkpoint read-only, validate it through the
production checkpoint store, and keep all restored saves and output outside
tracked assets. Never commit checkpoint bytes, screenshots, or generated reports.

Set these environment variables before running
`CheckpointRenderingBenchmarkTests.RecordsExternalCheckpoint` in the PlayMode
test assembly:

| Variable | Meaning |
| --- | --- |
| `PVZ_RENDER_BENCHMARK_CHECKPOINT` | Absolute path to the external checkpoint. |
| `PVZ_RENDER_BENCHMARK_OUTPUT` | Absolute path to a new ignored output directory. |
| `PVZ_BOARD_STRESS_ASSETS` | Complete original runtime resource root. |
| `PVZ_RENDER_BENCHMARK_NEXT_STAGE` | `1` continues a saved cleared stage using its saved seed bank. |
| `PVZ_RENDER_BENCHMARK_TICKS` | Native 0.01-second updates before warm-up; default `0`. |
| `PVZ_RENDER_BENCHMARK_ADVANCE` | `1` runs one additional native tick per measured frame. |
| `PVZ_RENDER_BENCHMARK_FRAMES` | Measured frames, default `900`. |
| `PVZ_RENDER_BACKEND` | `Auto`, `Mesh`, or `Instanced`; absent interfaces on older revisions use their original renderer. |
| `PVZ_RENDER_USE_BAKED` | `1` uses the isolated project's production atlas/catalog and packaged texts. |
| `PVZ_RENDER_PROFILE_ALLOCATIONS` | `1` records raw allocation callstacks for separate diagnosis; do not use its CPU timings for acceptance. |

The fixture warms presentation for 120 frames with simulation held at the exact
requested offset. It records all measured frames, per-scope `GC.Alloc` event
counts, whole-frame GC bytes, simulation and presentation CPU time, draw counters,
model population, elapsed time, random-stream position, final model hashes and a
PNG. Primitive renderer counters use delegates bound before warm-up, including
instance draws, prefix uploads/bytes, retained GPU bytes, prepared instance slots,
fallback polygons, and cumulative native buffer creations. Pool snapshots bracket
the complete measured window. Older revisions explicitly report unavailable
counters. Whole-frame bytes include Test Runner and must not be advertised as Player
gameplay allocation. A deliberate allocation verifies both scoped recorders.
Missing original assets skip this opt-in test; a skip is not verification.

For the supplied survival checkpoint, compare the original cleared stage, the
next stage at 6000 ticks with advancement, and the next stage at 12000 ticks held
fixed. Use three baseline/candidate pairs in AB, BA, AB order. Keep machine,
graphics API, target resolution, artwork, and simulation tick counts identical.
Do not exclude allocating or slow event frames. Record full Player observations
separately from these deterministic traversal measurements.

```powershell
./Tools/Tests/Run-UnityTests.ps1 -Platform PlayMode -TestFilter CheckpointRenderingBenchmarkTests -IncludeRuntimeAssets
./Tools/Tests/RenderingPerformance/Run-CheckpointBenchmarks.ps1 -BaselineProject .unity-runs/<baseline>/p -CandidateProject .unity-runs/<candidate>/p -Checkpoint <read-only-save> -Output .unity-runs/perf/paired
python -B Tools/Tests/RenderingPerformance/CompareBenchmarks.py --baseline .unity-runs/perf/before --candidate .unity-runs/perf/after --output .unity-runs/perf/comparison.json
python -B -m unittest discover -s Tools/Tests/RenderingPerformance -p 'test_*.py' -v
```

`CompareBenchmarks.py` rejects differing authoritative model states and PNGs,
and reports unfiltered mean, nearest-rank P95/P99, allocation events and draw-call
changes. Zero allocations are an explicit reported result, not an assumed
property of a warmed or compiled benchmark. GPU frame timings are advisory;
use recorded GPU Profiler samples to distinguish rendering cost from frame pacing.

The paired runner uses three sequential AB/BA/AB pairs at each requested starting
point and refuses to overwrite a completed observation. Supply distinct, already
prepared isolated baseline/candidate snapshots; it updates only the shared test
fixture. It records source hashes and validates the immutable checkpoint after
every run. Allocation-callstack capture is a separate opt-in diagnosis, never a
formal timing source. Keep other Unity builds, graphics tests, and Player runs idle
while collecting paired measurements. The comparator reports target booleans and
resource growth without dropping any frame; its default exit code checks replay
and exact PNG identity. Editor hierarchy/name costs remain included and cannot
stand in for Player allocation acceptance.

`-IncludeRuntimeAssets` copies both StreamingAssets and generated resources into
the isolated project. Original pool-water decoding still needs the physical
source images; an atlas-only copy is not a faithful complete-board benchmark.
