#!/usr/bin/env python3
"""Verify and prepare the original PvZ runtime assets without changing source archives."""

import argparse
import csv
import hashlib
import json
import os
from pathlib import Path, PurePosixPath
import re
import shutil
import subprocess
import sys
import tempfile
import uuid
from collections import Counter

RAW_EXTENSIONS = frozenset({
    ".png", ".jpg", ".jpeg", ".gif", ".reanim", ".xml", ".txt", ".trail",
    ".compiled", ".ogg", ".au", ".mo3", ".fla",
})
TEXT_EXTENSIONS = frozenset({".reanim", ".xml", ".txt", ".trail", ".fnt"})
CONVERTED_TREES = (
    ("rgba", ""), ("gif_png", ""), ("fonts", "fonts"),
    ("audio", "sounds"), ("exe_icons", "images/exe_icons"),
)
CONVERTED_EXTENSIONS = {
    "rgba": {".png"}, "gif_png": {".png"}, "fonts": {".png", ".fnt"},
    "audio": {".wav"}, "exe_icons": {".png"},
}
GUID_PATTERN = re.compile(rb"(?m)^guid:[ \t]*([0-9a-fA-F]{32})[ \t]*\r?$")


class ImportFailure(Exception):
    """Identify a failed precondition before any destination asset is changed."""


def sha256(path):
    """Hash a file incrementally, including large original music conversions."""
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest()


def safe_relative(value):
    """Accept a canonical relative asset path without parent traversal or drive syntax."""
    path = PurePosixPath(value)
    if (not value or "\\" in value or ":" in value or path.is_absolute()
            or any(part in {"", ".", ".."} for part in value.split("/"))):
        raise ImportFailure("Unsafe relative asset path: " + value)
    return path.as_posix()


def file_index(root, skip_meta=False):
    """Index files case-insensitively without silently collapsing different source names."""
    result = {}
    if not root.exists():
        return result
    if not root.is_dir() or root.is_symlink():
        raise ImportFailure("Expected a real directory: " + str(root))
    for path in sorted(root.rglob("*")):
        if path.is_symlink():
            raise ImportFailure("Asset symlinks are not supported: " + str(path))
        if not path.is_file() or path.name == ".DS_Store" or skip_meta and path.suffix == ".meta":
            continue
        relative = path.relative_to(root).as_posix()
        key = relative.casefold()
        if key in result:
            raise ImportFailure("Case-insensitive asset collision: " + relative)
        result[key] = (relative, path)
    return result


def text_equivalent(source, destination):
    """Recognize only UTF-8 BOM, line-ending, and trailing horizontal whitespace changes."""
    if source.suffix.lower() not in TEXT_EXTENSIONS:
        return False
    def normalized(path):
        text = path.read_bytes().decode("utf-8-sig").replace("\r\n", "\n").replace("\r", "\n")
        return "\n".join(line.rstrip(" \t") for line in text.split("\n"))
    try:
        return normalized(source) == normalized(destination)
    except UnicodeError:
        return False


def ignored_report(path):
    """Require both the report and its backups to remain in an untracked, ignored Git location."""
    if path.suffix.lower() != ".json":
        raise ImportFailure("The report must have a .json extension.")
    ancestor = path.parent
    while not ancestor.exists():
        ancestor = ancestor.parent
    try:
        process = subprocess.run(["git", "-C", str(ancestor), "rev-parse", "--show-toplevel"],
                                 check=True, capture_output=True, text=True)
        repository = Path(process.stdout.strip()).resolve()
        for candidate in (path, path.parent / "backups" / "import-probe"):
            relative = candidate.relative_to(repository).as_posix()
            command = ["git", "-C", str(repository)]
            ignored = subprocess.run(command + ["check-ignore", "--no-index", "--quiet", "--", relative],
                                     capture_output=True)
            tracked = subprocess.run(["git", "--literal-pathspecs", "-C", str(repository)] + ["ls-files", "--error-unmatch", "--", relative],
                                     capture_output=True)
            if ignored.returncode != 0 or tracked.returncode == 0:
                raise ImportFailure("Report and backups must be untracked and Git-ignored: " + str(candidate))
    except (OSError, subprocess.CalledProcessError, ValueError) as error:
        raise ImportFailure("Cannot validate the ignored report location: " + str(path)) from error


