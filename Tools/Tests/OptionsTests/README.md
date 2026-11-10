# OptionsView integration contract

`PvZReplica.UI.OptionsView` is an independent in-game options view. It is not
wired into `PvZGame`; the owner is responsible for opening it, routing input,
applying settings and resuming the game. It does not access `Screen`, audio,
preferences, saves, scene loading or application restart.

## API and lifecycle

- `new OptionsView(string resourceRoot)`: on the Unity main thread, load
  `<resourceRoot>/images` from the local filesystem. Missing, unreadable,
  invalid or mismatched images throw; do not pass a URL or a packed archive.
- `Open(float musicVolume, float soundVolume, bool fullScreen)`: start a new
  edit session. Finite volumes are clamped to `[0, 1]`; NaN and infinities are
  rejected. Opening an already open view throws. Opening clears `CloseResult`.
- `MusicVolume`, `SoundVolume`, `FullScreen`, `IsOpen` and `CloseResult` are
  read-only properties. Values are drafts, including during slider drag.
- `Draw()`: call inside `OnGUI`, with the owner's GUI matrix already set to map
  the top-left-origin 800 x 600 logical coordinates to the display. It does
  not transform input or dispatch events and does nothing while closed.
- `HandlePointer(Vector2 position, EventType type)`: send logical coordinates.
  Forward left-button `MouseDown`, `MouseDrag`, `MouseUp`, plus `MouseMove` for
  hover. It returns `true` for those events whenever open, even outside the
  panel, including the mouse-up which closes it; otherwise it returns `false`.
  The owner must consume those events and suppress all other game input while
  modal. Forward drag and release even when the pointer leaves the panel.
- `Close()`: owner-initiated close with `ReturnToGame`; this is suitable for
  Escape. `OptionsCloseResult` has `None` and `ReturnToGame`. After the return
  button or `Close()`, read the drafts once and apply/persist them in the owner.
  Do not repeatedly apply a stale result on subsequent frames.
- `Dispose()`: release the owned Unity textures and dynamic font on the main
  thread. Idempotent. Other methods throw after disposal.

Example routing pattern, after `view.Open(...)` and after the owner has paused
its game. `ApplyOptions` is an owner-defined one-shot method:

```csharp
void OnGUI()
{
    Event e = Event.current;
    bool wasOpen = view.IsOpen;
    float scale = Mathf.Min(Screen.width / 800f, Screen.height / 600f);
    float ox = (Screen.width - 800f * scale) * 0.5f;
    float oy = (Screen.height - 600f * scale) * 0.5f;
    if (wasOpen)
    {
        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
        {
            view.Close();
            e.Use();
        }
        else if (e.type == EventType.MouseMove ||
                 ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag ||
                   e.type == EventType.MouseUp) && e.button == 0))
        {
            var p = new Vector2((e.mousePosition.x - ox) / scale,
                                (e.mousePosition.y - oy) / scale);
            if (view.HandlePointer(p, e.type)) e.Use();
        }
        // Suppress remaining game keyboard, other mouse buttons and scroll here.
        if (!view.IsOpen && view.CloseResult == OptionsCloseResult.ReturnToGame)
            ApplyOptions(view.MusicVolume, view.SoundVolume, view.FullScreen);
    }
    // Draw the game first, then the modal view.
    Matrix4x4 previous = GUI.matrix;
    try
    {
        GUI.matrix = Matrix4x4.TRS(new Vector3(ox, oy, 0), Quaternion.identity,
            new Vector3(scale, scale, 1));
        view.Draw();
    }
    finally { GUI.matrix = previous; }
}
```

Do not also handle mouse events automatically in `Draw`, or forward them twice.
The user-facing slider values are normalized; the owner decides the audio
mapping. The original binary additionally multiplied the SFX value by 0.65;
that is not applied by the view.

## Assets

Set `resourceRoot` to the absolute path of `extracted_assets/raw` in this
workspace, or arrange the same filesystem layout in the deployed application.
The existing project's StreamingAssets do **not** currently contain the
options images. This task intentionally does not alter asset preparation.

Required files under `images/`:

```
options_menuback.jpg
options_menuback_.png
options_sliderslot.png
options_sliderknob2.png
options_checkbox0.png
options_checkbox1.png
options_backtogamebutton0.png
options_backtogamebutton2.png
button_left.png
button_middle.png
button_right.png
```

The JPEG and grayscale PNG are combined into a new RGBA texture at runtime.
The extra three `button_*` images are original PAK assets used by the native
three-segment menu buttons. A suitable CJK system font must be available.

## Evidence and limitations

Evidence is specific to local `PlantsVsZombies.exe`, SHA-256
`E1511201EC2AE9A7E30259F04BC2EC7272BADB2574A2E9B405D0E916AE5C082C`.
IDA `0x45CCF0` centers the 423x498 background using integer division;
`0x46AD90` defines the control rectangles; `0x46AF10` places labels and
sets RGB `(107,109,145)`; `0x5C5F20`, `0x5C6400`, `0x5C6660` define slider
knob placement, track click and knob drag; `0x5CE210`, `0x5CE3E0` describe
checkbox drawing and toggling; `0x452720` draws three-segment buttons.

This is **not a verified pixel-identical implementation**. The original uses
its DwarvenTodcraft bitmap fonts; this view uses a CJK OS font and approximate
label baselines, size, green return-label color and disabled tint. No original
versus Unity screenshot comparison, original window scaling, native text
metrics, cursor and audio feedback, or modal dimming have been verified.
The selector-screen variant, context-dependent visibility, original 3D
acceleration dialogs, almanac, restart and main-menu actions are not
implemented. The latter four entries are deliberately inert and visually
disabled; fullscreen is only a draft until the owner applies it. The view
shows the in-game variant unconditionally.

## Tests

`UnityProject` is an isolated Unity 6000.3.17f1 editor test project. Before
running, copy the current `Assets/Scripts/UI/OptionsView.cs` into
`UnityProject/Assets/OptionsView.cs`; the copied file is only a test fixture.
Set `PVZ_OPTIONS_RESOURCE_ROOT` to the absolute path of `extracted_assets/raw`
and invoke `OptionsEditorTests.Run` in batch mode. The test uses actual Unity
texture decoding and checks the background mask, error handling, sliders,
checkbox, disabled entries, close/reopen and `Screen` isolation. It does not
exercise actual IMGUI rendering or integration into the main scene.

From the workspace root, the supplied runner performs the copy and runs the
test with the local editor:

```powershell
& .\PvZUnity\tests\OptionsTests\run.ps1
```

Pass `-Unity <path>` to use a different installation of Unity 6000.3.17f1.
