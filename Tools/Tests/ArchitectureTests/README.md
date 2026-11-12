# Architecture regression checks

Requires .NET SDK 9 or newer. Run from the repository root:

```powershell
dotnet run --project Tools/Tests/ArchitectureTests/ArchitectureTests.csproj -c Release
dotnet run --project Tools/Tests/ArchitectureTests/ArchitectureTests.csproj -c Release -- --self-test
```

The scanner uses the pinned Roslyn parser package. It checks production namespaces,
single-file type ownership, public nested types, partial declarations, nullable
directives, braces, line endings, script metadata and duplicate GUIDs. It also
resolves the configured URP pipeline and renderer asset GUIDs, checks quality
overrides, and detects immediate GL rendering that bypasses attached renderers.
Positive and negative syntax fixtures run before every repository scan.

A coordinator size check keeps `PvZGame.cs` under 700 lines so new responsibilities are
added to a collaborator (audio, input, menus, adventure, modes, store, garden, tree)
instead of accumulating in the game coordinator.

Presentation boundary checks reject runtime `OnGUI` callbacks, `GUI`, `GUILayout`
and `GUIUtility` accesses (including aliases and static imports), and
`Event.current` input. They also keep drawing methods and direct textured-quad
submissions outside `PvZGame`. Passive `GUIStyle` layout data remains valid;
scene renderers, Canvas presentation, and coordinator delegation are covered by
positive fixtures. These source checks protect ownership boundaries, not pixels
or event ordering.

This is a structural regression check. Run the isolated Unity tests and Player
smoke tests to verify compilation, rendered pixels, and interaction behavior.
It does not build the root Unity-generated solution or project files.
