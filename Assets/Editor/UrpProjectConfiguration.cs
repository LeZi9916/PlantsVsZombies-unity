#nullable enable
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace PlantsVsZombies.Editor
{
    /// <summary>Creates the shared unlit Universal Render Pipeline configuration.</summary>
    public static class UrpProjectConfiguration
    {
        /// <summary>Stores the project pipeline asset path.</summary>
        private const string PipelinePath = "Assets/Settings/PlantsVsZombiesPipeline.asset";
        /// <summary>Stores the project renderer asset path.</summary>
        private const string RendererPath = "Assets/Settings/PlantsVsZombiesRenderer.asset";
        /// <summary>Creates missing rendering assets and assigns the pipeline to all quality levels.</summary>
        /// <exception cref = "InvalidOperationException">The Editor version differs from the project version.</exception>
        [MenuItem("PlantsVsZombies/Configure Universal Render Pipeline")]
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

            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (renderer == null)
            {
                renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                renderer.name = "PlantsVsZombiesRenderer";
                renderer.renderingMode = RenderingMode.Forward;
                AssetDatabase.CreateAsset(renderer, RendererPath);
            }

            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (pipeline == null)
            {
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                pipeline.name = "PlantsVsZombiesPipeline";
                pipeline.supportsHDR = false;
                pipeline.msaaSampleCount = 1;
                pipeline.renderScale = 1f;
                pipeline.supportsCameraDepthTexture = false;
                pipeline.supportsCameraOpaqueTexture = false;
                pipeline.useSRPBatcher = true;
                var settings = new SerializedObject(pipeline);
                settings.FindProperty("m_MainLightRenderingMode").intValue = (int)LightRenderingMode.Disabled;
                settings.FindProperty("m_AdditionalLightsRenderingMode").intValue = (int)LightRenderingMode.Disabled;
                settings.FindProperty("m_MainLightShadowsSupported").boolValue = false;
                settings.FindProperty("m_AdditionalLightShadowsSupported").boolValue = false;
                settings.FindProperty("m_SoftShadowsSupported").boolValue = false;
                settings.ApplyModifiedPropertiesWithoutUndo();
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
                }
            }
            finally
            {
                QualitySettings.SetQualityLevel(activeQuality, false);
            }

            EditorSettings.projectGenerationRootNamespace = "PlantsVsZombies";
            AssetDatabase.SaveAssets();
            Debug.Log("[Rendering] Configured the shared Universal Render Pipeline.");
        }
    }
}