def verified_sources(source, reference):
    """Verify every PAK entry against both the unpacked source and the reference archive."""
    manifest = source / "pak_manifest.csv"
    reference_manifest = reference / "Manifests" / "pak_manifest.csv"
    if not manifest.is_file() or not reference_manifest.is_file() or sha256(manifest) != sha256(reference_manifest):
        raise ImportFailure("Source and reference PAK manifests are missing or different.")
    raw = file_index(source / "raw")
    reference_raw = file_index(reference / "Raw", skip_meta=True)
    expected = set()
    verified = []
    with manifest.open(encoding="utf-8-sig", newline="") as stream:
        for record in csv.DictReader(stream):
            name = safe_relative(record.get("extracted_path", ""))
            if not name.startswith("raw/"):
                raise ImportFailure("Manifest entry is outside raw/: " + name)
            relative = name[4:]
            key = relative.casefold()
            digest = record.get("sha256", "").lower()
            if key in expected or not re.fullmatch(r"[0-9a-f]{64}", digest):
                raise ImportFailure("Duplicate or invalid manifest entry: " + name)
            try:
                size = int(record["size_bytes"])
            except (KeyError, ValueError) as error:
                raise ImportFailure("Invalid manifest size: " + name) from error
            if size < 0:
                raise ImportFailure("Invalid negative manifest size: " + name)
            expected.add(key)
            for inventory, label in ((raw, "source"), (reference_raw, "reference")):
                if key not in inventory:
                    raise ImportFailure("Missing " + label + " PAK entry: " + relative)
                path = inventory[key][1]
                if path.stat().st_size != size or sha256(path) != digest:
                    raise ImportFailure("PAK size/SHA-256 mismatch in " + label + ": " + relative)
            verified.append((relative, raw[key][1], digest))
    if not expected or set(raw) != expected or set(reference_raw) != expected:
        raise ImportFailure("Raw source/reference file inventories differ from the complete PAK manifest.")
    return verified


def source_plan(source, reference):
    """Build the original and converted asset mapping, rejecting unsupported derived inputs."""
    verified = verified_sources(source, reference)
    plan = []
    for relative, path, digest in verified:
        if path.suffix.lower() in RAW_EXTENSIONS:
            category = "authoring-art" if path.suffix.lower() == ".fla" else "raw-" + relative.split("/")[0]
            plan.append((path, relative, digest, category))
    for tree, target in CONVERTED_TREES:
        source_root = source / "converted" / tree
        reference_root = reference / "Converted" / tree
        if not source_root.is_dir() or not reference_root.is_dir():
            raise ImportFailure("Missing converted source/reference directory: " + tree)
        originals = file_index(source_root)
        references = file_index(reference_root, skip_meta=True)
        for key, (relative, path) in originals.items():
            if path.suffix.lower() not in CONVERTED_EXTENSIONS[tree]:
                raise ImportFailure("Unexpected converted asset type: " + str(path))
            digest = sha256(path)
            if key not in references or sha256(references[key][1]) != digest:
                raise ImportFailure("Converted source/reference SHA-256 mismatch: " + tree + "/" + relative)
            destination = "/".join(part for part in (target, relative) if part)
            plan.append((path, safe_relative(destination), digest, "converted-" + tree))
        if set(originals) != set(references):
            raise ImportFailure("Converted source/reference inventory differs: " + tree)
    seen = set()
    for _, relative, _, _ in plan:
        if relative.casefold() in seen:
            raise ImportFailure("Two source assets map to the same destination: " + relative)
        seen.add(relative.casefold())
    return len(verified), plan


