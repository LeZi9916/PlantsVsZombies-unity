# Selector view

`PlantsVsZombies.UI.SelectorView` is an independent Canvas view, not an implementation
of the games or menus it points to. Its constructor takes a directory containing
`images/` and `reanim/`; for this workspace use `extracted_assets/raw`. The current
`Tools/PrepareAssets.ps1` does not copy these assets, so the existing
`StreamingAssets/PvZ` directory is not a complete selector resource root.

The presenter calls `Draw(seconds)` once per frame in its 800x600 logical
Canvas space. The input router converts screen pixels through the shared
viewport and calls `HitTest` with logical coordinates; set `HoveredAction`
from that result for highlights. Neither method dispatches navigation.
`SetModeLocked` changes the dark tint for MiniGames, Puzzle, and Survival;
`Dispose` releases cached textures. Drawing and hit testing reject use after
disposal. A GUI event or `OnGUI` callback is not required.
The returned actions are `None`, `Adventure`, `MiniGames`, `Puzzle`, `Survival`,
`ZenGarden`, `Almanac`, `Store`, `Options`, `Help`, `Quit`, `ChangeUser`, and
`Zombatar`. The view does not load profiles, automatically gate progress, invoke
dialogs, play sounds, or choose a screen. `seconds` is reserved for animation;
the implemented pose is static.

## Asset inventory

All paths below are relative to `resourceRoot`. The extensionless stems in the
code resolve to PNG first, then JPG. The three background JPGs require their
matching grayscale `_.png` masks when a merged PNG is not supplied.

- `reanim/SelectorScreen_BG.jpg`; `SelectorScreen_BG_Center.jpg`,
  `SelectorScreen_BG_Left.jpg`, `SelectorScreen_BG_Right.jpg` and the three
  corresponding `_.png` alpha masks.
- `reanim/SelectorScreen_StartAdventure_Button1.png`, `..._Highlight.png`,
  `SelectorScreen_Survival_button.png`, `..._highlight.png`,
  `SelectorScreen_Challenges_button.png`, `..._highlight.png`,
  `SelectorScreen_Vasebreaker_button.png`, `..._highlight.png`.
- `reanim/SelectorScreen_Shadow_StartAdventure.png`, `..._Shadow_Survival.png`,
  `..._Shadow_Challenge.png`, `..._Shadow_ZenGarden.png`,
  `SelectorScreen_almanac_key_shadow.png`.
- `reanim/SelectorScreen_Leaf1.png` through `Leaf5.png`, `..._Leaves.png`,
  `..._Flower1.png` through `Flower3.png`, `..._WoodSign1.png`, `..._WoodSign2.png`,
  `..._WoodSign2_press.png`, `..._WoodSign3.png`, `..._WoodSign3_press.png`.
- `images/SelectorScreen_ZenGarden.png`, `..._ZenGardenHighlight.png`,
  `..._Almanac.png`, `..._AlmanacHighlight.png`, `..._Store.png`,
  `..._StoreHighlight.png`, `..._Options1.png`, `..._Options2.png`,
  `..._Help1.png`, `..._Help2.png`, `..._Quit1.png`, `..._Quit2.png`.

`...` above abbreviates the immediately preceding `SelectorScreen` prefix.
The raw files in this Chinese package actually show MiniGames on the image
named `Survival`, Puzzle on `Challenges`, and Survival on `Vasebreaker`.

## Evidence and limits

- `analysis/ida/decompiled/45C4A0.c` identifies selector construction at
  `0x4539E0`. The independently exported `4539E0.c` in this test directory
  gives button-local quadrilaterals for IDs 100, 101, 102 and 110. For example,
  MiniGames uses `(4,2), (312,51), (296,130), (7,77)`.
- `456280.c` and `456D00.c` place controls against reanim tracks; `4578E0.c`
  maps IDs 101, 102 and 110 to MiniGames, Puzzle and Survival. `454F30.c`
  shows the three locked mode signs are tinted to `(128,128,128)`.
- The final `anim_open` frame in `raw/reanim/SelectorScreen.reanim` places the
  background center at `(80,250)`, left at `(0,-79.8)`, right at `(71,41)`,
  and the four signs at `(405,65)`, `(406,173.1)`, `(410,257.5)`, `(413,328)`.
  The sign's final `anim_sign` frame supplies its three wood-sign positions.
- EXE anchor offsets and button dimensions inform the auxiliary rectangles;
  their exact z-order, integer rounding and hit-edge semantics are not yet
  established by a live native comparison. The tests record this implementation,
  not an independent native oracle.
- The implementation does not reproduce moving clouds, opening transitions,
  independent leaf/flower animation, profile text, level numbers, optional
  progression-dependent appearance, audio, or every edition-specific control.
  Image dimensions, JPEG mask handling and compositing have not been compared
  pixel-by-pixel to the native executable. Complete visual/state equivalence
  must not be claimed.

## Verification

```powershell
dotnet run --project PvZUnity/Tools/Tests/SelectorTests/SelectorTests.csproj -c Release
```

The standalone tests exercise hit intents, boundaries, cache behavior, mask
load paths and disposal against the real filenames using Unity API stubs; the
stubs inspect image headers but do not decode pixels. A separate isolated Unity
6000.3.17f1 project under `UnityProject/` compiled and built successfully using
the same selector source. Its hidden-window screenshot was uniformly black, so
the runtime screenshot check failed and visual correctness remains unverified.
The exported IDA files are reference output, not code linked into the project.
