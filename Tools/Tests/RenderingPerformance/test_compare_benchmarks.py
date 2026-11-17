"""Check that benchmark comparisons retain event frames and reject replay drift."""

import json
import tempfile
import unittest
from pathlib import Path

from CompareBenchmarks import compare, summarize


def document():
    """Return a small complete deterministic observation with prepared resources."""
    return {
        "SourceSha256": "source",
        "StartPayloadSha256": "start",
        "FinalPayloadSha256": "final",
        "AdvanceTicks": 6000,
        "Advancing": True,
        "UnityVersion": "7000.0.0a16",
        "GraphicsApi": "Direct3D11",
        "GraphicsDevice": "fixture",
        "Processor": "fixture",
        "InitialPool": {"Created": 100, "Active": 10, "Recycled": 20},
        "FinalPool": {"Created": 100, "Active": 12, "Recycled": 25},
        "InitialGpuBufferCreations": 2,
        "InitialInstanceBufferCreations": 2,
        "InitialMeshBufferReconfigurations": 0,
        "InitialMeshCreations": 0,
        "InitialSubmissionBindingCreations": 10,
        "InitialSameFrameBufferFallbacks": 0,
        "InitialPreparedInstances": 8192,
        "InitialInstanceGpuBytes": 2097152,
        "Samples": [
            {
                "SimulationMs": 1,
                "PresentationMs": 8,
                "SimulationAllocationEvents": 0,
                "PresentationAllocationEvents": 0,
                "SimulationAllocationBytes": 0,
                "PresentationAllocationBytes": 0,
                "DrawCalls": 20,
                "WholeFrameGcBytes": 128,
                "InstanceDraws": 10,
                "InstanceUploads": 2,
                "InstanceUploadBytes": 1000,
                "InstanceGpuBytes": 2097152,
                "PreparedInstances": 8192,
                "GpuBufferCreations": 2,
                "InstanceBufferCreations": 2,
                "MeshBufferReconfigurations": 0,
                "MeshCreations": 0,
                "SubmissionBindingCreations": 10,
                "SameFrameBufferFallbacks": 0,
                "FallbackInstances": 0,
                "Plants": 45,
                "Zombies": 74,
                "Peas": 10,
                "Elapsed": 60 + frame / 100,
                "RandomDraws": 1000 + frame,
            }
            for frame in range(100)
        ],
    }


class ComparisonTests(unittest.TestCase):
    """Exercise correctness and capacity observations independently of Unity."""

    def compare_documents(self, before, after, image_after=b"same pixels"):
        """Write isolated observations and run the production comparator."""
        with tempfile.TemporaryDirectory() as temporary:
            baseline = Path(temporary) / "baseline"
            candidate = Path(temporary) / "candidate"
            for directory, data, pixels in (
                (baseline, before, b"same pixels"),
                (candidate, after, image_after),
            ):
                directory.mkdir()
                (directory / "checkpoint.json").write_text(json.dumps(data), encoding="utf-8")
                (directory / "checkpoint.png").write_bytes(pixels)
            return compare(baseline, candidate)

    def test_event_frames_are_never_filtered(self):
        data = document()
        for sample in data["Samples"][-6:]:
            sample["PresentationMs"] = 100
            sample["PresentationAllocationEvents"] = 1
            sample["PresentationAllocationBytes"] = 128
        result = summarize(data)
        self.assertEqual(result["frames"], 100)
        self.assertEqual(result["PresentationMs"]["p95"], 100)
        self.assertEqual(result["PresentationAllocationEvents"]["total"], 6)
        self.assertEqual(result["PresentationAllocationBytes"]["total"], 768)

    def test_rng_drift_rejects_comparison(self):
        after = document()
        after["Samples"][50]["RandomDraws"] += 1
        result = self.compare_documents(document(), after)
        self.assertFalse(result["compatible"])
        self.assertIn("authoritative frame 50", result["mismatches"])

    def test_changed_pixels_are_reported(self):
        result = self.compare_documents(document(), document(), b"different pixels")
        self.assertFalse(result["identical_png_bytes"])

    def test_first_measured_frame_growth_is_counted(self):
        after = document()
        for sample in after["Samples"]:
            sample["GpuBufferCreations"] += 2
        result = self.compare_documents(document(), after)
        self.assertEqual(result["candidate"]["ResourceReuse"]["GpuBufferCreations"], 2)
        self.assertFalse(result["prepared_resources_reused"])

    def test_unavailable_counters_do_not_claim_reuse(self):
        after = document()
        del after["InitialPool"]
        del after["FinalPool"]
        del after["InitialGpuBufferCreations"]
        result = self.compare_documents(document(), after)
        self.assertFalse(result["prepared_resources_reused"])
        self.assertIsNone(result["candidate"]["Pool"]["created_during_measurement"])

    def test_framework_gc_is_kept_separate_from_scoped_allocations(self):
        result = self.compare_documents(document(), document())
        self.assertTrue(result["zero_allocation_scopes"])
        self.assertTrue(result["prepared_resources_reused"])
        self.assertEqual(result["candidate"]["WholeFrameGcBytes"]["mean"], 128)


if __name__ == "__main__":
    unittest.main()
