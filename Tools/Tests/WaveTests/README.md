# Original PC wave rule regressions

Requires .NET SDK 9 or newer. Run from the repository root:

```sh
PVZ_ORIGIN_ROOT=/path/to/pvz-origin dotnet run --project Tools/Tests/WaveTests/WaveTests.csproj -c Release
```

The read-only original directory must contain `PlantsVsZombies.exe` and
`analysis/ida/balance`. The executable and exported-table hashes must match the
checked-in machine-code fixture. A sibling `pvz-origin` directory is discovered
automatically. The test fixture stays in this repository.

These tests cover local eligibility, flag predicates, base budgets, flag prefill,
and candidate selection. They do not prove full startup or same-seed game replay.
