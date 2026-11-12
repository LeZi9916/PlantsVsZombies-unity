#nullable enable

using NUnit.Framework;
using PlantsVsZombies.Rendering.Pipeline;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Verifies the persisted render pipeline configuration and the shader written for it.</summary>
    public sealed class PvZRenderPipelineAssetTests
    {
        /// <summary>The project path of the configured pipeline asset.</summary>
        private const string PipelinePath = "Assets/Settings/PvZRenderPipeline.asset";

        /// <summary>Checks that graphics settings select the persisted project pipeline and no quality level overrides it.</summary>
        [Test]
        public void GraphicsAndQualitySettingsSelectProjectPipeline()
        {
            var asset = AssetDatabase.LoadAssetAtPath<PvZRenderPipelineAsset>(PipelinePath);
            Assert.That(asset, Is.Not.Null, PipelinePath);
            Assert.That(GraphicsSettings.defaultRenderPipeline, Is.SameAs(asset));
            for (var index = 0; index < QualitySettings.names.Length; index++)
            {
                var levelPipeline = QualitySettings.GetRenderPipelineAssetAt(index);
                Assert.That(levelPipeline == null || levelPipeline == asset, Is.True, QualitySettings.names[index]);
            }
        }

        /// <summary>Checks the defaults of a newly created pipeline asset.</summary>
        [Test]
        public void NewAssetUsesProjectShaderTagAndSrpBatcher()
        {
            var asset = ScriptableObject.CreateInstance<PvZRenderPipelineAsset>();
            try
            {
                Assert.That(asset.renderPipelineShaderTag, Is.EqualTo(PvZRenderPipelineAsset.ShaderTag));
                Assert.That(asset.pipelineType, Is.EqualTo(typeof(PvZRenderPipeline)));
                Assert.That(asset.UseSrpBatcher, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(asset);
            }
        }

        /// <summary>Checks that the sprite shader compiles without a pipeline package and targets the project pipeline.</summary>
        [Test]
        public void SpriteShaderCompilesForProjectPipeline()
        {
            var shader = Resources.Load<Shader>("Shaders/PvZCombatSprite");
            Assert.That(shader, Is.Not.Null);
            if (shader == null)
            {
                return;
            }

            Assert.That(ShaderUtil.ShaderHasError(shader), Is.False);
            // Tag values are interned case-insensitively, so compare identifiers rather than names.
            Assert.That(shader.FindSubshaderTagValue(0, new ShaderTagId("RenderPipeline")), Is.EqualTo(new ShaderTagId(PvZRenderPipelineAsset.ShaderTag)));
            Assert.That(shader.FindPassTagValue(0, new ShaderTagId("LightMode")), Is.EqualTo(new ShaderTagId("SRPDefaultUnlit")));
        }
    }
}