def meta_plan(destination, assets):
    """Preserve existing GUIDs and plan only missing metadata or the known literal-newline repair."""
    paths = set()
    for path in assets:
        paths.add(Path(str(path) + ".meta"))
        parent = path.parent
        while parent == destination or destination in parent.parents:
            paths.add(Path(str(parent) + ".meta"))
            parent = parent.parent
    paths.update(destination.rglob("*.meta") if destination.exists() else ())
    repairs = []
    used = {}
    missing = []
    for path in sorted(paths):
        if path.is_symlink():
            raise ImportFailure("Metadata symlinks are not supported: " + str(path))
        if not path.exists():
            missing.append(path)
            continue
        if not path.is_file():
            raise ImportFailure("Metadata destination is not a file: " + str(path))
        original = path.read_bytes()
        broken = original.startswith((b"fileFormatVersion: 2\\n", b"fileFormatVersion:2\\n"))
        corrected = original.replace(b"\\n", b"\n") if broken else original
        matches = GUID_PATTERN.findall(corrected)
        if len(matches) != 1:
            raise ImportFailure("Existing metadata has no unambiguous GUID; refusing to rebuild: " + str(path))
        guid = matches[0].decode("ascii").lower()
        if guid in used:
            raise ImportFailure("Duplicate existing metadata GUID: " + str(path) + " and " + str(used[guid]))
        used[guid] = path
        if broken:
            repairs.append({"path": str(path), "status": "repair", "guid": guid, "_bytes": corrected})
    asset_paths = set(assets)
    for path in missing:
        guid = uuid.uuid4().hex
        while guid in used:
            guid = uuid.uuid4().hex
        used[guid] = path
        paired = Path(str(path)[:-5])
        folder = paired not in asset_paths
        content = "fileFormatVersion: 2\nguid: " + guid + "\n"
        if folder:
            content += "folderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
        repairs.append({"path": str(path), "status": "create", "guid": guid, "_bytes": content.encode()})
    return repairs


def atomic_bytes(path, content):
    """Replace one file atomically while cleaning up an incomplete temporary file."""
    path.parent.mkdir(parents=True, exist_ok=True)
    temporary = None
    try:
        with tempfile.NamedTemporaryFile(dir=path.parent, prefix="." + path.name + ".", suffix=".tmp", delete=False) as stream:
            temporary = Path(stream.name)
            stream.write(content)
        os.replace(temporary, path)
    finally:
        if temporary is not None and temporary.exists():
            temporary.unlink()


