# Presentation CPU performance

The fourth pass brings **all eight benchmark states above the original 30%
mean/P95 reduction target** in the final three paired trials. Dense combat
improves by 32.4%-33.3% in mean and 33.4%-34.1% in P95 versus the initial
presentation implementation. The completed cooldown optimization is preserved:
warmed cooldown captures remain zero and pixels remain identical.

## Fourth pass: dense combat and submission overhead

The baseline for incremental gains is `2dbac57`; cumulative gains use the same
original presentation source snapshot as earlier passes. No simulation code,
source artwork, shader, scene or existing asset GUID changes in this pass.

- GPU geometry is still generated and uploaded every presentation. Only the
  separate `ObjectSprite` diagnostic corner/UV/color expansion is deferred until
  a diagnostic getter is read. Source identity, blend/clip/effect metadata remain
  immediate, as does the independent live `SpriteHierarchySprite` Inspector.
  Each source snapshots six affine coefficients and its logical parameters.
  The same three arrays are reused; diagnostic arrays are borrowed until the next
  source submission, and cross-frame snapshots must copy them. Existing consumers
  follow that lifetime contract. Original float-expression order is retained.
- Completed polygons write their five shared attribute buffers in one block,
  instead of passing large repeated attribute values through one call per vertex.
  Capacity checks, Float32 data, fan indices and the 65532-vertex UInt16 limit stay
  intact. The immediate renderer remains the independent geometry/pixel oracle.
- Numeric geometry now writes a compact prefix with offsets accumulated from
  actual polygon counts. Zero-vertex regions do not advance the prefix. After
  synchronous Managed/Burst execution, bounded result and valid vertex prefixes
  are copied once to managed mirrors, avoiding per-vertex native safety reads
  during mesh assembly. Owner changes, 1024-region chunks and failure cleanup
  retain the original ordering and lifecycle.
- Sampling resolves combat-body ownership once per source draw while retaining
  case-insensitive names and attachment isolation. Ordinary zombies bypass only
  equipment rules that always return true/null; missing-arm/head rules remain
  live, and ModeHead 27/31 plant-head helms and catapult overrides are preserved.
  Special equipment rules continue through their original implementations.
- Immutable source frame elements and matrix parameters use read-only references
  between sampling/submission helpers. Blended current poses, deferred commands,
  numeric region inputs, Inspector state and diagnostic records still take owned
  value snapshots before a caller can change state. No animation rate or update
  frequency is reduced.
- Stable imitation-card inner captures now use the existing static interface
  cache with resolved target ID, grayscale, Rect and inherited color in their
  signature. The outer Imitater cooldown mask remains uncached: base/mask color
  changes still invalidate and rewrite the shared inner surface as before.
  Stable no-mask cards reuse pixels; prices stay outside the capture and live.

### Memory tradeoff

The numeric arena retains about **204 KiB** of additional managed numeric mirrors,
with unchanged native capacity. Diagnostic snapshots add approximately **72 bytes
per pooled source slot** (about 110 KiB at 1563 slots), bounded by the existing
source pool. The three diagnostic arrays keep their original allocation policy
and identity. This is a CPU/memory tradeoff, not a zero-memory-growth claim.
No new warmed per-frame managed allocations are observed.

### Final paired measurements

The same Unity 6000.3.17f1, Apple M4 / macOS 27.2 / Metal environment, 800x600
target and 640x480 screen viewport were used. Every state warms 120 frames and
measures 900 complete Present calls. Original, previous and current sources use
identical simulation/assets/fixtures; run order reverses in the second trial.
Deep Profile and phase markers are disabled. The following reductions are the
median of the three paired ratios, rather than ratios of independent medians.

| Workload | Cumulative mean reduction | Cumulative P95 reduction | Additional mean reduction | Additional P95 reduction |
| --- | ---: | ---: | ---: | ---: |
| combat / Normal | 33.3% | 33.4% | 12.9% | 12.7% |
| combat / Hit feedback | 32.5% | 33.9% | 12.4% | 12.6% |
| combat / Chilled | 32.4% | 34.1% | 12.8% | 12.2% |
| roof / Normal | 34.1% | 32.1% | 7.3% | 4.4% |
| roof / Cooldown masks | 61.7% | 61.1% | 9.5% | 12.7% |
| roof / Selected card | 40.4% | 41.6% | 10.2% | 15.8% |
| roof / Imitater | 38.8% | 38.3% | 19.1% | 18.9% |
| roof / Paused | 32.8% | 32.4% | 8.3% | 8.4% |

All 48 paired PNG comparisons (current against original and previous, eight
states, three trials) are byte-identical. Active source count, vertices, batches,
final population/time/match state match. Whole-Present allocation-event counts
remain unchanged. The cooldown state still performs **zero captures after
warmup**; the no-mask imitation state now also performs zero warmed captures.
The eight previously retained cooldown surfaces and their invalidation rules
remain unchanged.

