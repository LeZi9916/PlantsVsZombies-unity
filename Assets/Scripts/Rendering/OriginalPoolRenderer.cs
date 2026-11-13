#nullable enable

using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace PlantsVsZombies.Rendering
{
    /// <summary>Draws PC PoolEffect's three original grids with reusable meshes and its original caustic sampler.</summary>
    internal sealed class OriginalPoolRenderer : IDisposable
    {
        /// <summary>The source texture property shared with the normal sprite shader.</summary>
        private static readonly int s_texture = Shader.PropertyToID("_MainTex");
        /// <summary>The screen clipping property shared with scene sprites.</summary>
        private static readonly int s_clip = Shader.PropertyToID("_ClipRect");
        /// <summary>The shader switch enabling the shared screen clipping bounds.</summary>
        private static readonly int s_clipEnabled = Shader.PropertyToID("_ClipEnabled");
        /// <summary>Owns the three retained scene objects.</summary>
        private readonly GameObject _root;
        /// <summary>The original pool size used by the first two grids.</summary>
        private readonly Vector2 _poolSize;
        /// <summary>The original day and night base and shading images, owned by the host's texture cache.</summary>
        private readonly Texture2D[] _artwork;
        /// <summary>The three reusable pool meshes.</summary>
        private readonly Mesh[] _meshes = new Mesh[3];
        /// <summary>The three retained scene renderers.</summary>
        private readonly MeshRenderer[] _renderers = new MeshRenderer[3];
        /// <summary>The reusable transformed positions for each original grid.</summary>
        private readonly Vector3[][] _vertices = new Vector3[3][];
        /// <summary>The reusable original source coordinates for each original grid.</summary>
        private readonly Vector2[][] _uvs = new Vector2[3][];
        /// <summary>The reusable inherited tint and native vertex alpha for each grid.</summary>
        private readonly Color[][] _colors = new Color[3][];
        /// <summary>The shared original sprite material used by all three layers.</summary>
        private readonly Material _material;
        /// <summary>Reuses per-renderer texture and clip properties.</summary>
        private readonly MaterialPropertyBlock _properties = new MaterialPropertyBlock();
        /// <summary>The original top-to-bottom 256-by-256 caustic intensity data.</summary>
        private readonly byte[] _source = new byte[256 * 256];
        /// <summary>The reusable bottom-to-top dynamic caustic output.</summary>
        private readonly Color32[] _causticPixels = new Color32[OriginalPoolEffectRules.CausticWidth * OriginalPoolEffectRules.CausticHeight];
        /// <summary>The original-sized dynamic caustic texture owned by this renderer.</summary>
        private readonly Texture2D _caustics;
        /// <summary>The last native tick sampled into the UV and caustic buffers.</summary>
        private int _sampledTicks = -1;
        /// <summary>Whether this renderer has released its Unity resources.</summary>
        private bool _disposed;

        /// <summary>Creates three scene meshes and reads the original caustic intensity image once.</summary>
        /// <param name="poolSize">The PC IMAGE_POOL dimensions used by its grid geometry.</param>
        /// <param name="dayBase">The original day base image with its original alpha mask.</param>
        /// <param name="nightBase">The original night base image with the shared base alpha mask.</param>
        /// <param name="dayShading">The original day shading image with its original alpha mask.</param>
        /// <param name="nightShading">The original night shading image with the shared shading alpha mask.</param>
        /// <param name="causticSource">The original 256-by-256 PC grayscale caustic image.</param>
        /// <exception cref="ArgumentException">The original caustic image dimensions differ from the PC resource.</exception>
        /// <exception cref="InvalidOperationException">The shared sprite shader is unavailable.</exception>
        internal OriginalPoolRenderer(Vector2 poolSize, Texture2D dayBase, Texture2D nightBase,
            Texture2D dayShading, Texture2D nightShading, Texture2D causticSource)
        {
            if (causticSource.width != 256 || causticSource.height != 256)
            {
                throw new ArgumentException("The original pool caustic source must be 256 by 256 pixels.", nameof(causticSource));
            }

            var shader = Resources.Load<Shader>("Shaders/PvZCombatSprite")
                ?? throw new InvalidOperationException("The PvZ object rendering shader is missing.");
            _poolSize = poolSize;
            _artwork = new[] { dayBase, nightBase, dayShading, nightShading };
            var sourcePixels = causticSource.GetPixels32();
            for (var row = 0; row < 256; row++)
            {
                for (var column = 0; column < 256; column++)
                {
                    // PC MemoryImage stores rows downwards and reads the first BGR byte.
                    _source[row * 256 + column] = sourcePixels[(255 - row) * 256 + column].b;
                }
            }

            _caustics = new Texture2D(OriginalPoolEffectRules.CausticWidth, OriginalPoolEffectRules.CausticHeight, TextureFormat.RGBA32, false)
            {
                name = "PC pool caustics",
                hideFlags = HideFlags.DontSave,
                filterMode = FilterMode.Bilinear,
                // PC 0x4796F7..0x47974E switches both texture address axes to WRAP for this layer.
                wrapMode = TextureWrapMode.Repeat
            };
            _material = new Material(shader) { hideFlags = HideFlags.DontSave };
            _material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            _material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            _material.SetInt("_SrcBlendAlpha", (int)BlendMode.One);
            _material.SetInt("_DstBlendAlpha", (int)BlendMode.OneMinusSrcAlpha);
            _material.SetInt("_ZWrite", 0);
            _material.SetInt("_ZTest", (int)CompareFunction.LessEqual);
            _root = new GameObject("PvZ Original Pool") { hideFlags = HideFlags.DontSave, layer = 31 };
            var triangles = new int[OriginalPoolEffectRules.Columns * OriginalPoolEffectRules.Rows * 6];
            for (var column = 0; column < OriginalPoolEffectRules.Columns; column++)
            {
                for (var row = 0; row < OriginalPoolEffectRules.Rows; row++)
                {
                    var index = (column * OriginalPoolEffectRules.Rows + row) * 6;
                    var topLeft = column * (OriginalPoolEffectRules.Rows + 1) + row;
                    var topRight = topLeft + OriginalPoolEffectRules.Rows + 1;
                    triangles[index] = topLeft;
                    triangles[index + 1] = topLeft + 1;
                    triangles[index + 2] = topRight + 1;
                    triangles[index + 3] = topLeft;
                    triangles[index + 4] = topRight + 1;
                    triangles[index + 5] = topRight;
                }
            }

            for (var layer = 0; layer < 3; layer++)
            {
                var child = new GameObject("PC pool layer " + layer) { hideFlags = HideFlags.DontSave, layer = 31 };
                child.transform.SetParent(_root.transform, false);
                var mesh = new Mesh { name = child.name, hideFlags = HideFlags.DontSave };
                mesh.MarkDynamic();
                _vertices[layer] = new Vector3[OriginalPoolEffectRules.VertexCount];
                _uvs[layer] = new Vector2[OriginalPoolEffectRules.VertexCount];
                _colors[layer] = new Color[OriginalPoolEffectRules.VertexCount];
                mesh.vertices = _vertices[layer];
                mesh.triangles = triangles;
                child.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = child.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = _material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.lightProbeUsage = LightProbeUsage.Off;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
                renderer.enabled = false;
                _meshes[layer] = mesh;
                _renderers[layer] = renderer;
            }
        }

        /// <summary>Hides the retained meshes before a new screen is submitted.</summary>
        internal void Hide()
        {
            if (_disposed)
            {
                return;
            }

            for (var layer = 0; layer < _renderers.Length; layer++)
            {
                _renderers[layer].enabled = false;
            }
        }

        /// <summary>Draws one original pool frame at the current shared world transform and painter order.</summary>
        /// <param name="ticks">The original independent pool animation counter.</param>
        /// <param name="night">Whether to select the original night pool artwork and caustic opacity.</param>
        /// <exception cref="ObjectDisposedException">The renderer has already released its resources.</exception>
        internal void Draw(int ticks, bool night)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(OriginalPoolRenderer));
            }

            var changed = _sampledTicks != ticks;
            if (changed)
            {
                OriginalPoolEffectRules.FillCaustics(_source, _causticPixels, ticks);
                _caustics.SetPixels32(_causticPixels);
                _caustics.Apply(false, false);
                _sampledTicks = ticks;
            }

            var matrix = SpriteDrawState.Matrix;
            var tint = SpriteDrawState.Color;
            var clip = SpriteDrawState.Clip;
            for (var layer = 0; layer < 3; layer++)
            {
                var order = SpriteDrawState.NextOrder();
                var depth = -order / 32768f;
                for (var column = 0; column <= OriginalPoolEffectRules.Columns; column++)
                {
                    for (var row = 0; row <= OriginalPoolEffectRules.Rows; row++)
                    {
                        var index = column * (OriginalPoolEffectRules.Rows + 1) + row;
                        var local = OriginalPoolEffectRules.Position(layer, column, row, _poolSize);
                        var point = matrix.MultiplyPoint3x4(new Vector3(local.x, local.y, 0f));
                        _vertices[layer][index] = new Vector3(point.x, -point.y, depth);
                        var color = tint;
                        if (layer == 2)
                        {
                            color.a *= OriginalPoolEffectRules.CausticVertexAlpha(night, column, row) / 255f;
                        }

                        _colors[layer][index] = color;
                        if (changed)
                        {
                            var uv = OriginalPoolEffectRules.Uv(layer, column, row, ticks);
                            _uvs[layer][index] = new Vector2(uv.x, 1f - uv.y);
                        }
                    }
                }

                var mesh = _meshes[layer];
                mesh.vertices = _vertices[layer];
                mesh.colors = _colors[layer];
                if (changed)
                {
                    mesh.uv = _uvs[layer];
                }

                mesh.RecalculateBounds();
                var renderer = _renderers[layer];
                renderer.sortingOrder = order;
                _properties.SetTexture(s_texture, layer == 2 ? _caustics : _artwork[(layer == 0 ? 0 : 2) + (night ? 1 : 0)]);
                _properties.SetVector(s_clip, new Vector4(clip.xMin, clip.yMin, clip.xMax, clip.yMax));
                _properties.SetFloat(s_clipEnabled, 1f);
                renderer.SetPropertyBlock(_properties);
                renderer.enabled = true;
            }

            _ = ObjectSpriteRenderer.SceneCamera;
        }

        /// <summary>Releases only the meshes, generated caustic image, material, and scene objects owned here.</summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            Hide();
            for (var layer = 0; layer < _meshes.Length; layer++)
            {
                Release(_meshes[layer]);
            }

            Release(_caustics);
            Release(_material);
            Release(_root);
            _disposed = true;
        }

        /// <summary>Destroys one owned Unity resource on the current application lifecycle.</summary>
        /// <param name="resource">The owned resource that is no longer needed.</param>
        private static void Release(UnityEngine.Object resource)
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
