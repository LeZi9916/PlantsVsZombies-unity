# Unity regression tests

Open the project in Unity **7000.0.0a16** and use Window > General > Test Runner.
The EditMode and PlayMode assemblies reference `PlantsVsZombies.Runtime` and are
enabled only with `UNITY_INCLUDE_TESTS`.

The former runtime diagnostics are now PlayMode fixtures in
`Assets/Tests/PlayMode/Smoke` (category `PlayerSmoke`). Their original scenario
checks run with per-case temporary profiles and Test Runner cleanup. Set
`PVZ_BOARD_STRESS_ASSETS` to complete original runtime resources; without it,
these opt-in cases are skipped. See `Tools/Tests/PlayerSmokeTests/README.md` for
case filters, screenshots, and the migrated runner. Shared reflection helpers
live in `Assets/Tests/Shared`, in a test-only assembly referenced by PlayMode
and EditorRendering. None of these helpers or smoke cases ships in Runtime.

Unity 7 requires Burst 2.0.0, uGUI 7.0.0, and Test Framework 1.9.0; the manifest
and Editor-resolved lock file pin those minimum supported versions. Historical
validation records below retain the Editor version actually used at the time.

The Unity 7 migration passes 100 targeted EditMode cases and 57 unique PlayMode
cases, including all 13 migrated smoke cases across focused runs. The nine
parameter cases in `AudioPoolingTests.GameFoleyRoutePlaysEveryRapidProjectileImpact`
and `GameFoleyRoutePreservesNonImpactSuppressionBoundary` fail identically on the
unchanged pre-migration `ae4bc4c` with Unity 6000.3.17f1. They retain obsolete
voice-replacement/admission assertions; this migration preserves those existing
failures. Packaged Players and other target platforms were not validated.

Input System regressions use the Editor's built-in `com.unity.inputsystem` 7.0.0
with Active Input Handling set to the new backend only. `GameInputSystemTests`
queues isolated mouse, keyboard and touch events through the production sampler;
`CanvasInputSystemTests` uses real Canvas controls and official UI actions to
check click/drag, cancellation, focus return, keyboard replacement and IME
composition. Its input fixture is owned for the complete Unity setup/test/teardown
lifetime. `PrimaryTouchTrackerTests`, `PointerRoutingTests` and `GameViewportTests`
retain independent ownership and coordinate coverage.

```powershell
./Tools/Tests/Run-UnityTests.ps1 -Platform EditMode -TestFilter 'PrimaryTouchTrackerTests;PointerRoutingTests;GameViewportTests;SceneBindingTests'
./Tools/Tests/Run-UnityTests.ps1 -Platform PlayMode -TestFilter 'GameInputSystemTests;CanvasInputSystemTests;CanvasPresentationTests'
```

The focused migration run passes 28 EditMode and 37 PlayMode cases, including
the original-resource UI, single-row and campaign smoke scenarios.

These checks use virtual Input System devices and a real graphics backend. They
do not replace physical touch devices, native OS IME/candidate-window inspection,
mobile software keyboards or packaged Player verification. Restart an already
open Editor after changing Active Input Handling.

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
scripts reach non-public session state through `GameMemberReflection` (`Assets/Tests/Shared`),
which searches the coordinator and then each collaborator and rejects ambiguous names, so they do
not depend on which collaborator owns a field or method.

Audio prewarms 64 shared pitched voices and preserves the native eight-voice
Foley limit per category. Full categories drop new requests while existing
sounds finish, preserving the original voice admission behavior. Simulation pools grow with concurrent demand and bound retained inactive
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
runs, giving 284 unique passing PlayMode cases. At that rendering-validation
revision, two `SquashShakesFiveTicksAfterDamageOnlyOnLand` cases also failed on
its unchanged HEAD baseline. The focused Squash follow-up below now corrects
their obsolete immediate-damage setup using the complete original PC sequence.
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
quotas, and native float stores. Run the focused Unity **7000.0.0a16** suites:

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

## Original PC Vasebreaker feedback

`VasebreakerConveyorTests` and `VasebreakerMalletTests` cover adventure 4-5's
three stages and all ten standalone puzzles, including endless. They preserve
real adventure conveyors, the single active hammer, deferred seed/zombie/sun
contents, pause behavior, and stage-clear flash timing.

The supplied PC executable's `434A50` starts `anim_open_pot` at the fixed vase
cell with forty FPS; `4349E0` opens it after twenty native updates. Its
PlayOnceAndHold loop samples `(frameCount - 1) * normalizedTime`. `459120`
draws the original 80x101 foreground at grid(-5,-15), with a separate additive
alpha-196 hover pass. Foley 47 uses Swing with [0,+2] semitones; opening at
`434D60` uses direct unpitched Bonk and Foley 35 VaseBreaking with [-5,0]
semitones. Intermediate-stage clearing starts the unchanged fifty-tick
`ScreenFlash.xml` at Dave's entrance, before the next dialogue population entry.

With `PVZ_BOARD_STRESS_ASSETS` set to the read-only runtime resource directory,
`OriginalVasebreakerTests` checks the actual click paths, original atlas and
hammer pixels, decoded sound playback without duplicates, and warm-white flash
coverage at every viewport corner. Run only the relevant filters:

```powershell
./Tools/Tests/Run-UnityTests.ps1 -Platform EditMode -TestFilter 'VasebreakerPacketTests;VasebreakerJackExplosionTests;VasebreakerMalletTests;AdventureJackAndCoinTests'
./Tools/Tests/Run-UnityTests.ps1 -Platform PlayMode -TestFilter 'OriginalVasebreakerTests;OriginalJackAudioTests'
```

`VasebreakerPacketTests` checks PC `434D60` and `43B610` usable seed drops
at `(60 + 80 * column, 80 + 100 * row)`, native velocity draws, gravity `.09`,
and landing precision. Cards count 1500 grounded updates before fifteen final
updates, rather than expiring by total age. Held cards remain at their original
position with grayness 128; only the first cancellation clamps the counter to
1200. The final-three-second warning uses grayness 192 for thirty of every
sixty updates. PC `49D260` resets alpha for non-white grayness, so the last
fifteen updates retain that gray card before removal. No countdown overlay is
part of the original behavior.

`VasebreakerJackExplosionTests` checks PC `542560` calls `435050` after the
carrier dies. This opens all pots in the explosion center's three-by-three grid,
including diagonals and hypnotized carriers, directly in creation order.
Released contents use the same drop rules; newly released box carriers retain
their original startup and opening delay. A pending hammer cannot open an
already exploded pot twice. Direct Bonk samples play for each opened pot;
VaseBreaking Foley retains its original ten-update admission window.

The focused Unity 6000.3.17f1 macOS follow-up run passes 54 EditMode and nine
PlayMode cases with no skipped tests. These fixtures exercise production presentation and
input in isolation; full-level Player playthroughs, physical-speaker listening,
and other target-platform devices remain unverified.


## Original PC Stormy Night (adventure 4-10)

The supplied `PlantsVsZombies.exe` (SHA-256
`e1511201ec2ae9a7e30259f04bc2ec7272badb2574a2e9b405d0e916ae5c082c`)
is the authority for the storm constants. `42ACA0` initializes phase 6/countdown
150 and looping Rain Foley 49; `42B000` starts gameplay at phase 5/countdown 400.
`42EF10` selects the original random flash intervals, handles the Yeti and loss
branches, emits Thunder Foley 17, and lets paused flashes settle at countdown one.

`431E20` draws two 9x7 rain layers using `particles/Rain.png`, scales 1 and 1.5,
periods 100/20 and 161/33, and board-pan alignment in 100-pixel steps. `437750`
creates RainSplash/RainCircle/RainSplash in the three original vertical bands,
with the original alpha/scale draws and an 11-to-21-update spawn interval.
The supplied reanimations retain their native 12 FPS and eight/nineteen frames.