Host activity increased late in the final run: the main Editor process became
CPU-active, and original normal-combat trial means ranged from about 3.04 to
3.99 ms. The paired raw ratios and run order are preserved, rather than dropping
slower runs or treating these absolute times as a Player budget. An earlier,
more stable complete three-trial sequence before the final imitation-card cache
already measured dense combat at 31.9%-32.4% mean and 31.0%-35.1% P95 cumulative
reduction. This remains Editor evidence, not a target-device guarantee.

Accepted final evidence is `.unity-runs/present-cpu-v4/final-{original,v3,v4}-{combat,roof}-{1,2,3}`
and `summary.json`; earlier `pilot-*`, `accepted-*`, `verified-*` and diagnostic
runs document the implementation stages. All captures/logs/arrays remain ignored.
Runtime sources are frozen during final timing; test-only evidence also records
battle random draw counts for advancing-state comparisons.

### Fourth-pass validation

New tests cover unobserved/latest diagnostic poses, original formula equality,
reused array identity, smaller source populations, warm zero allocation,
compact mixed four/five/zero-vertex offsets, shorter/empty segments, case-insensitive
combat bodies and same-named unrelated attachments. Equipment tests compare
10 mode-head values, five health/armor/shield combinations and four arm/head
states against the original pure rules, including explicit Wallnut/Tallnut crack
images. The imitation oracle independently uses the old uncached inner capture;
it checks no-mask/mask/no-mask transitions, target changes and actual source
readiness. Its fixture includes the production `PeaShooterSingle` resource name.

The final focused filters extend the previous suites with
`ObjectSpriteDiagnosticsTests|ReanimationCombatAssociationTests` and the updated
`SpriteGeometryBatchTests`, plus original catapult, bungee, digger, newspaper,
ladder, magnet, Zomboni and bobsled graphics. Final focused results: **73 EditMode passed; 268 PlayMode passed, nine unchanged
3100-event allocation failures, zero skipped**. The failure names and original
baseline are documented below; no assertion was relaxed for them. The suite now
includes the 21 newly added/extended sampling, geometry and imitation cases.
Final logs/XML are `.unity-runs/present-cpu-v4/final-{EditMode,PlayMode}.{log,xml}`.
Only validated code is retained; the unused compatibility-material gate experiment
showed no reliable benefit and was reverted.

An advancing comparison over 120 warm plus 900 deterministic 0.01-second steps
ends at identical simulation time 10.200138, 25 plants, 47 zombies, Playing state
and **866 battle random draws**. Final PNG bytes and main geometry are identical.
This checks animation/cache transitions and behavior, not repeated active-match
CPU performance. The benchmark records `FinalBattleRandomDrawCount` alongside
population and match state for this purpose.

The main Editor was observed running Play Mode and recording the Profiler during
late measurements; it was left running. Its Console included font diagnostics
and a Temp file-system timestamp check error, not a C# compilation error. This
pass does not claim a completely clear main Console. Final isolated compilation
and regression results are the validation evidence; Windows, target-platform
Players, other ABIs and the full active game's performance remain **unverified**.

## Third-pass history

The latest pass meets the 30% mean/P95 target for the cooldown-mask workload:
against the previous presentation commit `e348a94`, complete Present mean time
falls **49.4%**, and P95 falls **48.8%**. This is a macOS/Metal Editor result for
that workload; the broader all-scenario 30% target is not claimed.

## Third pass: immutable cooldown captures

Ordinary cooldown masks now retain the full original offscreen card image and
apply the current `topCrop` on every composite. The native mask tint is constant
64; cooldown progress changes the displayed height, not the captured artwork.
The change retains the original RenderTexture format, premultiplied composition,
source transforms, live prices, alpha, clipping and cooldown-state queries.
It avoids eight redundant native render requests per warmed cooldown frame.

`DrawNativeCachedClipped` is an internal interface-art entry point. It validates
caller content revision, full viewport Rect, inherited Color, renderer layout
version, grayscale and native RT backing. It uses the existing bounded surface
pool; return/reset clears static state, and lost RT backing is recreated. A key
cannot change dimensions after it was already submitted in the same frame;
between frames it can acquire matching dimensions. Generic captures invalidate
a static signature whenever they write new pixels. Actors, combat and additive
contexts use the live generic path, preserving same-frame hierarchy switches.
Unlike generic frozen actor captures, static interface hits do not scan the
whole actor hierarchy for each card.

