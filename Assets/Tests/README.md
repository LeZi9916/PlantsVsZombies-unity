# Unity regression tests

Open the project in Unity **6000.3.17f1** and use Window > General > Test Runner.
The EditMode and PlayMode assemblies reference `PlantsVsZombies.Runtime` and are
enabled only with `UNITY_INCLUDE_TESTS`.

Run isolated tests from the repository root:

```powershell
./Tools/Tests/Run-UnityTests.ps1
./Tools/Tests/Run-UnityTests.ps1 -Platform PlayMode -TestFilter ObjectSpriteRendererTests
```

The script copies the source, test, shader, and pipeline assets into a unique
ignored `.unity-runs` project. It checks the exact editor version, runs with a
real graphics device, and rejects missing, skipped-only, or failed test results.
The main project's caches, scenes, assets, and Player settings are not used as
test output. The `-pvz-test-runner` argument suppresses game bootstrapping in the
isolated project so synthetic rendering tests do not need original game data.

An optional `-PackageCache <path>` copies an existing Unity package cache into
the isolated project before import. This can avoid Windows package extraction
rename failures; Unity still resolves the copied manifest and package hashes.
For a repeat run, `-ReuseProject .unity-runs/<run>/p` refreshes only the isolated
source snapshot and retains its package and import cache. Never run two Unity
processes against the same isolated directory concurrently.

EditMode tests cover the current and legacy loose resource layouts and load
the shipped Lawn scene to verify its migrated script binding. Pointer tests
exercise aspect-ratio mapping, letterbox rejection, modal capture, dragging,
closing consumption, and reopening without creating a GUI event. PlayMode
tests render synthetic textures through the project render pipeline and compare
colors, transparency, overlap order, transforms, object reuse, and disposal.
Canvas tests interleave artwork with scene meshes, clip transformed groups,
retire stale graphics, and retain editable text/focus across presentation frames.
These checks do not replace scene interaction or target-platform Player tests.

The optional original-artwork board stress fixture uses the complete production
presenter, a six-column planted lawn, repeated 100-zombie bursts, flag attachments,
wave notices, cooldown masks, and nested imitation captures. It compares each
plant's pixels with the empty lawn and keeps capture camera count independent of
actor count. Supply a read-only original runtime asset directory to enable it:

```powershell
$env:PVZ_BOARD_STRESS_ASSETS = (Resolve-Path Assets/StreamingAssets).Path
$env:PVZ_BOARD_STRESS_OUTPUT = Join-Path (Get-Location) '.unity-runs/board-stress/screenshots'
./Tools/Tests/Run-UnityTests.ps1 -Platform PlayMode -TestFilter OriginalBoardStressTests
```

The fixture skips when that asset directory is absent. It creates no saves and
does not modify the supplied original artwork or the main scene.
The same filter also checks original zombie shadow geometry and body origins
for day, night, small, frozen, chilled, and moving poses, and optionally saves
white-background captures for inspection against the native coordinate oracle.

Performance regressions use `GC.GetAllocatedBytesForCurrentThread` after warming
reusable buffers. `SimulationPoolingTests` exercises projectile/sun reuse, full
state reset, mutation-safe nested snapshots, and steady simulation queries.
`AdventureIntroPerformanceTests` checks cached chooser frames.
`ReanimationHotPathTests` checks clip/texture lookups, sampled track counts,
particle recycling, and multi-head cursor drawing. PlayMode tests cover pooled
audio admission/pause/completion, retained Canvas dirty state, direct/captured
zombie alpha and clipping, and reusable actor callbacks. The real `PvZGame.Update`
allocation test uses synthetic assets with audio and persistent progress loading
disabled; it does not measure a complete loaded Player.

`PvZGame` is a thin coordinator: audio, input routing, menus, the adventure board, independent
modes, the store, the garden, the Wisdom Tree and the other screens live in collaborator classes
that it creates on first use, so an inactive test host that never ran `Awake` still reaches working
instances. `GameCollaboratorTests` checks that creation, that the effects, music and Dave
components adopt their audio sources and follow later volume changes, and that named-member
lookup finds each member on the collaborator that declares it. Fixtures and the Player smoke
scripts reach non-public session state through `GameMemberReflection` (`Assets/Scripts/Diagnostics`),
which searches the coordinator and then each collaborator and rejects ambiguous names, so they do
not depend on which collaborator owns a field or method.