`431CB0` seeds a **local** MT generator from `MainCounter / 6`; its integer noise
must not reseed or consume the board RNG. `432110` draws the native black/white
masks, including the weaker second flash in phase 5, and redraws BottomUI and
TopRightUI. The opaque black intervals cover the world and street-preview actors;
pickups and cursor previews remain above Weather layer 500003. The original
lightning effect is this fullscreen flash, without an additional bolt sprite.

```powershell
./Tools/Tests/Run-UnityTests.ps1 -Platform EditMode -TestFilter 'AdventureStormTests;AdventureFogTests'
# Set PVZ_BOARD_STRESS_ASSETS to the read-only original runtime resources first.
./Tools/Tests/Run-UnityTests.ps1 -Platform PlayMode -TestFilter 'OriginalStormGraphicsTests;OriginalStormAudioTests'
```

The graphics tests compare actual rain pixels with independent native tile
rectangles and check the real reanimations, world darkness, interface/pickup
brightness and street-preview occlusion. Audio tests decode the shipped clips
through the production loader and inspect actual loop/thunder voices, pitch,
volume and pause/exit behavior. These isolated production-component checks do
not establish a full-level Player playthrough, physical-speaker listening or
other target-platform behavior.


### Storm flow and native actor-layer follow-up

The follow-up uses the same hash-identified PC executable, rather than the mobile
port's differing loss timings. `447B78..447BCA` forces phase 6/countdown 310 at
`PanRightEnd - 1000` and `PanLeftEnd` (2500/6000 ms for adventure 4-10). The same
update then decrements to 309. `420E45` updates game objects before `420FF7` tests
the scene and `42102B` increments MainCounter, so the losing combat update does
not increment that counter. The presentation accumulator preserves centisecond
updates across pause/resume without accumulating float roundoff.

`448040` pans toward the house from 1500 to 3500 ms, plays the two loss chomps,
and starts `ZombiesWon` at 6000 ms, at 12 FPS. PC initializers `6EF150..6EF19A`
set the banner shake to 7000..8000 ms and the screen-layer/retry dialog cue to
11000 ms. The image-free `fullscreen` track supplies its authored black opacity;
the retry dialog pauses rain/effects while the native flash settles. Original
Board::Pause keeps Rain Foley running during LevelIntro. The award phase keeps
the weather active while the board scene is still Playing.

In 4-10, **the conveyor intentionally draws above overlapping zombie heads**:
`4321C3` redraws `425B40` after the storm mask; `425CDD` calls the complete
`SeedBank::Draw` at `49F100`, including the original conveyor backdrop and belt.
The native-overlap regression preserves this original behavior.

The intermittent partial board was a separate rendering exception. PC particle
table `74C8D8`, eight-byte entry 16, resolves to `particles\ZombiePogo.xml`.
`PogoBreak` (`540DF0`) emits it when the stick breaks without magnetic removal;
its authored SystemDuration is forty updates. Missing this mapping aborted the
frame before later rows, weather and the conveyor could draw. The regression
produces a real broken-stick event and completes the whole production storm frame.

`OriginalStormFlowTests` runs the production opening, options pause, battle pause,
unplanted battle through natural loss, camera pan, original banner opacity and
pausing retry transition. Captures use the production deferred geometry path.
`OriginalPlantLayerTests` compares Pumpkin_back / protected body / Pumpkin_front
against independent original-art composition, including both damage cels, and
checks attracted metal against other rows' zombies. The magnet's airborne item
belongs to native Top order 399999. Held metal uses the plant's local foreground
pass after its own body, as corrected by the follow-up below.

Pumpkin initialization at `46C777` sets width 120; GetPlantRect removes twenty
pixels, giving a 100x80 eating rectangle. Its unchanged shovel hit test uses the
original 25..50-pixel rim and lower-edge constraint. Repair uses strict integer
thirds. Bungee/CatapultOrder selects flying/normal/pumpkin/support independently
of creation order. The bungee follow-up below corrects the earlier Cattail
interpretation: lifting it from a pumpkin restores Lily Pad support. Whole-cell crushing
removes the shell and normal contents together; the native spike exceptions and
empty-pumpkin support-placement restriction are preserved.

```powershell
./Tools/Tests/Run-UnityTests.ps1 -Platform EditMode -TestFilter 'AdventureStormTests;AdventurePumpkinTests;AdventurePogoTests.BrokenStickUsesOriginalParticleNameAndLifetime'
./Tools/Tests/Run-UnityTests.ps1 -Platform PlayMode -TestFilter 'OriginalStormFlowTests;OriginalStormGraphicsTests;OriginalStormAudioTests;OriginalPlantLayerTests;OriginalMagnetGraphicsTests;OriginalPoolPlantBobbingTests;GamePresentationPerformanceTests.DefensiveDamageReplacesOnlyTheOriginalBodyTrack'
```

These production-component simulations and real graphics/audio-source checks do
not constitute a packaged Player playthrough, physical-speaker listening, or
validation on Windows, Linux, mobile devices or IL2CPP.


## Original PC Bungee Zombie

The supplied hash-identified `PlantsVsZombies.exe` is authoritative where the
mobile C# reference differs. `53E245..53E326` initializes 450 body health,
altitude `3000 + Rand(151)`, phase 4, looping `anim_drop` at 24 FPS, and **four**
foreground arm prefixes in render group 2. `540180` selects column-major cells
with empty/occupied weights 1/10000, reserves existing bungee targets, excludes
graves/Grave Buster/Cob Cannon, and leaves the final unreserved sun producer.

`540C20` descends and rises eight pixels per centisecond. `540980` plays Grassstep
at the 2596-altitude crossing, BungeeScream Foley 44 below 1500 (except the boss),
and checks Umbrella Leaf only at altitude 40. The bottom pause is 300 updates;
`540700` starts the fifteen-frame `anim_grab` at 24 FPS with blend 20. The native
plant remains allocated while grabbed and rising. `540800` lifts it with Floop,
starts `anim_raise` at 36 FPS, and `540C20` removes the carrier at altitude 600.
Death during grabbing restores the plant; lifted cargo is removed on departure.

The previous pumpkin follow-up incorrectly treated a pumpkin-enclosed Cattail as
immune to theft. PC `5408F2..540950` explicitly lifts it and creates a Lily Pad
(type 16) under the remaining pumpkin. The corrected regression checks this
original replacement and retains the pumpkin.

`540410` positions a suspended zombie fifteen pixels left of its carrier,
sets height 9 and a zero-rate idle pose, and gives the carrier row order 303004.
Landing releases the passenger alive; carrier death or umbrella departure removes
unreleased cargo without loot. The ordinary thief body uses order 301007 until
grabbing promotes it to 305000. An umbrella bounce promotes it to 400001 and
retains the previous body pose. The target is a separate 305001 pass.

`548780` draws rope, body group 0, cargo, then group 2. `5489B0` draws the unchanged
71x76 reticle with its original 3000..2600 descent curve; it does not scale it with
the zombie or draw it for passengers, ascent or the boss. `549520` tiles the
unchanged 8x64 rope from the original body-track center, preserving native
center-based scaled-image placement. Preview phase 10 uses `anim_drop` at 24 FPS
and the original integer SinWave over a 200-update counter. Bounced bungees lose
their shadow, and the frozen original pose retains its rope when burned.

Captured plants stop their abilities, remain occupied during grabbing, and leave
the board's occupancy/targeting when lifted. A carried sunflower yields its remaining
I, Zombie sun at lift rather than waiting for carrier removal. Captured support
plants cannot be used for new roof/water planting. Grabbed plants cannot be
upgraded, and First Aid cannot replace a grabbed defensive plant.

```powershell
./Tools/Tests/Run-UnityTests.ps1 -Platform EditMode -TestFilter 'AdventureBungeeTests;AdventurePumpkinTests;AdventureSpawnFidelityTests.UmbrellaInterceptionFollowsCarrierAndPassengerCreation'
# Set PVZ_BOARD_STRESS_ASSETS to the read-only original runtime resources.
./Tools/Tests/Run-UnityTests.ps1 -Platform PlayMode -TestFilter 'OriginalBungeeGraphicsTests;OriginalBungeeAudioTests'
```

