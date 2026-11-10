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
tests render synthetic textures through the actual URP camera and compare
colors, transparency, overlap order, transforms, object reuse, and disposal.
Canvas tests interleave artwork with scene meshes, clip transformed groups,
retire stale graphics, and retain editable text/focus across presentation frames.
These checks do not replace scene interaction or target-platform Player tests.
