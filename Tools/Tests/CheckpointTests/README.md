# Match checkpoint regression

Requires .NET SDK 9 or newer. Run from the repository root:

```powershell
dotnet run --project Tools/Tests/CheckpointTests/CheckpointTests.csproj -c Release
```

The fixtures compare full model graphs before and after restoration, then advance both
boards and compare their authoritative state and random continuation. They exercise
every implemented savable native mode, adventure boards, linked entities, capability
state (including cannon firing, homing projectiles and bungee cargo), profile/mode
isolation, atomic replacement, write failure and corrupt-file rejection. Saves
are written only to a disposable temporary directory. This does not replace Unity
Play Mode or IL2CPP/device verification.

PC save eligibility is taken from `analysis/ida/decompiled/414A00.c` (Playing scene,
excluding native modes 42, 43, 50, 71 and 72). Per-mode files follow the ownership
of PC `464510`/`45C370`; the serialized replica models use their own versioned format.
