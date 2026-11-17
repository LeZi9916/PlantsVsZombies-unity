# Ordered instancing and prepared gameplay validation

## macOS continuation on 2026-11-17

The continuation uses Unity **7000.0.0a16**, macOS / Apple M4 / Metal. The original
read-only checkpoint still has SHA-256
`2D2A44D986C0D2EF9947B55463D6DB30131F79CC8D4785E312D5AC890B0D2848`.
The earlier Windows checkpoint record below is historical; its immediate-stop
notice does not describe the resumed work.

**Acceptance remains incomplete.** All three minimal sequential comparisons have
identical authoritative payloads and exact PNG bytes. Scoped simulation allocations
are zero in the deterministic 900-tick battle comparison. A live Player still has
small Update allocations, so a complete zero-allocation gameplay claim is rejected.
Windows timing and its independent graphics/backend matrix remain unverified on
this Mac. No original zero-allocation or correctness assertion was relaxed.

| Starting point | Mesh sprite draws | Ordered draws | Reduction | Candidate Present mean / P95 / P99 (ms) |
| --- | ---: | ---: | ---: | --- |
| Saved cleared board | 185 | 13 | 92.97% | 0.510 / 0.568 / 0.587 |
| Continue, advance 6000 ticks, measure 900 ticks | 421.31 | 17.95 | 95.74% | 1.626 / 1.867 / 2.040 |
| Continue, advance 12000 ticks, frozen | 451 | 18 | 96.01% | 1.223 / 1.318 / 1.396 |

These are one baseline/candidate pair per point, with 120 warm frames and 900
unfiltered measured traversals. Deep Profile and allocation stacks are disabled.
The baseline is the immutable `c1271aa` source snapshot with the independent Mesh
backend selected, not a claim that a new full Windows AB/BA/AB matrix was completed.
Both frozen candidate windows have zero scoped managed bytes. The advancing
Editor candidate retains 29,576 presentation bytes, including inspection hierarchy
naming and node work. All measured model-pool construction and miss deltas are
zero. Whole-frame allocation events remain in every raw observation.

### Completed continuation changes

- Loading prepares painter descriptors, immutable native property blocks and owned
  submission materials. First-use run/material/binding creations and misses are
  sampled independently in the Editor benchmark and opt-in Player replay.
- Simulation capacity incorporates selected cards, generated adjacent waves,
  backup dancers, thrown imps, Redeye normalization, concrete behavior/state types
  and observed high water. Targets add 25% headroom and round to powers of two;
  retained inactive records are bounded at 8192 per concrete type. Sound capacity
  separately protects still-owned pending history and its burst/grace reserve.
- Existing checkpoint roots and linked owners are adopted incrementally before
  gameplay. Production birth, damage, death, corpse release, planting and delayed
  return are rehearsed on an independently restored model. Neither live checkpoint
  bytes nor the formal random continuation changes.
- Vehicle particles, reflected basketball effects, cob shells, crushed pictures,
  seed-ready flashes, Blover state and dancer state use the existing typed lifetime
  pools. Crushed pictures retain their removed plant until final release. Public
  retained event snapshots remain independent; synchronous production board/wave
  notifications use value payloads.
- Impact Foley results use two inline cue values; row gates no longer capture
  predicates. Case-insensitive death-clip lookup no longer concatenates a key.
- Preparation covers active and idle pose containers, full definition clips,
  damage/equipment/arm/head images, particle sheet cells and unconditional fallback
  curves. Optional track absence is preserved, including skew and launch-angle
  branches that affect decorative random consumption.
- Exact fractional UV keys consume prepared bounded cache records and region
  arrays. Native capture source slots, standard 500x460 surfaces, compatibility
  meshes and their buffers are prepared before combat. Mesh and instance uploads
  use persistent native CPU staging and explicitly dispose it with their owner.
- Loading-only capture pools retire destroyed page material bindings, so restoring
  the same model no longer accumulates six page materials per cycle. The 45-second
  explicit reentry diagnostic keeps material count at 1124 across three restores.
  This forced-restore diagnostic is labeled separately from natural gameplay stress.
