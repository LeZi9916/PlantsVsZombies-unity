#nullable enable
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PlantsVsZombies.Rendering.Pipeline
{
    /// <summary>Renders unlit sprites, uGUI canvases, and editor views without lighting, shadows, or post-processing.</summary>
    /// <remarks>
    /// Each camera is culled, cleared according to its clear flags, and drawn in an opaque pass followed by a
    /// transparent pass. Only shader passes without a light mode or with <c>SRPDefaultUnlit</c> are drawn.
    /// </remarks>
    public sealed class PvZRenderPipeline : RenderPipeline
    {
        /// <summary>The engine features skipped while culling because the game has no lights, shadows, or probes.</summary>
        private const CullingOptions UnusedCullingFeatures = CullingOptions.ShadowCasters | CullingOptions.NeedsLighting | CullingOptions.NeedsReflectionProbes;
        /// <summary>The pass tag drawn by this pipeline, which also matches built-in shaders without a light mode such as UI/Default.</summary>
        private static readonly ShaderTagId s_unlitPass = new ShaderTagId("SRPDefaultUnlit");
        /// <summary>The reused buffer recording the clear and draw commands of one camera.</summary>
        private readonly CommandBuffer _commands = new CommandBuffer { name = "PvZ Render Camera" };
        /// <summary>The reused single-camera list reported to context callbacks while processing a render request.</summary>
        private readonly List<Camera> _requestCameras = new List<Camera>(1);

        /// <summary>Creates the pipeline and applies the global settings of its asset.</summary>
        /// <param name="asset">The asset whose settings configure this pipeline instance.</param>
        public PvZRenderPipeline(PvZRenderPipelineAsset asset)
        {
            GraphicsSettings.useScriptableRenderPipelineBatching = asset.UseSrpBatcher;
            // Screen Space - Overlay canvases are drawn by the engine after every camera has rendered.
            SupportedRenderingFeatures.active.rendersUIOverlay = false;
        }

        /// <summary>Renders every active camera in ascending depth order.</summary>
        /// <param name="context">The context receiving the recorded rendering commands.</param>
        /// <param name="cameras">The cameras to render; sorted in place by depth.</param>
        protected override void Render(ScriptableRenderContext context, List<Camera> cameras)
        {
            SortByDepth(cameras);
            BeginContextRendering(context, cameras);
            for (var index = 0; index < cameras.Count; index++)
            {
                RenderCamera(context, cameras[index]);
            }

            EndContextRendering(context, cameras);
        }

        /// <summary>Reports whether a render request type can be processed by this pipeline.</summary>
        /// <typeparam name="TRequestData">The type of the submitted request.</typeparam>
        /// <param name="camera">The camera that would render the request.</param>
        /// <param name="data">The request to check.</param>
        /// <returns><see langword="true"/> for <see cref="RenderPipeline.StandardRequest"/>; otherwise <see langword="false"/>.</returns>
        protected override bool IsRenderRequestSupported<TRequestData>(Camera camera, TRequestData data)
        {
            return data is StandardRequest;
        }

        /// <summary>Renders one camera into the two-dimensional render texture named by a standard request.</summary>
        /// <typeparam name="TRequestData">The type of the submitted request.</typeparam>
        /// <param name="context">The context receiving the recorded rendering commands.</param>
        /// <param name="camera">The camera to render; its target texture is restored afterwards.</param>
        /// <param name="renderRequest">The request naming the destination texture.</param>
        protected override void ProcessRenderRequests<TRequestData>(ScriptableRenderContext context, Camera camera, TRequestData renderRequest)
        {
            if (renderRequest is not StandardRequest request)
            {
                Debug.LogWarning($"{nameof(PvZRenderPipeline)}: Render request type {typeof(TRequestData).FullName} is not supported.", camera);
                return;
            }

            var destination = request.destination;
            if (destination == null || destination.dimension != TextureDimension.Tex2D || request.mipLevel != 0 || request.slice != 0)
            {
                Debug.LogError($"{nameof(PvZRenderPipeline)}: Render requests need a two-dimensional destination texture at mip level 0.", camera);
                return;
            }

            var previousTarget = camera.targetTexture;
            _requestCameras.Add(camera);
            try
            {
                camera.targetTexture = destination;
                BeginContextRendering(context, _requestCameras);
                RenderCamera(context, camera);
                EndContextRendering(context, _requestCameras);
            }
            finally
            {
                camera.targetTexture = previousTarget;
                _requestCameras.Clear();
            }
        }

        /// <summary>Releases the reused command buffer.</summary>
        /// <param name="disposing">Whether managed resources should be released.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _commands.Release();
            }

            base.Dispose(disposing);
        }

        /// <summary>Culls, clears, and draws one camera, then submits its commands.</summary>
        /// <param name="context">The context receiving the recorded rendering commands.</param>
        /// <param name="camera">The camera to render into its current target.</param>
        private void RenderCamera(ScriptableRenderContext context, Camera camera)
        {
            BeginCameraRendering(context, camera);
            EmitEditorGeometry(camera);
            if (camera.TryGetCullingParameters(out var cullingParameters))
            {
                cullingParameters.cullingOptions &= ~UnusedCullingFeatures;
                // Render requests use cameras that stay disabled between captures.
                cullingParameters.cullingOptions |= CullingOptions.ForceEvenIfCameraIsNotActive;
                var cullingResults = context.Cull(ref cullingParameters);
                context.SetupCameraProperties(camera);
                AppendClear(camera);
                AppendRenderers(context, cullingResults, camera, SortingCriteria.CommonOpaque, RenderQueueRange.opaque);
                AppendRenderers(context, cullingResults, camera, SortingCriteria.CommonTransparent, RenderQueueRange.transparent);
                AppendGizmos(context, camera);
                context.ExecuteCommandBuffer(_commands);
                _commands.Clear();
                context.Submit();
            }

            EndCameraRendering(context, camera);
        }

        /// <summary>Records the clear operation selected by the camera's clear flags.</summary>
        /// <param name="camera">The camera whose clear flags and background color are used.</param>
        /// <remarks>Skybox cameras, such as the editor Scene view, are cleared to their background color because skyboxes are not drawn.</remarks>
        private void AppendClear(Camera camera)
        {
            var flags = camera.clearFlags;
            if (flags == CameraClearFlags.Nothing)
            {
                return;
            }

            var clearColor = flags == CameraClearFlags.SolidColor || flags == CameraClearFlags.Skybox;
            var color = clearColor ? camera.backgroundColor : Color.clear;
            if (clearColor && QualitySettings.activeColorSpace == ColorSpace.Linear)
            {
                color = color.linear;
            }

            _commands.ClearRenderTarget(true, clearColor, color);
        }

        /// <summary>Records the visible renderers of one render queue range.</summary>
        /// <param name="context">The context that creates the renderer list.</param>
        /// <param name="cullingResults">The renderers visible to the camera.</param>
        /// <param name="camera">The camera providing the sorting origin.</param>
        /// <param name="criteria">The sorting applied to the renderers, which includes sorting layer and order.</param>
        /// <param name="queues">The render queue range to draw.</param>
        private void AppendRenderers(ScriptableRenderContext context, CullingResults cullingResults, Camera camera, SortingCriteria criteria, RenderQueueRange queues)
        {
            var sorting = new SortingSettings(camera)
            {
                criteria = criteria
            };
            var drawing = new DrawingSettings(s_unlitPass, sorting)
            {
                perObjectData = PerObjectData.None
            };
            var filtering = new FilteringSettings(queues);
            var parameters = new RendererListParams(cullingResults, drawing, filtering);
            _commands.DrawRendererList(context.CreateRendererList(ref parameters));
        }

        /// <summary>Records editor gizmos for the Scene view and for Game view cameras that render to the display.</summary>
        /// <param name="context">The context that creates the gizmo renderer lists.</param>
        /// <param name="camera">The camera being rendered.</param>
        private void AppendGizmos(ScriptableRenderContext context, Camera camera)
        {
#if UNITY_EDITOR
            var displayCamera = camera.cameraType == CameraType.SceneView || (camera.cameraType == CameraType.Game && camera.targetTexture == null);
            if (!displayCamera || !Handles.ShouldRenderGizmos())
            {
                return;
            }

            _commands.DrawRendererList(context.CreateGizmoRendererList(camera, GizmoSubset.PreImageEffects));
            _commands.DrawRendererList(context.CreateGizmoRendererList(camera, GizmoSubset.PostImageEffects));
#endif
        }

        /// <summary>Adds Canvas and world geometry that the engine only emits on request for editor and preview cameras.</summary>
        /// <param name="camera">The camera being rendered.</param>
        private static void EmitEditorGeometry(Camera camera)
        {
#if UNITY_EDITOR
            if (camera.cameraType == CameraType.SceneView)
            {
                ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
            }
#endif
            if (camera.cameraType == CameraType.Preview)
            {
                ScriptableRenderContext.EmitGeometryForCamera(camera);
            }
        }

        /// <summary>Stably sorts cameras by ascending depth without allocating.</summary>
        /// <param name="cameras">The cameras to reorder in place.</param>
        private static void SortByDepth(List<Camera> cameras)
        {
            for (var index = 1; index < cameras.Count; index++)
            {
                var camera = cameras[index];
                var position = index - 1;
                while (position >= 0 && cameras[position].depth > camera.depth)
                {
                    cameras[position + 1] = cameras[position];
                    position--;
                }

                cameras[position + 1] = camera;
            }
        }
    }
}
