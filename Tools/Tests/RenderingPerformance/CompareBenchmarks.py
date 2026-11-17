"""Compare complete checkpoint benchmark observations without dropping event frames."""

from __future__ import annotations

import argparse
import hashlib
import json
import math
from pathlib import Path
from statistics import mean


def summarize(document: dict) -> dict:
    """Summarize unfiltered samples with explicit nearest-rank percentiles."""
    samples = document["Samples"]
    if not samples:
        raise ValueError("A benchmark must contain measured frames.")
    result: dict = {"frames": len(samples)}
    for field in ("SimulationMs", "PresentationMs", "DrawCalls", "WholeFrameGcBytes"):
        values = sorted(sample[field] for sample in samples)
        result[field] = {
            "mean": mean(values),
            "p95": values[math.ceil(len(values) * .95) - 1],
            "p99": values[math.ceil(len(values) * .99) - 1],
            "max": values[-1],
        }
    sprite_draws = sorted(sample.get("SubmittedSceneSpriteDrawCalls", -1) for sample in samples)
    result["SubmittedSceneSpriteDrawCalls"] = {
        "available": sprite_draws[0] >= 0,
        "mean": mean(sprite_draws),
        "p95": sprite_draws[math.ceil(len(sprite_draws) * .95) - 1],
        "max": sprite_draws[-1],
    }
    for field in ("SimulationAllocationEvents", "PresentationAllocationEvents"):
        values = [sample[field] for sample in samples]
        result[field] = {
            "total": sum(values),
            "allocating_frames": sum(value > 0 for value in values),
            "max": max(values),
        }
    result["UpdateAndPresentationMs"] = mean(
        sample["SimulationMs"] + sample["PresentationMs"] for sample in samples
    )
    work = sorted(sample["SimulationMs"] + sample["PresentationMs"] for sample in samples)
    result["UpdateAndPresentationDistributionMs"] = {
        "mean": mean(work),
        "p95": work[math.ceil(len(work) * .95) - 1],
        "p99": work[math.ceil(len(work) * .99) - 1],
        "max": work[-1],
    }
    for field in ("SimulationAllocationBytes", "PresentationAllocationBytes", "InstanceUploads", "InstanceUploadBytes"):
        values = [sample.get(field, -1) for sample in samples]
        result[field] = {"available": all(value >= 0 for value in values)}
        if result[field]["available"]:
            result[field].update(total=sum(values), mean=mean(values), max=max(values))
    for field in ("InstanceDraws", "InstanceGpuBytes", "PreparedInstances", "GpuBufferCreations", "FallbackInstances"):
        values = [sample.get(field, -1) for sample in samples]
        result[field] = {"available": all(value >= 0 for value in values)}
        if result[field]["available"]:
            result[field].update(first=values[0], last=values[-1], min=min(values), max=max(values), mean=mean(values))
    initial = document.get("InitialPool", {})
    final = document.get("FinalPool", {})
    result["Pool"] = {"initial": initial, "final": final}
    result["Pool"]["created_during_measurement"] = (
        final["Created"] - initial["Created"]
        if initial.get("Created", -1) >= 0 and final.get("Created", -1) >= 0 else None
    )
    result["ResourceReuse"] = {}
    for initial_field, frame_field in (
        ("InitialGpuBufferCreations", "GpuBufferCreations"),
        ("InitialPreparedInstances", "PreparedInstances"),
        ("InitialInstanceGpuBytes", "InstanceGpuBytes"),
        ("InitialInstanceBufferCreations", "InstanceBufferCreations"),
        ("InitialMeshBufferReconfigurations", "MeshBufferReconfigurations"),
        ("InitialMeshCreations", "MeshCreations"),
        ("InitialSubmissionBindingCreations", "SubmissionBindingCreations"),
        ("InitialSameFrameBufferFallbacks", "SameFrameBufferFallbacks"),
    ):
        values = [sample.get(frame_field, -1) for sample in samples]
        start = document.get(initial_field, -1)
        result["ResourceReuse"][frame_field] = (
            max(values) - start if start >= 0 and all(value >= 0 for value in values) else None
        )
    result["EditorHierarchy"] = {}
    for initial_field, frame_field in (
        ("InitialHierarchyLabelMisses", "HierarchyLabelMisses"),
        ("InitialHierarchyActorCreations", "HierarchyActorCreations"),
        ("InitialHierarchySpriteCreations", "HierarchySpriteCreations"),
    ):
        start = document.get(initial_field, -1)
        values = [sample.get(frame_field, -1) for sample in samples]
        result["EditorHierarchy"][frame_field] = (
            max(values) - start if start >= 0 and all(value >= 0 for value in values) else None
        )
    result["CumulativeWork"] = {}
    for initial_field, frame_field in (
        ("InitialMeshUploadBytes", "MeshUploadBytes"),
        ("InitialTextureBankSplits", "TextureBankSplits"),
        ("InitialBlendSplits", "BlendSplits"),
        ("InitialPainterOrderSplits", "PainterOrderSplits"),
        ("InitialCapacitySplits", "CapacitySplits"),
    ):
        start = document.get(initial_field, -1)
        end = samples[-1].get(frame_field, -1)
        result["CumulativeWork"][frame_field] = end - start if start >= 0 and end >= 0 else None
    return result


