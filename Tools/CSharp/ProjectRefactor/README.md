# Project source migration

This standalone .NET 9 tool plans or applies the project's namespace, type-file,
partial-declaration and private-identifier migration. It uses the Roslyn libraries
bundled with SDK 9.0.316; it does not build Unity-generated IDE projects.

Run commands **from this directory** so `global.json` selects the supported SDK:

```powershell
dotnet run --project ProjectRefactor.csproj -- ../../..
dotnet run --project ProjectRefactor.csproj -- ../../.. --apply
dotnet run --project ProjectRefactor.csproj -- ../../.. --cleanup-imports --apply
dotnet run --project ProjectRefactor.csproj -- ../../.. --layout --apply
```

The first command validates syntax and writes only the ignored report
`Temp/ProjectRefactor/migration-plan.json`. Review the report and Git status before
using `--apply`. Apply merges partial declarations, extracts top-level types and
public nested types into individual files, maps the project namespace roots to
`PlantsVsZombies`, and transfers existing primary-script `.meta` GUIDs. Simulation,
game entry point and diagnostic components get separate responsibility folders.

Private fields, internal fields, locals and parameters are renamed using their
original Roslyn symbols. The tool expands embedded control-flow statements into
blocks, chooses `var` only when the inferred type is identical, preserves existing
XML comments, and documents declarations that previously lacked XML comments.

The newest `Library/Bee/**/PlantsVsZombies.Runtime.rsp` and
`PlantsVsZombies.Editor.rsp` supply metadata references and conditional compilation
symbols. Projects not yet migrated to asmdefs fall back to the corresponding
`Assembly-CSharp` response files. The tool excludes the project's own output DLLs
from metadata references because those types are already supplied as source.
Only metadata is read: Unity remains responsible
for compiling the actual project. The migration must be followed by Unity
compilation and regression tests. Nullable flow semantics and readable member
documentation still require review after this mechanical migration.

`--cleanup-imports` removes imports reported as unnecessary by the C# compiler;
when metadata is missing or compilation reports errors, it only considers imports
bound to resolved project namespaces and preserves external imports for review.
`--layout` expands long initializers and removes excess blank lines without
changing expressions. The layout-only command skips generated data and the
rendering implementation so those maintain their own authoring workflows.

Run the isolated migration regression with
`../../../Tools/Tests/ProjectRefactorTests/Run.ps1`. It validates qualified nested
type references, partial implementations, source-symbol renames, asset GUID
preservation and executable behavior.

The tool never modifies generated root projects, third-party source, runtime data,
or the `Library` directory. Test source links and asset-generation scripts are
maintained separately as part of the migration review.