Audio prewarms 64 shared pitched voices and preserves the native eight-voice
Foley limit per category. Projectile impacts admit consecutive hits and reuse the
oldest same-category voice when saturated. Other full categories drop the new
sound while existing sounds finish. Simulation pools grow with concurrent demand and bound retained inactive
entries; new assets, changed labels, larger bursts, special feedback captures,
and startup can still allocate. Synthetic steady-state zero-allocation checks
are not a claim of zero allocation or a CPU-time target for an entire match.
Re-profile a loaded Player's chooser and combat on each target platform to
compare frame time with the original capture.

Original Puff-shroom graphics can be checked with `PVZ_BOARD_STRESS_ASSETS` set
to a read-only original runtime resource directory, then
`./Tools/Tests/Run-UnityTests.ps1 -Platform PlayMode -TestFilter OriginalPuffShroomTests`.
The tests call the production presenter with original day/night shadow and spore
artwork. They isolate visible shadow pixels, check the native muzzle, and compare
new and grown spore sizes. `PVZ_BOARD_STRESS_OUTPUT` optionally saves screenshots
to an explicitly selected ignored directory. Without the original assets these
opt-in graphics tests are skipped; a skipped run is not graphics verification.

Original feedback regressions use the same `PVZ_BOARD_STRESS_ASSETS` directory:

```powershell
./Tools/Tests/Run-UnityTests.ps1 -Platform EditMode -TestFilter 'AdventureAdviceRulesTests;AdventureGravePresentationTests;AdventureZombieRiseTests'
./Tools/Tests/Run-UnityTests.ps1 -Platform PlayMode -TestFilter 'OriginalPuffShroomTests;OriginalCoinFeedbackTests;OriginalCoinGraphicsTests;OriginalNewspaperTests;OriginalGravestoneTests;OriginalBattleTooltipTests;ResourceSpriteClippingTests'
```

These fixtures cover the first mushroom attack's original windup, currency pickup
samples and collection artwork, newspaper surprise and angry head artwork, grave
atlas selection and separate soil, preview suppression, and final-wave emergence
clipping. Synthetic sprite tests independently check particle atlas rows and top
and bottom cropping. Tooltip fixtures keep combat names and availability warnings
while reserving descriptions for the seed chooser. Audio checks decode original
non-silent PCM and inspect production source playback; they do not measure a
physical speaker or replace target-device listening checks.

Native motion regressions use the same original-resource environment variable.
`AdventureNativeMotionTests` covers pole-vault events and Tall-nut contact, the
PC pool-entry probes, snorkel phases and bank limits, and Threepeater velocity
decay. `OriginalNativeMotionTests` compares rise/reverse-dive pixels, checks the
native waterline and original splash artwork, and decodes/plays the vault and
water Foley. Run only those filters for changes to these paths. The motion
constants follow PC `541850`, `5421A0`, `54B5F0`, `548D40`, `549740`, `4752E0`,
and `47CCB0`; the mobile-derived reference source has different pool thresholds.

## Atlas, batching, and original resource validation

`SpriteAtlasProviderTests`, `SpriteHierarchyTests`, `UiSpriteImageTests`, and
`SpriteAtlasBakerTests` cover device limits, lossless tiling, layout fallback,
page retirement, bounded zero-allocation region caching, hierarchy visibility,
and offline source generation. The EditorRendering test assembly references
both the existing Runtime and Editor assemblies; their friend declarations
are maintained alongside those assemblies.

`ObjectSpriteRendererTests`, `CanvasPresentationTests`,
`BakedSpriteGraphicsTests`, `ImportedSpriteAtlasGraphicsTests`, and
`RenderingCoverageCaptureTests` exercise real GPU batching, original clip
semantics at pixel centers, reversed sources, scaled scene targets, imported
2K page completeness, UI interleaving, and native capture isolation. Atlas
images are compared to the unchanged original files, not to generated
expectations. Opt-in original-artwork tests use `PVZ_BOARD_STRESS_ASSETS`.

The final isolated macOS run using Unity 6000.3.17f1 passes 282 PlayMode tests
with two opt-in tests skipped, and 448 EditMode tests. Both skipped PlayMode
cases (the benchmark and original Dave voice) also pass in separate configured
runs, giving 284 unique passing PlayMode cases. Two pre-existing
`SquashShakesFiveTicksAfterDamageOnlyOnLand` cases also fail on the unchanged
HEAD baseline; they are simulation fixture failures, not graphics validation.
The rendering benchmark is run separately with its output environment variable.
A GPU timing source that provides too few samples is explicitly marked unavailable.

