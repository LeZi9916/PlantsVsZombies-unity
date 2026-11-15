# Player smoke scenarios in Test Runner

The former command-line MonoBehaviour scenarios now live in
`Assets/Tests/PlayMode/Smoke`, in the existing PlayMode test assembly. They are
ordinary `[UnityTest]` cases, visible under the `PlayerSmoke` category in
Window > General > Test Runner. They do not run in a normal Player or call
`Application.Quit`. Their assertions, coroutine exceptions, unexpected Unity
errors, and cleanup are handled by the test framework.

Use Unity **7000.0.0a16** and supply the complete original runtime resources:

```powershell
$env:PVZ_BOARD_STRESS_ASSETS = (Resolve-Path Assets/StreamingAssets).Path
./Tools/Tests/PlayerSmokeTests/Run.ps1 -Unity <Unity-editor-path> -Mode FirstUser,UI,LevelOne
./Tools/Tests/PlayerSmokeTests/Run.ps1 -Unity <Unity-editor-path> -Mode Adventure
./Tools/Tests/Run-UnityTests.ps1 -Unity <Unity-editor-path> -Platform PlayMode -TestFilter PlantsVsZombies.Tests.PlayMode.Smoke
```

`Run.ps1` forwards the selected cases to the isolated Unity runner. It accepts
FirstUser, UI, LevelOne, Battle, Adventure, Feedback, Progression, IntroAudio,
and CombatFidelity. `-LogicOnly` selects the existing capture-free FirstUser,
UI, or LevelOne cases; it still uses the real graphics device. The additional
`CombatFidelityPlayerSmoke.RunsLoadingScenario` case preserves the formerly
optional loading-art checks. No prebuilt game executable is needed.

The fixture starts the production game host using the supplied resources and
fresh profile, account, and campaign files for each case. All output goes into
an ignored `.unity-runs/player-smoke/<case>-<id>` directory inside the test
project, or under `PVZ_PLAYER_SMOKE_OUTPUT` when explicitly set. It restores
previous game hosts, cursor visibility, frame rate, and background-update policy
even when a case fails. With no resource environment variable, Test Runner
reports these opt-in cases as skipped.

NUnit XML and Editor logs are produced by `Tools/Tests/Run-UnityTests.ps1`.
Screenshots retain the original checks and synchronous PNG writers. These are
production-host Editor tests; they do not verify a packaged Player, real OS
input, target-device audio, or native-game pixel equivalence. The old automatic
`-pvz-*-smoke` and `-pvz-smoke-output` Player switches have been removed. Manual
`-pvz-ui-review`, `-pvz-level-one`, and `-pvz-demo` session choices remain.