The original-art tests compare exact body/rope/reticle/cargo pixels, native plant
occlusion, the suspended idle passenger, and animated street preview. Audio tests
load all three shipped scream variants through the production loader and check
actual Foley voices, the original [0,+2] semitone pitch range, the direct Boing
one-shot and Umbrella's original Throw2 sample. These isolated checks do not prove
packaged Player behavior, physical-speaker output or other target-platform devices.


### Retained magnetic metal foreground correction

The original PC `Plant::Draw` (`473F00`) draws its body at `47420B` through
`Reanimation::DrawRenderGroup` (`4822F0`). At the end, `4744C6` checks
`DrawMagnetItemsOnTop` (`4700B0`); when the metal is within twenty pixels,
`4744D3` calls `DrawMagnetItems` (`4738B0`) **after** the body. Returning from the
global Top layer therefore selects a local foreground pass. The earlier
implementation and its artwork oracle incorrectly put held items behind the body.

`OriginalPlantLayerTests.AllHeldMagneticItemsUseLocalForegroundOnLandAndWater`
checks all eighteen original metal identifiers/damage cels (1..16, 20, 21), at
their native destinations on both land and Lily Pad water cells. It compares
production deferred-camera pixels with independent body-then-item composition.
The existing cross-row flying-metal regression still protects native global order
399999. Original item movement, stopping distance, scale, offsets and artwork are
unchanged.

```powershell
./Tools/Tests/Run-UnityTests.ps1 -Platform PlayMode -TestFilter 'OriginalMagnetGraphicsTests;OriginalPlantLayerTests.AllHeldMagneticItemsUseLocalForegroundOnLandAndWater;OriginalPlantLayerTests.FlyingMetalDrawsAboveOtherRowsRatherThanOnlyAboveItsMagnet'
```


## Roof planting, lobber flight, and bungee contact follow-up

PC `473490` supplies the same body/shadow height without the mobile roof -12
shift. `473340` receives pot scale one, so its additional mushroom scale terms
cancel. The pot's +26 baseline and native shadow rectangles remain aligned.
`46CA8B..46CAD6` stops an occupied pot at its current frame; Plant::Die restarts
only the uncovered pot at 10..15 FPS without resetting its animation phase.

All four lobber plants and both corn weapons now use `475AA0`/`4765B0` attack
rectangles, `546270` body-center prediction (zero velocity while eating, frozen,
or buttered), and `4752E0`'s fifty-update lead, forty-pixel minimum distance and
120-update velocity divisor. `47C880` integrates native float32 state per update,
checks nearby bodies during the first twenty updates, and later checks the
weapon-specific descending height. `47DFF0` uses 40-pixel cabbage/corn/butter
rectangles and the melon's +20/60-pixel rectangle; `47C130` resolves overlapping
hits by native zombie position, independently of aiming's body-left priority.
Roof floor rebasing follows the native contact order, and height above 80 causes
a ground impact even after target death. Missing launch targets keep the original
`700 - muzzleX` fallback. Existing reward/dolphin fixtures seed complete native
flight state rather than reconstructing an arc from age alone.

`5496A6` reads the original cord offset **61**, rather than 39. Native contact
plants retain `54D810`'s bungee bottom phases, plus `475AA0`'s species-specific
column checks: a mine triggers for its own column, while Chomper excludes its own
carrier. `54E920` freezes the burned bungee without replacing its render order;
pre-grab corpses stay behind plants and grabbing corpses keep order 305000.

```powershell
./Tools/Tests/Run-UnityTests.ps1 -Platform EditMode -TestFilter 'AdventureRoofCombatTests|AdventureBungeeTests|SimulationPoolingTests.ProjectileResetRestoresEveryFieldAndRetainsHitStorage|AdventureDolphinTests.InFlightProjectilesRespectOriginalProtectedPhases|AdventureDolphinTests.SplashAndChillSkipScriptsButOriginalCherryBlastCanHit'
# Set PVZ_BOARD_STRESS_ASSETS to the read-only original runtime resources.
./Tools/Tests/Run-UnityTests.ps1 -Platform PlayMode -TestFilter 'OriginalPlantShadowTests|OriginalBungeeGraphicsTests'
```

These focused checks cover body/shadow pixels, occupied pot frames, normal/small
cords, burned-body occlusion, near eating targets with/without Pumpkin, and all
lobber weapons hitting stationary/moving column-nine targets from roof column
two. They do not establish a full-level Player playthrough or other target devices.


## Original ladder placement and traversal

PC `546620` starts the twelve-frame `anim_placeladder` at 24 FPS with blend ten,
then rechecks the defensive target before `414D10` creates the cell ladder.
Detaching the shield switches to the ordinary walking clip with zero blend and
plays the original `ladder_zombie` sample. A missing target retains the carried
ladder; ice and butter suspend placement, while chill slows its animation clock.

`459610` draws `IMAGE_REANIM_ZOMBIE_LADDER_5` at cell +(25,-4), scale .8, using
the scaled image half-size so its top-left remains at that offset. The placed
object uses row order 302800, after plants and before ordinary zombies. PC `546CE0` raises the walking body by .8 per update
and probes the cell at integer zombie X + 5 + altitude/2; low native velocities
also receive the original extra .5-pixel leftward movement. Missing the ladder
or reaching height ninety transitions to `5452B0`'s falling update in the same
action pass. The original keeps walking artwork during ascent/descent; there is
no separately authored climb clip. `mUseLadderCol` prevents retriggering the same
crossing. Plant removal, magnetic removal, body collision heights and falling
death playback retain the same cell and altitude state.

```powershell
./Tools/Tests/Run-UnityTests.ps1 -Platform EditMode -TestFilter 'AdventureLadderTests|AdventureMagnetTests.ZombieMetalPrecedesPlacedLaddersAndLadderRangeIncludesSquareCorners|AdventurePogoTests.PlantTriggersPreparationThenAForwardVaultIncludingLadders'
# Set PVZ_BOARD_STRESS_ASSETS to the read-only original runtime resources.
./Tools/Tests/Run-UnityTests.ps1 -Platform PlayMode -TestFilter OriginalLadderGraphicsTests
```

The focused cases exercise original placement timing, interruption, slow/frozen
clocks, normal/armored/football/ladder/pole followers, ladder removal, corpse
falling, original shield render groups, day/night and roof ladder occlusion, and
raised walking-body pixels. These isolated component checks do not replace a
full-level Player playthrough or target-platform device validation.


## Original lobber reload and independent butter state

PC `472C70` keeps the spent shooting counter at one until the body reanimation
finishes, then `4768A0` restarts idle at frame time zero and the source definition's
12 FPS (with PC `4768F9` preserving the static I, Zombie idle). The original
cabbage/melon/winter-melon shooting clips have 38 frames;
corn has 34, all at 35 FPS. `47301D..473069` restores the corn render prefix before
creating a butter projectile. The projectile keeps its butter metadata after the
basket switches back to kernels.

`4827D0` starts blends only for currently visible source tracks. The retained
renderer now records hidden poses in both drawing paths, so a reappearing load
cannot blend from an old throwing pose. Butter attachments may read the body's
already-drawn blended head matrix rather than jumping to an unblended new clip.

PC `54E470` uses a separate 400-update butter counter and the original head,
freeze-admission, vehicle, flying, sled and kelp gates. It does not create ice or
chill. Physics, playback and render interpolation honor this independent counter;
fire removes actual cold state while preserving butter. `549EC0` uses the
transformed head center (special tracks for a newspaper gasp and catapult), fixed
(-29,-36) top-left placement, scaled image half-size, and original species offsets/scales.
These PC values differ from the supplied mobile-derived source.

