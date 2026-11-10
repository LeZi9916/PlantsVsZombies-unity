# Selector progression tests

`SelectorProgressRules` is a pure C# interpretation of the selector refresh and
the three mode-click branches in the local EXE. It does not read native saves,
advance levels, award unlocks, implement modes, or connect itself to the UI.
See `analysis/ida/selector_progress/REPORT.md` for addresses, field evidence,
native behavior and unresolved questions.

The host supplies a nullable `SelectorProfileFields` with actual raw values and
explicit `SelectorRuntimeFields` inputs. Each effective platform query result
must be false when the original service object is absent; otherwise it is the
result of the respective `0x651F30` call. The four-argument constructor uses one
result for all three call sites; the six-argument overload accepts separate
mode, completion-indicator and garden results. Profile and other app values are
treated as one stable snapshot; platform callback side effects and reentrant
changes are outside this model. Do not infer these inputs from the existing
replica's profile catalog, which currently contains no game progress.
The three integer unlock fields use signed `> 0`; the store field uses `!= 0`.

`SelectorProgressState` reports mode locks separately from auxiliary visibility.
`DecideModeClick` only models the callback for MiniGames, Puzzle and Survival;
the host still owns pointer hit testing, localized dialogs, screen navigation,
actual destination availability, animation and other widget state. A nonzero
input blocker corresponds to `selector+0x15C`. A completion indicator is only
eligible, not necessarily being drawn. The current `SelectorView` cannot hide
the auxiliary entries, and its `SetModeLocked` changes tint only.

Build both targets and run the standalone checks from the workspace root:

```powershell
dotnet build PvZUnity/tests/SelectorProgressTests/SelectorProgressTests.csproj -c Release
dotnet run --project PvZUnity/tests/SelectorProgressTests/SelectorProgressTests.csproj -c Release -f net9.0 --no-build
```

Optional independent differential verification requires Python 3, Unicorn
2.1.4, the local EXE with the SHA-256 recorded in the report, and the Release
`net9.0` test build. It maps the EXE into an isolated x86 emulator, supplies
mock memory and a platform-query return, skips rendering, and compares the
native instruction branches against C#. It neither starts the game nor reads or
writes its saves:

```text
python3 PvZUnity/tests/SelectorProgressTests/native_oracle.py
```

On this machine the Windows Unicorn runtime exited while executing even a
minimal x86 sample, so the successful differential run used Linux Python and
Unicorn in WSL. A native Windows Unicorn installation is not assumed to work.
The test does not exercise full UI rendering, OS authorization, persistence,
or live game transitions.
