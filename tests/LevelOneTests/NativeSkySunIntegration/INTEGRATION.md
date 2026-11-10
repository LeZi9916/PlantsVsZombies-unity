# Experimental 1-1 native sky-sun integration

## Activation and API

This is an opt-in path for `GameRules.EnableExperimentalLevelOne == true`:

```csharp
var options = new ExperimentalLevelOneOptions {
    UseNativeSkySunRules = true,
    SecondsPerNativeTick = 0.01
};
var game = new GameSimulation(new GameRules {
    EnableExperimentalLevelOne = true,
    LevelOneOptions = options
});
```

The option and callback references are captured during construction. When the option is false, ordinary demo and existing experimental sky rules retain their previous behavior. When true, `SkySunInterval`, `SkySunTargetY`, and `SkySunFactory` do not schedule native sky suns, including when `SkySunInterval` is zero; the existing finite-value validation of `SkySunTargetY` still applies. Existing plant suns and manually added legacy suns remain in `game.Suns` and keep the existing `CollectSun` behavior. This integration never calls `SunPlantRules` and does not replace `PlantTiming`.

`game.LevelOneNativeSkySuns` is a read-only list of live `LevelOneNativeSkySun` references. `PixelX` and `PixelY` are native 800x600-space top-left coordinates, with positive Y downward; `LandingPixelY`, `Kind`, `GroundAgeTicks`, `FadeTicksRemaining`, `IsCollecting`, and `IsDead` expose state. The base native object dimensions are `BaseWidthPixels` and `BaseHeightPixels`, each 60. These are native object bounds, not a guarantee about the size of its rendered animation.

The host converts pointer coordinates to **native integer pixels** and chooses which object to target. `HitTestLevelOneNativeSkySun(sun, x, y)` performs only the single-object geometric test; it remains queryable while paused and does not resolve overlaps. `TryClickLevelOneNativeSkySun(sun, x, y, button)` requires a live object owned by this simulation, a playing match and a nonnegative button. It starts flight, not immediate credit. At arrival the simulation updates `SunAmount`, removes the object and raises `GameEvent.SunCollected` once, including when the balance is already at the cap. A retained object reference reports `IsDead` after removal but cannot be clicked again.

`LevelOneUsesNativeSkySunRules`, `LevelOneNativeSkySunCountdownTicks`, and `LevelOneNativeSkySunDropCount` expose the captured mode and scheduler state; the two counters are null when disabled. The subsequent `PvZGame` experimental entry enables this option, draws the separate list, and routes logical pointer coordinates to it. Its choice of a 60x60 animation destination and linear fade opacity is provisional; no original-frame comparison establishes those pixels.

## Scope and host assumptions

- First-play mode and background are supplied as numeric mode `0` and background `0`. The host treats board `+0x5624` as false, `+0x561C` as 0, `+0x5618` as -1, all four additional sky-blocking predicate results as false, and the special hit-box expansion and extended lifetime flag as false. `State`, tutorial stage and `Plants.Count` are the only modeled dynamic gates. This does not reproduce all native scene, challenge, end-of-level or automatic-collection conditions.
- The tutorial uses `Plants.Count` as an approximation for native `Board+0xD4`; native allocation/removal timing may differ. The verified tutorial effect overwrites the current countdown with 200 without resetting the previous drop count. Stage 1/2 with no plants freezes the scheduler; stage 3 does not.
- `SecondsPerNativeTick` is a configurable **host conversion assumption**, not a verified EXE wall-clock rate. The host runs a fixed 0.01-second simulation step and accumulates native sky ticks, allowing at most 1024 native updates per simulation step. Consequently values below `0.01 / 1024` seconds are rejected only when native sky rules are enabled. Other simulation systems, including `PlantTiming`, still run on their existing fixed steps, so non-default conversions do not establish original relative timing.
- Existing native sky objects update before a new sky spawn. This matches the examined `0x00420E45 -> 0x0041F1F0 -> 0x0043CC10` ordering before `0x00421031 -> 0x0041FB40`; a newly spawned object does not move in its creation **native tick**. The surrounding host still runs its existing `UpdatePlants` after sky processing, not the full native board order. With multiple native substeps, a new object may move in a later native tick within the same 0.01-second host step.
- Default draws use the simulation's shared `System.Random`, not the original RNG. `NativeSkySunRandomDraw`, if supplied, replaces only these sky draws and must return an integer in `[0, bound)`; invalid results throw. It may run during construction or `Update`; a failing update is not a transaction and must not be assumed to have rolled back all simulation time. The host requests bounds 276 at initialization, then 550, 275, and 250 for each spawn. The first two per-spawn draws reproduce the observed scheduler order, but this integration assumes the 250 landing draw immediately follows them. Native object creation and other systems may consume intervening RNG draws, and the host's shared RNG ordering differs from the EXE.
- For legacy `SunAmount` values above 9990, an uncollected native object does not change the balance; an actual native credit clamps it to 9990. The native achievement-check signal is not wired to an achievement system. If a collection event callback pauses during a large update, remaining time from that update is discarded rather than replayed after resume.
- The original rule functions and their individual x86 comparisons are documented separately in `analysis/ida/sun/README.md`. The later Unity Player logic smoke exercises pointer routing and delayed credit, but captures no image. This integration's scene gates, RNG sequence, cross-system ordering, tick conversion and visual output are not established by that smoke test.

## Test

From the workspace root:

```powershell
dotnet run --project PvZUnity/tests/LevelOneTests/NativeSkySunIntegration/NativeSkySunIntegration.csproj --configuration Release
dotnet build PvZUnity/tests/LevelOneTests/UnityCoreCompile/UnityCoreCompile.csproj --configuration Release
dotnet run --project PvZUnity/tests/LevelOneTests/LevelOneTests.csproj --configuration Release
dotnet run --project PvZUnity/tests/LevelOneTests/FullSimulationRegression/FullSimulationRegression.csproj --configuration Release
dotnet run --project PvZUnity/tests/LevelOneTests/FullAdvancedRegression/FullAdvancedRegression.csproj --configuration Release
```

The integration suite checks opt-in isolation, tutorial freezing and both initial and repeated 200-tick overrides, later spawn intervals and draw order, pixel hit boundaries, falling and landing, delayed credit, pause/resume, expiry and clickable fade, terminal states, configurable clock conversion, frame chunking, cap handling and continued use of existing `PlantTiming`. The .NET Standard build is a C# 7.3 compatibility check, not a Unity Editor or Player build.
