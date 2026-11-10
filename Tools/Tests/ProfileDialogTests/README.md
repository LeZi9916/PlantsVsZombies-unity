# ProfileDialog integration

`PlantsVsZombies.UI.ProfileDialog` is an independent modal. It does not modify
`PvZGame`, create the catalog, import original saves, or change the game scene.
The current production catalog uses `Guid` profile IDs and `Guid? SelectedId`.

Run the current logic fixtures with the prepared resource root:

```powershell
dotnet run --project Tools/Tests/ProfileDialogTests/ProfileDialogTests.csproj -c Release -- Assets/StreamingAssets
dotnet run --project Tools/Tests/ProfileDialogTests/RealCatalogTests/RealCatalogTests.csproj -c Release -- Assets/StreamingAssets
```

`UnityStubs.cs` and `OriginalUiWidgetsFixture.cs` provide inert image drawing
and deterministic input for state, fallback-label, persistence, and ownership
checks. These fixtures do not verify widget appearance, native GUI input, or
rendered pixels. Use the Unity and Player regression paths for rendering.

## Lifecycle and input

- Construct on Unity's main thread with `new ProfileDialog(resourceRoot, catalog)`.
  The catalog must already exist; handle failures from its constructor and from
  loading the required images. The owner chooses the catalog file path.
- `Open()` starts a session. An empty catalog opens the new-user editor. A
  catalog read failure is displayed inside the still-open dialog.
- `Draw()` runs from `OnGUI` after the owner establishes its 800x600,
  top-left-origin GUI matrix. Draw the underlying screen first.
- While the dialog is open, suppress all input to the underlying screen.
  Forward a primary `MouseDown` in logical coordinates to `HandleClick`.
  If it returns `true`, call `Event.Use()`. If it returns `false`, do not
  route the event to the underlying screen: leave it available to the IMGUI
  text field. Forward normal keyboard, composition, mouse drag and mouse-up
  events to IMGUI without consuming them first.
- Route Escape once to `HandleBack()` and suppress the underlying Escape
  handler. Back cancels a nested editor or confirmation first. On the list,
  an outstanding error is dismissed without closing; closing is allowed only
  when the catalog remains readable and its selected ID identifies a profile.
- Check `IsOpen` after each action, including the click that closes it. Use
  `catalog.SelectedId` and `catalog.Profiles` to refresh the selector. Do not
  send the closing click to the newly exposed screen.
- `Dispose()` releases owned textures and the font; call it on Unity's main
  thread. It is idempotent. Other methods reject use after disposal.

In the existing host, the `OnGUI` input branch must give the modal priority:

```csharp
bool wasOpen = profileDialog.IsOpen;
if (wasOpen)
{
    if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
    {
        // point is already converted from screen pixels to 800x600 coordinates.
        if (profileDialog.HandleClick(point)) Event.current.Use();
    }
    // Do not run the underlying screen's input handler, even when false was returned.
}
else
{
    // Existing input routing.
}
// Set the host GUI matrix, draw the underlying screen, then profileDialog.Draw().
```

The error text may contain a catalog exception message. A catalog that reports
it is unusable must be reconstructed by the owner after inspecting the file;
this view intentionally does not reopen files or guess the result of a failed
write. Deleting the last profile leaves the modal open until a user is created.

## Required Assets

`resourceRoot` must be an ordinary filesystem directory with these original
files beneath `images/`:

```
dialog_topleft.png       dialog_topmiddle.png       dialog_topright.png
dialog_centerleft.png    dialog_centermiddle.png    dialog_centerright.png
dialog_bigbottomleft.png dialog_bigbottommiddle.png dialog_bigbottomright.png
dialog_header.png
button_left.png          button_middle.png          button_right.png
```

They are present in `extracted_assets/raw/images`. The project's current
`StreamingAssets/PvZ/images` contains the three buttons but not the ten
dialog images. Asset preparation and deployment are the main integrator's
responsibility. The view uses a CJK system font, not the original bitmap font.
The original Chinese labels are embedded in the UI, so `LawnStrings.txt` is
not a runtime dependency of this component.

## Verified And Missing

- Creation, selection, rename, delete confirmation/cancellation, paging,
  empty-list protection, read/write errors and postcondition checks are
  covered. The view uses the production catalog methods directly and does not
  depend on their return values.
- `ProfileCatalog.AdventureLevel` currently reports `1`; this UI neither
  imports native profiles nor persists gameplay progress. The main selector
  and game still need to consume the selected user's name and progress.
- The native EXE confirms centered presentation, a 200-pixel list with 24-pixel
  rows, and the relevant actions/text. The outer sizing, font, rendering,
  scrolling/pagination, input feedback and exact nested-dialog layout are not
  verified as pixel-identical. No original-versus-Unity screenshot comparison,
  real keyboard/IME test or full host integration smoke test was performed.
  See `analysis/ida/profile_ui/REPORT.md` for the address-level evidence.

## Tests

From the workspace root:

```powershell
dotnet run --project PvZUnity/Tools/Tests/ProfileDialogTests/ProfileDialogTests.csproj -c Release
dotnet run --project PvZUnity/Tools/Tests/ProfileDialogTests/RealCatalogTests/RealCatalogTests.csproj -c Release
& ./PvZUnity/Tools/Tests/ProfileDialogTests/run-unity.ps1
```

The first suite uses controllable Unity and catalog test doubles. The second
compiles and exercises the actual catalog with filesystem reloads and a real
locked-file failure. The final suite copies the actual UI and catalog sources
into an isolated Unity 6000.3.17f1 project, compiles their actual Unity assembly,
loads the real textures and checks the modal contract. It does not render the
dialog or simulate real keyboard/IME events. Test output stays in this folder.
