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
