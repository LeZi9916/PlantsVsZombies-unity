# Independent-mode native wave fixture

`mode-wave-native.json` records original x86 machine-code results for the supplied
PC executable, identified by the SHA-256 in the fixture. Expected results were not
generated from the Unity implementation or from another port.

The native audit executed `0x430B50` and `0x4309A0` eligibility, `0x415E40` local
seed derivation, `0x4150C0` complete wave generation, and `0x4195B0` candidates with
native `0x526F20` curves in Unicorn 2.1.4. Native MT initialization and weighted
selection were executed without substituting random results. Candidate-only calls
stopped at the native weighted selector to observe the exact candidate array.

The fixture covers normal/hard survival's five stages and all five endless boards
at stages 0 through 56, 100 and 500, using seeds 0, 1, 4357, 5489, 12345,
2147483647, 2147483648 and 4294967295. Fixed independent modes have their initial
stage, and Last Stand includes all five stages. Aquarium and Boss retain separate
controllers; their unused native generic tables are excluded from plan assertions.
Their eligibility rules may still appear in the profile cases.

Each plan records the 34-bit eligibility set, wave count, global weighted-draw
count, and SHA-256 of the exact wave table. The hash input is UTF-8 compact JSON
of the nested integer arrays: `[[0],[0,2],...]`, with no whitespace or newline.
`JsonSerializer.Serialize(int[][])` produces the same bytes. `seed` is both the
explicit raw profile seed and the separately initialized global MT seed in this
controlled local fixture. Other raw defaults are listed in the header. This is a
local function contract, not a claim of full original game startup reproduction.

Candidate vectors preserve ID order and zero-weight entries, and include exact
quota-minus-one/quota/quota-plus-one states. `wave_counts` and `round_counts` store
only nonzero or explicitly supplied `[id,count]` pairs; missing counts are zero.
The original gate-reduction curve starts at **18 completed flags** and ends at 50;
the ordinary/road-cone weight curves start at 10 and end at 50. Later survival
budgets are `floor(2 * (wave + stage * wavesPerStage) / 5) + 1`.

Regenerate the compact fixture from the read-only native audit exports:

```sh
python3 -B Tools/Tests/Modules/Spawn/GenerateModeWaveFixture.py \
  --evidence-directory /path/to/native-audit/evidence
```

The generator validates the executable identity in every source export. Raw audit
exports, PE files, emulator dependencies, logs and builds are intentionally not
stored in this directory. The checked-in fixture is sufficient for ordinary
regression runs and does not require the executable or Unicorn.
