# Original behavior fixes, 2026-11-11

The local PC executable, its exported instructions, and its extracted assets
provide the behavior and artwork used for these changes. This is a focused
regression record, not a claim of complete game or cross-platform equivalence.

## Changes and evidence

| Area | Result | Original PC evidence |
| --- | --- | --- |
| Seed chooser | Retains selection order, allows removal from the bank, flies packets in/out over 25 ticks, and closes gaps over 15 ticks. Rapid clicks first land the preceding flight. | `0x49ACF0`, `0x49B6D0`, `0x49B990`, `0x49C29F` |
| Packet spacing and layering | Uses the configured slot capacity and draws the playing bank before lawn actors. Intro packets retain their foreground order. | `0x427E20`, `0x421D80`, `0x425CDD` |
| Rejected seed clicks | Plays the original buzzer. An unaffordable ready packet starts the 70-tick red sun-counter warning; cooldown takes precedence. | `0x49E130`, `0x49F100` |
| Shovel | Removes the shovel from its holder while held, draws the original pointer image, and highlights exactly the plant that the tool will remove, including pumpkin rim/center selection. | `0x444100`, `0x4441A0`, native plant tool targeting and additive colors |
| Potato mine | Separates blinking proximity from detonation targeting; restores native body rectangles, edge contact, the eating target's 30-pixel tolerance, and the 60-pixel body-rectangle blast. Preserves damage flags `0x12`: held shields and body both receive damage; ordinary lethal victims disappear without falling corpses or loose parts, while Gargantuars retain their death animation. | `0x475D90`, `0x475AA0`, `0x46E1F0`, `0x474EE0`, `0x4294C0`, `0x54D560`, `0x54F0F0` |
| Defensive plants | Replaces the original damage track at strict integer health thresholds. Walnut and tall-nut preserve animation phase while eaten at 0.1 fps; damage transitions emit original debris. | `0x4728D0`, `0x472BB0` |
| Chomper | Uses original bite, digestion, and swallow clips and timing, including a missed bite and large-target exceptions. | `0x46F6E0`, original `Chomper.reanim` |
| Target entry | Rejects offscreen zombie body rectangles, preserving the original bobsled exception. | `0x475AA0` |
| Zombie parts/shadows | Hides all original pole/inner-arm prefixes after pole-vaulter head loss. Shadows use native coordinates, original image dimensions, and native phase/water/vehicle exclusions. | native `DropHead`/`DropPole`, `0x54FD80`, `0x54FF00`, `0x549740` |
| Huge-wave corruption | Restores the matrix and tint after drawing a flag attachment. The prior leak moved subsequent plant and mower rows offscreen. | Reproduced production renderer defect; original attachment transform remains unchanged |
| Capture pooling | Shares capture cameras and mesh buffers by nesting depth, retains independent actor textures, and prevents same-frame texture reuse from changing an earlier composite. | Rendering lifetime fix without new artwork or animation |
| Note award | Combines the original JPEG paper with its alpha image and the original black handwriting artwork with its alpha grid. | Original `resources.xml` and unchanged original image bytes |
| Level progress | Restores original counter timing, fill/head/flag pixel positions, level-name font and position, and native flag exceptions. | `0x420360`, `0x4229F0`, `0x423390` |
| Completed board | Advances existing projectiles and their retirement while the award is on the board; stops when the introduction replaces it. Does not restart combat, spawning, or drops. | Native board/game-object update during level completion |

Six-slot PC banks intentionally retain the original 59-pixel packet stride
(9 pixels between 50-pixel packets). Eight, nine, and ten slots use 54, 52,
and 51 pixels. The fix removes the incorrect use of a six-slot stride when
selecting into a larger bank.

Per the additional audio request, projectile impact categories allow closely
spaced hits to overlap, keeping the original samples/pitch choices and eight
voices per category. A saturated impact category reuses its oldest voice.
Other Foley categories retain the native ten-tick admission rule.

## Validation

Use Unity **6000.3.17f1** and a real graphics device:

```powershell
./Tools/Tests/Run-UnityTests.ps1 -Platform All -TestFilter 'AdventureSeedChooserTests;AdventureProgressMeterTests;BoardInteractionTests;GamePresentationPerformanceTests;AudioPoolingTests;OriginalImageTextureTests;CanvasPresentationTests;ObjectSpriteRendererTests;ReanimationHotPathTests'
dotnet run --project Tools/Tests/AdventureTests/AdventureTests.csproj -c Release -- --unit
dotnet run --project Tools/Tests/ArchitectureTests/ArchitectureTests.csproj -c Release
git diff --check
```