- The board wallet reads committed scalar balances without copying account
  collections, and formats its visible text only when content changes. Known
  survival flag captions and seed-ready particle owners are prepared separately.

### Current verification and remaining limits

- Focused instancing/capture GPU suite: **29 passed** on Metal, including the new
  single-work-unit binding preparation and first-use prepared Mesh buffer checks.
- Focused region/particle/active-pose/account preparation: **43 passed**, including
  1000 first-use exact UV API scopes with eviction and zero allocation, preserved
  optional particle tracks, and active actor pose preservation during preparation.
- Standalone CheckpointTests pass, including the original private save schema,
  complete reset enumeration, production pool ownership, detached lifecycle
  rehearsal, immutable payloads and random continuation. Comparator tests: **6 pass**.
- Architecture comparison: **211 baseline / 207 candidate violations, zero new**.
  Remaining failures are pre-existing LF files in this checkout; unrelated files
  were not reformatted. `git diff --check` passes. The opt-in original money-Foley
  check fails identically on the unchanged baseline and candidate on this Mac
  (`Silver and gold use the original pitched Coin Foley sample`); its assertion
  remains unchanged.
- A natural **300.017-second / 17,528-frame** CoreCLR Player stress run completed
  two restores. Model pool construction/misses, meshes, structured buffers, capture
  textures and pose container counts remained stable. Its ready-frame Present
  mean/P95 was 3.727/4.886 ms and Update mean/P95 was 0.552/0.806 ms. This earlier
  observation retained 1,324,193 whole-frame ready-phase GC bytes and preceded the
  final active-pose/material/native-staging fixes; it is **rejected for zero GC**.
- The subsequent short lifetime diagnostic has zero ready-phase Present allocation
  and stable native material count, but Update still retains **2064 bytes** in its
  ready-phase samples. It is not the final five-minute acceptance window.
- Allocation recorders have deliberate 32 KiB positive controls and bounded sample
  overflow checks. Player Update and Present scopes are recorded separately from
  complete-frame engine/replay allocations; no allocating frame is deleted.
- A prior macOS GPU CustomSampler used mismatched CPU/command-buffer spans and
  generated native log allocations. That observation is rejected and preserved.
  The helper now reports FrameTimingManager whole-frame GPU data when available,
  otherwise -1. It does not claim isolated scene GPU timestamps.
- The replay target is 1920x1080. The macOS batch Player has a 640x480 logical
  window; both sizes are reported. Offscreen rendering is not a swapchain 60 FPS
  guarantee. CPU results are diagnostic for this host, not Windows performance
  acceptance. Traditional Mono, IL2CPP, Windows Vulkan/D3D11, Linux, mobile, other
  ABIs and hardware remain unverified for the continuation.

Ignored current evidence is under `.unity-runs/performance-continuation`,
`.unity-runs/performance-continuation-candidate`, and
`.unity-runs/performance-continuation-player`. Original artwork, private saves,
raw profiles, screenshots, generated assets and Players remain uncommitted.

### Commands

```sh
# SDK bundled with the pinned Unity Editor; standalone test project only.
DOTNET_ROLL_FORWARD=Major <Unity-DotNetSdk>/dotnet run --project Tools/Tests/CheckpointTests/CheckpointTests.csproj -c Release
python3 -B -m unittest discover -s Tools/Tests/RenderingPerformance -p 'test_*.py' -v
```

The focused Unity test filters are `SpriteInstancingTests`,
`PlanternParticleTests;ReanimationBasisCacheTests;SpriteRegionGeometryPolicyTests;SpriteAtlasProviderTests;PacketUpgradeAccountTests.BoardWalletReadsCommittedValueWithoutAllocatingSnapshots`.
They run through `Tools/Tests/Run-UnityTests.ps1` or the same pinned Editor command
line in isolated `.unity-runs` copies. Existing full-suite baseline failures are
not hidden or claimed fixed by these focused results.

## Historical Windows implementation checkpoint

Validation date: 2026-11-17. Use Unity **7000.0.0a16** throughout. Historical
presentation and simulation reports remain records of their original revisions.