```powershell
./Tools/Tests/Run-UnityTests.ps1 -Platform EditMode -TestFilter 'AdventureButterAndReloadTests|AdventurePogoTests.BouncingPogoRejectsButterAndHeadLossBreaksItsStick'
# Set PVZ_BOARD_STRESS_ASSETS to the read-only original runtime resources.
./Tools/Tests/Run-UnityTests.ps1 -Platform PlayMode -TestFilter 'OriginalButterAndReloadGraphicsTests|GamePresentationPerformanceTests.KeepsPlantTrackFiltersLocalToEachDraw|GamePresentationPerformanceTests.WarmPlantPresentationDoesNotAllocateManagedMemory|GamePresentationPerformanceTests.WarmZombiePresentationDoesNotAllocateManagedMemory|OriginalDancerGraphicsTests.RaisedArmRestartsBlendFromCurrentPoseAcrossNativeDanceSteps|OriginalZombieDeathBlendTests.SuperlongDeathKeepsOriginalLegDirectionsThroughCorpseBlend'
```

The contact follow-up runs ordinary chewing against a four-health triggered
Squash through its airborne transition on day, night and roof boards, including
latched eating. The roof failure reproduces when its already-worn flower pot is
eaten after takeoff: support-stack cleanup previously deleted the airborne Squash.
Cleanup now preserves the original NotOnGround state so the attack can finish.
A Chomper completes a bite against an eligible adjacent bungee at the bottom or
while it is grabbing another plant, restoring that captured plant.
PC `475BEE..475BF7` excludes a bungee targeting the Chomper's own column, and
Plant::Update stops abilities while grabbed; that original restriction remains.
These focused simulation, allocation and original-art graphics checks do not
replace a full-level Player playthrough or other target-device validation.


## Ladder geometry, carrying followers, ash removal and Chomper range

PC `459610` calls `TodDrawImageScaledF` (`527EF0`): its matrix adds the
**scaled** half-image to the requested top-left. Placed ladders retain cell
+(25,-4) at scale .8 on both sloped and flat roof columns. The independent
rendering oracle now uses a top-left destination rectangle rather than repeating
the presenter's center arithmetic. The same helper corrects the previous butter
follow-up's 1.2-scale Bungee/Gargantuar attachments.

`Zombie::EatPlant` checks the ladder cell regardless of the targeted plant layer.
A carrier therefore still climbs when a bungee lifts the defensive main plant
and leaves its flower pot. The carried shield and carrying phase survive the
crossing, and original walking artwork rises and falls with body altitude.

`Board::KillAllZombiesInRadius` (`4294C0`) removes ladders in a square of cells
around the pixel-to-grid explosion center, using the row range for both axes.
This happens even without eligible zombies, independently of the pixel circle
and burn flag. Cherry/Cob use range one, Doom range three, and Potato Mine range
zero. Jalapeno's `BurnRow` clears the complete row. Removing a ladder sends a
surviving climber into the original fall on its next action.

PC `4765B0` gives Chomper the horizontal interval [plant X+80, plant X+120];
`475AA0` accepts touching edges and extends overlap tolerance by sixty pixels
while biting or against eating targets. Roof/pot drawing offsets do not extend
that logical interval. Existing runtime targeting already matches these bounds;
new cases pin both ends on day/night and sloped/flat roof columns, including
the original body-X > 800 target exclusion.

```powershell
./Tools/Tests/Run-UnityTests.ps1 -Platform EditMode -TestFilter 'AdventureLadderTests|AdventureChomperRangeTests'
# Set PVZ_BOARD_STRESS_ASSETS to the read-only original runtime resources.
./Tools/Tests/Run-UnityTests.ps1 -Platform PlayMode -TestFilter 'OriginalLadderGraphicsTests|OriginalButterAndReloadGraphicsTests.ButterUsesTheOriginalTransformedHeadAndSpeciesOffsets'
```

These focused native-update and original-art graphics checks do not establish a
full-level Player playthrough or behavior on other target-platform devices.


## Original PC Catapult Zombie

The supplied PC executable is authoritative. `53E6CF..53E72A` initializes 850
health, twenty shots, an on-board walk at 5.5 FPS and UI idle at eight FPS.
`541320` chooses the leftmost eligible same-row plant at least one hundred
pixels ahead, then applies flying/normal/pumpkin/support priority. Spikes and
plants already off the ground are excluded from aiming.

`541460` starts the 32-frame `anim_shoot` at native X <= 650, 24 FPS and initial
blend zero. Its .545 event rechecks the target and creates a real basketball.
The 300-update timer starts at launch initiation and continues through shooting
and the 12-FPS idle reload. Later transitions blend for twenty updates. The shot
counter decreases only when the clip finishes; the final four basket tracks
are hidden in native order. Empty ammunition resumes the original walk at six
FPS. Vehicle squashing remains active during launch and never starts an eating
clip or resets the shooting clock. Ice/butter hold action and playback, while
chill slows playback without slowing the independent reload timer.

`5411C0` fires at mPos +(113,-44), with the original forty-pixel minimum range,
120-update velocity divisor, -7 vertical baseline and .115 acceleration.
The PC definition at `74A97C` gives basketball type nine **75 damage**.
`47C880` resolves real descending plant collision, the extra pool height,
roof floor rebasing after contact, and finite ground impacts after target loss.
Umbrella starts its five-update block at contact, then emits Splat and the
original `UmbrellaReflect` particle at global order 400001. `47D950`/`527B50`
retain the source image center, 1.1-scale art, negative rotation and original
shrinking day/night shadow.

`54D080` and `547FF0` supply damaged siding, the source-frame-dependent damaged
pole with/without a ball, engine smoke at +(47,77), critical shaking and silent
health loss. `54A980` preserves spike puncture as the 15-frame `anim_bounce`
at twelve FPS, blend ten, a tire particle and 280-update death timer. Existing
ice, butter and chill counters expire independently during this retained death.
Normal lethal damage emits the original `CatapultExplosion` immediately; ash
retains the existing original charred vehicle definition. Launch Foley 37 now
loads the shipped `basketball` sample with the native ten-semitone range;
Splat, TirePop and Explosion use their existing original recordings.

```powershell
./Tools/Tests/Run-UnityTests.ps1 -Platform EditMode -TestFilter 'AdventureCatapultTests|AdventurePumpkinTests.AerialAttacksSelectTheNormalPlantBeforeTheShell|AdventureZomboniTests|SimulationPoolingTests.ProjectileResetRestoresEveryFieldAndRetainsHitStorage'
# Set PVZ_BOARD_STRESS_ASSETS to the read-only original runtime resources.
./Tools/Tests/Run-UnityTests.ps1 -Platform PlayMode -TestFilter 'OriginalCatapultGraphicsTests|OriginalCatapultAudioTests'
```

These focused production-update, original-art GPU and real audio-voice checks do
not constitute a full-level packaged Player playthrough, physical-speaker
listening, or Windows/Linux/mobile/IL2CPP device validation.


## Original mushroom sleep, Doom cell cleanup and chooser warnings

PC `Plant::SetSleeping` (`46CB90`) creates the independent reanimation 68,
`Z.reanim`, at plant +(50,40), render order +2 and scale one. Fume adds twelve
pixels to X; Scaredy subtracts twenty from Y and Gloom subtracts twelve. The
seventeen-frame source loops at a random six-to-eight FPS with an initial
normalized phase in [0,.9]. This free board object keeps its original position
and clock while a bungee captures the body, and disappears when the plant wakes
or dies. Sleeping Gloom upgrades create the same independent state immediately.
All ten existing mushroom body clips were compared against the original
`anim_sleep`; these correct source body poses remain unchanged.

`474B10` calls `474AC0` before adding the crater: every allocated plant in the
Doom-shroom's own row/column dies, including flower pot, Lily Pad and Pumpkin.
Adjacent cells' plants survive. Clearing the roof support reveals the crater
that was previously covered by the surviving pot. The original 250-pixel,
three-row zombie blast, awake fuse, terrain-specific crater atlases, ninety-second
aging split and twenty-five-update fade remain intact. Crater occupancy now
expires at exactly 18000 native updates, avoiding accumulated float subtraction
error; permanent Beghouled holes retain their infinite lifetime.

