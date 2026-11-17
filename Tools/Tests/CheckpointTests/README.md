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

The model-pool regressions pollute all resettable types and compare every field
against construction defaults. They cover shared Kelp state, independent corpse
and impact references, Bungee cargo, nested magnetic source snapshots, bounded
preparation, unchanged checkpoint bytes/randomness, and warmed allocation-free
reference traversal and event recycling. Set `PVZ_POOL_COMPATIBILITY_SOURCE` to a
private existing match file to additionally verify its original payload schema
and preparation; the optional source is read only and is never committed or
copied over an active checkpoint.

PC save eligibility is taken from `analysis/ida/decompiled/414A00.c` (Playing scene,
excluding native modes 42, 43, 50, 71 and 72). Per-mode files follow the ownership
of PC `464510`/`45C370`; the serialized replica models use their own versioned format.

Preparation regressions also exercise production birth/hit/death/corpse/recycle
paths on an independently restored model, retain public event snapshots while
checking synchronous value notifications, and compare the live checkpoint bytes
and random continuation before and after rehearsal. The larger derived capacity
requests use a 5000-step fixture bound; the per-step construction assertion stays
at 16 and all allocation/correctness assertions are unchanged.
