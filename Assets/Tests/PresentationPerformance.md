# Presentation CPU performance

The second optimization pass removes the previously reported dense-combat
regression in controlled, alternating-order measurements. Against the original
presentation implementation, dense-combat mean CPU time improves by 22.8%-23.6%
and roof mean CPU time by 18.2%-27.9%. Roof normal/paused P95 improves by 30.7%,
but the requested 30% improvement in both mean and P95 is **not achieved** across
all workloads. These are macOS Editor results, not Windows or Player claims.

## Cause of the earlier regression report

The first pass compared independent medians of baseline and optimized runs.
Dense-combat baseline means drifted from 3.01 to 4.02 ms, while optimized means
ranged from 2.83 to 3.60 ms. Pairwise first-pass comparisons actually improved in
two trials and regressed in one; the median paired reduction was approximately
6%. Taking separate medians discarded the pairing and reported a 4.6% regression.

A new diagnostic ABBA run in the same imported project, with phase markers enabled
and Deep Profile disabled, reproduced no regression: original normal combat was
3.134/3.170 ms versus first-pass 3.038/3.036 ms. Atlas-region preparation fell
from 0.233/0.250 ms to 0.123/0.112 ms. This identifies run drift and the aggregation
method as the source of the earlier negative conclusion, rather than evidence
of a stable code slowdown. The final results below use separate uninstrumented
trials and retain pairing and run order.

## Implementation

- Stable `SpriteImageBinding` handles retain resolved atlas metadata and page
  residency stamps. Case-insensitive external aliases remain compatible, while
  ordinary crop lookups use provider-local integer identities.
- Each handle now retains its most recent `SpriteRegionCacheEntry`, bypassing
  repeated crop-key dictionary lookups. Entries verify image ID, exact UV and
  geometry policy before use. The shared 512-entry LRU, page retirement and
  layout/storage invalidation remain active. Evicted slots cannot be reused
  through another handle's stale reference; disposal clears retained page arrays.
- Native card thumbnails sample time-zero source-image poses once, including
  attached heads, then replay with live placement, tint and composition. There
  is no extra raster texture; at most 64 pose keys are retained per renderer.
- Zombie body/shield feedback is calculated once per synchronous actor draw,
  with each track retaining its own alpha and dolphin water-track exclusions.
  Nested draws restore the complete outer feedback context.
- Actor hierarchy groups retain numeric track tables. Repeated tracks avoid
  composite-key hashing while retaining the original inspection dictionary,
  attachment separation, external visibility and retirement semantics. Negative
  and sparse indices outside the 4096-entry table bound use the original lookup.
- Pose blending uses renderer-local integer IDs assigned by exact ordinal track
  name. Cross-resource same-name blending, case-sensitive distinctions, duplicate
  name overwrite order, hidden poses and playback resets retain their semantics.
- Source tracks retain one exact rotation/scale basis; translation is updated on
  every draw, and any changed final blended rotation/scale recomputes the basis.
  Equal authored axes reuse identical sine/cosine results; skew remains independent.
- `SpriteMeshBatch` writes reusable attribute/index arrays and uploads only their
  populated prefixes. Full Float32 colors, original polygon order and UInt16
  index limits remain intact. A recent texture/material binding avoids repeated
  lookups within a batch and is cleared before the next batch's resource checks.
- Retained `CanvasBitmapGraphic` instances group consecutive glyphs on the same
  page, splitting at 1024 glyphs. Page transitions, clipping, overlapping glyphs
  and scene/Canvas ordering barriers remain intact; unchanged text stays clean.
- `PvZ.SeedBank`, `PvZ.Thumbnail`, `PvZ.BitmapText` and nonempty geometry segment
  counts supplement the existing markers. Nested marker durations overlap and
  must not be added; Canvas submission may flush earlier scene commands.

No simulation source, public gameplay API, save format, source artwork, shader,
serialized scene or existing asset GUID is changed by this presentation commit.
The GameSimulation refactor is the separate preceding commit `d053b8c`.