PC `498E30`/`49A2C0`/`49A300` select the original `NOT_ALLOWED_ON_THIS_LEVEL`,
`NOCTURNAL_WARNING` or `NOT_RECOMMENDED_FOR_LEVEL` text. Selected coffee suppresses
only the daytime-mushroom recommendation bit. The other original conditions
cover water, graves, fog, roof, spikes on roofs and coffee at night. Tooltip
routing now also covers selected bank cards and the independent-mode chooser;
combat hovers keep their existing cooldown/sun advice and omit descriptions.
No replacement warning prose or artwork is introduced.

```powershell
./Tools/Tests/Run-UnityTests.ps1 -Platform EditMode -TestFilter 'AdventureMushroomTests|AdventureScreenEffectsTests.DoomShroomStartsItsOriginalFuseWhenWokenAndDetonatesOnce|AdventureSeedChooserTests'
# Set PVZ_BOARD_STRESS_ASSETS to the read-only original runtime resources.
./Tools/Tests/Run-UnityTests.ps1 -Platform PlayMode -TestFilter 'OriginalMushroomGraphicsTests|OriginalBattleTooltipTests'
```

The focused checks cover sleep flags and independent clocks on all six original
backgrounds, own-cell Doom cleanup and exact crater expiry, original sleeping/Z
pixels including a small roof body and lifted cargo, dry/water/roof crater cells
and fade, and chooser warning pixels through the production bitmap fonts.
These checks do not constitute a full-level packaged Player playthrough or
validation on Windows/Linux/mobile/IL2CPP devices.


## Original PC Squash sequence and targeting

The supplied PC executable is authoritative where the mobile-derived C# differs.
`46ED90` uses eighty look updates, **forty-five** prelaunch updates, fifty rising
updates and ten falling updates. `46F052..46F058` damages at falling counter
**five**; at zero, dry/roof landings start the original (1,4) screen shake and
Thump, while water landings use Splat/Splash and immediately remove the Squash.
Dry land retains its flattened body for one hundred updates. The mobile source
uses thirty prelaunch updates and damage at counter four instead.

The old screen-feedback fixtures set `ActionTimer=.01` and expected damage on
the next update. They now exercise a real triggered Squash against a controlled
native zombie through every original phase, proving the general timer cannot
skip the sequence. Ground, pool and roof cases verify both sides of the damage
boundary, the five-update landing delay, terrain effects/sounds, surviving
support and exact flattened-body lifetime.

The audit also restores two native target gates. `46EC0C` calls `54E240` to
exclude kelp-owned/dragged-under zombies from acquisition. `546270` calls
`546300` before leading a body: stopped launch/reload phases, including native
Catapult phases 67/68, use zero prediction velocity. The shared original
ZombieNotWalking gate now supplies that decision. The PC `46EBA0` scan allows
multiple Squashes to select the same zombie; the mobile-only target reservation
restriction is deliberately not added.

```powershell
./Tools/Tests/Run-UnityTests.ps1 -Platform EditMode -TestFilter 'AdventureScreenEffectsTests.SquashShakesFiveTicksAfterDamageOnlyOnLand|AdventureSquashTests|AdventureButterAndReloadTests.TriggeredSquashSurvivesOrdinaryZombieChewingUntilAirborne|AdventureButterAndReloadTests.EatenFlowerPotKeepsTheAlreadyAirborneSquashAlive'
# Set PVZ_BOARD_STRESS_ASSETS to the read-only original runtime resources.
./Tools/Tests/Run-UnityTests.ps1 -Platform PlayMode -TestFilter OriginalPuffShroomTests.SquashUsesOriginalLookLaunchFlightAndFlattenedArtwork
```

The focused cases include the previously corrected chewing immunity and airborne
roof-support independence, plus original source-art playback. They do not
constitute a full-level packaged Player playthrough or other platform/device
validation.


## Original flying coffee bean, mushroom wake and chooser grayness

PC `46C847..46C853` gives coffee its own hundred-update special countdown.
`474FEA..47503E` then sets the sleeping main plant's separate wake counter to
one hundred, starts the bean's fifteen-frame `anim_crumble` at 22 FPS with
blend twenty, and requests Coffee Foley 69. The body completes its own crumble
before removal; the mushroom wakes independently and requests Wakeup Foley 83
at counter sixty. Placement now creates a real flying Effect-layer bean rather
than returning the mushroom and starting an aggregate timer. Its occupancy
blocks duplicate coffee immediately; removing the mushroom does not skip the
bean's own authored lifetime. A grabbed bean suspends its special while its
body reanimation continues; an already-started mushroom wake remains independent.

`471D60` draws coffee at scale .8 with +(12,10) over its ordinary draw-height
offset. `471F22..471FBD` uses the mushroom's last seventy wake updates for the
original EaseSinWave squish/stretch, preserving the eighty-pixel foot anchor.
`46CB90` retains normalized body time across sleep-to-idle clip ranges; the
original Sun-shroom's ten sleeping and twelve idle frames are checked explicitly.
Later body actions discard this initial wake phase instead of reusing it.

PC chooser draw `49A500..49A769` supplies RGB grayness 115 for warned home
packets, 55 for selected/flying homes, and 255 for ordinary available homes.
Opacity remains unchanged. `49C7E0`/PickedPlantType requires a settled bank packet
and recognizes Imitater coffee. Reaching the bank therefore removes only the
nocturnal recommendation bit and restores those mushroom homes to full brightness;
water/roof/other warnings and selected-home darkness remain independent.
The actual campaign chooser, legacy/mode homes, simple development chooser and
native tooltip warning use the same recommendation decision.

```powershell
./Tools/Tests/Run-UnityTests.ps1 -Platform EditMode -TestFilter 'AdventureCoffeeBeanTests|AdventurePlantSoundTests.CoffeeAndWakeSamplesFollowIndependentFuseAndWakeCounters|AdventurePlantingEffectsTests.CoffeeBeanDoesNotCreateSoilOrWaterParticles|AdventurePlantingSoundTests.SuccessfulPacketPlacementRequestsOriginalSquareSound|AdventureMushroomTests.SleepingClockRemainsIndependentOfCapturedBodyAndCoffeeWake|AdventureSeedChooserTests'
# Set PVZ_BOARD_STRESS_ASSETS to the read-only original runtime resources.
./Tools/Tests/Run-UnityTests.ps1 -Platform PlayMode -TestFilter 'OriginalCoffeeBeanGraphicsTests|OriginalMushroomGraphicsTests|OriginalPlantSoundTests.NativePlantSamplesLoadAndReachTheirCorrectProductionVoices|GamePresentationPerformanceTests.WarmPlantPresentationDoesNotAllocateManagedMemory'
```

Focused cases cover dry/water/roof coffee, independent actor/wake lifetimes,
coffee placement effects and sound timing, capture during idle and crumble,
original idle/crumble pixels, wake body matrices and normalized phase, original
card pixels and campaign submissions, coffee flights/removal/Imitater, existing
sleep/Z and tooltip artwork, original audio voices and warmed plant allocation.
They do not constitute a full-level packaged Player playthrough, physical-speaker
listening, or other target-platform/device validation.


## Original PC Gargantuar, roof shooters and squished plant pictures

The supplied PC executable is authoritative. `53F988..53FACB` initializes the
normal/red-eye giant's body health, carried imp and original weapon variations.
`542790` retains the 33-frame `anim_smash` at 16 FPS and 34-frame `anim_throw`
at 24 FPS, both once-and-hold with twenty-update blends. The .64 smash event
reacquires its target, applies original whole-cell Squish, damages Spikerock by
fifty and the giant by twenty, and requests Thump plus (0,3) board shake. The
450-health rock therefore survives eight smashes. Lowgroan starts the smash.

