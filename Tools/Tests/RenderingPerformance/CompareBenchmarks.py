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
    return {
        "compatible": not mismatches,
        "mismatches": mismatches,
        "identical_png_bytes": images_equal,
        "baseline": first,
        "candidate": second,
        "mean_reduction_percent": reductions,
        "zero_allocation_scopes": all(
            second[field]["total"] == 0
            for field in ("SimulationAllocationEvents", "PresentationAllocationEvents")
        ),
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