**Status: implementation checkpoint, acceptance incomplete.** The user requested
an immediate stop and commit. All task-owned tests and Player runs were stopped;
the existing user Editors were left open. No further verification was started.
The last fully exercised runtime revision is `ea4b0c8`. The subsequent incremental
run/property-block/material preparation changes in `ObjectSpriteRenderer` and
`SpriteInstancingRenderer` are saved as unfinished work: they have not been compiled
or tested. The implemented contracts below describe intended/current behavior and
must not be read as a claim that the complete performance plan has passed.

## Open work and unmet acceptance

1. **Dynamic event allocation remains.** The first 900-tick battle pair retained
   exact model hashes and pixels, but the candidate allocated 177,384 bytes in
   simulation (1,041 allocation events, 75 frames) and 290,800 bytes in presentation
   (3,218 events, 98 frames). Presentation includes Editor-only naming work: 61
   actor label misses. It also created three native submission bindings and 300
   pooled records during measurement. These allocating/growing frames remain in
   the evidence; the window is rejected for zero-allocation acceptance.
2. **Preparation is incomplete.** Model capacities still use fixed floors and
   margins; stage-wave concurrency, summons, historical high water, twenty-five
   percent headroom and power-of-two rounding are not unified across all pools.
   Production preparation does not yet rehearse a complete detached
   birth/hit/death/corpse/return lifecycle. Per-container growth/miss/high-water
   diagnostics also remain incomplete. The saved submission-preparation patch
   addresses only one identified source and still needs implementation review
   and regression coverage.
3. **CPU timing is not accepted.** In that Editor battle pair, Present mean/P95
   changed from 19.13/31.62 ms to 14.41/25.34 ms. This does not meet the requested
   10/14 ms thresholds in that environment and is not a Windows Player result.
   Business-thread P95 and stable on-screen 60 FPS remain unverified.
4. **The final matrix is unfinished.** Nine of eighteen sequential Editor runs
   completed before stopping; all three cleared-point pairs matched pixels and
   model hashes. The 6000-tick advancing case is partial and the 12000-tick frozen
   final pairs were not run. No final four-combination Player AB/BA/AB matrix,
   live 900-frame acceptance or five-minute stress loops were started. The existing
   short CoreCLR/IL2CPP and D3D11/Vulkan probes establish availability only and
   precede the latest source changes. GPU timestamps and other platforms remain
   unverified.
5. **Existing suite failures remain visible.** Final complete EditMode passed
   1613/1613. Complete PlayMode recorded 570 passes, 61 failures and three skips;
   its single new order-dependent binding test was corrected and passed an
   isolated follow-up, leaving 60 failures also present in the unchanged baseline.
   The baseline had 76 failures; nine allocation regressions and seven cooldown
   fixture/signature/oracle failures were resolved. No original assertion was
   weakened to claim a clean full suite.

The rejected battle pair nevertheless reduced mean submitted scene-sprite draws
from 421.31 to 17.95 (95.74%), with exact model and pixel agreement. Cleared-point
draws changed from 185 to 13; all three cleared candidate windows had zero scoped
allocation and no observed prepared-resource growth. These are measured partial
results, not completion of the full plan. Source checkpoint SHA-256 remained
unchanged. Raw evidence and the preparation audit are retained under
`.unity-runs/performance-implementation`; the interrupted formal set is marked
`paired-final/REJECTED.json`.

## Implementation contracts

Scene sprites prefer `Graphics.RenderMeshPrimitives`. A run binds up to eight
existing textures in first-use order without moving any transparent part across
another part. CPU/Burst polygon clipping, original depth, feedback blending and
the shared twelve-vertex fan are preserved. The instance layout is exactly 64
bytes; vertex data is `float4(x,y,u,v)`. Each 8192-instance chunk reserves 2 MiB
of structured GPU data. Eight chunks cap this data at 16 MiB; CPU staging, atlases,
materials, template geometry, native captures and the Mesh fallback are additional.
Immutable native binding snapshots are reused after their Unity frame expires.
Different GPU data in a repeated same-frame presentation selects complete-scene
Mesh rendering. Last-owner teardown uses eight fixed retired-session slots until
later-frame context completion, normally retaining one retiring session. The
eight-slot structured-data bound across exceptional session churn is 128 MiB;
admission stops when all slots are occupied. Ordinary frames retain no retired
sessions. There is a separate 8192-snapshot budget for instance property blocks
and owned materials, independent of the 8192 native Canvas/world material copies.
Vulkan material validation receives the same actual buffers as the property block;
buffer-identity caching avoids redundant writes without extra resource copies.

