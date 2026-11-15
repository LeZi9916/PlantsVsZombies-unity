# Original PC native random fixtures

`ExportNativeRandomFixtures.py` reads the supplied original executable and analysis
without modifying either. Requires Python, Unicorn **2.1.4** and Capstone **5.0.6**.
Run from the repository root:

```sh
python3 -B Tools/Tests/Modules/Random/ExportNativeRandomFixtures.py \
  --origin /path/to/pvz-origin --assets /path/to/imported/runtime/assets --check
```

Omit `--check` to export JSON to stdout. The script verifies the executable's
SHA-256 through the supplied `analysis/ida/random/machine_oracle.py` loader.
It executes PC `551ED0`, its actual action selection branches and the original
MT/remainder instructions. The 24 vectors use three seeds and cover idle,
both damage transitions, eligible/empty stomp selection, bungee/RV selection,
head readiness and summoning. Sound is explicitly muted in these vectors;
external row and plant probes are controlled. Animation allocation and existing
ball updates are stubbed, so these are scheduler comparisons, not full-process
oracles. Adventure health is 40000 in these vectors.

The same script reads all 104 PC Foley records at `74B3B8`, resolves sample
references through the original resource-initialization instructions, and checks
pitch ranges, flags, duplicate weighting and sample order against the runtime
catalog. It checks the 132 recordings' sample counts and rates directly from
Vorbis granules or WAV metadata. No replacement recordings or guessed lengths
are generated. The check rejects table or fixture drift.

Unity regressions are in `AdventureNativeRandomTests` and
`OriginalNativeRandomGraphicsTests`. The former includes clock continuity,
original voice admission and no-repeat sampling, constructor cell draws,
original blink gates/counters, mine arming and native boss decision vectors.
The latter checks every plant's original blink marker length, source-art
blink/head attachment playback and boss action-clock rendering. Use Unity
**6000.3.17f1** and the isolated runner documented in `Assets/Tests/README.md`.

Original PC animation range selection at `482690` includes hidden frames between
the first and last visible frame. Multiple nut blinks and Puff-shroom's hidden
middle frame depend on that rule. Tangle Kelp has two `anim_blink` tracks; the
original first-match lookup selects the five-frame marker, not the later
three-frame image track. Initial omitted visibility is zero, not minus one.

The remaining full-game random-stream limitation is particle simulation. The
current renderer advances independently seeded effect models. PC emitters and
particles initialize ten and sixteen interpolation samples respectively
(`52B4D0` and the sixteen-iteration loop at `52B742..52B762`), and
particle creation/update ordering also consumes the shared global generator.
These effects need simulation-owned native lifetimes, creation boundaries and
field/cross-fade handling before full-game instruction-for-instruction RNG
parity can be claimed. The current eight-element general particle bank plus
separate color/spin/animation draws has a different ordering; enlarging that
array alone cannot repair the stream. Dedicated looping-audio paths and
whole-process startup ordering also remain outside these fixture guarantees.
Sharing a mutable generator from `Draw` would make combat
depend on render frequency and is not a valid fix. These fixtures make no such
full-process parity claim, nor do they validate Windows/Linux/mobile players,
physical speaker output or device-dependent voice completion timing.
