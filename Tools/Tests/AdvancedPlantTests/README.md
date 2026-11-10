# Advanced plant simulation tests

Run from the `PvZUnity` directory:

```powershell
dotnet run --project Tools/Tests/AdvancedPlantTests/AdvancedPlantTests.csproj -c Release
dotnet run --project Tools/Tests/AdvancedPlantTests/LegacyRegression/LegacyRegression.csproj -c Release
```

The first executable tests the extended plants, including the shared damage model's
documented differences from the executable. The second compiles the existing
`Tools/Tests/Program.cs` unchanged with all current plant partials and verified data.
Both run without Unity or a test-framework package. They validate simulation
events and flame state, not Unity rendering, animation playback or audio output.

The test source uses a `.cs.txt` extension to keep it separate from projects
that recursively include `*.cs` files. The original parent test project also
includes `Assets/Scripts/Plants/*.cs` and can run its existing tests directly.
