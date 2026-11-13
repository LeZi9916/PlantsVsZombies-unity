# Original PC spawn fidelity regressions

Requires .NET SDK 9 or newer. Run from the repository root:

```sh
dotnet run --project Tools/Tests/SpawnFidelityTests/SpawnFidelityTests.csproj -c Release
```

The committed fixtures in `Tools/Tests/Modules/Spawn` were captured by executing
the hash-identified original PC x86 functions. No original executable, save file,
or external Python installation is needed to run these deterministic comparisons.

The runner checks all 50 adventure boards at three progress values with zero
tickets, including eligibility and random bounds. It also checks complete
survival/minigame plans with native local eligibility seeding, the shared MT draw
count, and dynamic candidate/limit boundaries. Zero-ticket cases are deliberately
separate from same-seed local generator comparisons.

Unity integration and runtime row selection, scheduler, balloon, and puzzle
regressions live in `Assets/Tests/EditMode`. Run those with Unity **6000.3.17f1**
using `Tools/Tests/Run-UnityTests.ps1`. These suites do not establish complete
original-process startup replay or target-platform Player behavior.