The source import tool has 14 standard-library regression cases:

```sh
python3 -B -m unittest discover -s Tools/Tests/RuntimeAssetImportTests -p 'test_*.py' -v
```

Imported PNG/JPEG, audio companions, font page references, and metadata can
be checked through `PlantsVsZombies.Editor.RuntimeAssetVerification.Verify`
in the exact Editor. Generated atlases and original archive trees are excluded
from the default isolated test snapshot; explicit imported-page tests generate
small independent source fixtures instead.

## Production baked almanac regressions

`BakedAlmanacGraphicsTests` compares the direct-entry home page, all 49 plant
previews and all 26 zombie previews with original loose artwork through public
almanac navigation. It uses the actual production `Application.streamingAssetsPath`
catalog, not the external loose provider used by older original-art fixtures.
`BakedAlmanacResourceLifetimeTests` verifies cold Pole Vaulter portraits and frozen
thumbnails after page eviction and menu re-entry. `SpriteCaptureBatchReuseTests`
checks capture reuse with SRP batching on/off; `SpriteRegionGeometryPolicyTests`
checks tight-scene/native-tile cache separation and warm zero-allocation behavior.

The regression evidence in `.unity-runs/almanac-fix` includes a pre-fix 3,379-pixel
Pole Vaulter preview failure, the cache-hit invalid-importer failure, and fixed
whole-portrait/lifetime tests. The fixed workload retains seven draw calls and
33.4 MiB total texture counter. macOS Metal verification does not establish
Windows Editor D3D validation; reproduce the reported direct-entry case there.

## Almanac description layout regressions

`AlmanacDescriptionLayoutTests` checks positive multiline/Chinese rich-text
heights, original token spacing, scroll bounds, and agreement with Unity UI
Text's own layout settings. The almanac measures with a retained `TextGenerator`;
IMGUI `GUIStyle.CalcHeight` can return zero for the same dynamic font on macOS,
which clips the entire body while the short header remains visible.

With `PVZ_BOARD_STRESS_ASSETS` set as above, `AlmanacDescriptionGraphicsTests`
checks original plant/zombie description pixels through real menu navigation,
then scrolls the long body to its bottom. `CanvasGroupedTextGraphicsTests`
independently checks rich text group clipping against an absolute label before
and after dynamic font atlas growth. Run both layout and graphics coverage:

```powershell
./Tools/Tests/Run-UnityTests.ps1 -Platform EditMode -TestFilter 'AlmanacDescriptionLayoutTests;PointerRoutingTests;UiSpriteImageTests'
./Tools/Tests/Run-UnityTests.ps1 -Platform PlayMode -TestFilter 'Almanac;CanvasPresentationTests;CanvasGroupedTextGraphicsTests;SeedBankLayoutTests;OriginalBattleTooltipTests'
```

The `.unity-runs/almanac-text` evidence records the macOS Metal zero-height
failure and fixed card/scroll screenshots using Unity 6000.3.17f1. Windows Editor
and target-platform Players remain unverified for this text-layout change.

## Deferred geometry and adventure allocation regressions

`SpriteGeometryBatchTests` compares the original immediate renderer with the managed
and Burst deferred executors. It checks exact vertex/UV/index streams, polygon areas,
painter order and GPU pixels, including mirrored crops, half-pixel clipping,
noninteger destination grids, multi-page tight geometry, provider cache reuse,
1024-region chunks, oversized-sprite fallback, portable index/sorting limits,
capture interleaving, resource retirement and disposal. The production presenter
opens the deferred scope explicitly; public `DrawQuad` remains immediate. When
Burst is disabled or unsupported, production uses the faster original managed
path; the managed batch executor remains available for differential measurements.

`AdventureBoardViewTests` checks stable live read-only views, bounded presentation
keys and native ground-clip lookup. `GamePresentationPerformanceTests` warms and
measures the real adventure Update/Present traversal for ordinary combat, pause,
conveyors, detached arms, snow trails, explosions and three corpse paths. These
scoped allocation checks use Unity's current-thread `GC.Alloc` recorder with a
positive control: the macOS Mono runtime can return zero from
`GC.GetAllocatedBytesForCurrentThread` even for an allocated byte array. Zero
observed allocation events establishes zero managed bytes in the measured scope;
a nonzero event count does not supply the unavailable byte sizes. Event frames,
new resources, changed labels and entity creation are measured separately.

