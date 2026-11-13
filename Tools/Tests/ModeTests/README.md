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

The focused advice regression checks all 55 mode introductions, suppresses repeated
survival-stage tutorials, and resolves controlled slot results through the production
slot update. Advice uses the original bracketed LawnStrings keys; modes without an
original introduction keep an empty advice string.

The native wave regression additionally checks local survival eligibility, zero-weight
slots, the doubled later-stage budget, the 18-to-50-flag gate curve, dynamic weights
and quotas, forced Columns waves, and the original 40-wave mode lengths. Full
machine-code-derived plan and candidate fixtures run in `SpawnFidelityTests`.
