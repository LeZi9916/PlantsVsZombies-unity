# Sun rules tests

From the workspace root on Windows:

```powershell
dotnet run --project PvZUnity/tests/SunTests/SunTests.csproj --configuration Release
dotnet build PvZUnity/tests/SunTests/CoreBuildProbe/CoreBuildProbe.csproj --configuration Release
```

The first command runs an isolated C# test executable and compares it with the instructions in the local `PlantsVsZombies.exe`. It requires the .NET 9 SDK, WSL with Python 3, and the existing Linux Unicorn dependency at `analysis/ida/plant_timing/vendor`; it does not download dependencies. The default WSL distribution is `Ubuntu-24.04`; set `SUN_TEST_WSL_DISTRO` to use another distribution. On Linux it invokes `python3` directly with the same local dependency.

The second command builds only the independent rules as C# 7.3 / .NET Standard 2.1. This is a compatibility check, not a Unity Player or scene integration test.

The machine oracle verifies the executable hash and isolates its dependencies with explicit hooks. Its tested paths, caveats, and the exact behavioral boundaries are documented in `analysis/ida/sun/README.md`.
