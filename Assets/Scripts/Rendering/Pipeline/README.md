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

## Ordered sprite batching and atlases

`ObjectSpriteRenderer` retains source commands for diagnostics, but only creates a
mesh renderer for each contiguous compatible run. Texture pages and hardware blend
state select shared materials. Per-sprite color, grayscale, producer brightness,
and textured hit values travel in vertex streams. Painter-order gaps preserve
Canvas, pool-water, and other sprite-pool insertions. Each mesh stays within the
16-bit index limit. CPU polygon clipping removes invisible geometry before upload.
`PvZSpriteBatch` is separate from the legacy `PvZCombatSprite` used by pool water.

`SpriteImage` preserves original image dimensions, identity, and source UVs while
`SpriteAssetProvider` resolves lossless atlas tiles. The Editor baker generates
256/512/1024/2048 layouts before ordinary Play Mode and all Player builds. The
provider selects a complete layout before loading any page, bounded by the device's
maximum texture dimension and a 16 MiB RGBA32 page budget. Oversized source images
are tiled without rescaling; padding uses neighboring source texels at internal
seams. Alpha geometry keeps faint pixels and a one-pixel bilinear filtering halo.

Recoverable missing or incorrectly imported pages select a smaller complete layout.
Old pages remain alive through frame retirement, and idle pages are unloaded. GPU
out-of-memory and driver/process failures are not treated as recoverable resource
errors. The hardware dimension limit is not a promise of available memory.

The ignored generated directory contains the catalog, page PNGs, and original
animation/particle/properties text. Production roots (including Android URI roots)
use packaged Resources. External directories use one shared loose-image cache.
Static image consumers avoid retaining source GPU textures alongside their baked
atlas copies. The existing dynamic pool renderer still uses its separate readable
input cache; its platform file access requires target-device validation.

The inspectable actor hierarchy owns visibility and source-pose metadata, not a
renderer per track. Stable model IDs survive clip changes and death presentation.
`LastFrameStatistics` reports source counts, actual batch counts, uploaded geometry,
culled commands, geometric coverage, and material/order/capacity split reasons.
`RenderingBenchmarkTests` is opt-in through `PVZ_BOARD_STRESS_ASSETS` and
`PVZ_RENDER_BENCHMARK_OUTPUT`; `PVZ_RENDER_USE_BAKED=1` selects the production catalog
in the isolated project. It records three fixed feedback states, three rounds of
300 frames after warmup, screenshots, and main-target geometry coverage heatmaps.
Heatmaps count geometric coverage before texture alpha; they are not hardware
fragment counters. Editor GPU timing does not replace target-Player profiling.

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

## Verified reference workload

Unity 6000.3.17f1 on macOS, 2026-11-13: fixed seed 7529, 30 plants,
50 visible normal zombies, 800x600 target, 60 warmup frames and three rounds
of 300 measured frames per state. The original HEAD snapshot and optimized
snapshot use the same original resources and unchanged simulation clock.

| State | Draw calls before / after | Presentation CPU mean before / after | CPU p95 before / after |
| --- | --- | --- | --- |
| Neutral | 1570 / 8 | 3.159 / 3.199 ms | 3.515 / 3.550 ms |
| Hit flash | 2520 / 8 | 14.632 / 3.255 ms | 15.164 / 3.585 ms |
| Chilled | 2520 / 8 | 14.595 / 3.251 ms | 15.028 / 3.569 ms |

The main target's mean geometric coverage changes from 3.098 to 2.744 in
neutral presentation, and from 18.180 to 2.744 in feedback presentation.
Feedback eliminates 50 native captures and 1,083,500 offscreen geometric
pixel samples per representative frame. These are geometry proxies, not
hardware fragment invocation counts. Different decks, scenes, blend modes,
and device layout sizes have different batch counts.

The reference uses three resident atlas pages (40 MiB of RGBA32 page data).
The engine's total texture counter, including Editor textures and fonts,
increases from about 31.2 to 67.0 MiB compared with the loose-source snapshot.
Pages remain nonreadable and are retired when unused. This memory tradeoff
must be included when profiling low-memory devices. Only 28/900 measured
frames returned nonzero GPU timestamps; GPU-frame-time acceptance and other
target devices remain unverified.

Ignored evidence lives under `.unity-runs/render-before/metrics-final` and
`.unity-runs/render-after/final`: JSON counters, frame images, main-target
heatmaps, per-size offscreen heatmaps, and texture inventories. Whole-frame
RGBA comparison has maximum channel differences of 2/255, 3/255, and 7/255
for neutral, hit, and chilled states; the original graphics tolerances are
unchanged. Run with `PVZ_RENDER_PROFILE=1` to enable the optional CPU markers.

## Lossless memory compaction

The subsequent V5 bake replaces broad filename-prefix promotion with complete
common-animation, equipment, particle, and board-UI dependencies. The common
page is sealed against unrelated cold artwork. Offline rectangle trials minimize
actual used page bounds; PNGs use exact non-power-of-two sizes, RGBA32, Clamp,
bilinear filtering, and no mipmaps. Original image dimensions, pixels, aliases,
and two-pixel neighbor halos are unchanged. No runtime repacking or lossy texture
compression is introduced. The dependency recipe itself participates in the source
fingerprint, including the original currency animations and button highlight.

Using the same reference workload and sampling as above:

| Metric | V4 checkpoint | V5 compact pages |
| --- | --- | --- |
| Engine total texture counter | 67.05 MiB | 33.40 MiB |
| Resident atlas logical RGBA pixels | 40.00 MiB / 3 pages | 6.35 MiB / 2 pages |
| Neutral draw calls | 8 | 7 |
| Neutral presentation CPU mean | 3.199 ms | 3.176 ms |
| Mixed eight-plant / four-zombie draw calls | 16 | 7 |
| Mixed presentation CPU mean | 3.426 ms | 3.303 ms |

The original loose-source reference counter was 31.22 MiB. The counter above is
kept separate from `GetRuntimeMemorySizeLong`, which also includes Editor/native
object allocations and must not be described as GPU memory. Benchmark CSV output
records format, graphics format, readability, mip count, and actual provider-owned
pages. All resident pages remain nonreadable, single-mip RGBA32. Four whole-frame
comparisons (neutral/hit/chilled and a mixed deck) have a maximum channel difference
of 1/255 from the V4 checkpoint, within the unchanged graphics tolerances.

Evidence is under `.unity-runs/atlas-memory`: `comparison.json`, before/final
images, pixel comparisons, full atlas-page inventories, and test XML. Actual GPU
timing and other target devices remain unverified. On Windows, opaque `color|mask`
resource identities are split before filesystem path operations; the full identity
and alpha pairing are preserved.