def prepare_assets(source, reference, destination, report_path, *, apply=False, verify_only=False, replace_existing=()):
    """Preflight the complete import before writing assets; verification mode never writes files."""
    if apply and verify_only:
        raise ImportFailure("--apply and --verify-only are mutually exclusive.")
    source, reference, destination, report_path = (Path(path).absolute() for path in (source, reference, destination, report_path))
    for path in (source, reference, destination, report_path):
        if path != path.resolve():
            raise ImportFailure("Paths must be canonical and must not traverse symlinks: " + str(path))
    for first, second in ((source, destination), (reference, destination), (source, reference)):
        if first == second or first in second.parents or second in first.parents:
            raise ImportFailure("Source, reference, and destination must be separate directory trees.")
    if any(root == report_path or root in report_path.parents for root in (source, reference, destination)):
        raise ImportFailure("The report must be outside the source, reference, and destination trees.")
    ignored_report(report_path)
    count, plan = source_plan(source, reference)
    existing = file_index(destination, skip_meta=True)
    approved = {safe_relative(value).casefold() for value in replace_existing}
    if approved - {relative.casefold() for _, relative, _, _ in plan}:
        raise ImportFailure("A replacement authorization does not identify a planned asset.")
    entries = []
    asset_paths = []
    for path, relative, digest, category in plan:
        target = destination / relative
        old_hash = None
        status = "copy"
        if relative.casefold() in existing:
            _, target = existing[relative.casefold()]
            old_hash = sha256(target)
            if old_hash == digest:
                status = "unchanged"
            elif text_equivalent(path, target):
                status = "text-equivalent"
            elif relative.casefold() in approved:
                status = "replace"
            else:
                raise ImportFailure("Destination content differs; use an explicit --replace-existing only after review: " + relative)
        elif target.exists():
            raise ImportFailure("Asset destination is not a file: " + str(target))
        asset_paths.append(target)
        entry = {"source": str(path.relative_to(source)), "destination": str(target.relative_to(destination)),
                 "category": category, "status": status, "source_sha256": digest, "existing_sha256": old_hash,
                 "_source": path, "_target": target}
        if status == "replace":
            backup = report_path.parent / "backups" / old_hash / target.relative_to(destination)
            if backup.exists() and (not backup.is_file() or sha256(backup) != old_hash):
                raise ImportFailure("An existing replacement backup is inconsistent: " + str(backup))
            entry["backup"] = str(backup)
        entries.append(entry)
    metadata = meta_plan(destination, asset_paths)
    if verify_only and (any(entry["status"] in {"copy", "replace"} for entry in entries) or metadata):
        raise ImportFailure("Verification found missing/different assets or metadata requiring repair.")
    if apply:
        # Every hash, conflict, and existing GUID has been checked before this first mutation.
        for entry in entries:
            if entry["status"] == "replace":
                backup = Path(entry["backup"])
                if not backup.exists():
                    atomic_bytes(backup, entry["_target"].read_bytes())
        for entry in entries:
            if entry["status"] in {"copy", "replace"}:
                atomic_bytes(entry["_target"], entry["_source"].read_bytes())
                if sha256(entry["_target"]) != entry["source_sha256"]:
                    raise ImportFailure("Copied asset failed SHA-256 verification: " + entry["destination"])
        for entry in metadata:
            atomic_bytes(Path(entry["path"]), entry["_bytes"])
    summary = dict(Counter(entry["status"] for entry in entries))
    summary.update({"manifest_files_verified": count, "planned_assets": len(entries),
                    "meta_create": sum(entry["status"] == "create" for entry in metadata),
                    "meta_repair": sum(entry["status"] == "repair" for entry in metadata),
                    "authoring_art": sum(entry["category"] == "authoring-art" for entry in entries)})
    public = lambda records: [{key: value for key, value in entry.items() if not key.startswith("_")} for entry in records]
    result = {"version": 1, "mode": "verify-only" if verify_only else "apply" if apply else "dry-run",
              "source": str(source), "reference": str(reference), "destination": str(destination),
              "summary": summary, "assets": public(entries), "metadata": public(metadata)}
    if not verify_only:
        atomic_bytes(report_path, (json.dumps(result, indent=2, ensure_ascii=False) + "\n").encode())
    return result


def main(argv=None):
    """Run the explicit CLI and return a nonzero status when preflight or verification fails."""
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--source", required=True, type=Path)
    parser.add_argument("--reference", required=True, type=Path)
    parser.add_argument("--destination", required=True, type=Path)
    parser.add_argument("--report", required=True, type=Path)
    mode = parser.add_mutually_exclusive_group()
    mode.add_argument("--apply", action="store_true")
    mode.add_argument("--verify-only", action="store_true")
    parser.add_argument("--replace-existing", action="append", default=[], metavar="RELATIVE_PATH")
    args = parser.parse_args(argv)
    try:
        report = prepare_assets(args.source, args.reference, args.destination, args.report,
                                apply=args.apply, verify_only=args.verify_only, replace_existing=args.replace_existing)
    except (ImportFailure, OSError, csv.Error) as error:
        print("Asset import refused: " + str(error), file=sys.stderr)
        return 1
    print(json.dumps(report["summary"], sort_keys=True))
    return 0


if __name__ == "__main__":
    sys.exit(main())