## Final measurements: 2026-11-15

Unity 6000.3.17f1; Apple M4; macOS 27.2; Metal; 800x600 target; 640x480 screen
viewport. Baked production assets are used. The original and current variants
share exactly the same frozen simulation source and benchmark fixture. Actual
measured simulation hashes are stored in
`.unity-runs/present-cpu-v2/measured-simulation-hashes.json`.

Each state warms 120 frames and records 900 complete Present calls. Three paired
trials run sequentially in order original1/current1, current2/original2,
original3/current3, with the other development session's heavy workloads stopped.
Deep Profile and optional phase markers are disabled for these final trials.
Each displayed time is the median of the three trial values. Reductions are the
median of the three *paired* ratios, computed independently for mean and P95.
Present includes all deferred geometry, commit and uploads.

| Workload | Original mean | Current mean | Paired mean reduction | Original P95 | Current P95 | Paired P95 reduction |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| roof / Normal | 0.8417 | 0.6153 | 26.6% | 0.9559 | 0.6790 | 30.7% |
| roof / Cooldown masks | 1.5158 | 1.2369 | 18.2% | 1.5975 | 1.2928 | 19.1% |
| roof / Selected card | 0.9328 | 0.7013 | 24.7% | 0.9801 | 0.7432 | 24.0% |
| roof / Imitater | 0.9057 | 0.6744 | 25.5% | 1.0171 | 0.7210 | 26.4% |
| roof / Paused | 0.8595 | 0.6219 | 27.9% | 0.9526 | 0.6622 | 30.7% |
| combat / Normal | 3.0665 | 2.3510 | 23.6% | 3.3995 | 2.4951 | 26.6% |
| combat / Hit feedback | 3.0514 | 2.3551 | 23.0% | 3.2323 | 2.6046 | 19.9% |
| combat / Chilled | 3.0397 | 2.3470 | 22.8% | 3.2886 | 2.5306 | 25.1% |

All 24 paired screenshots (eight states, three trials) match exactly, including
alpha, with maximum channel error 0. Submitted sprites, uploaded vertices,
mesh batches, final populations, simulation time and match state also match.
Scoped allocation-event counts are unchanged: 1800 per 900-frame roof state and
900 per combat state. These whole-Present workloads are not zero-allocation
claims; the warmed thumbnail and existing focused allocation checks remain
separate. Cold resource/cache creation is excluded from the warm CPU results.

The original Windows Editor capture remains useful for identifying call paths;
its deep-profiling absolute times are not this macOS uninstrumented baseline.
The first-pass measurements and experiments remain under `.unity-runs/present-cpu`;
accepted second-pass results are under `.unity-runs/present-cpu-v2/final-{original,v2}-{roof,combat}-{1,2,3}`.
The paired summary includes raw source paths and per-trial ratios. Generated
captures, atlases, logs and measurement data remain ignored local evidence.

## Reproduction

The existing `RenderingBenchmarkTests.RecordsVisibleBoardBaseline` accepts
`PVZ_RENDER_BENCHMARK_SCENARIO=roof` or `combat`. Roof uses level 41, seed 7529,
exactly 15 configured plants and eight cards (six native thumbnails, two packet
atlas images); the level's 25 preplaced pots are removed before population setup.
Roof states are 0 normal, 1 cooldown masks, 2 selected card, 3 Peashooter imitation,
4 paused. Combat uses 30 plants/50 zombies and states 0 normal, 1 hit feedback,
2 chilled. The output records deck, kinds, level, seed, source mode and device.

Prepare inactive isolated projects under `.unity-runs`, using identical
simulation sources, fixtures and copied production `Assets/StreamingAssets` and
`Assets/Generated`. The normal test isolation script omits those resources.
Freeze baseline presentation sources separately; `Run-UnityTests.ps1 -ReuseProject`
refreshes source, so use the Editor directly for a frozen historical baseline.
For example, in PowerShell:

```powershell
$unity = '/Applications/Unity/Hub/Editor/6000.3.17f1/Unity.app/Contents/MacOS/Unity'
$project = (Resolve-Path '.unity-runs/present-cpu-v2-after/p').Path
$output = Join-Path (Get-Location) '.unity-runs/present-cpu-v2/new-roof-trial'
New-Item -ItemType Directory -Force $output | Out-Null
$env:PVZ_BOARD_STRESS_ASSETS = (Resolve-Path 'Assets/StreamingAssets').Path
$env:PVZ_RENDER_USE_BAKED = '1'
$env:PVZ_RENDER_BENCHMARK_SCENARIO = 'roof'
$env:PVZ_RENDER_BENCHMARK_OUTPUT = $output
Remove-Item Env:PVZ_RENDER_BENCHMARK_STATES -ErrorAction SilentlyContinue
Remove-Item Env:PVZ_RENDER_BENCHMARK_ADVANCE -ErrorAction SilentlyContinue
Remove-Item Env:PVZ_RENDER_PROFILE -ErrorAction SilentlyContinue
& $unity -batchmode -projectPath $project -runTests -testPlatform PlayMode `
    -testFilter RenderingBenchmarkTests -testResults "$output/results.xml" `
    -logFile "$output/editor.log" -pvz-test-runner
```

Use fresh output directories and alternate baseline/current run order. Set
`PVZ_RENDER_BENCHMARK_ADVANCE=1` to include deterministic 0.01-second simulation
steps. Fixed-pose timing above does not establish active-match CPU savings.

## Validation

The focused EditMode filter is `SpriteAtlasProviderTests|SpriteHierarchy|Reanimation`.
It covers aliases, page retirement/fallback, direct-entry eviction/reuse, bounded
crop caches, disposed references, track-table growth/sparse indices, nested actor
scopes, exact affine bases and cross-resource pose identity/reset semantics.

The focused PlayMode filter includes `SpriteGeometryBatchTests`,
`ObjectSpriteRendererTests`, `ZombieFeedbackContextTests`,
`NativeThumbnailCacheTests`, `CanvasBitmapBatchTests`, `CanvasPresentationTests`,
`GamePresentationPerformanceTests`, `SeedBankLayoutTests`, baked/imported atlas
and capture reuse fixtures, plus original board/layer/shadow, pool, dolphin,
dancer, death-blend and gargantuar graphics. Run with the same Editor and a real
graphics device using `Tools/Tests/Run-UnityTests.ps1` and `-TestFilter`.

Final focused integration results: **73 EditMode passed; 212 PlayMode passed,
9 existing failures, 0 skipped**. The nine failures are the unchanged
`WarmAdventureUpdateAndPresentDoesNotAllocateManagedMemory` scenarios Combat,
Paused, Conveyor, DetachedArm, SnowTrail, Explosion, Corpse, FrozenCorpse and
CharredCorpse, each reporting 3100 GC.Alloc events as in the original baseline.
No assertion was relaxed for those failures. Logs and NUnit XML are
`.unity-runs/present-cpu-v2/final-{EditMode,PlayMode}.{log,xml}`.

Pixel checks include original assets and independent immediate-renderer oracles;
new cases cover exact group feedback/alpha, nested draw restoration, dolphin
water exclusions, array capacity growth/shrink, full-precision colors, atlas
LRU reassignment/disposal, track-table switching and source-name blend identity.
An additional advancing-simulation comparison (120 warm + 900 measured fixed
0.01-second updates) ends at the same 10.200138 simulation seconds, 25 plants,
47 zombies and Playing state, with identical final screenshot bytes and geometry.
It is a behavior/cache-transition check, not a repeated active-match timing claim.
Presentation GC.Alloc events were 8545 original versus 8627 current; evolving
workloads include newly initialized cache/resource state, so this result does
not claim allocation equivalence for event frames. Focused warmed allocation
regressions retain their original outcomes. The main project was refreshed in
Unity 6000.3.17f1 with zero Console errors (existing nullable warnings remain).

Windows source hardware, target-platform Players and other ABIs remain
**unverified** for this presentation change.
