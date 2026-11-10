# Verified data catalog

The pure C# API is `PvZReplica.VerifiedData.VerifiedDataCatalog`. It has no Unity,
JSON, or gameplay dependencies. `TryGetPlant`, `TryGetZombie`,
`TryGetAdventureLevel`, `TryGetAdventureZombieRowHeader`, `TryGetChallengeMode`,
`TryGetReanimation`, and `TryGetAdventureZombieFlagRaw` look up original numeric
IDs and return `false` for IDs outside the source tables. A failed object lookup
sets its output to `null`; a failed flag lookup sets it to zero. Collections and
flag rows are read-only; names are not unique identifiers.

Plant, zombie, animation, and matrix-header IDs start at 0. Adventure level
indices and challenge mode IDs start at 1; adventure mode 0 is not a mode-table
row. Each adventure row exposes 34 raw integer flags in zombie-ID order. The
stored matrix header for zombie index 33 is 0 and is intentionally not corrected.
Unknown fields retain their raw offset-based names. Animation paths retain the
original separators and spelling.

These are static definitions, not complete level scripts or final runtime rules.
Base costs, recharge thresholds, wave counts, and zombie selection inputs can
be overridden or bypassed at runtime. A flag or weight does not guarantee or
exclude a spawn; the recharge threshold has not been verified as seconds. A mode
metadata row does not establish that a mode is accessible.

From the workspace root, generate or byte-check the C# data with PowerShell 5.1
or PowerShell 7:

```powershell
pwsh -NoProfile -File PvZUnity/Tools/GenerateVerifiedData.ps1
pwsh -NoProfile -File PvZUnity/Tools/GenerateVerifiedData.ps1 -Check
```

Use `powershell.exe` instead of `pwsh` for Windows PowerShell 5.1. The generator
checks source provenance metadata, row IDs, dimensions, and animation references;
it does not independently compare the EXE bytes. It writes only the generated C#
file, with deterministic UTF-8 (no BOM), LF line endings, and invariant numbers.
`-Check` is read-only and fails if the bytes differ. `-InputDirectory` can point
to an alternate directory containing the same five JSON filenames.

Run the independent .NET 9 console tests (no test framework packages or Unity):

```powershell
dotnet run --project PvZUnity/tests/VerifiedDataTests/VerifiedDataTests.csproj --configuration Release
```

The optional PowerShell generator regression tests create temporary fixture
copies under the test directory, then check a matching baseline and rejected
invalid input or stale output:

```powershell
pwsh -NoProfile -File PvZUnity/tests/VerifiedDataTests/GeneratorTests.ps1
```

The tests locate the original JSON under `analysis/ida/balance` by walking up
from their output directory. They compare every exposed source field and all
1,700 flags, as well as fixed values, IDs, boundaries, and read-only behavior.
Failure returns a nonzero exit code. The `.cs.txt` extension and disabled
generated assembly sources prevent the existing parent test project from
compiling the new test code or intermediate sources through its default glob.