`542C74..542CB4` requires body health strictly below half and native X > 400.
The PC throwing distance uses **mPosX - 360**, while the mobile-derived source
uses 460. The .74 release hides the imp/white rope, requests Swing and Imp,
and creates the imp at mPosX -133, altitude 88 and horizontal velocity three.
Roofs subtract 180 from the distance and allow a -140 minimum. `542D60` keeps
.05 gravity and native floor rebasing independent of chill; the original
sixteen-frame thrown and six-frame landing bodies use 18/24 FPS. `547173`
updates actions before walking, including the update that finishes an action.
Flying imps retain the original range-mask exclusion for ordinary projectiles.
`54D47A` supplies the original integer-thirds body, arm, foot and head damage
images, including red-eye variants. Foley table `74B3B8` supplies both Imp
recordings at a five-semitone range and the two Squish/chomp recordings.

`4752E0`/`4737D0` launch pea weapons from the animated original body anchor and
plant cell, including the five-pixel pot correction. Threepeater, Split Pea,
Cactus and Scaredy retain their individual source muzzles and image dimensions.
`47C4B0` tests the original shadow-to-projectile clearance (28 pixels for peas
and spikes, zero for spores). Zombie collision precedes that test; roof shadow
rebasing follows it in `UpdateMotion`. Slope contact now uses the original
DoImpact particle coordinates and Foley. The original launch path also permits
Gatling Pea to fire on sloping roof columns. The existing Torchwood fixture now
launches from a roof position where the native shot survives to reach its tree.

`470FA0` runs the awake instant-plant specials and preserves triggered Squash
immunity. Other crushed plants leave active lawn occupancy while retaining
static cached pictures at row order 301008, after pre-grab bungees and before
live plants. The native disappear counter starts at 500 and removes the object
only when negative. `473F88` uses quarter-height images and an unscaled
height-plus-sixty argument; `47F48E` scales the cached image offsets rather than
that argument. Pots retain their -15 adjustment. Whole-cell crushing preserves
each layer's picture, and Doom's own-cell cleanup also removes earlier crushed
pictures. The PC picture remains opaque throughout its retained lifetime.

`54E920` creates a free charred catapult reanimation at native mPos +(-14,-30).
Its thirty original frames run at twelve FPS times the original .9..1.1 random
rate. Live body's GetDrawPos species offsets and body overlay do not apply to
this free definition. Dry ground, pool banks and both roof sections are checked
against the unchanged source artwork.

```powershell
./Tools/Tests/Run-UnityTests.ps1 -Platform EditMode -TestFilter 'AdventureScreenEffectsTests.IceShroomCreatesOriginalIceTrapAndExpiresAtFortyTicks|AdventurePlantSoundTests.InstantSpecialRequestsItsNativeActivationSample|AdventureGargantuarTests|AdventureRoofShooterTests|AdventureRoofCombatTests|AdventurePumpkinTests.CrushingDestroysTheShellAndItsContainedPlantTogether|AdventureTorchwoodTests|AdventureCactusTests|AdventureScaredyShroomTests.ActiveShotCompletesBeforeLoweringWithoutFiringTwice|AdventureCatapultTests|AdventureButterAndReloadTests.EatenFlowerPotKeepsTheAlreadyAirborneSquashAlive|AdventureSquashTests.ImpactUsesOriginalShieldAndCorpseRulesInsteadOfAnExplosion|SimulationPoolingTests.ProjectileResetRestoresEveryFieldAndRetainsHitStorage'
# Set PVZ_BOARD_STRESS_ASSETS to the read-only original runtime resources.
./Tools/Tests/Run-UnityTests.ps1 -Platform PlayMode -TestFilter 'OriginalGargantuarGraphicsTests|OriginalGargantuarAudioTests|OriginalCatapultGraphicsTests|OriginalRepeaterTests|OriginalTorchwoodGraphicsTests'
```

Focused checks cover real production action clocks, freeze and target loss,
throw thresholds and roof ballistics, source-backed body/weapon/damage pixels,
flattened layers and expiry, actual roof launches/muzzles/impact particles,
original decoded recordings and playing audio voices, and nearby existing
vehicle/projectile behavior. They do not establish a packaged Player playthrough,
physical-speaker listening, or Windows/Linux/mobile/IL2CPP device validation.

## Original PC random placement and randomness audit

The supplied PC disassembly is authoritative. Plant initialization `46BE80`
(`plant_timing/asm/46BE80.asm` in the supplied analysis) changes the native
position only for three in-play species:

| Plant | Persistent X offset | Persistent Y offset |
| --- | --- | --- |
| Puff-shroom / Sea-shroom | `R(10)-5` (-5..4 pixels) | `R(6)-3` (-3..2 pixels) |
| Sun-shroom | `R(10)-5` (-5..4 pixels) | `R(10)-5` (-5..4 pixels) |

`R(n)` is the original exclusive integer remainder draw. X precedes Y. Offsets
are sampled once on placement, including an Imitater's resulting mushroom,
without changing the occupied row/column. Body, shadow, cached crushed picture,
bite/explosion rectangles, short-range attack boundary, spore muzzle, plant sun
origin and catapult acquisition use the displaced position. Cursor previews and
cell-based planting particles retain their original coordinates. `46CB90`
creates the free sleeping Z before the displacement, so that independent object
retains its earlier position.

PC zombie initialization `53DD90` draws `R(40)+780` at `53DE17`, then the
400..500 groan clock at `53DED5`. Explicit lawn placements still consume this
initial X draw before their caller replaces the position. `53E018` adds forty
pixels for a flag wave before species overrides: ordinary X is 780..819, or
820..859 on a flag wave. Pole-vaulter uses 870..879, Zomboni 800..809, Catapult
825..834, Gargantuar 845..854, flag zombies 800 and riding Bobsleds 880.
Scheduled actors sample independently; their list index supplies no spacing.

The broader source inspection found these existing random paths: the original
31-bit MT/remainder generator, weighted wave species and smoothed lane picks,
shooter countdowns and kernel butter, native zombie speed ranges, Jack-in-the-box
fuses, Pogo phases, Gargantuar weapons, garlic lane choices, freeze durations,
loot, pickup motion, grave appearances, storm/rain and original particle tracks.
Their presence does not establish a complete original-process random replay.

The audit also corrected production paths that bypassed existing native rules:
Sun-shroom initialization is 300..1250 updates, Marigold initialization 0..2500,
and their repeat interval is 2350..2500. Twin Sunflower now draws its reset
before constructing either pickup. Sky initialization uses `425+R(276)`;
subsequent drops draw X, increment the drop count, draw
`min(950,425+10*drops)+R(275)`, then construct the pickup and draw landing Y.
These replace fixed production intervals, floating sky intervals, the delayed
sky count increment and the incorrect additive cap.

The follow-up native-random repair restores the ordinary plant blink lifecycle,
original boss phases/counters/contact markers and adventure/challenge health
thresholds. Board cell looks/jitter now consume the shared stream once before
level initialization; later graves reuse the constructor-selected cell. Street
previews and Dave actions also use the active game's stream. Native Foley
pitch, admission and variation are sampled at the request boundary and retained
on the event, so drawing or delayed audio playback does not resample the game.
The original initialization draws discarded by specialized plant/zombie paths
are retained, including repeat speed selection and unused plant launch counters.
Night boards no longer draw an unused sky countdown, and first-play level one
uses the original integer sky initializer.

A complete original-process replay is still **not established**. Particle models
remain independently seeded and advanced by the renderer; the original ten
emitter/sixteen particle interpolation samples, native creation order, fields
and cross-fades require a simulation-owned implementation. No generator is
shared from `Draw`, and no guessed replacement probabilities are added.
Debug legacy/demo and experimental level-one entry points retain their separate
fixtures. Detailed provenance, regeneration commands and exact scope are in
[`Tools/Tests/Modules/Random/README.md`](../../Tools/Tests/Modules/Random/README.md).

