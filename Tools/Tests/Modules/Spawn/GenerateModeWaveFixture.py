#!/usr/bin/env python3
"""Compact the supplied PC machine-code oracle exports into portable mode regressions.

The input directory is produced by the read-only spawn audit's native Unicorn
oracles. This generator never derives expected outputs from the C# implementation.
It retains all complete applicable plan hashes and a focused set of candidate
boundary vectors; the large raw audit exports do not belong in the repository.
"""
import argparse
import hashlib
import json
from pathlib import Path

EXPECTED_EXE_SHA256 = "e1511201ec2ae9a7e30259f04bc2ec7272badb2574a2e9b405d0e916ae5c082c"


def load_export(directory, filename):
    document = json.loads((directory / filename).read_text(encoding="utf-8"))
    if document.get("exeSha256") != EXPECTED_EXE_SHA256:
        raise ValueError("Unexpected native executable identity in " + filename)
    return document


def compact_json(value):
    return json.dumps(value, separators=(",", ":"))


def compact_candidate(vector):
    mode = vector["mode"]
    return {
        "mode": mode,
        "flags": vector["stage0"] * (1 if mode <= 5 else 2),
        "wave": vector["wave0"],
        "budget": vector["budget"],
        "enabled": vector["enabled"],
        "wave_counts": sorted([[int(key), value] for key, value in vector["waveCounts"].items()]),
        "round_counts": sorted([[int(key), value] for key, value in vector["roundCounts"].items()]),
        "pairs": vector["nativePairs"],
    }


def generate(directory):
    native = load_export(directory, "survival_native_plans.json")
    plans = []
    for vector in native["vectors"]:
        if vector["mode"] in (23, 35):
            # Aquarium and Boss use special controllers; their unused generic
            # native table is deliberately not a gameplay regression contract.
            continue
        plans.append({
            "mode": vector["mode"],
            "stage": vector["stage0"],
            "seed": vector["seedInput"],
            "terrain": vector["raw"]["board_5564"],
            "allowed": vector["allowed"],
            "wave_count": vector["waveCount"],
            "sha256": hashlib.sha256(compact_json(vector["waves"]).encode("utf-8")).hexdigest(),
            "draws": vector["selectionDrawCount"],
        })
    plans.sort(key=lambda vector: (vector["mode"], vector["stage"], vector["seed"]))

    candidate_map = {}
    boundaries = load_export(directory, "survival_exact_boundaries.json")
    for vector in boundaries["vectors"]:
        if vector.get("profileCase"):
            continue
        result = compact_candidate(vector)
        candidate_map[compact_json(result)] = result
    sampled_stages = (4, 5, 8, 9, 10, 25, 50, 55, 56, 100, 500)
    for vector in load_export(directory, "survival_candidates.json")["vectors"]:
        if vector["wave0"] not in (0, 4, 9, 14, 19):
            continue
        if not (vector["stage0"] == 0 and vector["budget"] in (1, 10)
                or vector["mode"] == 13 and vector["stage0"] in sampled_stages
                and vector["budget"] in (1, 10, 50)):
            continue
        result = compact_candidate(vector)
        candidate_map[compact_json(result)] = result
    candidates = sorted(candidate_map.values(), key=lambda v: (
        v["mode"], v["flags"], v["wave"], v["budget"], compact_json(v["enabled"]),
        compact_json(v["wave_counts"]), compact_json(v["round_counts"])))

    eligibility = []
    for vector in load_export(directory, "survival_profile_lineups.json")["vectors"]:
        raw = vector["raw"]
        eligibility.append({
            "mode": vector["mode"], "stage": raw["challenge_6c"],
            "terrain": raw["board_5564"], "seed": raw["profile_20"],
            "profile_progress": raw["profile_54"], "profile_level": raw["profile_4c"],
            "board_seed_offset": raw["board_5634"], "special_rules": bool(raw["app_868"]),
            "allowed": vector["allowed"],
        })
    return {
        "format": 1, "exe_sha256": EXPECTED_EXE_SHA256,
        "method": "Actual supplied PC x86 functions under Unicorn 2.1.4; synthetic raw state. "
                  "Local generation and candidate contracts, not complete native startup replay.",
        "functions": ["0x415E40", "0x430B50", "0x4309A0", "0x4150C0", "0x4195B0", "0x526F20"],
        "hash_encoding": "SHA-256 of UTF-8 compact JSON nested integer wave arrays; no whitespace or newline.",
        "raw_defaults": {"profile_54": 0, "profile_4c": 1, "board_5634": 0, "app_868": 1},
        "special_modes_without_generic_plan": [23, 35],
        "plans": plans, "candidates": candidates, "eligibility": eligibility,
    }


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--evidence-directory", type=Path, required=True)
    parser.add_argument("--output", type=Path, default=Path(__file__).with_name("mode-wave-native.json"))
    arguments = parser.parse_args()
    fixture = generate(arguments.evidence_directory)
    arguments.output.write_text(compact_json(fixture) + "\n", encoding="utf-8")
    print("Native mode fixture: %d plans, %d candidate vectors, %d profile cases" % (
        len(fixture["plans"]), len(fixture["candidates"]), len(fixture["eligibility"])))


if __name__ == "__main__":
    main()