The focused Unity run passed 29 EditMode and 67 PlayMode cases. The independent
unit run passed 2115 Adventure assertions, 242 Upgrade/Imitater assertions,
and the new combat/completed-projectile fixtures. Architecture reported zero
violations. New StreamingAssets were checked against the extracted originals;
all eleven files match byte-for-byte, and every added Unity asset has its meta.

`OriginalBoardStressTests` adds an optional test using local original artwork
through the documented environment variable. It presents thirty plants and
one hundred zombies for twenty-four frames, with real wave notices, cooldowns,
nested captures, and alternating direct/captured flag zombies. The old flag
path fails in the first direct frame; the corrected path preserves every
plant region. Capture renderer count remains two throughout. The original-art
stress and associated presentation suite passed 19/19 without skips.

Windows x64 Player checks use a separate project under `.unity-runs` and
temporary profiles. Adventure smoke covers 70 checks and 22 real Direct3D 11
screenshots. The native-intro/note smoke covers 29 checks, including chooser
flight/return frames and the actual 1-9 note composition. These fixtures do
not claim the note level was played through, nor compare the entire game
pixel-for-pixel with the PC executable.

Logs, NUnit XML, and screenshots are retained under `.unity-runs`; no generated
project, cache, build, or personal save files are part of the source changes.
Linux, macOS, mobile devices, other ABIs, and IL2CPP Players remain unverified.

## Follow-up corrections from the supplied screenshots

The first shadow calculation incorrectly treated the arguments of PC helper
`0x527D20` as the image center. The real instructions add half the **unscaled**
image width and height before applying scale. This caused a normal 86x36 zombie
shadow to move 43 pixels left and 18 pixels up. The corrected draw uses the
original top-left arguments, the body's verified `Y-8` overlay, and PC species
offsets. `verify_native_shadow.py` independently interprets the hash-pinned
image-helper instructions; the original-artwork test checks actual submitted
shadow vertices and body placement on day/night, small, frozen, chilled, moving,
and special-zombie fixtures. Its assertions reject the former center formula.

Puff-shroom now uses the native half-size shadow (43x18 at normal plant size),
the original 230-pixel target rectangle, the zombie's body rectangle, the
32x27 spore's muzzle center, its 3.33-pixel native velocity, seventy-five-tick
lifetime, and growth from scale0.3 to1. The animation windup uses the same target
predicate as firing: planting beside a distant zombie no longer produces a
single false shooting animation. The regression plants through `TryPlant` and
observes one hundred real simulation ticks before moving the target into range.

The potato mine's lethal `0x12` flags now reach the death handler. Ordinary
zombies disappear without a standard falling corpse or detached parts, while
gargantuars retain their native death animation. Explosive shield/body damage
and the original sixty-pixel explosion-circle/body-rectangle test are retained
instead of the former actor-center approximation.

Bank positions now use configured capacity even when fewer packets are
selected. The supplied reference agrees with the running original game's ten
packet origins `(79,8), (130,8), ... (538,8)`. Original empty-slot silhouettes
are drawn during choosing, including underneath incoming packets. The final
intro bank origin is retained for gameplay rendering and input, eliminating
the extra ten-pixel snap to zero at handoff while preserving the native intro
curve. The regression compares the last intro pose, first playing pose,
packet hit testing, and a ten-slot bank with only four selected cards.

Additional focused commands:

```powershell
python Tools/Tests/AdventureTests/verify_native_shadow.py
dotnet run --project Tools/Tests/AdventureTests/AdventureTests.csproj -c Release -- --puff-shroom
```

The shadow oracle is a limited instruction interpreter, not a complete PC
game run. Original Puff-shroom image tests also fail under the former generic
shadow and projectile draw parameters, and pass with the corrected production
methods. All original-artwork graphics tests use explicit read-only asset roots
and ignored diagnostic output directories.

The follow-up combined Unity run passed **32 EditMode and 81 PlayMode tests**
with zero failures or skips, including the optional original-artwork fixtures.
The full independent `--unit` run also passes with the added mine-death and
Puff-shroom cases. A fresh Unity 6000.3.17f1 Windows x64 build passed the same
70-check Adventure and 29-check IntroAudio Player smokes (22 and 26 screenshots).
All 275 runtime C# files match the isolated Player snapshot. The follow-up
reports are retained in `.unity-runs/fidelity-player/followup-verification.json`
and `.unity-runs/renderer-a29be107/summary.json`.
