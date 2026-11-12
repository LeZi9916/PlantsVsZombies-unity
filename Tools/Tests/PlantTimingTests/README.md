# Plant timing tests

From the workspace root:

```powershell
dotnet run --project PvZUnity/Tools/Tests/PlantTimingTests/PlantTimingTests.csproj
dotnet build PvZUnity/Tools/Tests/PlantTimingTests/CoreBuildProbe/CoreBuildProbe.csproj
```

The test runs the independent C# model against the local EXE's x86 instructions. The second command compiles only the model as C# 9.0 / .NET Standard 2.1; it does not add a compatibility layer. On Windows the test uses the bundled Linux Unicorn dependency through WSL `Ubuntu-24.04`; set `PLANT_TIMING_WSL_DISTRO` to choose another installed distribution. The EXE hash, injected dependencies, exact scope and limitations are documented in `analysis/ida/plant_timing/README.md`.