The original-artwork `RenderingBenchmarkTests` fixture accepts:

- `PVZ_BOARD_STRESS_ASSETS`: read-only original runtime resource directory.
- `PVZ_RENDER_BENCHMARK_OUTPUT`: an ignored output directory (required to enable it).
- `PVZ_RENDER_USE_BAKED=1`: use the isolated project's production StreamingAssets
  and generated Resources atlas snapshot, rather than the external loose provider.
- `PVZ_RENDER_BENCHMARK_PLANT_COUNT` and `PVZ_RENDER_BENCHMARK_ZOMBIE_COUNT`:
  deterministic initial populations (defaults 30 and 50).
- `PVZ_RENDER_BENCHMARK_ADVANCE=1`: advance simulation by 0.01 seconds per sample.
- `PVZ_RENDER_GEOMETRY_MODE`: `Immediate`, `Managed` or `Burst`.
- `PVZ_RENDER_BENCHMARK_STATES=1`: restrict the fixture to the ordinary combat state.
- `PVZ_RENDER_PROFILE=1`: collect the optional phase markers.

Each selected state warms 120 frames, then records 900 frames with average/P95
complete Present time, simulation-plus-Present time, scoped allocation events,
retained native capacities, active populations and actual atlas-page diagnostics.
Present includes deferred geometry, commit and uploads; nested marker durations
must not be added together. Run before/after workloads sequentially with the same
seed, resolution and source snapshot, Deep Profile disabled and all resources and
Burst compilation warm. Raw per-frame CPU and allocation-event arrays are saved
alongside steady/event-frame average and P95 values. For a baseline that allocates
continuously, set `PVZ_RENDER_BENCHMARK_EVENT_REFERENCE` to its paired optimized
`state-0.json`: this applies identical event indices instead of classifying every
baseline frame as an event. The fixture checks that the workload and final
simulation state match before accepting that reference mask.

The default isolation script excludes StreamingAssets and Generated: explicitly copy those resources into a dedicated ignored test
project for production-atlas evidence. Never reuse another running test project.

Relevant filters are `AdventureBoardViewTests;ReanimationHotPathTests;ReanimationPoseBlendTests`
for EditMode and `SpriteGeometryBatchTests;GamePresentationPerformanceTests;ObjectSpriteRendererTests;CanvasPresentationTests;SpriteCaptureBatchReuseTests`
for PlayMode. The real-source atlas and original artwork regressions remain required
when changing source mapping, clipping or capture boundaries. Editor graphics
checks and the isolated synthetic Mono/IL2CPP Player smoke do not establish
complete game behavior or performance on other target platforms.


## Original PC spawn fidelity

The spawn regressions use the supplied hash-identified PC executable's machine-code
results, including all three adventure progress states, local survival selection,
18-to-50-flag gate reduction, doubled later survival budgets, dynamic weights and
quotas, and native float stores. Run the focused Unity **6000.3.17f1** suites:

```powershell
./Tools/Tests/Run-UnityTests.ps1 -Platform EditMode -TestFilter 'AdventureWaveSpawnTests;ModeWaveFidelityTests;AdventureSpawnFidelityTests;PuzzleSpawnTests'
./Tools/Tests/Run-UnityTests.ps1 -Platform PlayMode -TestFilter OriginalSpawnSmokeTests
```

The 90 EditMode cases include 200 actual native continuous smooth-row outputs and
weight boundaries, preserved row history, mower/portal weights, rake selection,
600/750-tick branches, balloon attachment health, AddZombieInRow variant draws,
sled fallback creation, and post-creation Umbrella Leaf interception. The PlayMode
smoke drives 33 public independent-mode sessions through real opening spawns and
checks the two 750-tick modes reach their second wave within the native window.
It does not inject entities, clear zombies, or adjust health; unattended Pogo Party
is not required to survive until a second wave.

The standalone `Tools/Tests/SpawnFidelityTests` runner verifies compact native
fixtures without the original executable. These local contracts do not prove full
original-process startup or every target-platform Player's random stream. Explicit
`ModeWaveContext` inputs preserve known native fields; the local account identity
is not asserted to equal the original raw profile seed. The forced-Yeti application
flag is explicit and has no inferred, unverified lifecycle mapping.
