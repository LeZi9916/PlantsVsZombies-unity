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

This is a structural regression check. Run the isolated Unity tests and Player
smoke tests to verify compilation, rendered pixels, and interaction behavior.
It does not build the root Unity-generated solution or project files.
