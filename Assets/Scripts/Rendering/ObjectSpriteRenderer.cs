#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace PlantsVsZombies.Rendering
{
    /// <summary>Renders ordered texture quads through pooled GameObjects and a URP camera.</summary>
    /// <remarks>Call on Unity's main thread. Layer 31 is reserved for the isolated sprite cameras.</remarks>
    public sealed class ObjectSpriteRenderer : IDisposable
    {
        /// <summary>The horizontal and vertical offset separating the pool from gameplay objects.</summary>
        private const float PoolOffset = 100000f;
        /// <summary>The shader property containing the source texture.</summary>
        private static readonly int s_textureId = Shader.PropertyToID("_MainTex");
        /// <summary>The shader property selecting the native compositing operation.</summary>
        private static readonly int s_modeId = Shader.PropertyToID("_RenderMode");
        /// <summary>The shader property controlling imitation-plant grayscale.</summary>
        private static readonly int s_grayscaleId = Shader.PropertyToID("_Grayscale");
        /// <summary>The shader property controlling the source-colored producer highlight.</summary>
        private static readonly int s_producerId = Shader.PropertyToID("_ProducerAdditive");
        /// <summary>The shader property controlling the white hit highlight.</summary>
        private static readonly int s_additiveId = Shader.PropertyToID("_Additive");
        /// <summary>The shader property controlling the colored hit highlight.</summary>
        private static readonly int s_addColorId = Shader.PropertyToID("_NativeAddColor");
        /// <summary>The objects retained between frames to avoid per-frame mesh allocation.</summary>
        private readonly List<ObjectSprite> _sprites = new List<ObjectSprite>();
        /// <summary>The material shared by each blending operation.</summary>
        private readonly Material[] _materials = new Material[5];
        /// <summary>The source opacity classifications shared with related animation surface pools.</summary>
        private readonly TextureOpacityCache _textureOpacity;
        /// <summary>The scene root owning this pool.</summary>
        private readonly GameObject _root;
        /// <summary>The camera that renders this pool into its destination surface.</summary>
        private readonly Camera _camera;
        /// <summary>The reused request submitted to the Universal Render Pipeline.</summary>
        private readonly UniversalRenderPipeline.SingleCameraRequest _request = new UniversalRenderPipeline.SingleCameraRequest();
        /// <summary>The final premultiplied-alpha bridge to the existing IMGUI interface.</summary>
        private readonly Material _compositeMaterial;
        /// <summary>The default full-screen surface retained until the screen size changes.</summary>
        private RenderTexture? _screenSurface;
        /// <summary>The number of active nested screen drawing scopes.</summary>
        private int _scopeDepth;
        /// <summary>The number of pool slots used by the current batch.</summary>
        private int _used;
        /// <summary>Whether this pool has released its Unity resources.</summary>
        private bool _disposed;
        /// <summary>Gets the number of GameObjects retained by this pool.</summary>
        public int PooledObjectCount => _sprites.Count;
        /// <summary>Gets the number of quads submitted in the current batch.</summary>
        public int SubmittedObjectCount => _used;
        /// <summary>Gets whether a screen drawing scope is active.</summary>
        public bool IsFrameActive => _scopeDepth > 0;

        /// <summary>Creates the camera and shared materials used by this sprite pool.</summary>
        /// <exception cref = "InvalidOperationException">The required rendering shaders are missing.</exception>
        public ObjectSpriteRenderer()
            : this(new TextureOpacityCache())
        {
        }

        /// <summary>Creates a sprite pool sharing the source texture classifications of its owner.</summary>
        /// <param name="textureOpacity">The cache shared by related screen and native surface pools.</param>
        /// <exception cref="InvalidOperationException">The required rendering shaders are missing.</exception>
        internal ObjectSpriteRenderer(TextureOpacityCache textureOpacity)
        {
            _textureOpacity = textureOpacity;
            var shader = Resources.Load<Shader>("Shaders/PvZCombatSprite");
            var compositeShader = Resources.Load<Shader>("Shaders/PvZFrameComposite");
            if (shader == null || compositeShader == null)
            {
                throw new InvalidOperationException("The PvZ object rendering shaders are missing.");
            }

            for (var index = 0; index < _materials.Length; index++)
            {
                var material = new Material(shader)
                {
                    hideFlags = HideFlags.DontSave
                };
                material.SetInt("_SrcBlend", (int)(index == 3 ? BlendMode.One : BlendMode.SrcAlpha));
                material.SetInt("_DstBlend", (int)(index == 1 || index == 2 ? BlendMode.One : BlendMode.OneMinusSrcAlpha));
                material.SetInt("_SrcBlendAlpha", (int)(index == 1 || index == 2 ? BlendMode.Zero : BlendMode.One));
                material.SetInt("_DstBlendAlpha", (int)(index == 1 || index == 2 ? BlendMode.One : BlendMode.OneMinusSrcAlpha));
                material.SetInt("_ZWrite", index == 4 ? 1 : 0);
                material.SetInt("_ZTest", (int)CompareFunction.LessEqual);
                if (index == 4)
                {
                    material.renderQueue = (int)RenderQueue.Geometry;
                    material.SetOverrideTag("RenderType", "Opaque");
                    material.SetInt("_SrcBlend", (int)BlendMode.One);
                    material.SetInt("_DstBlend", (int)BlendMode.Zero);
                    material.SetInt("_SrcBlendAlpha", (int)BlendMode.One);
                    material.SetInt("_DstBlendAlpha", (int)BlendMode.Zero);
                }
                _materials[index] = material;
            }

            _compositeMaterial = new Material(compositeShader)
            {
                hideFlags = HideFlags.DontSave
            };
            _root = new GameObject("PvZ Object Sprite Pool")
            {
                hideFlags = HideFlags.DontSave,
                layer = 31
            };
            _root.transform.position = new Vector3(PoolOffset, PoolOffset, 0f);
            var cameraObject = new GameObject("PvZ Sprite Camera")
            {
                hideFlags = HideFlags.DontSave,
                layer = 31
            };
            cameraObject.transform.SetParent(_root.transform, false);
            _camera = cameraObject.AddComponent<Camera>();
            _camera.enabled = false;
            _camera.orthographic = true;
            _camera.nearClipPlane = 0.1f;
            _camera.farClipPlane = 20f;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = Color.clear;
            _camera.cullingMask = 1 << 31;
            _camera.useOcclusionCulling = false;
            _camera.allowHDR = false;
            _camera.allowMSAA = false;
            var cameraData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
            cameraData.renderPostProcessing = false;
            cameraData.requiresColorOption = CameraOverrideOption.Off;
            cameraData.requiresDepthOption = CameraOverrideOption.Off;
        }

        /// <summary>Begins or nests a screen batch that is composited after its outermost scope ends.</summary>
        /// <returns>A scope that completes this batch on disposal.</returns>
        /// <exception cref = "ObjectDisposedException">This pool has already been disposed.</exception>
        public ObjectRenderScope BeginFrame()
        {
            ThrowIfDisposed();
            if (_scopeDepth++ == 0)
            {
                BeginBatch();
            }

            return new ObjectRenderScope(this);
        }

        /// <summary>Renders and composites the screen batch after its outermost scope ends.</summary>
        /// <exception cref = "InvalidOperationException">The active pipeline does not support URP camera requests.</exception>
        public void EndFrame()
        {
            if (_scopeDepth == 0 || --_scopeDepth > 0)
            {
                return;
            }

            EnsureScreenSurface();
            RenderTo(_screenSurface!);
            var previousMatrix = GUI.matrix;
            var previousColor = GUI.color;
            try
            {
                GUI.matrix = Matrix4x4.identity;
                GUI.color = Color.white;
                Graphics.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), _screenSurface, new Rect(0f, 0f, 1f, 1f), 0, 0, 0, 0, Color.white, _compositeMaterial);
            }
            finally
            {
                GUI.matrix = previousMatrix;
                GUI.color = previousColor;
            }
        }

        /// <summary>Clears the submitted command count while retaining reusable scene objects.</summary>
        /// <exception cref = "ObjectDisposedException">This pool has already been disposed.</exception>
        public void BeginBatch()
        {
            ThrowIfDisposed();
            _used = 0;
        }

        /// <summary>Copies a textured quad into the next reusable renderer.</summary>
        /// <param name = "texture">The source texture or previously rendered surface.</param>
        /// <param name = "width">The local width of the quad in pixels.</param>
        /// <param name = "height">The local height of the quad in pixels.</param>
        /// <param name = "uv">The normalized source texture rectangle.</param>
        /// <param name = "matrix">The source-to-destination pixel transform.</param>
        /// <param name = "color">The source color and opacity.</param>
        /// <param name = "mode">Zero for normal alpha, one for hit addition, two for particle addition, or three for premultiplied surfaces.</param>
        /// <param name = "grayscale">The amount of imitation-plant grayscale.</param>
        /// <param name = "producerAdditive">The source-colored producer brightness.</param>
        /// <param name = "additive">The amount of white hit addition.</param>
        /// <param name = "addColor">The additional hit color.</param>
        /// <exception cref = "ObjectDisposedException">This pool has already been disposed.</exception>
        /// <exception cref = "ArgumentOutOfRangeException">The blending mode is unsupported or the frame exceeds Unity's sorting-order range.</exception>
        public void DrawQuad(Texture texture, float width, float height, Rect uv, Matrix4x4 matrix, Color color, int mode, float grayscale = 0f, float producerAdditive = 0f, float additive = 0f, Color addColor = default)
        {
            ThrowIfDisposed();
            if (mode < 0 || mode > 3 || _used >= short.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(mode), "The sprite mode or frame sorting range is invalid.");
            }

            if (_used == _sprites.Count)
            {
                _sprites.Add(new ObjectSprite(_root.transform, _used));
            }

            var sprite = _sprites[_used];
            var depth = -_used / 32768f;
            SetCorner(sprite, 0, matrix, 0f, 0f, depth, color);
            SetCorner(sprite, 1, matrix, width, 0f, depth, color);
            SetCorner(sprite, 2, matrix, width, height, depth, color);
            SetCorner(sprite, 3, matrix, 0f, height, depth, color);
            sprite.TextureCoordinates[0] = new Vector2(uv.xMin, uv.yMax);
            sprite.TextureCoordinates[1] = new Vector2(uv.xMax, uv.yMax);
            sprite.TextureCoordinates[2] = new Vector2(uv.xMax, uv.yMin);
            sprite.TextureCoordinates[3] = new Vector2(uv.xMin, uv.yMin);
            sprite.Mesh.vertices = sprite.Vertices;
            sprite.Mesh.uv = sprite.TextureCoordinates;
            sprite.Mesh.colors = sprite.Colors;
            sprite.Mesh.RecalculateBounds();
            var opaque = mode == 0 && color.a >= 1f && _textureOpacity.IsOpaque(texture);
            sprite.Renderer.sharedMaterial = _materials[opaque ? 4 : mode];
            sprite.Renderer.sortingOrder = opaque ? 0 : _used;
            _used++;
            var properties = sprite.Properties;
            properties.SetTexture(s_textureId, texture);
            properties.SetFloat(s_modeId, mode);
            properties.SetFloat(s_grayscaleId, grayscale);
            properties.SetFloat(s_producerId, producerAdditive);
            properties.SetFloat(s_additiveId, additive);
            properties.SetColor(s_addColorId, addColor);
            sprite.Renderer.SetPropertyBlock(properties);
        }

        /// <summary>Renders the submitted objects into the supplied surface through URP.</summary>
        /// <param name = "destination">The render texture receiving this batch.</param>
        /// <exception cref = "ObjectDisposedException">This pool has already been disposed.</exception>
        /// <exception cref = "InvalidOperationException">The active pipeline does not support URP camera requests.</exception>
        public void RenderTo(RenderTexture destination)
        {
            ThrowIfDisposed();
            _camera.transform.localPosition = new Vector3(destination.width * 0.5f, -destination.height * 0.5f, -10f);
            _camera.orthographicSize = destination.height * 0.5f;
            _camera.aspect = destination.width / (float)destination.height;
            _request.destination = destination;
            if (!RenderPipeline.SupportsRenderRequest(_camera, _request))
            {
                throw new InvalidOperationException("PvZ sprite rendering requires the configured Universal Render Pipeline.");
            }

            var previousActive = RenderTexture.active;
            try
            {
                for (var index = 0; index < _used; index++)
                {
                    _sprites[index].Renderer.enabled = true;
                }

                RenderPipeline.SubmitRenderRequest(_camera, _request);
            }
            finally
            {
                RenderTexture.active = previousActive;
                for (var index = 0; index < _used; index++)
                {
                    _sprites[index].Renderer.enabled = false;
                }
            }
        }

        /// <summary>Releases all meshes, materials, render textures, cameras, and pooled objects.</summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            foreach (var sprite in _sprites)
            {
                DestroyResource(sprite.Mesh);
            }

            foreach (var material in _materials)
            {
                DestroyResource(material);
            }

            if (_screenSurface != null)
            {
                _screenSurface.Release();
                DestroyResource(_screenSurface);
            }

            DestroyResource(_compositeMaterial);
            DestroyResource(_root);
            _sprites.Clear();
            _disposed = true;
        }

        /// <summary>Writes one transformed pixel-space corner using Unity's upward-positive coordinates.</summary>
        /// <param name = "sprite">The reusable quad receiving the corner.</param>
        /// <param name = "index">The corner to replace.</param>
        /// <param name = "matrix">The source-to-destination pixel transform.</param>
        /// <param name = "x">The source horizontal coordinate.</param>
        /// <param name = "y">The source vertical coordinate.</param>
        /// <param name = "color">The vertex tint.</param>
        /// <param name="depth">The depth preserving the original painter order across render queues.</param>
        private static void SetCorner(ObjectSprite sprite, int index, Matrix4x4 matrix, float x, float y, float depth, Color color)
        {
            var point = matrix.MultiplyPoint3x4(new Vector3(x, y, 0f));
            sprite.Vertices[index] = new Vector3(point.x, -point.y, depth);
            sprite.Colors[index] = color;
        }

        /// <summary>Allocates or resizes the retained full-screen output texture.</summary>
        private void EnsureScreenSurface()
        {
            if (_screenSurface != null && _screenSurface.width == Screen.width && _screenSurface.height == Screen.height)
            {
                return;
            }

            if (_screenSurface != null)
            {
                _screenSurface.Release();
                DestroyResource(_screenSurface);
            }

            _screenSurface = new RenderTexture(Math.Max(1, Screen.width), Math.Max(1, Screen.height), 24, RenderTextureFormat.ARGB32)
            {
                name = "PvZ object frame",
                hideFlags = HideFlags.DontSave,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            _screenSurface.Create();
        }

        /// <summary>Rejects commands issued after the pool has released its Unity resources.</summary>
        /// <exception cref = "ObjectDisposedException">This pool has already been disposed.</exception>
        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(ObjectSpriteRenderer));
            }
        }

        /// <summary>Destroys a resource using the correct editor or player lifetime operation.</summary>
        /// <param name = "resource">The Unity resource owned by this pool.</param>
        private static void DestroyResource(UnityEngine.Object resource)
        {
            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(resource);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(resource);
            }
        }
    }
}