Focused regressions:

```powershell
./Tools/Tests/Run-UnityTests.ps1 -Platform EditMode -TestFilter 'AdventureRandomPlacementTests|AdventureSpawnFidelityTests|AdventureZombieGroanTests|AdventurePlantSoundTests|AdventureMushroomTests|AdventurePlantingEffectsTests|AdventureNativeMotionTests|AdventureCatapultTests'
# Set PVZ_BOARD_STRESS_ASSETS to the read-only original runtime resources.
./Tools/Tests/Run-UnityTests.ps1 -Platform PlayMode -TestFilter 'OriginalPuffShroomTests.NightPuffUsesOriginalHalfSizeShadow|OriginalPuffShroomTests.DayPuffUsesOriginalHalfSizeShadow|OriginalPuffShroomTests.OriginalSporeUsesNativeMuzzleAndGrowth|OriginalPuffShroomTests.FirstAttackDisplaysOriginalWindupBeforeLaunching|OriginalMushroomGraphicsTests.SleepingBodiesAndZUseTheOriginalClipsAndIndependentOffsets'
```

The checks cover integer endpoints/draw order, persistent placement, native
combat coordinates, production timers, sky interval caps and original-artwork
body/shadow/muzzle/Z rendering. They do not constitute a packaged Player
playthrough or Windows/Linux/mobile/IL2CPP device validation.


### Follow-up native randomness regressions

```powershell
./Tools/Tests/Run-UnityTests.ps1 -Platform EditMode -TestFilter 'AdventureNativeRandomTests|AdventureRandomPlacementTests|AdventureSpawnFidelityTests|AdventureZombieGroanTests|AdventureGravePresentationTests|AdventureFogTests|AdventureDancerTests|AdventurePlantSoundTests|AdventureMushroomTests|AdventureJalapenoTests|ReanimationHotPathTests|ReanimationPoseIdentityTests|ReanimationPoseBlendTests|AdventureIntroPerformanceTests|AdventureNativeMotionTests|AdventureCatapultTests'
# Set PVZ_BOARD_STRESS_ASSETS to the read-only original runtime resources.
# Set PVZ_DAVE_AUDIO_ASSETS to that directory's sounds subdirectory.
./Tools/Tests/Run-UnityTests.ps1 -Platform PlayMode -TestFilter 'OriginalNativeRandomGraphicsTests|OriginalMushroomGraphicsTests.SleepingBodiesAndZUseTheOriginalClipsAndIndependentOffsets|OriginalPuffShroomTests.FirstAttackDisplaysOriginalWindupBeforeLaunching|OriginalPlantSoundTests|OriginalZombieGroanAudioTests|CrazyDaveOpeningIntegrationTests.OriginalVoiceReachesAudioSource'
```

The native fixture export checks 24 boss machine-code vectors, 104 Foley rows
and 132 recording headers. Boss action probabilities, priorities, counters,
spawn-age gates and the twelve-entry summon table come from `551ED0`,
`550900..551260` and `747F28`. Adventure uses 40000 maximum health; challenge
35 uses 60000, with native strict eighty/fifty-percent transition thresholds.
Ball formation, ground-track movement and the one-in-ten trail gate follow
`5518B0`/`551AA0` and the original reanimation tracks. Cold counters pause
native scheduler clocks; zero-ready counters retain the original selection
priority.

Blink initialization and rejection ordering follow `46BE80`, `472670`, `472370`
and `472860`. Successful shooting ends the old blink. Sleep pauses only the
attempt clock; independent blink playback can finish. Mine arming stores its
new 400..799 countdown. Body and selected-head attachments retain their native
base pose, fifteen-FPS full-last-frame playback and unmodified source images.
`482690` keeps internal hidden frames in animation ranges, preserving multiple
nut blinks and the original Puff-shroom gap.

Foley flags, pitch ranges and weighted recordings are the original PC table.
Pitch is drawn before admission; rejected requests draw no variation. Loop,
one-at-a-time and no-repeat flags preserve their native selection behavior.
Voice retirement uses imported sample counts, native pitch/frequency limits and
a continuous opening/dialogue/combat clock. Physical device completion can
still differ and is not a full-process audio/RNG replay guarantee. The loader
accepts the original converted WAV diamond recording as well as OGG resources.

The boss driver's idle/drive/laugh clock remains independent of the body and
respects original freeze/chill rate changes. Native head-contact Foley markers
(.37/.42/.55 and leave .48/.8) consume their original request-time draws; normal
head leave does not borrow the distinct `551E00` death branch's audio.

## Original Zomboss lifecycle and rendering

`AdventureBossFidelityTests`, `OriginalBossGraphicsTests` and
`OriginalBossAudioTests` cover the original 5-10 roof opening and boss lifecycle.
The source is the supplied PC executable (SHA-256
`e1511201ec2ae9a7e30259f04bc2ec7272badb2574a2e9b405d0e916ae5c082c`)
and its analysis: `4473C0` / `PlayBossEnter`, `53DD90`, `551ED0`,
`552610`, `54D080`, `54D810`, `551E00`, `5524F0`, and `54F4A0`.
The mobile-derived source is used for navigation; PC driver offset `(28,-84)`,
initial rate 18, phase gates and frame ranges follow PC instructions/assets.

The tests check empty preview, creation at `6000 + DaveTime + LawnMowerTime`,
40-frame entrance and its .24/.79 thumps, native body overlay `(15,-8)`,
separate back/front legs and back arm, attached driver order, RV and bungee
offsets, damage images and blue glows, ball additive groups and counters,
all-row lobber targeting, lethal health-one retreat, death explosions, flag
and the award boundary. The existing 24 native boss decision vectors remain
in `AdventureNativeRandomTests` and the read-only exporter in
`Tools/Tests/Modules/Random`.

Set `PVZ_BOARD_STRESS_ASSETS` to the original imported runtime directory and run:

```powershell
./Tools/Tests/Run-UnityTests.ps1 -Platform EditMode -TestFilter 'AdventureBossFidelityTests;AdventureNativeRandomTests;AdventureRoofCombatTests'
./Tools/Tests/Run-UnityTests.ps1 -Platform PlayMode -TestFilter 'OriginalBossGraphicsTests;OriginalBossAudioTests;OriginalNativeRandomGraphicsTests;FinalBossOpeningPlaysDialogueAndSkipsItAfterRestart'
```

Graphics and audio tests require the original resources and a real graphics/audio
backend. Playback checks inspect production voices, not physical speaker output.
They do not establish packaged Player or other-platform verification. As described
in the random-module README, the existing renderer-owned particle engine still
has a separate random stream; these tests validate native boss contacts, source
resources and placements, not full-process particle RNG parity.

## PC survival directory and re-selection

`SurvivalPresentationTests`, `OriginalSurvivalGraphicsTests` and
`OriginalSurvivalFlowTests` cover the local PC survival table (all fifteen
identifiers), the eleven-cell `Survival_Thumbnails` atlas and four separate
endless images, original directory locks/caption splitting, pointer release,
terrain previews, empty first selection, retained re-selection bank, native
view-lawn timing, the 500-update handoff/music fade, persisted partial flags,
and on-lawn finite rewards. Original-art tests require `PVZ_BOARD_STRESS_ASSETS`;
`PVZ_BOARD_STRESS_OUTPUT` optionally records their real GPU captures.

The authoritative PC sources are `ui_challenge/438EE0`, `4397F0`, `439C20`,
`439CB0`, `43A780`, `43AD10`, the original table at `74E490`, resource bindings
at `4891C0`/`48B0C0`/`48B620`, the original `Layout.xml` wrap property,
`adventure_intro/446520`/`4473C0`, and chooser `499170`/`49A8D0`/`49B6D0`/
`49C110`/`49C9B0` in the supplied origin analysis. PC `41869D` sets the survival
handoff to 500 native ticks; the common PC loss sequence is `448040` and
`6EF150`. The mobile-derived C# source is used for navigation and callback
semantics; its hidden-endless table and compact mobile menu geometry are not
used. The local PC executable puts all fifteen survival entries on page zero.

