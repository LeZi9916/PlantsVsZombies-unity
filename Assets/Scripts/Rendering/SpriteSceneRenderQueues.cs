#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

namespace PlantsVsZombies.Rendering
{
    /// <summary>Bridges explicit GPU draws and native Canvas batches with compressed painter-span queues.</summary>
    /// <remarks>Only retained material copies are modified. Source materials, masks, native controls, and textures remain owned by their original components.</remarks>
    internal static class SpriteSceneRenderQueues
    {
        /// <summary>The first queue reserved for ordered game transparency.</summary>
        private const int FirstQueue = 3000;
        /// <summary>The number of valid queues through Unity's final render queue.</summary>
        internal const int MaximumSpans = 2001;
        /// <summary>The maximum retained material copies before selecting the complete mesh fallback.</summary>
        private const int MaximumMaterials = 8192;
        /// <summary>The sorted unique starts of externally ordered spans.</summary>
        private static readonly List<int> s_orders = new List<int>(128);
        /// <summary>The active native world renderer spans.</summary>
        private static readonly List<WorldSpan> s_world = new List<WorldSpan>(128);
        /// <summary>The stable material copies owned by native world renderers.</summary>
        private static readonly Dictionary<MeshRenderer, MaterialBinding> s_worldMaterials = new Dictionary<MeshRenderer, MaterialBinding>();
        /// <summary>The stable copies for each native UI layer and source material.</summary>
        private static readonly Dictionary<(Canvas, Material), MaterialBinding> s_canvasMaterials = new Dictionary<(Canvas, Material), MaterialBinding>();
        /// <summary>The reverse lookup preventing clones from becoming new clone sources.</summary>
        private static readonly Dictionary<Material, Material> s_sources = new Dictionary<Material, Material>();
        /// <summary>The reusable native renderer traversal, including carets and mask pop instructions.</summary>
        private static readonly List<CanvasRenderer> s_canvasRenderers = new List<CanvasRenderer>(128);
        /// <summary>The current retained native Canvas layers.</summary>
        private static IReadOnlyList<Canvas>? s_canvases;
        /// <summary>The original logical Canvas orders retained alongside the completed queue mapping.</summary>
        private static readonly List<int> s_canvasOrders = new List<int>(32);
        /// <summary>The number of active native Canvas layers.</summary>
        private static int s_canvasCount;
        /// <summary>The material refresh pass, preventing duplicate copies for one camera.</summary>
        private static int s_refresh;
        /// <summary>Whether material queues are needed for this completed scene.</summary>
        private static bool s_enabled;
        /// <summary>Gets whether the reserved game layer uses compressed queues as its primary rendering order.</summary>
        internal static bool UsesQueueOrdering => s_enabled && !RequiresMeshFallback;
        /// <summary>Whether the bounded bridge requires the complete scene mesh fallback.</summary>
        internal static bool RequiresMeshFallback { get; private set; }
        /// <summary>Gets the most recent bounded fallback reason.</summary>
        internal static string? FallbackReason { get; private set; }

        /// <summary>Starts recording a new complete painter sequence.</summary>
        internal static void BeginFrame()
        {
            s_orders.Clear();
            s_world.Clear();
            s_enabled = false;
            RequiresMeshFallback = false;
            FallbackReason = null;
        }

        /// <summary>Records one externally sorted span without using an order for every sprite part.</summary>
        /// <param name="order">The original first logical painter order of the span.</param>
        internal static void RegisterOrder(int order)
        {
            var index = s_orders.BinarySearch(order);
            if (index < 0)
            {
                s_orders.Insert(~index, order);
            }
        }

        /// <summary>Records a scene mesh or water layer without modifying its shared source material.</summary>
        /// <param name="renderer">The retained native renderer.</param>
        /// <param name="source">Its original shared material.</param>
        /// <param name="order">The first painter order covered by this renderer.</param>
        internal static void RegisterRenderer(MeshRenderer renderer, Material source, int order)
        {
            RegisterOrder(order);
            s_world.Add(new WorldSpan(renderer, source, order));
        }

