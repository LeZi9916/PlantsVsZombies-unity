# Original PC random generator regressions

Requires .NET SDK 9 or newer and Python with Unicorn 2.1.4. Run from the repository root:

```sh
PVZ_ORIGIN_ROOT=/path/to/pvz-origin PVZ_ORACLE_PYTHON=/path/to/python dotnet run --project Tools/Tests/RandomTests/RandomTests.csproj -c Release
```

The original directory contains the hash-matched `PlantsVsZombies.exe` and
`analysis/ida/random/machine_oracle.py`. It is read-only. A sibling `pvz-origin`
directory is also discovered automatically. `PVZ_ORACLE_PYTHONPATH` optionally
supplies a compatible Python dependency directory; a foreign-platform vendor
wheel is never selected implicitly. Windows retains its existing WSL fallback
unless `PVZ_ORACLE_PYTHON` is explicitly set.

The test executes isolated original x86 generator instructions and checks integer
outputs, remainder selection and zero-seed behavior. It does not validate the
initialization or random consumption order of a complete game.