```powershell
./Tools/Tests/Run-UnityTests.ps1 -Platform EditMode -TestFilter 'SurvivalPresentationTests;AdventureIntroPerformanceTests;ModeWaveFidelityTests;AdventureSeedChooserTests;HypnoCompletionTests'
./Tools/Tests/Run-UnityTests.ps1 -Platform PlayMode -TestFilter 'OriginalSurvivalGraphicsTests;OriginalSurvivalFlowTests;OriginalStormFlowTests;OriginalPoolGraphicsTests;SeedBankLayoutTests;IndependentPuzzleDialogueTests'
```

The flow fixtures use disposable profiles and the production Lawn host. GPU
assertions compare native atlas cells and actual widget geometry, while source
captures check original rendering resources. These runs do not establish
native-executable pixel equivalence, packaged Players or other target devices.

`ChallengeDirectoryTests` and `OriginalChallengeDirectoryTests` cover the PC
mini-game and puzzle directories. Evidence is the unchanged executable's table
at `0x74E490`, constructor `438EE0`, draw routines `43A780/439CB0`, and tooltip
routine `43AD10` in the supplied `pvz-origin/analysis/ida/ui_challenge` materials.
Both pages use the 22-cell `Challenge_Thumbnails` atlas, four rows at the native
119-pixel pitch, finite trophies, and independent puzzle unlock chains. Normal
pages hide the developer-only page tabs. Endless puzzle records use the original
`LONGEST_STREAK` text and do not count toward the 18 puzzle trophies.

```powershell
./Tools/Tests/Run-UnityTests.ps1 -Platform EditMode -TestFilter 'ChallengeDirectoryTests;SurvivalPresentationTests'
./Tools/Tests/Run-UnityTests.ps1 -Platform PlayMode -TestFilter 'OriginalChallengeDirectoryTests;OriginalSurvivalGraphicsTests.MenuUsesOriginalSurvivalArtworkAndWidgets;OriginalSurvivalGraphicsTests.LockedCardsAndReleaseSemanticsMatchPcDirectory'
```

The focused directory run passes 38 EditMode and four PlayMode cases with the
original artwork and a real graphics device. These are native layout/resource
checks, not executable screenshot pixel equivalence or target-device validation.

`OriginalChallengeOpeningTests` renders the original opening for all 20 mini-games
and all 20 puzzles. It checks PC `417A70/446380/4463F0/446520` chooser/scrolling
predicates, `499170`'s preselected Starfruit, plant-headed street zombies, and
Zomboss revenge allocation at the original entrance boundary. Puzzle dialogue
coverage retains the original 51/61 completion conditions; a non-dialogue board
now still owns its native short CutScene. Store returns refresh chooser capacity
and entitlements for all independent modes which choose plants.

```powershell
./Tools/Tests/Run-UnityTests.ps1 -Platform EditMode -TestFilter 'AdventureIntroPerformanceTests;ModeWaveFidelityTests;AdventureSeedChooserTests;AdventureBossTests'
./Tools/Tests/Run-UnityTests.ps1 -Platform PlayMode -TestFilter 'OriginalChallengeOpeningTests;IndependentPuzzleDialogueTests;OriginalSurvivalGraphicsTests'
```

This opening run passes 47 EditMode and 16 PlayMode cases. Packaged Player,
other target platforms and native executable pixel equivalence remain unverified.

`ChallengeSeedChooserTests` covers all five PC Last Stand exclusions, Imitater
copy restrictions, the ordered producer/pool/roof/flyer/upgrade confirmations,
and purchased access after leaving the store. `OriginalChallengeOpeningTests`
also exercises the real original-artwork copy and re-pick dialogs. Their sizing,
frame, header and buttons follow PC `464B40/464FF0/4651D0`; copy-card positions
follow `498C70`, and warning order follows `49B120/49AF50`. For the supplied
Chinese title, executing the original `464B40` instructions with original font
metrics gives the copy dialog rectangle `(147,11,506,578)`.

```powershell
./Tools/Tests/Run-UnityTests.ps1 -Platform EditMode -TestFilter 'ChallengeSeedChooserTests;AdventureSeedChooserTests;ModeWaveFidelityTests'
./Tools/Tests/Run-UnityTests.ps1 -Platform PlayMode -TestFilter 'OriginalChallengeOpeningTests;OriginalSurvivalFlowTests'
```

The chooser run passes 53 EditMode and eight PlayMode cases. The store and
almanac widgets retain their PC profile-level gates. The copy dialog previews
its target without changing the match's selected Imitater template during Draw.

Native challenge preview presentation also uses PC `430610`'s bowling/I, Zombie
stripe resource and coordinates, `4300E0`'s alpha-100 Starfruit guides, and
`436AE0`'s three white-filter cardboard edge passes. The solid source-alpha
sprite mode preserves the existing premultiplied surface and additive modes;
GPU coverage checks translucent edges and the following ordinary sprite, and
the original I, Zombie capture also exercises bite feedback without leaking tint.
Fixed introduction banks follow `49F100/427E20/49D260/49DDB0`: three slot reels
and their original overlay/attached handle, the initial empty Beghouled bank,
two aquarium purchase packets, and each I, Zombie board's actual zombie cards.
They retain the original intro bank-darken value and Pico129 packet-cost baseline.

`OriginalChallengeFlowTests` runs the production loading, directory callback,
chooser, original warning, re-pick and successful battle handoff with disposable
profiles. It checks that the unchanged `SOUND_GRAVEBUTTON` recording is loaded
for native confirmation buttons. The hidden developer page retains the existing
supported garden/tree callbacks without evaluating unsupported mode unlock rules.

```powershell
./Tools/Tests/Run-UnityTests.ps1 -Platform PlayMode -TestFilter 'OriginalChallengeOpeningTests;OriginalChallengeDirectoryTests;IndependentPuzzleDialogueTests;ObjectSpriteRendererTests;OriginalSurvivalGraphicsTests;OriginalSurvivalFlowTests;OriginalVasebreakerTests;OriginalBossGraphicsTests'
./Tools/Tests/Run-UnityTests.ps1 -Platform PlayMode -TestFilter 'OriginalChallengeFlowTests;OriginalChallengeDirectoryTests.HiddenDebugPagePreservesOnlySupportedExternalEntries'
```

Across the focused runs, 93 distinct EditMode and 93 distinct PlayMode cases
pass. The presentation change covers the directories and pre-game flows; it is
not a rewrite or full parity claim for every independent-mode combat mechanic,
mid-battle HUD or result screen. Original executable pixel equivalence, packaged
Players and other target devices remain unverified.

`PlantZombieAttachmentTests` checks the six PC plant-zombie attachment transforms.
Executing `527B50` with `(65,-5,0.2,-1,1)` yields the literal matrix
`[-0.980066597,0.198669329,65;0.198669329,0.980066597,-5]`.
The child uses the parent's ninety-pixel mind-control mirror and its actual
sampled/blended track, and is submitted after that original track even when the
original head image is hidden. The test uses labeled original-format tracks to
check matrix channels, clip-transition continuity and foreground-arm order.
Original pool waterline and cracked-helm cases retain their coverage.

```powershell
./Tools/Tests/Run-UnityTests.ps1 -Platform PlayMode -TestFilter 'PlantZombieAttachmentTests;OriginalZombiePoolGraphicsTests.PlantHeadAttachmentKeepsParentWaterlineInSceneAndCapture;ReanimationCombatAssociationTests.NormalKindPlantHeadHelmsKeepTheirCrackedArtwork'
```

Twelve distinct targeted PlayMode cases pass for this attachment repair. This is
instruction-derived geometry and actual source-command validation, not native
executable screenshot pixel equivalence or a target-device build.