Unity 7's explicit draws and native Canvas do not interleave correctly using
`sortingOrder` alone. Painter spans share queues 3000 through 5000. Only bridged
scene/SceneView transparency on reserved layer 31 removes the `SortingLayer`
criterion and adds `CanvasOrder`; Canvas properties and pointer priorities remain
unchanged. Other layers, local captures and complete Mesh fallback retain the
original transparent criteria. Nondefault native sorting layers, more than 2001
spans or more than 8192 retained material copies select complete-scene fallback.
Material copies preserve fonts, stencil/pop instructions and source properties.

The Mesh path retains buffer capacity/layout and updates changed vertex/index
prefixes independently. Frame completion retires omitted/empty batches and
invalidates earlier same-frame explicit submissions. A retained complete scene
renews its explicit draws on later Unity frames without uploading identical data.
Local captures never seal a partially collected legacy scene. A portrait-only
empty scene preserves its native Canvas while retiring old scene sprites.

Value identities include session generation, role, entity and part/attachment.
Public string APIs remain compatible. Player creates no inspection actor, sprite
or attachment hierarchy; Editor retains pooled nodes and synchronous visibility
notifications. Exact local-pose hits require the same source track, frame/fraction,
blend version and blend fraction. Placement, equipment, color, visibility and
painter order remain live.

Simulation pools reset every mutable field and follow typed authoritative
references, including shared effects, corpses, Bungee cargo and sound history.
Records return only after two complete unreferenced update boundaries. Runtime
pool metadata is excluded from the persisted schema. Preparation preserves
checkpoint bytes and random continuation, uses a cooperative 2 ms budget, and
safely grows on a miss. An indivisible resource operation can exceed the budget.
Leaving the board releases stage page pins; returning prepares the same model
again. Board options retain those pins.

## Counter interpretation

- `InstanceUploadBytes` and upload count describe the latest submitted scene.
  `InstanceTotalUploadBytes` is cumulative for its rendering session.
- `InstanceBufferCreationCount` counts actual structured-buffer creations.
  `MeshBufferReconfigurationCount` counts vertex/index `Set*BufferParams` calls;
  `MeshCreationCount` counts owned Mesh objects. Their aggregate is diagnostic,
  not a measurement of opaque driver-internal native objects.
- Texture-bank, blend, painter and capacity boundaries can coincide. Their counts
  cannot be summed and interpreted as draw count minus one.
- Logical structured-buffer bytes are separate from Unity's whole graphics-driver
  allocation counter. Neither is a direct measurement of physical VRAM residency.
- Pool construction counters are cumulative within a world. Long runs record
  prepared and ending snapshots for each restored world, so growth is retained
  before the world is replaced.
- Whole-frame GC bytes include engine, worker and diagnostic boundary activity.
  Scoped game allocation events/bytes are recorded separately. Every counter must
  pass a deliberate-allocation positive control; allocating frames are retained.

Live Player allocation call stacks identified a two-second 64-byte listener scan
in `DynamicMusicPlayer.EnsureOriginalAudioListener`. Scene-root and listener lists
now replace allocating object-array queries, preserving newly added, disabled,
destroyed, additive-scene and persistent-scene external listeners. The scan owns
one inactive persistent-scene anchor and reports capacity growth. Direct Player
binary logs could not be loaded in this Editor despite complete-session and closure
checks; the accepted stack evidence uses live connection and Editor `SaveProfile`.

## Correctness evidence

