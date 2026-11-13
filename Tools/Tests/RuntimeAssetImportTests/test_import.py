"""Regression tests for the fail-fast, repeatable original runtime asset importer."""

import csv
import hashlib
import importlib.util
import json
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
import unittest

TOOL = Path(__file__).resolve().parents[2] / "PrepareRuntimeAssets.py"
SPEC = importlib.util.spec_from_file_location("runtime_asset_import", TOOL)
IMPORTER = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(IMPORTER)


class RuntimeAssetImportTests(unittest.TestCase):
    """Exercise real files, hashes, Git ignore rules, backups, and Unity GUID ownership."""

    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory(prefix="pvz-runtime-import-")
        self.root = Path(self.temporary.name).resolve()
        subprocess.run(["git", "init", "--quiet", str(self.root)], check=True, capture_output=True)
        (self.root / ".gitignore").write_text(".reports/\n")
        self.source = self.root / "source"
        self.reference = self.root / "reference"
        self.destination = self.root / "Assets" / "StreamingAssets"
        self.report = self.root / ".reports" / "import.json"
        raw = {
            "images/ad/game_pass.jpg": b"original-jpeg",
            "images/leaf.png": b"original-png",
            "reanim/plant.reanim": b"\xef\xbb\xbf<fps>12</fps>  \r\n",
            "reanim/plant.fla": b"original-flash-artwork",
            "particles/spark.xml": b"<Emitter>\r\n</Emitter>\r\n",
            "sounds/coin.ogg": b"original-ogg",
            "data/font.txt": b"original-font-description",
            "compiled/reanim/plant.reanim.compiled": b"original-compiled-animation",
            "images/cache.db": b"cache-not-for-runtime",
            "reanim/tool.jsfl": b"tool-not-for-runtime",
            "properties/partner.xml.sig": b"signature-not-for-runtime",
        }
        for name, content in raw.items():
            self.write(self.source / "raw" / name, content)
        manifest = self.source / "pak_manifest.csv"
        with manifest.open("w", encoding="utf-8-sig", newline="") as stream:
            writer = csv.DictWriter(stream, fieldnames=["extracted_path", "size_bytes", "sha256"])
            writer.writeheader()
            for name, content in raw.items():
                writer.writerow({"extracted_path": "raw/" + name, "size_bytes": len(content),
                                 "sha256": hashlib.sha256(content).hexdigest()})
        converted = {
            "rgba/images/merged.png": b"merged-original-alpha",
            "gif_png/images/single-frame.png": b"converted-original-gif",
            "fonts/Original.fnt": b"BMFont-description",
            "fonts/Original.png": b"BMFont-atlas",
            "audio/diamond.wav": b"converted-original-au",
            "exe_icons/icon.png": b"original-executable-icon",
        }
        for name, content in converted.items():
            self.write(self.source / "converted" / name, content)
        shutil.copytree(self.source / "raw", self.reference / "Raw")
        shutil.copytree(self.source / "converted", self.reference / "Converted")
        self.write(self.reference / "Manifests" / "pak_manifest.csv", manifest.read_bytes())

    def tearDown(self):
        self.temporary.cleanup()

    @staticmethod
    def write(path, content):
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_bytes(content)

    @staticmethod
    def snapshot(path):
        return {file.relative_to(path).as_posix(): (file.read_bytes(), file.stat().st_mtime_ns)
                for file in path.rglob("*") if file.is_file()}

    def run_import(self, **options):
        return IMPORTER.prepare_assets(self.source, self.reference, self.destination, self.report, **options)

    def test_dry_run_writes_only_ignored_report(self):
        report = self.run_import()
        self.assertEqual(report["mode"], "dry-run")
        self.assertEqual(report["summary"]["manifest_files_verified"], 11)
        self.assertEqual(report["summary"]["planned_assets"], 14)
        self.assertEqual(report["summary"]["authoring_art"], 1)
        self.assertFalse(self.destination.exists())
        self.assertTrue(self.report.is_file())

    def test_apply_is_idempotent_and_verification_is_completely_read_only(self):
        self.run_import(apply=True)
        before = self.snapshot(self.destination.parent)
        report = self.run_import(apply=True)
        self.assertEqual(before, self.snapshot(self.destination.parent))
        self.assertEqual(report["summary"].get("copy", 0), 0)
        self.assertEqual(report["summary"]["meta_create"], 0)
        report_before = self.snapshot(self.report.parent)
        verified = self.run_import(verify_only=True)
        self.assertEqual(verified["mode"], "verify-only")
        self.assertEqual(before, self.snapshot(self.destination.parent))
        self.assertEqual(report_before, self.snapshot(self.report.parent))

    def test_wrong_source_hash_refuses_before_any_asset_or_report_is_written(self):
        self.write(self.source / "raw/images/leaf.png", b"corrupt-source")
        with self.assertRaisesRegex(IMPORTER.ImportFailure, "SHA-256"):
            self.run_import(apply=True)
        self.assertFalse(self.destination.exists())
        self.assertFalse(self.report.exists())

    def test_wrong_reference_hash_refuses_before_mutation(self):
        self.write(self.reference / "Raw/images/leaf.png", b"corrupt-reference")
        with self.assertRaisesRegex(IMPORTER.ImportFailure, "reference"):
            self.run_import(apply=True)
        self.assertFalse(self.destination.exists())

    def test_non_whitelisted_raw_files_are_validated_but_never_copied(self):
        self.run_import(apply=True)
        for name in ("images/cache.db", "reanim/tool.jsfl", "properties/partner.xml.sig"):
            self.assertFalse((self.destination / name).exists())
        self.assertEqual((self.destination / "reanim/plant.fla").read_bytes(), b"original-flash-artwork")
        self.assertTrue((self.destination / "compiled/reanim/plant.reanim.compiled").exists())

    def test_gif_font_audio_and_icon_mappings_preserve_original_bytes(self):
        self.run_import(apply=True)
        expected = {
            "images/merged.png": b"merged-original-alpha",
            "images/single-frame.png": b"converted-original-gif",
            "fonts/Original.fnt": b"BMFont-description",
            "fonts/Original.png": b"BMFont-atlas",
            "sounds/diamond.wav": b"converted-original-au",
            "images/exe_icons/icon.png": b"original-executable-icon",
        }
        for name, content in expected.items():
            self.assertEqual((self.destination / name).read_bytes(), content)
            self.assertTrue(Path(str(self.destination / name) + ".meta").exists())
        self.assertTrue((self.destination / "fonts.meta").exists())
        self.assertTrue(Path(str(self.destination) + ".meta").exists())

    def test_text_equivalence_keeps_destination_bytes_mtime_and_guid(self):
        target = self.destination / "reanim/plant.reanim"
        self.write(target, b"<fps>12</fps>\n")
        guid = "1234567890abcdef1234567890abcdef"
        meta = Path(str(target) + ".meta")
        self.write(meta, ("fileFormatVersion: 2\nguid: " + guid + "\n").encode())
        before = (target.read_bytes(), target.stat().st_mtime_ns, meta.read_bytes())
        report = self.run_import(apply=True)
        self.assertEqual(report["summary"]["text-equivalent"], 1)
        self.assertEqual(before, (target.read_bytes(), target.stat().st_mtime_ns, meta.read_bytes()))

    def test_unapproved_content_conflict_refuses_entire_import(self):
        target = self.destination / "images/ad/game_pass.jpg"
        self.write(target, b"existing-artwork")
        before = self.snapshot(self.destination)
        with self.assertRaisesRegex(IMPORTER.ImportFailure, "replace-existing"):
            self.run_import(apply=True)
        self.assertEqual(before, self.snapshot(self.destination))
        self.assertFalse(self.report.exists())

    def test_explicit_replacement_backs_up_old_bytes_and_preserves_existing_meta(self):
        target = self.destination / "images/ad/game_pass.jpg"
        self.write(target, b"existing-artwork")
        meta = Path(str(target) + ".meta")
        original_meta = b"fileFormatVersion: 2\nguid: abcdef1234567890abcdef1234567890\nTextureImporter:\n  userData: keep\n"
        self.write(meta, original_meta)
        report = self.run_import(apply=True, replace_existing=["images/ad/game_pass.jpg"])
        replacement = next(entry for entry in report["assets"] if entry["status"] == "replace")
        self.assertEqual(Path(replacement["backup"]).read_bytes(), b"existing-artwork")
        self.assertEqual(target.read_bytes(), b"original-jpeg")
        self.assertEqual(meta.read_bytes(), original_meta)
        repeated = self.run_import(apply=True, replace_existing=["images/ad/game_pass.jpg"])
        self.assertEqual(repeated["summary"].get("replace", 0), 0)

    def test_literal_newline_metadata_is_repaired_without_changing_three_guids(self):
        self.run_import(apply=True)
        paths = [self.destination / "images/leaf.png.meta", self.destination / "reanim/plant.fla.meta",
                 self.destination / "fonts/Original.fnt.meta"]
        originals = {path: path.read_bytes() for path in paths}
        for path, content in originals.items():
            self.write(path, content.replace(b"\n", b"\\n"))
        before = self.snapshot(self.destination.parent)
        with self.assertRaisesRegex(IMPORTER.ImportFailure, "repair"):
            self.run_import(verify_only=True)
        self.assertEqual(before, self.snapshot(self.destination.parent))
        report = self.run_import(apply=True)
        self.assertEqual(report["summary"]["meta_repair"], 3)
        for path, original in originals.items():
            self.assertEqual(path.read_bytes(), original)

    def test_other_invalid_metadata_never_regenerates_an_existing_guid(self):
        target = self.destination / "images/leaf.png"
        self.write(target, b"original-png")
        self.write(Path(str(target) + ".meta"), b"fileFormatVersion: 2\nguid: broken\n")
        before = self.snapshot(self.destination)
        with self.assertRaisesRegex(IMPORTER.ImportFailure, "refusing to rebuild"):
            self.run_import(apply=True)
        self.assertEqual(before, self.snapshot(self.destination))

    def test_converted_reference_mismatch_and_unignored_report_are_rejected(self):
        self.write(self.reference / "Converted/fonts/Original.fnt", b"changed-description")
        with self.assertRaisesRegex(IMPORTER.ImportFailure, "Converted source/reference"):
            self.run_import(apply=True)
        self.report = self.root / "tracked-report.json"
        with self.assertRaisesRegex(IMPORTER.ImportFailure, "Git-ignored"):
            self.run_import(apply=True)
        self.assertFalse(self.destination.exists())

    def test_verify_only_missing_files_does_not_create_report_or_directory(self):
        with self.assertRaisesRegex(IMPORTER.ImportFailure, "Verification found"):
            self.run_import(verify_only=True)
        self.assertFalse(self.destination.exists())
        self.assertFalse(self.report.parent.exists())

    def test_cli_defaults_to_dry_run(self):
        result = subprocess.run([sys.executable, "-B", str(TOOL), "--source", str(self.source),
                                 "--reference", str(self.reference), "--destination", str(self.destination),
                                 "--report", str(self.report)], capture_output=True, text=True)
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(json.loads(result.stdout)["planned_assets"], 14)
        self.assertFalse(self.destination.exists())


if __name__ == "__main__":
    unittest.main()
