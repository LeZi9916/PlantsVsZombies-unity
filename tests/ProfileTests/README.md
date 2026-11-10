# Replica profile catalog

This is an independent replica-only format, not a Plants vs. Zombies original
save, importer, exporter, or compatibility layer. It is not connected to
`PvZGame` or any UI. The game has no implemented progress to persist.

## Public API

Namespace: `PvZReplica.Profiles`.

```csharp
var catalog = new ProfileCatalog(filePath);
IReadOnlyList<UserProfile> profiles = catalog.Profiles;
Guid? selectedId = catalog.SelectedId;

UserProfile created = catalog.Create("Alice"); // creates and selects
UserProfile renamed = catalog.Rename(created.Id, "Bob");
catalog.Select(renamed.Id);
catalog.Delete(renamed.Id);
```

`UserProfile` exposes getter-only `Guid Id`, `string Name`, and
`int AdventureLevel`. The level is always 1: loading rejects any other value,
and there is no API to set progress. IDs are randomly generated GUIDs and do
not change on rename or reload. `Profiles` is a read-only snapshot in creation
order; existing snapshots do not change after subsequent operations. If the
selected profile is deleted, the first remaining profile is selected, or the
selection becomes null when none remain. An unknown ID raises
`KeyNotFoundException`.

Names are trimmed, must contain 1 through `MaxNameLength` (32) UTF-16 code
units, must be valid Unicode without control characters, and must be distinct
under `StringComparison.OrdinalIgnoreCase`. They are not Unicode-normalized.
An invalid or duplicate name raises an `ArgumentException` (or
`ArgumentNullException`). The catalog holds at most `MaxProfiles` (1024)
profiles; creating more raises `InvalidOperationException`.

Construction reads the supplied path without creating files. A missing file
means an empty catalog. Its parent directory must already exist when saving;
the implementation does not create it. The binary file has a replica-specific
magic, a version, bounded fields, and a SHA-256 corruption check. The checksum
is not authentication. A malformed, oversized, or unsupported-version file
raises `InvalidDataException` and is not rewritten. No original-save support or
upgrade from unknown versions is implied.

## Save and recovery

Each mutation persists before publishing a new in-memory snapshot. A same-folder
exclusive `filePath + ".lock"` sidecar coordinates participating catalog
instances; it is not truncated or deleted, even if it already exists. After
checking that the file still matches the loaded bytes, the catalog writes a
unique same-directory temporary file and flushes it to disk. It then uses
`File.Move` without overwrite for a new catalog or `File.Replace` for an existing
one. Each replacement reserves a unique `filePath + ".backup-<guid>"` and
retains the old file there. No non-atomic delete-and-move fallback is used if
the platform or filesystem does not support replacement. A no-op rename or
selection still verifies the disk but creates no backup.

If a save raises an error, the catalog rereads the target while holding the
lock. If it still matches the old bytes, the original error is propagated and
the old snapshot remains usable. If it matches the new bytes, the new snapshot
is published and the operation returns successfully. If the target is missing
and the reserved backup matches the old bytes, the old file is restored via a
new temporary file and a non-overwriting move; the original error is propagated.
Any unexpected or unreadable outcome is reported as an `IOException` and the
instance is disabled. Lock acquisition failure or an externally changed file
also disables the instance. Accessing either property or calling a mutation on
a disabled instance raises `InvalidOperationException`; reopen a catalog from
disk and inspect backups when necessary. No unexpected target file is
automatically overwritten during recovery.

Backups are intentionally **not cleaned up automatically**. They consume disk
space and can retain names of profiles deleted from the current catalog. A
failed save may also leave an empty reserved backup, and abrupt termination or
cleanup failure may leave a temporary file. Users must decide how to retain or
remove these sidecars. Startup does not automatically replace a missing,
corrupt, or unknown-version target with a backup.

The locking and stale-write check coordinate only participants using the same
path and protocol. Snapshots are not automatically refreshed, and an external
writer that ignores the lock can race the final replacement; use an isolated
path. Atomicity and durable flushes depend on the operating system and
filesystem; flushing the file does not guarantee directory metadata survives
every power failure. The code does not claim authentication, encryption, or
crash-proof transactional storage.

## Tests

From this directory, with the .NET 9 SDK installed:

```powershell
dotnet build ProfileTests.csproj -c Release
dotnet run --project ProfileTests.csproj -c Release -f net9.0 --no-build
```

The build also compiles the production sources against .NET Standard 2.1 with
C# 7.3. The executable uses no test framework or third-party dependencies. It
tests real files, replacement/lock failures, injected commit-boundary failures,
recovery, corrupt and unknown formats, stale writers, and cross-process locking.
