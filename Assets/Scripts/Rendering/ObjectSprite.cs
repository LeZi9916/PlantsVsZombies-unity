#nullable enable
using UnityEngine;
using UnityEngine.Rendering;

namespace PlantsVsZombies.Rendering
{
    /// <summary>Owns one reusable scene object and the geometry used by a sprite draw.</summary>
    internal sealed class ObjectSprite
    {
        /// <summary>Gets the scene object containing the renderer and mesh filter.</summary>
        internal GameObject GameObject { get; }
        /// <summary>Gets the dynamic quad mesh owned by this sprite.</summary>
        internal Mesh Mesh { get; }
        /// <summary>Gets the renderer used by the Universal Render Pipeline.</summary>
        internal MeshRenderer Renderer { get; }
        /// <summary>Gets the per-draw material values reused by this sprite.</summary>
        internal MaterialPropertyBlock Properties { get; } = new MaterialPropertyBlock();
        /// <summary>Gets the reusable positions of the four quad corners.</summary>
        internal Vector3[] Vertices { get; } = new Vector3[4];
        /// <summary>Gets the reusable texture coordinates of the four quad corners.</summary>
        internal Vector2[] TextureCoordinates { get; } = new Vector2[4];
        /// <summary>Gets the reusable tint values of the four quad corners.</summary>
        internal Color[] Colors { get; } = new Color[4];

        /// <summary>Creates a reusable sprite below the specified pool root.</summary>
        /// <param name = "parent">The transform owning this sprite pool.</param>
        /// <param name = "index">The stable pool slot displayed in the hierarchy.</param>
        internal ObjectSprite(Transform parent, int index)
        {
            GameObject = new GameObject($"Sprite {index}")
            {
                hideFlags = HideFlags.DontSave,
                layer = 31
            };
            GameObject.transform.SetParent(parent, false);
            Mesh = new Mesh
            {
                name = $"PvZ sprite {index}",
                hideFlags = HideFlags.DontSave
            };
            Mesh.MarkDynamic();
            Mesh.vertices = Vertices;
            Mesh.uv = TextureCoordinates;
            Mesh.colors = Colors;
            Mesh.triangles = new[]
            {
                0,
                1,
                2,
                0,
                2,
                3
            };
            GameObject.AddComponent<MeshFilter>().sharedMesh = Mesh;
            Renderer = GameObject.AddComponent<MeshRenderer>();
            Renderer.shadowCastingMode = ShadowCastingMode.Off;
            Renderer.receiveShadows = false;
            Renderer.lightProbeUsage = LightProbeUsage.Off;
            Renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            Renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            Renderer.enabled = false;
        }
    }
}