        /// <summary>Registers the completed native interface layers, including original controls and fonts.</summary>
        /// <param name="layers">The existing Canvas layer pool.</param>
        /// <param name="count">The active prefix submitted by the interface.</param>
        internal static void SetCanvasLayers(IReadOnlyList<Canvas> layers, int count)
        {
            s_canvases = layers;
            s_canvasCount = count;
            s_canvasOrders.Clear();
            for (var index = 0; index < count; index++)
            {
                var order = layers[index].sortingOrder;
                s_canvasOrders.Add(order);
                RegisterOrder(order);
            }
        }

        /// <summary>Finalizes the bounded mapping before any instance draw is registered.</summary>
        /// <param name="hasInstances">Whether this scene uses explicit GPU instance draws.</param>
        internal static void CompleteFrame(bool hasInstances)
        {
            if (s_canvases != null)
            {
                for (var index = 0; index < s_canvasCount; index++)
                {
                    var canvas = s_canvases[index];
                    if (canvas != null && canvas.isActiveAndEnabled)
                    {
                        RegisterOrder(s_canvasOrders[index]);
                    }
                }
            }
            s_enabled = hasInstances;
            ValidateNativeSortingLayers();
            if (hasInstances && s_orders.Count > MaximumSpans)
            {
                SelectFallback("The complete painter sequence exceeds the 3000..5000 queue range.");
            }
            ApplyWorldMaterials();
        }

        /// <summary>Resolves one compressed queue without changing original sorting orders or per-instance depths.</summary>
        /// <param name="order">The original first order of the draw.</param>
        /// <returns>The stable ordered queue, or the original transparent queue during complete fallback.</returns>
        internal static int GetQueue(int order)
        {
            if (!s_enabled || RequiresMeshFallback)
            {
                return FirstQueue;
            }
            var index = s_orders.BinarySearch(order);
            if (index < 0)
            {
                index = Math.Max(0, ~index - 1);
            }
            return FirstQueue + index;
        }

        /// <summary>Refreshes native Canvas material copies after geometry, fonts, and stencil materials have rebuilt.</summary>
        /// <param name="camera">The camera about to cull and draw native interface geometry.</param>
        internal static void PrepareCanvasForCamera(Camera camera)
        {
            ValidateNativeSortingLayers();
            if (s_canvases == null || s_canvasCount == 0)
            {
                return;
            }
            var first = s_canvases[0];
            if (first == null || camera != first.worldCamera && camera.cameraType != CameraType.SceneView)
            {
                return;
            }
            Canvas.ForceUpdateCanvases();
            s_refresh++;
            ApplyCanvasMaterials();
            if (RequiresMeshFallback)
            {
                // A material-budget miss discovered after a native mask/font rebuild must
                // restore every already visited layer, not only the remaining suffix.
                ApplyCanvasMaterials();
                ApplyWorldMaterials();
            }
        }

        /// <summary>Restores the original material queue contract after selecting complete mesh rendering.</summary>
        internal static void RestoreWorldMaterials()
        {
            s_enabled = false;
            ApplyWorldMaterials();
        }

        /// <summary>Selects native mesh ordering when an inspected component requires a non-default sorting layer.</summary>
        private static void ValidateNativeSortingLayers()
        {
            if (!s_enabled || RequiresMeshFallback)
            {
                return;
            }
            foreach (var span in s_world)
            {
                if (span.Renderer != null && span.Renderer.sortingLayerID != 0)
                {
                    SelectFallback("A native scene renderer requires a non-default sorting layer.");
                    return;
                }
            }
            if (s_canvases == null)
            {
                return;
            }
            for (var index = 0; index < s_canvasCount; index++)
            {
                var canvas = s_canvases[index];
                if (canvas != null && canvas.sortingLayerID != 0)
                {
                    SelectFallback("A native interface layer requires a non-default sorting layer.");
                    return;
                }
            }
        }