The unchanged source is the supplied mode-13 endless-pool checkpoint, stage 34
cleared, 69 plants and no zombies. Its SHA-256 is
`2D2A44D986C0D2EF9947B55463D6DB30131F79CC8D4785E312D5AC890B0D2848`.
Only isolated copies are restored. Continued stages use the saved seed bank and
production native 0.01-second updates, with no injected actors or extra random
calls. Authoritative payloads match at the saved point, 6000 ticks and 12000 ticks.

- Complete EditMode: **1613 passed, 0 failed, 0 skipped**. This includes page
  release/reentry, hierarchy, exact poses, source caches and pool ownership.
- Focused D3D11 GPU: **46 passed, 0 failed, 0 skipped**, including eight/nine
  textures, 8192/8193 instances, shrink/empty/capacity fallback, reversed UVs,
  fractional clipping, Canvas interleaving, positive Canvas order, masks/fonts,
  actual pointer presses and native water.
- Pure simulation comparison: **60 scenarios, 13,500 complete state observations,
  0 mismatches** against the unchanged baseline, covering five terrains and all
  55 catalogued independent modes. Fixture inspection supports both historical
  and current test-only ownership APIs; compared fields and assertions are unchanged.
- Standalone CheckpointTests, AdvancedPlantTests, LevelOneTests and
  SpawnFidelityTests pass. ArchitectureTests reports zero violations.
- The benchmark comparator's six standard-library tests retain slow/allocation
  event frames, detect first-frame growth, reject RNG drift and flag pixel changes.

Ignored raw evidence is under `.unity-runs/performance-implementation`,
`.unity-runs/447f00d5`, and `.unity-runs/mesh-canvas`. Private saves, screenshots,
Player binaries, caches and generated resources are not committed.

## Reproduction

```powershell
dotnet run --project Tools/Tests/CheckpointTests/CheckpointTests.csproj -c Release
dotnet run --project Tools/Tests/ArchitectureTests/ArchitectureTests.csproj -c Release
python -B -m unittest discover -s Tools/Tests/RenderingPerformance -p 'test_*.py' -v
./Tools/Tests/Run-UnityTests.ps1 -Platform All -IncludeRuntimeAssets
./Tools/Tests/RenderingPerformance/Run-CheckpointBenchmarks.ps1 -BaselineProject .unity-runs/<baseline>/p -CandidateProject .unity-runs/<candidate>/p -Checkpoint <private-read-only-save> -Output .unity-runs/perf/paired
./Tools/Tests/PerformanceReplay/Run-PerformanceReplay.ps1 -SourceProject . -Checkpoint <private-read-only-save> -WorkingRoot C:/pvz-replay-candidate -Label candidate -Backend CoreCLR -Graphics D3D11
```

The paired fixture uses 120 warm frames, 900 measured traversals, and AB/BA/AB
ordering at each of the three starting points. Deep Profile and allocation stacks
are disabled for timing; stacks are captured separately. Full runtime artwork and
generated Resources are required; pool water also requires physical source images.

The Player helper is compiled only with `PVZ_PERFORMANCE_REPLAY`. Its builder
requires an explicit isolated-project marker. ASCII build staging avoids Unity 7
command-line decoding problems; accepted evidence is copied back into
`.unity-runs/performance-implementation/player`. Normal builds contain no replay
controller. Player runs render the complete scene offscreen at 1920x1080, with
VSync 0 and target 60 only in the diagnostic helper. This measures Player work
and actual rendered pixels, not an on-screen swapchain FPS guarantee.

## Limits

CoreCLR and IL2CPP, D3D11 and Vulkan must be reported independently. This Unity 7
installation uses explicit CoreCLR; the legacy `Mono` wrapper spelling is an
alias, and traditional Mono remains unverified. A build or a nonempty screenshot
does not establish a GPU-time result. Unavailable GPU timestamps remain minus one.
Linux, macOS, Android, iOS, other ABIs and physical device performance require
their own builds and runtime checks.

Existing standalone ModeTests (`PC28 is jalapeno, not tall-nut`) and
AdventureTests (`umbrella intercepts the initialized carrier`) failures reproduce
on the unchanged baseline. Their assertions have not been relaxed.
