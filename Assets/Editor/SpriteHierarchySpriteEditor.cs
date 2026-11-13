#nullable enable
using PlantsVsZombies.Rendering;
using UnityEditor;
using UnityEngine;

namespace PlantsVsZombies.Editor
{
    /// <summary>Displays sampled sprite metadata without exposing animation pose edits that batching cannot apply.</summary>
    [CustomEditor(typeof(SpriteHierarchySprite))]
    internal sealed class SpriteHierarchySpriteEditor : UnityEditor.Editor
    {
        /// <summary>Displays source dimensions, tint, UVs, and the complete affine target transform as read-only diagnostics.</summary>
        public override void OnInspectorGUI()
        {
            var sprite = (SpriteHierarchySprite)target;
            EditorGUILayout.HelpBox("Use the GameObject active switch to hide this sprite. These nodes provide inspection and visibility; Transform edits do not change the sampled artwork.", MessageType.Info);
            if (!sprite.HasPose)
            {
                EditorGUILayout.LabelField("No visible source geometry has been submitted for this track.");
                return;
            }

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField("Original image", sprite.SourceIdentity);
                EditorGUILayout.Vector2Field("Logical size", sprite.LogicalSize);
                EditorGUILayout.RectField("Source UV", sprite.SourceUv);
                EditorGUILayout.ColorField("Tint", sprite.Tint);
                EditorGUILayout.IntField("Render mode", sprite.RenderMode);
                EditorGUILayout.LabelField("Destination transform (target pixels)");
                for (var row = 0; row < 4; row++)
                {
                    EditorGUILayout.Vector4Field("Row " + row, sprite.DestinationTransform.GetRow(row));
                }
            }
        }
    }
}