        /// <summary>Releases all material copies when the rendering session ends.</summary>
        internal static void Release()
        {
            foreach (var binding in s_worldMaterials.Values)
            {
                Destroy(binding.Copy);
            }
            foreach (var binding in s_canvasMaterials.Values)
            {
                Destroy(binding.Copy);
            }
            s_worldMaterials.Clear();
            s_canvasMaterials.Clear();
            s_sources.Clear();
            s_canvasRenderers.Clear();
            s_orders.Clear();
            s_world.Clear();
            s_canvases = null;
            s_canvasCount = 0;
            s_canvasOrders.Clear();
            s_enabled = false;
        }

        /// <summary>Assigns each active scene mesh an owned material copy with the same original properties.</summary>
        private static void ApplyWorldMaterials()
        {
            s_refresh++;
            foreach (var span in s_world)
            {
                if (span.Renderer == null || span.Source == null)
                {
                    continue;
                }
                if (!s_enabled || RequiresMeshFallback)
                {
                    span.Renderer.sharedMaterial = span.Source;
                    span.Renderer.sortingOrder = span.Order;
                    continue;
                }
                if (!s_worldMaterials.TryGetValue(span.Renderer, out var binding))
                {
                    if (!CanCreateMaterial())
                    {
                        SelectFallback("The retained ordered-material budget was exhausted.");
                        RestoreWorldMaterials();
                        return;
                    }
                    binding = new MaterialBinding(span.Source);
                    s_worldMaterials.Add(span.Renderer, binding);
                }
                binding.Refresh(span.Source, GetQueue(span.Order), s_refresh);
                // Explicit Graphics submissions do not share MeshRenderer's native
                // sorting-order key in this editor. The compressed material queue is
                // therefore the common ordering key; the original depth is unchanged.
                span.Renderer.sortingOrder = 0;
                if (span.Renderer.sharedMaterial != binding.Copy)
                {
                    span.Renderer.sharedMaterial = binding.Copy;
                }
            }
        }

        /// <summary>Rebinds native materials and mask-pop materials without replacing any CanvasRenderer geometry.</summary>
        private static void ApplyCanvasMaterials()
        {
            if (s_canvases == null)
            {
                return;
            }
            for (var layerIndex = 0; layerIndex < s_canvasCount; layerIndex++)
            {
                var canvas = s_canvases[layerIndex];
                if (canvas == null || !canvas.isActiveAndEnabled)
                {
                    continue;
                }
                s_canvasRenderers.Clear();
                canvas.GetComponentsInChildren(true, s_canvasRenderers);
                foreach (var renderer in s_canvasRenderers)
                {
                    for (var material = 0; material < renderer.materialCount; material++)
                    {
                        var current = renderer.GetMaterial(material);
                        var prepared = PrepareCanvasMaterial(canvas, current, s_canvasOrders[layerIndex]);
                        if (prepared != null && prepared != current)
                        {
                            renderer.SetMaterial(prepared, material);
                        }
                    }
                    for (var material = 0; material < renderer.popMaterialCount; material++)
                    {
                        var current = renderer.GetPopMaterial(material);
                        var prepared = PrepareCanvasMaterial(canvas, current, s_canvasOrders[layerIndex]);
                        if (prepared != null && prepared != current)
                        {
                            renderer.SetPopMaterial(prepared, material);
                        }
                    }
                }
            }
        }

        /// <summary>Finds an original native material and refreshes its bounded per-layer copy.</summary>
        /// <param name="canvas">The original independently sorted native layer.</param>
        /// <param name="current">The material produced by the latest native UI rebuild.</param>
        /// <param name="logicalOrder">The original painter order retained outside camera-local overrides.</param>
        /// <returns>The original material during fallback, otherwise the retained ordered copy.</returns>
        private static Material? PrepareCanvasMaterial(Canvas canvas, Material? current, int logicalOrder)
        {
            if (current == null)
            {
                return null;
            }
            var source = s_sources.TryGetValue(current, out var original) ? original : current;
            if (source == null)
            {
                // A native stencil cache can release its source after this retained copy
                // was bound. Keep the owned valid copy until the native rebuild supplies
                // a replacement; never dereference the destroyed original material.
                current.renderQueue = GetQueue(logicalOrder);
                return current;
            }
            if (!s_enabled || RequiresMeshFallback)
            {
                return source;
            }
            var key = (canvas, source);
            if (!s_canvasMaterials.TryGetValue(key, out var binding))
            {
                if (!CanCreateMaterial())
                {
                    SelectFallback("The retained ordered-material budget was exhausted after a native UI rebuild.");
                    return source;
                }
                binding = new MaterialBinding(source);
                s_canvasMaterials.Add(key, binding);
                s_sources.Add(binding.Copy, source);
            }
            binding.Refresh(source, GetQueue(logicalOrder), s_refresh);
            return binding.Copy;
        }

