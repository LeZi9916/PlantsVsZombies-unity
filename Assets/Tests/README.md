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


## Original mushroom sleep and Doom cell cleanup

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

```powershell
./Tools/Tests/Run-UnityTests.ps1 -Platform EditMode -TestFilter 'AdventureMushroomTests|AdventureScreenEffectsTests.DoomShroomStartsItsOriginalFuseWhenWokenAndDetonatesOnce'
# Set PVZ_BOARD_STRESS_ASSETS to the read-only original runtime resources.
./Tools/Tests/Run-UnityTests.ps1 -Platform PlayMode -TestFilter OriginalMushroomGraphicsTests
```

These focused checks do not constitute a full-level packaged Player playthrough
or validation on Windows/Linux/mobile/IL2CPP devices.
