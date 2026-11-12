#nullable enable
using System;
using PlantsVsZombies.Rendering.Pipeline;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace PlantsVsZombies.Editor
{
    /// <summary>Creates the project render pipeline asset and selects it in the graphics and quality settings.</summary>
    /// <remarks>Run from the menu or with <c>-executeMethod PlantsVsZombies.Editor.PvZRenderPipelineConfiguration.Configure</c>.</remarks>
    public static class PvZRenderPipelineConfiguration
    {
        /// <summary>The project path of the shared pipeline asset.</summary>
        public const string PipelinePath = "Assets/Settings/PvZRenderPipeline.asset";

        /// <summary>Creates the pipeline asset when missing, selects it everywhere, and disables multisampling on every quality level.</summary>
        /// <exception cref="InvalidOperationException">The Editor version differs from the project version.</exception>
        [MenuItem("PlantsVsZombies/Configure Render Pipeline")]
        public static void Configure()
        {
            if (Application.unityVersion != "6000.3.17f1")
            {
                throw new InvalidOperationException("Use Unity 6000.3.17f1 to configure this project.");
            }

            if (!AssetDatabase.IsValidFolder("Assets/Settings"))
            {
                AssetDatabase.CreateFolder("Assets", "Settings");
            }

            var pipeline = AssetDatabase.LoadAssetAtPath<PvZRenderPipelineAsset>(PipelinePath);
            if (pipeline == null)
            {
                pipeline = ScriptableObject.CreateInstance<PvZRenderPipelineAsset>();
                pipeline.name = "PvZRenderPipeline";
                AssetDatabase.CreateAsset(pipeline, PipelinePath);
            }

            GraphicsSettings.defaultRenderPipeline = pipeline;
            var activeQuality = QualitySettings.GetQualityLevel();
            try
            {
                for (var index = 0; index < QualitySettings.names.Length; index++)
                {
                    QualitySettings.SetQualityLevel(index, false);
                    QualitySettings.renderPipeline = pipeline;
                    // The pipeline has no multisampled targets; the display buffer must match.
                    QualitySettings.antiAliasing = 0;
                }
            }
            finally
            {
                QualitySettings.SetQualityLevel(activeQuality, false);
            }

            EditorSettings.projectGenerationRootNamespace = "PlantsVsZombies";
            AssetDatabase.SaveAssets();
            Debug.Log($"{nameof(PvZRenderPipelineConfiguration)}: Configured {PipelinePath} for graphics and all quality levels.");
        }
    }
}
