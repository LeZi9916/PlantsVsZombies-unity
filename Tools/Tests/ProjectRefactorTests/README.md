# Source migration regression

Run from the repository root:

```powershell
./Tools/Tests/ProjectRefactorTests/Run.ps1
```

The test invokes the source migration tool from its SDK-pinned directory. It
creates a disposable fixture under the ignored `artifacts/` directory, verifies
that dry run leaves its source unchanged, applies the migration, and compiles and
runs the migrated fixture using its standalone .NET 9 project.

Coverage includes symbol-aware private field and local renames, merging partial
methods, extracting a public nested struct and updating its qualified references,
preserving the primary script GUID over a fragment GUID, preserving unrelated
source, and preserving runtime behavior. No Unity project or production assets
are modified by this test.

A separate unresolved-reference fixture verifies that import cleanup preserves
external namespace imports when the required assembly metadata is unavailable.