def compare(baseline: Path, candidate: Path) -> dict:
    """Check deterministic identity before comparing CPU, allocation, and draw observations."""
    before = json.loads((baseline / "checkpoint.json").read_text(encoding="utf-8-sig"))
    after = json.loads((candidate / "checkpoint.json").read_text(encoding="utf-8-sig"))
    fields = (
        "SourceSha256", "StartPayloadSha256", "FinalPayloadSha256", "AdvanceTicks", "Advancing",
        "UnityVersion", "GraphicsApi", "GraphicsDevice", "Processor",
    )
    mismatches = [field for field in fields if before[field] != after[field]]
    if len(before["Samples"]) != len(after["Samples"]):
        mismatches.append("frame count")
    for index, (left, right) in enumerate(zip(before["Samples"], after["Samples"])):
        if any(left[field] != right[field] for field in ("Plants", "Zombies", "Peas", "Elapsed", "RandomDraws")):
            mismatches.append(f"authoritative frame {index}")
            break
    images_equal = hashlib.sha256((baseline / "checkpoint.png").read_bytes()).digest() == hashlib.sha256(
        (candidate / "checkpoint.png").read_bytes()
    ).digest()
    first = summarize(before)
    second = summarize(after)
    reductions = {}
    for field in ("SimulationMs", "PresentationMs", "DrawCalls"):
        denominator = first[field]["mean"]
        reductions[field] = 100 * (1 - second[field]["mean"] / denominator) if denominator else None
    sprite_field = "SubmittedSceneSpriteDrawCalls"
    reductions[sprite_field] = (
        100 * (1 - second[sprite_field]["mean"] / first[sprite_field]["mean"])
        if first[sprite_field]["available"] and second[sprite_field]["available"] and first[sprite_field]["mean"] > 0 else None
    )
    zero_allocations = all(
        second[field]["total"] == 0
        for field in ("SimulationAllocationEvents", "PresentationAllocationEvents")
    )
    no_growth = second["Pool"]["created_during_measurement"] == 0 and all(
        value == 0 for value in second["ResourceReuse"].values()
    )
    return {
        "compatible": not mismatches,
        "mismatches": mismatches,
        "identical_png_bytes": images_equal,
        "baseline": first,
        "candidate": second,
        "mean_reduction_percent": reductions,
        "zero_allocation_scopes": zero_allocations,
        "prepared_resources_reused": no_growth,
        "performance_targets": {
            "presentation_mean_le_10_ms": second["PresentationMs"]["mean"] <= 10,
            "presentation_p95_le_14_ms": second["PresentationMs"]["p95"] <= 14,
            "update_and_presentation_p95_le_16_67_ms": second["UpdateAndPresentationDistributionMs"]["p95"] <= 16.67,
            "whole_scene_draw_calls_reduced_at_least_40_percent": reductions["DrawCalls"] is not None and reductions["DrawCalls"] >= 40,
            "sprite_draw_submissions_reduced_at_least_40_percent": reductions[sprite_field] is not None and reductions[sprite_field] >= 40,
            "zero_allocation_scopes": zero_allocations,
            "prepared_resources_reused": no_growth,
        },
        "editor_hierarchy_costs_are_included": True,
        "whole_frame_bytes_include_test_runner": True,
    }


def main() -> int:
    """Write a comparison and return failure for incompatible replay or changed pixels."""
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--baseline", required=True, type=Path)
    parser.add_argument("--candidate", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    arguments = parser.parse_args()
    result = compare(arguments.baseline, arguments.candidate)
    arguments.output.parent.mkdir(parents=True, exist_ok=True)
    arguments.output.write_text(json.dumps(result, indent=2), encoding="utf-8")
    print(json.dumps(result, indent=2))
    return 0 if result["compatible"] and result["identical_png_bytes"] else 1


if __name__ == "__main__":
    raise SystemExit(main())
