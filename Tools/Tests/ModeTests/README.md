# Mode simulation regressions

Requires .NET SDK 9 or newer. Run from the repository root:

```powershell
dotnet run --project Tools/Tests/ModeTests/ModeTests.csproj -c Release
```

This is the original mode core probe relocated from the Unity Assets tree.
It covers survival persistence, puzzle actions, slot and aquarium rules,
unlock gates, native wave timing, and rewards using the production simulation
sources. It does not start Unity or prove rendered or interactive behavior.
Build output stays in the ignored `bin` and `obj` directories beside this test.
