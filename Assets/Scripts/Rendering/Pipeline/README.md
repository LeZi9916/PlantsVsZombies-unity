# PvZ render pipeline

A small Scriptable Render Pipeline that replaces URP. It covers only what the game
draws: unlit 2D quads, uGUI canvases, and Editor views. Nothing is lit, shadowed, or
post-processed.

## Structure

| File | Role |
| --- | --- |
| `PvZRenderPipelineAsset.cs` | The asset selected in Graphics and Quality settings (`Assets/Settings/PvZRenderPipeline.asset`). It sets the SubShader tag `PvZRenderPipeline` and the SRP Batcher switch. |
| `PvZRenderPipeline.cs` | The per-frame camera loop and standard render requests. |
| `ShaderLibrary/PvZPipelineCore.hlsl` | Engine matrices (`unity_ObjectToWorld`, `unity_MatrixVP`) in the `UnityPerDraw` buffer the SRP Batcher expects, plus `TransformObjectToHClip`. |
| `Assets/Editor/PvZRenderPipelineConfiguration.cs` | Menu **PlantsVsZombies > Configure Render Pipeline**. It creates the asset, selects it for Graphics and every Quality level, and turns MSAA off. |

For each camera, in ascending `Camera.depth` order, the pipeline:

1. Emits Scene view geometry (Editor only) or preview-camera geometry.
2. Culls the camera's layers. Shadow casters, lights, and reflection probes are left out.
   Disabled cameras are still culled, which render requests rely on.
3. Calls `SetupCameraProperties`, which sets the target, viewport rect, and matrices.
4. Clears by the camera's flags. `SolidColor` and `Skybox` both clear to `backgroundColor`
   (no skybox is drawn), `Depth` clears only depth, and `Nothing` skips the clear.
5. Draws the opaque queue (`CommonOpaque`), then the transparent queue (`CommonTransparent`:
   sorting layer and order, then render queue, then back to front). Only passes tagged
   `SRPDefaultUnlit`, or with no `LightMode` (such as uGUI's `UI/Default`), are drawn.
6. Draws gizmos for the Scene view and for Game cameras that render to the display.

`RenderPipeline.SubmitRenderRequest` accepts `RenderPipeline.StandardRequest` with a 2D
destination at mip 0. The camera renders into the destination, and its `targetTexture`
is restored afterwards. `ObjectSpriteRenderer.RenderTo` uses this for cropped native surfaces.

Screen Space - Camera canvases draw as ordinary transparent renderers. Screen Space - Overlay
canvases are drawn by the engine after all cameras (`rendersUIOverlay` is false).

The render loop allocates nothing per frame. It reuses one `CommandBuffer`, sorts
cameras with an in-place insertion sort, and builds renderer lists from value types.
Culling cannot be skipped, because renderer lists need `CullingResults`. Orthographic
frustum culling of the pooled quads is cheap.

## Not supported

Lighting, shadows, post-processing, Volumes, HDR, MSAA, depth and opaque textures,
deferred rendering, reflection probes, decals, skyboxes, XR, Render Graph, and
camera stacking. Shaders that only have lit or legacy built-in passes (`ForwardBase`,
URP `UniversalForward`) do not draw.

## Adding a new kind of draw

1. Write an unlit shader. Tag the SubShader `"RenderPipeline"="PvZRenderPipeline"` and
   the pass `"LightMode"="SRPDefaultUnlit"` (or leave out `LightMode`). Include
   `Assets/Scripts/Rendering/Pipeline/ShaderLibrary/PvZPipelineCore.hlsl`. Put material
   properties in `CBUFFER_START(UnityPerMaterial)` so the SRP Batcher stays compatible.
   Do not add `multi_compile` or `shader_feature` variants unless every target platform needs them.
2. Put renderers on a layer in the camera's culling mask (layer 31 for game sprites).
   Order transparent geometry with `sortingOrder`, or give opaque geometry
   depth-writing materials in the Geometry queue.
3. If the draw needs a new stage, such as a full-screen effect, add it in
   `PvZRenderPipeline.RenderCamera` with the reused command buffer. Do not create
   per-frame objects. Then add a pixel test to `Assets/Tests/PlayMode/PvZRenderPipelineTests.cs`.
