# Player smoke verification

Run from `PvZUnity/` in an interactive Windows desktop, after building the Player:

```powershell
& .\Tools\Tests\PlayerSmokeTests\Run.ps1
```

The default run launches five separate visible Direct3D 11 windows in sequence:
first-user creation, UI navigation, experimental 1-1, the legacy battle/audio smoke, and the new Adventure campaign.
It requests an 800x600 viewport. Each test uses the existing isolated temporary
profile mechanism. Screenshots, Player logs, stdout/stderr, executable and script
assembly hashes, exit codes, dimensions, and image hashes go to a fresh
`Build/SmokeTests/<local-time>-<random-id>/` directory. `results.json` is written
on success or failure. There are 48 expected PNGs in a successful default run (21 from UI navigation and 22 from Adventure).

A PASS log marker is insufficient: a nonzero process exit code fails the run,
including a native crash after the game has printed its success marker. Each
expected PNG must decode, match the requested dimensions, and contain more than
one percent nonblack samples. This detects empty captures; it does not prove
visual fidelity. Rendering is real, while gameplay/name-input events inside the
automated smoke scripts are still invoked directly by the test.

For a wider viewport or a subset, invoke the script directly from PowerShell:

```powershell
& .\Tools\Tests\PlayerSmokeTests\Run.ps1 -Width 1280 -Height 720 -Mode UI,LevelOne
& .\Tools\Tests\PlayerSmokeTests\Run.ps1 -Mode FirstUser,UI,LevelOne -LogicOnly
```

`-LogicOnly` uses a hidden `-batchmode -nographics` Player, expects no screenshots,
and cannot run `Battle`, which has no logic-only switch. This mode verifies only
internal interactions. A timeout stops only the process started by this runner.
Existing editors and Players are never stopped. No retries turn failures into a
PASS; all runs retain their own results. `-TimeoutSeconds` defaults to 120.

For manual checks with real OS mouse/keyboard input, run:

```powershell
& .\Build\PvZUnity.exe -pvz-ui-review -force-d3d11 -screen-fullscreen 0
```

`-pvz-ui-review` creates a separate temporary profile without starting automated
navigation. Settings changes in this mode stay in memory. Add `-pvz-level-one`
to inspect the experimental first level. Explicit Tools/Tests/review keep updating
when their window loses focus; normal gameplay keeps its existing setting.

The runtime flag `-pvz-smoke-output <directory>` redirects only explicit smoke
screenshots. Without the flag, existing direct smoke commands still write next
to the executable. The runner uses a unique subdirectory for each test.

A native EXE comparison is a separate task: use matching scene/state/viewports
and `Tools/CompareUiScreenshots.ps1`. The report always records
`NativeVisualComparison: false`. Do not call these screenshots an original-game
baseline or infer pixel equivalence from a successful Player run.

The UI test visits all 49 plant and 26 zombie entries, checks retained category
selections and release/cancel semantics, and verifies that closing a modal
almanac restores the options draft while combat stays suspended. Dedicated
PNGs cover Threepeater, Tall-nut, Coffee Bean, Gatling Pea and Dr. Zomboss.
Almanac resource or animation diagnostics fail the run.

The Adventure test is enabled by -pvz-adventure-smoke and keeps its profile/checkpoint in a temporary location. It earns a first-level win using seed/board/sun inputs, clicks reward collection, reopens the saved checkpoint, continues to 1-2 and restarts without losing progress. Separate isolated test boards cover choosing, shovel teaching, bowling, hammer, six-row water support, vase opening, conveyors, fog/storm, roof and boss rendering. The test uses no forced victory, enemy-health edits or bonus sun. Source-pixel visual fidelity and real OS input are separate verification concerns. Use -Mode Adventure to run this slice; it does not support -LogicOnly.

The Adventure slice also grants two explicit test entitlements to check Imitater target selection, target pricing, a delayed transformation and GPU grayscale output. It separately checks actual Marigold production and coin collection. This does not test or imply a completed shop/purchase flow; upgrade entitlements and wallets are not yet persisted.

Legacy screenshot writers now use CaptureScreenshotAsTexture after EndOfFrame, encode to PNG and finish the file write before image reads. The old asynchronous filename-exists check could race Unity's still-open writer; the failed 20261109-194806 run is retained as failure evidence.
