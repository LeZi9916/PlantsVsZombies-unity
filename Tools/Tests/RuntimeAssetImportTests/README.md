# Original runtime asset preparation

`Tools/PrepareRuntimeAssets.py` verifies the complete extracted archive against
`pak_manifest.csv` and `Assets/OriginalPvZ/Raw`, then prepares the original runtime
files and documented conversions. It uses Python 3's standard library and Git.
The original archive and reference tree are never changed.

The complete local archive currently has 3,394 verified raw entries. The runtime
plan has 3,473 files, including 151 original Flash artwork sources. Counts are
computed from the supplied manifests, rather than hard-coded into the importer.

## Mapping

| Input below `--source` | Destination below `--destination` |
| --- | --- |
| `raw/` | Original relative paths |
| `converted/rgba/`, `converted/gif_png/` | Original relative image paths |
| `converted/fonts/` | `fonts/` |
| `converted/audio/` | `sounds/` |
| `converted/exe_icons/` | `images/exe_icons/` |

Raw inputs are limited to PNG, JPG/JPEG, GIF, reanimation text, XML, TXT, trails,
compiled definitions, OGG, AU, MO3, and FLA. FLA files are reported separately as
`authoring-art`; the renderer does not load them. DB caches, JSFL tooling, signatures,
executables, and DLLs remain in the reference archive. Original `data/` fonts and
all four converted WAV files are retained. Existing music stems, `mainmusic.ogg`,
and other compatibility assets absent from the input plan remain untouched.

Converted files must also match `Assets/OriginalPvZ/Converted`. Existing Unity
metadata is never copied from either source tree, preventing duplicate GUIDs.

## Commands

Run from the repository root. The report and its sibling `backups/` directory
must be untracked and Git-ignored; `.unity-runs/` already satisfies that rule.
All preflight checks finish before the first asset or metadata change.

```sh
python3 -B Tools/PrepareRuntimeAssets.py \
  --source /Volumes/OCZ-ARC100/pvz-origin/extracted_assets \
  --reference Assets/OriginalPvZ \
  --destination Assets/StreamingAssets \
  --report .unity-runs/runtime-assets/import.json \
  --replace-existing images/ad/game_pass.jpg
```

The default is a dry run: it writes only the ignored JSON report. Add `--apply`
to execute that plan. An explicit replacement path permits only that exact asset;
other content conflicts still abort the entire preflight. Every replaced file is
backed up under `backups/<old SHA-256>/<original relative path>` before replacement.
Existing `.meta` bytes and GUIDs remain unchanged.

After applying, run the same command with `--verify-only` instead of `--apply`.
Verification writes nothing, including reports, metadata, directories, or backups.
It exits unsuccessfully if a target file or metadata pair is missing or needs repair.

UTF-8 text differences limited to BOM, line endings, or trailing spaces/tabs are
reported as `text-equivalent`; destination bytes and timestamps are preserved.
Other differences require explicit replacement. Invalid source hashes, duplicate
paths, source/reference disagreements, symlinks, ambiguous existing GUIDs, and
unapproved content conflicts abort without changing target assets.

Missing file and folder `.meta` files receive fresh UUIDs and basic Unity YAML.
Only metadata starting with the known malformed `fileFormatVersion: 2\\n` header
(or its no-space equivalent) receives a literal-newline repair; its existing GUID
is preserved. Other malformed GUIDs are refused, rather than regenerated. Open
with Unity **6000.3.17f1** afterwards to perform the normal asset import and fill
in importer-specific settings. Asset preparation does not replace that import.

The JSON report contains source/target hashes, unchanged and equivalent paths,
replacements and backups, metadata actions, categories, and the mode/summary.
An identical second apply leaves all destination bytes, metadata, GUIDs, and
modification times unchanged.

## Regression tests

```sh
python3 -B Tools/Tests/RuntimeAssetImportTests/test_import.py
```

The tests create temporary Git repositories and synthetic archives. They cover
dry-run behavior, idempotency, completely read-only verification, source/reference
hash failures, the raw whitelist and Flash sources, GIF/font/audio/icon mappings,
text equivalence, preflight conflict rejection, explicit replacement backups,
GUID preservation, three malformed literal-newline metadata repairs, and refusal
to regenerate other invalid GUIDs. No Unity Editor, original game files, external
Python packages, or network access is required for these tests.