        /// <summary>Checks the explicit material budget before a native allocation.</summary>
        /// <returns>Whether another retained copy is admitted.</returns>
        private static bool CanCreateMaterial()
        {
            return s_worldMaterials.Count + s_canvasMaterials.Count < MaximumMaterials;
        }

        /// <summary>Requests original native ordering for every scene span after a bounded instancing miss.</summary>
        /// <param name="reason">The bounded condition requiring complete fallback.</param>
        internal static void RequireCompleteFallback(string reason)
        {
            SelectFallback(reason);
        }

        /// <summary>Records one complete-frame fallback without logging every subsequent frame.</summary>
        /// <param name="reason">The bounded-resource condition selecting the original renderer.</param>
        private static void SelectFallback(string reason)
        {
            RequiresMeshFallback = true;
            FallbackReason = reason;
            s_enabled = false;
        }

        /// <summary>Destroys one owned material using the current Unity lifecycle.</summary>
        /// <param name="material">The owned copy, never a source material.</param>
        private static void Destroy(Material material)
        {
            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(material);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(material);
            }
        }

        /// <summary>Captures one active native world renderer and its immutable source binding.</summary>
        private readonly struct WorldSpan
        {
            /// <summary>The retained scene renderer.</summary>
            internal readonly MeshRenderer Renderer;
            /// <summary>The original shared material.</summary>
            internal readonly Material Source;
            /// <summary>The first original painter order.</summary>
            internal readonly int Order;

            /// <summary>Creates a nonallocating registration snapshot.</summary>
            /// <param name="renderer">The native scene renderer.</param>
            /// <param name="source">Its unmodified shared material.</param>
            /// <param name="order">Its first original painter order.</param>
            internal WorldSpan(MeshRenderer renderer, Material source, int order)
            {
                Renderer = renderer;
                Source = source;
                Order = order;
            }
        }

        /// <summary>Retains one material copy while preserving live source properties and keywords.</summary>
        private sealed class MaterialBinding
        {
            /// <summary>The owned copy whose render queue may change.</summary>
            internal readonly Material Copy;
            /// <summary>The source used in the most recent refresh.</summary>
            private Material _source;
            /// <summary>The last camera refresh pass copied into this binding.</summary>
            private int _refresh = -1;

            /// <summary>Creates an independently owned native binding.</summary>
            /// <param name="source">The original native material, including stencil or font state.</param>
            internal MaterialBinding(Material source)
            {
                _source = source;
                Copy = new Material(source) { hideFlags = HideFlags.HideAndDontSave, name = "PvZ ordered " + source.name };
            }

            /// <summary>Copies live source properties once per camera and applies only the compressed queue override.</summary>
            /// <param name="source">The current original material.</param>
            /// <param name="queue">The queue assigned to this painter span.</param>
            /// <param name="refresh">The current complete material refresh pass.</param>
            internal void Refresh(Material source, int queue, int refresh)
            {
                if (_refresh != refresh || !ReferenceEquals(_source, source))
                {
                    if (Copy.shader != source.shader)
                    {
                        Copy.shader = source.shader;
                    }
                    Copy.CopyPropertiesFromMaterial(source);
                    _source = source;
                    _refresh = refresh;
                }
                if (Copy.renderQueue != queue)
                {
                    Copy.renderQueue = queue;
                }
            }
        }
    }
}