The callback must represent immutable source pixels and have no required
per-draw side effects on other submitted surfaces. Source-art changes require
a new caller revision. Production ordinary cards use their stable plant-kind
revision and shipped immutable artwork. **Imitater masks retain the original
uncached path:** the nested imitation surface is also used by the earlier base
card in the same frame, so omitting its write changes existing warm-frame pixels.
The strengthened regression rejects the naive universal cache (maximum channel
error 13) and passes with this compatibility path. No simulation source or
serialized asset is changed in this pass.

### Accepted measurements

The same Unity 6000.3.17f1 / Apple M4 / macOS 27.2 / Metal environment and
800x600 target / 640x480 screen viewport were used. Both versions share the
same simulation, baked assets and benchmark fixture. Each state warms 120
frames then measures 900 complete Present calls, including deferred work and
uploads. Deep Profile and optional phase markers are disabled. The six runs
alternate `previous1/current1`, `current2/previous2`, `previous3/current3`.
Displayed times are the median trial values; reduction columns use the median
of the three paired ratios. Positive values mean reduced CPU time.

| Workload | Previous mean | Current mean | Paired mean reduction | Previous P95 | Current P95 | Paired P95 reduction |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| roof / Normal | 0.6036 | 0.6082 | -0.9% | 0.6434 | 0.6630 | -3.0% |
| roof / Cooldown masks | 1.2389 | 0.6273 | 49.4% | 1.2972 | 0.6649 | 48.8% |
| roof / Selected card | 0.6971 | 0.6077 | 12.9% | 0.7346 | 0.6459 | 12.0% |
| roof / Imitater | 0.6719 | 0.6712 | 0.1% | 0.7102 | 0.7105 | 0.3% |
| roof / Paused | 0.6211 | 0.6194 | -0.1% | 0.6648 | 0.6648 | -0.2% |
| combat / Normal | 2.3468 | 2.3583 | -0.2% | 2.4597 | 2.5573 | -2.8% |
| combat / Hit feedback | 2.3316 | 2.3393 | -0.4% | 2.4073 | 2.4182 | -0.7% |
| combat / Chilled | 2.3413 | 2.3458 | -0.3% | 2.4205 | 2.5078 | -0.2% |

All 24 paired screenshots are byte-identical, including the cooldown and selected
card states. Main submitted sprites, vertices, mesh batches, final populations
and simulation state match. Whole-Present GC.Alloc counts remain 1800 per 900-frame
roof state and 900 per combat state. In the eight-card cooldown state,
`CaptureRendersDuringMeasurement` is zero after warmup; the previous path made
eight captures per presentation (also observed by the untimed capture observer).
The existing eight color/depth surfaces remain pooled; this adds no raster cache
outside their existing lifetime and bounds. Other workloads show only small
variation, within the existing 5% regression threshold.

Accepted raw results, screenshots, commands, logs and the paired summary are in
`.unity-runs/present-cpu-v3/accepted-{v2,v3}-{roof,combat}-{1,2,3}` and `summary.json`.
Earlier `final-*` runs in that directory are preliminary, before the Imitater
compatibility guard, and are not used for these accepted ratios. These artifacts
are ignored local evidence and are not committed.

### Third-pass validation

Focused final results: **73 EditMode passed; 223 PlayMode passed, 9 pre-existing
allocation failures, 0 skipped**. The nine failures are the same 3100-event
`WarmAdventureUpdateAndPresentDoesNotAllocateManagedMemory` cases documented below.
No assertion was relaxed for them. The expanded PlayMode filter adds
`SeedCooldownCaptureTests|NativeStaticCaptureTests` to the second-pass filter.
For example:

```powershell
./Tools/Tests/Run-UnityTests.ps1 -Unity $unity -Platform PlayMode `
    -TestFilter 'SeedCooldownCaptureTests|NativeStaticCaptureTests|GamePresentationPerformanceTests|SeedBankLayoutTests|NativeThumbnailCacheTests|SpriteCaptureBatchReuseTests|CanvasPresentationTests'
```

The eleven new cases compare production packets with an independent uncached
oracle through full/partial/zero/restarted cooldowns, inherited tint/alpha,
position/size, outer transforms, repeated imitation targets and nested capture.
They also cover revision/grayscale changes, generic overwrites, pool reuse/reset,
RT loss, actor visibility and same-frame resize rejection. Direct warmed cache
composition observes zero GC.Alloc events while changing crop height without
additional captures. Full production/reference traversals report 128/256 events
respectively over 64 frames; the production path does not add allocation events.
Original board stress, layer/shadow, pool, dolphin, dancer, death-blend, gargantuar,
mesh, atlas, Canvas and feedback regressions remain in the final integration run.
The main project was refreshed in Unity 6000.3.17f1 with zero Console errors.
Windows/Player/other target ABI performance remains **unverified**.

## Second-pass history

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

## Second-pass measurements: 2026-11-15

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
